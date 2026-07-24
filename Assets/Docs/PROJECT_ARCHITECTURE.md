# 프로젝트 아키텍처

## 전체 구조

UI

↓

EventHub

↓

Manager

↓

RuntimeData

↓

Systems (Calculator)

↓

EventHub

↓

UI

---

# EventHub

UI와 Manager 사이를 중계하는 정적(static) 이벤트 허브. (Assets/Scripts/manager/EventHub.cs)

- UI는 EventHub만 호출한다 (`EventHub.Raise*()`).
- Manager는 EventHub 이벤트를 구독한다 (`OnEnable`/`OnDisable`).
- UI와 Manager는 서로 직접 참조하지 않는다.
- Manager끼리는 직접 호출할 수 있다.

## 이벤트 목록

- OnDayChanged : 하루 경과 (TimeManager 발행 → MarketManager 구독)
- OnSkillClicked : 스킬 아이콘 클릭 요청 (SkillManager 구독) — 토글형은 활성화/비활성화, 재사용형은 `RuntimeSkillData.SelectedSkillId`에 선택만 저장
- OnSkillPurchased : 스킬 구매 버튼 클릭 요청, 인자 없음, 재사용형 전용 (SkillManager 구독) — `SelectedSkillId` 기준으로 구매+사용을 함께 처리, 잠기지 않음
- OnJobSelected : 직업 선택 요청 (JobManager 구독)
- OnBuyCoin / OnSellCoin : 코인 매수/매도 요청 (PlayerManager, MarketManager 구독)
- OnManipulateSupply : 발행량 조작 요청, 양수/음수로 증가·감소 (MarketManager 구독) — `추가발행권한` 스킬을 구매하기 전에는 무시된다
- OnNewsEvent : 시사 이벤트 수동 트리거 (MarketManager 구독) — 자동 발생(30턴마다 확률)은 `MarketManager.NextTurn()`이 별도로 처리하며 동일한 `TriggerNewsEvent`/`EventCalculator.Calculate`를 호출한다. 수동 트리거는 다음 턴까지 기다리지 않고 처리 직후 `EventHub.RaiseMarketUpdated`를 바로 발행해 가격 변화를 즉시 반영한다
- OnMarketUpdated : 시장 계산 완료 후 UI 갱신 (MarketManager 발행)
- OnExitRequested : 엑시트 버튼 클릭 요청, 인자 없음 (MarketManager 구독) — `MarketManager.CanExit`(목표 자산 달성 여부)가 false면 무시된다
- OnGameEnded : 게임 종료(엔딩 확정) 통지, `EndingType` 전달 (MarketManager 발행)

---

# Manager

- TimeManager
- MarketManager
- SkillManager
- JobManager
- PlayerManager

Manager는 게임 상태를 관리하며, EventHub를 구독하여 요청을 처리한다.

---

# RuntimeData

- PlayerStat
- RuntimeSkillData
- RuntimeJobData
- RuntimeEventData

RuntimeData는 현재 게임 상태와 계산 결과를 저장한다. `RuntimeSkillData.SelectedSkillId`는 재사용형 스킬 아이콘을
클릭해 선택한 상태를 들고 있으며, 구매 버튼(`OnSkillPurchased`)이 이 값을 기준으로 동작한다. `RuntimeEventData.Log`
(`List<EventLogEntry>`)는 지금까지 발생한 시사 이벤트 기록(`EventSO` 참조 + 발생 날짜)을 들고 있으며,
`MarketManager.EventLog`로 노출된다.

`PlayerStat.Support`/`Growth`/`Supply`/`CurrentPrice`는 턴을 넘어 유지되는 값이다. Job 선택, 거래(Long/Short),
시사 이벤트가 발생하는 순간 그 값에 직접 반영되고, 매 턴 서서히 감쇠한다 (자세한 내용은 Game_Formula.md 참고).
`JobSkillSupportBonus`/`JobSkillGrowthBonus`는 이 중 Job 선택·재사용형 Skill 구매가 준 몫만(Trade/이벤트 제외)
별도로 추적하는 UI 표시 전용 값으로, 게임 계산에는 관여하지 않고 Support/Growth와 동일하게 감쇠한다
(Game_Formula.md 3장 "누적치 감쇠" 참고).
`Supply`는 시사 이벤트, 발행량 조작 액션(`OnManipulateSupply`, `추가발행권한` 스킬 구매 후 사용 가능), 재사용형
스킬의 `SupplyIncrease`/`SupplyDecrease` 효과로 변화한다. Job은 Supply에 영향을 주지 않는다.

