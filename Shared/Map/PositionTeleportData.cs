using UnityEngine;

namespace AKeepersNeed2.Shared.Map;

/// <summary>
/// Teleport target for <c>PlayerController.Teleport</c> that is just a position in the
/// player's current scene, so the game's same-scene path (fade, move, camera snap) runs.
/// </summary>
internal sealed class PositionTeleportData : TeleportDataBase
{
    private readonly Vector3 _position;

    public PositionTeleportData(Vector3 position)
    {
        _position = position;
    }

    public override string GetDestinationId()
    {
        return "akn_map_teleport";
    }

    public override GameSceneData GetDestinationSceneData()
    {
        return MainGame.WorldData.GetGameSceneDataById(MainGame.PlayerData.currentGameSceneId);
    }

    public override Vector3 GetPosition()
    {
        return _position;
    }
}
