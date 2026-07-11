using UnityEngine;

namespace MapSystem
{
    /// <summary>
    /// 카메라 이동량에 비례해 배경을 따라가게 하는 패럴랙스 레이어.
    /// scrollFactor 0 = 일반 오브젝트처럼 동작 (따라가지 않음, 화면에서 빠르게 스쳐감)
    /// scrollFactor 1 = 카메라에 완전히 고정 (하늘처럼, 절대 안 움직이는 것처럼 보임)
    /// 원경일수록 높은 값. X/Y를 따로 지정할 수 있어 수직형 레벨에도 대응.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = 일반 오브젝트, 1 = 카메라에 완전 고정. X/Y 따로 설정 가능.")]
        [Range(0f, 1f)]
        public Vector2 scrollFactor = new Vector2(0.5f, 0.5f);

        [Tooltip("추적할 카메라. 비워두면 Camera.main을 자동으로 사용.")]
        public Transform targetCamera;

        private Vector3 lastCameraPosition;
        private bool initialized;

        private void OnEnable()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            if (targetCamera != null)
            {
                lastCameraPosition = targetCamera.position;
                initialized = true;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
                lastCameraPosition = targetCamera.position;
                initialized = true;
                return;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (!initialized)
            {
                lastCameraPosition = targetCamera.position;
                initialized = true;
                return;
            }

            Vector3 delta = targetCamera.position - lastCameraPosition;
            Vector3 offset = new Vector3(delta.x * scrollFactor.x, delta.y * scrollFactor.y, 0f);
            transform.position += offset;
            lastCameraPosition = targetCamera.position;
        }
    }
}
