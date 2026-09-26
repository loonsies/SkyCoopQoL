using System.Collections.Generic;
using SkyCoop;
using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private sealed class PlayerTarget
    {
        public GameObject Root = null!;
        public Comps.MultiplayerPlayer? Component;
        public readonly List<Renderer> Renderers = new();
        public string Name = "Player";
        public bool Debug;
        public float NextRendererScan;
        public Material? FillMaterial;
        public Material? EdgeMaterial;
        public readonly Dictionary<int, SkinState> Skins = new();
        public string TagText = "";
        public string? TagSourceName;
        public int TagDistance = -1;
        public int TagNameLimit;
        public bool TagShowDistance;
        public bool TagNewLine;
        public Vector2 TagSize;
        public Mesh? TagMesh;
        public Material? TagMaterial;
        public Material? TagBackgroundMaterial;
        public string? TagBitmapKey;
        public NametagBitmap? TagBitmap;
    }

    private sealed class SkinState
    {
        public SkinnedMeshRenderer Renderer = null!;
        public bool OriginalUpdateWhenOffscreen;
    }
}
