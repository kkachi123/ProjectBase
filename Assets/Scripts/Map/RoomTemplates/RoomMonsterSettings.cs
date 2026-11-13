namespace ProjectRE.MapGeneration
{
    using UnityEngine;

    public enum RoomMonsterKind { Normal, Elite }

    [DisallowMultipleComponent]
    public sealed class RoomMonsterSettings : RoomSpawnSettings
    {
        [SerializeField] private RoomMonsterKind _kind;
        public override ContentKind Kind => _kind == RoomMonsterKind.Elite ? ContentKind.EliteMonster : ContentKind.Monster;
    }
}
