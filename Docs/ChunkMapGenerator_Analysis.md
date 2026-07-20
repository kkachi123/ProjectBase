# 청크 기반 랜덤 맵 생성 — 스크립트 및 구현 정리

> `chunk_system_spec_v3.md`의 규칙을 실제로 구현한 에디터 도구 세트. "이미 만들어진 청크 프리팹들을 이어붙이는 조립기"와 "청크 하나의 내부 지형·콘텐츠를 처음부터 자동으로 만드는 생성기"두 가지가 하나의 창(`Map/Generate Random Map`)에 탭으로 통합되어 있다.

## 1. 스크립트 목록

| 파일 | 위치 | 역할 |
|---|---|---|
| `ChunkData.cs` | `Assets/Scripts/Map/` | 청크 프리팹에 붙는 컴포넌트. `ChunkType` 열거형, entrance1/2, 코인/몬스터/아이템/함정 좌표 목록을 들고 있고, Scene 뷰에 Gizmo로 그려준다. |
| `ChunkDatabaseEntry.cs` | `Assets/Scripts/Map/` | `ChunkDifficulty` 열거형과, 데이터베이스에 캐싱되는 청크 1개 분량의 요약 정보(프리팹 참조, 타입/난이도, entrance 좌표, 폭/높이). |
| `ChunkDatabase.cs` | `Assets/Scripts/Map/` | `ChunkDatabaseEntry`들을 모아 담는 `ScriptableObject`. 타입/난이도로 조회하는 함수 제공. |
| `ChunkDatabaseBuilder.cs` | `Assets/Scripts/Editor/` | `Assets/Prefabs/Map/ChunkMap/` 아래 프리팹을 전부 스캔해서 `ChunkDatabase`를 재생성하는 에디터 유틸(`Map/Rebuild Chunk Database` 메뉴). 난이도는 폴더명(Easy/Medium/Hard)에서 추론. |
| `ChunkReachability.cs` | `Assets/Scripts/Editor/` | 스펙 5장(세그먼트 추출·점프 가능 판정·BFS 도달가능성)을 그대로 C#으로 포팅한 순수 로직 모듈. 생성기와 무관하게 재사용 가능. |
| `ChunkAutoGenerator.cs` | `Assets/Scripts/Editor/` | 청크 하나의 내부 지형·콘텐츠를 실제로 만들어내는 핵심 생성기. |
| `ChunkMapGeneratorWindow.cs` | `Assets/Scripts/Editor/` | `Map/Generate Random Map` 메뉴로 여는 에디터 창. "Assemble Map"(기존 청크 조립) / "Generate Chunk"(신규 청크 생성) 두 탭 제공. |

---

## 2. 데이터 구조

### ChunkType / ChunkDifficulty

```csharp
public enum ChunkType { Transition, Combat, Puzzle, Treasure }
public enum ChunkDifficulty { None, Easy, Medium, Hard } // None = Transition용
```

### ChunkData (프리팹에 붙는 컴포넌트)

- `chunkType`
- `entrance1`, `entrance2` — "입구/출구"라는 고정 역할이 아니라 그냥 "연결 가능한 두 지점". 어느 쪽이든 이전/다음 청크에 이어붙일 수 있다.
- `coins`, `monsters`, `items`, `arrowTraps`, `spikeTraps` — 전부 청크 로컬 좌표(`Vector2Int`) 목록.

### ChunkDatabase / ChunkDatabaseEntry

`ChunkDatabaseBuilder.Rebuild()`가 프리팹들을 스캔해서 만드는 캐시. entrance 좌표·폭·높이를 미리 담아두기 때문에 조립기가 프리팹을 실제로 로드하지 않고도 후보를 빠르게 필터링할 수 있다.

---

## 3. 모드 A — Assemble Map (기존 청크 조립)

`ChunkMapGeneratorWindow.Generate()` / `TryConnect()`가 담당.

- `ChunkDatabase`에서 원하는 타입/난이도의 청크 후보들을 무작위 순서로 시도.
- 매 스텝, 후보 청크의 entrance1과 entrance2 중 어느 쪽을 현재 커서에 붙일지 두 가지 방식을 다 시도해서, **겹치지 않는 배치**가 나오는 쪽을 채택.
- 필요한 조건은 딱 두 가지뿐:
  1. 타일(Rect)이 기존에 배치된 것들과 겹치지 않을 것
  2. 실제로 연결점이 이어질 것 (좌표가 정확히 맞물림)
