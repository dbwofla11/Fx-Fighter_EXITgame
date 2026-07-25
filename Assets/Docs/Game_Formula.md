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
- Doubt : 의심도 (0 ~ 100, 감쇠 없음)
- ws : 지지도 가중치 = 0.5 (`ProbabilityCalculator.SupportWeight`)
- wg : 상승률 가중치 = 0.5 (`ProbabilityCalculator.GrowthWeight`)
- wd : 의심도 가중치 = 1 (`ProbabilityCalculator.DoubtWeight`)

`ws`/`wg`가 원래 1.0이었을 때는 Support+Growth 합이 50만 넘어도(둘 다 -100~100 범위인데 절반도 안 채운
수준) score가 1.0을 넘어 `Clamp01`에 걸려 확률이 그대로 100%에 고정돼버리는 문제가 있었다. 게다가
decayRate=0.995(3장)로 감쇠가 느려서 한 번 포화되면 수십~수백 턴 동안 100%가 유지됐다 (플레이 로그에서
`확률:1`이 계속 찍히는 현상으로 확인됨). 0.5로 낮춰 Support/Growth가 훨씬 많이 쌓여야 포화되도록 완화했다.
Doubt 항은 코드에는 원래부터 있었으나 이 문서에 누락돼 있던 것을 반영했다 (기존 동작 변경 없음).

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

