using UniRx;
using UnityEngine;
public interface IAgentCombatInput
{
    IReadOnlyReactiveProperty<int> AttackPressed { get; }
}
