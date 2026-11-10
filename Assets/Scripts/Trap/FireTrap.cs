namespace ProjectRE
{
    using UnityEngine;

    /// <summary>불의 점화와 소화를 반복하고 활성 상태에만 접촉 피해를 적용한다.</summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator), typeof(TrapContactDamage))]
    public sealed class FireTrap : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float onDuration = 2f;
        [SerializeField, Min(0.1f)] private float offDuration = 2f;
        [SerializeField] private Sprite offSprite;

        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private TrapContactDamage contactDamage;
        private Collider2D damageCollider;
        private bool burning;
        private float remaining;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            contactDamage = GetComponent<TrapContactDamage>();
            damageCollider = GetComponent<Collider2D>();
        }

        private void OnEnable() => SetBurning(true);

        private void OnDisable() => SetBurning(false);

        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) SetBurning(!burning);
        }

        private void SetBurning(bool value)
        {
            burning = value;
            remaining = Mathf.Max(0.1f, value ? onDuration : offDuration);
            contactDamage.enabled = value;
            damageCollider.enabled = value;
            animator.enabled = value;
            if (value)
                animator.Play(0, 0, 0f);
            else
                spriteRenderer.sprite = offSprite;
        }
    }
}
