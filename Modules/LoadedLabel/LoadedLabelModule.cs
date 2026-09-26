using AKeepersNeed2.Core;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AKeepersNeed2.Modules.LoadedLabel;

/// <summary>
/// Shows "A Keepers Need 2 loaded (vX.Y.Z)" under the game's version line on the main menu, so
/// it's obvious the mod is installed. The line is a clone of <c>UIMainMenuInfoPanel.versionLabel</c>
/// (same font/style/alignment) placed right below it, and follows the panel's menu-only labels
/// (developer/publisher), so it's hidden when the panel shows just the version in-game.
/// </summary>
internal sealed class LoadedLabelModule : IModule
{
    private const string LabelName = "AKN_LoadedLabel";

    private static readonly AccessTools.FieldRef<UIMainMenuInfoPanel, TextMeshProUGUI> VersionLabelRef =
        AccessTools.FieldRefAccess<UIMainMenuInfoPanel, TextMeshProUGUI>("versionLabel");

    private static readonly AccessTools.FieldRef<UIMainMenuInfoPanel, TextStyle> RightSideStyleRef =
        AccessTools.FieldRefAccess<UIMainMenuInfoPanel, TextStyle>("rightSideStyle");

    private static TextMeshProUGUI _label;
    private static RectTransform _panel;
    private static Vector2 _panelBase;
    private static float _lineHeight;
    private Harmony _harmony;

    public string Name => "LoadedLabel";

    public int Order => 0;

    public void Enable()
    {
        _harmony = new Harmony($"{MyPluginInfo.PLUGIN_GUID}.{Name}");
        _harmony.Patch(
            AccessTools.Method(typeof(UIMainMenuInfoPanel), "Draw"),
            postfix: new HarmonyMethod(typeof(LoadedLabelModule), nameof(AfterDraw))
        );
        _harmony.Patch(
            AccessTools.Method(typeof(UIMainMenuInfoPanel), "SetMenuOnlyLabelsVisible"),
            postfix: new HarmonyMethod(typeof(LoadedLabelModule), nameof(AfterSetMenuOnlyLabelsVisible))
        );
    }

    public void Disable()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
        if (_label != null)
        {
            Object.Destroy(_label.gameObject);
            _label = null;
        }
        if (_panel != null)
        {
            _panel.anchoredPosition = _panelBase;
        }
        _panel = null;
    }

    private static void AfterDraw(UIMainMenuInfoPanel __instance)
    {
        TextMeshProUGUI version = VersionLabelRef(__instance);
        if (version == null)
        {
            return;
        }
        if (_label == null)
        {
            _label = CreateLabel(version);
            _panel = (RectTransform)__instance.transform;
            _panelBase = _panel.anchoredPosition;
            _lineHeight = ((RectTransform)version.transform).rect.height;
            ShiftPanel(_label.gameObject.activeSelf);
        }

        string versionText = $"(v{MyPluginInfo.PLUGIN_VERSION})";
        TextStyle style = RightSideStyleRef(__instance);
        if (style != null)
        {
            versionText = style.ApplyStyleToString(versionText, staticFont: true);
        }
        _label.text = $"{MyPluginInfo.PLUGIN_NAME} loaded {versionText}";
    }

    private static void AfterSetMenuOnlyLabelsVisible(bool isVisible)
    {
        if (_label != null)
        {
            _label.gameObject.SetActive(isVisible);
            ShiftPanel(isVisible);
        }
    }

    /// <summary>
    /// The version line already sits at the bottom edge of the screen, so the line added below
    /// it would be off-screen. While it's shown, the whole panel is raised by one line instead.
    /// </summary>
    private static void ShiftPanel(bool raised)
    {
        if (_panel == null)
        {
            return;
        }
        _panel.anchoredPosition = raised
            ? _panelBase + new Vector2(0f, _lineHeight)
            : _panelBase;
    }

    private static TextMeshProUGUI CreateLabel(TextMeshProUGUI version)
    {
        GameObject go = Object.Instantiate(version.gameObject, version.transform.parent, false);
        go.name = LabelName;
        // Drop anything beyond the text itself (e.g. localizers) that could overwrite our text.
        foreach (Component component in go.GetComponents<Component>())
        {
            if (component is Transform
                || component is CanvasRenderer
                || component is TextMeshProUGUI
                || component is LayoutElement)
            {
                continue;
            }
            Object.Destroy(component);
        }
        go.transform.SetSiblingIndex(version.transform.GetSiblingIndex() + 1);

        // In a layout group the sibling order already stacks it below; otherwise place it by hand.
        if (go.transform.parent.GetComponent<LayoutGroup>() == null)
        {
            var rect = (RectTransform)go.transform;
            var versionRect = (RectTransform)version.transform;
            rect.anchoredPosition = versionRect.anchoredPosition - new Vector2(0f, versionRect.rect.height);
        }
        return go.GetComponent<TextMeshProUGUI>();
    }
}