- **초기 가격(`MarketManager.InitialPrice`) = 1000**. 게임 시작 시 `CurrentPrice`가 이 값으로 설정된다
  (이전에는 초기화 코드가 없어 C# 기본값인 0으로 시작했었다 — 아래 "가격 하한선" 참고).
- **가격 하한선(`PriceCalculator.MinPrice`) = 1**. `CurrentPrice`는 절대 이 값 밑으로 내려가지 않는다
  (`PriceCalculator.ClampPrice`가 정규 가격 변화·이벤트 가격 충격 양쪽 모두에 적용된 뒤 강제한다). 0 이하로
  내려가면 거래 계산(수량×가격)이 깨지고 이벤트의 `priceRatio`(가격에 곱하는 충격)도 0에 곱해져 무력화되므로
  반드시 필요한 하한선이다.

Scarcity는 발행량(Supply)을 기반으로 계산되는 희소성 지표이다.

공식

Scarcity = 100 × (1 - Supply / MaxSupply)

- MaxSupply = 20000 (`PriceCalculator.MaxSupply`)
- Supply가 0에 가까울수록 Scarcity는 100(최대 희소성)에 가까워지고, Supply가 MaxSupply에 가까워질수록 0에 가까워진다.
- `PlayerStat.Supply`의 기본값은 0이며, 아래 소스로 변화한다. Job은 Supply에 영향을 주지 않는다.
  - 시사 이벤트(4장)의 `EventSO.supplyDelta`
  - 플레이어의 "발행량 조작" 액션 (3장 "발행량 조작") — `추가발행권한` 스킬을 구매해야 사용할 수 있다
  - 재사용형 스킬 구매 시 `EffectType.SupplyIncrease`/`SupplyDecrease` 효과 (`StatCalculator.ApplySkillUse`)
- Supply는 `Support`/`Growth`와 동일하게 `CurrentPrice`처럼 턴을 넘어 유지되는 값이며, 위 소스가 반영되는 순간 직접
  반영된 뒤 매 턴 `TradeCalculator.Decay`로 0을 향해 감쇠한다 (decayRate = 0.995, 3장 "누적치 감쇠" 참고).
- **`MaxSupply = 20000`이 실제 기준값이다.** UI 목업이 "현재 발행량 : 20,000,000개"처럼 더 큰 자릿수를 보여주는
  경우가 있는데, 이는 `TargetAsset`(5장 참고) 때와 동일하게 예시/목업 수치일 뿐 실제 값이 아니다. 이벤트
  `supplyDelta`, 발행량 조작 버튼의 `amount`, 스킬의 `SupplyIncrease`/`SupplyDecrease` 등 Supply를 바꾸는 모든
  수치는 이 20000 스케일을 기준으로 정한다.

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

`MarketManager.NextTurn()`(자동 턴 진행)과 `HandleNewsEvent()`(시사 이벤트 수동 트리거) 양쪽 모두, 계산 전
`CurrentPrice`를 기억해뒀다가 계산 후와 비교해 `PlayerStat.PriceChangeThisTurn`/`StreamerReaction`을 갱신한다.
별도 `EventHub` 이벤트를 추가하지 않았다 — `PlayerStat` 전체가 이미 `EventHub.OnMarketUpdated`로 나가므로 UI는
그 안의 두 필드만 읽으면 된다. 감쇠/이월 없이 매 턴(또는 수동 트리거 시점) 새로 계산되는 값이다.

---

# 2-2. 캔들 차트

플레이 화면 중앙의 코인 가격 캔들스틱 차트를 위한 데이터/렌더링 정의다.

## 캔들 정의

"1턴 = 1일" 기획을 그대로 살려 **캔들 1개 = 하루(한 턴)**로 정의한다. High/Low(꼭지 심지)는 두지 않고
Open-Close 몸통만 그린다 (목업 디자인에도 심지가 없는 단순 사각 막대 형태).

- **Open** : 그 턴 계산 시작 전의 `CurrentPrice`
- **Close** : 그 턴 계산(정규 가격 변화 + 그 턴에 발생한 시사 이벤트 가격 충격 포함) 후의 `CurrentPrice`
- **색상** : `Close >= Open`이면 상승(초록), 아니면 하락(빨강) — `BtnLong`/`BtnShort` 버튼과 동일한 색을 그대로 재사용한다

시사 이벤트 수동 트리거(`EventHub.OnNewsEvent`)는 턴을 넘기지 않는 즉시 반영이라 별도 캔들을 만들지 않는다.
정규 턴 진행(`MarketManager.NextTurn()`) 시점에만 캔들 1개가 기록된다.

## 데이터 : PriceHistory

`MarketManager.PriceHistory`(`IReadOnlyList<PricePoint>`)로 노출된다. `RuntimeEventData`/`EventLog`와 동일한
패턴 — `RuntimePriceHistory`가 `List<PricePoint>`를 들고 있고, 매 턴 하나씩 쌓인다. 이력은 잘라내지 않고 전부
보관한다 (게임 특성상 수천 턴이 지나도 메모리 부담이 무시할 수준).

## 렌더링

`Assets/Scripts/UI/PriceChartUI.cs`가 `EventHub.OnMarketUpdated`를 구독해 매 턴 다시 그린다. 이력 전체가 아니라
최근 `visibleCandleCount`개(기본 14개)만 오브젝트 풀링(미리 만들어둔 Image를 재사용)으로 그리므로 이력이 아무리
쌓여도 성능에 영향이 없다. Y축은 화면에 보이는 캔들들의 Open/Close 최소~최대 값 기준으로 매번 자동 스케일링된다
(위아래 10% 여백 포함).

차트 바로 위에는 `Assets/Scripts/UI/CoinPriceHeaderUI.cs`가 같은 방식(`EventHub.OnMarketUpdated` 구독)으로
코인명과 현재가(`MarketManager.CurrentStat.CurrentPrice`)를 "코인명 ₩현재가" 형식으로 표시한다.

차트 하단에는 각 캔들의 날짜("MM/dd")를 표시하는 X축 라벨이 있고, 캔들에 마우스를 올리면 그 캔들의
날짜/시가/종가를 차트 좌상단에 툴팁으로 보여준다 (캔들의 `raycastTarget`을 켜서 포인터 이벤트를 받는다).

---

# 3. 거래 시스템

플레이어는 현재 가격으로 즉시 거래한다.

## Long

- 현재 가격으로 구매 : Cost = Amount × CurrentPrice (현금 감소, 코인 증가)
- Support 증가
- Growth 증가

## Short

- 현재 가격으로 판매 : Revenue = Amount × CurrentPrice (현금 증가, 코인 감소) — Job/토글형 스킬의 `CashBonus`가
  있으면 이 Revenue에 배율로 붙는다 (3-3장 참고)
- Support 감소
- Growth 감소

거래량이 많을수록 영향력이 증가한다.

## 거래가 Support/Growth에 주는 영향

거래는 발생하는 그 순간 Support/Growth에 직접 반영된다. 영향의 크기는 거래량(Amount)에 비례한다.

Support += ±Amount × ws_trade

Growth += ±Amount × wg_trade

- ws_trade = 0.1 (Support 가중치)
- wg_trade = 0.1 (Growth 가중치)
- 부호는 Long(+) / Short(-)

## 발행량 조작

Long/Short와 마찬가지로 플레이어가 수량을 직접 입력해 즉시 실행하는 액션이다 (`EventHub.RaiseManipulateSupply(long amount)`,
`TradeCalculator.ManipulateSupply`). 현금 비용은 없다.

- `추가발행권한` 스킬을 구매하기 전에는 사용할 수 없다 (`SkillManager.IsUnlocked(SkillID.추가발행권한)`가 false면
  `MarketManager.HandleManipulateSupply`가 아무 일도 하지 않는다).
- amount가 양수면 발행량 증가(희석), 음수면 발행량 감소(소각)를 의미한다.

Supply += amount

Support -= amount × ws_trade

Growth -= amount × wg_trade

Doubt += |amount| × wd_supply

- ws_trade/wg_trade = 0.1 (Long/Short와 동일한 가중치를 그대로 재사용한다)
- wd_supply = 0.1 (Doubt 가중치)
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
- Supply(2장)도 동일한 감쇠 대상이다.
- **UI 표시 전용 그림자 값** : `PlayerStat.JobSkillSupportBonus`/`JobSkillGrowthBonus`는 Job 선택·재사용형 Skill
  구매가 준 기여분만 Trade/시사 이벤트를 제외하고 별도로 누적하며, `Support`/`Growth`와 동일한 `decayRate`로
  똑같이 감쇠한다. 게임 계산(가격, 확률 등)에는 전혀 관여하지 않고 오직 개요 화면에 "Job+Skill이 지금
  기여하고 있는 몫"을 스탯당 하나의 숫자로 보여주기 위한 값이다.
- **Doubt(의심도)는 감쇠하지 않는다.** Job/Skill의 `DoubtDecrease` 효과(활성 상태인 동안 매 턴 계속 재적용,
  CashBonus/Volume과 같은 그룹)와 시사 이벤트(4장)로 값이 바뀌지만, 한 번 바뀐 값은 시간이 지나도 원래대로
  돌아오지 않고 턴을 넘어 그대로 유지된다. 기획상 Doubt는 시간이 지날수록 자동으로 100을 향해 올라가다가 100이
  되면 게임오버가 되는 지표이기 때문이다 (자동 상승 로직은 아직 미구현, 4장 "후보" 참고).

---

# 3-1. 직업(Job)이 Support/Growth에 주는 영향

직업의 Support/Growth 효과는 매 턴 계속 재적용되지 않고, **직업을 선택하는 순간 직접 반영된 뒤 거래와 동일하게 감쇠**한다.
(부정 이벤트/스킬/숏 거래로 Support/Growth가 내려가야 하는데, 직업 효과가 매 턴 무한정 다시 채워지면 실질적으로 내려갈 수 없기 때문)

- 직업 선택 시 : `Support += 해당 직업의 SupportIncrease 효과 합`, Growth도 동일 (1회성)
- 이후 매 턴 : 위 "누적치 감쇠" 공식과 동일하게 감쇠
- 직업의 Support/Growth **외** 효과(DoubtDecrease, CashBonus, VolumeIncrease/Decrease, ExitUnlock 등)는 기존처럼 직업을 유지하는 동안 매 턴 계속 재적용된다 (감쇠 대상 아님).

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

## 재사용 불가 스킬 (`isReusable == false`, 예: `ExitUnlock`)

영구 효과형 스킬이다. 기존과 동일하게 활성화(On) 상태를 유지하는 동안 매 턴 효과가 계속 재적용되며, 감쇠 대상이 아니다.

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

---

# 4. 시사 이벤트

시사 이벤트는 발생하는 즉시, 뽑힌 `EventSO`가 정의한 만큼 Support/Growth/Doubt/Supply/가격에 직접 반영된다
(`EventCalculator.Calculate(PlayerStat stat, IReadOnlyList<EventSO> eventDatabase)`).
Support/Growth/Supply는 Trade/Job과 동일하게 반영 후 매 턴 감쇠하지만, Doubt는 감쇠하지 않고 그대로 유지된다
(3장 참고).

## 발생 시점

- **무조건 발생** : `EventSO.guaranteedTurn`(0이면 해당 없음, N이면 게임 시작 후 N번째 턴)이 설정된 이벤트는
  확률 판정 없이 그 턴에 반드시 발생한다. 무조건 발생 이벤트가 있는 턴에는 아래 "자동(확률)" 판정을 건너뛴다.
  `MarketManager.FindGuaranteedEvent(turnCount)`가 매 턴 `eventDatabase`를 훑어 찾는다.
- 자동(확률) : `MarketManager.NextTurn()`에서 `NewsEventIntervalTurns`(30)턴마다 `NewsEventChance`(40%) 확률로 발생
  여부를 판정한다.
- 수동 : `EventHub.OnNewsEvent`를 통해 즉시 발생시킬 수 있다 (UI/시스템 트리거용, 자동 판정과 무관).
- 자동/수동 모두 동일하게 `EventCalculator.Calculate(CurrentStat, eventDatabase)`를 호출한다.
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
- Supply : `stat.Supply += chosen.supplyDelta` (이후 매 턴 감쇠, 2장 참고)
- 가격 : `stat.CurrentPrice += stat.CurrentPrice × chosen.priceRatio` — 그 턴의 `PriceCalculator` 정규 가격
  변화(2장 공식)와는 별개로 이벤트가 즉시 일으키는 1회성 충격이며, 감쇠하지 않는다.

발생한 이벤트는 `MarketManager.EventLog`(`IReadOnlyList<EventLogEntry>`)에 `{EventSO 참조, 발생 날짜
(TimeManager.CurrentGameDate)}`로 기록되어, 나중에 이벤트 로그 UI가 그대로 읽어 그릴 수 있다.

## Doubt 자동 상승

Doubt는 시간이 지나면 저절로 오르는 지표다. 게임 시간으로 2년이 지나면 자동으로 Doubt가 상승하기 시작하고,
그 이후로는 매 턴 계속 증가한다. Doubt는 감쇠하지 않으므로(3장) 자동 상승분도 그대로 누적된다.

- 1턴 = 1일 기준, 게임 시간 2년 = 730턴
- 730턴째 : Doubt += 20 (1회)
- 731턴부터 : 매 턴 Doubt += 0.5

---

# 5. 엔딩 조건

게임 종료 조건은 4가지다. 자동 판정(체포/거지)과 플레이어가 직접 트리거하는 판정(엑시트/영웅)으로 나뉜다.

## 1. 체포 엔딩 (Bad Ending)

- **조건** : `Doubt >= 100`
- **결과** : 수사 후 체포, 자산 몰수
- **판정 시점** : 자동 (매 턴 체크)
- **설명** : "끝없는 탐욕은 결국 법의 심판으로 돌아왔다."

## 2. 엑시트 엔딩 (Neutral Ending)

- **조건** : 목표 자산 달성 (`PlayerManager.currentMoney >= TargetAsset`)
- **결과** : 해외 도피 성공, 법적 처벌은 피함, 사회적 평판 붕괴 (SNS 여론 악화, 언론 비난, 업계 퇴출, 평생 꼬리표)
- **판정 시점** : 플레이어가 "엑시트" 버튼을 클릭할 때 — 3번(영웅) 조건을 만족하지 못하면 이쪽으로 처리된다
- **설명** : "당신은 돈을 얻었지만 사람들의 신뢰를 잃었다. 세상은 당신을 성공한 사업가가 아닌 사기꾼으로 기억한다."

## 3. 영웅 엔딩 (True Ending)

- **조건** : 목표 자산 달성 + `Doubt <= 50` + `Support >= 80`
- **결과** : 합법적인 운영, 투자자와 함께 성장, 사회적 인정 획득
- **판정 시점** : 플레이어가 "엑시트" 버튼을 클릭할 때 — Doubt/Support 조건까지 만족하면 엑시트 대신 이쪽으로 처리된다
- **설명** : "당신은 투기 대신 신뢰를 선택했다. 많은 사람이 당신의 프로젝트로 이익을 얻었고, 당신은 업계의
  모범 사례로 남았다."

## 4. 거지 엔딩

- **조건** : 현금 0 + 코인 0
- **결과** : 미정 (추가 기획 필요)
- **판정 시점** : 자동 (매 턴 체크)

## 공통 사항

- **목표 자산(TargetAsset) = 1,000,000,000** (`MarketManager.TargetAsset`). 시작 자금(10,000,000)의 100배.
- "엑시트" 버튼(2/3번)은 **현금이 목표 자산 이상일 때만** 누를 수 있다 (`MarketManager.CanExit`). 코인 보유량은
  조건에 포함되지 않는다 — 코인을 안 팔고 그대로 들고 있어도 현금만 충분하면 버튼이 활성화된다.
- `PlayerStat.ExitUnlocked`(스킬의 `EffectType.ExitUnlock`으로 해금되는 기존 플래그)는 **이 엑시트 버튼과 무관**하다.
  버튼 활성화는 오직 목표 자산 달성 여부로만 판단한다.
- 자동 판정(1/4번)이 같은 턴에 동시에 성립하면 **체포 엔딩이 우선**이다.
- 체포/거지 엔딩 모두 `PlayerManager`의 현금/코인 수치를 실제로 바꾸지는 않는다 ("자산 몰수"는 결과 화면상의
  설명일 뿐, 게임이 그 시점에 종료되므로 수치 조작은 불필요).

---

# 참고

본 문서는 게임 계산 로직의 기준 문서이다.

Calculator는 본 문서를 기준으로 구현한다.

게임 밸런스 변경 시 본 문서와 Calculator를 함께 수정한다.