`PlayerStat.Doubt`도 턴을 넘어 유지되지만 **감쇠하지 않는다.** Job/Skill의 `DoubtDecrease`(활성 상태인 동안 매 턴
재적용)와 시사 이벤트로 변화하고, 게임 시간 2년(730턴)째부터는 매 턴 자동으로도 오른다
(`MarketManager.ApplyDoubtAutoRise`). 100에 도달하면 체포 엔딩으로 게임이 종료된다 (5장 "엔딩 판정" 참고,
공식은 Game_Formula.md 3장/5장).

---

# Systems

- StatCalculator
- ProbabilityCalculator
- PriceCalculator
- TradeCalculator
- EventCalculator

Calculator는 상태를 변경하지 않고 계산만 수행한다.

---

# 현재 계산 흐름

## 턴 진행

TimeManager

↓

EventHub.OnDayChanged

↓

MarketManager.NextTurn()

↓

StatCalculator.Calculate() (Support/Growth/Supply 이월 후 감쇠, Doubt는 감쇠 없이 이월, Job/Skill 효과 적용)

↓

(30턴마다 확률) EventCalculator.Calculate() — eventDatabase에서 가중치 랜덤으로 뽑은 EventSO 값을 Support/Growth/Supply/Doubt/가격에 직접 반영

↓

ProbabilityCalculator.Calculate()

↓

PriceCalculator.Calculate()

↓

PlayerStat 갱신

↓

EventHub.OnMarketUpdated (UI 갱신)

## 거래 (Long/Short)

EventHub.OnBuyCoin / OnSellCoin

↓

PlayerManager : 현재가 기준 현금 정산 + 코인 증감

MarketManager : TradeCalculator.Long/Short → CurrentStat.Support/Growth에 직접 반영 (매 턴 감쇠)

## 발행량 조작

EventHub.OnManipulateSupply(amount)

↓

MarketManager : SkillManager.IsUnlocked(추가발행권한) 확인 → 실패 시 무시

↓ 성공

TradeCalculator.ManipulateSupply → CurrentStat.Supply/Support/Growth/Doubt에 직접 반영 (현금 비용 없음)

## 5. 엔딩 판정

자동 판정(체포/거지) : `MarketManager.NextTurn()`의 `EventHub.OnMarketUpdated` 발행 직후 `CheckAutomaticEndings()`가
매 턴 확인한다. `Doubt >= 100` → 체포, 아니면 `(현금 == 0 && 코인 == 0)` → 거지. 둘 다 성립하면 체포가 우선.

플레이어 판정(엑시트/영웅) : `EventHub.OnExitRequested`

↓

MarketManager.HandleExitRequested : `CanExit`(현금 >= TargetAsset) 확인 → 실패 시 무시

↓ 성공

`Doubt <= 50 && Support >= 80`이면 영웅 엔딩, 아니면 엑시트 엔딩

어느 쪽이든 확정되면 `MarketManager.EndGame(EndingType)`이 `IsGameOver = true` 설정, `TimeManager.PauseGame()`
호출, `EventHub.RaiseGameEnded(ending)` 발행을 수행한다. `IsGameOver`가 true인 동안 `NextTurn()`은 아무것도
하지 않는다.

---

# 역할

## TimeManager

게임 시간을 관리한다.

---

## MarketManager

