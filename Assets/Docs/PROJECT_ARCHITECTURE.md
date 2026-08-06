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

# 씬 구조

`Assets/Scenes/`에 4개 씬이 있고, 전환은 전부 `GameSceneManager`(static, `Assets/Scripts/manager/utils/GameSceneManager.cs`)
한 곳을 거친다 — 씬 이름 문자열을 여기 한 곳에만 두기 위함이다 (EventHub가 Manager 호출을 한 곳에 모으는 것과
같은 이유).

TitleScene (타이틀)

↓ TitleScreenUI "게임 시작"

CharacterSelectScene (직업 선택)

↓ CharacterSelectUI "다음으로" (선택한 JobSO를 JobSelectionHandoff에 저장)

SampleScene (메인 게임, `GameSceneManager.MainScene`)

↓ 체포/거지 엔딩 확정 시에만 (EndingResultUI가 EndingHandoff에 엔딩 타입을 저장)

EndingScene (배드엔딩 전용)

↓ "타이틀로 돌아가기"

TitleScene

영웅/엑시트 엔딩은 씬을 옮기지 않고 `SampleScene` 안에서 `EndingResultUI`가 오버레이 패널로 보여준다 (5장
"엔딩 판정" 참고).

## 씬 간 데이터 전달 (Handoff)

씬을 넘어가며 값을 들고 가야 할 때, `DontDestroyOnLoad` 오브젝트를 새로 만드는 대신 정적(static) 필드 하나만
쓰는 가벼운 "handoff" 클래스를 쓴다 (둘 다 `Assets/Scripts/manager/`).

- `JobSelectionHandoff.SelectedJob` (`JobSO`) : `CharacterSelectUI`가 저장 → `JobManager.Start()`가 읽고 즉시
  null로 비운다.
- `EndingHandoff.Ending` (`EndingType?`) : `EndingResultUI`가 체포/거지 확정 시 저장 → `EndingSceneUI.Start()`가
  읽는다 (null이면 `Broke`로 폴백).

## DontDestroyOnLoad 싱글톤

씬이 바뀌어도 파괴되지 않는 6개 오브젝트가 있다 (전부 `Assets/Scripts/manager/`) : `TimeManager`, `MarketManager`,
`SkillManager`, `JobManager`, `PlayerManager`(이상 5개는 아래 "Manager" 절 참고), 그리고 `AudioManager`
(`utils/AudioManager.cs`, BGM 크로스페이드 + SFX 재생 — `GameSceneManager`/`EventHub`처럼 게임 상태가 아닌
유틸 성격이라 5개 Manager와는 별도로 취급한다).

`Awake()`가 씬 전환마다 재실행되지 않으므로, 새 게임을 시작할 때(`CharacterSelectUI`)는 각 Manager의 리셋
메서드를 명시적으로 호출해 이전 플레이 상태를 지운다. 마찬가지로 `TimeManager`(일시정지 timeScale)와
`AudioManager`(재생 중이던 BGM)도 씬을 벗어날 때 스스로 초기화하지 않으므로, 타이틀로 돌아가는 지점들
(`EndingSceneUI.OnBackToTitle`, `SettingsUI.QuitGame`)이 각각 `TimeManager.SetTimeScale(1f)` +
`AudioManager.StopBGM()`을 직접 호출해 정리한다.

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
- OnSkillPurchaseSucceeded : 스킬이 실제로 결제·적용에 성공했을 때만 발행, `SkillID` 전달 (SkillManager 발행) — 클릭만 하면 항상 뜨는 `OnSkillPurchased`와 달리 성공 시점 VFX/SFX 트리거용
- OnJobSelected : 직업 선택 요청 (JobManager 구독)
- OnBuyCoin / OnSellCoin : 코인 매수/매도 요청 (PlayerManager, MarketManager 구독)
- OnManipulateSupply : 발행량 조작 요청, 양수/음수로 증가·감소 (MarketManager 구독) — `추가발행권한` 스킬을 구매하기 전에는 무시된다
- OnMarketUpdated : 시장 계산 완료 후 UI 갱신 (MarketManager 발행)
- OnEventTriggered : 시사 이벤트가 실제로 발생했을 때 발행, `EventLogEntry` 전달 (MarketManager 발행) — 메인 화면 알림 토스트(`EventNotificationUI`)가 구독
- OnGamePaused / OnGameResumed : 거래/발행량 조작 등 게임 시간을 멈춰야 하는 모달이 열리고 닫힐 때 발행 (TimeManager 구독) — `TradeModalUI`/`CoinControlModalUI`/`EventLogPanelUI`의 `Open()`/`Close()`가 발행
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
- RuntimePriceHistory

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

