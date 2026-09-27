# WOODS Tile RuleTile 제작 체크리스트

## 목적

`Assets/@tempAssets/karsiori/TileMap/Tiles/`의 `Tilesheet - WOODS` 타일을 용도별로 분류하고, 승인된 지형 그룹만 RuleTile로 제작하여 `Assets/Prefabs/BackGround/Floor/`에서 사용할 수 있게 한다.

> 진행 원칙: 각 단계의 결과를 검토·승인한 뒤 다음 단계로 넘어간다. 분류 단계에서는 원본 Tile, Sprite, Palette, Tilemap을 변경하지 않는다.

## 사전 확인

- [x] Unity Editor가 프로젝트에 연결된 상태인지 확인한다.
- [x] `com.unity.2d.tilemap` 패키지가 설치되어 있는지 확인한다. (`1.0.0`)
- [x] `com.unity.2d.tilemap.extras` 패키지가 설치되어 있고 `RuleTile`을 사용할 수 있는지 확인한다. (`9.0.0`)
- [x] 대상은 `Tilesheet - WOODS.png` 및 이에 연결된 `WOODS_0`~`WOODS_87` Tile 에셋임을 확인한다.
- [x] 생성 결과물의 저장 위치와 이름 규칙을 확정한다. `Assets/Prefabs/BackGround/Floor/RuleTiles/WOODS_<Group>.asset`

---

## 1단계 — Tile 파일 파악 및 그룹 후보 작성

### 수행 내용

- [x] 원본 Sprite sheet와 각 Tile 에셋이 참조하는 Sprite를 읽기 전용으로 조사한다.
- [x] 각 타일의 시각적 역할을 기록한다: 중앙, 상/하/좌/우 경계, 외곽 코너, 안쪽 코너, 전이 타일, 장식, 미확정.
- [x] 인접 타일과 자연스럽게 이어지는 후보를 하나의 지형 그룹으로 묶는다.
- [x] 장식물, 단독 오브젝트, 방향성이 강한 오브젝트는 RuleTile 후보에서 분리한다.
- [x] 색 또는 형태만으로 판별하기 어려운 타일은 `미확정`으로 남긴다.

### 산출물

- [x] 타일 번호별 역할표 — `WOODS_Tile_Grouping_Report.md` 참조
- [x] RuleTile 후보 그룹표 — `WOODS_Tile_Grouping_Report.md` 참조
- [x] 제외/미확정 타일 목록 — `WOODS_Tile_Grouping_Report.md` 참조
- [x] 각 그룹의 신뢰도(높음/보통/낮음) — `WOODS_Tile_Grouping_Report.md` 참조

### 다음 단계 진행 조건

- [x] 그룹표를 검토하고, RuleTile로 만들 그룹을 승인한다.

---

## 2단계 — 그룹 분류 기준 설명 및 규칙 초안 검토

### 분류 기준

- [x] **시각적 연속성:** 인접했을 때 색, 질감, 윤곽이 이어지는가?
- [x] **3×3 위치 역할:** 중앙·변·외곽 코너·안쪽 코너 중 어느 위치용 Sprite인가?
- [x] **방향성:** 회전 또는 대칭으로 재사용 가능한가, 특정 방향에만 맞는가?
- [x] **용도 분리:** 바닥 지형인지, 경계/벽인지, 장식인지 구분되는가?
- [x] **동일 지형성:** 같은 RuleTile을 칠한 인접 셀끼리 연결되어도 자연스러운가?

### RuleTile 규칙화 방식

- [x] 각 후보 Sprite를 3×3 패턴으로 분석한다.
- [x] 중심을 제외한 여덟 방향에서 연결되어야 하는 위치를 `This` 조건으로 설정한다.
- [x] `This` 조건이 많은 구체적인 규칙을 먼저, 기본 타일을 마지막에 배치한다.
- [x] 중복 규칙은 제거하고, 어떤 규칙에도 맞지 않을 때 사용할 기본 Sprite를 지정한다.
- [x] 분석 결과가 지형 의도와 다르면 자동 결과를 그대로 사용하지 않고 규칙을 보정한다.

### 검토 항목

- [x] 한 그룹이 하나의 연속 지형이라는 판단이 맞는지 확인한다.
- [x] 장식 Sprite가 RuleTile에 포함되지 않았는지 확인한다.
- [x] 경계 타일이 반대 방향 또는 잘못된 코너에 배치되지 않는지 확인한다.
- [x] 자동 분석 결과 중 수동 보정할 규칙을 확정한다.

### 다음 단계 진행 조건

- [x] 그룹별 RuleTile 이름, 포함 Sprite, 기본 Sprite, 보정 규칙을 승인한다.

---

## 3단계 — Floor 경로에 RuleTile 제작 및 검증

### 생성

- [x] `Assets/Prefabs/BackGround/Floor/` 아래에 `RuleTiles/` 폴더를 만든다.
- [x] 승인된 그룹마다 별도의 RuleTile 에셋을 만든다. (`WOODS_Ground.asset`)
- [x] 각 RuleTile에 Sprite와 정렬된 인접 규칙을 적용한다. (대표 규칙 9개, `This`/`NotThis` 명시)
- [x] 기존 원본 Tile 에셋은 수정하거나 삭제하지 않는다.

### Palette 및 Tilemap 검증

- [x] RuleTile을 Tile Palette에 추가한다. (`Rectangular Palette.prefab`의 `Layer1`, 셀 `(-3, -8, 0)`)
- [ ] 테스트용 Tilemap에 단일 셀, 직선, 사각형, 오목/볼록 코너, 좁은 통로 형태로 칠한다. (임시 Tilemap에서 내부 타일 1건만 검증 완료)
- [ ] 모든 경계 및 코너가 의도한 Sprite로 자동 전환되는지 확인한다. (내부 `WOODS_1`은 검증됨; 나머지는 Pipeline 연결 불안정으로 보류)
- [ ] 문제가 있는 규칙만 수정하고 같은 테스트 케이스를 다시 확인한다.

### 완료 조건

- [ ] 승인된 모든 그룹이 독립 RuleTile 에셋으로 생성되었다.
- [ ] 기본·변·코너·내부 타일이 테스트 Tilemap에서 올바르게 전환된다.
- [ ] 결과 RuleTile의 경로와 포함 타일 목록을 보고한다.

## 결정 기록

| 항목 | 결정 | 비고 |
| --- | --- | --- |
| 첫 번째 분석 대상 그룹 | 미정 | 1단계 결과 후 확정 |
| RuleTile 저장 폴더 | `Assets/Prefabs/BackGround/Floor/RuleTiles/` | 필요 시 변경 |
| 자동 분석 외 수동 보정 | 미정 | 2단계 검토 후 확정 |
| 원본 Tile 에셋 변경 | 하지 않음 | 원본 보존 |
