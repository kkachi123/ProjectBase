namespace ProjectRE.MapGeneration
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class RoomItemSettings : RoomSpawnSettings
    {
        public override ContentKind Kind => ContentKind.Item;
    }
}
