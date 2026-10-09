namespace ProjectRE
{
    using UnityEngine;

    /// <summary>현재 씬에 배치한 Player 참조 제공.</summary>
    public class ScenePlayerManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;

        public static ScenePlayerManager Instance { get; private set; }
        public PlayerController Player => _player;

        /// <summary>씬 전용 싱글톤 등록. 중복 Component 제거.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"{name}: ScenePlayerManager 중복 Component.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>자신이 등록한 싱글톤 참조 해제.</summary>
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
