using System;
using DG.Tweening;
using UniRx;
using UnityEngine;

// Animator 컴포넌트와 SpriteRenderer 컴포넌트를 필수로 요구하는 스크립트입니다.
[RequireComponent(typeof(Animator))]
// SpriteRenderer는 플레이어의 방향에 따라 스프라이트를 뒤집는 데 사용됩니다.
[RequireComponent(typeof(SpriteRenderer))]

public class PlayerAnimator : MonoBehaviour
{
    private Animator _anim;
    private SpriteRenderer _spriteRenderer;

    private static readonly int MoveDirHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsJumpingHash = Animator.StringToHash("JumpTrigger");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGround");

    private static readonly int DieHash = Animator.StringToHash("DieTrigger");

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
            .Pairwise() 
            .Where(pair => pair.Current < pair.Previous) 
            .Subscribe( _ =>
            {
                PlayHurtSequence();
            })
            .AddTo(this);
        }
    }
    #region Animation Control
    public void ApplyMovementAnimation(Vector2 horizontalInput)
    {
        float velocity = horizontalInput.x;
        _anim.SetFloat(MoveDirHash, Math.Abs(velocity));

        if(Math.Abs(velocity) > 0.01f)
        {
            if (velocity > 0) _spriteRenderer.flipX = false;
            else if (velocity < 0) _spriteRenderer.flipX = true;
        }
    }

    public void ApplyJumpingAnimation()
    {
        _anim.SetTrigger(IsJumpingHash);
    }

    public void ApplyFallingAnimation(bool isFalling)
    {
        _anim.SetBool(IsGroundedHash, !isFalling);
    }

    public void ApplyDieAnimation()
    {
        _anim.SetTrigger(DieHash);
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
