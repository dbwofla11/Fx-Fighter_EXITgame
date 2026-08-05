# 이슈 : 가격 차트 과거 스크롤 / 확대·축소(줌) 기능

작성일 : 2026-08-05

관련 구현 : `Assets/Scripts/UI/MainModal/PriceChartUI.cs`, `Assets/Scripts/UI/MainModal/PriceChartViewport.cs`(신규),
`Assets/Scripts/UI/MainModal/PriceChartCandles.cs`(신규), `Assets/Scripts/UI/MainModal/PriceChartGrid.cs`(신규),
`Assets/Scripts/UI/MainModal/PriceChartTooltip.cs`(신규), `Assets/Scripts/UI/MainModal/PriceChartMovingAverage.cs`,
`Assets/Scripts/UI/Vfx/UIBurstParticle.cs`

## 배경

기존 `PriceChartUI`는 항상 최신 `visibleCandleCount`개 캔들만 고정 스케일로 그려서, 지나간 구간을 다시 볼
방법이 없고 캔들 개수/기간을 바꿀 수도 없었음. 실거래 차트(토스증권 등)처럼 휠/드래그로 과거를 스크롤하고,
Ctrl+휠로 확대/축소하며, 계속 축소하면 주봉 → 월봉으로 자연스럽게 넘어가도록 개선.

## 구현 내역

1. **과거 스크롤(팬)** — 마우스 휠 또는 드래그로 `viewOffset`(꼬리에서 몇 캔들 뒤로 갔는지)을 변경.
   `OnScroll`/`OnBeginDrag`/`OnDrag`(`IScrollHandler`/`IBeginDragHandler`/`IDragHandler`)가 `PriceChartUI`에
   구현돼있고, 실제 상태 계산은 `PriceChartViewport.PanBy()`/`Drag()`가 담당. Y축 스케일은 원래 있던
   "현재 보이는 캔들 기준" 자동 계산 로직(`ComputePriceRange`)을 그대로 재사용해서, 스크롤된 구간의
   변동폭에 맞춰 자동으로 다시 스케일링됨 — 별도 로직 추가 없이 공짜로 해결됨.

2. **확대/축소(줌)** — `Ctrl+휠`로 화면에 보이는 캔들 개수(`ZoomCandleCount`)를
   `[minZoomCandleCount(6), visibleCandleCount(16, 풀 용량)]` 범위 안에서 조절. 캔들 오브젝트 풀은 그대로
   두고(재생성 없음) 매 `Redraw()`마다 활성화 개수·슬롯 폭만 바꾸는 방식이라 GC 부담이 없음.

3. **주봉 ↔ 월봉 자동 전환** — 줌이 캔들 개수 범위를 벗어나려는 순간(계속 축소 → 풀 용량 초과 / 계속 확대
   → 최소 개수 미만) `PriceChartViewport.PeriodDays`를 7일 ↔ 30일로 전환하고, 캔들 개수를 반대쪽 극단으로
   리셋. "계속 축소" 제스처 하나가 끊김 없이 주봉에서 월봉으로 이어지는 느낌을 줌.
   `AggregateHistory`(구 `AggregateWeekly`)와 `PriceChartMovingAverage`도 고정 7일 대신 이 `PeriodDays`를
   매 호출마다 받아 계산하도록 일반화함.

4. **모듈 분리** — 비대해진 `PriceChartUI.cs`(약 580줄)를 6개 파일로 분리:
   - `PriceChartViewport` — 스크롤/줌/기간(주·월) 상태 계산
   - `PriceChartCandles` — 캔들 오브젝트 풀, 호버 이벤트, 급등락 파티클, 엔딩 붕괴 애니메이션
   - `PriceChartGrid` — 배경 격자선 + 가격 라벨
   - `PriceChartTooltip` — 호버 툴팁
   - `PriceChartMovingAverage` — 이동평균선(기존)

   `PriceChartUI`는 이제 `MarketManager`/`EventHub` 이벤트를 받아 이 모듈들을 조립·중계만 하는
   오케스트레이터(약 250줄)로 축소됨. 코루틴이 필요한 `PriceChartCandles`만 `MonoBehaviour`가 아니라서,
   생성 시 `PriceChartUI`(`this`)를 "코루틴 실행기"로 받아 `runner.StartCoroutine()`으로 돌림.

## 동작 원리

### "매 프레임"이 아니라 "이벤트마다" 다시 그리는 구조