`PlayerStat.PriceChangeThisTurn`/`StreamerReaction`은 이번 턴 `CurrentPrice`가 실제로 얼마나/어느 방향으로
움직였는지를 나타내는 UI 표시 전용 값으로, 감쇠·이월 없이 매 턴(또는 시사 이벤트 수동 트리거 시점) 새로
계산된다. 스트리머 패널 UI가 `StreamerReaction`(`StreamerReactionState` 5단계)을 읽어 표정/멘트를 바꾸는 데
쓰도록 만들었다 (2-1장 참고). 별도 `EventHub` 이벤트 없이 기존 `OnMarketUpdated`가 나르는 `PlayerStat`에 이미
포함된다.

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
- StreamerReactionCalculator
- EndingCalculator

Calculator는 상태를 변경하지 않고 계산만 수행한다. `EndingCalculator`는 체포/거지(`CheckAutomatic`)와
영웅/엑시트(`CheckExit`) 판정을 순수 함수로 제공하며, 실제 게임 종료 처리(`IsGameOver`, `TimeManager.PauseGame`,
`EventHub.RaiseGameEnded`)는 여전히 `MarketManager.EndGame()`이 담당한다 (MarketManager 책임 분리 리팩토링,
`Completed_Tasks.md` 참고).

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

자동 판정(체포/거지) : `MarketManager.NextTurn()`의 `EventHub.OnMarketUpdated` 발행 직후(거래·발행량 조작
직후에도 동일하게) `CheckAutomaticEndings()`가 확인한다. `Doubt >= 100` → 체포, 아니면
`(현금 == 0 && 코인 == 0)` 또는 가격이 상폐 기준(`MarketManager.DelistingPriceThreshold`) 이하로
`EndingCalculator.PriceFloorStreakLimit`턴 연속 방치 → 거지. 둘 다 성립하면 체포가 우선.

플레이어 판정(엑시트/영웅) : `EventHub.OnExitRequested`

↓

MarketManager.HandleExitRequested : `CanExit`(현금 >= TargetAsset) 확인 → 실패 시 무시

↓ 성공

`Doubt <= 50 && Support >= 80`이면 영웅 엔딩, 아니면 엑시트 엔딩

어느 쪽이든 확정되면 `MarketManager.EndGame(EndingType)`이 `IsGameOver = true` 설정, `TimeManager.PauseGame()`
호출, `EventHub.RaiseGameEnded(ending)` 발행을 수행한다. `IsGameOver`가 true인 동안 `NextTurn()`은 아무것도
하지 않는다.

### 엔딩별 화면 전환

`EndingResultUI`(`SampleScene`)가 `OnGameEnded`를 구독해 4종을 나눠 처리한다.

- **영웅/엑시트** : 씬을 옮기지 않고 그 자리에서 전체화면 오버레이 패널을 띄운다.
- **체포/거지** : `StatGaugeUI.ArrestCollapseDuration`만큼(게이지 패널이 무너지는 연출) 기다린 뒤, 패널을
  띄우는 대신 `EndingHandoff.Ending`에 타입을 저장하고 `GameSceneManager.LoadEnding()`으로 전용
  `EndingScene`으로 넘어간다. `EndingSceneUI`가 체포/거지에 맞는 배경 이미지와 문구를 띄우고(암전 페이드
  연출 포함), "타이틀로 돌아가기" 버튼이 `TimeManager.SetTimeScale(1f)` + `GameSceneManager.LoadTitle()`로
  루프를 닫는다 (씬 구조 절 참고).

---

# 역할

## TimeManager

게임 시간을 관리한다.

---

## MarketManager

시장 계산을 수행한다. 게임 시작 시 `CurrentStat.CurrentPrice`를 `InitialPrice`(10)로 초기화하며,
`PriceCalculator.MinPrice`(1)가 가격 하한선을 강제한다 (Game_Formula.md 2장 참고). `CurrentStat`(Support/Growth/Supply/Doubt 포함)을 보유하며, 거래·시사 이벤트는 이 값에 직접
반영된다. `NextTurn()`에서 `NewsEventIntervalTurns`(30)턴마다 `NewsEventChance`(40%) 확률로 시사 이벤트를 자동
발생시킨다. `[SerializeField] List<EventSO> eventDatabase`(`SkillManager.skillDatabase`와 동일한 패턴)를 들고
있으며, `NextTurn()`이 내부 `TriggerNewsEvent()`를 거쳐
`EventCalculator.Calculate(CurrentStat, eventDatabase)`를 호출한다 (수동 트리거 경로는 없음 — 자동 발생만
존재). `EventCalculator`는 먼저
`PositiveEventRate`/`NegativeEventRate`로 `Positive`/`Negative` 카테고리를 정하고, 그 카테고리에 속한 `EventSO`
중 하나를 `weight` 가중치 랜덤으로 골라 그 SO에 authored된 값(`effects`의 Support/Growth/Doubt, `supplyDelta`,
`priceRatio`)을 그대로 적용한다. 실제로 발생했으면 `RuntimeEventData`에 기록되고 `MarketManager.EventLog`로
노출된다.

`EventHub.OnManipulateSupply`도 구독한다. `SkillManager.IsUnlocked(SkillID.추가발행권한)`이 true일 때만
`TradeCalculator.ManipulateSupply(CurrentStat, amount)`를 호출한다 (현금 비용 없음, Long/Short와 달리
PlayerManager를 거치지 않는다).