- 방향 플래그나 진입 높이 필터는 없음 — entrance 좌표 자체가 이미 "여기서 이어질 수 있다"는 정보를 담고 있기 때문.
- 짝수 스텝은 항상 `ChunkType.Transition`, 홀수 스텝은 Combat/Puzzle/Treasure 중 무작위 — 통로와 콘텐츠 방을 번갈아 배치.

---

## 4. 모드 B — Generate Chunk (청크 자동 생성)

`ChunkAutoGenerator.GeneratePreview()`가 담당하는 전체 파이프라인. 결과는 씬에 미리보기로 생성되고, 확인 후 `Save As Prefab` 버튼으로 `Assets/Prefabs/Map/ChunkMap/<Type>/<Difficulty>/`에 저장 + `ChunkDatabaseBuilder.Rebuild()` 자동 호출.

### 4.1 Entrance 8지점 시스템

좌표를 직접 입력하는 대신, 청크 4변(North/South/East/West) × 2지점 = 8개의 고정 지점 중에서 고른다(`ChunkEntrancePoint` 열거형, `ChunkAutoGenerator.ResolveEntrancePoint()`).

실제 기존 프리팹들의 entrance 좌표를 실측해서 정한 값:

| 지점 | 좌표 | 비고 |
|---|---|---|
| West1 / West2 | (0, 3) / (0, 7) | 왼쪽 변, 낮음/높음 |
| East1 / East2 | (width, 3) / (width, 7) | 오른쪽 변, 낮음/높음 |
| North1 / North2 | (모서리에서 폭의 10%, height) | 위쪽 변 |
| South1 / South2 | (모서리에서 폭의 10%, 0) | 아래쪽 변 |

entrance1과 entrance2로 **같은 지점**을 고르면 문이 하나뿐인 "구석진 방"으로 생성된다(4.5절 참고).

### 4.2 entrance 좌표 규칙 (실측으로 확인한 핵심 사실)

- **West / East / North**: entrance.y는 실제 발판 표면이 아니라 **"표면 + 1"**로 기록되어 있다(기존 프리팹 다수 실측 결과 전부 정확히 +1 차이 — 플레이어 스폰 트랜스폼의 피벗이 발밑이 아니라 몸통 중앙쯤인 것으로 추정). 그래서 생성기는 항상 `SurfaceHeight(entrance) = entrance.y - 1`을 실제 발판 높이로 쓴다.
- **South**: 실측 결과(`GoUp`/`GoDown` 프리팹) 해당 좌표 근처에 실제 타일이 전혀 없었다 — South는 "서 있는 지점"이 아니라 **떨어져서 통과하는 지점**이다. 그래서 South는 `ChunkSegment.isVirtual = true`인 가상 세그먼트로만 다뤄지고, 실제 타일을 칠하지 않는다. 다만 남쪽 이웃 청크의 North(실제 발판, 표면+1 규칙)와 대칭이 맞아야 두 청크를 이어붙였을 때 실제 필요한 점프 높이가 `maxRiseY`를 넘지 않으므로, South의 가상 앵커에도 동일하게 `entrance.y - 1`을 적용한다.

### 4.3 메인 경로 생성 (Dijkstra)

1. `BuildCandidateLayers()` — entrance1과 entrance2 사이를 x/높이 모두 선형보간(Lerp)한 "레이어드 후보 그래프"를 만든다. 레이어 수는 `가로 거리 / maxJumpX`와 `높이 차 / maxRiseY` 중 큰 쪽으로 결정해서, 순수 선형보간만 따라가도 항상 점프 제약을 만족하도록 보장한다.
2. `FindMainPathDijkstra()` — 인접한 레이어끼리만 간선을 만들되, 간선 하나마다 `ChunkReachability.CanJump`(가로/상승 제약)와 `HasClearance`(플레이어 세로 크기만큼의 머리 공간)를 모두 통과해야 한다. 간선 가중치는 시드 기반 난수라서, 시드를 바꾸면 매번 다른 경로가 나온다.
3. 후보 그래프에서 경로를 못 찾는 극단적인 경우, `BuildFallbackLinearPath()`가 순수 선형보간 후보만 이어붙인 경로로 대체한다(수학적으로 항상 유효함이 보장됨).

