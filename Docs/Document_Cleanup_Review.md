# 문서 정리 검토

기준: 2026-11-08 문서 목록·주요 참조와 현재 소스 조사. 이 문서는 정리 후보와 판단 근거이며, 후보로 분류된 문서를 자동으로 삭제하라는 지시가 아니다. Map 문서는 다른 작업이 진행 중이므로 현재 변경과 최신 상태를 먼저 확인한다.

## 정리 기준

- **유지:** 현재 작업·검증·포트폴리오의 기준이거나 다른 문서가 사용하는 자료.
- **현행화:** 현재 코드나 검증 상태와 다를 수 있어 소스·에셋과 대조가 필요한 자료.
- **통합 후 삭제 후보:** 현재 Guide·Progress·Log에 역할이 흡수될 수 있으나 고유한 결정·미검증·참조를 먼저 옮겨야 하는 자료.

삭제 전에는 문서 내용과 현재 구현·검증 상태를 대조하고, 남은 작업과 이력을 보존하며 전체 참조·상대 링크를 검색한다. PDF·이미지는 원본·제출 자료 여부와 참조를 별도 확인한다. 개별 작업 Plan은 완료 후 필요한 결과를 Log·Guide에 남기고 정리한다. 실제 삭제 전 대상과 범위는 해당 작업 지시를 따른다.

## 유지

| 문서·자료 | 근거 |
| --- | --- |
| [공통 지침](Skill/AGENTS.md) · [Plan Guide](Skill/Plan_Guide.md) · [AI 협업 개발 절차](AI_Assisted_Development_Workflow.md) | 작업 지침, 계획 작성과 공통 절차의 기준. |
| [FSM Progress](FSM/FSM_Progress.md) · [FSM Guide](FSM/ProjectSkill/) · [FSM 로그](FSM/Refactoring_Log/FSM_Refactoring_Source_Log.md) | 현재 진행 상태, 반복 작업 절차와 리팩토링·검증 이력. |
| [Map Progress](Map/Map_Progress.md) · [Map 코드 Guide](Map/Map_Code_Architecture_Guide.md) · [Map 로그](Map/Map_Refactoring_Log.md) · [Map 재설계 계획](Map/Spelunky_Chunk_Map_Plan.md) | 전체 맵 목표·구조·진행 상태의 연결된 기준. 현재 Map 작업 변경을 보존한다. |
| [Map Assembly](Map/Map_Assembly/) · [Chunk Generation](Map/Chunk_Generation/) 문서 | 조합·방 내부 생성의 세부 구현, 제작 규격, 진행 상태와 미검증 항목. 하위 Images에도 문서에서 참조하는 검증 화면이 있다. |
| [Play 회귀 체크리스트](QA/ProjectRE_Play_Regression_Checklist.md) | FSM·Inventory 등 미완료 Play 검증과 이전 정적 확인 기록. |
| [WOODS 타일 분류](BackGround/WOODS_Tile_Grouping_Report.md) · [RuleTile 규칙 기록](BackGround/WOODS_Ground_RuleTile_Draft.md) | 타일 분류와 생성된 RuleTile의 규칙·미검증 경계/코너 기록. |
| [Boss FSM 조사 기록](FSM/Hollow_Knight_Boss_Research/Research_Notes.md)과 같은 폴더의 PDF | 출처·관찰 범위가 명시된 별도 조사 자료. 현행 프로젝트 구현이나 모든 보스의 구조로 단정하지 않는다. |
| [포트폴리오 슬라이드 구성](FSM/Agent_Seperation/Portfolio_Slides_16_17_Outline.md) | 제출용 구성과 Notion 연결·제출 전 확인 항목. 실제 제출본과의 차이는 별도 확인한다. |

## 현행화 후보

| 문서·자료 | 확인할 내용 |
| --- | --- |
| [프로젝트 스크립트 구조](Adventure_Sample/project_script_structure_for_ai.md) | 작성 당시 GridChunk 생성 흐름을 기술한다. 현재 MapManager는 RoomMapFactory를 사용하므로 최신 코드와 전반적으로 대조하거나 과거 자료임을 표시한다. |
| [Adventure Sample PDF](Adventure_Sample/) | 동일 주제의 PDF 2개는 당시 산출물이다. 문서·현재 코드와 내용 및 제출 용도를 확인한 뒤 갱신 또는 이력 보관을 결정한다. |
| [FSM PDF](FSM/Refactoring_Log/) | Change Log·Portfolio PDF는 별도 산출물이다. 최신 로그와 실제 제출본을 대조하고 갱신 또는 이력 보관을 결정한다. 생성 시점만으로 현행 여부를 확정하지 않는다. |
| [FSM Agent 제작 Guide](FSM/ProjectSkill/FSM_Agent_Creation_Guide.md) | 공용 Monster 에셋 경로 등 최근 폴더 정리 이후의 설명을 실제 에셋과 대조한다. |
| [Combo 리팩토링 요약](FSM/Combo_Attack/Player_Combo_Attack_Refactoring_Summary.md) | 현재 공격 SO 구조와 과거 입력·State 책임 설명을 구분해 유지한다. |

## 통합 후 삭제 후보

| 문서·자료 | 선행 확인 |
| --- | --- |
| [Combo Plan1~6](FSM/Combo_Attack/) | 현재 구조와 다른 중간 설계가 포함되고 수동 검증 체크가 남아 있다. 각 미검증 항목이 현재에도 유효한지 확인해 Progress·Log로 옮기고 요약의 링크를 정리한 뒤 삭제 여부를 결정한다. |
| [WOODS 분류·규칙 기록](BackGround/) | 사용 중인 WOODS 에셋, 남은 검증과 관련 문서의 참조를 확인한다. 관련 지형 작업이 끝나고 핵심 결과가 다른 유지 문서로 옮겨진 경우에만 정리한다. |

## 삭제 전환 기록

- Combo 종료 Clip 계획과 공격 SO 전환 계획의 구현·검증 이력은 FSM 로그에, 남은 Play·Editor 확인은 FSM Progress에 정리했다.
- WOODS 작업 체크리스트의 미검증 항목은 RuleTile 규칙 기록에 정리했다. 분류 보고서와 규칙 기록은 유지한다.

위 세 문서 이외에는 이번 정리에서 삭제하지 않았다. 후보 분류는 향후 소스·문서 상태가 바뀌면 다시 확인한다.
