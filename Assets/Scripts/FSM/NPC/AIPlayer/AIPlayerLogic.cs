using UnityEngine;
using System.Collections;
[RequireComponent (typeof(AIPlayerInput), typeof(JumpFloorDetector))]
public class AIPlayerLogic : MonoBehaviour
{
    private AIPlayerInput _input;
    private JumpFloorDetector _fieldOfView;
    private PlayerSearchDetector _fieldOfView2; 

    [Header("AI Settings")]
    [SerializeField] private float _attackRange = 1.0f;
    [SerializeField] private float _jumpThresholdY = 0.5f; // 대상이 이 높이보다 위에 있으면 점프
    [SerializeField] private float _decisionInterval = 0.1f; // 판단 주기
    private void Awake()
    {
        _input = GetComponent<AIPlayerInput>();
        _fieldOfView = GetComponent<JumpFloorDetector>();
    }

    private void Start()
    {
        //StartCoroutine(AILogicRoutine());
        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator AILogicRoutine()
    {
        // 초기 안정화 시간
        yield return new WaitForSeconds(1f);

        while (true)
        {
            if (_fieldOfView2.IsTargetInView())
            {
                // 1. 플레이어를 인식한 경우
                yield return StartCoroutine(ChaseAndAttackRoutine());
            }
            else
            {
                // 3. 플레이어를 인식하지 못한 경우 (Idle -> 정찰)
                yield return StartCoroutine(PatrolRoutine());
            }

            yield return new WaitForSeconds(_decisionInterval);
        }
    }

    // --- [1. 추적 및 공격 로직] ---
    private IEnumerator ChaseAndAttackRoutine()
    {
        Transform target = _fieldOfView.currentTarget;
        if (target == null) yield break;

        float dist = Vector2.Distance(transform.position, target.position);
        Vector2 dir = (target.position - transform.position).normalized;
        // 1-1. 공격 범위 체크
        if (dist <= _attackRange)
        {
            _input.Move(Vector2.zero); // 공격 시 정지
            int attackType = (dist > _attackRange * 0.5f) ? 2 : 1; // 거리에 따라 다른 공격 선택 (예시)
            _input.Attack(attackType);
            yield return new WaitForSeconds(0.1f);
            _input.Attack(0); // 공격 명령 후 초기화

            float attackDelay = Random.Range(0f, 1.5f); // 공격 후 대기 시간 (랜덤화)
            // 반대방향 이동
            yield return StartCoroutine(MoveAction(attackDelay , new Vector2(-dir.x, 0)));
        }
        else
        {
            // 1-2. 점프 조건 체크 (y축 차이)
            if (target.position.y > transform.position.y + _jumpThresholdY)
            {
                Transform nextGround = _fieldOfView.GetClosedGround();
                if (nextGround != null) 
                    yield return StartCoroutine(JumpAction(nextGround)); // 점프 후 착지까지 이동 유지
                else
                {
                    _input.Move(new Vector2(dir.x, 0));
                    yield return null;
                }    
            }
            else
            {
                // 1-3. 단순 추적 이동
                _input.Move(new Vector2(dir.x, 0));
                yield return null; 
            }
        }
    }
    private IEnumerator MoveAction(float duration , Vector2 dir)
    {
       while (duration > 0.0f)
       {
            duration -= Time.deltaTime;
            _input.Move(dir);
            yield return null; 
       }
    }

    // --- [3. 정찰 및 탐색 로직] ---
    private IEnumerator PatrolRoutine()
    {
        // 3-1. 일정 기간 Idle (멍 때리기)
        _input.Move(Vector2.zero);
        float idleTime = Random.Range(1f, 2f);
        while (idleTime > 0)
        {
            //if (_fieldOfView.IsTargetInView()) yield break; // 도중 발견 시 즉시 탈출
            idleTime -= Time.deltaTime;
            yield return null;
        }

        // 3-2. 방향 전환 후 이동 또는 가까운 지면 탐색 점프
        Transform nextGround = _fieldOfView.GetClosedGround();

        if (nextGround != null)
        {
            // 가까운 땅이 있다면 그 방향으로 점프 탐색
            yield return StartCoroutine(JumpAction(nextGround));
        }
        else
        {
            // 그냥 반대 방향으로 정찰 이동
            float patrolDir = transform.localScale.x > 0 ? -1f : 1f;
            float walkTime = Random.Range(0.3f, 3f);

            while (walkTime > 0)
            {
                //if (_fieldOfView.IsTargetInView()) yield break;
                _input.Move(new Vector2(patrolDir, 0));
                walkTime -= Time.deltaTime;
                yield return null;
            }
        }

        _input.Move(Vector2.zero);
        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator JumpAction(Transform ground)
    {
        // 가까운 땅이 있다면 그 방향으로 점프 탐색
        Vector2 dirToGround = (ground.position - transform.position).normalized;
        _input.Jump(true);
        yield return null;
        _input.Jump(false);

        while (ground != null)
        {
            _input.Move(new Vector2(dirToGround.x > 0 ? 1 : -1, 0));

            if(Mathf.Abs(transform.position.x - ground.position.x) < 0.2f)
            {
                yield break; 
            }
            yield return null; 
        }
    }
}
