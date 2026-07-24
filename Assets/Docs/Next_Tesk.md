# 다음 작업

## 진행 상태

- **완료** : Support/Growth 감쇠. `PlayerStat.Support`/`Growth`가 `CurrentPrice`처럼 턴을 넘어 유지되는 값이 되었고,
  매 턴 `TradeCalculator.Decay(stat)`로 감쇠한다. Job 선택/거래(Long·Short)는 발생하는 순간 `MarketManager.CurrentStat`에
  직접 반영된다. 별도 누적 데이터 클래스(`RuntimeTradeData`, `JobBoostData`)는 시도했다가 불필요해서 제거했다.
  (`Logging.md` "Support/Growth 감쇠 (Decay) — 최종 구조" 참고, 공식은 `Game_Formula.md` 3장 / 3-1장)
- **완료** : Skill 재사용 시스템. `SkillSO.isReusable` 플래그로 재사용형(Support/Growth 부스트, 사용 시 1회 반영 후
  재구매 필요) / 재사용 불가(토글형, 감쇠 없음)를 구분한다. (`Logging.md` "Skill 재사용 시스템" 참고)
- **완료** : 시사 이벤트(`EventCalculator`) 및 발행량(Supply)/Scarcity. 이벤트 발생 시 Support/Growth와 동일한
  "직접 반영 → 매 턴 감쇠" 패턴으로 Supply/가격까지 반영하도록 구현했다 (이후 "발행량 조작" 작업으로 Supply의
  소스가 이벤트 하나만은 아니게 확장됨, 아래 참고). `Doubt`도 이벤트가 건드리게 됐지만, Doubt는 Support/Growth와
  달리 **감쇠하지 않는 값**이라(시간이
  지날수록 자동으로 100을 향해 오르다 100이 되면 게임오버가 되는 기획) Job/Skill의 `DoubtDecrease`는 기존 "활성
  상태인 동안 매 턴 재적용" 방식 그대로 유지했다. 이벤트가 반영한 Doubt가 다음 턴에 사라지지 않도록
  `StatCalculator.Calculate()`가 Doubt를 턴 사이 이월하도록만 고쳤다. (`Logging.md` "Doubt(의심도)도 이벤트에
  포함" 참고, 공식은 `Game_Formula.md` 2장 / 3장 / 3-1장 / 3-2장 / 4장)
- **완료** : EventSO 도입. `EventCalculator`에 하드코딩돼 있던 등급 상수를 `EventSO` ScriptableObject로 뺐다.
  이벤트마다 `message`(로그 문구), `category`(`Positive`/`Negative`), `weight`(같은 카테고리 안 가중치 랜덤),
  `effects`(`List<EffectData>` — Support/Growth/Doubt 등 원하는 스탯만 부호 있는 값으로 지정, Job/Skill과 동일한
  구조 재사용), `supplyDelta`/`priceRatio`(부호 포함, 이벤트 전용 필드)를 갖는다. `EventCalculator.Calculate`는
  먼저 `PositiveEventRate`/`NegativeEventRate`로 카테고리를 정하고, 그 카테고리 안에서만 `weight` 가중치 랜덤으로
  하나를 뽑아 값을 그대로(부호 변환 없이) 적용한다. `MarketManager`가 `SkillManager.skillDatabase`와 동일한
  패턴으로 `[SerializeField] List<EventSO> eventDatabase`를 들고, 발생한 이벤트는 `MarketManager.EventLog`
  (`RuntimeEventData`/`EventLogEntry`, 날짜+SO 참조)에 기록된다. (`Logging.md` "EventSO 도입" 참고, 공식은
  `Game_Formula.md` 4장)
- **완료** : 발행량 조작 액션 + EffectType 직렬화 버그 수정. Long/Short와 동일한 구조로 `EventHub.OnManipulateSupply(amount)`
  → `TradeCalculator.ManipulateSupply`를 추가했다 (현금 비용 없음, Support/Growth는 Long/Short와 반대 부호,
  Doubt는 `amount`의 절대값에 비례해 증가). `추가발행권한` 스킬을 구매(`SkillManager.IsUnlocked`)하기 전에는
  `MarketManager`가 무시한다. 이 과정에서 `EffectType`에 `DoubtIncrease`를 **중간 삽입**해 `추가발행권한.asset`의
  `effectType: 5`(원래 `CashBonus`)가 `NegativeEventRate`로 조용히 바뀌어 있던 걸 발견해 수정했다 — enum은
  항상 끝에만 추가해야 한다는 원칙을 세웠다. `SupplyIncrease`/`SupplyDecrease`를 `EffectType`에 추가하고
  `StatCalculator.ApplySkillUse`가 재사용형 스킬 구매 시 이를 반영하도록 했다 (Job/토글형 스킬 매 턴 재적용
  루프에서는 Supply 관련 타입을 방어적으로 제외 — 감쇠와 충돌해 무한 증가하는 걸 막기 위해, Doubt 때와 동일한
  이유). (`Logging.md` "발행량 조작 기능 추가" 참고, 공식은 `Game_Formula.md` 3장 "발행량 조작")
- **완료** : Doubt 자동 상승 + 엔딩 시스템(4종). 게임 시간 2년(730턴)째 Doubt +20, 이후 매 턴 +0.5씩 자동 증가
  (`MarketManager.ApplyDoubtAutoRise`, 감쇠 없이 누적). 엔딩 4종을 구현했다 — 체포(Bad, `Doubt>=100`, 자동),
  거지(현금·코인 모두 0, 자동, 결과 서사 미정), 엑시트(Neutral)/영웅(True)은 새 `EventHub.OnExitRequested` 버튼으로
  플레이어가 트리거하며 `MarketManager.CanExit`(현금 >= `TargetAsset`=10억)을 만족해야 하고, 그 시점의
  `Doubt<=50 && Support>=80`이면 영웅, 아니면 엑시트로 갈린다. 코인 보유량과 `PlayerStat.ExitUnlocked`(기존 스킬
  해금 플래그)는 이 판정과 무관하다. 자동 판정 두 개가 같은 턴에 겹치면 체포가 우선. 엔딩 확정 시
  `TimeManager.PauseGame()`으로 정지하고 `EventHub.OnGameEnded(EndingType)`를 발행하며, 체포/거지 엔딩도
  `PlayerManager`의 실제 현금/코인 수치는 건드리지 않는다. (`Logging.md` "Doubt 자동 상승 + 엔딩 시스템" 참고,
  공식은 `Game_Formula.md` 3장/5장)
- **완료** : Supply 스케일 재검토. `MaxSupply = 20000`(코드)이 실제 기준값이고, UI 스크린샷의 "20,000,000"은
  `TargetAsset` 때와 동일하게 예시/목업 수치였음을 확인했다. 코드 변경 없음, 앞으로 Supply 관련 수치(이벤트
  `supplyDelta`, 발행량 조작 버튼 `amount`, 스킬 `SupplyIncrease`/`SupplyDecrease`)는 이 스케일 기준으로 정한다.
  (`Logging.md` "Supply 스케일 재검토" 참고, 공식은 `Game_Formula.md` 2장)
- **완료** : EventSO 데이터 작성 + Inspector 연결. 긍정 3개(스트리머_소개/거래소_상장/규제_완화), 부정 3개
  (당국_조사/해킹_사고/인플루언서_폭로) 예시 이벤트를 `Assets/Scripts/Profile/이벤트_프로파일/`에 만들고
  `Managers` GameObject의 `MarketManager.eventDatabase`에 6개 모두 등록했다 (Unity MCP로 작업, 씬 저장 완료).
  Positive/Negative 각 카테고리에 최소 1개씩 있어 이제 시사 이벤트가 실제로 발생한다. (`Logging.md` "EventSO
  예시 데이터 작성" 참고)
- **완료** : 시사 이벤트 수동 트리거 가격 미반영 버그 수정. `MarketManager.HandleNewsEvent()`가
  `EventHub.RaiseMarketUpdated`를 안 불러서 수동 트리거 시 가격 변화가 즉시 안 보이던 문제를 한 줄로 수정.
  (`Logging.md` "시사 이벤트 수동 트리거가 가격 변화를 즉시 반영하지 않던 문제 수정" 참고)
- **완료** : 스킬 정보 패널용 조회 훅 추가. `SkillManager.GetSkillProfile(id)`(SkillSO 반환)와
  `GetCurrentCost(id)`(구매 횟수 반영된 현재 비용)를 추가해, UI가 선택된 스킬의 정보 패널(설명/현재 Cost)을
  그릴 수 있게 됐다. 직업 선택 화면은 UI가 이미 들고 있는 `JobSO` 참조로 바로 그릴 수 있어 추가 작업 불필요함을
  확인. (`Logging.md` "스킬/직업 정보 패널을 위한 UI 훅 정리" 참고)
- **완료** : 개요 화면 Job+Skill 보너스 표시. `PlayerStat`에 `JobSkillSupportBonus`/`JobSkillGrowthBonus`를
  추가해, Trade/이벤트는 제외하고 Job 선택·재사용형 Skill 구매가 준 몫만 Support/Growth와 동일하게 감쇠시키며
  별도 추적한다. Doubt 쪽은 감쇠가 없는 값이라 `JobManager.CurrentJob.effects` + `SkillManager.GetActiveSkills()`를
  그대로 합산해서 보여주면 되므로 코드 변경 없음. (`Logging.md` "개요 화면 Job+Skill 보너스 표시 구현" 참고,
  공식은 `Game_Formula.md` 3장 "누적치 감쇠")
- **완료(이후 재설계됨)** : CashBonus를 실제 현금 증가로 처음 연결했을 때는 `MarketManager.ApplyCashBonus()`로
  매 턴 `currentMoney`에 %를 곱하는 방식이었는데, "이자와 다를 게 없다"는 지적을 받고 아래 두 항목으로
  대체됐다. `ApplyCashBonus()`는 삭제됨 — 남아있지 않음.
- **완료** : 재사용형 스킬 CashBonus 수정. 구매해도 실제로는 단 한 턴도 현금 증가에 기여하지 못하던 버그를
  발견해 수정 — `SkillManager.HandlePurchase()`가 구매 즉시 `currentMoney × (CashBonus/100)`을 바로 지급하도록
  바꿨다 (Support/Growth와 동일한 "구매 시점 1회성" 철학). (`Logging.md` "재사용형 스킬의 CashBonus가 실제로는
  0턴도 반영 안 되던 문제 수정" 참고)
- **완료** : Job/토글형 스킬 CashBonus 재설계. 매 턴 보유 현금 전체에 곱연산하던 방식(사실상 이자)을 폐기하고,
  코인을 팔 때(Short) 받는 수익에 배율로 붙는 방식으로 바꿨다 — `Revenue = Amount × CurrentPrice × (1 +
  CashBonus/100)`. 거래를 해야만 체감되는 버프가 됐다. (`Logging.md` "Job/토글형 스킬 CashBonus 재설계" 참고,
  공식은 `Game_Formula.md` 3장 "Short"/3-3장)
- **완료** : 초반 이벤트 무조건 발생. `EventSO.guaranteedTurn` 필드를 추가해 확률 없이 특정 턴에 반드시
  발생하도록 만들고, `스트리머_소개`(1턴)/`거래소_상장`(2턴)을 순차 배정했다. 스트리머 UI(캐릭터+가짜 채팅
  패널)는 `MarketManager.EventLog`로 이미 조회 가능해 코드 변경 불필요. (`Logging.md` "초반 이벤트 무조건 발생 +
  스트리머 UI 힌트" 참고, 공식은 `Game_Formula.md` 4장)
- **남음** : 아래 참고.

---

## 후보 : 거지 엔딩 결과 서사 정의

체포/엑시트/영웅 엔딩은 결과 서사(수사·체포/해외 도피/합법적 운영 등)가 있는데, 거지 엔딩은 트리거 조건(현금 0
+ 코인 0)만 정해졌고 결과 설명 문구가 아직 없다. 나머지 세 엔딩과 톤을 맞춰 확정해야 한다.

---

## 후보 : UI 연결

`EventHub`의 이벤트 대부분은 Manager 쪽 구독 로직만 갖춰져 있고, 이를 발행하는 실제 UI가 아직 없다.

- Long/Short 거래 버튼 (`EventHub.RaiseBuyCoin`/`RaiseSellCoin`)
- 발행량 조작 버튼 (`EventHub.RaiseManipulateSupply`) — `SkillManager.IsUnlocked(SkillID.추가발행권한)`가 false면
  버튼을 비활성화/숨김 처리해야 한다 (게임 로직은 이미 막고 있지만 UI에서도 표시해줘야 함)
- 스킬 아이콘/구매 버튼 (`EventHub.RaiseSkillClicked`/`RaiseSkillPurchased`)
- 직업 선택 화면 (`EventHub.RaiseJobSelected`)
- 시사 이벤트 수동 트리거가 필요한 경우의 UI (`EventHub.RaiseNewsEvent`) — 자동 발생은 이미 `MarketManager`에 구현됨
- 이벤트 로그 패널 — `MarketManager.EventLog`(`IReadOnlyList<EventLogEntry>`)를 순회하며 `Profile.message`/`Date`/
  `Profile.effects`를 표시. 데이터는 이미 쌓이고 있으니 UI만 그리면 된다.
- 엑시트 버튼 (`EventHub.RaiseExitRequested`) — `MarketManager.CanExit`(현금 >= `TargetAsset`=10억)가 true일 때만
  누를 수 있도록 활성화 처리. 목표 금액 진행률 표시(스크린샷의 "목표금액/현재금액/목표까지 남은 금액")도 이
  값들을 그대로 읽으면 된다.
- 엔딩 결과 화면 — `EventHub.OnGameEnded(EndingType)`을 구독해 4종 엔딩(체포/엑시트/영웅/거지)에 맞는 결과 문구를
  표시. 각 엔딩의 설명 텍스트는 `Game_Formula.md` 5장에 정리되어 있음.

`TimeUI`, `PlayerUI`, `SettingsUI`만 기존처럼 `TimeManager`/`PlayerManager`를 직접 참조하는 상태이고, 나머지는
설계는 끝났으나 화면이 없다.

## 후보 : 발행량 관련 스킬 3종에 실제 Supply 효과 부여

`추가발행권한`/`우회발행권한`/`발행량은폐` 세 스킬은 이름과 설명(예: "위기 상황에서 자금을 빠르게 마련",
"발행 사실이 드러나더라도 의심을 최소화")으로 미루어 보면 발행량과 강하게 연관되어 있지만, 지금 `.asset`
데이터에는 `SupplyIncrease`/`SupplyDecrease` 효과가 하나도 없다 (`추가발행권한`은 발행량 조작 버튼의 해금
조건 역할만 하고 있음). 아래를 정해야 한다.

- `추가발행권한`/`우회발행권한`을 구매하는 순간에도 (버튼 해금과 별개로) Supply를 직접 늘리는 1회성 효과를
  줄 것인지, 아니면 순수하게 "버튼 해금 + 기존 효과(Growth/CashBonus/DoubtDecrease)"만으로 끝낼 것인지.
- `발행량은폐`는 설명상 Supply 자체보다는 "정보를 숨긴다"는 쪽이라 `DoubtDecrease`만으로 충분해 보이는데,
  이대로 유지할지 확인 필요.
- 만약 Supply 효과를 추가한다면 구체적 수치도 함께 정해야 한다.
