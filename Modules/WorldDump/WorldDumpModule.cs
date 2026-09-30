using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AKeepersNeed2.Core;
using BepInEx;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;

namespace AKeepersNeed2.Modules.WorldDump;

/// <summary>
/// Temporary research aid: a few seconds after a save loads, writes the weather state machine and
/// a summary of every world object (grouped by interaction type and definition id) to
/// <c>BepInEx/AKN_world_dump.txt</c>. Read-only. Remove once the map markers are designed.
/// </summary>
internal sealed class WorldDumpModule : IModule, IUpdatable
{
    private const float DelaySeconds = 5f;
    private const string FileName = "AKN_world_dump.txt";

    private static readonly FieldInfo OwnerField = AccessTools.Field(typeof(WeatherSystem), "fsmOwner");

    private PlayerData _dumpedFor;
    private float _readyAt = -1f;

    public string Name => "WorldDump";

    public int Order => 0;

    public void Enable()
    {
    }

    public void Disable()
    {
    }

    public void Tick()
    {
        PlayerData player = MainGame.PlayerData;
        if (player == null || player == _dumpedFor || MainGame.Instance?.GameSave?.worldData == null)
        {
            return;
        }
        if (_readyAt < 0f)
        {
            _readyAt = Time.realtimeSinceStartup + DelaySeconds;
            return;
        }
        if (Time.realtimeSinceStartup < _readyAt)
        {
            return;
        }
        _dumpedFor = player;
        _readyAt = -1f;
        string path = Path.Combine(Paths.BepInExRootPath, FileName);
        var text = new StringBuilder();
        try
        {
            text.AppendLine($"A Keepers Need 2 world dump, {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Player scene: {player.currentGameSceneId}");
            text.AppendLine();
            AppendWeather(text);
            text.AppendLine();
            AppendWorld(text);
        }
        catch (Exception e)
        {
            text.AppendLine($"!! dump failed: {e}");
        }
        File.WriteAllText(path, text.ToString());
        Plugin.Logger.LogInfo($"[WorldDump] wrote {path}");
    }

    private static void AppendWeather(StringBuilder text)
    {
        text.AppendLine("== Weather ==");
        WeatherData data = MainGame.Instance.GameSave.weatherData;
        text.AppendLine(
            $"current={data.stateName} forced={data.hasForceState} phase={data.currentPhaseLen:0.###} "
                + $"pausedIndoor={data.isWeatherPausedByTimeOfDay} "
                + $"pausedCinematics={data.isWeatherPausedByCinematics}"
        );
        text.AppendLine($"enabled components: {string.Join(", ", data.enabledWeatherComponents)}");
        WeatherSystem system = WeatherSystem.Instance;
        if (system == null)
        {
            text.AppendLine("no WeatherSystem");
            return;
        }
        text.AppendLine($"all components: {string.Join(", ", system.components.Keys.ToArray())}");
        object owner = OwnerField?.GetValue(system);
        if (owner == null)
        {
            text.AppendLine("no FSMOwner");
            return;
        }
        object current = Traverse.Create(owner).Method("GetCurrentState", true).GetValue();
        text.AppendLine($"current node: {NodeName(current)} ({current?.GetType().Name})");
        AppendFsm(text, Traverse.Create(owner).Property("behaviour").GetValue(), 0);
    }

    private static void AppendFsm(StringBuilder text, object fsm, int depth)
    {
        string indent = new string(' ', depth * 2);
        if (fsm == null)
        {
            text.AppendLine($"{indent}(no graph)");
            return;
        }
        text.AppendLine($"{indent}graph {fsm.GetType().Name} '{Traverse.Create(fsm).Property("name").GetValue()}'");
        if (!(Traverse.Create(fsm).Property("allNodes").GetValue() is IEnumerable nodes))
        {
            text.AppendLine($"{indent}(no allNodes)");
            return;
        }
        foreach (object node in nodes)
        {
            Traverse t = Traverse.Create(node);
            var weather = t.Field("weather").GetValue() as UnityEngine.Object;
            text.AppendLine(
                $"{indent}- {node.GetType().Name} '{NodeName(node)}' weather={(weather != null ? weather.name : "-")}"
            );
            if (t.Field("exits").GetValue() is IEnumerable exits)
            {
                foreach (object exit in exits)
                {
                    Traverse e = Traverse.Create(exit);
                    object target = Traverse.Create(e.Field("connection").GetValue()).Property("targetNode").GetValue();
                    text.AppendLine($"{indent}    -> {NodeName(target)} w={e.Field("w").GetValue()}");
                }
            }
            object nested = t.Property("currentInstance").GetValue() ?? t.Property("subGraph").GetValue();
            if (nested != null && depth < 3)
            {
                AppendFsm(text, nested, depth + 1);
            }
        }
    }

    private static string NodeName(object node)
    {
        return node == null
            ? "null"
            : Traverse.Create(node).Property("name").GetValue() as string ?? "?";
    }

    private sealed class Group
    {
        public string Type;
        public string Id;
        public string WgoGroup;
        public string Label;
        public string Drop;
        public int Count;
        public int Hidden;
        public readonly HashSet<string> Scenes = new HashSet<string>();
    }

    private static void AppendWorld(StringBuilder text)
    {
        text.AppendLine("== World objects (type, def id, count, hidden, group, name, first drop, scenes) ==");
        var groups = new Dictionary<string, Group>();
        foreach (GameSceneData scene in MainGame.WorldData.gameSceneDataList)
        {
            int count = 0;
            foreach (WgoData wgo in scene.wgoDataList)
            {
                count++;
                WGODef def = wgo.Definition;
                string type = def != null
                    ? def.interactionType.ToString()
                    : "NoDef";
                string id = def != null
                    ? def.id
                    : wgo.id;
                string key = type + "|" + id;
                if (!groups.TryGetValue(key, out Group group))
                {
                    group = new Group
                    {
                        Type = type,
                        Id = id,
                        WgoGroup = def?.wgoGroup,
                        Label = def != null
                            ? SafeLabel(def.id)
                            : string.Empty,
                        Drop = def?.deathChanceItems?.chanceOutputItems?.FirstOrDefault()?.id,
                    };
                    groups.Add(key, group);
                }
                group.Count++;
                if (wgo.IsHidden)
                {
                    group.Hidden++;
                }
                group.Scenes.Add(scene.id);
            }
            text.AppendLine($"scene {scene.id}: {count} objects");
        }
        text.AppendLine();
        foreach (Group group in groups.Values.OrderBy(g => g.Type).ThenByDescending(g => g.Count))
        {
            text.AppendLine(
                $"{group.Type,-18} {group.Id,-45} n={group.Count,-5} hidden={group.Hidden,-4} "
                    + $"group={group.WgoGroup} name={group.Label} drop={group.Drop} "
                    + $"scenes={string.Join(",", group.Scenes.ToArray())}"
            );
        }
    }

    private static string SafeLabel(string id)
    {
        try
        {
            return LLBase.L(id);
        }
        catch (Exception)
        {
            return "?";
        }
    }
}
