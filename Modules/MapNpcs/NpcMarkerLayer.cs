using System;
using System.Collections.Generic;
using AKeepersNeed2.Shared.Map;
using AKeepersNeed2.Shared.Ui;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.MapNpcs;

/// <summary>
/// Container on the map's <c>mapRect</c> holding the NPC markers. Its <c>Update</c> only runs
/// while the map window is open (the map is inactive otherwise): markers follow their NPC every
/// frame, and the NPC list is rescanned periodically so NPCs that appear or hide are picked up.
/// Removes itself once the feature is switched off.
/// </summary>
internal sealed class NpcMarkerLayer : MonoBehaviour
{
    private const string ObjectName = "AKN_NpcMarkers";
    private const float MarkerSize = 32f;
    private const float RescanSeconds = 1f;
    private const float LabelGap = 4f;

    // Treated as generic by request, even if they turn out to have a reputation stat.
    private static readonly string[] ExcludedIdPrefixes = { "npc_old_bandit" };

    private static readonly AccessTools.FieldRef<UINpcWidget, Image> NpcPortraitRef =
        AccessTools.FieldRefAccess<UINpcWidget, Image>("portrait");

    private readonly Dictionary<WgoData, NpcMarker> _markers = new Dictionary<WgoData, NpcMarker>();
    private readonly HashSet<WgoData> _seen = new HashSet<WgoData>();
    private readonly List<WgoData> _stale = new List<WgoData>();

    private RectTransform _mapRect;
    private Material _portraitMaterial;
    private RectTransform _label;
    private TextMeshProUGUI _labelText;
    private NpcMarker _hovered;
    private float _nextRescan;

