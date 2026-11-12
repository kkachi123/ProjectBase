namespace ProjectRE
{
    using System;
    using System.Collections.Generic;
    using ProjectRE.MapGeneration;
    using UnityEngine;

    /// <summary>생성된 콘텐츠와 목표 후보를 해당 방의 진행 조건에 연결한다.</summary>
    public sealed class RoomObjectiveSetup : MonoBehaviour
    {
        [Serializable]
        private sealed class TriggerCandidate
        {
            public string id;
            public RoomTriggerObjective objective;
            [Range(0, 100)] public int chance = 50;
        }

        [Serializable]
        private sealed class ExitGate
        {
            public RoomOpening direction;
            public GameObject gate;
        }

        [SerializeField] private RoomObjectiveController controller;
        [SerializeField] private List<TriggerCandidate> triggers = new();
        [SerializeField] private List<int> monsterSlots = new();
        [SerializeField] private List<ExitGate> gates = new();

        public void Configure(BuiltRoom room, int seed, MapBuildContext context)
        {
            if (room == null || context == null || controller == null || !controller.transform.IsChildOf(transform))
                throw new InvalidOperationException("방 목표 설정 또는 관리자가 올바르지 않습니다.");
            if (room.Template.gameObject.activeInHierarchy)
                throw new InvalidOperationException("비활성 맵에서 방 목표를 구성해야 합니다.");

            var directions = new HashSet<RoomOpening>();
            GameObject selectedGate = null;
            foreach (var entry in gates)
            {
                if (entry == null || entry.gate == null || !entry.gate.transform.IsChildOf(transform) ||
                    (entry.direction != RoomOpening.North && entry.direction != RoomOpening.East &&
                     entry.direction != RoomOpening.South && entry.direction != RoomOpening.West) ||
                    !directions.Add(entry.direction))
                    throw new InvalidOperationException("방 출구 잠금 설정이 올바르지 않습니다.");
                var gateCollider = entry.gate.GetComponent<Collider2D>();
                if (gateCollider == null || !gateCollider.enabled || gateCollider.isTrigger)
                    throw new InvalidOperationException("출구 잠금에 활성 충돌체가 필요합니다.");
                context.SetActive(entry.gate, false);
                if (entry.direction == room.Plan.Exit) selectedGate = entry.gate;
            }

            bool mainRoom = !room.Plan.IsOptional && room.Plan.Entry != RoomOpening.None && room.Plan.Exit != RoomOpening.None;
            var selected = new List<RoomObjective>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var triggerObjects = new HashSet<RoomTriggerObjective>();
            foreach (var candidate in triggers)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.id) || !ids.Add(candidate.id) ||
                    candidate.objective == null || !triggerObjects.Add(candidate.objective) || !candidate.objective.enabled ||
                    candidate.objective.gameObject == gameObject || !candidate.objective.transform.IsChildOf(transform) ||
                    candidate.chance < 0 || candidate.chance > 100)
                    throw new InvalidOperationException("방 트리거 목표 후보가 올바르지 않습니다.");
                var triggerCollider = candidate.objective.GetComponent<Collider2D>();
                if (triggerCollider == null || !triggerCollider.enabled || !triggerCollider.isTrigger)
                    throw new InvalidOperationException("목표 후보에 활성 Trigger Collider가 필요합니다.");
                bool enabled = mainRoom && Select(seed, room.Plan.Cell, room.Template.TemplateId, candidate.id, candidate.chance);
                context.SetActive(candidate.objective.gameObject, enabled);
                if (enabled) selected.Add(candidate.objective);
            }

            var slots = new HashSet<int>();
            foreach (int index in monsterSlots)
            {
                if (index < 0 || index >= room.Template.Content.ContentSlots.Count || !slots.Add(index))
                    throw new InvalidOperationException("몬스터 목표 슬롯이 올바르지 않습니다.");
                RoomContentSlot slot = null;
                foreach (var candidate in room.Template.Content.ContentSettings)
                    if (candidate.slotIndex == index) { slot = candidate; break; }
                if (slot == null) throw new InvalidOperationException("몬스터 목표 슬롯에 콘텐츠 설정이 없습니다.");
                if (!mainRoom || !room.Content.TryGetValue(index, out GameObject content)) continue;
                ContentSelection choice = null;
                foreach (var candidate in room.Plan.Content)
                    if (candidate.SlotIndex == index) { choice = candidate; break; }
                RoomContentOption option = null;
                foreach (var candidate in slot.options)
                    if (candidate.id == choice?.OptionId) { option = candidate; break; }
                if (option == null) throw new InvalidOperationException("생성된 콘텐츠의 선택 정보를 찾을 수 없습니다.");
                if (option.kind != ContentKind.Monster && option.kind != ContentKind.EliteMonster) continue;
                var health = content.GetComponent<Health>();
                if (health == null || !health.enabled) throw new InvalidOperationException("목표 몬스터에 활성 Health가 없습니다.");
                var objective = (RoomMonsterObjective)context.AddComponent(controller.gameObject, typeof(RoomMonsterObjective));
                if (objective == null) throw new InvalidOperationException("몬스터 목표를 생성하지 못했습니다.");
                context.RecordComponent(objective);
                objective.SetDescription("몬스터 처치");
                objective.SetTarget(health);
                selected.Add(objective);
            }

            if (mainRoom && selectedGate == null) throw new InvalidOperationException("진행 방향의 잠금 출구가 없습니다.");
            context.RecordComponent(controller);
            controller.Configure(selected, mainRoom ? selectedGate : null);
            if (!mainRoom) context.SetActive(gameObject, false);
        }

        private static bool Select(int seed, Vector2Int cell, string templateId, string id, int chance)
        {
            uint hash = 2166136261u;
            void Add(uint value) => hash = unchecked((hash ^ value) * 16777619u);
            Add(unchecked((uint)seed)); Add(unchecked((uint)cell.x)); Add(unchecked((uint)cell.y));
            foreach (char c in templateId) Add(c);
            Add(0x4F424A45u);
            foreach (char c in id) Add(c);
            hash = unchecked(hash * 1664525u + 1013904223u);
            return (int)(((ulong)hash * 100u) >> 32) < chance;
        }
    }
}