`PriceChartUI`엔 `Update()`가 없다. `EventHub.OnMarketUpdated`(턴 진행)와 휠/드래그 이벤트가 올 때만
`Redraw()`를 호출한다. `Redraw()`는 이전 프레임과 diff를 내지 않고 매번 처음부터 다시 계산한다 — 캔들이
최대 `visibleCandleCount`(16)개뿐이라 매번 통째로 다시 그려도 비용이 무시할 수준이라 가능한 단순화다.

### `Redraw()` 한 번의 흐름

1. `MarketManager.Instance.PriceHistory`(일별 원본, 잘리지 않고 게임 시작부터 전체 보관)를 가져온다.
2. `AggregateHistory(daily, viewport.PeriodDays)` — 일별 데이터를 `PeriodDays`(7 또는 30)일씩 묶어 캔들
   리스트로 집계한다. 진행 중인 마지막 그룹은 날짜가 덜 찬 채로 매 턴 Close/Date만 갱신되다가, 기간이 다
   차는 순간 값이 고정되고 다음 턴부터 새 그룹이 시작된다.
3. `viewport.Resolve(history.Count, out count, out startIndex)` — "지금 몇 개를, 이력의 어디서부터 그릴지"
   결정. 내부적으로 `count = min(history.Count, ZoomCandleCount)`, `startIndex = history.Count - count -
   ViewOffset`. `ViewOffset`이 그 사이 유효 범위를 벗어났으면(줌으로 `count`가 바뀌었거나 이력이 짧을 때)
   여기서 자동으로 재클램프된다 — 팬/줌 어느 쪽이 상태를 바꾸든 동기화 코드를 따로 안 둬도 되는 이유.
4. `ComputePriceRange` — 이 구간 캔들의 Open/Close 최소·최대에 이동평균값(`movingAverage.ExpandPriceRange`)
   까지 더해 min/max를 구하고, 위아래 10% 여백을 붙여 최종 `range`를 확정한다.
5. `grid.Redraw(...)` → 격자선/가격 라벨을 이 min/range 기준으로 갱신.
6. 캔들 풀(`candles`)을 0~`visibleCandleCount` 순회하며, `i < count`인 슬롯만 `Update()`(위치·크기·색
   갱신), 나머지는 `Hide()`(SetActive(false)).
7. `movingAverage.Redraw(...)` — 캔들과 똑같은 min/range/slotWidth를 넘겨받아 같은 좌표계에 이평선을 그림.

Y축이 "스크롤/줌해도 알아서 맞다"고 느껴지는 이유는 3~4단계가 매번 **그 순간 화면에 보이는 구간만** 기준으로
새로 계산되기 때문이다 — 캔들이 몇 개든, 어느 시점을 보고 있든 로직 자체는 항상 동일하다.

### `PriceChartViewport` : 팬 · 줌 · 기간을 하나의 상태로

- `ViewOffset`(int) — 꼬리(최신)에서 몇 캔들 뒤로 갔는지. 휠 한 칸 또는 드래그 누적량이 `slotWidth`를
  넘을 때마다 ±1.
- `ZoomCandleCount`(int) — 지금 화면에 보여줄 캔들 개수. `[minZoomCandleCount, visibleCandleCount]`로 클램프.
- `PeriodDays`(int) — 캔들 1개가 며칠치인지. `WeekDays=7` 또는 `MonthDays=30`.

`ZoomBy(delta)`가 이 중 `ZoomCandleCount`/`PeriodDays`를 함께 다루는 작은 상태 머신이다:

```
next = ZoomCandleCount + delta
next가 풀 용량 초과      && 지금 주봉  → 월봉 전환, ZoomCandleCount = 최소값(재확대 여지 확보)
next가 최소값 미만       && 지금 월봉  → 주봉 전환, ZoomCandleCount = 풀 용량(재축소 여지 확보)
그 외                                → 그냥 클램프
```

캔들 개수 축(가로 확대율)과 기간 축(주/월)을 휠 한 칸(정수 델타) 하나로 잇는 구조라 "계속 축소"가 중간에
끊기지 않는다. `ViewOffset`은 이 전환과 무관하게 값 자체는 유지되지만, "몇 번째 캔들"이 가리키는 실제
날짜가 기간 전환으로 바뀌므로 화면상 보이던 절대 구간은 살짝 밀릴 수 있다(아래 "남은 것" 참고).

### 드래그가 "캔들 폭 1개" 단위로 딱딱 끊겨 스크롤되는 방법

