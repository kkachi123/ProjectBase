using System;
using UnityEngine;

public class GridChunkGenerationContext
{
    public Func<GameObject, Transform, GameObject> InstantiatePrefab { get; set; } = DefaultInstantiatePrefab;
    public Action<GameObject> DestroyObject { get; set; } = DefaultDestroyObject;

    public GameObject Instantiate(GameObject prefab, Transform parent)
    {
        if (prefab == null) return null;
        return InstantiatePrefab != null ? InstantiatePrefab(prefab, parent) : DefaultInstantiatePrefab(prefab, parent);
    }

    public void Destroy(GameObject target)
    {
        if (target == null) return;

        if (DestroyObject != null)
        {
            DestroyObject(target);
            return;
        }

        DefaultDestroyObject(target);
    }

    private static GameObject DefaultInstantiatePrefab(GameObject prefab, Transform parent)
    {
        return UnityEngine.Object.Instantiate(prefab, parent);
    }

    private static void DefaultDestroyObject(GameObject target)
    {
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }
}
