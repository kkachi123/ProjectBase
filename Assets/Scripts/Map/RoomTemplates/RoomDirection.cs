namespace ProjectRE.MapGeneration
{
    using UnityEngine;

    internal static class RoomDirection
    {
        public static readonly RoomOpening[] All =
            { RoomOpening.North, RoomOpening.East, RoomOpening.South, RoomOpening.West };

        public static bool MatchesType(RoomType type, RoomOpening openings) => type switch
        {
            RoomType.Side => openings == RoomOpening.None,
            RoomType.Horizontal => openings != RoomOpening.None &&
                (openings & ~(RoomOpening.East | RoomOpening.West)) == 0,
            RoomType.Down => (openings & RoomOpening.South) != 0,
            RoomType.Arrival => (openings & RoomOpening.North) != 0 && (openings & RoomOpening.South) == 0,
            _ => false,
        };

        public static RoomOpening Opposite(RoomOpening side) => side switch
        {
            RoomOpening.North => RoomOpening.South,
            RoomOpening.East => RoomOpening.West,
            RoomOpening.South => RoomOpening.North,
            RoomOpening.West => RoomOpening.East,
            _ => RoomOpening.None,
        };

        public static Vector2Int Offset(RoomOpening side) => side switch
        {
            RoomOpening.North => new Vector2Int(0, -1),
            RoomOpening.East => Vector2Int.right,
            RoomOpening.South => new Vector2Int(0, 1),
            RoomOpening.West => Vector2Int.left,
            _ => Vector2Int.zero,
        };

        public static RoomOpening Between(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            foreach (RoomOpening side in All)
                if (Offset(side) == delta) return side;
            return RoomOpening.None;
        }

        public static Vector3 Origin(Vector2Int cell) =>
            new Vector3(cell.x * RoomTemplate.RoomSize, -cell.y * RoomTemplate.RoomSize, 0);
    }
}