    public static NpcMarkerLayer Ensure(RectTransform mapRect)
    {
        Transform existing = mapRect.Find(ObjectName);
        NpcMarkerLayer layer = existing != null ? existing.GetComponent<NpcMarkerLayer>() : null;
        if (layer == null)
        {
            var go = new GameObject(ObjectName, typeof(RectTransform));
            go.transform.SetParent(mapRect, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            layer = go.AddComponent<NpcMarkerLayer>();
            layer._mapRect = mapRect;
            layer._portraitMaterial = CreatePortraitMaterial();
            layer.BuildLabel();
        }
        layer.transform.SetAsLastSibling();
        return layer;
    }

    public void Rescan()
    {
        _nextRescan = Time.unscaledTime + RescanSeconds;
        Vector2 mapSize = _mapRect.sizeDelta;

        _seen.Clear();
        foreach (GameSceneData scene in MainGame.WorldData.gameSceneDataList)
        {
            foreach (WgoData wgo in scene.wgoDataList)
            {
                if (!IsMappedNpc(wgo, mapSize))
                {
                    continue;
                }
                _seen.Add(wgo);
                if (!_markers.ContainsKey(wgo))
                {
                    _markers.Add(wgo, NpcMarker.Create(this, wgo, MarkerSize, _portraitMaterial));
                }
            }
        }

        _stale.Clear();
        foreach (WgoData wgo in _markers.Keys)
        {
            if (!_seen.Contains(wgo))
            {
                _stale.Add(wgo);
            }
        }
        foreach (WgoData wgo in _stale)
        {
            if (_hovered == _markers[wgo])
            {
                HideLabel(_hovered);
            }
            Destroy(_markers[wgo].gameObject);
            _markers.Remove(wgo);
        }

        Reposition();
        _label.SetAsLastSibling();
    }

    public void ShowLabel(NpcMarker marker)
    {
        _hovered = marker;
        _labelText.text = marker.DisplayName;
        Vector2 textSize = _labelText.GetPreferredValues(marker.DisplayName);
        _label.sizeDelta = new Vector2(textSize.x + 16f, textSize.y + 6f);
        _label.gameObject.SetActive(true);
        _label.SetAsLastSibling();
        PlaceLabel();
    }

    public void HideLabel(NpcMarker marker)
    {
        if (_hovered != marker)
        {
            return;
        }
        _hovered = null;
        _label.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (!ModConfig.MapNpcsEnabled.Value)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (_hovered != null)
        {
            HideLabel(_hovered);
        }
    }

    private void OnDestroy()
    {
        if (_portraitMaterial != null)
        {
            Destroy(_portraitMaterial);
        }
    }

    private void Update()
    {
        if (!ModConfig.MapNpcsEnabled.Value)
        {
            Destroy(gameObject);
            return;
        }
        if (Time.unscaledTime >= _nextRescan)
        {
            Rescan();
            return;
        }
        Reposition();
    }

    private void Reposition()
    {
        Vector2 mapSize = _mapRect.sizeDelta;
        foreach (NpcMarker marker in _markers.Values)
        {
            bool onMap = MapProjection.TryWorldToMap(marker.Wgo.Position, mapSize, out Vector2 point);
            if (onMap)
            {
                marker.Rect.anchoredPosition = point;
            }
            if (marker.gameObject.activeSelf != onMap)
            {
                marker.gameObject.SetActive(onMap);
            }
        }
        if (_hovered != null)
        {
            PlaceLabel();
        }
    }

    private void PlaceLabel()
    {
        Vector2 offset = new Vector2(0f, MarkerSize * 0.5f + LabelGap);
        _label.anchoredPosition = _hovered.Rect.anchoredPosition + offset;
    }

    private static bool IsMappedNpc(WgoData wgo, Vector2 mapSize)
    {
        if (wgo == null || wgo.IsHidden)
        {
            return false;
        }
        if (!IsNamedNpc(wgo.Definition))
        {
            return false;
        }
        return MapProjection.TryWorldToMap(wgo.Position, mapSize, out _);
    }

    // Generic NPCs (guards, bandits) have portraits too; only the named cast carries a
    // reputation stat, the same one the game's NPC relationship widget shows.
    private static bool IsNamedNpc(WGODef def)
    {
        if (def == null || string.IsNullOrEmpty(def.portrait))
        {
            return false;
        }
        if (string.IsNullOrEmpty(def.repResName))
        {
            return false;
        }
        foreach (string prefix in ExcludedIdPrefixes)
        {
            if (def.id.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    // The game's portraits use a shader that replaces their blue key colour with _Color
    // (UINpcWidget sets it transparent via ImageExtensions.BlueColorReplace). Copy that
    // material from the NPC widget; without one the key colour stays visible.
    private static Material CreatePortraitMaterial()
    {
        foreach (UINpcWidget widget in Resources.FindObjectsOfTypeAll<UINpcWidget>())
        {
            Image portrait = NpcPortraitRef(widget);
            if (portrait == null || portrait.material == null)
            {
                continue;
            }
            var material = new Material(portrait.material);
            material.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
            return material;
        }
        Plugin.Logger.LogInfo("[MapNpcs] no NPC portrait material found; using the default.");
        return null;
    }

    private void BuildLabel()
    {
        Image panel = MenuUi.CreateImage(
            "Label",
            transform,
            NativeUiSkin.IsReady ? Color.white : new Color(0.12f, 0.07f, 0.055f, 0.95f)
        );
        panel.raycastTarget = false;
        if (NativeUiSkin.IsReady && NativeUiSkin.HeaderSprite != null)
        {
            panel.sprite = NativeUiSkin.HeaderSprite;
            panel.type = Image.Type.Sliced;
        }
        _label = panel.rectTransform;
        _label.anchorMin = _label.anchorMax = new Vector2(0.5f, 0.5f);
        _label.pivot = new Vector2(0.5f, 0f);

        _labelText = MenuUi.CreateText("Text", _label, 13f, TextAlignmentOptions.Center, Color.white);
        MenuUi.ApplyHeaderText(_labelText);
        MenuUi.Stretch(_labelText.rectTransform);
        _labelText.textWrappingMode = TextWrappingModes.NoWrap;

        _label.gameObject.SetActive(false);
    }
}
