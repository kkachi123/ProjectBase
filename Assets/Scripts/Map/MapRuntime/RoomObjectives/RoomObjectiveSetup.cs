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

        [SerializeField] private RoomObjectiveController controller;
        [SerializeField, Range(0, 100)] private int conditionChance = 50;
        [SerializeField] private List<RoomObjectiveRule> rules = new();
        [SerializeField] private List<TriggerCandidate> triggers = new();
        public void Configure(BuiltRoom room, int seed, MapBuildContext context, GameObject gatePrefab)
        {
            if (room == null || context == null || controller == null || !controller.transform.IsChildOf(transform))
                throw new InvalidOperationException("방 목표 설정 또는 관리자가 올바르지 않습니다.");
            if (room.Template.gameObject.activeInHierarchy)
                throw new InvalidOperationException("비활성 맵에서 방 목표를 구성해야 합니다.");

            bool mainRoom = !room.Plan.IsOptional && room.Plan.Entry != RoomOpening.None && room.Plan.Exit != RoomOpening.None;
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
            foreach (var monster in room.Monsters)
            {
                var health = monster.GetComponent<Health>();
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
                applicable[Pick(seed, room, "rule", applicable.Count)].CreateObjectives(candidates, selected);
            }

            RoomExitGate gate = null;
            if (selected.Count > 0)
            {
                var gateComponent = gatePrefab == null ? null : gatePrefab.GetComponent<RoomExitGate>();
                var gateCollider = gatePrefab == null ? null : gatePrefab.GetComponent<BoxCollider2D>();
                var gateVisual = gatePrefab == null ? null : gatePrefab.GetComponent<SpriteRenderer>();
                if (gateComponent == null || !gateComponent.enabled || !gatePrefab.activeSelf || gateCollider == null ||
                    !gateCollider.enabled || gateCollider.isTrigger || gateVisual == null || !gateVisual.enabled || gateVisual.sprite == null)
                    throw new InvalidOperationException("출구 문 프리팹에 활성 문·충돌체·Sprite가 필요합니다.");
                var instance = context.InstantiatePrefab(gatePrefab, room.Template.transform);
                if (instance == null) throw new InvalidOperationException("출구 문을 생성하지 못했습니다.");
                gate = instance.GetComponent<RoomExitGate>();
                context.RecordComponent(instance.transform);
                gate.Place(room.Plan.Exit);
            }
            context.RecordComponent(controller);
            controller.Configure(selected, gate);
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
