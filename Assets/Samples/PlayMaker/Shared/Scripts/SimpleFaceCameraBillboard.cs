using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.Rendering;

namespace HutongGames.PlayMaker
{
    /// <summary>
    /// Makes the object face the rendering camera.
    /// Works with multiple cameras via OnWillRenderObject + Camera.current.
    /// </summary>
    [Icon(Strings.EditorIconsPath+"PlayMakerUtilityIcon.png")]
    public class SimpleFaceCameraBillboard : MonoBehaviour
    {
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

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            UpdateFacing(cam);
        }

        private void UpdateFacing(Camera cam)
        {
            if (cam == null)
                return;

            // Face the camera directly
            var toCam = transform.position - cam.transform.position;
            if (toCam.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(toCam, cam.transform.up);
        }
    }
}
