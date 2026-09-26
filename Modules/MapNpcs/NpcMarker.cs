using AKeepersNeed2.Shared.Map;
using LazyBearTechnology;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.MapNpcs;

/// <summary>One NPC's face on the map: hover shows the name, shift-click teleports.</summary>
internal sealed class NpcMarker :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IMapClickTarget
{
    public WgoData Wgo { get; private set; }

    public bool HandlesShiftClick => ModConfig.MapNpcsTeleportEnabled.Value;

    public string DisplayName { get; private set; }

    public RectTransform Rect { get; private set; }

    private NpcMarkerLayer _layer;

    public static NpcMarker Create(NpcMarkerLayer layer, WgoData wgo, float size, Material material)
    {
        var go = new GameObject($"Npc_{wgo.id}", typeof(RectTransform));
        go.transform.SetParent(layer.transform, false);

        var image = go.AddComponent<Image>();
        image.sprite = wgo.Definition.Portrait;
        image.preserveAspect = true;
        if (material != null)
        {
            image.material = material;
        }

        var marker = go.AddComponent<NpcMarker>();
        marker._layer = layer;
        marker.Wgo = wgo;
        marker.DisplayName = LLBase.L(wgo.Definition.id);
        marker.Rect = (RectTransform)go.transform;
        marker.Rect.anchorMin = marker.Rect.anchorMax = new Vector2(0.5f, 0.5f);
        marker.Rect.pivot = new Vector2(0.5f, 0.5f);
        marker.Rect.sizeDelta = new Vector2(size, size);
        return marker;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _layer.ShowLabel(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _layer.HideLabel(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }
        if (!ModConfig.MapNpcsTeleportEnabled.Value)
        {
            return;
        }
        if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
        {
            return;
        }
        MapTeleporter.TeleportNear(Wgo.Position);
    }
}
