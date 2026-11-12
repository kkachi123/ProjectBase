namespace ProjectRE
{
    using System;
    using UniRx;
    using UnityEngine;

    /// <summary>지정된 몬스터의 사망 상태를 구독한다.</summary>
    public sealed class RoomMonsterObjective : RoomObjective
    {
        [SerializeField] private Health target;
        private IDisposable subscription;
        private bool started;
        private GameObject targetMarker;

        public void SetTarget(Health value) => target = value;

        public void CreateTargetMarker(ProjectRE.MapGeneration.MapBuildContext context)
        {
            targetMarker = context.CreateRoot("Objective Target");
            context.SetParent(targetMarker.transform, target.transform);
            context.RecordComponent(targetMarker.transform);
            targetMarker.transform.localPosition = new Vector3(0, 2, 0);
            var label = (TextMesh)context.AddComponent(targetMarker, typeof(TextMesh));
            context.RecordComponent(label);
            label.text = "!";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = .1f;
            label.color = Color.yellow;
            targetMarker.GetComponent<MeshRenderer>().sortingOrder = 100;
        }

        private void Start()
        {
            started = true;
            Bind();
        }

        private void OnEnable()
        {
            if (started) Bind();
        }

        private void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
        }

        private void Bind()
        {
            if (subscription != null || target == null || IsComplete) return;
            subscription = target.IsDead.Subscribe(dead =>
            {
                if (!dead) return;
                if (targetMarker != null) targetMarker.SetActive(false);
                Complete();
            });
        }
    }
}
