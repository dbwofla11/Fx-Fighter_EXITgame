# 게임 공식

본 문서는 게임의 시장 계산 공식을 정의한다.

---

# 1. 가격 방향성

기본 상승 확률 : 50%

기본 하락 확률 : 50%

Support와 Growth에 따라 상승 확률이 변경된다.

상승 확률과 하락 확률의 합은 항상 100%이다.

공식

Pup = Clamp(
0.5
+ ws × Support / 100
+ wg × Growth / 100
- wd × Doubt / 100
,0,1)

Pdown = 1 - Pup

변수

- Support : 코인 지지도 (-100 ~ 100)
- Growth : 코인 상승률 (-100 ~ 100)
- Doubt : 의심도 (-100 ~ 100, 감쇠 없음) — 기획상 체감 범위는 0~100(100이면 체포 엔딩)이지만, 직업의 초기
  `DoubtDecrease` 효과가 기본값 0에서도 실제로 보이도록 하한을 Support/Growth와 동일한 -100으로 뒀다
  (`StatCalculator.ClampStat`, 2026-08-02 수정)
- ws : 지지도 가중치 = 0.25 (`ProbabilityCalculator.SupportWeight`)
- wg : 상승률 가중치 = 0.25 (`ProbabilityCalculator.GrowthWeight`)
- wd : 의심도 가중치 = 0.025 (`ProbabilityCalculator.DoubtWeight`, 2026-08-04에 0.25 → 0.025로 1/10 낮춤 — Doubt가
  100까지 차도 가격 상승확률을 거의 못 누르도록 완화)

`ws`/`wg`가 원래 1.0이었을 때는 Support+Growth 합이 50만 넘어도(둘 다 -100~100 범위인데 절반도 안 채운
수준) score가 1.0을 넘어 `Clamp01`에 걸려 확률이 그대로 100%에 고정돼버리는 문제가 있었다. 게다가
decayRate=0.995(3장)로 감쇠가 느려서 한 번 포화되면 수십~수백 턴 동안 100%가 유지됐다 (플레이 로그에서
`확률:1`이 계속 찍히는 현상으로 확인됨). 1차로 0.5로 낮췄지만, `TradeCalculator.Long/Short`가 거래 1건마다
Support/Growth를 항상 동일한 양만큼 같이 움직이는 탓에(3장 `ws_trade`/`wg_trade` 참고) 실질적으로는 독립된
두 신호가 아니라 "거래 신호" 하나가 `ws+wg`로 두 배 반영되는 셈이라, 몇 번만 거래해도 여전히 바로 포화됐다.
0.25로 다시 절반 낮춰 Support/Growth가 **둘 다 클램프 상한(100)까지 차야만** 포화되도록 완화했다.

`wd`도 원래 코드에 있었으나(1.0) 이 문서에는 누락돼 있었다. 1.0이면 Doubt=100(3장 "Doubt 자동 상승"으로
게임 후반 730턴 이후 계속 오름)일 때 Support/Growth가 아무리 좋아도(둘 다 최대 100이어도)
`score = 0.5+0.25+0.25-1.0 = 0.0`으로 상승확률이 완전히 0까지 눌려 후반 게임이 사실상 진행 불가능하다는
피드백을 받았다. `ws`/`wg`와 동일하게 0.25로 낮춰서, Doubt가 100까지 차도 Support/Growth가 최대치면
`Pup`이 최대 0.75까지는 유지되도록(= Doubt 하나만으로 확률을 완전히 압도하지 못하도록) 완화했다.
(2026-08-04 추가 조정) 이후 요청으로 `wd`를 0.25 → 0.025로 한 번 더 1/10 낮췄다 — 이제 Doubt=100이어도
Support/Growth가 최대치면 `Pup`이 최대 0.975까지 유지된다(= Doubt가 상승확률에 주는 영향이 훨씬 작아짐).

Support/Growth는 `CurrentPrice`처럼 턴을 넘어 유지되는 값이다. Job 선택, 거래(Long/Short), 스킬 사용이
발생하는 순간 그 값에 직접 반영되고, 매 턴 서서히 0으로 감쇠한다 (자세한 내용은 3장 "누적치 감쇠" 참고).

---

# 2. 가격 변화량

가격 변화량은 방향성과 별도로 계산된다. **여기서 계산되는 값은 부호 없는 크기(magnitude)이며, 방향(오르는지
내리는지)은 오직 1장의 `isUp`(Pup 확률로 굴린 결과)만으로 결정한다.** `0.6 × Growth + 0.25 × Support + 0.15 ×
Scarcity`는 Growth/Support가 음수일 수 있어 그 자체로는 부호가 있는 값이 나오지만, 이 부호를 그대로 가격에
반영하면 안 된다 — 절대값을 취해 크기만 쓴다 (`PriceCalculator.Calculate`가 `Mathf.Abs`로 처리).

Price(t+1)

=

Price(t)

±

|0.6 × Growth

+

0.25 × Support

+

0.15 × Scarcity|

(`isUp`이면 +, 아니면 -)

**거래량(Volume) 배율** : 위 절대값(`|0.6×Growth + 0.25×Support + 0.15×Scarcity|`)에 `volumeMultiplier`를 한
번 더 곱한다 — 이 문서에 그동안 누락돼 있었지만 `PriceCalculator.Calculate`에 이미 구현돼 있다.

volumeMultiplier = Max(MinVolumeDeltaMultiplier, 1 + Volume × VolumeDeltaWeight)

- VolumeDeltaWeight = 0.01 (`PriceCalculator.VolumeDeltaWeight`) — 거래량부풀리기(+25) 스킬 하나만 썼을 때
  대략 변동폭이 +25% 되도록 잡은 임시 수치(ponytail, 플레이 후 조정 필요)
- MinVolumeDeltaMultiplier = 0.2 (`PriceCalculator.MinVolumeDeltaMultiplier`) — `Volume`이 크게 마이너스여도
  배율이 0에 너무 가까워지지 않게 하는 하한
- `PlayerStat.Volume`은 Job/Skill의 `VolumeIncrease`/`VolumeDecrease` 효과(3-1/3-2장)로만 바뀌며, CashBonus와
  같은 그룹(활성 상태인 동안 매 턴 새로 채워짐, 감쇠 없음)이다.

