namespace ProjectRE
{
    using UnityEngine;
    using System.Text;

    /// <summary>시제품 진행 수를 월드 텍스트로 표시한다.</summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class RoomObjectivePrototypeDisplay : MonoBehaviour
    {
        [SerializeField] private RoomObjectiveController controller;
        private TextMesh label;

        private void Awake() => label = GetComponent<TextMesh>();

        private void OnEnable()
        {
            if (controller == null) return;
            controller.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null) controller.Changed -= Refresh;
        }

        private void LateUpdate()
        {
            if (controller == null || !controller.PlayerInside || Camera.main == null) return;
            Camera camera = Camera.main;
            Vector3 position = camera.ViewportToWorldPoint(new Vector3(.65f, .82f, Mathf.Abs(camera.transform.position.z)));
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        private void Refresh()
        {
            if (!controller.PlayerInside) { label.text = ""; return; }
            var text = new StringBuilder($"목표 {controller.CompletedCount}/{controller.TotalCount}");
            foreach (var objective in controller.Objectives)
                if (objective != null) text.Append($"\n{objective.Description}: {(objective.IsComplete ? "완료" : "진행 중")}");
            label.text = text.ToString();
        }
    }
}
