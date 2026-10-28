namespace ProjectRE
{
    using ProjectRE.MapGeneration;
    using UnityEngine;

    /// <summary>계획·방 생성과 기존 Run의 맵 정보 사이를 연결한다.</summary>
    public static class RoomMapFactory
    {
        public static bool TryCreate(MapPlanningSettings settings, int seed, GameObject goalPrefab,
            MapBuildContext context, out GeneratedMapInfo info, out MapPlan plan, out string error)
        {
            info = null;
            plan = null;
            GoalTrigger goal = goalPrefab == null ? null : goalPrefab.GetComponent<GoalTrigger>();
            Collider2D collider = goalPrefab == null ? null : goalPrefab.GetComponent<Collider2D>();
            if (goal == null || !goal.enabled || collider == null || !collider.enabled || !collider.isTrigger || !goalPrefab.activeSelf)
            {
                error = "활성화된 GoalTrigger와 Trigger Collider가 있는 Goal 프리팹이 필요합니다.";
                return false;
            }

            context ??= new MapBuildContext();
            if (context.AddComponent == null || context.RecordComponent == null)
            {
                error = "맵 컴포넌트 생성 컨텍스트가 없습니다.";
                return false;
            }
            MapBuildResult result = MapGenerator.Generate(settings, seed, goalPrefab, context);
            if (!result.Success) { error = result.Error; return false; }
            try
            {
                info = (GeneratedMapInfo)context.AddComponent(result.Root, typeof(GeneratedMapInfo));
                if (info == null) throw new System.InvalidOperationException("GeneratedMapInfo 생성 실패");
                context.RecordComponent(info);
                info.playerSpawnPosition = result.Plan.PlayerStart;
                info.endPosition = result.Plan.Goal;
                plan = result.Plan;
                error = null;
                return true;
            }
            catch (System.Exception exception)
            {
                context.DestroyObject(result.Root);
                info = null;
                error = $"맵 정보 생성 실패: {exception.Message}";
                return false;
            }
        }
    }
}
