namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>제작 원본 목록과 계획기가 사용하는 스냅샷 사이의 경계.</summary>
    [CreateAssetMenu(fileName = "RoomTemplateCatalog", menuName = "Map/Room Template Catalog")]
    public sealed class RoomTemplateCatalog : ScriptableObject
    {
        [SerializeField] private List<RoomTemplate> _templates = new();
        public IReadOnlyList<RoomTemplate> Templates => _templates;

        public bool TryCreateSnapshot(out IReadOnlyList<RoomTemplateSnapshot> snapshots, out string error)
        {
            var result = new List<RoomTemplateSnapshot>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RoomTemplate template in _templates)
            {
                List<string> errors = RoomTemplateChecks.Validate(template);
                if (errors.Count > 0)
                {
                    snapshots = null;
                    error = $"템플릿 검사 실패 ({(template == null ? "null" : template.name)}): {string.Join(", ", errors)}";
                    return false;
                }
                if (!ids.Add(template.TemplateId))
                {
                    snapshots = null;
                    error = $"중복 템플릿 ID: {template.TemplateId}";
                    return false;
                }

                bool hasStart = template.PlayerStart != null;
                bool hasGoal = template.Goal != null;
                var terrain = new List<RoomTerrainSnapshot>();
                foreach (RoomTerrainVariation variation in template.TerrainVariations)
                {
                    var idsForRegion = new List<string>();
                    var cells = new List<Vector2Int>();
                    var chances = new List<int>();
                    foreach (RoomTerrainPattern pattern in variation.patterns) idsForRegion.Add(pattern.id);
                    foreach (RoomChanceCell chance in variation.chanceCells) { cells.Add(chance.cell); chances.Add(chance.chance); }
                    terrain.Add(new RoomTerrainSnapshot(variation.regionIndex, variation.allowEmptyPattern, idsForRegion, cells, chances));
                }
                terrain.Sort((a, b) => a.RegionIndex.CompareTo(b.RegionIndex));
                var content = new List<ContentSlotSnapshot>();
                foreach (RoomContentSlot slot in template.ContentSettings)
                {
                    var options = new List<ContentOptionSnapshot>();
                    foreach (RoomContentOption option in slot.options) options.Add(new ContentOptionSnapshot(option.id, option.weight, option.kind));
                    content.Add(new ContentSlotSnapshot(slot.slotIndex, slot.emptyWeight, options));
                }
                content.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
                result.Add(new RoomTemplateSnapshot(template.TemplateId, template.Type, template.Openings,
                    template.Passages, template.PassagesVerified,
                    hasStart, hasStart ? template.transform.InverseTransformPoint(template.PlayerStart.position) : default,
                    hasGoal, hasGoal ? template.transform.InverseTransformPoint(template.Goal.position) : default,
                    template.VerifiedStartExits, template.VerifiedGoalEntries, terrain, template.VerifiedDeadEndReturns, content,
                    template.BaseGroupId));
            }
            result.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            snapshots = result.AsReadOnly();
            error = null;
            return true;
        }
    }
}
