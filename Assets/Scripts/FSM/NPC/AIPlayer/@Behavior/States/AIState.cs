using System;
using Unity.Behavior;

[BlackboardEnum]
public enum AIState
{
	Idle,
	Move,
	Jump,
	Attack,
	Chase,
    Dodge
}