- **초기 가격(`MarketManager.InitialPrice`) = 10** (2026-08-04, 1000 → 10로 조정). 게임 시작 시 `CurrentPrice`가
  이 값으로 설정된다 (이전에는 초기화 코드가 없어 C# 기본값인 0으로 시작했었다 — 아래 "가격 하한선" 참고).
- **가격 하한선(`PriceCalculator.MinPrice`) = 1**. `CurrentPrice`는 절대 이 값 밑으로 내려가지 않는다
  (`PriceCalculator.ClampPrice`가 정규 가격 변화·이벤트 가격 충격 양쪽 모두에 적용된 뒤 강제한다). 0 이하로
  내려가면 거래 계산(수량×가격)이 깨지고 이벤트의 `priceRatio`(가격에 곱하는 충격)도 0에 곱해져 무력화되므로
  반드시 필요한 하한선이다.
- **표시 형식** (2026-08-04) : `CurrentPrice`는 원래도 `float`였고 타입 변경은 없었다. `UIFormat.CurrencyTight`
  (`CoinPriceHeaderUI`/`PriceChartUI`가 가격 표시에 쓰는 포맷)만 `"N0"` → `"N3"`로 바꿔 소수점 3자리까지
  보여주도록 했다 — 초기 가격이 1000→10으로 작아지면서 정수 표시만으로는 변동폭이 잘 안 보이기 때문. 현금
  표시(`UIFormat.Currency`, `PlayerManager.currentMoney`)는 그대로 정수(`long`)/`"N0"` 유지.

Scarcity는 발행량(Supply)을 기반으로 계산되는 희소성 지표이다.

공식

Scarcity = 100 × (1 - Supply / MaxSupply)

- MaxSupply = 100000 (`PriceCalculator.MaxSupply`, 2026-08-04에 20000 → 100000으로 조정)
- Supply가 0에 가까울수록 Scarcity는 100(최대 희소성)에 가까워지고, Supply가 MaxSupply에 가까워질수록 0에 가까워진다.
- `PlayerStat.Supply`의 초기값은 `InitialSupply(2000) + PlayerManager.currentCoins(10000) = 12000`이다
  (2026-08-04). 보유 코인도 이미 발행된 코인이라는 지적을 받아, 순수 초기 발행분(`MarketManager.InitialSupply`
  = 2000, `Awake()`에서 `CurrentPrice`처럼 설정)에 시작 시점 보유 코인만큼을 `Start()`에서 더한다 —
  `PlayerManager.Instance`가 `Awake()` 시점엔 초기화 순서가 보장되지 않아, 씬의 모든 `Awake`가 끝난 뒤 호출되는
  `Start()`에서 더했다. 이후 아래 소스로 변화한다. Job은 Supply에 영향을 주지 않는다.
  - 시사 이벤트(4장)의 `EventSO.supplyDelta`
  - 플레이어의 "발행량 조작" 액션 (3장 "발행량 조작") — `추가발행권한` 스킬을 구매해야 사용할 수 있다
  - 재사용형 스킬 구매 시 `EffectType.SupplyIncrease`/`SupplyDecrease` 효과 (`StatCalculator.ApplySkillUse`)
- (설계 수정, 2026-08-04) Supply는 Support/Growth와 반대로 **매 턴 자동으로 늘어난다** (`TradeCalculator.
  GrowSupply`, 채굴/인플레이션 개념). 처음엔 Support/Growth와 동일하게 0을 향해 감쇠하도록 짰었는데("발행량이
  시간이 지나면 줄어든다"), 실제로는 "턴이 지날수록 늘어나야 한다"는 정정을 받아 감쇠 대신 매 턴 증가로
  바꿨다 — `TradeCalculator.SupplyDecayRate`는 삭제, `GrowSupply(stat, suppressionRatio)`가
  `StatCalculator.Calculate()`에서 `Decay(stat)` 바로 다음에 호출된다.
- **(2026-08-08) 증가 속도가 Support(지지도)에 로그로 연동된다 — "러쉬막기".** 고정값(`SupplyGrowthPerTurn = 50`)
  대신, Support(-100~100)를 0~1로 정규화한 뒤 로그를 태워 지지도가 높을수록 발행 속도가 빨라지되 무한정
  커지지 않고 완만해지는 곡선으로 바꿨다.

  ```
  normalizedSupport = Clamp01((Support + 100) / 200)
  growthPerTurn = SupplyGrowthBase + SupplyGrowthLogCoefficient × ln(1 + normalizedSupport)
  Supply += growthPerTurn × (1 - suppressionRatio)
  ```

  - `SupplyGrowthBase = 10`, `SupplyGrowthLogCoefficient = 100` (`TradeCalculator`, 둘 다 밸런스용 임시
    수치라 실제 플레이 후 조정 필요) — Support가 -100 이하(정규화 0)면 10/턴, Support가 0(정규화 0.5)이면
    약 50/턴(기존 고정값과 비슷하게 맞춤), Support가 100(정규화 1)이면 약 79/턴까지 완만하게 늘어난다.
  - `suppressionRatio`는 기존과 동일하게 추가발행권한/우회발행권한 스킬이 이 증가폭 자체를 깎는 비율이다.
- **`MaxSupply = 100000`이 실제 기준값이다.** UI 목업이 "현재 발행량 : 20,000,000개"처럼 더 큰 자릿수를 보여주는
  경우가 있는데, 이는 `TargetAsset`(5장 참고) 때와 동일하게 예시/목업 수치일 뿐 실제 값이 아니다. 이벤트
  `supplyDelta`, 발행량 조작 버튼의 `amount`, 스킬의 `SupplyIncrease`/`SupplyDecrease` 등 Supply를 바꾸는 모든
  수치는 이 100000 스케일을 기준으로 정한다.
- **Supply는 보유 코인(`PlayerManager.currentCoins`) 밑으로 못 내려간다** (버그 수정, 2026-08-04).
  `StatCalculator.ClampStat`이 Support/Growth/Doubt 클램프와 함께 `Supply = Max(Supply, currentCoins)`를
  강제한다 — 발행량 조작(소각)/이벤트/스킬로 Supply가 깎이다가 마이너스가 되던 버그가 있었다("내가 들고 있는
  코인보다 전체 발행량이 적을 수는 없다"는 게 최소 전제). 같은 이유로 `PlayerManager.AddCoin`도 `currentCoins`가
  0 밑으로 내려가지 않게 막는다(소각 반복 시 보유 코인이 함께 마이너스로 내려가던 연쇄 버그) — 매도는
  `TradeModalUI`가 이미 보유량 이하로 제한하므로 실질적으로는 발행량 조작(소각)에만 영향을 준다.
  결과적으로 발행량 조작(민팅) 한 번으로 Supply와 `currentCoins`가 같은 방향으로 같이 바뀌므로(3장 참고)
  두 값은 더 이상 완전히 무관하지 않다.

---

# 2-1. 스트리머 반응

플레이 화면 우측 "스트리머" 패널이 이번 턴 가격 변화량에 따라 반응(표정/멘트)을 바꾸기 위한 값이다. UI는
아직 스프라이트가 준비되지 않아 로직만 먼저 확정했다 (`Next_Tesk.md` 참고).

## 판정 기준

이번 턴 `CurrentPrice`의 총 변화량(`PriceChangeThisTurn`)을 기준으로 5단계 중 하나를 고른다. 변화량은 그 턴의
정규 가격 변화(2장 공식)와 시사 이벤트의 즉시 가격 충격(4장)을 모두 합산한 값이다 — "그 턴에 CurrentPrice가
실제로 얼마나 움직였는가"를 그대로 쓴다. 퍼센트가 아니라 절대값 기준이다 (2장 공식상 정규 변화량 자체가
Support/Growth/Scarcity 범위(-100~100)에 묶여 있어 게임 진행에 따라 가격 규모가 커져도 절대값 기준의 의미가
크게 흔들리지 않기 때문).

```
PriceChangeThisTurn = CurrentPrice(이번 턴 계산 후) - CurrentPrice(이번 턴 계산 전)
```

| 단계 | 조건 | 의미 |
|---|---|---|
| Surge(폭등) | PriceChangeThisTurn >= 50 | 큰 폭의 상승 |
| Up(상승) | 10 <= PriceChangeThisTurn < 50 | 상승 |
| Neutral(보합) | -10 < PriceChangeThisTurn < 10 | 변화 거의 없음 |
| Down(하락) | -50 < PriceChangeThisTurn <= -10 | 하락 |
| Crash(폭락) | PriceChangeThisTurn <= -50 | 큰 폭의 하락 |

임계값(50/10)은 정규 가격 변화의 이론상 최대치(약 ±85~100)를 참고한 예시치로, 실제 스프라이트가 들어오고
플레이테스트가 진행되면 밸런스 조정이 필요할 수 있다.

## 갱신 시점

`MarketManager.NextTurn()`(자동 턴 진행)은 계산 전
`CurrentPrice`를 기억해뒀다가 계산 후와 비교해 `PlayerStat.PriceChangeThisTurn`/`StreamerReaction`을 갱신한다.
별도 `EventHub` 이벤트를 추가하지 않았다 — `PlayerStat` 전체가 이미 `EventHub.OnMarketUpdated`로 나가므로 UI는
그 안의 두 필드만 읽으면 된다. 감쇠/이월 없이 매 턴 새로 계산되는 값이다.

---

# 2-2. 캔들 차트

플레이 화면 중앙의 코인 가격 캔들스틱 차트를 위한 데이터/렌더링 정의다.

## 캔들 정의

**데이터 기록은 "1턴 = 1일" 그대로**이지만, **화면에 그리는 캔들 1개는 7일(1주)**을 모은 주봉이다
(플레이 피드백 반영 — 매일 찍히는 일봉은 선처럼 촘촘해서 보기 불편하다는 이유로 변경). High/Low(꼭지 심지)는
두지 않고 Open-Close 몸통만 그린다 (목업 디자인에도 심지가 없는 단순 사각 막대 형태).

- **Open** : 그 주(7일 그룹) 첫날 턴 계산 시작 전의 `CurrentPrice`
- **Close** : 그 주 마지막 날(또는 아직 7일이 안 찼으면 지금까지의 마지막 날) 턴 계산 후의 `CurrentPrice`
- **색상** : `Close >= Open`이면 상승(초록), 아니면 하락(빨강) — `BtnLong`/`BtnShort` 버튼과 동일한 색을 그대로 재사용한다

진행 중인 마지막 주는 1~6일차엔 Open이 고정된 채 Close/Date만 매 턴 갱신되며 같은 캔들 자리에 그려지고,
7일째가 되는 순간 그 캔들이 확정되며 8일째부터는 다음 캔들 자리가 새로 시작된다 (한 번 확정된 캔들은 이후
값이 바뀌지 않는다).

정규 턴 진행(`MarketManager.NextTurn()`) 시점에만 일별 `PricePoint` 1개가 기록된다 (수동 트리거 경로는
존재하지 않는다 — 시사 이벤트는 자동 발생만 있다).

## 데이터 : PriceHistory

`MarketManager.PriceHistory`(`IReadOnlyList<PricePoint>`)로 노출된다. `RuntimeEventData`/`EventLog`와 동일한
패턴 — `RuntimePriceHistory`가 `List<PricePoint>`를 들고 있고, 매 턴(하루) 하나씩 쌓인다. **이 리스트 자체는
여전히 일별 데이터**이며, 7일 단위 주봉 집계는 저장 단계가 아니라 아래 렌더링 단계에서만 이뤄진다 (다른
기능이 일별 원본을 참조할 수도 있어 원본 해상도를 그대로 유지). 이력은 잘라내지 않고 전부 보관한다 (게임
특성상 수천 턴이 지나도 메모리 부담이 무시할 수준).

## 렌더링

`Assets/Scripts/UI/PriceChartUI.cs`가 `EventHub.OnMarketUpdated`를 구독해 매 턴 다시 그린다. `Redraw()`가
먼저 일별 `PriceHistory`를 `AggregateWeekly()`로 7일씩 묶어 주봉 리스트로 변환한 뒤, 그 중 최근
`visibleCandleCount`개(기본 16개, 이제 "일" 대신 "주" 단위)만 오브젝트 풀링(미리 만들어둔 Image를 재사용)으로
그리므로 이력이 아무리 쌓여도 성능에 영향이 없다. Y축은 화면에 보이는 캔들들의 Open/Close 최소~최대 값 기준으로
매번 자동 스케일링된다 (위아래 10% 여백 포함).

차트 배경에는 가격대를 나타내는 가로 격자선 `gridLineCount`개(기본 4개, 위 Y축 범위를 5구간으로 등분)와
왼쪽 가격 라벨("₩N,NNN" 형식)이 캔들보다 먼저 그려져 뒤에 깔린다 (토스뱅크류 앱 스타일 참고, 반올림 없이
실제 계산값을 그대로 표시한다).

차트 바로 위에는 `Assets/Scripts/UI/CoinPriceHeaderUI.cs`가 같은 방식(`EventHub.OnMarketUpdated` 구독)으로
코인명과 현재가(`MarketManager.CurrentStat.CurrentPrice`)를 "코인명 ₩현재가" 형식으로 표시한다.

차트 하단에는 각 캔들(주봉)의 마지막 날짜("MM/dd")를 표시하는 X축 라벨이 있고, 캔들에 마우스를 올리면 그
주의 날짜/시가/종가를 차트 좌상단에 툴팁으로 보여준다 (캔들의 `raycastTarget`을 켜서 포인터 이벤트를 받는다).

---

# 3. 거래 시스템

플레이어는 현재 가격으로 즉시 거래한다.

(버그 수정, 2026-08-04) Long/Short/발행량 조작 확정 직후에도 `StatCalculator.ClampStat`을 호출해 Support/Growth
(-100~100)/Doubt(-100~100) 범위를 즉시 강제한다. 원래 `ClampStat`은 `MarketManager.NextTurn()`/
`HandleNewsEvent()` 두 곳에만 있었는데(1장 "상한선 초과 버그 수정" 참고), `TradeModalUI`/`CoinControlModalUI`의
확정 버튼이 `EventHub.RaiseMarketUpdated`를 직접 쏘는 세 번째 경로라는 걸 그때 놓쳤다. 그 결과 거래를 반복하면
값이 범위를 넘어 계속 오르는데 게이지 막대는 `Clamp01`로 이미 꽉 찬 채라 안 움직여서, "거래해도 스탯이 더 이상
반영 안 된다"처럼 보였다.

## Long

- 현재 가격으로 구매 : Cost = Amount × CurrentPrice (현금 감소, 코인 증가)
- Support 증가
- Growth 증가
- Doubt 증가 (거래 자체가 의심을 산다 — 방향과 무관)
- **매수 1회 상한(러쉬막기, 2026-08-08)** : `TradeModalUI.GetMaxTradeAmount()`가 잔고 기준 상한
  (`currentMoney / CurrentPrice`)에 발행량 기준 상한을 `Min`으로 추가 적용한다 —
  `TradeCalculator.MaxTradeAmountBySupply(currentSupply, currentCoins) = Max(0, Supply - currentCoins)`
  ("이미 보유한 만큼 빼고 시장에 남은 유통량까지만 매수 가능"). 처음엔 "고정 10,000개 + 발행량 기준 중
  더 작은 쪽"으로 계획했으나, `MarketManager.InitialSupply = 2000`이라 게임 시작 시점부터 발행량 기준
  상한(2000)이 고정 10,000개보다 항상 타이트해서 고정 상한은 죽은 코드가 되므로 뺐다. Doubt 캡
  (`MaxTradeAmountByDoubt`)은 기존처럼 마지막에 `Min`으로 적용된다. Short(매도)에는 적용하지 않는다.

## Short

- 현재 가격으로 판매 : Revenue = Amount × CurrentPrice (현금 증가, 코인 감소) — Job/토글형 스킬의 `CashBonus`가
  있으면 이 Revenue에 배율로 붙는다 (3-3장 참고)
- Support 감소
- Growth 감소
- Doubt 증가 (Long과 동일 — 방향과 무관)

거래량이 많을수록 영향력이 증가한다.

## 거래가 Support/Growth/Doubt에 주는 영향

거래는 발생하는 그 순간 Support/Growth/Doubt에 직접 반영된다. 영향의 크기는 거래량(Amount)에 비례한다.

Support += ±Amount × ws_trade

Growth += ±Amount × wg_trade

Doubt += |Amount| × wd_trade

- ws_trade = 0.005 (Support 가중치, `TradeCalculator.TradeSupportWeightPerCoin`, 2026-08-05에 발행량 조작과
  같은 값(0.1)이었다가 거래만으로 지지도/상승률이 너무 크게 흔들린다는 피드백으로 낮춤)
- wg_trade = 0.005 (Growth 가중치, `TradeCalculator.TradeGrowthWeightPerCoin`, 위와 동일한 조정)
- wd_trade = 0.002 (Doubt 가중치, `TradeCalculator.DoubtWeightPerTradeCoin`, 2026-08-04에 0.02 → 0.002로 1/10
  낮춤) — Long/Short 둘 다 부호 없이 더해진다 (감쇠 없이 그대로 누적, `ManipulateSupply`와 동일한 설계). 거래를
  자주/크게 할수록 "시장에서 눈에 띈다"는 의미로, 후반 Doubt가 자동으로도 오르는 상황(3장 "Doubt 자동 상승")에
  거래까지 더해지면 부담이 커지므로 Support/Growth 가중치보다 훨씬 작게 잡았다.
- (2026-08-05 정리) `ws_trade`/`wg_trade`는 발행량 조작 전용 가중치(아래 "발행량 조작" 절의 `ws_supply`/
  `wg_supply`)와 더는 같은 상수를 재사용하지 않는다 — 거래(Long/Short)와 발행량 조작이 시장에 주는 충격의
  크기가 서로 다르다는 판단으로 `TradeCalculator`에서 각각 별도 상수로 분리했다.
- Support/Growth 부호는 Long(+) / Short(-)

## 발행량 조작

Long/Short와 마찬가지로 플레이어가 수량을 직접 입력해 즉시 실행하는 액션이다 (`EventHub.RaiseManipulateSupply(long amount)`,
`TradeCalculator.ManipulateSupply`). 현금 비용은 없다.

- `추가발행권한` 스킬을 구매하기 전에는 사용할 수 없다 (`SkillManager.IsUnlocked(SkillID.추가발행권한)`가 false면
  `MarketManager.HandleManipulateSupply`가 아무 일도 하지 않는다).
- amount가 양수면 발행량 증가(희석), 음수면 발행량 감소(소각)를 의미한다.
- (버그 수정, 2026-08-04) 발행/소각한 만큼 `PlayerManager.currentCoins`(보유 코인)도 함께 변한다 — 발행 주체가
  곧 플레이어 자신이므로, 시장 전체 발행량(Supply)뿐 아니라 그 코인을 실제로 갖게 되는(또는 소각으로 잃는)
  것까지 한 세트다. 이전에는 `Supply`만 바뀌고 보유 코인수량은 그대로였다.

Supply += amount

PlayerManager.currentCoins += amount

Support -= amount × ws_supply

Growth -= amount × wg_supply

Doubt += |amount| × wd_supply

- ws_supply = 0.02, wg_supply = 0.02 (`TradeCalculator.SupportWeightPerCoin`/`GrowthWeightPerCoin`, Long/Short
  전용 `ws_trade`/`wg_trade`(위 "거래가 Support/Growth/Doubt에 주는 영향" 절)와는 별개 상수 — 2026-08-05에
  지지도/상승률 변화가 너무 크다는 피드백으로 5분의 1로 낮춤)
- wd_supply = 0.015 (Doubt 가중치, `TradeCalculator.DoubtWeightPerSupplyUnit`, 2026-08-05에 1/10로 낮췄다가
  다시 1.5배로 올림)
- 발행량 증가(희석)든 감소(소각)든 "조작했다는 사실 자체"가 의심을 키우므로 Doubt는 amount의 **절대값**에 비례해
  증가한다. Doubt는 감쇠하지 않으므로(3장 참고) 한 번 늘어난 만큼 그대로 남는다.
- Support/Growth는 부호가 거래와 반대다 : 발행량 증가는 시장에 코인이 흔해진다는 뜻이라 Support/Growth를
  깎고, 발행량 감소(소각)는 희소해진다는 뜻이라 Support/Growth를 올린다.

## 누적치 감쇠

Support/Growth는 거래·직업 선택·스킬 사용으로 반영된 값을 계속 들고 있다가, 아무 행동이 없어도
시간이 지나면 (마이너스 포함) 절대값 0으로 서서히 수렴한다.

Support(t+1) = Support(t) × decayRate

Growth(t+1) = Growth(t) × decayRate

- decayRate = 0.995 (매 턴 0.5%씩 감소, 절반이 되는 데 약 138턴)
- 매 턴(`StatCalculator.Calculate()`, `MarketManager.NextTurn()`에서 호출) 적용된다.
- 직업(Job)의 Support/Growth 효과도 동일하게 취급한다 — 3-1장 참고.
- Supply(2장)는 반대로 매 턴 자동으로 늘어난다(감쇠 대상이 아니다, `GrowSupply`가 Support 연동 로그 공식으로
  증가폭을 계산, 2026-08-04 정정 / 2026-08-08 로그 공식으로 변경).
- **UI 표시 전용 그림자 값** : `PlayerStat.JobSkillSupportBonus`/`JobSkillGrowthBonus`는 Job 선택·재사용형 Skill
  구매가 준 기여분만 Trade/시사 이벤트를 제외하고 별도로 누적하며, `Support`/`Growth`와 동일한 `decayRate`로
  똑같이 감쇠한다. 게임 계산(가격, 확률 등)에는 전혀 관여하지 않고 오직 개요 화면에 "Job+Skill이 지금
  기여하고 있는 몫"을 스탯당 하나의 숫자로 보여주기 위한 값이다.
- **Doubt(의심도)는 감쇠하지 않는다.** Job 선택/재사용형 Skill 사용/재사용 불가(토글형) Skill 구매의
  `DoubtDecrease`/`DoubtIncrease`(전부 1회성 — 3-1/3-2장, `ApplySkills`가 매 턴 재적용 대상에서 제외한다,
  2026-08-05 정리), 시사 이벤트(4장)로 값이 바뀌지만, 한 번 바뀐 값은 시간이 지나도 원래대로 돌아오지 않고
  턴을 넘어 그대로 유지된다.
  기획상 Doubt는 시간이 지날수록 자동으로 100을 향해 올라가다가 100이 되면 게임오버가 되는 지표이기 때문이다
  (자동 상승 로직은 아직 미구현, 4장 "후보" 참고).
  - (버그 수정, 2026-08-02) 원래 Job의 `DoubtDecrease`도 CashBonus/Volume과 같은 그룹으로 매 턴 재적용됐는데,
    Doubt는 이 값들과 달리 매 턴 새로 계산되지 않고 그대로 이어지는(carry-over) 값이라 매 턴 반복 적용되면
    무한정 계속 깎이는 버그가 있었다. Support/Growth와 동일하게 "선택 시점 1회 반영"으로 고쳤다.

---

# 3-1. 직업(Job)이 Support/Growth/Doubt에 주는 영향

직업의 Support/Growth/Doubt 효과는 매 턴 계속 재적용되지 않고, **직업을 선택하는 순간 직접 반영**된다.
Support/Growth는 그 뒤 거래와 동일하게 감쇠하고, Doubt는 감쇠 없이 그대로 유지된다.
(부정 이벤트/스킬/숏 거래로 Support/Growth가 내려가야 하는데, 직업 효과가 매 턴 무한정 다시 채워지면 실질적으로
내려갈 수 없는 것과 같은 이유로, Doubt도 carry-over 값이라 매 턴 재적용하면 무한정 계속 깎이거나 오른다.)

- 직업 선택 시 : `Support += 해당 직업의 SupportIncrease 효과 합`, Growth도 동일, Doubt도
  `DoubtDecrease`/`DoubtIncrease` 합만큼 동일하게 1회 반영 (전부 1회성)
- 이후 매 턴 : Support/Growth는 위 "누적치 감쇠" 공식과 동일하게 감쇠, Doubt는 감쇠 없이 그대로 유지
- 직업의 Support/Growth/Doubt **외** 효과(CashBonus, VolumeIncrease/Decrease, ExitUnlock 등)는 기존처럼 직업을 유지하는 동안 매 턴 계속 재적용된다 (매 턴 새로 계산되는 값이라 감쇠/carry-over 대상이 아님).
- (버그 수정, 2026-08-04) 위 "매 턴 재적용" 효과들은 `StatCalculator.ApplyJob`이 채우는데, 원래 이 함수가
  `MarketManager.NextTurn()`을 통해서만 호출돼서 **직업을 고른 시점부터 그 날의 첫 턴이 지나기 전까지는
  CashBonus 등이 전부 0으로 비어있었다**. 매수/매도 모달이 열려있는 동안은 시간이 멈추므로(3장 참고), 직업
  선택 직후 바로 거래하면 CashBonus 보너스가 안 붙는 문제로 나타났다. `JobManager.SelectJob`이
  `ApplyJobSelection` 직후 `StatCalculator.ApplyJob`도 한 번 더 호출하도록 고쳐서, 선택 즉시 이 그룹도
  채워지게 했다 (그 뒤 매 턴 `NextTurn()`에서 동일한 값으로 다시 계산되므로 중복 반영 문제는 없다).

---

# 3-2. 스킬(Skill)이 Support/Growth에 주는 영향

스킬은 `SkillSO.isReusable` 값에 따라 두 종류로 나뉜다.

## 재사용 가능 스킬 (`isReusable == true`)

Support/Growth 부스트형 스킬이다. 직업과 동일하게, **구매 버튼을 누르는 순간 직접 반영된 뒤 거래·직업과 동일하게 감쇠**한다.
구매 버튼 클릭 한 번이 구매와 사용을 동시에 처리하며, 잠기는 단계 없이 비용만 내면 몇 번이고 다시 구매할 수 있다.
(스킬 아이콘 클릭은 정보 표시용일 뿐 구매를 일으키지 않는다.)

- 구매 시 : `Cost = baseCost × costMultiplier ^ PurchaseCount` 결제 성공 시 `Support += 해당 스킬의 SupportIncrease 효과 합`, Growth도 동일 (1회성)
- 결제에 실패(잔액 부족)하면 아무 효과도 적용되지 않는다.
- 다음 구매의 비용은 `PurchaseCount`가 오른 만큼 상승한다. 잠금 상태가 없으므로 연속 구매도 가능하다.
- 이후 매 턴 : "누적치 감쇠" 공식과 동일하게 감쇠 (별도 처리 불필요, `TradeCalculator.Decay`가 Support/Growth 전체를
  대상으로 하므로 자동 적용됨)
- `defaultUnlocked` 값과 무관하게 최초 구매도 항상 비용을 지불해야 한다.

## 재사용 불가 스킬 (`isReusable == false`, 예: `추가발행권한`, `ExitUnlock`)

영구 효과형 스킬이다. 구매 순간 `ApplySkillUse`가 Support/Growth/Doubt 등을 1회 반영하는 건 재사용형과 동일 —
Support/Growth는 그 뒤 거래·직업과 동일하게 감쇠하고(재구매로 못 갱신하니 시간이 지나면 옅어짐), Doubt는
감쇠 없이 그대로 유지된다(위 "Doubt는 감쇠하지 않는다" 참고). 다만 CashBonus/`PositiveEventRate`/
`NegativeEventRate`/`ExitUnlock`처럼 매 턴 새로 계산되는 값(감쇠 대상이 아닌 필드)은 구매 후에도 활성화(On)
상태를 유지하는 동안 `StatCalculator.ApplySkills()`가 매 턴 재적용한다(2026-08-05, `SkillManager.EnableSkill`
연결 — 3-3장 참고). `SupplyGrowthSuppress`/`SupplyIncrease`/`SupplyDecrease`/Volume류는 별도 로직(3-3장/2장)이
처리하므로 이 재적용 대상이 아니다.

---

# 3-3. CashBonus(현금 증가)

`EffectType.CashBonus`는 코인을 팔아 현금화할 때(Short) 받는 수익에 배율로 붙는 버프(%)다. 매수(Long)는
지출이라 대상이 아니다.

## Job/토글형 스킬의 CashBonus

Job의 효과나 토글형 스킬의 효과처럼, 활성 상태인 동안 매 턴 `PlayerStat.CashBonus`에 그 시점의 값이 다시
채워진다(3-1/3-2장의 Doubt류와 같은 그룹 — 감쇠하지 않고, 매 턴 새로 계산됨).

공식 (Short, 3장 참고)

Revenue = Amount × CurrentPrice × (1 + CashBonus / 100)

- `PlayerManager.HandleSellCoin`이 판매 수익(`Amount × CurrentPrice`)에 그 순간의 `CurrentStat.CashBonus`%를
  배율로 곱해서 지급한다.
- `CashBonus`가 0이면(기본값) 배율 없이 원래 수익 그대로 지급된다.
- ~~매 턴 보유 현금 전체에 곱해서 불리는 방식(복리 이자)~~은 "버프가 아니라 이자 아니냐"는 지적을 받고
  폐기했다 (`Logging.md` 참고). 지금은 실제 거래를 해야만 효과를 보는 구조다.

## 재사용형 스킬의 CashBonus

Support/Growth와 동일하게 **구매 시점 1회성 현금 지급**이다. `PlayerStat.CashBonus`에 이월/감쇠가 없어서
Support/Growth처럼 값을 쌓아뒀다가 서서히 줄이는 방식이 불가능하므로, `SkillManager.HandlePurchase()`가 구매
즉시 `currentMoney × (해당 스킬의 CashBonus 효과값 / 100)`만큼 현금을 바로 지급한다
(`SkillManager.GrantCashBonus`). Job/토글형 스킬의 CashBonus(거래 수익 배율)와는 완전히 별개의 경로다.

## 재사용 불가(영구형) 스킬의 CashBonus

(버그 수정, 2026-08-04) `추가발행권한`(재사용 불가, `isReusable == false`)에 CashBonus+15 효과가 있는데,
`GrantCashBonus`가 `isReusable` 여부를 안 가리고 모든 구매에 적용되고 있어서 이 스킬도 재사용형과 똑같이
"구매 즉시 현금 한 방"으로 처리되고 있었다 — "현금증가량"(`PlayerStat.CashBonus`, 개요 탭 표시값)은 전혀
오르지 않아, 이후 Short 거래에서 보너스가 안 붙는 버그였다.

재사용 불가 스킬의 CashBonus는 위 "Job/토글형 스킬의 CashBonus"와 같은 그룹(활성 상태인 동안 매 턴 다시 채워짐)
이어야 하는 게 맞는 설계다.

(2026-08-05) 토글(`SkillManager.IsEnabled`/`EnableSkill`/`DisableSkill`) 시스템을 실제로 연결하면서, CashBonus도
`추가발행권한` 전용 하드코딩 없이 일반 토글 루프로 흡수했다.

- `HandlePurchase()`가 재사용 불가 스킬 구매 시 `EnableSkill()`을 호출해 `IsEnabled = true`로 만든다.
- `StatCalculator.ApplySkills()`(매 턴, 활성화된 모든 재사용 불가 스킬 순회)가 `ApplyJob`과 동일하게
  Support/Growth/DoubtIncrease/DoubtDecrease/Supply류/Volume류는 재적용 대상에서 제외한다 — 구매 시 1회
  반영된 값이 매 턴 또 쌓이는 회귀를 막기 위함(과거 Job의 DoubtDecrease 매 턴 재적용 버그와 동일 패턴).
  CashBonus/`PositiveEventRate`/`NegativeEventRate`/`ExitUnlock`은 매 턴 새로 계산되는(감쇠 없는) 값이라
  제외하지 않고 그대로 재적용한다 — 그래서 CashBonus를 가진 재사용 불가 스킬이 몇 개든 스킬 이름을
  하드코딩할 필요 없이 자동으로 매 턴 채워진다.
- 다만 구매 순간에는 아직 `Calculate()`가 안 돌아서 `ApplySkills()`가 자동으로 안 채워주므로, 방금 산
  스킬 하나만 즉시 반영해야 한다(`StatCalculator.ApplyToggleSkillEffects(stat, skill.Profile)` — `ApplySkills`와
  같은 제외 목록을 쓰는 공용 함수). 전체 활성 스킬을 다시 순회하면 이미 활성화돼 있던 다른 스킬들의
  CashBonus까지 이번 턴에 또 더해져 중복되므로, 반드시 방금 산 스킬 하나로 범위를 좁혀야 한다.
- 예전에 있던 `추가발행권한` 전용 함수(`ApplyUnlockedPermanentSkillCashBonus`)는 이걸로 대체되어 삭제했다.

---

# 3-4. 의심도 하락 (DoubtDecline, 여론조작 스킬 전용)

여론조작 카테고리 스킬 중 3종(실시간여론관리/수상경력홍보/후기마케팅)만 기존 `DoubtDecrease`(구매 즉시
1회 차감, 3-1/3-2장) 대신 `DoubtDecline`을 쓴다(2026-08-07 추가). 방어/코인설계 카테고리의 기존
`DoubtDecrease` 9개는 그대로 즉시 감소 방식을 유지한다 — Doubt를 즉시 깎는 대신, 총량을 일정 기간에 걸쳐
나눠 깎아서 "서서히 깎이는" 느낌을 준다.

공식

DoubtDeclinePerTurn = totalAmount × declineRatioPerTurn

Doubt(t+1) = Doubt(t) - Σ (그 시점에 진행 중인 모든 DoubtDecline의 DoubtDeclinePerTurn)

변수

- totalAmount : 스킬의 `effect.value` (기존 `DoubtDecrease`와 동일한 값 그대로, 절반으로 낮추지 않음) —
  실시간여론관리=20, 수상경력홍보=5, 후기마케팅=5
- declineRatioPerTurn = 0.005 (0.5%, `BuffCalculator.DoubtDeclineRatioPerTurn`)
- durationTurns = 200 (`BuffCalculator.DoubtDeclineDurationTurns`) — `declineRatioPerTurn × durationTurns = 1.0`이
  되도록 짝을 맞춰서 정확히 총량만큼만 깎이고 끝난다. 세션 중 50→100→200턴으로 여러 번 조정된 임시 밸런스
  값이다.

**스킬별 독립 진행** : `PlayerStat.DoubtDeclines`(`Dictionary<SkillID, DoubtDeclineBuff>`)로 관리되어, 서로
다른 스킬의 하락이 동시에 진행 중이면 각자의 `DoubtDeclinePerTurn`이 전부 합산 차감된다(예: 세 스킬을 모두
사면 매 턴 `(20+5+5) × 0.005`가 한꺼번에 깎임). 처음엔 `PlayerStat`에 단일 필드 하나로 구현했다가, 서로
다른 스킬을 동시에 쓰면 하나가 다른 하나를 덮어써버리는 문제가 있어 스킬별 딕셔너리로 다시 짰다. **같은
스킬을 재구매**하면 그 스킬 항목만 새 값으로 덮어써지고(재사용 시 값 갱신 — 2장 "거래량(Volume) 배율"의
Volume N턴 버프와 동일한 패턴), 다른 스킬의 진행 중인 하락은 그대로 유지된다.

**구현** : `Assets/Scripts/Runtimes/Systems/BuffCalculator.cs`(`TradeCalculator`/`EventCalculator`와 동일한
static 클래스 패턴)의 `StartDoubtDecline`(구매 시 시작, `StatCalculator.ApplySkillUse`가 호출)/
`TickDoubtDeclines`(매 턴 차감, `StatCalculator.Calculate()`가 호출)가 전담한다. 기존
`StatCalculator`/`PlayerStat`에 흩어져 있던 Volume N턴 버프 로직(`VolumeBuffTurnsRemaining` 카운트다운)도
이 세션에서 같은 파일로 이관했다. 자세한 관련 파일/호출 스택은 `Issue_DoubtDecline.md` 참고.

---

# 4. 시사 이벤트

시사 이벤트는 발생하는 즉시, 뽑힌 `EventSO`가 정의한 만큼 Support/Growth/Doubt/Supply/가격에 직접 반영된다
(`EventCalculator.Calculate(PlayerStat stat, IReadOnlyList<EventSO> eventDatabase)`).
Support/Growth는 Trade/Job과 동일하게 반영 후 매 턴 감쇠하고, Supply는 반영 후에도 매 턴 자동 증가가 계속
적용된다. Doubt는 감쇠하지 않고 그대로 유지된다 (3장 참고).

## 발생 시점

- **무조건 발생** : `EventSO.guaranteedTurn`(0이면 해당 없음, N이면 게임 시작 후 N번째 턴)이 설정된 이벤트는
  확률 판정 없이 그 턴에 반드시 발생한다. 무조건 발생 이벤트가 있는 턴에는 아래 "자동(확률)" 판정을 건너뛴다.
  `MarketManager.FindGuaranteedEvent(turnCount)`가 매 턴 `eventDatabase`를 훑어 찾는다.
- 자동(확률) : `MarketManager.NextTurn()`에서 `NewsEventIntervalTurns`(30)턴마다 `NewsEventChance`(40%) 확률로 발생
  여부를 판정한다.
- 무조건 발생/자동(확률) 모두 동일하게 `EventCalculator.Calculate(CurrentStat, eventDatabase)`를 호출한다.
  `eventDatabase`는 `MarketManager`가 `[SerializeField] List<EventSO>`로 들고 있다 (`SkillManager.skillDatabase`와
  동일한 패턴).

## EventSO — 이벤트 하나의 정의

각 이벤트는 `EventSO`(`Assets/Scripts/SOs/EventSo.cs`) 에셋 하나로 완결된다. Job/Skill과 마찬가지로 여러 개를
에셋으로 만들어 `MarketManager.eventDatabase`(`List<EventSO>`)에 등록해둔다.

| 필드 | 설명 |
|---|---|
| `message` | 이벤트 로그에 표시될 문구 (예: "유명 스트리머가 코인을 소개했습니다!") |
| `category` | `Positive`/`Negative`. 선택 확률 편향과 UI 색상(초록/빨강) 구분에 쓰인다 |
| `weight` | 같은 `category` 안에서 가중치 랜덤 선택에 쓰이는 값. 클수록 자주 뽑힌다 |
| `effects` | `List<EffectData>`. Job/Skill과 동일한 구조로, Support/Growth/Doubt 등 **원하는 스탯만 골라 부호 있는 값**을 지정한다 (예: `SupportIncrease +15`, `GrowthIncrease +10`만 넣고 Doubt는 아예 안 넣는 식) |
| `supplyDelta` | Supply 변화량 (부호 포함). Job/Skill은 Supply를 건드리지 않으므로 `effects`에는 포함하지 않고 전용 필드로 둔다 |
| `priceRatio` | 가격에 즉시 반영되는 변화율 (부호 포함, 예: `0.03` = +3%, `-0.06` = -6%) |
| `guaranteedTurn` | 0이면 확률 발생 대상. N(1 이상)이면 게임 시작 후 N번째 턴에 확률 체크 없이 무조건 발생 (예: 스트리머_소개=1, 거래소_상장=2로 초반 두 턴에 순차 발생하도록 설정됨) |

`effects`에서 Doubt를 다루려면 `EffectType.DoubtIncrease`(신규 추가, Job/Skill의 기존 `DoubtDecrease`와 대칭)와
`DoubtDecrease`를 상황에 맞게 쓴다.

## 방향 (긍정/부정) — 카테고리 선택

Pup_event = Clamp(0.5 + PositiveEventRate / 100 - NegativeEventRate / 100, 0, 1)

이벤트가 `Positive` 카테고리로 뽑힐 확률이다. `PlayerStat.PositiveEventRate`/`NegativeEventRate`는 Job/Skill의
기존 `EffectType`(`PositiveEventRate`/`NegativeEventRate`)으로 조정될 수 있다.

먼저 이 확률로 카테고리(Positive/Negative)를 정한 뒤, `eventDatabase` 중 **그 카테고리에 속한 `EventSO`들만**
대상으로 `weight` 가중치 랜덤을 돌려 하나를 뽑는다. 해당 카테고리에 속한 이벤트가 하나도 없으면 그 턴은 아무
이벤트도 발생하지 않는다.

## 반영 방식

뽑힌 `EventSO`(`chosen`)의 값을 그대로, 부호 변환 없이 적용한다 (부호는 이미 각 `EventSO`의 `effects`/
`supplyDelta`/`priceRatio`에 authored되어 있다).

- Support/Growth/Doubt : `chosen.effects`를 하나씩 `StatCalculator.ApplyEffect(stat, effect)`로 적용 (Job/Skill과
  동일한 함수 재사용). Support/Growth는 이후 매 턴 감쇠(3장), Doubt는 감쇠하지 않고 그대로 누적(3장)
- Supply : `stat.Supply += chosen.supplyDelta` (이후 매 턴 자동 증가가 계속 적용됨, 2장 참고)
- 가격 : `stat.CurrentPrice += stat.CurrentPrice × chosen.priceRatio` — 그 턴의 `PriceCalculator` 정규 가격
  변화(2장 공식)와는 별개로 이벤트가 즉시 일으키는 1회성 충격이며, 감쇠하지 않는다.

발생한 이벤트는 `MarketManager.EventLog`(`IReadOnlyList<EventLogEntry>`)에 `{EventSO 참조, 발생 날짜
(TimeManager.CurrentGameDate)}`로 기록되어, 나중에 이벤트 로그 UI가 그대로 읽어 그릴 수 있다.

## Doubt 자동 상승

Doubt는 시간이 지나면 저절로 오르는 지표다. 게임 시간으로 2년이 지나면 자동으로 Doubt가 상승하기 시작하고,
그 이후로는 매 턴 계속 증가한다. Doubt는 감쇠하지 않으므로(3장) 자동 상승분도 그대로 누적된다.

- 1턴 = 1일 기준, 게임 시간 2년 = 730턴
- 730턴째 : Doubt += 4 (1회, `MarketManager.DoubtAutoRiseInitialAmount`)
- 731턴부터 : 매 턴 Doubt += 0.1 (`MarketManager.DoubtAutoRisePerTurn`)
- (2026-08-05) 자동 상승 속도가 너무 빠르다는 피드백으로 기존 수치(20 / 0.5)의 1/5로 낮췄다.

---

# 5. 엔딩 조건

게임 종료 조건은 3가지다. 자동 판정(체포/거지)과 플레이어가 직접 트리거하는 판정(엑시트)으로 나뉜다.

(과거엔 엑시트 시점 Doubt/Support 조건으로 "영웅(True Ending)"을 따로 갈랐으나 폐지됐다(2026-08-13
기획서 정리) — `EndingCalculator.CheckExit()`가 이제 조건 없이 항상 엑시트 엔딩만 반환하고,
`ExitEndingSceneUI`/스토리(`Assets/Profile/엔딩_스토리/엑시트_영웅.asset`)도 이 엔딩 하나로 통합돼 있다.
스토리/통계 화면 자체는 기존 그대로 재사용한다.)

## 1. 체포 엔딩 (Bad Ending)

- **조건** : `Doubt >= 100`
- **결과** : 수사 후 체포, 자산 몰수
- **판정 시점** : 자동 (매 턴 체크)
- **설명** (2026-08-06, 전용 `EndingScene`에 배경 이미지와 함께 표시, `EndingSceneUI.cs`) : "결국 금감원에
  걸려버렸다....."

## 2. 엑시트 엔딩

- **조건** : 목표 자산 달성 (`PlayerManager.currentMoney >= TargetAsset`)
- **결과** : 해외 도피 성공, 법적 처벌은 피함, 사회적 평판 붕괴 (SNS 여론 악화, 언론 비난, 업계 퇴출, 평생 꼬리표)
- **판정 시점** : 플레이어가 "엑시트" 버튼을 클릭할 때
- **설명** : "당신은 돈을 얻었지만 사람들의 신뢰를 잃었다. 세상은 당신을 성공한 사업가가 아닌 사기꾼으로 기억한다."
  (다음 세션에 새 스토리로 교체 예정)

## 3. 거지 엔딩

- **조건** : 현금 0 + 코인 0, **또는** 코인 가격이 상폐 기준(`MarketManager.DelistingPriceThreshold = 2`)
  이하로 7턴(1주, `DaysPerCandle=7` 기준) 연속으로 붙어있음
- **결과** : 게임오버 연출은 체포 엔딩과 동일하게, `SampleScene`에서 StatGaugeUI 패널 붕괴 + PriceChartUI
  캔들 붕괴가 먼저 보인 뒤 전용 `EndingScene`으로 넘어간다.
- **설명** (2026-08-06, `EndingSceneUI.cs`) : "코인이 상장폐지 당했다......."
- **판정 시점** : 자동 (매 턴 체크). 상폐 기준 연속 턴 수는 `MarketManager.priceFloorStreak`가 매 턴
  갱신하며, 가격이 상폐 기준을 벗어나면 즉시 0으로 리셋된다 (`EndingCalculator.PriceFloorStreakLimit = 7`).

## 공통 사항

- **목표 자산(TargetAsset) = 500,000,000** (`MarketManager.TargetAsset`, 2026-08-04에 1,000,000,000 → 절반으로
  조정). 시작 자금(10,000, 같은 날 10,000,000 → 조정)의 50,000배.
- "엑시트" 버튼(2번)은 **현금이 목표 자산 이상일 때만** 누를 수 있다 (`MarketManager.CanExit`). 코인 보유량은
  조건에 포함되지 않는다 — 코인을 안 팔고 그대로 들고 있어도 현금만 충분하면 버튼이 활성화된다.
- `PlayerStat.ExitUnlocked`(스킬의 `EffectType.ExitUnlock`으로 해금되는 기존 플래그)는 **이 엑시트 버튼과 무관**하다.
  버튼 활성화는 오직 목표 자산 달성 여부로만 판단한다.
- 자동 판정(1/3번)이 같은 턴에 동시에 성립하면 **체포 엔딩이 우선**이다.
- 체포/거지 엔딩 모두 `PlayerManager`의 현금/코인 수치를 실제로 바꾸지는 않는다 ("자산 몰수"는 결과 화면상의
  설명일 뿐, 게임이 그 시점에 종료되므로 수치 조작은 불필요).

---

# 참고

본 문서는 게임 계산 로직의 기준 문서이다.

Calculator는 본 문서를 기준으로 구현한다.

게임 밸런스 변경 시 본 문서와 Calculator를 함께 수정한다.