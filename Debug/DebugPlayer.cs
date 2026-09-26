using MelonLoader;
using SkyCoop;
using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private GameObject? debugPlayer;

    private bool debugMotion;

    private Vector3 debugOrigin;

    private Vector3 debugRight;

    private float debugStartTime;

    // Spawn the raw prefab without registering a network player.
    public void SpawnDebugPlayer()
    {
        if (!this.Config.DebugMode || !this.InGame) return;
        Transform local = GameManager.GetPlayerTransform();
        Camera camera = GameManager.GetMainCamera();
        if (local == null || camera == null)
        {
            MelonLogger.Warning("Load into a game before spawning the test player.");
            return;
        }
        if (MyMod.LoadedBundle == null)
        {
            MelonLogger.Warning("SkyCoop is still loading the test model. Try again in a moment.");
            return;
        }
        GameObject prefab = MyMod.LoadedBundle.LoadAsset<GameObject>("multiplayerPlayer");
        if (prefab == null)
        {
            MelonLogger.Warning("The SkyCoop test player prefab is missing.");
            return;
        }
        this.RemoveDebugPlayer();
        Vector3 forward = camera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = local.forward;
        forward.Normalize();
        this.debugPlayer = UnityEngine.Object.Instantiate<GameObject>(prefab);
        this.debugPlayer.name = "SkyCoopQoL_DebugGhost";
        this.debugPlayer.SetActive(false);
        this.debugOrigin = local.position + forward * this.Config.DebugDistance;
        this.debugPlayer.transform.position = this.debugOrigin;
        this.debugPlayer.transform.rotation = Quaternion.LookRotation(-forward);
        foreach (Collider collider in this.debugPlayer.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (MonoBehaviour behaviour in this.debugPlayer.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
        Transform root = this.debugPlayer.transform;
        if (root.childCount > 2) root.GetChild(2).gameObject.SetActive(false);
        if (root.childCount > 4) root.GetChild(4).gameObject.SetActive(false);
        if (root.childCount > 1 && root.GetChild(1).childCount > 1)
        {
            Transform heads = root.GetChild(1).GetChild(1);
            for (int i = 0; i < heads.childCount; i++) heads.GetChild(i).gameObject.SetActive(i == 0);
        }
        this.debugPlayer.SetActive(true);
        this.debugRight = new Vector3(forward.z, 0f, -forward.x);
        this.debugMotion = false;
        this.nextRefresh = 0f;
        MelonLogger.Msg("Test player spawned at " + FormatVector(this.debugOrigin) + ".");
    }

    private void RemoveDebugPlayer()
    {
        if (this.debugPlayer != null) UnityEngine.Object.Destroy(this.debugPlayer);
        this.debugPlayer = null;
        this.debugMotion = false;
        this.nextRefresh = 0f;
    }

    private void UpdateDebugPlayer()
    {
        if (!this.Config.DebugMode)
        {
            if (this.debugPlayer != null) this.RemoveDebugPlayer();
            return;
        }
        if (Input.GetKeyDown(this.Config.ToggleDebug))
        {
            if (this.debugPlayer == null) this.SpawnDebugPlayer();
            else this.RemoveDebugPlayer();
            this.nextRefresh = 0f;
        }
        if (Input.GetKeyDown(this.Config.ToggleDebugMotion) && this.debugPlayer != null)
        {
            this.debugMotion = !this.debugMotion;
            this.debugOrigin = this.debugPlayer.transform.position;
            this.debugStartTime = Time.time;
        }
        if (this.debugPlayer != null && this.debugMotion)
            this.debugPlayer.transform.position = this.debugOrigin + this.debugRight * (Mathf.Sin((Time.time - this.debugStartTime) * 0.8f) * 3f);
    }
}