시장 계산을 수행한다. `CurrentStat`(Support/Growth/Supply/Doubt 포함)을 보유하며, 거래·시사 이벤트는 이 값에 직접
반영된다. `NextTurn()`에서 `NewsEventIntervalTurns`(30)턴마다 `NewsEventChance`(40%) 확률로 시사 이벤트를 자동
발생시킨다. `[SerializeField] List<EventSO> eventDatabase`(`SkillManager.skillDatabase`와 동일한 패턴)를 들고
있으며, 자동/수동(`EventHub.OnNewsEvent`) 두 경로 모두 내부 `TriggerNewsEvent()`를 거쳐
`EventCalculator.Calculate(CurrentStat, eventDatabase)`를 호출한다. `EventCalculator`는 먼저
`PositiveEventRate`/`NegativeEventRate`로 `Positive`/`Negative` 카테고리를 정하고, 그 카테고리에 속한 `EventSO`
중 하나를 `weight` 가중치 랜덤으로 골라 그 SO에 authored된 값(`effects`의 Support/Growth/Doubt, `supplyDelta`,
`priceRatio`)을 그대로 적용한다. 실제로 발생했으면 `RuntimeEventData`에 기록되고 `MarketManager.EventLog`로
노출된다.

`EventHub.OnManipulateSupply`도 구독한다. `SkillManager.IsUnlocked(SkillID.추가발행권한)`이 true일 때만
`TradeCalculator.ManipulateSupply(CurrentStat, amount)`를 호출한다 (현금 비용 없음, Long/Short와 달리
PlayerManager를 거치지 않는다).

게임 종료(엔딩) 판정도 담당한다. `IsGameOver`(게임 종료 여부), `CanExit`(현금이 `TargetAsset`(10억) 이상인지,
엑시트 버튼 활성화 조건)를 외부에 노출한다. 매 턴 자동으로 체포(`Doubt>=100`)/거지(현금·코인 모두 0) 엔딩을
확인하고, `EventHub.OnExitRequested`를 받으면 `CanExit`을 만족할 때만 Doubt/Support 기준으로 영웅/엑시트 엔딩을
확정한다. 엔딩이 확정되면 `TimeManager.PauseGame()`으로 게임을 멈추고 `EventHub.OnGameEnded`를 발행한다.

---

## SkillManager

스킬 구매 및 활성화를 관리한다. `SkillSO.isReusable`로 스킬을 두 종류로 나눠 처리한다.

- 재사용형(`isReusable == true`) : 아이콘 클릭(`OnSkillClicked`)은 `RuntimeSkillData.SelectedSkillId`에 선택만
  저장하고 끝난다. 구매 버튼(`OnSkillPurchased`, 인자 없음) 클릭 시 `SelectedSkillId`로 스킬을 조회해 구매와
  사용이 함께 일어난다. `PlayerManager.TrySpend`로 비용(`baseCost × costMultiplier^PurchaseCount`)을 결제하고
  성공하면 `StatCalculator.ApplySkillUse`로 `CurrentStat`에 Support/Growth를 직접 1회 반영한다. 잠기는 단계가
  없어 비용만 내면 계속 다시 구매할 수 있다 (`defaultUnlocked`와 무관하게 최초 구매도 결제 필요).
  감쇠는 Trade/Job과 동일하게 자동 처리된다.
- 재사용 불가(`isReusable == false`, 예: `ExitUnlock`) : 기존 토글(On/Off) 방식 그대로, 활성 상태인 동안
  `StatCalculator.ApplySkills`가 매 턴 재적용한다. 감쇠 대상이 아니다.

`IsUnlocked(SkillID id)`로 특정 스킬을 구매/해금했는지 다른 Manager가 조회할 수 있다 (예: `MarketManager`가
발행량 조작 버튼 사용 가능 여부를 판단할 때 사용). 스킬 정보 패널 UI를 위해 `GetSkillProfile(SkillID id)`(해당
스킬의 `SkillSO` 반환, description/효과 등 정적 데이터 조회용)와 `GetCurrentCost(SkillID id)`(구매 횟수가
반영된 실제 현재 비용 반환)도 제공한다.

---

## JobManager

현재 직업을 관리한다.

---

## PlayerManager

플레이어 자산과 보유 코인을 관리한다. `TrySpend(amount)`로 잔액이 충분할 때만 차감하고 성공 여부를 반환한다
(스킬 재구매 등 실패 가능한 지출에 사용).

---

# 설계 원칙

- UI는 EventHub만 호출한다.
- Manager는 EventHub를 구독하여 상태를 관리한다.
- RuntimeData는 현재 상태를 저장한다.
- Calculator는 계산만 수행한다.
- UI는 RuntimeData를 읽는다.

---
