using UnityEditor;
using UnityEngine;

namespace GridMapSystem.Editor
{
    /// <summary>
    /// GridChunkDatabase를 이용해 청크를 그리드(10 단위 셀) 위에 4방향 자유 분기로 조합한다.
    /// 방식 1(ChunkMapGeneratorWindow, 픽셀 커서 기반)과는 완전히 독립된 창이다.
    /// </summary>
    public class GridChunkMapGeneratorWindow : EditorWindow
    {
        private GridChunkDatabase database;
        private int stepCount = 10;
        private int seed = 0;
        private bool useRandomSeed = true;

        [MenuItem("Map/Generate Grid Map")]
        public static void Open()
        {
            GetWindow<GridChunkMapGeneratorWindow>("Grid Chunk Map Generator");
        }

        private void OnEnable()
        {
            if (database == null)
                database = AssetDatabase.LoadAssetAtPath<GridChunkDatabase>("Assets/Prefabs/@Data/Map/GridChunkDatabase.asset");
        }

        private void OnGUI()
        {
            database = (GridChunkDatabase)EditorGUILayout.ObjectField("Grid Chunk Database", database, typeof(GridChunkDatabase), false);
            stepCount = EditorGUILayout.IntField("Content Chunk Count", stepCount);
            useRandomSeed = EditorGUILayout.Toggle("Random Seed", useRandomSeed);
            if (!useRandomSeed)
                seed = EditorGUILayout.IntField("Seed", seed);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Map (그리드)"))
            {
                int actualSeed = useRandomSeed ? System.Environment.TickCount : seed;
                ChunkGridGenerator.GenerateGrid(database, stepCount, actualSeed);
            }
            if (GUILayout.Button("Clear Generated Grid Map"))
            {
                GameObject existingRoot = GameObject.Find(GridPlacementUtility.RootName);
                if (existingRoot != null) Object.DestroyImmediate(existingRoot);
            }
        }
    }
}
