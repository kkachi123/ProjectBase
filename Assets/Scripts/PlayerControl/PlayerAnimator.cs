using DG.Tweening;
using UniRx;
using UnityEngine;

// 플레이어의 애니메이션을 제어하는 스크립트입니다. Animator 컴포넌트를 필요로 합니다.
[RequireComponent(typeof(Animator))]
// SpriteRenderer 컴포넌트도 필요로 합니다. 이는 플레이어의 스프라이트를 제어하기 위해 사용됩니다.
[RequireComponent(typeof(SpriteRenderer))]

public class PlayerAnimator : MonoBehaviour
{
    private Animator _anim;
    private SpriteRenderer _spriteRenderer;

    private static readonly int MoveDirHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGround");

    Health _playerHealth;

    public void Initialize(Health health)
    {
        _playerHealth = health;
        _anim = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        if (_playerHealth != null)
        {
            _playerHealth.CurrentHealth
            .Pairwise() // 이전 값과 현재 값을 쌍으로 묶음 (이전, 현재)
            .Where(pair => pair.Current != pair.Previous) // 값이 실제로 변했을 때만 실행
            .Subscribe( _ =>
            {
                PlayHurtSequence();
            })
            .AddTo(this);
        }
    }
    #region Animation Control
    public void ApplyMovementAnimation(float velocity)
    {
        _anim.SetFloat(MoveDirHash, Mathf.Abs(velocity));

        if(Mathf.Abs(velocity) > 0.01f)
        {
            if (velocity > 0) _spriteRenderer.flipX = false;
            else if (velocity < 0) _spriteRenderer.flipX = true;
        }
    }

    public void ApplyJumpingAnimation(bool isJumping , bool isGrounded)
    {
        _anim.SetBool(IsJumpingHash, isJumping);
        _anim.SetBool(IsGroundedHash, isGrounded);
    }

    public void ApplyFallingAnimation(bool isGrounded)
    {
        _anim.SetBool(IsGroundedHash, isGrounded);
    }

    public void ApplyDieAnimation()
    {
        _anim.SetTrigger("Die");
    }
    #endregion

    #region Sprite Control
    public void PlayHurtSequence()
    {
        Sequence hurtSequence = DOTween.Sequence();
        hurtSequence.Append(_spriteRenderer.DOColor(Color.red, 0.1f))
                    .Append(_spriteRenderer.DOColor(Color.white, 0.1f))
                    .SetLoops(3 , LoopType.Yoyo)
                    .SetEase(Ease.InOutQuad);
    }

    #endregion
}
