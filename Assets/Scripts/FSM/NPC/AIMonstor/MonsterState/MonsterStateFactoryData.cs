namespace ProjectRE
{
    /// <summary>기본 Monster State 구성에 필요한 공통·전용 의존성 전달.</summary>
    public class MonsterStateFactoryData : StateFactoryData
    {
        /// <summary>공통 Animator 참조의 MonsterAnimator 타입 접근·주입.</summary>
        public MonsterAnimator MonsterAnimator
        {
            get => (MonsterAnimator)Animator;
            set => Animator = value;
        }
    }
}
