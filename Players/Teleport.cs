using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private void TeleportTo(Vector3 targetPosition)
    {
        var manager = GameManager.GetPlayerManagerComponent();
        if (manager == null || targetPosition == Vector3.zero || !IsFinite(targetPosition)) return;
        Vector3 destination = targetPosition;
        if (!IsFinite(destination) || destination == Vector3.zero) return;
        manager.TeleportPlayer(destination, Quaternion.identity);
    }
}
