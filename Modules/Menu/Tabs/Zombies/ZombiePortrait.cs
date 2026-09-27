using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.Menu.Tabs.Zombies;

/// <summary>
/// A zombie's face, drawn by a clone of the game's own <c>UIWorkerIcon</c> (the one in its zombie
/// window). That widget stacks static sprite layers recoloured by a LUT material; there's no single
/// sprite to copy, so each list row gets its own clone and redraws it with
/// <c>ShowWithoutTalent</c>. If the template can't be found the row simply has no portrait.
/// </summary>
internal sealed class ZombiePortrait
{
    private static readonly AccessTools.FieldRef<UIZombieWorkerWindow, UIWorkerIcon> TemplateField =
        AccessTools.FieldRefAccess<UIZombieWorkerWindow, UIWorkerIcon>("workerIcon");

    private static bool _templateMissingLogged;

    private readonly UIWorkerIcon _icon;

    public ZombiePortrait(RectTransform slot, float size)
    {
        UIWorkerIcon template = FindTemplate();
        if (template == null)
        {
            return;
        }
        GameObject clone = Object.Instantiate(template.gameObject, slot, false);
        clone.name = "Portrait";
        foreach (Graphic graphic in clone.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
        var rect = (RectTransform)clone.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        float native = Mathf.Max(rect.rect.width, rect.rect.height);
        if (native > 0f)
        {
            rect.localScale = Vector3.one * (size / native);
        }
        _icon = clone.GetComponent<UIWorkerIcon>();
    }

    public void Show(ZombieWgoData zombie)
    {
        if (_icon == null || zombie == null)
        {
            return;
        }
        SkinPresetGK2 preset = ZombieSkinHelper.GetPresetForWgoData(zombie, "zombie_worker");
        _icon.ShowWithoutTalent(zombie, preset);
        // GetPresetForWgoData creates a new ScriptableObject every call.
        if (preset != null)
        {
            Object.Destroy(preset);
        }
    }

    private static UIWorkerIcon FindTemplate()
    {
        UIWorkerIcon template = null;
        try
        {
            UIZombieWorkerWindow window = LazyUI.GetWindow<UIZombieWorkerWindow>();
            template = window != null
                ? TemplateField(window)
                : null;
        }
        catch (System.Exception e)
        {
            Plugin.Logger.LogWarning($"[Menu] zombie portrait template lookup failed: {e.Message}");
        }
        if (template == null && !_templateMissingLogged)
        {
            _templateMissingLogged = true;
            Plugin.Logger.LogWarning("[Menu] no zombie portrait template; the zombie list shows no faces.");
        }
        return template;
    }
}
