namespace ProjectRE.MapGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    /// <summary>계획 계산에 필요한 템플릿 데이터. 씬 오브젝트를 참조하지 않는다.</summary>
    public sealed class RoomTemplateSnapshot
    {
        private readonly ReadOnlyCollection<RoomPassage> _passages;

        public string Id { get; }
        public string BaseGroupId { get; }
        public RoomType Type { get; }
        public RoomOpening Openings { get; }
        public bool PassagesVerified { get; }
        public bool HasPlayerStart { get; }
        public bool HasGoal { get; }
        public Vector3 PlayerStart { get; }
        public Vector3 Goal { get; }
        public RoomOpening VerifiedStartExits { get; }
        public RoomOpening VerifiedGoalEntries { get; }
        public RoomOpening VerifiedDeadEndReturns { get; }
        public IReadOnlyList<RoomPassage> Passages => _passages;
        public IReadOnlyList<RoomTerrainSnapshot> Terrain { get; }
        public IReadOnlyList<ContentSlotSnapshot> Content { get; }

        public RoomTemplateSnapshot(string id, RoomType type, RoomOpening openings,
            IEnumerable<RoomPassage> passages, bool passagesVerified,
            bool hasPlayerStart = false, Vector3 playerStart = default,
            bool hasGoal = false, Vector3 goal = default,
            RoomOpening verifiedStartExits = RoomOpening.None, RoomOpening verifiedGoalEntries = RoomOpening.None,
            IEnumerable<RoomTerrainSnapshot> terrain = null, RoomOpening verifiedDeadEndReturns = RoomOpening.None,
            IEnumerable<ContentSlotSnapshot> content = null, string baseGroupId = null)
        {
            Id = id;
            BaseGroupId = string.IsNullOrWhiteSpace(baseGroupId) ? id : baseGroupId;
            Type = type;
            Openings = openings;
            _passages = new List<RoomPassage>(passages ?? Array.Empty<RoomPassage>()).AsReadOnly();
            PassagesVerified = passagesVerified;
            HasPlayerStart = hasPlayerStart;
            PlayerStart = playerStart;
            HasGoal = hasGoal;
            Goal = goal;
            VerifiedStartExits = verifiedStartExits;
            VerifiedGoalEntries = verifiedGoalEntries;
            VerifiedDeadEndReturns = verifiedDeadEndReturns;
            Terrain = new List<RoomTerrainSnapshot>(terrain ?? Array.Empty<RoomTerrainSnapshot>()).AsReadOnly();
            Content = new List<ContentSlotSnapshot>(content ?? Array.Empty<ContentSlotSnapshot>()).AsReadOnly();
        }

        public bool Supports(RoomOpening entry, RoomOpening exit)
        {
            if (!PassagesVerified) return false;
            foreach (RoomPassage passage in _passages)
                if (passage.entry == entry && passage.exit == exit) return true;
            return false;
        }
    }
}
