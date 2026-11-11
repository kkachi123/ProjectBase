namespace ProjectRE
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>목표를 집계하고 지정된 진행 출구를 제어한다.</summary>
    public sealed class RoomObjectiveController : MonoBehaviour
    {
        [SerializeField] private RoomObjective[] objectives = Array.Empty<RoomObjective>();
        [SerializeField] private GameObject exitGate;

        public bool PlayerInside { get; private set; }
        public bool HasEntered { get; private set; }
        public int CompletedCount { get; private set; }
        public int TotalCount => objectives.Length;
        public IReadOnlyList<RoomObjective> Objectives => objectives;
        public bool IsComplete => CompletedCount == TotalCount;
        public event Action Changed;

        private void OnEnable()
        {
            foreach (var objective in objectives)
                if (objective != null) objective.Completed += OnObjectiveCompleted;
            Refresh();
        }

        private void OnDisable()
        {
            foreach (var objective in objectives)
                if (objective != null) objective.Completed -= OnObjectiveCompleted;
            PlayerInside = false;
        }

        public void SetPlayerInside(bool inside)
        {
            if (PlayerInside == inside) return;
            PlayerInside = inside;
            if (inside) HasEntered = true;
            Changed?.Invoke();
        }

        private void OnObjectiveCompleted(RoomObjective _) => Refresh();

        private void Refresh()
        {
            CompletedCount = 0;
            foreach (var objective in objectives)
                if (objective != null && objective.IsComplete) CompletedCount++;
            if (exitGate != null) exitGate.SetActive(!IsComplete);
            Changed?.Invoke();
        }
    }
}
