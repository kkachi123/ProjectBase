using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.Rendering;

namespace HutongGames.PlayMaker
{
    /// <summary>
    /// Rotates a quad around a chosen local axis (e.g. +Z) so that
    /// its face behaves like a ribbon facing the camera, without
    /// changing the axis direction itself.
    /// </summary>
    [Icon(Strings.EditorIconsPath+"PlayMakerUtilityIcon.png")]
    public class AxisBillboardRibbon : MonoBehaviour
    {
        public enum Axis { X, Y, Z }

        [Tooltip("Local axis that defines the ribbon direction (does NOT rotate).")]
        public Axis ribbonAxis = Axis.Z;

        [Tooltip("Local vector perpendicular to the axis, used as the reference 'side' of the quad.")]
        public Vector3 referenceSideLocal = Vector3.up;

        [Tooltip("If true, flip 180° when the ribbon is back-facing the camera.")]
        public bool ensureFacingCamera = true;

        private void Awake()
        {
            // If reference side accidentally aligns with the axis, pick a safe default
            var axisLocal = GetAxisLocal();
            if (Vector3.Dot(axisLocal.normalized, referenceSideLocal.normalized) > 0.999f)
            {
                // pick something orthogonal
                referenceSideLocal = Vector3.Cross(axisLocal, Vector3.up);
                if (referenceSideLocal.sqrMagnitude < 1e-4f)
                    referenceSideLocal = Vector3.Cross(axisLocal, Vector3.right);
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        private void UnregisterCallbacks()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnWillRenderObject()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                return;

            UpdateFacing(Camera.current);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera currentCam)
        {
            UpdateFacing(currentCam);
        }

        private void UpdateFacing(Camera currentCam)
        {
            if (currentCam == null)
                return;

            // World-space axis that must stay fixed (no yaw/pitch changes)
            var axisWorld = transform.TransformDirection(GetAxisLocal()).normalized;

            // Current world-space side vector (perpendicular to axis)
            var sideWorld = transform.TransformDirection(referenceSideLocal).normalized;

            // Direction from ribbon to camera
            var camDir = (currentCam.transform.position - transform.position).normalized;

            // Project camera direction onto plane orthogonal to axis
            var targetSide = Vector3.ProjectOnPlane(camDir, axisWorld);

            var magSq = targetSide.sqrMagnitude;
            if (magSq < 1e-6f)
            {
                // Camera is almost exactly along the axis; nothing sensible to do
                return;
            }

            targetSide /= Mathf.Sqrt(magSq);

            // Angle to rotate sideWorld -> targetSide, around axisWorld
            var angle = Vector3.SignedAngle(sideWorld, targetSide, axisWorld);

            // Apply roll around the ribbon axis
            transform.rotation = Quaternion.AngleAxis(angle, axisWorld) * transform.rotation;

            if (ensureFacingCamera)
            {
                // Check if the quad's normal is facing away and flip if needed
                var normalWorld = transform.TransformDirection(Vector3.forward);
                var dot = Vector3.Dot(normalWorld, (transform.position - currentCam.transform.position).normalized);
                if (dot < 0f)
                {
                    // Flip 180 around axis so front side faces camera
                    transform.rotation = Quaternion.AngleAxis(180f, axisWorld) * transform.rotation;
                }
            }
        }

        private Vector3 GetAxisLocal()
        {
            return ribbonAxis switch
            {
                Axis.X => Vector3.right,
                Axis.Y => Vector3.up,
                _ => Vector3.forward
            };
        }
    }
}
