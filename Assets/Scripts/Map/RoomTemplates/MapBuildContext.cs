namespace ProjectRE.MapGeneration
{
    using System;
    using UnityEngine;

    /// <summary>런타임과 에디터가 프리팹 생성과 정리 방식을 제공한다.</summary>
    public sealed class MapBuildContext
    {
        public Func<string, GameObject> CreateRoot { get; set; } = name => new GameObject(name);
        public Func<GameObject, Transform, GameObject> InstantiatePrefab { get; set; } =
            (prefab, parent) => UnityEngine.Object.Instantiate(prefab, parent);
        public Func<GameObject, Type, Component> AddComponent { get; set; } =
            (target, type) => target.AddComponent(type);
        public Action<Component> RecordComponent { get; set; } = _ => { };
        public Action<GameObject> DestroyObject { get; set; } = DefaultDestroy;

        private static void DefaultDestroy(GameObject target)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
