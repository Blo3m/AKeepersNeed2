using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace AKeepersNeed2.Modules.Weather;

/// <summary>
/// Reads the weather state machine. <c>WeatherSystem</c> keeps a private NodeCanvas
/// <c>FSMOwner</c>; each state is an <c>FSMWeatherState</c> named after its weather component.
/// States without a component ("NULL Weather") are routing nodes that jump straight to a random
/// state, so they are left out, as are states with no exits (story weathers). NodeCanvas.dll isn't
/// referenced, so everything past <c>WeatherSystem</c> goes through reflection. The graph comes
/// from game assets and doesn't change during a session, so the names are cached once found.
/// </summary>
internal static class WeatherStates
{
    private static readonly FieldInfo OwnerField = AccessTools.Field(typeof(WeatherSystem), "fsmOwner");
    private static readonly Type StateType = AccessTools.TypeByName("FSMWeatherState");
    private static readonly List<string> Empty = new List<string>();

    private static List<string> _names;

    public static bool HasGame => MainGame.Instance?.GameSave?.weatherData != null && MainGame.PlayerData != null;

    public static WeatherData Data => MainGame.Instance.GameSave.weatherData;

    /// <summary>The selectable weather names, in graph order; empty until a game is loaded.</summary>
    public static IList<string> Names()
    {
        if (_names != null)
        {
            return _names;
        }
        if (!HasGame)
        {
            return Empty;
        }
        var names = new List<string>();
        foreach (object node in Nodes())
        {
            if (StateType == null || !StateType.IsInstanceOfType(node))
            {
                continue;
            }
            Traverse state = Traverse.Create(node);
            var weather = state.Field("weather").GetValue() as UnityEngine.Object;
            if (weather != null && HasExit(state) && !names.Contains(weather.name))
            {
                names.Add(weather.name);
            }
        }
        if (names.Count == 0)
        {
            return Empty;
        }
        _names = names;
        return _names;
    }

    // Story weathers (EventMistZombie, …) have no exits: a roll re-enters them forever, so a Set
    // would stick in the save even with the lock off.
    private static bool HasExit(Traverse state)
    {
        return state.Field("exits").GetValue() is ICollection exits && exits.Count > 0;
    }

    /// <summary>
    /// Every node of the state machine the current weather state sits in. That's the one
    /// <c>WeatherSystem.SetWeatherState</c> looks names up in (it descends into nested machines).
    /// </summary>
    public static IEnumerable<object> Nodes()
    {
        WeatherSystem system = WeatherSystem.Instance;
        object owner = system != null && OwnerField != null
            ? OwnerField.GetValue(system)
            : null;
        if (owner == null)
        {
            yield break;
        }
        object current = Traverse.Create(owner).Method("GetCurrentState", true).GetValue();
        object fsm = current != null
            ? Traverse.Create(current).Property("FSM").GetValue()
            : null;
        if (fsm == null)
        {
            yield break;
        }
        Traverse fsmTraverse = Traverse.Create(fsm);
        object nodes = fsmTraverse.Property("allNodes").GetValue() ?? fsmTraverse.Field("_nodes").GetValue();
        if (!(nodes is IEnumerable list))
        {
            yield break;
        }
        foreach (object node in list)
        {
            yield return node;
        }
    }
}
