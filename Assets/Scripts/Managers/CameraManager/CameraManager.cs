using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Camera _cameraPrefab;

    public Camera CurrentCamera { get; private set; }
    public CameraFollow2D CurrentFollow { get; private set; }

    public void BindTarget(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[CameraManager] Target is missing.");
            return;
        }

        Camera camera = GetOrCreateCamera();
        if (camera == null)
        {
            Debug.LogError("[CameraManager] Camera is missing.");
            return;
        }

        CameraFollow2D follow = GetOrCreateFollow(camera);
        follow.SetTarget(target);
    }

    public void ResetCameraPosition(Vector3 position)
    {
        Camera camera = GetOrCreateCamera();
        if (camera == null) return;

        CameraFollow2D follow = GetOrCreateFollow(camera);
        camera.transform.position = position + follow.Offset;
    }

    private Camera GetOrCreateCamera()
    {
        if (CurrentCamera != null)
            return CurrentCamera;

        if (Camera.main != null)
        {
            CurrentCamera = Camera.main;
            return CurrentCamera;
        }

        if (_cameraPrefab != null)
        {
            CurrentCamera = Instantiate(_cameraPrefab);
            CurrentCamera.gameObject.name = _cameraPrefab.gameObject.name;
            if (!CurrentCamera.CompareTag("MainCamera"))
                CurrentCamera.tag = "MainCamera";

            return CurrentCamera;
        }

        CurrentCamera = FindFirstObjectByType<Camera>();
        return CurrentCamera;
    }

    private CameraFollow2D GetOrCreateFollow(Camera camera)
    {
        if (CurrentFollow != null)
            return CurrentFollow;

        CurrentFollow = camera.GetComponent<CameraFollow2D>();
        if (CurrentFollow == null)
            CurrentFollow = camera.gameObject.AddComponent<CameraFollow2D>();

        return CurrentFollow;
    }
}
