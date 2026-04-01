using UnityEngine;
[System.Serializable]
public class AgentMovementHandler 
{
    private AgentMover2D _mover;
    private AgentStateData _data;

    public void Initialize(AgentMover2D mover, AgentStateData data)
    {
        _mover = mover;
        _data = data;
    }

    public void HandleMove(Vector2 moveVec)
    {
        _mover.Move(moveVec, _data.moveSpeed);
    }

    public void HandleJump(Vector2 input)
    {
        _mover.Jump(input, _data.jumpForce);
    }
}
