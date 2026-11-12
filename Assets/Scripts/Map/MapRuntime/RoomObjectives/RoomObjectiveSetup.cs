namespace ProjectRE
{
    using System;
    using System.Collections.Generic;
    using ProjectRE.MapGeneration;
    using UnityEngine;

    /// <summary>실제로 생성된 후보에 적용할 규칙 하나를 결정한다.</summary>
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
        [SerializeField, Range(0, 100)] private int conditionChance = 50;
        [SerializeField] private List<RoomObjectiveRule> rules = new();
        [SerializeField] private List<TriggerCandidate> triggers = new();
        // 자동 문을 도입하기 전까지 기존 출구를 사용한다.
        [SerializeField] private List<ExitGate> gates = new();

        public void Configure(BuiltRoom room, int seed, MapBuildContext context)
        {
            if (room == null || context == null || controller == null || !controller.transform.IsChildOf(transform))
                throw new InvalidOperationException("방 목표 설정 또는 관리자가 올바르지 않습니다.");
            if (room.Template.gameObject.activeInHierarchy)
                throw new InvalidOperationException("비활성 맵에서 방 목표를 구성해야 합니다.");

            bool mainRoom = !room.Plan.IsOptional && room.Plan.Entry != RoomOpening.None && room.Plan.Exit != RoomOpening.None;
            var directions = new HashSet<RoomOpening>();
            GameObject selectedGate = null;
            foreach (var entry in gates)
            {
                if (entry == null || entry.gate == null || !entry.gate.transform.IsChildOf(transform) ||
                    (entry.direction != RoomOpening.North && entry.direction != RoomOpening.East &&
                     entry.direction != RoomOpening.South && entry.direction != RoomOpening.West) ||
                    !directions.Add(entry.direction))
                    throw new InvalidOperationException("방 출구 잠금 설정이 올바르지 않습니다.");
                var collider = entry.gate.GetComponent<Collider2D>();
                if (collider == null || !collider.enabled || collider.isTrigger)
                    throw new InvalidOperationException("출구 잠금에 활성 충돌체가 필요합니다.");
                context.SetActive(entry.gate, false);
                if (entry.direction == room.Plan.Exit) selectedGate = entry.gate;
            }

            var availableTriggers = new List<RoomTriggerObjective>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var triggerObjects = new HashSet<RoomTriggerObjective>();
            foreach (var candidate in triggers)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.id) || !ids.Add(candidate.id) ||
                    candidate.objective == null || !triggerObjects.Add(candidate.objective) || !candidate.objective.enabled ||
                    candidate.objective.gameObject == gameObject || !candidate.objective.transform.IsChildOf(transform) ||
                    candidate.chance < 0 || candidate.chance > 100)
                    throw new InvalidOperationException("방 트리거 목표 후보가 올바르지 않습니다.");
                var collider = candidate.objective.GetComponent<Collider2D>();
                if (collider == null || !collider.enabled || !collider.isTrigger)
                    throw new InvalidOperationException("목표 후보에 활성 Trigger Collider가 필요합니다.");
                context.SetActive(candidate.objective.gameObject, false);
                if (mainRoom && Pick(seed, room, "trigger:" + candidate.id, 100) < candidate.chance)
                    availableTriggers.Add(candidate.objective);
            }

            var monsters = new List<Health>();
            foreach (var choice in room.Plan.Content)
            {
                if (!room.Content.TryGetValue(choice.SlotIndex, out var content)) continue;
                RoomContentOption option = null;
                foreach (var slot in room.Template.Content.ContentSettings)
                    if (slot.slotIndex == choice.SlotIndex)
                        foreach (var candidate in slot.options)
                            if (candidate.id == choice.OptionId) option = candidate;
                if (option == null) throw new InvalidOperationException("생성된 콘텐츠의 선택 정보를 찾을 수 없습니다.");
                if (option.kind != ContentKind.Monster && option.kind != ContentKind.EliteMonster) continue;
                var health = content.GetComponent<Health>();
                if (health == null || !health.enabled) throw new InvalidOperationException("목표 몬스터에 활성 Health가 없습니다.");
                monsters.Add(health);
            }

            var candidates = new RoomObjectiveCandidates(monsters, availableTriggers, controller.gameObject, context,
                Pick(seed, room, "target", int.MaxValue));
            var selected = new List<RoomObjective>();
            var applicable = new List<RoomObjectiveRule>();
            if (mainRoom && Pick(seed, room, "condition", 100) < conditionChance)
                foreach (var rule in rules)
                    if (rule != null && rule.CanApply(candidates)) applicable.Add(rule);
            if (applicable.Count > 0)
            {
                if (selectedGate == null) throw new InvalidOperationException("진행 방향의 잠금 출구가 없습니다.");
                applicable[Pick(seed, room, "rule", applicable.Count)].CreateObjectives(candidates, selected);
            }

            context.RecordComponent(controller);
            controller.Configure(selected, selectedGate);
            if (!mainRoom) context.SetActive(gameObject, false);
        }

        private static int Pick(int seed, BuiltRoom room, string domain, int count)
        {
            uint hash = 2166136261u;
            void Add(uint value) => hash = unchecked((hash ^ value) * 16777619u);
            Add(unchecked((uint)seed)); Add(unchecked((uint)room.Plan.Cell.x)); Add(unchecked((uint)room.Plan.Cell.y));
            foreach (char c in room.Template.TemplateId) Add(c);
            foreach (char c in domain) Add(c);
            hash = unchecked(hash * 1664525u + 1013904223u);
            return (int)(((ulong)hash * (uint)count) >> 32);
        }
    }
}
