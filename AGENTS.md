# ProjectBase 작업 지침

작업 전에 [공통 지침](Docs/Skill/AGENTS.md)을 확인한다. Plan 또는 Markdown 작성·갱신은 [Plan Guide](Docs/Skill/Plan_Guide.md)를 따른다. 요구사항·기존 사용자 변경·현재 소스와 에셋을 먼저 확인하고, 작업에 필요한 문서만 읽는다.

## 분야별 확인

- **FSM·Agent·공격:** [FSM 진행 현황](Docs/FSM/FSM_Progress.md)과 작업에 맞는 [State 확장](Docs/FSM/ProjectSkill/FSM_State_Extension_Guide.md), [Agent 제작](Docs/FSM/ProjectSkill/FSM_Agent_Creation_Guide.md), [지상 Monster 제작](Docs/FSM/ProjectSkill/FSM_Grounded_Monster_Creation_Guide.md), [Player 공격 확장](Docs/FSM/ProjectSkill/Player_Attack_Extension_Guide.md) Guide를 확인한다. 변경 이력은 [FSM 로그](Docs/FSM/Refactoring_Log/FSM_Refactoring_Source_Log.md)를 참고한다.
- **Map·방·Chunk:** [Map 진행 현황](Docs/Map/Map_Progress.md)과 [코드 구조](Docs/Map/Map_Code_Architecture_Guide.md)를 확인하고, 대상에 따라 Map Assembly 또는 Chunk Generation의 세부 문서를 읽는다. Chunk Generation 문서는 [문서 안내](Docs/Map/Chunk_Generation/README.md)의 사용자용·AI 작업용 폴더 기준을 따른다.
- **UI:** [UI 진행 현황](Docs/UI/UI_Progress.md)과 `Assets/Scripts/Managers/UIManager/`의 실제 화면·관리 코드를 확인하고 연결된 Prefab·Scene과 해당 프로젝트 문서를 조사한다. 사용 가능한 Unity UI Skill은 작업에 맞게 적용한다.
- **디자인·Sprite·Tile:** 대상 원본과 프로젝트 에셋·설정, 관련 분야 Guide를 먼저 확인한다. 맵 그래픽은 [방 그래픽 제작 규칙](Docs/Map/Chunk_Generation/Work/Room_Art_Authoring_Guide.md), WOODS RuleTile은 [규칙 기록](Docs/BackGround/WOODS_Ground_RuleTile_Draft.md)을 참고한다. 사용 가능한 Sprite·Pixel 관련 Skill은 작업에 맞게 적용한다.

작업 순서와 검증·인수인계는 [AI 협업 개발 절차](Docs/AI_Assisted_Development_Workflow.md)를 참고한다. 문서는 현재 소스보다 우선하지 않으며, 프로젝트 문서와 지침이 충돌하면 현재 사용자 지시와 실제 구현을 확인한다.
