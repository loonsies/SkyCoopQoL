using System.Collections.Generic;
using SkyCoop;
using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private readonly List<PlayerTarget> players = new();

    private readonly Dictionary<int, PlayerTarget> cache = new();

    private readonly HashSet<int> liveIds = new();

    private readonly List<int> staleIds = new();

    private float nextRefresh;

    private void RefreshPlayers()
    {
        if (Time.unscaledTime < this.nextRefresh) return;
        this.nextRefresh = Time.unscaledTime + 0.5f;
        this.players.Clear();
        this.liveIds.Clear();
        int count = Mathf.Min(MyMod.players.Count, MyMod.playersData.Count);
        for (int slot = 0; slot < count; slot++)
        {
            GameObject root = MyMod.players[slot];
            var data = MyMod.playersData[slot];
            if (slot == API.m_MyClientID || root == null || !root.activeInHierarchy || data == null ||
                data.m_IsLoading || data.m_Levelid != MyMod.levelid || data.m_LevelGuid != MyMod.level_guid)
                continue;
            int id = root.GetInstanceID();
            if (!this.cache.TryGetValue(id, out PlayerTarget? target))
            {
                target = new PlayerTarget { Root = root, Component = root.GetComponent<Comps.MultiplayerPlayer>() };
                this.cache.Add(id, target);
            }
            target.Name = string.IsNullOrEmpty(data.m_Name) ? "Player " + slot : data.m_Name;
            this.players.Add(target);
            this.liveIds.Add(id);
        }
        if (this.Config.DebugMode && this.debugPlayer != null)
        {
            int id = this.debugPlayer.GetInstanceID();
            if (!this.cache.TryGetValue(id, out PlayerTarget? target))
            {
                target = new PlayerTarget { Root = this.debugPlayer, Name = "[DEBUG] Ghost test player", Debug = true };
                this.cache.Add(id, target);
            }
            this.players.Add(target);
            this.liveIds.Add(id);
        }
        this.staleIds.Clear();
        foreach (int id in this.cache.Keys)
            if (!this.liveIds.Contains(id)) this.staleIds.Add(id);
        foreach (int id in this.staleIds)
        {
            this.ReleaseTarget(this.cache[id]);
            this.cache.Remove(id);
        }
    }

    private bool IsLive(PlayerTarget target)
    {
        if (target.Root == null || !target.Root.activeInHierarchy) return false;
        if (target.Debug) return this.Config.DebugMode && target.Root == this.debugPlayer;
        if (target.Component == null) return false;
        int slot = target.Component.m_ID;
        if (slot < 0 || slot >= MyMod.playersData.Count || slot >= MyMod.players.Count || slot == API.m_MyClientID)
            return false;
        var data = MyMod.playersData[slot];
        return MyMod.players[slot] == target.Root && data != null && !data.m_IsLoading &&
            data.m_Levelid == MyMod.levelid && data.m_LevelGuid == MyMod.level_guid;
    }

    private void ScanRenderers(PlayerTarget target)
    {
        target.Renderers.Clear();
        var component = target.Component;
        GameObject? root = target.Debug ? target.Root : component?.m_Player;
        if (root == null) return;
        // SkyCoop sets these fields on its first Update.
        GameObject? body = component?.body;
        GameObject? clothing = component?.clothing;
        if (body == null && root.transform.childCount > 1) body = root.transform.GetChild(1).gameObject;
        if (clothing == null && root.transform.childCount > 0) clothing = root.transform.GetChild(0).gameObject;
        AddBranchRenderers(target, body);
        AddBranchRenderers(target, clothing);
    }

    private static void AddBranchRenderers(PlayerTarget target, GameObject? branch)
    {
        if (branch == null) return;
        // Concrete proxy types preserve access to skinned mesh properties.
        foreach (SkinnedMeshRenderer renderer in branch.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (renderer != null) target.Renderers.Add(renderer);
        foreach (MeshRenderer renderer in branch.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer != null) target.Renderers.Add(renderer);
    }

    private bool TryGetBounds(PlayerTarget target, out Bounds bounds)
    {
        bounds = default;
        if (!this.IsLive(target)) return false;
        if (Time.unscaledTime >= target.NextRendererScan)
        {
            target.NextRendererScan = Time.unscaledTime + 1f;
            this.ScanRenderers(target);
        }
        bool found = false;
        foreach (Renderer renderer in target.Renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            Bounds current = renderer.bounds;
            if (!IsFinite(current.center) || !IsFinite(current.size) || current.size.sqrMagnitude < 0.0001f) continue;
            if (!found) { bounds = current; found = true; }
            else bounds.Encapsulate(current);
        }
        return found;
    }

    private void ReleaseTarget(PlayerTarget target)
    {
        this.ReleaseWorldTag(target);
        RestoreSkins(target);
        if (target.FillMaterial != null) UnityEngine.Object.Destroy(target.FillMaterial);
        if (target.EdgeMaterial != null) UnityEngine.Object.Destroy(target.EdgeMaterial);
        target.FillMaterial = null;
        target.EdgeMaterial = null;
    }

    private void ClearPlayers()
    {
        if (this.modelPass != null) this.modelPass.Clear();
        if (this.tagPass != null) this.tagPass.Clear();
        foreach (PlayerTarget target in this.cache.Values) this.ReleaseTarget(target);
        this.players.Clear(); this.cache.Clear(); this.liveIds.Clear(); this.staleIds.Clear(); this.nextRefresh = 0f;
    }
}
