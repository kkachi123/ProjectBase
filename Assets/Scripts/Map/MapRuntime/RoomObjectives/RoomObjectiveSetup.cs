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

        /// <summary>선택 결과와 관계없이 방에 저장된 목표 연결을 검사한다.</summary>
        public List<string> ValidateConfiguration()
        {
            var errors = new List<string>();
            if (controller == null || !controller.transform.IsChildOf(transform))
                errors.Add("방 목표 관리자 연결이 올바르지 않습니다.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var objects = new HashSet<RoomTriggerObjective>();
            for (int i = 0; i < triggers.Count; i++)
            {
                var candidate = triggers[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.id) || !ids.Add(candidate.id) ||
                    candidate.objective == null || !objects.Add(candidate.objective) || !candidate.objective.enabled ||
                    candidate.objective.gameObject == gameObject || !candidate.objective.transform.IsChildOf(transform) ||
                    candidate.chance < 0 || candidate.chance > 100)
                {
                    errors.Add($"방 트리거 목표 후보 #{i + 1}의 ID·연결·확률이 올바르지 않습니다.");
                    continue;
                }
                var collider = candidate.objective.GetComponent<Collider2D>();
                if (collider == null || !collider.enabled || !collider.isTrigger)
                    errors.Add($"방 트리거 목표 후보 #{i + 1}에 활성 Trigger Collider가 필요합니다.");
            }
            return errors;
        }

        public bool HasEnabledRules => conditionChance > 0 && rules.Exists(rule => rule != null);

        public static List<string> ValidateTemplate(RoomTemplate template)
        {
            var errors = new List<string>();
            if (template == null) return errors;
            var setup = template.GetComponentInChildren<RoomObjectiveSetup>(true);
            if (setup == null) return errors;
            errors.AddRange(setup.ValidateConfiguration());
            var monsters = template.GetComponent<RoomMonsterSettings>();
            if (monsters == null) return errors;
            for (int i = 0; i < monsters.Slots.Count; i++)
            {
                var slot = monsters.Slots[i];
                if (slot == null) continue;
                for (int j = 0; j < slot.options.Count; j++)
                {
                    var option = slot.options[j];
                    if (option == null || option.prefab == null) continue;
                    var health = option.prefab.GetComponent<Health>();
                    if (health == null || !health.enabled)
                        errors.Add($"몬스터 슬롯 #{i + 1} 후보 #{j + 1}에 활성 Health가 필요합니다.");
                }
            }
            return errors;
        }

        public static bool IsValidGatePrefab(GameObject prefab)
        {
            var gate = prefab == null ? null : prefab.GetComponent<RoomExitGate>();
            var collider = prefab == null ? null : prefab.GetComponent<BoxCollider2D>();
            var visual = prefab == null ? null : prefab.GetComponent<SpriteRenderer>();
            return gate != null && gate.enabled && prefab.activeSelf && collider != null && collider.enabled &&
                !collider.isTrigger && visual != null && visual.enabled && visual.sprite != null;
        }

        public void Configure(BuiltRoom room, int seed, MapBuildContext context, GameObject gatePrefab)
        {
            if (room == null || context == null)
                throw new InvalidOperationException("방 목표 설정 또는 관리자가 올바르지 않습니다.");
            var configurationErrors = ValidateConfiguration();
            if (configurationErrors.Count > 0)
                throw new InvalidOperationException(string.Join(" ", configurationErrors));
            if (room.Template.gameObject.activeInHierarchy)
                throw new InvalidOperationException("비활성 맵에서 방 목표를 구성해야 합니다.");

            bool mainRoom = !room.Plan.IsOptional && room.Plan.Entry != RoomOpening.None && room.Plan.Exit != RoomOpening.None;
            var availableTriggers = new List<RoomTriggerObjective>();
            foreach (var candidate in triggers)
            {
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
                if (!IsValidGatePrefab(gatePrefab))
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
