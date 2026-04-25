using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AIMonsterInput))]
public class OrcBrain : MonoBehaviour
{
    [SerializeField] AIMonsterInput _input;
    [SerializeField] GroundDetector _leftGroundDetector;
    [SerializeField] GroundDetector _rightGroundDetector;
    public bool IsFrontGrounded => 
    _leftGroundDetector != null && _direction == -1 && _leftGroundDetector.IsGrounded 
    || _rightGroundDetector != null && _direction == 1 && _rightGroundDetector.IsGrounded;
    private Vector2 _moveDir;
    [SerializeField] private int _direction;
    [SerializeField] private bool isfrontGrounded;
    void Start()
    {
        _input = GetComponent<AIMonsterInput>();
        _leftGroundDetector = GetComponent<GroundDetector>();
        _rightGroundDetector = GetComponent<GroundDetector>();
        // -1 : left, 1 : right
        _direction = Random.value < 0.5f ? -1 : 1;
        _moveDir = new Vector2(_direction, 0);
        StartCoroutine(WalkRoutine());
    }
    
    IEnumerator WalkRoutine()
    {
        while (true)
        {
            _input.Move(_moveDir);
            isfrontGrounded = IsFrontGrounded;
            if(!IsFrontGrounded)
            {
                _moveDir = new Vector2(-_moveDir.x, 0);
                _direction *= -1;
            }
            yield return null;
        }
    }
    
}
