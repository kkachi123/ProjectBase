using UnityEditor;
using UnityEngine;

namespace MapSystem.Editor
{
    /// <summary>
    /// ChunkDatabase를 이용해 청크를 순서대로 이어붙여 랜덤 맵을 생성한다.
    ///
    /// entrance1/entrance2는 "입구/출구"라는 고정된 역할이 아니라 그냥 "연결 가능한 두 지점"이다.
    /// 매 스텝마다 두 지점 중 어느 쪽이든 현재 커서에 연결해보고, 겹치지 않는 배치가 나오는
    /// 쪽을 채택한다. 방향 플래그나 진입 높이 필터는 없다 — 필요한 조건은 오직
    /// (1) 타일이 겹치지 않을 것, (2) 실제로 연결점이 이어질 것, 이 두 가지뿐이다.
    ///
    /// 이 클래스는 순수하게 에디터 GUI만 담당한다. 실제 조합 알고리즘은
    /// ChunkLinearGenerator / ChunkBranchingGenerator에 있고, 둘이 공유하는 저수준
    /// 연결·배치 로직은 ChunkPlacementUtility에 있다.
    /// </summary>
    public class ChunkMapGeneratorWindow : EditorWindow
    {
        private ChunkDatabase database;
        private int stepCount = 10;
        private int seed = 0;
        private bool useRandomSeed = true;

        [MenuItem("Map/Generate Random Map")]
        public static void Open()
        {
            GetWindow<ChunkMapGeneratorWindow>("Chunk Map Generator");
        }

        private void OnEnable()
        {
            if (database == null)
                database = AssetDatabase.LoadAssetAtPath<ChunkDatabase>("Assets/Data/ChunkDatabase.asset");
        }

        private void OnGUI()
        {
            DrawAssembleMapGUI();
        }

        private void DrawAssembleMapGUI()
        {
            database = (ChunkDatabase)EditorGUILayout.ObjectField("Chunk Database", database, typeof(ChunkDatabase), false);
            stepCount = EditorGUILayout.IntField("Step Count (Transition+Content 합)", stepCount);
            useRandomSeed = EditorGUILayout.Toggle("Random Seed", useRandomSeed);
            if (!useRandomSeed)
                seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Map (선형)"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                ChunkLinearGenerator.Generate(database, stepCount, actualSeed);
            }
            if (GUILayout.Button("Generate Map (가지치기 — 다방향 청크에서 분기)"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                ChunkBranchingGenerator.GenerateBranching(database, stepCount, actualSeed);
            }
            if (GUILayout.Button("Clear Generated Map"))
            {
                GameObject existingRoot = GameObject.Find(ChunkPlacementUtility.RootName);
                if (existingRoot != null) Object.DestroyImmediate(existingRoot);
            }
        }
    }
}