### 4.4 보너스 발판 (`GenerateBonusSegments`)

메인 경로 주변에 코인/몬스터/보물용 발판을 무작위로 추가한다. 새 발판은 다음을 모두 만족해야 채택된다.

- 기존 발판 중 최소 하나와 `CanJump` 관계가 성립할 것 (스펙 6장의 "보너스 콘텐츠는 최소 한쪽에서 도달 가능해야 함")
- `IsBlocked()` 검사 통과: 같은 높이 발판과는 최소 2타일 간격, 겹치는 x범위에서는 `HasClearance`로 머리 공간 확보

### 4.5 구석진 방 (단일 entrance)

entrance1과 entrance2로 같은 지점을 고르면 문이 하나뿐인 방으로 취급된다.

- 두 지점을 잇는 Dijkstra 경로 대신, 그 한 지점을 "허브"로 삼아 보너스 발판들을 붙여나간다.
- 검증도 "entrance1↔entrance2 양방향 도달"이 아니라 "그 허브 하나에서 모든 발판에 도달 가능한가"로 바뀐다.
- 콘텐츠용 발판을 조금 더 넉넉하게(2~4개 → 3~5개) 생성한다.

### 4.6 최종 검증 + 수선 (`VerifyAndRepair`, 스펙 5장)

계획 단계 세그먼트가 아니라, **실제로 구워진 Tilemap을 다시 읽어서** 검증한다.

1. `ChunkReachability.ExtractSegments()`로 실제 타일에서 세그먼트를 다시 추출.
2. entrance1/entrance2(또는 구석진 방이면 단일 entrance)가 어느 세그먼트 위에 있는지 찾는다(`ResolveEntranceIndex`) — South처럼 타일이 없는 entrance는 가상 세그먼트를 목록 끝에 추가해서 같은 방식으로 다룬다.
3. BFS(`ReachableFrom`)로 양방향(또는 단일 방향) 도달가능성을 확인.
4. 실패 시, 도달 가능한 쪽과 그렇지 못한 쪽 사이에서 가장 가까운 세그먼트 쌍을 찾아(`TryFindBridge`) 그 사이에 높이차를 쪼개는 중간 발판을 추가하고 1회 재검증(스펙 5.3의 수선 전략).
5. 그래도 실패하면 콘솔에 경고 로그만 남기고 프리뷰는 유지 — 디자이너가 수동으로 확인.

### 4.7 콘텐츠 배치

검증까지 끝난 **최종 결과물**에서 다시 한번 실제 도달가능 세그먼트를 뽑아(`ComputeReachableContentSegments`), entrance 발판 자체와 South 같은 가상 세그먼트를 제외한 뒤에만 코인/몬스터/아이템을 배치한다(`PlaceContent`). 계획 단계 목록을 그대로 믿지 않고 항상 최종 결과 기준으로 필터링하기 때문에, 플레이어가 못 가는 자리에 몬스터가 놓이는 일이 없다.

| 청크 타입 | 배치 규칙 |
|---|---|
| Combat | 코인 0~3개, 몬스터는 Easy 1개 / Medium 1~3개 / Hard 1~5개(가장자리 1타일 여백) |
| Puzzle | 코인 0~3개 |
| Treasure | 코인 0~2개 + 아이템 1개 |
| Transition | 콘텐츠 없음(경로만) |

### 4.8 저장

`SaveAsPrefab()`이 `Assets/Prefabs/Map/ChunkMap/<Type>/<Difficulty>/`(Transition은 `.../ChunkMap/Transitions/`) 아래에 프리팹으로 저장하고, 곧바로 `ChunkDatabaseBuilder.Rebuild()`를 호출해 데이터베이스에 자동 등록한다.

---

## 5. 핵심 알고리즘 모듈 — ChunkReachability.cs

스펙 5장을 그대로 옮긴 순수 함수 모음. 생성기뿐 아니라 향후 "기존 청크 재검사" 도구에도 재사용 가능하도록 독립시켰다.

