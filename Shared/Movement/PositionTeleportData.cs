using UnityEngine;

namespace AKeepersNeed2.Shared.Movement;

/// <summary>
/// Teleport target for <c>PlayerController.Teleport</c> that is just a position in the
/// player's current scene, so the game's same-scene path (optional fade, move, camera snap)
/// runs. It keeps the current lighting preset: the game applies the teleport's preset on
/// arrival, and the base default ("outdoor") would relight interiors.
/// </summary>
internal sealed class PositionTeleportData : TeleportDataBase
{
    private const string FallbackPreset = "outdoor";

    private readonly Vector3 _position;

    public PositionTeleportData(Vector3 position, bool fade = true)
        : base(CurrentPreset(), donNotFade: !fade)
    {
        _position = position;
    }

    public override string GetDestinationId()
    {
        return "akn_teleport";
    }

    public override GameSceneData GetDestinationSceneData()
    {
        return MainGame.WorldData.GetGameSceneDataById(MainGame.PlayerData.currentGameSceneId);
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