`OnDrag`는 매 프레임 `RectTransformUtility.ScreenPointToLocalPointInRectangle`로 마우스의 chartArea 로컬
x좌표를 구해, 직전 프레임과의 차이를 `dragAccumX`에 계속 누적한다. 누적량의 절댓값이 `slotWidth`(지금
줌 레벨에서 캔들 1개가 차지하는 폭)를 넘어설 때마다 `PanBy(±1)`을 호출하고 그만큼 누적을 덜어낸다 —
그래서 화면 어디서 드래그하든, 줌 레벨이 얼마든 "캔들 한 칸 = 정확히 그 폭만큼 끌었을 때"로 항상 일관되게
동작한다.

### 캔들 풀링과 줌의 상호작용 (+ 파티클이 왜 얼어붙었는지)

`PriceChartCandles`는 `visibleCandleCount`개의 GameObject를 `Awake` 시점에 딱 한 번 만들고 계속 재사용한다.
줌으로 보여줄 개수가 바뀌어도 오브젝트를 새로 만들거나 파괴하지 않고, 매 `Redraw()`마다 "몇 번 인덱스까지
활성화할지 + 그 인덱스가 지금 어느 날짜를 나타낼지"만 바뀐다. 이 재사용 특성 때문에 앞서 고친 파티클 잔상
버그가 생겼다 — 파티클이 캔들의 자식으로 붙은 채 스폰되는데, 캔들이 재활용되거나 `Hide()`로 숨겨질 때
그 자식인 파티클까지 통째로 `SetActive(false)`되어 애니메이션 코루틴이 중간에 얼어붙는다. 그래서 스폰
직후 파티클을 `chartArea`(캔들 풀링과 무관하게 항상 활성 상태인 조상)로 재부모잉해 분리했다.

## 세션 중 발견/수정한 버그

- **휠 자체가 아예 안 먹음** — Ctrl 키 확인에 레거시 `UnityEngine.Input.GetKey`를 썼는데, 프로젝트가
  New Input System 전용(`ProjectSettings.activeInputHandler: 1`)이라 호출 즉시 예외가 발생해서 휠/Ctrl+휠
  둘 다 막힘. `UnityEngine.InputSystem.Keyboard.current.leftCtrlKey/rightCtrlKey`로 교체.

- **줌 중 파티클이 멈추고 잔상으로 남음** — 급등/급락 파티클(`UIBurstParticle.Spawn`)이 캔들 자신을
  부모로 스폰되는데, 캔들이 오브젝트 풀이라 줌/스크롤로 `Redraw()`가 같은 슬롯을 다른 날짜로 재활용하거나
  `SetActive(false)`로 숨기면, 그 밑에 붙어있던 파티클의 코루틴이 부모 비활성화로 얼어붙어 반쯤 사라진
  조각이 화면에 고정된 채 남음. 스폰 직후 캔들 풀과 무관한 `chartArea`로 재부모잉(`SetParent(chartArea,
  true)`, 월드 위치 유지)하도록 수정 — `TradeModalUI`/`SkillPanelUI`에서 이미 같은 원인(모달 닫힘 시
  부모 비활성화)으로 한 번씩 고쳐졌던 것과 동일한 패턴.
  - 대안으로 `Destroy(obj, delay)` 지연 파괴도 검토했으나 기각 — 이 오버로드는 scaled time 기준이라,
    이 파티클 시스템이 원래 지원해야 하는 `Time.timeScale = 0`(거래/스킬 모달 오픈 중 일시정지) 상황에서
    영원히 안 불려서 오히려 더 나쁨.

- **이동평균선이 차트 밖으로 삐져나감** — Y축 스케일이 캔들 Open/Close만 기준으로 계산돼서, 이평값이 그
  범위를 벗어나면 패널 밖으로 잘림. `ComputePriceRange`가 이동평균값도 min/max 계산에 포함하도록 수정
  (`PriceChartMovingAverage.ExpandPriceRange`).

## 남은 것 / 제외한 것

- **줌 도중 화면 중심 고정 안 함** — 과거로 스크롤된 상태(`ViewOffset != 0`)에서 줌하면 `count`만 바뀌고
  보던 절대 구간이 살짝 밀릴 수 있음. 실제 플레이해보고 거슬리면 중심 유지 로직 추가 필요.
- **Y축 수동 스케일**(가격축을 직접 드래그해서 세로 배율 조절) — 범위 밖. X축(캔들 개수) 줌만 우선
  구현했고, 필요하면 별도 작업으로 진행.
- `minZoomCandleCount`는 주봉/월봉 공통 값(6) — 기간별로 다르게 두는 세분화는 안 함(단순화).
