# Project RE Play 회귀 검증 기록

작성일: 2026-10-22

포트폴리오의 「08 · 검증 범위」에서 개발 작업용 기록을 분리했다. 아래 기존 기록은 이전 문서에 적혀 있던 상태를 옮긴 것이며, 이번 문서 작성으로 새 Play 검증을 완료했다는 의미는 아니다.

## 이전 문서에 남아 있던 확인 기록

- 최신 FSM·공격 SO 구현 및 설정 이전
- 컴파일
- 구형 참조·Missing Script 검사

## Play 확인 대기

- [ ] 최신 구현을 기준으로 전체 Play 회귀 검증
- [ ] Player 콤보 연결·Stamina 소비·피격 중 공격 중단
- [ ] Monster 일반·배후 공격
- [ ] Monster 대상 재인식·복귀
- [ ] 같은 원본 공격 SO를 사용하는 개체들의 실행 상태·구독·정리 수명 독립성

## InventoryScreen 참조 캐싱 변경 확인

InventorySystem은 TestPlayerManager가 생성해 수명 동안 보관한다. InventoryScreen은 최초 표시 시 EnsureInit에서 해당 참조를 저장하고, 메뉴를 닫아도 참조를 유지한다. 이벤트는 OnShow에서 구독하고 OnHide에서 해제한다.

- [ ] 최초 메뉴 표시에서 슬롯 초기화와 현재 목록 렌더링
- [ ] 메뉴를 닫은 상태에서 아이템 변경 후 다시 열 때 현재 목록 반영
- [ ] 메뉴·탭을 반복 전환했을 때 갱신과 구독 해제가 정상적으로 대응
- [ ] Player 재생성 후 같은 Manager의 인벤토리 목록을 정상 표시

## 실행 기록 양식

| 실행일 | 소스 커밋·추가 변경 | Scene·조건 | 검증 항목 | 결과 | 로그·재현 정보 |
|---|---|---|---|---|---|
| 미실행 | — | — | — | 확인 대기 | — |

## 이번 코드 변경의 정적 확인

- 커밋: `e0d86d1e8f0d72277d48e5d6bb65bc03a3ea9ab9` — InventoryScreen 참조 초기화 위치 변경 및 OnHide의 참조 초기화 제거. 코드 파일 하나만 커밋했다.
- `git diff --check` 통과.
- Unity 6000.6.0f1의 Roslyn과 기존 Editor용 Assembly-CSharp 응답 파일을 사용해 현재 작업 트리의 C# 컴파일을 확인했다. 출력은 `Temp/QA/InventoryScreenCompilation/`으로 분리했으며 컴파일 오류는 없었다. 다른 코드·외부 에셋의 경고는 남아 있다.
- 이 확인은 Unity Play 실행·Player 빌드·IL 후처리 검증을 대신하지 않는다. 위 Play 확인 항목은 대기 상태를 유지한다.
