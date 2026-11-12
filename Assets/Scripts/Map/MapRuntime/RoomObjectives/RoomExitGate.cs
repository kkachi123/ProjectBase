namespace ProjectRE
{
    using System;
    using DG.Tweening;
    using ProjectRE.MapGeneration;
    using UnityEngine;

    /// <summary>주 출구를 막고 목표 완료 시 충돌 해제와 페이드를 담당한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class RoomExitGate : MonoBehaviour
    {
        [SerializeField, Min(0)] private float fadeDuration = .5f;
        private BoxCollider2D barrier;
        private SpriteRenderer visual;
        private Tween fade;
        private bool opened;

        public bool IsOpen => opened;

        public void Place(RoomOpening direction)
        {
            RectInt opening = RoomTemplate.OpeningCells(direction);
            const float inset = .35f;
            Vector3 position = new Vector3(opening.center.x, opening.center.y, 0);
            switch (direction)
            {
                case RoomOpening.West: position.x = inset; break;
                case RoomOpening.East: position.x = RoomTemplate.RoomSize - inset; break;
                case RoomOpening.South: position.y = inset; break;
                case RoomOpening.North: position.y = RoomTemplate.RoomSize - inset; break;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(0, 0,
                direction == RoomOpening.North || direction == RoomOpening.South ? 90 : 0);
        }

        public void Open()
        {
            if (opened) return;
            opened = true;
            barrier = GetComponent<BoxCollider2D>();
            visual = GetComponent<SpriteRenderer>();
            barrier.enabled = false;
            if (!Application.isPlaying || fadeDuration <= 0)
            {
                Color color = visual.color;
                color.a = 0;
                visual.color = color;
                return;
            }
            fade = visual.DOFade(0, fadeDuration).SetLink(gameObject).OnKill(() => fade = null);
        }

        private void OnDisable()
        {
            fade?.Kill();
            if (opened && visual != null)
            {
                Color color = visual.color;
                color.a = 0;
                visual.color = color;
            }
        }
    }
}
