using System.Collections.Generic;
using UnityEngine;

namespace MapSystem.Editor
{
    /// <summary>
    /// 지면 타일 위의 "발판 표면"을 나타내는 세그먼트.
    /// height는 chunk_system_spec_v3.md 5.1의 h(=ty+1, 타일 바로 위 빈 칸)와 동일한 좌표계.
    /// </summary>
    public struct ChunkSegment
    {
        public int xStart;
        public int xEnd; // inclusive
        public int height;

        public int Width => xEnd - xStart + 1;
    }

    /// <summary>
    /// chunk_system_spec_v3.md 5장(extract_segments / seg_gap / can_jump / BFS 도달가능성 검증)을
    /// 그대로 C#으로 포팅한 것. 청크 자동 생성기의 최종 검증에 쓰이며, 그래프 자체는 순수 함수라
    /// 추후 기존 청크 재검사 도구에서도 재사용할 수 있다.
    /// </summary>
    public static class ChunkReachability
    {
        public static List<ChunkSegment> ExtractSegments(HashSet<Vector2Int> tiles)
        {
            var segments = new List<ChunkSegment>();
            if (tiles.Count == 0) return segments;

            int minX = int.MaxValue, maxX = int.MinValue;
            foreach (var t in tiles)
            {
                if (t.x < minX) minX = t.x;
                if (t.x > maxX) maxX = t.x;
            }

            var colYs = new Dictionary<int, HashSet<int>>();
            foreach (var t in tiles)
            {
                if (!colYs.TryGetValue(t.x, out var set))
                {
                    set = new HashSet<int>();
                    colYs[t.x] = set;
                }
                set.Add(t.y);
            }

            var colSurfaces = new Dictionary<int, HashSet<int>>();
            for (int x = minX; x <= maxX; x++)
            {
                var tops = new HashSet<int>();
                if (colYs.TryGetValue(x, out var ys))
                {
                    foreach (var y in ys)
                        if (!ys.Contains(y + 1))
                            tops.Add(y + 1);
                }
                colSurfaces[x] = tops;
            }

            var allHeights = new SortedSet<int>();
            foreach (var kv in colSurfaces)
                foreach (var h in kv.Value)
                    allHeights.Add(h);

            foreach (int h in allHeights)
            {
                int curStart = -1;
                int prevX = -1;
                for (int x = minX; x <= maxX + 1; x++)
                {
                    bool present = x <= maxX && colSurfaces[x].Contains(h);
                    if (present)
                    {
                        if (curStart == -1) curStart = x;
                        prevX = x;
                    }
                    else if (curStart != -1)
                    {
                        segments.Add(new ChunkSegment { xStart = curStart, xEnd = prevX, height = h });
                        curStart = -1;
                    }
                }
            }

            return segments;
        }

        public static int SegGap(ChunkSegment a, ChunkSegment b)
        {
            if (a.xEnd < b.xStart) return b.xStart - a.xEnd - 1;
            if (b.xEnd < a.xStart) return a.xStart - b.xEnd - 1;
            return 0;
        }

        /// <summary>
        /// a에서 b로의 점프 가능 여부(방향 있음). 핵심 공식: dx(a,b) &lt;= maxJumpX AND height(b)-height(a) &lt;= maxRiseY.
        /// 내려가는 경우 height(b)-height(a)가 음수이므로 자동으로 항상 만족한다(하강 무제한).
        /// </summary>
        public static bool CanJump(ChunkSegment a, ChunkSegment b, int maxJumpX, int maxRiseY)
        {
            return SegGap(a, b) <= maxJumpX && (b.height - a.height) <= maxRiseY;
        }

        /// <summary>
        /// entrance1/entrance2는 타일 인덱스가 아니라 "이음매(seam)" 좌표다 — 청크 왼쪽 끝은 x=0(첫 타일의
        /// 왼쪽 경계), 오른쪽 끝은 x=width(마지막 타일의 오른쪽 경계, 즉 xEnd+1)로 기록된다(실제 프리팹 예:
        /// 20폭 청크의 entrance2.x=20, 세그먼트는 xEnd=19). 그래서 xEnd 쪽 경계는 +1까지 포함해야 한다.
        /// </summary>
        public static int FindSegmentAt(List<ChunkSegment> segments, Vector2Int point)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                if (s.height == point.y && point.x >= s.xStart && point.x <= s.xEnd + 1)
                    return i;
            }
            return -1;
        }

        /// <summary>startIdx에서 CanJump 간선만 따라 BFS로 도달 가능한 모든 세그먼트 인덱스 집합.</summary>
        public static HashSet<int> ReachableFrom(List<ChunkSegment> segments, int startIdx, int maxJumpX, int maxRiseY)
        {
            var visited = new HashSet<int> { startIdx };
            var queue = new Queue<int>();
            queue.Enqueue(startIdx);

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                for (int i = 0; i < segments.Count; i++)
                {
                    if (visited.Contains(i)) continue;
                    if (CanJump(segments[cur], segments[i], maxJumpX, maxRiseY))
                    {
                        visited.Add(i);
                        queue.Enqueue(i);
                    }
                }
            }

            return visited;
        }
    }
}