- **`ExtractSegments(tiles)`**: 같은 x열에서 "타일이 있고 바로 위 칸이 빈" y를 그 열의 표면으로 보고, 인접한 열끼리 표면 높이가 같으면 하나의 세그먼트로 묶는다.
- **`SegGap(a, b)`**: 두 세그먼트 사이의 가로 간격(겹치면 0).
- **`CanJump(a, b, maxJumpX, maxRiseY)`**: 핵심 공식 — `가로간격 <= maxJumpX` 그리고 `b높이 - a높이 <= maxRiseY`. 내려가는 경우는 값이 음수라 항상 통과(하강 무제한).
- **`HasClearance(a, b, playerHeight)`**: x범위가 겹치는 두 세그먼트는 높이차가 `playerHeight` 이상이어야 함(그렇지 않으면 위 발판이 아래 발판의 천장이 되어 플레이어가 끼임).
- **`FindSegmentAt(segments, point)`**: 좌표가 어느 세그먼트 위에 있는지 탐색. entrance 좌표는 타일 인덱스가 아니라 "이음매(seam)" 좌표라서(예: 20폭 청크의 entrance2.x=20, 세그먼트는 xEnd=19), 오른쪽 경계는 `xEnd + 1`까지 포함해서 비교한다.
- **`ReachableFrom(segments, startIdx, maxJumpX, maxRiseY, playerHeight)`**: `CanJump` + `HasClearance`를 만족하는 간선만 따라가는 BFS. 도달 가능한 모든 세그먼트 인덱스 집합을 반환.

---

## 6. 알려진 이슈 / 보류 중인 사항

- **청크 간 교차 검증**: 개별 청크는 자기 자신의 entrance 사이 도달가능성만 검증한다. 두 청크를 조립했을 때 이음매를 낀 "합산 점프 거리"가 실제로 maxRiseY 이내인지는 South/North 오프셋을 맞춰서 간접적으로 보장하지만, 별도의 교차-청크 검증 단계는 아직 없다.
- **안전 바닥(Safety Floor) — 보류**: 점프를 실패했을 때 발밑에 아무것도 없으면(특히 메인 경로가 청크 상단에 몰려 있는 경우) 청크 바닥까지 떨어지고, 거기서는 가장 가까운 발판까지 rise가 maxRiseY를 넘어 복귀가 불가능해질 수 있다. 청크 하단 전체를 덮는 낮은 "안전 바닥"(South 출구 자리는 구멍을 남김)을 추가하는 방안을 논의했으나, 모든 청크에 강제 적용 시 "Aerial"류 의도적으로 위험한 청크의 설계 의도를 해칠 수 있어 **현재는 미구현 상태로 보류**.
- **Hard 난이도 11종**: 스펙 문서 기준 설계만 있고 아직 Unity 프리팹으로 만들어지지 않음.
- **Transition 타입의 존재 이유**: 8지점 entrance + 위 이슈들이 전부 해결되면, Combat/Puzzle/Treasure 청크도 얼마든지 서로 다른 높이·방향을 연결하는 통로 역할을 겸할 수 있어 전용 Transition 타입이 불필요해질 가능성이 있음 — 다만 `ChunkDatabase`/Assemble Map(홀짝 스텝으로 Transition을 강제 교차 배치)과 기존 9개 Transition 프리팹의 역할에 영향을 주는 더 큰 구조 변경이라 별도 논의 필요.

---

## 7. 사용 방법

1. Unity 메뉴 `Map/Generate Random Map` → **Generate Chunk** 탭.
2. Chunk Type / Difficulty 선택.
3. Entrance 1 / Entrance 2를 8지점 중에서 선택(같은 지점을 고르면 구석진 방).
4. Player Limit(Max Jump X, Max Rise Y, Player Width/Height) 확인 — 기본값은 스펙 문서 기준.
5. Ground Tile, 시드 확인 후 **Generate** — 씬에 프리뷰 생성, 콘솔에서 검증 로그(통과/수선/실패) 확인.
6. 마음에 들 때까지 시드를 바꿔가며 재생성.
7. **Save As Prefab**으로 이름 지정 후 저장 — 폴더 자동 생성 + `ChunkDatabase` 자동 갱신.
8. **Assemble Map** 탭에서 저장된 청크들을 포함해 전체 맵 조립 테스트.
