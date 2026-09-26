using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace HutongGames.PlayMaker
{
    /// <summary>
    /// Fades a SpriteRenderer as it gets close to the camera to hide
    /// ugly near-plane intersections (e.g., laser bolts).
    /// Designed to be pooling-friendly: it does not permanently disable
    /// the renderer and resets color on reuse.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [Icon(Strings.EditorIconsPath+"PlayMakerUtilityIcon.png")]
    public class SpriteNearFade : MonoBehaviour
    {
        [Tooltip("Distance along the camera's forward where fading starts (world units).")]
        public float fadeStart = 1.5f;

        [Tooltip("Distance along the camera's forward where the sprite is fully invisible.")]
        public float fadeEnd = 0.3f;

        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            // Capture whatever color the sprite currently has when spawned/reused
            _baseColor = _spriteRenderer.color;
            _spriteRenderer.enabled = true;
        }

        private void OnDisable()
        {
            // Restore original color so pooled objects don't keep a faded alpha
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
                _spriteRenderer.enabled = true;
            }
        }

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || _spriteRenderer == null)
                return;

            // Distance along camera forward (not full 3D distance)
            Vector3 toSprite = transform.position - cam.transform.position;
            float distForward = Vector3.Dot(toSprite, cam.transform.forward);

            // Behind camera? Just hide by alpha 0, but keep renderer enabled.
            if (distForward <= 0f)
            {
                SetAlpha(0f);
                return;
            }

            float alpha = 1f;

            if (distForward <= fadeEnd)
            {
                alpha = 0f;
            }
            else if (distForward < fadeStart)
            {
                alpha = Mathf.InverseLerp(fadeEnd, fadeStart, distForward);
            }

            SetAlpha(alpha);
        }

        private void SetAlpha(float alpha)
        {
            var c = _baseColor;
            c.a = _baseColor.a * alpha;
            _spriteRenderer.color = c;
        }
    }
}
