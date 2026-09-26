using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
        return path;
    }

    private static string FormatVector(Vector3 value) => $"({value.x:F2}, {value.y:F2}, {value.z:F2})";

    private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