매 턴마다 계산 전후 `CurrentPrice` 차이로 `PriceChangeThisTurn`을 구해
`StreamerReactionCalculator.Calculate()`로 `StreamerReaction`을 갱신한다 (2-1장 참고).

매 턴(`NextTurn()`)마다 그 턴의 가격 캔들(`PricePoint` — Open=턴 시작 전 가격, Close=턴 계산 후 가격,
Date=`TimeManager.CurrentGameDate`)을 `RuntimePriceHistory`에 기록하고, `MarketManager.PriceHistory`
(`IReadOnlyList<PricePoint>`)로 노출한다 (1턴=1개 일별 `PricePoint`, 2장 "캔들 차트" 참고). 캔들 차트 UI
(`PriceChartUI`)는 이 일별 리스트를 7일씩 모아 캔들(주봉) 1개로 집계해서 최근 N개(주 단위)만 읽어 그린다 —
집계는 UI 쪽에서만 하고 `MarketManager`/`RuntimePriceHistory`의 기록 방식 자체는 바뀌지 않는다.

게임 종료(엔딩) 판정도 담당한다. `IsGameOver`(게임 종료 여부), `CanExit`(현금이 `TargetAsset`(5억) 이상인지,
엑시트 버튼 활성화 조건)를 외부에 노출한다. 매 턴 자동으로 체포(`Doubt>=100`)/거지(현금·코인 모두 0, 또는
가격이 상폐 기준 이하로 일정 턴 연속 방치) 엔딩을 확인하고, `EventHub.OnExitRequested`를 받으면 `CanExit`을
만족할 때만 Doubt/Support 기준으로 영웅/엑시트 엔딩을 확정한다. 어떤 엔딩인지 "판정"하는 순수 로직은 `EndingCalculator.CheckAutomatic`/`CheckExit`이 맡고,
`MarketManager`는 그 결과를 받아 `EndGame()`으로 `IsGameOver` 설정, `TimeManager.PauseGame()`,
`EventHub.RaiseGameEnded` 발행 등 실제 상태 변경만 수행한다.

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

# UI

`Assets/Scripts/UI/`는 기능별 하위 폴더로 나뉜다.

- (루트) : `TitleScreenUI`, `CharacterSelectUI` — 타이틀/직업 선택 화면 (각각 별도 씬).
- `MainModal/` : `SampleScene`의 상시 HUD — 가격 헤더(`CoinPriceHeaderUI`), 스트리머 반응 패널
  (`StreamerPanelUI`), 자산/거래 패널(`PlayerUI`), Support/Growth/Doubt 게이지(`StatGaugeUI`), 인게임 엔딩
  오버레이(`EndingResultUI`).
- `FeatherModal/` : 매수/매도 모달(`TradeModalUI`), 발행량 조작 모달(`CoinControlModalUI`).
- `ChartModal/` : 캔들 차트 및 하위 렌더러(`PriceChartUI`/`PriceChartCandles`/`PriceChartGrid`/
  `PriceChartViewport`/`PriceChartTooltip`/`PriceChartMovingAverage`/`PriceChartPeriodToggle`).
- `SkillModal/` : 스킬 구매 패널(`SkillPanelUI`).
- `EventModal/` : 시사 이벤트 관련 — 개요(`EventOverviewUI`), 카드(`EventCardView`), 알림 토스트
  (`EventNotificationUI`), 이력 패널(`EventLogPanelUI`).
- `EndingScene/` : 배드엔딩 전용 씬의 UI(`EndingSceneUI`, "씬 구조" 절 참고).
- `Utils/` : 공용 소품 — 숫자 포맷(`UIFormat`), 버튼 눌림 효과(`ButtonPressEffect`), 이벤트 효과 문구 포맷
  (`EventEffectFormatter`), 모달 열림 시 게임 일시정지(`ModalPause`), 민트 버튼(`MintButtonUI`), 설정 메뉴
  (`SettingsUI`), 스킬/이벤트로그 진입 버튼(`SkillPanelButton`/`EventLogButton`), 시계 표시(`TimeUI`).
- `Vfx/` : 순수 연출용 — `HoverIdleBob`(호버 중 또는 상시 재생되는, 위아래로 둥둥 뜨는 idle 애니메이션.
  `AddComponent`로 아무 UI 오브젝트에나 붙여 쓴다), `UIBurstParticle`(파티클 시스템 없이 uGUI만으로 만드는
  버스트 이펙트 — `manager/utils/ScreenShaker`도 함께 트리거해 화면을 흔든다).

---

# 설계 원칙

- UI는 EventHub만 호출한다.
- Manager는 EventHub를 구독하여 상태를 관리한다.
- RuntimeData는 현재 상태를 저장한다.
- Calculator는 계산만 수행한다.
- UI는 RuntimeData를 읽는다.

---
