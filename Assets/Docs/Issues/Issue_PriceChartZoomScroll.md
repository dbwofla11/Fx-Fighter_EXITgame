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
