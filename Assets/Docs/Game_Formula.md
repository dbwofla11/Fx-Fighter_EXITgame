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
,0,1)

Pdown = 1 - Pup

변수

- Support : 코인 지지도 (-100 ~ 100)
- Growth : 코인 상승률 (-100 ~ 100)
- ws : 지지도 가중치
- wg : 상승률 가중치

Support/Growth는 `CurrentPrice`처럼 턴을 넘어 유지되는 값이다. Job 선택, 거래(Long/Short), 스킬 사용이
발생하는 순간 그 값에 직접 반영되고, 매 턴 서서히 0으로 감쇠한다 (자세한 내용은 3장 "누적치 감쇠" 참고).

---

# 2. 가격 변화량

가격 변화량은 방향성과 별도로 계산된다.

Price(t+1)

=

Price(t)

+

0.6 × Growth

+

0.25 × Support

+

0.15 × Scarcity

Scarcity는 발행량(Supply)을 기반으로 계산되는 희소성 지표이다.

공식

Scarcity = 100 × (1 - Supply / MaxSupply)

- MaxSupply = 20000 (`PriceCalculator.MaxSupply`)
- Supply가 0에 가까울수록 Scarcity는 100(최대 희소성)에 가까워지고, Supply가 MaxSupply에 가까워질수록 0에 가까워진다.
- `PlayerStat.Supply`의 기본값은 0이며, 시사 이벤트(4장)에 의해서만 변화한다. Job/Skill은 Supply에 영향을 주지 않는다.
- Supply는 `Support`/`Growth`와 동일하게 `CurrentPrice`처럼 턴을 넘어 유지되는 값이며, 이벤트가 발생한 순간 직접 반영된 뒤
  매 턴 `TradeCalculator.Decay`로 0을 향해 감쇠한다 (decayRate = 0.995, 3장 "누적치 감쇠" 참고).

---

# 3. 거래 시스템

플레이어는 현재 가격으로 즉시 거래한다.

## Long

- 현재 가격으로 구매 : Cost = Amount × CurrentPrice (현금 감소, 코인 증가)
- Support 증가
- Growth 증가

## Short

- 현재 가격으로 판매 : Revenue = Amount × CurrentPrice (현금 증가, 코인 감소)
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

## 누적치 감쇠

Support/Growth는 거래·직업 선택·스킬 사용으로 반영된 값을 계속 들고 있다가, 아무 행동이 없어도
시간이 지나면 (마이너스 포함) 절대값 0으로 서서히 수렴한다.

Support(t+1) = Support(t) × decayRate

Growth(t+1) = Growth(t) × decayRate

- decayRate = 0.995 (매 턴 0.5%씩 감소, 절반이 되는 데 약 138턴)
- 매 턴(`StatCalculator.Calculate()`, `MarketManager.NextTurn()`에서 호출) 적용된다.
- 직업(Job)의 Support/Growth 효과도 동일하게 취급한다 — 3-1장 참고.
- Supply(2장)도 동일한 감쇠 대상이다.
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

# 4. 시사 이벤트

시사 이벤트는 발생하는 즉시, 뽑힌 `EventSO`가 정의한 만큼 Support/Growth/Doubt/Supply/가격에 직접 반영된다
(`EventCalculator.Calculate(PlayerStat stat, IReadOnlyList<EventSO> eventDatabase)`).
Support/Growth/Supply는 Trade/Job과 동일하게 반영 후 매 턴 감쇠하지만, Doubt는 감쇠하지 않고 그대로 유지된다
(3장 참고).

## 발생 시점

- 자동 : `MarketManager.NextTurn()`에서 `NewsEventIntervalTurns`(30)턴마다 `NewsEventChance`(40%) 확률로 발생
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

## Doubt와 게임오버

Doubt는 100에 도달하면 게임오버가 되는 지표다. 현재는 Job/Skill의 `DoubtDecrease`(활성 상태인 동안 매 턴 감소)와
시사 이벤트(위 표, 부정 이벤트 시 증가)로만 값이 바뀐다.

## 후보 : 2년 경과 후 자동 Doubt 상승 (+ 게임오버 판정)

기획상 게임 시간으로 2년이 지나면 자동으로 Doubt가 +20 이상 반영되고, 그 이후로는 매 턴 자동으로 Doubt가 계속
증가하는 것으로 예정되어 있다. 아직 구현 전이며, 아래를 확정해야 한다 (`Next_Tesk.md` 참고).

- 트리거 조건 (게임 시간 2년 = 몇 턴인지)
- 최초 +20(혹은 그 이상) 반영량과 이후 매 턴 증가량
- `Doubt >= 100`이 됐을 때의 게임오버 처리(어디서 판정하고 어떻게 종료시킬지)도 아직 구현되어 있지 않다.

---

# 참고

본 문서는 게임 계산 로직의 기준 문서이다.

Calculator는 본 문서를 기준으로 구현한다.

게임 밸런스 변경 시 본 문서와 Calculator를 함께 수정한다.