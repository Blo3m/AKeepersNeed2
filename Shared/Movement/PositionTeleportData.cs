using UnityEngine;

namespace AKeepersNeed2.Shared.Movement;

/// <summary>
/// Teleport target for <c>PlayerController.Teleport</c> that is just a position, in the player's
/// current scene or another one. The same scene takes the game's same-scene path (optional fade,
/// move, camera snap); another scene makes the game unload this one, show its loading screen and
/// load that one. The game applies the teleport's lighting preset on arrival, and the base default
/// ("outdoor") would relight interiors, so it defaults to the current preset.
/// </summary>
internal sealed class PositionTeleportData : TeleportDataBase
{
    private const string FallbackPreset = "outdoor";

    private readonly Vector3 _position;
    private readonly string _sceneId;

    /// <param name="sceneId">Destination scene; null for the current one.</param>
    /// <param name="preset">Lighting preset to arrive with; null or empty keeps the current one.</param>
    public PositionTeleportData(Vector3 position, bool fade = true, string sceneId = null, string preset = null)
        : base(string.IsNullOrEmpty(preset) ? CurrentPreset() : preset, donNotFade: !fade)
    {
        _position = position;
        _sceneId = sceneId;
    }

    public override string GetDestinationId()
    {
        return "akn_teleport";
    }

    public override GameSceneData GetDestinationSceneData()
    {
        return MainGame.WorldData.GetGameSceneDataById(_sceneId ?? MainGame.PlayerData.currentGameSceneId);
    }

    public override Vector3 GetPosition()
    {
        return _position;
    }

    // The loaded TimeOfDayPresets asset is named after its preset; re-applying the same name is
    // a no-op in EnvironmentEngine.
    private static string CurrentPreset()
    {
        EnvironmentEngine engine = EnvironmentEngine.Instance;
        return engine != null && engine.TimesOfDay != null
            ? engine.TimesOfDay.name
            : FallbackPreset;
    }
}
