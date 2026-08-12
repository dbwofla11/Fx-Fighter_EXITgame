# 완료 작업

`Next_Tesk.md`에 있던 완료 항목을 분리한 문서. 각 항목의 자세한 배경/설계 과정은 `Logging.md`, 공식은
`Game_Formula.md`를 참고. 새로 완료되는 작업은 이 문서 맨 아래에 이어서 추가한다.

---

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
- **완료** : 스트리머 반응(가격 변화 연동) 로직. `PlayerStat.PriceChangeThisTurn`/`StreamerReaction`
  (`StreamerReactionState` 5단계 : Crash/Down/Neutral/Up/Surge)을 추가하고, `MarketManager`가 매 턴(및 시사
  이벤트 수동 트리거 시점) 계산 전후 `CurrentPrice` 차이로 갱신한다. `StreamerReactionCalculator`가 절대값
  delta 기준(퍼센트 아님)으로 5단계를 판정한다. 새 `EventHub` 이벤트 없이 기존 `OnMarketUpdated`로 이미 나간다.
  (`Logging.md` "스트리머 반응(가격 변화 연동) 로직 설계" 참고, 공식은 `Game_Formula.md` 2-1장)
- **완료** : 캔들스틱 가격 차트 UI. 캔들 1개 = 턴(하루) 1개(Open=턴 시작 전 가격, Close=턴 계산 후 가격)로
  정의하고, `MarketManager.PriceHistory`(신규 `RuntimePriceHistory`/`PricePoint`)에 매 턴 기록한다.
  `Assets/Scripts/UI/PriceChartUI.cs`가 `EventHub.OnMarketUpdated`를 구독해 최근 14개만 오브젝트 풀링으로
  그리고, Y축은 자동 스케일링한다. 씬 `Main_Canvas` 하위에 `ChartPanel` GameObject를 배치했다 (좌표는
  `Scene_Hierarchy.md` 참고). Play 모드에서 실제 동작 확인 완료. (`Logging.md` "캔들스틱 가격 차트 UI 구현"
  참고, 공식은 `Game_Formula.md` 2-2장)
- **완료** : 차트 모서리 둥글게 + 코인명/현재가 헤더. `ChartPanel`에 다른 패널과 동일한
  `ImageWithRoundedCorners`(radius 30)를 적용했다. 목업의 좌측 상단 코인명/가격 표시를 위해
  `Assets/Scripts/UI/CoinPriceHeaderUI.cs`(신규)를 만들어 `EventHub.OnMarketUpdated`로 실시간 가격을
  "BitBitCoin(BBIT)  ₩1,000" 형식(기존 관례에 맞춰 $ 대신 ₩ 사용)으로 표시한다. 헤더 자리 확보를 위해
  `ChartPanel` 세로 크기를 줄였다. (`Logging.md` "후속 요청 : 코인명/가격 헤더 + 차트 모서리 둥글게" 참고)
- **완료** : 캔들 간격 좁히기. `PriceChartUI.candleWidthRatio`를 0.6 → 0.85 → 0.95로 올려 옆 캔들과의 틈을
  목업처럼 좁혔다 (`visibleCandleCount`도 14 → 16으로 함께 조정, 씬 `ChartPanel` 값도 매번 동기화).
  (`Logging.md` "캔들 간격 좁히기" 참고)
- **완료** : 캔들 차트 후속 작업 — X축 날짜 라벨 + 호버 툴팁. `PriceChartUI`가 각 캔들 아래에 날짜
  라벨("MM/dd", 오브젝트 풀링)을 그리고, 캔들에 마우스를 올리면 좌상단에 "날짜 Open ₩x → Close ₩y" 툴팁을
  띄운다 (`CandleHoverTarget` 내부 클래스, `IPointerEnterHandler`/`IPointerExitHandler`, 캔들
  `raycastTarget`을 true로 변경). `DateTime.ToString("MM/dd")`가 시스템 문화권에 따라 "/"가 "-"로 바뀌는
  문제를 발견해 직접 포맷하는 `FormatDate()`로 고정했다. (`Logging.md` "캔들 차트 후속 작업" 참고)
- **되돌림** : 헤더 우측 햄버거 메뉴 버튼. 설정창을 여는 용도로 오해하고 만들었다가(`SettingsUI.Instance`,
  `HamburgerMenuButton.cs`), 실제로는 이벤트 로그 패널로 넘어가는 버튼이고 아이콘도 "UI뉴스아이콘"이어야
  한다는 지적을 받고 전부 원상복구했다. 상세 정보는 `Next_Tesk.md`의 "이벤트 로그 패널" 항목에 남겨뒀다.
  (`Logging.md` "헤더 우측 아이콘 — 햄버거 메뉴로 잘못 만들었다가 되돌림" 참고)
- **완료** : 이벤트 로그 패널 진입 버튼. `Assets/Scripts/UI/EventLogButton.cs`(신규)를 만들어 헤더 우측에
  `EventLogBtn` GameObject로 배치했다 (헤더 줄 우측 끝, 80x80). 클릭할 때마다 "UI뉴스아이콘_on"/
  "UI뉴스아이콘_off" 두 스프라이트를 토글한다. 처음 48x48로 만들었다가 목업 비율 대비 작다는 피드백을 받고
  80x80으로 키웠다. 이벤트 로그 패널 본체는 아직 씬에 없어서 패널을 실제로 열고 닫는 연결은 비워뒀다 —
  패널이 만들어지면 `Toggle()`에 이어서 붙이면 된다. (`Logging.md` "이벤트 로그 패널 진입 버튼" 참고)
- **완료** : 가격 상승 확률이 100%(`확률:1`)에 고정되는 버그 수정. `ProbabilityCalculator.CalculateScore`가
  `ws`/`wg` 가중치를 사실상 1.0으로 하드코딩하고 있어서, Support+Growth 합이 50만 넘어도 score가 1.0을 초과해
  `Clamp01`에 걸려버렸다 (decayRate=0.995로 감쇠가 느려 한 번 포화되면 수십~수백 턴 유지됨). `ws`/`wg`를 0.5로
  낮춰 포화 임계값을 합 100으로 올렸다 (`ProbabilityCalculator.SupportWeight`/`GrowthWeight` 상수 추가). 코드에는
  있었지만 문서에 누락돼 있던 Doubt 항(`wd`, 기존 동작 그대로 1.0)도 `Game_Formula.md` 1장에 반영했다. Play
  모드에서 `MarketManager.Instance.NextTurn()`을 반복 호출해 확률이 0.774 → 0.998 → 0.995 → 0.993처럼 자연스럽게
  변동/감쇠하는 것을 확인했다. (공식은 `Game_Formula.md` 1장)
- **완료** : Long/Short 거래 버튼 + Support/Growth/Doubt 게이지 연결. `TradePanel`의 `BtnPlus1/10/100/MAX`가
  하나의 거래 수량(`tradeAmount`, `TradeAmountText`)을 공유하도록 `PlayerUI.cs`(기존 파일)를 확장했다.
  `BtnPlusMAX`는 "현재 현금으로 살 수 있는 최대 수량"(`currentMoney / CurrentPrice`, Long 기준)으로 정의했고,
  `BtnLong`/`BtnShort`는 각각 `EventHub.RaiseBuyCoin`/`RaiseSellCoin`을 호출한 뒤 수량을 0으로 초기화한다.
  `PlayerManager.HandleBuyCoin`/`HandleSellCoin`이 잔액/보유량 검증 없이 그대로 반영하는 기존 구조라 마이너스로
  빠지지 않도록, UI 쪽에서 `BtnLong`은 결제 가능 금액(amount×price <= currentMoney), `BtnShort`는 보유 코인
  수량(amount <= currentCoins) 기준으로 매 프레임 `interactable`을 갱신한다. Support/Growth/Doubt 게이지
  (`SupportPanel`/`IncreaseScorePanel`/`DoubtScorePanel`)는 신규 `Assets/Scripts/UI/StatGaugeUI.cs`(공용
  컴포넌트, `EventHub.OnMarketUpdated` 구독)로 연결했다 — Support/Growth는 `PositiveBar`/`NegativeBar`(Filled
  Image, fillOrigin 좌/우) 양쪽을 값의 부호에 따라 채우고, Doubt는 `negativeBar`를 비워두면 `PositiveBar` 하나가
  전체 폭(0~100)을 채우도록 분기한다. 값 텍스트는 기존 씬의 "+0" placeholder 포맷을 따라 부호를 항상 표시한다
  (`+34`, `-12`, Doubt도 `+72`처럼 표시). Play 모드에서 거래 체결/수치 갱신/게이지 클램핑(극단값에서 fill이
  0~1로 정상 clamp)까지 확인했다. (공식/구조는 `Game_Formula.md` 3장, `PROJECT_ARCHITECTURE.md` 참고)
- **완료** : Support/Growth/Doubt가 문서 범위(-100~100, Doubt는 0~100)를 벗어나 찍히는 버그 수정. 거래
  (Long/Short/발행량 조작)·이벤트·스킬·직업 효과 등 여러 경로가 `PlayerStat.Support`/`Growth`/`Doubt`를 직접
  가감하는데, 어디에도 상한/하한을 강제하는 코드가 없어서 값이 문서 범위를 한참 벗어난 채로 UI에 그대로
  찍히고 있었다 (게이지 UI 클램프(`StatGaugeUI`, `Clamp01`)는 막대 길이만 0~1로 눌러줄 뿐 텍스트/실제 계산에는
  영향이 없어서 근본 원인이 아니었음). 신규 `StatCalculator.ClampStat(stat)`을 추가해 `MarketManager`가
  `EventHub.RaiseMarketUpdated`를 발행하기 직전(자동 턴 진행 `NextTurn()`과 수동 시사 이벤트 트리거
  `HandleNewsEvent()` 양쪽 모두, `ProbabilityCalculator`/`PriceCalculator`가 그 값을 읽기 전) 호출해 Support/
  Growth를 [-100, 100], Doubt를 [0, 100]으로 강제한다. Play 모드에서 비정상적으로 큰 매수/매도/발행량 조작을
  실행해 클램프가 정확히 걸리는지(Support/Growth=±100, Doubt=100에서 체포 엔딩까지 정상 발동) 확인했다.
  (공식은 `Game_Formula.md` 1장/2장/3장 — Support/Growth (-100~100), Doubt (0~100) 범위 정의 참고)
- **완료** : 200줄 넘는 스크립트 4개 책임별 정리 (동작 변경 없는 순수 리팩토링). `MarketManager`(263줄)의
  엔딩 판정(체포/거지/영웅/엑시트) 순수 로직만 신규 `Systems/EndingCalculator.cs`(`CheckAutomatic`/`CheckExit`)로
  뽑아냈고, `IsGameOver` 설정·`TimeManager.PauseGame()`·`EventHub.RaiseGameEnded` 발행 같은 실제 상태 변경은
  그대로 `MarketManager.EndGame()`에 남겼다. 나머지 책임(턴 진행/시사 이벤트/캔들 기록/스트리머 반응/거래)은
  새 클래스로 쪼개지 않고 `#region`으로만 나눴다 — Manager는 MonoBehaviour 싱글턴 자리를 유지해야 해서
  기존 "Manager/Calculator 분리" 패턴에 맞는 것만 클래스로 뽑고 나머지는 클래스 내부 정리로 그쳤다.
  `SkillManager`(201줄)·`StatCalculator`(210줄)·`PriceChartUI`(227줄)는 새 파일 없이 `#region`으로만 정리했고,
  `PriceChartUI.Redraw()`는 가격 범위 계산(`ComputePriceRange`)과 캔들 1개 갱신(`UpdateCandle`)만 내부 private
  메서드로 추출했다. 4개 파일 모두 git diff로 계산식·조건문·이벤트 발행 순서가 그대로인지 확인했다 (Unity
  MCP가 이 세션에 연결되어 있지 않아 컴파일/Play 모드 검증은 사용자가 에디터에서 직접 진행하기로 함).
  (`Logging.md` "200줄 넘는 스크립트 4개 책임별 정리" 참고)
- **완료** : Play 테스트 피드백 3건 반영 (확률 포화 버그 재수정 + 주봉 캔들 + 격자판). ①
  `ProbabilityCalculator.SupportWeight`/`GrowthWeight`를 0.5 → 0.25로 다시 절반 낮췄다 — `TradeCalculator.Long/
  Short`가 거래 1건마다 Support/Growth를 항상 동일한 양만큼 같이 움직이는 탓에 0.5에서도 몇 번만 거래하면
  상승확률이 바로 100%에 고정되던 문제를 해결, 이제 Support/Growth가 **둘 다** 클램프 상한(100)까지 차야
  포화된다. ② `PriceChartUI.cs`에 `AggregateWeekly()`를 추가해 매일 찍히던 캔들을 7일씩 모은 주봉으로
  바꿨다 — `MarketManager`/`RuntimePriceHistory`의 일별 기록 자체는 그대로 두고 차트가 그릴 때만 집계하며,
  진행 중인 마지막 캔들은 Open이 고정된 채 Close/Date만 매 턴 갱신되다가 7일째 확정되고 다음 캔들로 넘어간다.
  ③ 토스뱅크 스타일 격자판(가로 가격선 4개 + 왼쪽 가격 라벨, 캔들 뒤에 깔림)을 추가했다. 이벤트 발생 표시는
  사용자가 예시 UI를 주기로 해서 이번 스코프에서 제외했다. Unity MCP(Play 모드 `execute_code`)로 실제
  거래→포화 안 됨, 7일 단위 주봉 그룹핑, 격자 렌더링까지 스크린샷으로 확인했다. (`Logging.md` "Play 테스트
  피드백 3건 반영" 참고, 공식은 `Game_Formula.md` 1장/2-2장)
- **완료** : 의심도(Doubt)가 후반에 확률을 완전히 압도하던 문제 완화 + 거래 시 Doubt 증가 추가. Doubt는
  730턴부터 자동으로 계속 오르는데(감쇠 없음) `ProbabilityCalculator.DoubtWeight`(wd)가 1.0이라 Doubt=100이면
  Support/Growth가 최대치여도 상승확률이 0까지 눌려 후반 게임이 사실상 진행 불가능했다. `ws`/`wg`와 동일하게
  wd를 0.25로 낮춰서, Doubt가 완전히 차도 Pup이 최대 0.75까지는 유지되도록(Doubt 하나만으로 확률을 완전히
  압도하지 못하도록) 완화했다. 동시에 `TradeCalculator.Long/Short`가 거래 수량에 비례해 Doubt도 함께 올리도록
  바꿨다(`DoubtWeightPerTradeCoin`=0.02, `ManipulateSupply`와 동일하게 방향 무관 절대값 비례, 감쇠 없이 누적) —
  "거래가 잦거나 크면 시장에서 눈에 띈다"는 의미이며, Support/Growth 가중치(0.1)의 1/5로 낮게 잡아 거래
  자체가 주된 Doubt 원인이 되지 않게 했다. Unity MCP(`execute_code`)로 `Support=Growth=100, Doubt=100 ->
  Pup=0.75`, `Long(100) -> Doubt+2.0`, `Short(50) -> Doubt+1.0`을 수식대로 정확히 확인했다. (공식은
  `Game_Formula.md` 1장/3장)
- **완료** : 거래/발행량 조작 모달 리뉴얼 — 기본 화면 배치만 목업에 맞춤 (기능/연결은 `Next_Tesk.md` "최우선
  후보"에 남겨둠). `TradePanel`의 수량 버튼(`BtnPlus1/10/100/MAX`)과 `TradeAmountText`를 숨기고, `BtnLong`/
  `BtnShort`를 세로로 쌓은 전체폭 바로 재배치했다. `CoinControlPanel`의 조작 버튼 6개를 숨기고 배경을
  꺼서 텍스트 2줄(`TotalSupplyText`/`AdjustAmountText`)만 코인 아이콘과 함께 보이도록 바꿨다 (아이콘은
  원래 1개뿐이라 복제해서 `CoinIcon1`/`CoinIcon2`로 분리, `CoinIcon1`은 "코인거래량" 스프라이트로 교체).
  `MoneyText`/`CoinText`가 줄바꿈되며 아이콘과 겹쳐 보이던 것도 한 줄로 고쳤다. `PlayerUI.cs` 등 스크립트는
  건드리지 않고 씬 오브젝트 배치만 바꿨다 (`Logging.md` "모달 리뉴얼 — 레이아웃 전용 패스" 참고).
- **완료** : 일부 한글 글자가 `LiberationSans SDF`에서 깨지던(□□) 문제 수정. 네오둥근모(`neodgm.ttf`, 유저
  로컬 폰트 폴더에 설치돼 있던 것)를 `Assets/Fonts/NeoDunggeunmo/`로 복사해 Dynamic 모드 TMP Font Asset
  (`NeoDunggeunmo SDF.asset`)으로 생성하고, `TMP_Settings.fallbackFontAssets`(프로젝트 전역)와
  `LiberationSans SDF.asset`의 `fallbackFontAssetTable`에 등록했다. 기존 `LiberationSans SDF - Fallback.asset`
  (소스 폰트가 `LiberationSans` 그대로라 한글 커버리지가 없었음)은 건드리지 않고 새 폰트를 추가하는 방식을
  택했다. Play 모드 스크린샷으로 "보유 현금"/"보유 코인" 정상 렌더링 확인. (`Logging.md` "한글 폰트 폴백
  추가" 참고) 이어서 사용자가 "발행량/보유 코인수량이 아직 영어"라고 지적해서 확인해보니, 폰트 문제와는
  별개로 `TotalSupplyText`/`AdjustAmountText`("Total Supply : 200"/"Adjust Amount : <0>")와 게이지 라벨 3개
  (`SupportText`/`IncreaseText`/`DoubtText`, "Coin Support"/"Coin Increase Score"/"Doubt Score")가 애초에
  텍스트 **내용 자체**가 영어 placeholder였다. "현재 발행량 : 200개"/"보유 코인수량 : 0개"/"코인 지지도"/
  "코인 상승률"/"의심도"로 고쳤다 (스크립트가 갱신하는 값이 아니라 씬에 고정 텍스트로 박혀있던 것들이라
  단순 텍스트 교체).
- **완료** : 거래(Long/Short)/발행량 조작 모달 — 기능/연결 단계. 레이아웃 전용 패스(위 항목) 다음으로 실제
  모달 기능을 붙였다.
  - 신규 `Assets/Scripts/UI/TradeModalUI.cs` : 매수/매도 공용 모달(`TradeMode` enum으로 분기). 숨겨뒀던
    `BtnPlus1/10/100/MAX`/`TradeAmountText`를 새로 만든 `TradeModal`(전체화면 딤 오버레이) →
    `ModalBox`(중앙 팝업 박스) 밑으로 이동시키고, `PreviewText`/`BtnConfirm`/`BtnCancel`을 새로 만들었다.
    확정 전 미리보기는 `PlayerManager.HandleBuyCoin/HandleSellCoin`과 동일한 공식(Long:
    `cost = amount × CurrentPrice`, Short: `revenue = amount × CurrentPrice × (1 + CashBonus/100)`)을
    그대로 재사용했고, `BtnConfirm`을 눌러야만 `EventHub.RaiseBuyCoin`/`RaiseSellCoin`을 호출한다. `BtnLong`/
    `BtnShort`는 `PlayerUI.cs`에서 즉시 거래 대신 `TradeModalUI.Open(TradeMode)`만 호출하도록 축소했다
    (`tradeAmount` 관리/수량 버튼 로직은 전부 모달 쪽으로 옮겨감).
  - 신규 `Assets/Scripts/UI/CoinControlModalUI.cs` : `CoinControlPanel`에 부착. `TotalSupplyText`(Supply,
    `EventHub.OnMarketUpdated` 구독)/`heldCoinText`(보유 코인수량, `PlayerManager.currentCoins` 폴링)는
    기본 화면에서도 항상 보이도록 유지한다. 발행량 조작 자체는 매수/매도 모달과 동일한 스타일(전체화면 딤
    오버레이 `MintModal` → 중앙 팝업 박스 `MintModalBox`, `TitleText`/`AmountText`/`+/-`·`+1`/`+10`/`+100`/
    `MAX`/`PreviewText`/`BtnConfirm`/`BtnCancel` 전부 신규 생성)로 새로 만들었다 — 아래 "1차 시도 → 수정"
    참고. `BtnConfirm`을 눌러야만 `EventHub.RaiseManipulateSupply(±amount)`를 호출한다.
  - 신규 `Assets/Scripts/UI/MintButtonUI.cs` : "코인 발행" 트리거 버튼. `CoinControlPanel` 안, 발행량/보유
    코인 두 줄 텍스트 오른쪽에 배치했다 (아래 "1차 시도 → 수정" 참고). `SkillManager.IsUnlocked(SkillID.
    추가발행권한)`가 false면 `CanvasGroup.alpha/interactable/blocksRaycasts`로 숨김/비활성화한다 —
    `gameObject.SetActive(false)`를 쓰면 `Update()`가 멈춰서 나중에 스킬을 사도 다시 안 나타나는 문제가 있어
    반드시 CanvasGroup 방식을 썼다.
  - **1차 시도 → 사용자 지적으로 수정** : 처음엔 "코인 발행" 버튼을 `RightPanel`의 `TimePanel`/`TradePanel`
    사이 빈 공간에 놓고, 발행량 조작 버튼도 기존에 있던 범용 UI 키트풍 버튼(`BtnPlus/Minus`, `BtnAmount1/10/
    100/MAX`, `BtnAdjust`)을 그대로 재활용해 `MintControlsGroup`이라는 래퍼로만 묶어 껐다 켰다 했다. 사용자가
    실제 리뉴얼 목업(기본 화면) 스크린샷을 보여주며 지적한 두 가지를 반영해 다시 만들었다 —
    ① `RightPanel`의 그 빈 공간은 "코인 발행" 버튼 자리가 아니라 스트리머 패널(캐릭터+말풍선, 별도 작업)
    자리였다. 버튼을 `CoinControlPanel` 안, 발행량/보유 코인 텍스트 오른쪽으로 옮겼다.
    ② "옛날 UI"(범용 버튼 재활용)를 없애고 매수/매도 모달과 통일된 스타일의 새 모달로 교체해달라는 요청을
    받아, 기존 `BtnPlus/Minus`/`BtnAmount1/10/100/MAX`/`BtnAdjust`/`BtnClose`/`MintControlsGroup`을 전부
    삭제하고 `TradeModal`과 동일한 구조(딤 오버레이+중앙 박스+제목+큰 숫자 표시+수량 버튼+미리보기+확인/취소)로
    새로 만들었다.
  - **부수 버그 발견 및 수정** : 이 과정에서 `TradeModal`(및 옛 `MintControlsGroup`)이 씬에 저장된 기본
    상태가 `activeSelf = true`였던 것을 발견했다 — `SettingsPanel`은 기존부터 `activeSelf = false`로 저장돼
    있어서 에디터에서 Play를 안 해도 닫힌 채로 보이는데, 이 두 모달은 `Start()`가 런타임에만 숨겨줘서
    에디터에서 계속 열려있는 것처럼 보이는 상태였다. `SettingsPanel`과 동일하게 씬 저장 상태 자체를
    `activeSelf = false`로 고쳤다.
  - Unity MCP Play 모드로 매수(수량 200 선택 → 확인 → 현금/코인 반영 확인), 매도(미리보기 공식 확인), 취소
    (아무 반영 없이 닫힘), 발행량 조작(+100/+/- 토글로 부호 전환/미리보기 정확도/취소 시 Supply 불변) 전부
    스크린샷으로 검증 완료.
  - 범위 밖에서 `SkillManager.skillDatabase` 등록 누락을 발견했다 — 아래 별도 항목으로 바로 이어서 수정함.
- **완료** : `SkillManager.skillDatabase` 데이터 누락 수정 (`SkillManager NullReferenceException` 버그
  후보와 동일 원인이었음). 위 모달 작업 검증 중 `SkillManager.Instance.GetSkillProfile(SkillID.추가발행권한)`가
  `null`을 반환하는 걸 발견하고 원인을 추적하다가, `Managers`의 `SkillManager.skillDatabase`(Inspector
  직렬화 리스트)가 `추가발행권한` 하나만 빠진 게 아니라 **`null` 항목 1개만 들어있는 사실상 빈 배열**임을
  확인했다 — 6개 스킬(`발행량은폐`/`지갑분산`/`락업`/`독약조항`/`추가발행권한`/`우회발행권한`) 중 단
  하나도 등록되어 있지 않았다. 이게 바로 예전에 캔들 차트 작업 중 발견해 미해결로 남겨뒀던
  "`SkillManager.cs:43`(`Initialize()`) `NullReferenceException`" 버그의 근본 원인이었다 —
  `foreach (SkillSO skill in skillDatabase)`가 `null` 항목을 순회하며 `skill.isReusable`을 읽으려 할 때
  터지는 것이었다. Unity MCP `manage_components.set_property`로 6개 `.asset` 경로를 배열에 채워 넣어
  해결했다. Play 모드에서 (1) `NullReferenceException`이 더 이상 발생하지 않는 것, (2)
  `EventHub.RaiseSkillClicked(추가발행권한)` → `RaiseSkillPurchased()` 실제 구매 흐름으로
  `IsUnlocked`가 `false → true`로 바뀌는 것, (3) 그 결과 "코인 발행" 버튼이 `CanvasGroup`으로 실제 나타나는
  것, (4) 그 버튼으로 모달을 열어 발행량을 실제로 조작(0 → 200)하는 것까지 전부 확인했다.
- **완료** : 매수/매도 모달(TradeModal) 슬라이더 + "+/-" 증감 토글 버튼. Figma "코인발행" 팝업 대비 남아있던
  구조 두 개를 마저 붙였다. `TradeModalUI.cs`에 `GetMaxTradeAmount()`를 새로 뽑아 `SetTradeAmountToMax()`와
  슬라이더 `maxValue`(Long: 현금으로 살 수 있는 최대 개수, Short: 보유 코인 전량)가 같은 로직을 공유하게
  했고, 버튼↔슬라이더 값을 `SetValueWithoutNotify`로 양방향 동기화했다(이벤트 루프 없음). "+/-" 버튼은
  용도가 불확실해 구현 전 사용자에게 확인했고 — "증감 모드 토글"로 확정(2026-07-31) — 누르면
  `isSubtractMode`가 반전되어 이후 +1/+10/+100이 더하기 대신 빼기로 동작한다(0 밑으로는 안 내려감, 버튼
  배경색으로 현재 모드 표시). 씬 오브젝트는 Unity MCP `execute_code`로 생성했다 — "+/-" 버튼은 기존
  `BtnPlus1`을 `Instantiate`로 복제해 스타일(회색/radius18/NeoDunggeunmo)을 그대로 물려받았고, 슬라이더는
  표준 Slider 계층(Background/Handle Slide Area/Handle)을 새로 조립했다(트랙 `#D9D9D9` radius16, 손잡이
  주황 원, `ImageWithRoundedCorners`로 원형 구현). 이후 슬라이더가 통계블록(현재 코인 아이콘)과 겹친다는
  피드백을 받아 `ModalBox` 높이를 660→760으로 키우고(pivot 중앙이라 위/아래 50씩 균등 확장), 슬라이더 이하
  전체(슬라이더/PreviewText/수량버튼/확인·취소)를 65px씩 아래로 재배치해 해결했다. (`Logging.md` "매수/매도
  모달 슬라이더 + +/- 버튼" 참고)
- **결정** : TradeModal 닫기 구조는 현행(`BtnConfirm`+`BtnCancel` 나란히) 유지하기로 함(2026-08-01). Figma
  목업은 우상단 X + 하단 확인버튼 하나뿐인 구조였지만, 사용자가 지금 코드/씬 그대로 가도 된다고 판단해
  X버튼 추가·Cancel 제거 작업은 하지 않기로 확정. `Next_Tesk.md` 후보 목록에서 제거함.
- **완료** : 이벤트 로그 패널 본체(2026-08-01). Figma(`EjUw2LdqxAYhL2180OAXHo`, node `1261:195`)를 확인해
  1512x982 목업 프레임을 1920x1080 캔버스에 축(scaleX=1920/1512, scaleY=1080/982) 매핑했다 — 목업의 하단
  게이지 행(y:826~982)이 변환식으로 계산한 Unity y:0~171.5에 그대로 떨어져, 기존 `RightPanel`/`ChartPanel`/
  `CoinControlPanel`이 공유하는 "y=172 아래는 게이지 자리" 경계와 거의 정확히 일치함을 확인했다(이번엔 눈대중
  배치 없이 수식이 실제 기존 요소 좌표와 맞아떨어짐). `Main_Canvas/EventLogPanel`(1920x908, anchoredPosition
  (0,86), y:172~1080)에 배경/탭 2개("개요"/"커뮤니티")/X 닫기 버튼/`ContentArea`(회색)/`OverviewBox`(흰색,
  "이벤트 로그" 카드 리스트용 `ScrollRect`)/카드 템플릿(`EventCardTemplate`, 비활성)까지 전부 **씬 오브젝트로**
  배치했다 — 루트가 y:172~1080만 차지해서 기존 Support/Growth/Doubt 게이지 3종은 중복 생성 없이 그대로 아래에
  보인다.
  - **1차 시도 → 사용자 지적으로 재작업** : 처음엔 `PriceChartUI`의 오브젝트 풀링 패턴을 오해해서 탭/닫기버튼/
    ContentArea/OverviewBox 같은 고정 요소까지 전부 `EventLogPanelUI.Awake()`에서 코드로 생성했다가, "왜 다
    코드로 짬?"이라는 지적을 받았다. 이 프로젝트 관례(`TradeModalUI`/`CoinControlModalUI`)는 정적 요소는 씬에
    직접 배치하고 스크립트는 참조/로직만 담당하는 방식이라, 정적 요소는 전부 씬 오브젝트로 다시 만들고
    `EventLogPanelUI`는 `public Button/GameObject/RectTransform` 필드로 참조만 갖도록 축소했다. 개수가
    가변적인 카드만 예외적으로 런타임 생성이 필요해서, 씬에 `EventCardTemplate`(비활성) 하나를 만들어두고
    `TradeModalUI`가 `BtnPlus1`을 복제해 "+/-" 버튼을 만든 것과 동일하게 `Instantiate`로 복제한다. 신규
    `Assets/Scripts/UI/EventCardView.cs`(템플릿에 부착, `background`/`titleText`/`dateText`/`effectsText`
    참조만 보관)를 만들어 `EventLogPanelUI`가 문자열/색만 넘기면 `Populate()`가 꽂아 넣게 했다.
  - "개요" 탭은 열 때마다 `MarketManager.EventLog`를 순회해 카드를 새로 만든다(긍정=연두 `#B1FFB1`/부정=빨강
    `#FFBAB1`, 제목/날짜/`effects` 목록 표시, `Nobi.UiRoundedCorners.ImageWithRoundedCorners`로 모서리를
    둥글게). 효과 표시는 `DoubtDecrease`처럼 "감소형" 타입만 부호를 뒤집어 "스탯이 실제로 변한 방향"(+ = 증가,
    - = 감소)으로 통일했다. 이벤트 개수가 적어(월 1회 수준) 오브젝트 풀링 없이 열 때마다 카드를 새로 생성/파괴한다.
  - "커뮤니티" 탭은 데이터/설계가 없어 클릭하면 빈 화면만 보이도록 자리만 잡아뒀다(사용자 확인 후 결정,
    `Next_Tesk.md` 참고).
  - **탭 선택 색상** : 처음엔 "개요"=연한 빨강(`#FFDBDB`)/"커뮤니티"=진한 빨강(`#FF8686`)을 고정 색상으로 오해했는데,
    사용자가 "진한 빨강이 눌린(선택된) 상태, 연한 빨강이 안 눌린 상태"라고 정정해줘서 탭을 누를 때마다
    선택된 탭은 진하게/나머지는 연하게 서로 바뀌도록 고쳤다(`SetTabSelected()`).
  - **닫기 버튼** : 처음엔 유니코드 "✕" 텍스트로 만들었다가 폰트에 없는 글리프라 빈 사각형으로 깨져서 "X"로
    바꿨는데, 이후 사용자가 `Assets/Sprites/UI아이콘/취소버튼.png`(흰색 X 아이콘)를 새로 추가해줘서 텍스트
    대신 이 스프라이트를 쓰도록 교체했다.
  - **부수 버그** : 정적 요소를 씬으로 옮기면서 루트(`EventLogPanel`) 자체의 배경 `Image`를 빠뜨려서 우측
    상단 일부가 `RightPanel`을 그대로 투과해 보이는 문제가 있었다 — 루트에 `Image`(어두운 회색 `#333333`)를
    추가해 해결했다.
  - `EventLogButton.cs`는 클릭 시 `EventLogPanelUI.Toggle()`을 호출하고, 아이콘은 패널의 X 닫기 버튼으로도
    닫힐 수 있어 클릭 시점이 아니라 매 프레임 `IsOpen`을 폴링해 동기화한다.
  - Unity MCP Play 모드로 카드 렌더링(색상/텍스트), 탭 전환(선택 색상 교대 포함), X 닫기, 아이콘 동기화,
    기존 게이지 노출까지 스크린샷으로 확인했다.
- **완료** : 이벤트 로그 패널 후속 수정 2건(2026-08-01). ① 열려있는 동안 게임 시간이 안 멈추던 문제 —
  `TradeModalUI`와 동일하게 `Open()`/`Close()`에서 `EventHub.RaiseGamePaused()`/`RaiseGameResumed()`를
  호출하도록 추가했다. ② 탭-콘텐츠 매핑이 반대였던 문제 — Figma를 보니 애초에 프레임이 2개로 나뉘어
  있었다(`EjUw2LdqxAYhL2180OAXHo` node `1253:2` "뉴스,이벤트 페이지 - 스탯개요" / node `1261:195` "뉴스,이벤트
  페이지 - 이벤트 패널"). 처음엔 이벤트 로그(1261:195 내용)를 "개요" 탭에 연결했는데, 실제로는 "개요" 탭이
  `1253:2`(스탯개요+엑시트 버튼, 아직 미구현), "커뮤니티" 탭이 `1261:195`(이벤트 로그)여야 했다. 씬의
  `OverviewBox`를 `EventPanelBox`로 이름을 바꾸고(내용은 그대로, "이벤트 패널"이라는 실제 정체성에 맞춤),
  `EventLogPanelUI.ShowOverview()`/`ShowCommunity()`가 반대로 여닫도록 바꿨다 — 이제 "개요" 탭(기본 선택)은
  빈 화면, "커뮤니티" 탭이 이벤트 로그를 보여준다. `Next_Tesk.md`에 "개요 탭 콘텐츠(스탯개요+엑시트 버튼)"를
  새 후보로 추가했다(기존에 따로 있던 "엑시트 버튼" 후보와 같은 화면임을 확인해 합침). Unity MCP Play
  모드로 시간 정지(`Time.timeScale`)와 탭 전환 방향을 스크린샷/값 조회로 재검증했다.
- **완료** : 개요 탭 콘텐츠(스탯개요 + 엑시트 버튼, 2026-08-01). Figma(`EjUw2LdqxAYhL2180OAXHo`, node
  `1253:2` "뉴스,이벤트 페이지 - 스탯개요")를 이벤트 로그 패널과 동일한 축 스케일(scaleX=1920/1512≈1.2698,
  scaleY=1080/982≈1.0998)로 `EventLogPanel/ContentArea` 로컬 좌표에 매핑했다 — `ContentArea`(1722×715)가
  Figma 그레이 박스(1356×650, node `1253:5`)와 정확히 일치함을 먼저 검증하고(children의 left/top을
  1356×650 기준 상대좌표로 보고 동일 축 스케일 적용), 우측 흰 박스(목표금액/엑시트, node `1261:276`) 크기를
  계산하니 813×660으로 나와 커뮤니티 탭의 `EventPanelBox`(813×660)와 **정확히 일치** — 두 프레임이 같은
  컴포넌트를 좌우 대칭으로 재사용했음을 확인해 매핑 공식의 정확성을 검증했다. 아이콘류(64px 정사각형)는
  `CloseBtn`(Figma 128px → 실제 80×80) 선례를 따라 비율 왜곡 없이 60×60 정사각형으로, 폰트는 탭 라벨
  변환 실측치(Figma 24px → Unity fontSize 28)를 기준으로 잡았다.
  - 신규 `Assets/Scripts/UI/EventOverviewUI.cs` : `ContentArea` 밑에 씬 오브젝트로 배치한 `OverviewContent`
    (좌측 아이콘 9개 + 텍스트 9줄 + 우측 흰 박스 `OverviewBox`)의 TMP/Button 참조만 갖고 갱신 로직을 담당한다
    (TradeModalUI/CoinControlModalUI 관례 그대로 — 정적 요소는 씬 오브젝트, 스크립트는 참조/로직만).
    좌측 : 코인 지지도/상승도/의심도 현재값(`PlayerStat.Support`/`Growth`/`Doubt`, 항상 부호 표시),
    Job+Skill 보너스(`JobSkillSupportBonus`/`JobSkillGrowthBonus`, Doubt 보너스는 `JobManager.CurrentJob.
    effects` + `SkillManager.GetActiveSkills()`의 `DoubtDecrease`/`DoubtIncrease`를 직접 합산 — Doubt는
    감쇠 없는 값이라 별도 누적 필드가 없어서 매번 재계산), 긍정/부정 이벤트 확률, 현금 증가량(`CashBonus`).
    우측 : 목표금액(`MarketManager.TargetAsset`)/현재금액(`PlayerManager.currentMoney`)/남은 금액을
    `"₩ " + N0` 형식(기존 TradeModalUI/PlayerUI 관례, Figma 목업의 `$` 표기는 예시 수치였음)으로 표시하고,
    엑시트 버튼은 `MarketManager.CanExit`일 때만 `interactable = true`, 클릭 시 확인 팝업 없이 바로
    `EventHub.RaiseExitRequested()`를 호출한다(둘 다 사용자 확인 후 확정, 2026-08-01). Figma 색상(현재값
    빨강/보너스 초록, 부호와 무관하게 카테고리별 고정색)을 TMP 리치텍스트 `<color>` 태그로 그대로 반영했다.
  - `EventLogPanelUI.cs` : `overviewContent`/`overviewUI` 필드를 추가하고 `ShowOverview()`가 커뮤니티 탭과
    대칭으로 `overviewContent.SetActive(true)` + `overviewUI.Refresh()`를 호출하도록 바꿨다(`RefreshLog()`와
    동일한 패턴 — 패널이 열려있는 동안은 `GamePaused`로 턴이 멈추므로 `OnMarketUpdated` 구독 대신 진입
    시점 1회 갱신만 함).
  - 씬 오브젝트(아이콘 9개 `Assets/Sprites/UI아이콘/UI아이콘_지지도.png` 등 기존 스프라이트 재사용, 텍스트
    9줄, 흰 박스+라벨 3줄+값 3줄+엑시트 버튼)는 Unity MCP `execute_code`로 계산된 좌표 그대로 생성했다.
    Play 모드에서 목표/현재/남은 금액 표시, `CanExit` 조건에 따른 엑시트 버튼 활성화(현금을 강제로
    올려 재검증), 탭 전환까지 스크린샷으로 확인했다.
- **완료** : 엔딩 결과 화면(2026-08-02). `EventHub.OnGameEnded(EndingType)`을 구독해 4종 엔딩(체포/엑시트/
  영웅/거지)에 맞는 제목+설명 문구를 표시하는 전체화면 오버레이. Figma(`EjUw2LdqxAYhL2180OAXHo`)에서 이
  화면에 대응하는 프레임을 찾지 못해(다른 두 프로젝트 자료가 섞여 있는 파일이라 `get_metadata`가 관련
  영역까지 못 읽는 문제도 있었음) 사용자에게 확인한 뒤 최소 구성(제목 텍스트 + 설명 텍스트, 검은 반투명
  배경)의 임시 UI로 만들었다 — 디자인이 정해지면 교체 필요. 재시작 등 후속 액션도 아직 기획되지 않아
  결과 문구 표시만 담당한다.
  - 신규 `Assets/Scripts/UI/EndingResultUI.cs` : `panel`(어두운 오버레이)/`titleText`/`descriptionText`
    필드만 갖고, `OnEnable`/`OnDisable`로 `EventHub.OnGameEnded`를 구독/해제한다. `MarketManager.EndGame()`이
    이미 `TimeManager.PauseGame()`을 호출한 뒤 이 이벤트를 발행하므로 별도로 게임을 멈추지 않는다. 거지
    엔딩은 `Game_Formula.md` 5장에 "결과: 미정(추가 기획 필요)"로만 적혀 있어 임시 문구로 채웠다.
  - 스크립트 자신(`EndingResultUI` 오브젝트)은 `Main_Canvas` 밑에서 항상 켜진 상태를 유지하고, 실제로
    보이는 자식 `Panel`(전체화면, 1920×1080, 검정 알파 0.85)만 `SetActive`로 토글한다 — `EventLogPanelUI`
    처럼 스크립트가 곧 패널인 구조로 만들면 패널이 꺼져 있는 동안 `OnEnable`이 다시 호출되지 않아 구독이
    끊기기 때문(비활성 오브젝트는 `OnEnable`이 실행되지 않는 Unity의 기본 동작).
  - `TitleText`/`DescriptionText`는 `NeoDunggeunmo SDF` 폰트로 지정했다(사용자 지시 — 기본 폰트로 두면
    나중에 다시 바꿔야 함).
  - Unity MCP Play 모드에서 `execute_code`로 `EventHub.RaiseGameEnded(EndingType.Hero)`를 직접 호출해
    문구 표시를 검증했다. 처음 스크린샷 미리보기에서는 반투명 오버레이가 화면 일부만 덮은 것처럼 보였는데,
    실제로는 축소된 미리보기 이미지를 잘못 읽은 것이었고 — 저장된 PNG를 직접 픽셀 단위로 읽어보니
    (`longBtn≈(0.03,0.03,0.06)`, `topLeft≈(0.06,0.06,0.06)`) 전체 화면이 정확히 덮여 있음을 확인했다.
    (완전 불투명 마젠타로 바꿔 `GetWorldCorners`가 `(0,0)~(1920,1080)`임도 별도로 재확인함.)
- **완료** : UI 스크립트 아키텍처 리팩토링 (`Next_Tesk.md` 10번, 지난 세션 진단 → 이번 세션 구현, 2026-08-02).
  진단됐던 5개 항목을 우선순위 순으로 처리했다. ① `TimeUI`/`PlayerUI`/`CoinControlModalUI`의 `Update()` 폴링을
  `EventHub` 구독형(`OnDayChanged`/`OnMarketUpdated`)으로 통일 — 매수/매도/발행량 조작이 지금까지
  `OnMarketUpdated`를 안 쐈다는 걸 발견해, `TradeModalUI`/`CoinControlModalUI`의 확정 버튼에 발행을 추가해서
  실제로 폴링을 없앨 수 있게 만들었다. ② `CoinControlModalUI.Open()`에 빠져있던
  `EventHub.RaiseGamePaused()`/`Close()`의 `RaiseGameResumed()`를 추가 — `TradeModalUI`/`EventLogPanelUI`와
  달리 이 모달만 열려있어도 시간이 안 멈추던 버그로 확인. ③ `SettingsUI`의 안 쓰는
  `using Unity.VisualScripting;`·실제 코드와 안 맞던 죽은 주석을 정리하고, `Time.timeScale = 1f` 강제 리셋을
  `EventHub.RaiseGamePaused/RaiseGameResumed`로 교체해 닫을 때 배속(2/4/8x)이 유지되게 고쳤다. ④ 신규
  `Assets/Scripts/UI/Utils/UIFormat.cs`(통화/퍼센트/부호/날짜 포맷, `EffectType` 증가·감소 방향 판정을 모은
  static 유틸)를 만들어 `PlayerUI`/`TradeModalUI`/`EventOverviewUI`/`CoinPriceHeaderUI`/`PriceChartUI`/
  `EventLogPanelUI`에 흩어져 있던 중복 구현을 연결했다(각 파일이 원래 쓰던 출력 형식은 그대로 유지 — 공백
  있는/없는 `₩` 표기 등 시각적 차이는 안 건드림). `PriceChartUI` 캔들 색 하드코딩은 그대로 뒀다 —
  `[SerializeField]`라 씬에 이미 구체값이 직렬화돼 있어서 코드 상수화만으론 "버튼 색 바뀌면 어긋나는" 문제
  자체가 안 없어지고, 실제로 고치려면 버튼 참조를 씬에 새로 연결해야 해서 범위 밖으로 남겨둠. ⑤
  `MintButtonUI`의 매 프레임 `SkillManager.IsUnlocked()` 폴링을 `EventHub.OnSkillPurchased` 구독으로 교체.
  작업 도중 실사용 버그 2건도 함께 발견해 고쳤다. `TradeModalUI.panel`이 스크립트 자신의 GameObject를
  가리키는 구조라, 씬에서 `TradeModal`이 비활성 상태로 시작하면 `Open()`이 `panel.SetActive(true)`로 처음
  활성화시키는 순간 Unity가 `Start()`를 다음 프레임으로 미루는데, 그 `Start()` 끝의
  `panel.SetActive(false)`가 방금 연 모달을 그대로 닫아버려서 첫 클릭은 시간정지만 되고 모달이 안 열리는
  버그가 있었다(두 번째 클릭부터는 `Start()`가 이미 끝나서 정상) — `isOpen` 플래그를 추가해 `Open()`이 먼저
  실행됐으면 뒤늦은 `Start()`가 다시 닫지 않도록 막았다(씬의 초기 활성/비활성 상태와 무관하게 동작, UI 편집을
  위해 오브젝트를 꺼둔 채로 작업해도 안전). `TimeUI`의 재생 버튼은 `TimeManager.ResumeGame()`(마지막 배속
  그대로 복귀)만 호출해서 이미 배속(2/4/8x) 상태에서 누르면 아무 반응이 없었는데, 항상 1배속으로 리셋하도록
  바꿨다. Unity MCP로 매 단계 컴파일 확인, 씬 데이터 조회(`TradeModal.m_IsActive` 등)로 원인 확정까지 진행했다.
  자세한 파일 목록/호출 스택은 `Issues/Issue_UI_Refactor.md` 참고.
- **완료** : 스트리머 패널 표정 스프라이트 연결(2026-08-02, `Next_Tesk.md` 8번). Figma에서 이 화면(RightPanel
  기본 화면) 프레임을 찾지 못한 건 엔딩 결과 화면 작업 때와 동일한 문제(`get_metadata`가 관련 영역까지 못
  읽음) — `Logging.md`에 이미 "목업엔 있지만 씬에 없는 새 요소"로 기록돼 있었고 실제로도 이 화면은 Figma
  프레임이 아니라 사용자가 준 스크린샷 기반이었으므로, Figma 대신 씬의 실제 `RectTransform`(`TimePanel`
  하단 y≈320.5 ~ `TradePanel` 상단 y≈-104.6, `RightPanel` 로컬 좌표)을 직접 조회해 빈 공간 크기(약
  425×490)를 계산하고 그 안에 정사각형(스프라이트 원본이 512×512) 400×400으로 배치했다.
  - 신규 `Assets/Scripts/UI/MainModal/StreamerPanelUI.cs` : `StatGaugeUI`/`CoinPriceHeaderUI`와 동일하게
    `EventHub.OnMarketUpdated` 구독형. `PlayerStat.StreamerReaction`(5단계) 값에 따라 `Image.sprite`를
    `Assets/Sprites/스트리머상태`의 5개 스프라이트 중 하나로 바꾼다. 새 `EventHub` 이벤트는 필요 없었음
    (기존 `OnMarketUpdated`가 매 턴/이벤트 트리거 시 이미 갱신된 `StreamerReaction`을 들고 전파).
  - 씬에 `RightPanel` 자식으로 `StreamerPanel`(Image + `StreamerPanelUI`) 오브젝트를 새로 만들고 5개
    스프라이트 필드를 전부 연결, 기본 표시는 "보통"(Neutral) 스프라이트로 설정.
  - 검증은 Unity MCP Play 모드에서 `execute_code`로 `MarketManager.CurrentStat.CurrentPrice`를 강제로
    ±200~500 바꾼 뒤 (private) `UpdateStreamerReaction`을 리플렉션으로 호출 → `EventHub.RaiseMarketUpdated`
    발행까지 재현해 Surge/Crash 스프라이트가 올바르게 교체되는 걸 스크린샷으로 확인했다.
  - 이번 범위에서 제외한 것 : 말풍선/멘트 텍스트, 립싱크나 캐릭터 모션 등 영상 기반 연출. 사용자가 참고
    영상을 준 다음 별도 세션에서 진행하기로 함(`Logging.md` "스트리머 반응(가격 변화 연동) 로직 설계" 절
    참고). `StreamerReactionCalculator`의 임계값(±10/±50)은 여전히 실플레이 후 조정 필요 항목으로 남아있음
    (`Next_Tesk.md` 7번 밸런스 후보에 포함).
- **완료** : 캐릭터 선택 씬 분리(2026-08-02, `Next_Tesk.md` 5번). Figma node `1202:186`("초반 캐릭터 선택")을
  확인해, 게임을 `CharacterSelectScene`(직업 선택, 새로 만듦) → `SampleScene`(기존 메인 게임) 2개 씬으로
  나누고 `Build Settings`에 이 순서로 등록했다(사용자가 씬 전환은 Build Settings 등록이 필요하다는 점과,
  씬 전환을 전담하는 매니저를 따로 두라고 명시적으로 요청함).
  - 신규 `Assets/Scripts/manager/GameSceneManager.cs` : 씬 이름 문자열을 여기 한 곳에만 두고
    `LoadCharacterSelect()`/`LoadMainGame()`만 노출하는 static 클래스. `EventHub`가 static인 것과 같은
    이유로 MonoBehaviour 싱글턴 대신 static을 택함 — 씬 전환 자체는 상태가 없는 동작이라 굳이
    `DontDestroyOnLoad` 오브젝트로 만들 필요가 없었음.
  - 신규 `Assets/Scripts/manager/JobSelectionHandoff.cs` : `public static JobSO SelectedJob` 필드 하나뿐인
    static 클래스. 씬이 바뀌어도(도메인 리로드 전까지) 정적 필드 값은 유지되므로, 이 필드에 선택된
    `JobSO`를 담아뒀다가 메인 씬에서 읽는 방식으로 두 씬 사이에 정보를 넘긴다. **왜 `Managers`
    오브젝트를 통째로 옮기지 않았는지** : `Managers`엔 `SettingsUI`도 같이 붙어있는데, 이건
    `SampleScene`의 `SettingsPanel`/`SettingsBtn` 같은 씬 전용 오브젝트를 직접 참조하고
    `Start()`에서 그 참조로 바로 `SetActive(false)`를 호출한다 — `Managers`를 캐릭터 선택 씬으로
    옮기면 그 씬엔 `SettingsPanel`이 없어서 `Start()`가 즉시 NullReferenceException을 낸다. 정적 필드
    핸드오프는 이 문제를 피하면서 필요한 정보(선택한 직업 하나)만 정확히 넘기는 더 작은 변경이었다.
  - `Assets/Scripts/manager/JobManager.cs`에 `Start()`를 추가 : `JobSelectionHandoff.SelectedJob`이
    있으면 `SelectJob()`으로 반영하고 필드를 비운다. `MarketManager.Instance`를 참조하는
    `SelectJob()` 특성상 `Awake()`가 아니라(다른 매니저의 `Awake` 순서 보장이 없음) 모든 `Awake`가
    끝난 뒤 실행되는 `Start()`에 뒀다.
  - 신규 씬 `Assets/Scenes/CharacterSelectScene.unity` : `2d_basic` 템플릿으로 만들고 `Main_Canvas`
    (1920×1080, ScaleWithScreenSize match=0.5, `SampleScene`과 동일 설정)/`EventSystem`을 직접 구성.
    Figma 프레임(1512×982)을 캔버스 중앙에 배치하고, `get_design_context`로 뽑은 좌표를 그대로
    (Figma px ≈ Unity unit, top-left pivot 기준 anchoredPosition=(x,-y)) 옮겨 직업 슬롯 6개(아이콘+라벨,
    `Nobi.UiRoundedCorners.ImageWithRoundedCorners`로 둥근 모서리) + 빈 슬롯 2개(향후 직업용 자리,
    회색 비활성) + 우측 상세 패널(이름/설명/초기자금/특성 2줄) + "다음으로" 버튼을 배치했다.
  - 신규 `Assets/Scripts/UI/CharacterSelectUI.cs` : 슬롯 클릭 시 하이라이트 갱신 + 상세 패널 갱신(선택된
    `JobSO`의 `effects`를 `UIFormat.SignedEffectValue`/`SignedPercent`로 포맷), "다음으로" 클릭 시
    `JobSelectionHandoff.SelectedJob` 설정 후 `GameSceneManager.LoadMainGame()` 호출.
  - **색상 표기 관련 메모** : 프로젝트가 Linear 컬러 스페이스라, Figma 헥스코드를 `255분의 N`으로만 바꿔
    `Image.color`에 그대로 넣으면(예: `#333333` → 0.2,0.2,0.2) 화면에 감마 보정이 다시 적용돼 실제로는
    더 밝게 렌더링된다(스크린샷 확인: 어두운 배경이 중간 회색으로 보임). 한 번 sRGB→Linear 변환값으로
    바꿔서 넣어봤는데(사용자 확인 요청으로), **사용자가 "그냥 색깔 코드 그대로 가져와서 반영"을 요청해
    최종적으로는 변환 없이 원본 sRGB 비율(예: 0.2 그대로) 그대로 두기로 함** — 화면상 밝기가 Figma
    목업과 정확히 일치하진 않지만, 이번 세션은 그 상태로 확정. `CharacterSelectUI.selectedColor`/
    `unselectedColor` 기본값도 동일하게 원본 sRGB 값(1, 0.631, 0.404)/(1, 0.796, 0.616)으로 되돌림.
  - **버튼/패널 그림자(2026-08-02, 같은 세션 후속 요청)** : Figma 목업의 버튼/`DetailPanel`엔
    `border-bottom` 10px짜리 진한 색 테두리가 있는데, 이건 실제로는 그림자가 아니라 "눌리지 않은
    버튼"처럼 보이게 하는 입체 효과다(선택된 `일반인` 슬롯만 이 테두리가 없어서 눌린 것처럼 납작해
    보임). 도형을 겹쳐서 재현했다 — 각 버튼/패널 뒤에 같은 크기 + 높이만 10 더 큰(top-left pivot이라
    아래로만 늘어남) 진한 색 `Shadow_*` 오브젝트를 만들고, 앞쪽 원래 도형이 위쪽만 덮어서 아래쪽 10만큼
    띠처럼 보이게 했다. `Shadow_*`는 `SetSiblingIndex`로 대응하는 앞면 오브젝트 바로 앞 순서로
    옮겨서 렌더링 순서를 맞췄다(대상: 직업 슬롯 6개 - 색 `#ff8235`, `DetailPanel` - 색 `#c95f02`,
    `NextButton` - 색 `#ff8235`. 빈 슬롯 2개는 Figma에 이 테두리가 없어서 그대로 둠). `CharacterSelectUI.
    cs`에 `slotShadows` 필드를 추가해 `SelectJob()`에서 선택된 슬롯만 그림자를 꺼서(`SetActive(false)`)
    Figma의 "선택=눌림" 표현을 그대로 재현했다.
  - **텍스트 버그 발견 및 수정** : 더미 직업 5종의 `description`에 "(Figma 목업엔 이름만 있고 효과/수치
    미정 — 더미)" 같은 내부 메모를 그대로 넣어뒀었는데, 상세 패널에서 그 문장이 길어서 아래 "초기 자금"
    줄과 겹치는 걸 Play 모드 스크린샷으로 발견했다 — 플레이어에게 보이는 텍스트에 개발 메모를 넣은 것
    자체가 잘못이라, 전부 짧은 설명 문구로만 정리하고 더미 여부는 `Next_Tesk.md`에만 남겼다.
  - 검증 : Unity MCP Play 모드에서 `execute_code`로 슬롯 버튼 클릭 → 상세 패널 갱신, "다음으로" 클릭 →
    `SceneManager.GetActiveScene().name`이 실제로 `SampleScene`으로 바뀜을 확인했다. 다만 이 세션의
    자동화 환경에서는 Editor 창이 포커스를 안 받아 Play 모드 프레임이 거의 진행되지 않는 문제가 있어서
    (`Time.frameCount`가 여러 툴 호출 뒤에도 그대로) `JobManager.Start()`의 자동 픽업이 실제로 실행되는
    순간까지는 프레임 펌프 한계로 못 봤다 — 대신 `JobManager.Instance.SelectJob(job)`을 직접 호출해서
    로직 자체(효과 적용, `MarketManager.CurrentStat.Growth` 변화)는 정상 확인했다. `Start()` 호출은
    Unity의 기본 생명주기 보장이라 실제 플레이(포커스 있는 상태)에서는 다음 프레임에 바로 실행된다.
  - 자세한 파일 목록/호출 스택은 `Issues/Issue_CharacterSelect.md` 참고.
- **완료** : 직업(일반인) `DoubtDecrease` 매 턴 재적용 버그 수정(2026-08-02). 실제 플레이 중 사용자가 발견 —
  "의심도 감소 -10%"가 최초 1회만 반영돼야 하는데 `StatCalculator.ApplyJob()`이 매 턴 재적용해 턴마다 계속
  깎였다. Doubt는 Support/Growth와 달리 감쇠 없이 그대로 이어지는(carry-over) 값인데, 원래 CashBonus/Volume
  (매 턴 새로 계산되는 값들)과 같은 그룹으로 취급했던 게 원인. `ApplyJob()`에서 `DoubtDecrease`/`DoubtIncrease`를
  Support/Growth와 동일하게 매 턴 재적용 대상에서 빼고, `ApplyJobSelection()`(선택 시점 1회)으로 옮겼다.
  `PlayerStat.JobSkillDoubtBonus` 필드를 신규 추가해 `JobSkillSupportBonus`/`GrowthBonus`와 동일한 패턴으로
  추적하고, `EventOverviewUI`의 "의심도 상승률" 표시도 매번 재계산하던 것에서 이 필드를 직접 읽는 걸로 정리.
  - 부수 발견 : `Doubt`가 0~100으로 클램프돼 있어서 기본값 0에서 "-10%"를 적용해도 즉시 0으로 잘려 체감이
    안 됐다. `StatCalculator.ClampStat()`의 Doubt 하한을 Support/Growth와 동일한 -100으로 넓혀 해결.
    `Game_Formula.md` 3장/3-1장도 이 변경에 맞춰 갱신.
  - 검증 : Unity MCP Play 모드에서 `execute_code`로 일반인 선택 → `MarketManager.NextTurn()` 3턴 반복 →
    `JobSkillDoubtBonus`가 `-10`으로 고정 유지(수정 전이면 -20/-30/-40으로 계속 감소)되는 걸 확인했다.
- **완료** : 이벤트 알림 모달(2026-08-02, 사용자가 준 스크린샷 2장 기준). 시사 이벤트가 실제로 발생하면
  메인 게임 화면 위에 제목/날짜/효과(예: "코인 지지도 +15")를 보여주는 배너를 띄우고, 떠 있는 동안 게임
  시간을 멈춘다. 신규 `EventHub.OnEventTriggered`를 `MarketManager.LogEvent()`에서 발행하도록 연결해 자동/
  수동/무조건 발생 이벤트 전부를 한 곳에서 커버했다. 알림 내용은 기존 이벤트 로그 카드(`EventCardView`)를
  그대로 재사용했고, 두 곳에서 중복돼 있던 색상/효과 문구 포맷 로직은 `EventEffectFormatter`로 뽑아 공용화했다.
  자세한 파일 목록/호출 스택/알려진 이슈는 `Issues/Issue_EventNotification.md` 참고.
- **완료** : 스킬 아이콘/구매 버튼 UI(2026-08-03). `EventHub.RaiseSkillClicked`/`RaiseSkillPurchased`를 발행하는
  실제 UI가 없던 걸 신규 `SkillPanelUI`로 채웠다. Figma MCP가 Starter 플랜 월 호출 한도(6회)에 걸려 있어서
  처음엔 `MintModal`을 복제한 예시 버전으로 시작했는데, 사용자가 실제 "스킬구상도 - 디테일1" 스크린샷을
  줘서 그 기준으로 다시 잡았다 — 탭 4개(`SkillCategory`와 대응) + 선택된 스킬을 크게 보여주는 미리보기
  박스 + 아이콘 그리드 + 이름/설명/효과/비용 + 구매 버튼. 재작업 중 사용자가 Play 테스트로 "닫은 뒤 재오픈
  안 됨"(토글 리스너 중복 등록 버그) 버그를 잡아줘서 같이 고쳤다. `SkillSO` 6개가 전부 `SkillCategory.CoinDesign`
  이라 나머지 3개 탭은 아직 빈 화면 — 데이터 이슈(`Next_Tesk.md` 참고), UI는 완료. 자세한 내용은
  `Issues/Issue_SkillPanel.md` 참고.
- **완료** : 발행량 스킬 3종 Supply 효과 부여(2026-08-05). `추가발행권한`/`우회발행권한`은 (버튼 해금과 별개로)
  매 턴 자동 발행량 증가분(`TradeCalculator.SupplyGrowthPerTurn`=50)을 비율로 억제하는 신규 `EffectType.
  SupplyGrowthSuppress`(추가발행권한 20%, 우회발행권한 15%, 중복 시 합산)를 갖도록 결정했다 —
  `StatCalculator.CalculateSupplyGrowthSuppression()`이 `ApplyUnlockedPermanentSkillCashBonus`와 동일한
  패턴(재사용 불가 스킬 두 개만 최소 범위로 직접 체크)으로 계산해 `GrowSupply`에 넘긴다. `발행량은폐`는 이름과
  달리 Supply 자체는 건드리지 않고 기존 `DoubtDecrease`만 유지하기로 결정 — 설명 문구에 "의심도를 낮춰준다"로
  명시했다. 부수 작업으로 죽어있던 `PlayerStat.Volume`(거래량)도 `PriceCalculator`의 가격 변동폭 배율
  (`VolumeDeltaWeight`=0.01, 30턴 버프)로 연결하고, `거래량부풀리기`/`자전거래`/`고래계정운용` 구매 시
  반영되도록 `StatCalculator.ApplySkillUse`에 누락돼 있던 `VolumeIncrease` 케이스를 추가했다. 정기소각/
  반감기는 `SupplyDecrease` 수치를 -15/-30 → -1500/-3000으로 올리고 `[1회성] → [재사용형]`으로 바꿔 반복
  소각이 가능해지도록 했다. 새 `EffectType`이 스킬 패널에 코드명 그대로 노출되던 버그(`EventEffectFormatter`에
  라벨 누락)도 같이 고쳤다.
  - 검증 : Unity MCP Play 모드에서 `execute_code`로 직접 스킬 구매(`EventHub.RaiseSkillClicked`/
    `RaiseSkillPurchased`) 후 `MarketManager.NextTurn()`을 반복 호출해 확인 — 거래량 3종 누적 시
    `PlayerStat.Volume`이 0→50으로 쌓이고 `PriceCalculator`의 실제 로그 변동폭이 1.5배(50×0.01+1)로 정확히
    반영됨, 30턴 뒤 `VolumeBuffTurnsRemaining` 만료로 Volume이 0으로 리셋됨을 확인. 추가발행권한/우회발행권한
    구매 전후로 턴당 Supply 증가량이 50 → 40(20% 억제) → 32.5(35% 억제)로 정확히 감소함을 확인. 정기소각/
    반감기를 재사용형 전환 후 3회/2회 반복 구매해 매번 `PurchaseCount` 증가 + 비용 스케일링 + Supply 감소가
    정상 동작함을 확인(Supply는 `ClampStat`이 보유 코인수량 밑으로 못 내려가게 막는 기존 클램프에 걸림 —
    의도된 동작). 수치(억제율 20%/15%, `VolumeDeltaWeight`, 소각량 1500/3000)는 전부 임시값이라 실제
    플레이 밸런스 조정은 아직 필요.
- **완료** : 시사 이벤트 수동 트리거 죽은 코드 제거(2026-08-05). 사용자가 "시사 이벤트는 자동으로만 되는데
  수동 트리거는 없다"고 확인해줘서, 아무 UI도 발행하지 않던 `EventHub.OnNewsEvent`/`RaiseNewsEvent`와
  이를 구독하던 `MarketManager.HandleNewsEvent()`를 통째로 삭제했다. 자동 발생 경로(`NextTurn()` →
  `TriggerNewsEvent()`/`TriggerGuaranteedEvent()`)는 그대로 남겨뒀다 — `HandleNewsEvent`가 내부적으로
  호출하던 게 이 함수라 로직 자체는 안 건드림. 수동 트리거만 언급하던 주석(`LogPricePoint`/
  `UpdateStreamerReaction`)도 같이 정리하고, `PROJECT_ARCHITECTURE.md`/`Game_Formula.md`의 관련 서술도
  현재 코드에 맞게 고쳤다(`Logging.md`/`Issues/*.md`는 과거 기록이라 그대로 둠). `Next_Tesk.md`의 "후보 :
  UI 연결" 섹션은 이걸로 마지막 미완료 항목까지 없어져서 통째로 제거했다(안에 있던 완료 항목들은 전부
  `Completed_Tasks.md`에 개별 기록이 이미 있음).
  - 검증 : Unity MCP로 컴파일 확인(에러/경고 0) 후 Play 모드에서 `MarketManager.NextTurn()`을 60회
    반복 호출 — `EventLog`에 무조건 발생 이벤트(스트리머_소개/거래소_상장)가 정상 기록됨을 확인, 콘솔
    에러/경고 없음. 자동 발생 경로가 삭제 영향을 받지 않았음을 확인.
- **완료(문서만 뒤늦게 반영)** : 스킬 `SkillCategory` 데이터 배분(2026-08-05 확인). `Next_Tesk.md` 11번이
  "스킬 6개가 전부 `CoinDesign`이라 탭 3개가 비어있고 여론조작 스킬은 `SkillID`/`SkillSO`도 없다"고 남아있어서
  Play 모드로 실제 확인해보니, 이미 다른 세션에서 끝나 있었다 — 코드/데이터 자체는 문제없었고 `Next_Tesk.md`
  갱신만 누락된 상태였다. `SkillID` enum에 44개 스킬이 전부 정의돼 있고(코인설계 11/시장조작 9/여론조작 15/
  방어 9), `SkillManager.skillDatabase`에도 44개 전부 등록돼 카테고리별로 정확히 분류된다. 씬의 `SkillPanelUI`도
  탭 4개 + 아이콘 슬롯 44개(`Icon_SNS선동`, `Icon_로비` 등) + 카테고리별 `groupLabels`(`GroupLabel_Propaganda_0~2`,
  `GroupLabel_Depence_Exit_0~1` 등)까지 전부 연결돼 있다.
  - 검증 : Unity MCP Play 모드에서 `execute_code`로 `SkillID` enum 44개 전부를
    `SkillManager.Instance.GetSkillProfile()`로 조회해 전부 null이 아님(등록됨) + `category`별 개수(11/9/15/9)를
    확인. `find_gameobjects`/컴포넌트 리소스로 `SkillPanelUI`의 `tabs`(4개)/`icons`(44개)/`groupLabels`(카테고리별
    라벨 오브젝트) 직렬화 필드도 전부 채워져 있음을 확인.
- **완료** : 토글형(재사용 불가) 스킬 활성화 시스템 일반화(2026-08-05, `Next_Tesk.md` 12번). `SkillManager.
  IsEnabled`/`EnableSkill`/`DisableSkill`/`GetActiveSkills`가 아무도 안 부르는 죽은 배관이었던 걸 실제로
  연결했다. `SkillManager.HandlePurchase()`의 1회성(재사용 불가) 구매 분기에 `EnableSkill(skill.Profile.id)`를
  추가(끄는 UI가 없으니 구매=영구 활성으로 취급). 이걸 안전하게 켜기 전에 `StatCalculator.ApplySkills()`의
  재적용 제외 목록에 `SupportIncrease`/`GrowthIncrease`/`DoubtIncrease`/`DoubtDecrease`(`ApplyJob()`과 동일 —
  구매 시점에 `ApplySkillUse`가 이미 1회 반영했고 이후 감쇠/누적이 의도된 동작이라 매 턴 재적용하면 무한정
  쌓이는 버그가 됨, 과거 Job의 DoubtDecrease 매 턴 재적용 버그와 동일 패턴)와 `CashBonus`(이미
  `ApplyUnlockedPermanentSkillCashBonus`가 전담 중이라 중복 방지)를 추가로 넣었다. 결과적으로 이 루프는
  `PositiveEventRate`/`NegativeEventRate`/`ExitUnlock`만 실제로 재적용하게 된다 — 지금 스킬 중엔 이 타입을
  쓰는 게 없어서 기존 동작(Growth/Support 감쇠, CashBonus/SupplyGrowthSuppress 유지)엔 변화가 없고, 이 타입을
  가진 1회성 스킬이 추가되면 앞으로 자동으로 매 턴 재적용된다. `SupplyGrowthSuppress`/`CashBonus`의 기존 전용
  함수(`CalculateSupplyGrowthSuppression`/`ApplyUnlockedPermanentSkillCashBonus`)는 구조가 달라서(비율 반환/
  중복 위험) 그대로 남겨뒀다.
  - 검증 : Unity MCP Play 모드에서 `추가발행권한` 구매 후 `GetActiveSkills()`가 실제로 1개를 반환함을 확인
    (변경 전엔 항상 0개). 40턴 반복 실행해 변경 전과 정확히 동일한 수치(Growth 62.1→59.1→56.2→53.4로 계속
    감쇠, CashBonus 15 고정 유지, 중복 없음)가 나옴을 확인해 회귀 없음을 검증. `우회발행권한`까지 추가
    구매해 `GetActiveSkills()` count=2, 턴당 Supply 증가량이 여전히 40→32.5로 정확함도 재확인. 콘솔 에러/
    경고 없음.
- **완료** : CashBonus도 위 토글 루프로 흡수(같은 날 후속, 2026-08-05). `추가발행권한` 하나만 하드코딩해서
  보던 `ApplyUnlockedPermanentSkillCashBonus()`를 삭제하고, `ApplySkills()`의 CashBonus 제외를 풀어 일반
  토글 루프가 처리하게 했다. 구매 즉시(이번 턴) 반영이 필요해서 `ApplySkills()`의 내부 순회 로직을
  `StatCalculator.ApplyToggleSkillEffects(stat, skillProfile)`라는 공용 함수로 뽑아 `ApplySkills()`(매 턴
  전체 순회)와 `SkillManager.HandlePurchase()`(방금 산 스킬 하나만 즉시 반영)가 같이 쓰게 했다 — 구매
  시점에 전체 순회(`ApplySkills(stat)`)를 다시 부르면 이미 활성화돼 있던 다른 스킬들의 CashBonus까지
  이번 턴에 또 더해져 중복되므로, 반드시 방금 산 스킬 하나로 범위를 좁혀야 한다는 게 포인트.
  - 검증 : Play 모드에서 `추가발행권한` 구매 직후 CashBonus=15, 5턴 뒤에도 15(중복/감쇠 없음), CashBonus
    없는 `우회발행권한`을 추가 구매한 시점에도 15 그대로(다른 스킬 구매가 기존 스킬 값을 안 건드림),
    그 뒤 5턴 더 진행해도 15 유지, 발행량 억제(40→32.5)도 그대로임을 확인. 컴파일·콘솔 에러 없음.
- **완료** : 죽은 코드 정리 + 중복 로직 통합(2026-08-05). 사용자 요청으로 전체 코드베이스를 서브에이전트로
  훑어 죽은 코드/중복을 찾고 정리했다.
  - **죽은 코드 삭제** : `ScreenWarningBorderUI`(씬의 어떤 GameObject에도 안 붙어있어 경고 테두리 연출
    전체가 런타임에 아예 작동 안 했음 — 스크립트 GUID로 전체 씬/에셋 재확인, 프로젝트에 `.prefab` 자체가
    0개라 프리팹 경유도 아님), `SkillManager.DisableSkill()`(정의부 말고 호출 0건 — 1회성 스킬은 끄는 UI가
    없어서 애초에 안 쓰임), `JobManager.HasJob()`/`ClearJob()`(호출 0건, `CurrentJob` getter만 실제로 쓰임) +
    이것만 쓰던 `RuntimeJobData.selected` 필드도 같이 제거.
  - **모달 Open/Close 보일러플레이트 통합** : `TradeModalUI`/`CoinControlModalUI`/`SkillPanelUI`/
    `EventLogPanelUI`/`EventNotificationUI`/`SettingsUI` 6곳이 각자 구현하던 "`EventHub.RaiseGamePaused()`
    + 패널 SetActive(true)" / "`RaiseGameResumed()` + SetActive(false)" 패턴을 신규
    `UI/Utils/ModalPause.cs`(정적 헬퍼, `Open(GameObject)`/`Close(GameObject)`)로 뽑았다. 베이스 클래스
    상속은 안 씀(패널 대상이 자기 자신 gameObject/별도 panel 필드/여러 자식 패널 등 클래스마다 달라서
    가상 메서드 오버라이드보다 정적 헬퍼가 더 간단함). `EventNotificationUI`는 원래 SetActive 이후에
    RaiseGamePaused를 부르던 순서가 다른 5곳과 달랐는데, 통합하면서 다른 곳과 동일하게 일시정지를 먼저
    부르도록 자연히 맞춰졌다(같은 프레임 내 순서라 시각적 차이 없음).
  - **`StatCalculator` Support/Growth/Doubt 1회성 적용 로직 통합** : `ApplyJobSelection`/`ApplySkillUse`가
    각자 구현하던 SupportIncrease/GrowthIncrease/DoubtIncrease/DoubtDecrease 반영 + `JobSkillXBonus` 갱신
    로직을 `ApplyOneShotBonusEffect(stat, effect, clampDoubtFloor)`로 통합했다. 스킬만 Doubt 감소가 0
    밑으로 안 내려가는 기존 차이는 `clampDoubtFloor` bool로 유지(Job=false, Skill=true). 이벤트에도
    재사용되는 범용 디스패처 `ApplyEffect()`(보너스 추적 없음)는 의도적으로 그대로 뒀다 — 통합하면 이벤트도
    `JobSkillXBonus`를 건드리게 돼버려 의미가 달라짐.
  - 검증 : 컴파일 클린 확인 후 Play 모드에서 `Resources.FindObjectsOfTypeAll<SkillPanelUI>()`로 비활성
    패널을 찾아 `Open()`/`Close()` 직접 호출 — `Time.timeScale`이 1→0→1로, `activeSelf`가 False→True→False로
    정확히 바뀜을 확인(`ModalPause` 배관 검증). `추가발행권한` 구매 시 Growth=30/CashBonus=15/Doubt가
    스킬 전용 0-플로어에 걸려 0으로 유지(음수로 안 내려감, `clampDoubtFloor` 검증), `거래량부풀리기` 추가
    구매로 Doubt가 0+5=5로 정확히 오름(DoubtIncrease 경로 검증), 3턴 뒤에도 CashBonus 15 유지(회귀 없음).
    콘솔 에러/경고 없음.
- **완료** : 재시작(2회차 플레이) 버그 2건 수정(2026-08-05, 사용자 실플레이 제보). 둘 다 "Managers"
  GameObject가 `DontDestroyOnLoad`라 두 번째 플레이부터 `Awake`/`Start`가 다시 안 불리는 동일 계열의 버그였다.
  - **버그 1 : 직업이 재시작 시 적용 안 됨.** `CharacterSelectUI.ConfirmSelection()`은
    `JobSelectionHandoff.SelectedJob`에 선택한 직업을 담아두고, `JobManager.Start()`가 `SampleScene` 로드 후
    그 핸드오프를 소비해서 `SelectJob()`을 호출하는 구조였다. `JobManager`가 `DontDestroyOnLoad`라 최초
    1회차에만 `Start()`가 실행되고, 2회차부터는 같은 인스턴스가 씬을 넘어 살아남아 `Start()`가 다시 안 불려서
    핸드오프가 영원히 소비되지 않았다(직업이 그대로 이전 판 값으로 남거나 안 바뀜). `PlayerManager`/
    `MarketManager`/`TimeManager`/`SkillManager`는 이미 `ConfirmSelection()`이 2회차부터 `ResetState()`를
    직접 불러 이 문제를 피해가고 있었는데 `JobManager`만 빠져있었다 — 같은 분기에
    `JobManager.Instance.SelectJob(jobs[selectedIndex])` 직접 호출을 추가해 맞췄다(1회차는 `Managers`가 아직
    없어 이 분기를 안 타므로 기존 핸드오프+`Start()` 경로가 계속 처리한다).
  - **버그 2 : 재시작하면 설정(SettingsUI) 메뉴가 안 열림.** `SettingsUI`가 실수로 `TimeManager`/`JobManager`
    등 실제 싱글턴들과 같은 "Managers" GameObject에 컴포넌트로 얹혀 있었다. 2회차 씬 로드 때 새로 생긴
    "Managers" 복제본의 `SettingsUI.Start()`는 (Destroy가 프레임 끝까지 지연되므로) 정상 실행되어 새
    `SettingsBtn`에 리스너를 등록하지만, 같은 프레임 끝에 형제 컴포넌트들(`TimeManager` 등)의 `Awake`가
    "이미 `Instance` 있음"을 보고 `Destroy(gameObject)`를 불러 그 복제 `SettingsUI`까지 통째로 파괴돼버려서,
    `SettingsBtn`의 클릭 리스너가 죽은 인스턴스를 가리키게 됐다. Unity 에디터의 "Copy Component/Paste As
    New"와 동일한 `UnityEditorInternal.ComponentUtility.CopyComponent`/`PasteComponentAsNew`를 코드로 호출해
    `SettingsUI`를 (Managers처럼 영구 지속될 필요 없는) `Main_Canvas`로 옮기고 원본은 제거했다 — 19개
    직렬화 필드(참조) 전부 그대로 보존됨을 확인. `GameStarter`(BGM 1회 재생)도 같은 GameObject에 있었지만
    1회성 트리거라 죽기 직전에 이미 효과가 발생해 실제로는 안 깨져서 그대로 뒀다.
  - 검증 : 두 버그 모두 이 자동화 환경의 알려진 한계(에디터 창 미포커스 시 Play 모드 프레임이 거의 안
    진행돼 `Start()`/씬 전환이 실제로 안 끝남 — `Time.frameCount`/`CurrentGameDate`가 멈춰있고
    `playmode_transition` 상태에 걸려있는 것으로 확인)에 걸려 실제 씬 전환으로는 재현이 불가능했다. 대신
    코드 레벨에서 원인을 특정하고, 각 버그의 핵심 호출 경로를 Play 모드에서 직접 실행해 검증했다 : (1)
    `PlayerManager.Instance != null` 분기를 그대로 재현해 `JobManager.Instance.SelectJob()`을 두 번 다른
    직업으로 호출 — `CurrentJob`/`Growth`가 매번 정확히 갱신됨을 확인. (2) 이동된 `SettingsUI`의
    `openButton.onClick.Invoke()`/`closeButton.onClick.Invoke()`를 실제 클릭처럼 호출 — `Time.timeScale`
    1→0→1, 패널 `activeSelf` False→True→False로 정상 동작함을 확인. 컴파일·콘솔 에러 없음.
- **완료** : 신규 스탯 "의심도 하락"(`DoubtDecline`, 여론조작 스킬 3종 전용, 2026-08-07). 기존 `DoubtDecrease`는
  구매 즉시 1회 차감이지만, 여론조작 카테고리의 실시간여론관리(재사용형, 총량 20)/수상경력홍보(1회성,
  총량 5)/후기마케팅(1회성, 총량 5) 3개만 총량을 즉시 깎지 않고 매 턴 0.5%씩(200턴에 걸쳐 균등 분할) Doubt를
  깎도록 바꿨다. 방어/코인설계 카테고리의 기존 `DoubtDecrease` 9개는 그대로 즉시 감소 유지. 신규
  `Assets/Scripts/Runtimes/Systems/BuffCalculator.cs`(static, `TradeCalculator`/`EventCalculator`와 동일 패턴)를
  만들어 N턴짜리 버프 로직을 전담시키고, 기존 `StatCalculator`/`PlayerStat`에 흩어져 있던 `Volume`(거래량) N턴
  버프 로직(`VolumeBuffTurnsRemaining` 카운트다운)도 여기로 이관했다. `PlayerStat.DoubtDeclines`는
  `Dictionary<SkillID, DoubtDeclineBuff>`라 스킬별로 독립적으로 진행된다 — 서로 다른 스킬의 하락은 합산 차감되고,
  같은 스킬을 재구매하면 그 스킬 항목만 새 값으로 덮어써진다(재사용 시 갱신, `Volume` 버프와 동일 패턴). 처음엔
  `PlayerStat`에 단일 필드(`DoubtDeclinePerTurn`/`DoubtDeclineTurnsRemaining`) 하나로 구현했다가, 서로 다른 스킬을
  동시에 사면 하나가 다른 하나를 덮어써버리는 문제가 있어 스킬별 딕셔너리로 다시 짰다. `EffectType`에
  `DoubtDecline`(13)을 끝에 추가(중간 삽입 금지 관례 유지)하고 여론조작 3개 스킬 .asset의 `effectType`만
  `DoubtDecrease`(2)→`DoubtDecline`(13)로 교체, `EventEffectFormatter`/`UIFormat`의 라벨·부호 switch에도
  추가해 스킬 정보 패널에 새 UI 코드 없이 자동 반영되게 했다. 작동 방식/호출 스택 상세는
  `Issue_DoubtDecline.md` 참고.
- **완료** : 스킬 가격 일괄 인상(2026-08-07). 전체 스킬 42개의 `baseCost`를 1.5배로 올리고, `로비`
  스킬만 예외로 2배 인상했다. 코드 변경 없음, 순수 데이터(.asset) 조정 — 재조정 필요 시 `Next_Tesk.md`
  "밸런스 수치 조정" 참고.
- **완료(부분)** : 엑시트 엔딩(영웅/엑시트) 전용 씬 분리(2026-08-08). 기존엔 `SampleScene` 안 `EndingResultUI`
  오버레이 패널로 문구만 표시했는데, 체포/거지처럼 별도 씬으로 관리하기로 방향을 바꿨다. 신규
  `Assets/Scripts/UI/EndingScene/ExitEndingSceneUI.cs` + `Assets/Scenes/ExitEndingScene.unity`(`EndingScene.unity`를
  복제해 만듦 — Camera/EventSystem/CanvasScaler(1920x1080)/암전 코루틴 구조 동일, 배경 스프라이트는 Hero/Exit
  공용 1장 + 문구만 분기)를 추가하고 Build Settings에 등록(`GameSceneManager.LoadExitEnding()`)했다.
  `EndingResultUI`는 오버레이 표시 로직(panel/titleText/descriptionText/Describe())을 전부 걷어내고 4종 엔딩
  전부(체포/거지는 `EndingScene`, 영웅/엑시트는 `ExitEndingScene`) 전용 씬으로 넘기기만 하는 디스패처로
  단순화됐다. **미완료** : 배경 이미지(`ExitEndingSceneUI.bgSprite`)가 아직 비어있음 — `generate_image`
  MCP 도구가 fal/openrouter 둘 다 API 키 미설정으로 막혀서 코드/씬 구조만 먼저 완성함. 키 등록(Unity MCP
  Tools 창 Asset Generation 탭) 후 이어서 생성하거나, 이미지를 직접 받아서 `Assets/Sprites/`에 넣고
  `ExitEndingScene.unity`의 `Main_Canvas(ExitEndingSceneUI).bgSprite`에 연결하면 됨.
- **완료** : SFX 추가 — 스킬 구매 / 차트 파티클(2026-08-08). `SkillPanelUI.confirmSfx`는 애초에
  `TradeModalUI`/`CoinControlModalUI`와 독립된 필드였음을 확인(씬에서 우연히 같은 클립을 공유했을 뿐) —
  코드 변경 없이 씬에서 `스킬구매소리.mp3`로 재할당만 했다. 차트 파티클은 `PriceChartCandles`가
  MonoBehaviour가 아니라 `[SerializeField]`를 못 써서, `gameOverSfx`와 동일한 기존 패턴대로 호출부인
  `PriceChartUI`에 `particleBurstSfx` 필드를 추가하고 `HandleMarketUpdated`의 `SpawnBurstOnLast()` 호출
  옆에서 재생하도록 했다(`파티클소리1.wav` 할당). 둘 다 씬 저장 완료, 컴파일 에러 없음.
- **완료** : 캐릭터 선택 "초기 자금" 표시 버그 수정(2026-08-08). `CharacterSelectScene.unity`의
  `StartingFundText`가 `CharacterSelectUI.cs`와 전혀 연결 안 된 정적 텍스트로 "초기 자금 : ₩ 10,000,000"을
  하드코딩하고 있었다 — 실제 시작 자원(`PlayerManager.currentMoney`/`currentCoins` = 10,000/10,000)과
  전혀 다른 값이었고 코인 수량은 애초에 표시조차 안 됐다. 값을 실제 수치로 고치고 코인 줄도 추가해
  2줄로("초기 자금 : ₩ 10,000\n보유 코인 : 10,000개") 박스 높이를 40→80으로 늘렸다(아래 "특성" 섹션까지
  132px 여유가 있어 안 겹침, Play 모드 스크린샷으로 확인). 이 시점까지는 여전히 정적 텍스트였다 — 바로
  다음 항목("직업별 초기 자금")에서 코드로 연결됨.
- **완료** : 직업별 초기 자금 차등 적용(2026-08-08). 코인 보유량(10,000)은 전 직업 공통으로 유지하고,
  시작 자금만 직업별로 다르게 밸런스를 맞췄다. `JobSO`에 `startingMoney`(long, 기본 10000) 필드를
  추가하고, `JobManager.SelectJob()`이 직업 선택 시 `PlayerManager.Instance.currentMoney = job.startingMoney`로
  덮어쓰게 했다(1회차 SampleScene 진입 시 `JobManager.Start()`가, 2회차부터는 `CharacterSelectUI.
  ConfirmSelection()`이 호출 — 두 경로 모두 `PlayerManager.ResetState()` 이후에 실행되므로 항상 이 값으로
  최종 확정됨). `PlayerManager`엔 `StartingCoins`(10000) 상수를 추가해 코인 쪽 매직넘버가 다시 흩어지지
  않게 했다. 자금 값은 각 직업의 기존 더미 스탯 효과와 반비례하도록 잡았다(일반인=10,000 기준,
  기업인=10,000/유튜버=11,000/개발자=11,000/연예인=9,000/정치인=12,000 — 스탯 보너스가 강할수록 자금은
  적게, 약할수록 자금으로 보완) — 스탯 값 자체는 손대지 않음. `CharacterSelectUI.cs`에 `startingFundText`
  필드를 추가해 `RefreshDetail(job)`에서 직업별 값을 실시간으로 표시하도록 했다(위 버그 수정 때는 아직
  정적이었던 걸 여기서 완전히 코드 연결함). Play 모드에서 `execute_code`로 버튼 클릭을 직접 호출해
  일반인(₩10,000)/정치인(₩12,000) 전환이 정상 반영됨을 스크린샷으로 확인. 값 자체는 여전히 임시
  밸런스라 실제 플레이/기획 확정 후 재조정 필요(`Next_Tesk.md` "직업(Job) 프로필" 참고).
- **완료** : 직업별 밸런스 2차 조정 + 시작 코인도 직업별 차등(2026-08-08, 피드백 반영). ① 일반인은
  "특성 없는 기준 직업"으로 재정의 — 기존 `SupportIncrease 10`/`DoubtDecrease 10` 두 효과를 전부 제거해
  `effects: []`. ② 기업인은 "자금은 넘치지만 그만큼 눈에 띄는" 컨셉으로 재설계 — `startingMoney`를
  10,000→1,000,000으로 대폭 인상하고, 대가로 `DoubtIncrease 25`(신규 효과, 기존 `CashBonus 10`은 유지)를
  추가해 시작부터 의심도 25(체포 엔딩 기준 100 중 1/4)를 안고 시작하게 했다. ③ 시작 코인 수량을
  `PlayerManager.StartingCoins`(전 직업 공통 10,000) 고정에서 `JobSO.startingCoins`(직업별) 필드로
  바꿨다 — `MarketManager.ResetState()`가 `Supply = InitialSupply(2000) + currentCoins`로 초기 발행량을
  잡으므로 코인 수량 차이가 그대로 시작 Supply(현재 `PriceCalculator.MaxSupply=100000` 대비 여유 충분)에도
  반영된다. 기업인=5,000(현금 위주라 코인 보유는 적게), 개발자=15,000(기술 창업자 컨셉으로 지분성 코인
  보유 많게), 연예인=12,000, 유튜버=11,000, 정치인=7,000(이해상충 회피 컨셉으로 적게), 일반인=10,000(기준
  유지). `JobManager.SelectJob()`에 `PlayerManager.Instance.currentCoins = job.startingCoins` 한 줄
  추가하고 `CharacterSelectUI`의 코인 표시도 고정 상수 대신 `job.startingCoins`를 읽도록 바꿨다. 검증
  중 `CharacterSelectUI.EffectLabel/EffectIcon`이 `DoubtIncrease`를 처리하는 case가 없어 특성 패널에
  "DoubtIncrease +25%"로 raw enum 이름이 그대로 노출되는 버그를 발견해 같이 고쳤다(`doubtIcon` +
  "의심도 증가" 라벨 추가). Play 모드 스크린샷으로 일반인(특성 없음)/기업인(자금 100만·코인 5천·의심도
  증가 25% 정상 표시) 확인.
- **완료** : 직업별 밸런스 3차 조정 — 스킬 가격 기준으로 자금 재조정(2026-08-08, 피드백 반영).
  2차 조정 때 잡은 자금(9,000~12,000원대)이 실제 스킬 가격(`Assets/Profile/스킬_프로파일/`의
  `baseCost`가 코인설계/시장조작/여론조작은 ₩15,000~180,000, 방어 7종은 ₩750,000~1,500,000, 로비만
  예외로 ₩120,000)과 비교해보니 가장 싼 스킬(₩15,000)조차 못 사는 수준이라는 지적을 받아 스킬 가격
  스케일에 맞춰 다시 잡았다. 일반인=40,000/유튜버=45,000/개발자=45,000/연예인=35,000/정치인=50,000
  (직전 9~12천원대에서 4~5배로 인상, 기존 상대적 우열 순서는 유지 — 저가~중가 스킬 1~2개는 시작부터
  살 수 있게). 기업인(1,000,000)은 앞선 3차 이전 값 그대로(사용자가 직접 지정한 값이라 유지) — 이미
  방어 최고가 스킬까지 살 수 있는 수준이라 추가 조정 불필요. 코인 수량은 스킬 구매에 안 쓰이는 값이라
  (스킬은 `PlayerManager.TrySpend`로 현금만 소비) 이번엔 손대지 않음.
- **완료** : 버그 수정 2건(2026-08-08, 플레이테스트 피드백).
  ① 메인 BGM이 한 번 재생되고 안 멈추는 문제 — `AudioManager.PlayBGM()`이 `AudioSource.Play()`만 호출하고
  `loop`를 세팅한 적이 없어서, `TitleScene`의 `bgmSourceA`/`bgmSourceB` 두 `AudioSource` 모두 Inspector
  기본값(`Loop: 0`)에 의존하고 있었다 — 둘 다 꺼져있어서 클립이 끝나면 멈췄다. Inspector 값을 고치는
  대신 `PlayBGM()`에서 `nextSource.loop = true`를 `Play()` 직전에 강제해, 이후 어떤 씬에서 어떤
  AudioSource를 새로 연결해도 Inspector 설정과 무관하게 항상 루프되도록 근본 수정.
  ② 스킬 구매 버튼을 누를 때마다(성공할 때마다) 파티클 버스트가 뜨는 게 스팸처럼 느껴진다는 피드백으로
  `SkillPanelUI.HandlePurchaseSucceeded()`의 `UIBurstParticle.Spawn(...)` 호출을 제거 — 구매 사운드만
  남기고 파티클 연출은 완전히 뺐다. 더 이상 안 쓰는 `PurchaseBurstIntensity` 상수도 같이 삭제.
- **완료** : 기업인 자금/코인 미세 조정(2026-08-08). 코인 5,000→4,000(−1,000), 자금 1,000,000→3,000,000
  (+2,000,000). 순수 데이터(.asset) 조정, 코드 변경 없음.
- **완료** : 부수 효과로 의심도가 오르는 스킬 12종의 `DoubtIncrease` 수치 전부 절반으로 축소(2026-08-08).
  대상 : 시장조작 6종(거래량부풀리기/고래계정운용/시세방어/자전거래/펌핑/허수매도벽/허수매수벽 — 이 중
  거래량부풀리기·시세방어·허수매도벽·허수매수벽은 5→2.5, 고래계정운용·자전거래·펌핑은 10→5),
  여론조작 4종(SNS선동/댓글부대운영/커뮤니티알바/파트너십발표, 전부 5→2.5), 코인설계 1종(독약조항,
  5→2.5). `기업인` 직업의 `DoubtIncrease 25`(직업 효과, 스킬 아님)는 이번 범위 밖이라 손대지 않음.
  순수 데이터(.asset) 조정, 코드 변경 없음.
- **완료** : 버그 수정 2건(2026-08-08, 플레이테스트 피드백 2차).
  ① 여론조작 스킬(실시간여론관리/수상경력홍보/후기마케팅)의 "의심도 하락"(`DoubtDecline`) 버프가 0 밑으로
  안 멈추고 계속 깎여 마이너스까지 내려가던 문제 — `BuffCalculator.TickDoubtDeclines`가 매 턴
  `stat.Doubt -= entry.Value.PerTurn`만 하고 하한 체크가 없었다. 즉시형 스킬 Doubt 감소(`ApplySkillUse`의
  `clampDoubtFloor: true`)는 이미 0에서 멈추는데 이 분할형만 그 원칙이 빠져있던 불일치였다. `Mathf.Max(0f, ...)`
  로 맞춰 고쳤다. `StatCalculator.ClampStat`의 전역 하한(-100)은 그대로 뒀다 — 이건 정치인 직업의
  `DoubtDecrease 10`이 선택 시점(Doubt=0)에 즉시 0으로 도로 잘려 무효화되는 걸 막으려고 의도적으로 열어둔
  것이라(`StatCalculator.cs` 주석 참고) 손대면 정치인 특성이 죽는다 — 이번 버그는 그 전역 하한이 아니라
  분할 하락 버프에 국소적으로 하한이 빠졌던 게 원인이었다.
  ② 재사용형 스킬 구매 버튼을 눌러도 반응이 없어 구매됐는지 안 보이던 문제 — 지난 세션에 파티클 버스트를
  뺀 뒤로는 사운드 말고 아무 시각 피드백이 없었다(재사용형은 `locked` 상태가 항상 false라 잠금 색상도 안
  바뀜). TimeUI의 PauseBtn/SpeedBtn/PlayBtn에 이미 붙어있던 `ButtonPressEffect`(누르면 축소, 떼면 원상복귀,
  코드 없이 컴포넌트만 붙이면 동작)를 `PurchaseBtn`에도 그대로 붙였다. 붙이고 나서 "버튼 밑에 깔린
  `PurchaseBtnShadow`(입체 음영)는 왜 같이 안 줄어드냐"는 피드백을 받아 확인해보니, 이 음영은 버튼의
  자식이 아니라 `DetailBox` 밑 형제 노드였다(버튼 뒤에 깔리려면 자식이 아니라 형제+낮은 sibling index여야
  해서 — 자식이면 부모 위에 그려짐). 그래서 `ButtonPressEffect`에 옵션 필드 `linkedShadow`(Transform)를
  추가해 지정돼 있으면 버튼과 같은 배율로 같이 눌리게 했다(비워두면 기존 PauseBtn/SpeedBtn/PlayBtn처럼
  그대로 동작). `PurchaseBtn`의 `linkedShadow`를 `PurchaseBtnShadow`로 연결.
- **완료** : 여론조작 스킬 설명에 의심도 즉발형/지속형 구분 표시(2026-08-08). `EventEffectFormatter.
  DescribeEffect`가 `DoubtDecrease`/`DoubtIncrease`/`DoubtDecline` 세 `EffectType`을 전부 "의심도"라는
  같은 라벨로 묶고 있어 즉시 적용인지 매 턴 나눠서 깎이는지 구분이 안 되던 문제 — 라벨을
  `DoubtDecrease`/`DoubtIncrease` → "의심도(즉시)", `DoubtDecline`(여론조작 스킬 3종 전용, 총량의 0.5%씩
  200턴에 걸쳐 분할 차감) → "의심도(200턴에 걸쳐 하락)"으로 분리했다. `DoubtDecline`은 이벤트 프로파일에서
  안 쓰는 스킬 전용 타입이라 이벤트 로그/알림(`EventLogPanelUI`, `EventNotificationUI`)에는 영향 없음.
- **완료** : 스킬 설명 스탯에 좋음/나쁨 색상 적용(2026-08-08). `EventEffectFormatter.BuildEffectsText`에
  `colorize` 파라미터(기본 false)를 추가해, `EffectType`별로 플레이어에게 좋은 효과인지 고정 판정한 뒤
  TMP `<color=...>` 태그로 감싸도록 했다. 기존 `PositiveColor`(#B1FFB1)/`NegativeColor`(#FFBAB1)를 그대로
  재사용했고, `SkillPanelUI.RefreshDetail`에서만 `colorize: true`로 호출해 이벤트 로그/알림 쪽 표시는
  그대로 무색으로 남겨뒀다(요청 범위 밖이라 손대지 않음). Supply/Volume 계열의 좋음/나쁨 판정은
  `Game_Formula.md` 설명(발행량 증가=희석/나쁨, 거래량 증가=영향력 증가/좋음) 기준의 추정치라 실제 플레이
  감각과 다르면 추후 재조정 필요.
- **완료** : 스킬 아이콘 테두리로 1회성/재사용 구분(2026-08-08). `SkillSO.isReusable` 판정을 그대로 써서,
  `SkillPanelUI.IconSlot.border` 필드를 `Image` 대신 `UnityEngine.UI.Outline`으로 추가했다 — 아이콘 뒤에
  오프셋 사본을 겹쳐 그리는 이펙트라 새 GameObject나 좌표 계산 없이 컴포넌트 하나로 해결됨
  (`RefreshSelectionHighlight`에서 `border.effectColor`를 재사용형=`PositiveColor`/1회성=`NegativeColor`로
  갱신, 선택된 아이콘도 테두리 유지). 씬 작업(스킬 아이콘 44개 전부 `Outline` 컴포넌트 추가 +
  `SkillPanelUI.icons[]`의 `border` 필드 연결)은 이번 세션에 새로 연결된 Unity MCP로 직접 처리했고,
  `SampleScene.unity` 저장까지 완료. Play 모드로 열어서 재사용형=초록/1회성=빨강 테두리 정상 표시 확인.
- **완료** : 스킬 설명 색상 텍스트 굵게 + 진한 색으로 변경(2026-08-08, 2차 피드백 포함). `EventEffectFormatter.
  BuildEffectsText`의 `colorize` 경로가 만드는 `<color=#B1FFB1/#FFBAB1>` 텍스트가 파스텔톤이라 잘 안 보인다는
  피드백으로 처음엔 `<b>` 태그만 추가했는데, 굵게만 해도 여전히 밝아서 안 보인다는 재피드백을 받아 색 자체를
  `#009900`(초록)/`#CC0000`(빨강)로 바꿈. `EventEffectFormatter.PositiveColor`/`NegativeColor`(이벤트 로그
  카드·스킬 아이콘 테두리에서 공유하는 파스텔 상수)는 그대로 두고, `BuildEffectsText` 내부 색상 문자열만
  독립적으로 바꿔서 다른 화면에는 영향 없음. 스킬 패널에서만 쓰이는 경로라 이벤트 로그/알림 표시는 영향 없음.
- **완료** : 스킬 아이콘 밑에 이름 + 구매횟수 표시(2026-08-08). `SkillPanelUI.IconSlot`에 `label`
  (`TextMeshProUGUI`) 필드를 추가해 아이콘 44개 전부 밑에 라벨을 붙였다. 처음엔 이름+횟수를 2줄로 아이콘
  바로 밑에 배치했는데, Play 모드 스크린샷으로 확인해보니 실제 그리드 간격(아이콘 하단~다음 줄
  서브카테고리 라벨 사이)이 20유닛 정도밖에 안 돼서 기존 서브카테고리 라벨("공급 구조" 등)과 겹쳤다.
  사용자 피드백을 받아 방식을 바꿈 — 1회성 스킬은 이름만 표시(1회성은 최대 1회라 횟수 표시 의미가
  없음), 재사용형만 이름 옆에 "(N회)"를 한 줄로 붙여 표시(`RefreshSelectionHighlight`에서
  `profile.isReusable` 분기, `SkillManager.GetPurchaseCount` 재사용). 라벨 `RectTransform`은 높이
  20(폭 150)으로 좁혀 서브카테고리 라벨과 안 겹치게 했고, TMP `enableAutoSizing`(6~16)으로 스킬 이름
  길이(예: "거래량부풀리기 (0회)")에 맞춰 폰트가 자동으로 줄어들게 함. 씬 작업(라벨 44개 생성 + 위치/폰트
  세팅 + `IconSlot.label` 필드 연결)은 이번 세션에 새로 연결된 Unity MCP로 처리했고, `SampleScene.unity`
  저장까지 완료. Play 모드에서 탭 4개 전부 스크린샷으로 겹침 없이 정상 표시되는 것 확인.
- **완료** : 의심도 조절 난이도 상향 + 신규/보완 스킬 2종(2026-08-08). "의심도 조절이 너무 쉽다" 피드백
  반영 — 여론조작 3종(후기마케팅/수상경력홍보/실시간여론관리, `EffectType.DoubtDecline`)이
  `BuffCalculator.TickDoubtDeclines`에서 동시에 합산 차감되고 로비(즉시 -30)까지 더해지는데
  `costMultiplier`가 낮아 반복 구매 비용이 거의 안 올라서 생기던 문제.
  - ① `costMultiplier` 인상 : 실제 값을 확인해보니 스킬군이 두 티어(코인설계/방어 1.15, 시장조작/여론조작
    1.2)로 나뉘어 있었음 — 사용자가 지정한 "현재 1.2"와 일치하는 시장조작+여론조작 24개 스킬만
    1.2 → **1.4**로 일괄 변경(코인설계/방어의 1.15는 요청 범위 밖이라 유지, `sed`로 일괄 치환).
  - ② `로비.asset` : `baseCost` 120,000 → **600,000**(5배), `costMultiplier`도 1.15 → 1.4로 별도 인상
    (①과 동일 목표치, 사용자가 로비를 ①과 같은 항목으로 지정했기 때문).
  - ③ 스킬 구매 횟수 상한 신설 : `SkillManager`에 `MaxPurchaseCount = 10` 상수를 추가하고
    `HandlePurchase()`에서 재사용형(`isReusable`) 스킬이 `PurchaseCount >= 10`이면 구매를 막도록
    가드 추가(1회성 스킬은 기존 `IsUnlocked` 체크로 이미 재구매가 막혀있어 해당 없음). 직업별로 상한을
    다르게 주는 건 이번 스코프 밖이라 전역 상수만 두고 `Next_Tesk.md`에 후속 후보로 남김.
  - ④ `기업인.asset` : `startingMoney` 3,000,000 → 2,000,000, `startingCoins` 4,000 → 3,000(1000개
    감소) — 초기 자본으로 의심도 하락 스킬을 넉넉히 사던 루트 완화.
  - ⑤ FOMO유도 신규 효과(`시장조작`, 기존 `effects: []` — 과거 `Issue_SkillBalancePatch.md`에 "일부러
    가격 하락 후 저점 재매수, 수치 미정"으로 보류됐던 스킬) : 보상 없는 순수 디버프 유틸리티 스킬로
    확정(과열 진화 등 특수 상황 전용, 사용자 컨펌). `baseCost` 37,500 → 90,000, `effects`에
    `SupportIncrease -20`, `GrowthIncrease -20` 추가.
  - ⑥ 인플루언서계약 스탯 보완(`여론조작`, 기존 `effects: []` — 과거 "투자자 +1000" 텍스트만 있고 실제
    반영 안 됐던 스킬) : 같은 카테고리 baseCost 60,000대 비교군(SNS선동/커뮤니티알바/방송출연/유명인홍보)
    스케일에 맞춰 `SupportIncrease +10`, `GrowthIncrease +7.5`, `DoubtIncrease +2.5` 추가.
  - Unity MCP Play 모드(`execute_code`)로 로비/FOMO유도 현재 비용(`GetCurrentCost`), FOMO유도/
    인플루언서계약 `effects` 값, FOMO유도 11회 연속 구매 시도 후 `PurchaseCount`가 10에서 막히고
    `IsUnlocked`는 유지되는 것까지 전부 확인. 첫 검증 시도에서는 디스크에 저장한 `.asset` 변경이 Unity에
    반영 안 된 채(리임포트 전) 옛날 값이 나와서, `refresh_unity(force)`로 강제 리프레시 후 재검증해
    통과했다 — 스크립트가 아닌 `.asset`(YAML)을 직접 텍스트 편집할 때는 Unity가 자동으로 즉시 인식하지
    않을 수 있다는 점 확인.
- **완료** : 여론조작 세부 카테고리별 가격 배율 적용(2026-08-08). 여론조작 15개 스킬이 `SkillID.cs` 주석
  ("SNS 조작 / 인플루언서 활용 / 언론")과 enum 선언 순서(5개씩 3그룹) 기준 세부 카테고리로 나뉘어 있는데,
  세 그룹 사이에 가격 배율 차등이 없던 걸 SNS조작을 1배 기준으로 인플루언서 활용 2배, 언론 3배로 조정.
  - SNS조작(변경 없음) : SNS선동 22,500 / 댓글부대운영 37,500 / 커뮤니티알바 45,000 / 밈생성 15,000 /
    실시간여론관리 67,500
  - 인플루언서 활용(×2) : 인플루언서계약 60,000→120,000 / 유명인홍보 112,500→225,000 / 방송출연
    52,500→105,000 / 인터뷰진행 30,000→60,000 / 후기마케팅 37,500→75,000
  - 언론(×3) : 파트너십발표 75,000→225,000 / 기사배포 37,500→112,500 / 보도자료배포 22,500→67,500 /
    수상경력홍보 30,000→90,000 / 언론플레이 60,000→180,000
  - 그룹 매핑은 세부 서브카테고리 필드가 따로 없어(`SkillSO.category`는 코인설계/시장조작/여론조작/방어
    4대 카테고리만 구분) `SkillID.cs` enum 선언 순서와 스킬명 의미로 판단했다 — 순서상 정확히 5개씩
    끊겨 코멘트의 세 항목과 맞아떨어짐을 확인. `costMultiplier`(1.4, 바로 위 항목에서 조정)는 그대로 두고
    `baseCost`만 조정했다. Unity MCP Play 모드로 15개 전부 `GetCurrentCost` 실제 값을 조회해 의도한
    배율대로 반영됐는지 확인했다.
- **완료** : 사운드 4종 추가(2026-08-08). 사용자가 확정한 목록(파일은 전부 기존 `Assets/Audio/`에 있던 것)을
  화면/액션에 연결했다.
  1. 엔딩씬 대사 타이핑음(`typing.mp3`) — `TypewriterText.cs`(범용 타이핑 이펙트, 유일한 사용처는
     `StoryDialogueController`→엑시트/영웅 엔딩 스토리 대사)에 `typeSfx` 필드를 추가해 글자 하나 출력할
     때마다(공백 제외) `AudioManager.PlaySFX(typeSfx)`를 재생하도록 했다.
  2. 이벤트 알림 뜰 때(`event_alarm_v2.mp3`) — `EventNotificationUI.cs`에 이미 있던 `closeSfx`(닫을 때 소리)와
     동일한 패턴으로 `openSfx` 필드를 추가해 `HandleEventTriggered`(알림 패널이 뜨는 시점)에서 재생한다.
  3. 상장폐지(거지 엔딩) 브금(`failure_sound.wav`) — `EndingSceneUI.cs`의 `delistingBgmClip` 필드(코드
     변경 없이 원래 준비돼 있던 필드)가 기존에 `police-siren.wav`(체포 엔딩과 동일)를 가리키고 있던 걸
     발견해 `failure_sound.wav`로 교체(인스펙터 참조만 변경, 체포 엔딩 쪽 `arrestBgmClip`은 그대로 둠).
  4. 엑시트 엔딩 브금(`true_ending_tension1.mp3`) — `ExitEndingSceneUI.cs`는 원래 영웅/엑시트 엔딩이
     `bgmClip` 하나를 공유하는 구조였는데, "엑시트 엔딩에서만" 재생하려면 구분이 필요해 `EndingSceneUI`와
     동일한 패턴으로 `heroBgmClip`/`exitBgmClip`으로 나눴다(`EndingHandoff.Ending`으로 분기). `exitBgmClip`에만
     연결하고 `heroBgmClip`은 비워둬 기존 "클립 없으면 무음" 관례를 따른다.
  - Unity MCP Play 모드로 4개 씬(`SampleScene`/`EndingScene`/`ExitEndingScene`) 전부 콘솔 에러 없이 로드되는
    것과, `SampleScene`에서 `EventHub.RaiseEventTriggered`를 직접 호출해 `openSfx` 재생 경로가 예외 없이
    도는 것까지 확인했다. 실제 사운드 청취 확인은 에디터 스피커로 사용자가 직접 진행.
- **완료** : 의심도(Doubt) 자동 상승 가속화(2026-08-08). 직전 밸런스 패치(스킬/로비 가격 인상 등) 이후에도
  "의심도 조절이 여전히 쉽다"는 재피드백을 받아, 2년(730턴)째 이후 매 턴 고정 +0.1이던 증가량을 경과 연차에
  비례해 선형으로 커지도록 바꿨다(지수 증가 아님 — "가속도" 자체는 연차당 일정) —
  `MarketManager.ApplyDoubtAutoRise()`에 `DoubtAutoRiseAccelPerYear`(0.05) 상수를 추가해
  `increment = 0.1 + 0.05 × yearsElapsed`(`yearsElapsed = (turnCount-730)/365`)로 계산한다. 2~3년차
  0.10~0.15/턴 → 4~5년차 0.20~0.25/턴 → 6~7년차 0.30~0.35/턴처럼 후반으로 갈수록 관리가 빡빡해진다.
  2년째 진입 시 1회성 +4는 그대로 유지. 슬로프 값(0.05)은 사용자가 즉석에서 확정. Unity MCP Play 모드
  `execute_code`로 `ApplyDoubtAutoRise()`를 turnCount 730/731/1095/1096/1460/2555에서 직접 호출해 공식대로
  4 → 0.100137 → 0.15 → 0.150137 → 0.2 → 0.35가 나오는 것을 확인했다.
- **완료** : 재생/정지 버튼 스위치형 통합(2026-08-08). Notion "2차 피드백 정리" A그룹 항목. 기존
  `TimeUI.cs`가 `pauseButton`/`playButton` 두 개의 별도 버튼(정지 아이콘/재생 아이콘 고정 표시)을 쓰던
  구조를 하나의 토글 버튼(`playPauseButton`)으로 합쳤다 — 재생 중이면 정지 아이콘(누르면 정지),
  정지 중이면 재생 아이콘(누르면 1배속 재생)을 보여주는 미디어 플레이어 관례를 따랐다. `Update()`에서
  기존 `pauseHighlight` 폴링과 동일한 방식으로 `TimeManager.IsPaused`를 매 프레임 확인해 아이콘
  스프라이트(`pauseIconSprite`/`playIconSprite`)를 교체한다. 씬 작업은 `PauseBtn`/`PlayBtn` 두 오브젝트 중
  `PlayBtn`을 삭제하고 `PauseBtn`을 `PlayPauseBtn`으로 이름을 바꿔 재사용했다(`PauseHighlight`가 원래
  `PauseBtn` 자리(-133,-16)에 맞춰져 있어서 그대로 재사용하면 좌표 계산이 필요 없었음). 아이콘은 이미
  씬에 연결돼 있던 `Assets/Sprites/UI요소_정지.png`/`UI요소_재생.png`를 그대로 재사용(신규 에셋 없음).
  Unity MCP Play 모드로 `onClick.Invoke()`를 두 번 연달아 호출해 정지→재생 전체 사이클을 검증했다 —
  단, 이 세션 환경은 Editor 창이 OS 포커스를 안정적으로 못 받아 `Update()`가 실시간으로 안 도는 문제가
  있어서(`Issue_EventNotification.md`에 기록된 것과 동일한 환경 한계), 리플렉션으로 `Update()`를 직접
  호출해 상태 전이(아이콘 교체/`pauseHighlight`/`TimeManager.IsPaused`/`Time.timeScale`)가 정확한 것까지
  확인했다.
  - **후속 수정 2건(같은 세션, 실사용 피드백)** : ① 정지/배속 버튼 사이 간격이 너무 멀다는 피드백 —
    병합 직후엔 `PlayPauseBtn`이 옛 `PauseBtn` 자리(-133,-16)에 남아있어 `SpeedBtn`(52,-16)과 125px
    떨어져 있었다(원래 인접 버튼 간 간격은 32~33px). `PlayPauseBtn`/`PauseHighlight`를 옛 `PlayBtn`
    자리(-41,-16, `SpeedBtn`과의 간격이 원래도 33px로 가장 가까웠던 자리)로 옮겨서 간격을 원래 수준으로
    좁혔다. ② 8배속 상태에서 정지 후 재생하면 무조건 1배속으로 리셋되던 버그 — `TimeUI`가 재생 버튼
    클릭 시 `currentSpeedIndex = 0`으로 직접 초기화하던 게 원인. `TimeManager`에 이미 있던
    `TogglePause()`(정지 중이면 `currentTimeScale`을 그대로 복원, 아니면 정지)로 교체해 `TimeUI`가 배속
    상태를 직접 관리하지 않게 했다 — 정지해도 `currentTimeScale` 필드는 그대로 남아있어서 재생 시 정지
    전 배속으로 정확히 복귀한다. Unity MCP Play 모드에서 배속을 8x까지 올린 뒤 정지→재생을 반복해
    `Time.timeScale`이 8로 유지되는 것을 확인했다.
- **완료** : 2차 피드백 UI/디테일 4건(2026-08-08). Notion "2차 피드백 정리" A그룹 나머지. 각 항목 세부사항은
  구현 전 사용자에게 문구/색상/위치를 컨펌받고 진행했다.
  1. **의심도 max 경고** : `TradeCalculator.MaxTradeAmountByDoubt`/`MaxSupplyAmountByDoubt`가 이미 Doubt
     99 근처에서 거래/발행 가능 수량을 0으로 깎고 있어서 새 로직은 추가하지 않았다. `TradeModalUI`/
     `CoinControlModalUI`에 `doubtWarningText` 필드를 추가해 모달을 열 때 상시 노출 안내 문구("의심도가
     MAX(99)에 가까워지면 매수/매도가 불가능해집니다"/"...발행이 불가능해집니다")를 표시한다.
  2. **매수/매도/발행 수치 직접 입력** : `TradeModalUI.tradeAmountText`(읽기전용 TMP)를
     `tradeAmountInput`(`TMP_InputField`, `ContentType.IntegerNumber`)로 교체했고, `CoinControlModalUI.
     amountText`도 동일하게 `amountInput`으로 교체했다(원문은 "매수 매도"만 명시했지만 사용자가 발행
     모달도 같이 적용하기로 결정). `onEndEdit`에서 입력값을 `Clamp(0, GetMaxTradeAmount()/
     GetMaxAdjustAmount())`로 정리해 기존 슬라이더/버튼과 `SetTextWithoutNotify`로 양방향 동기화한다.
     씬의 두 TMP 텍스트 오브젝트는 InputField로 교체가 필요해 Inspector 재연결이 필요하다.
  3. **개요 스탯 배경색** : `EventOverviewUI.cs`의 `supportText`/`growthText`/`doubtText` 값에
     `<mark=#33333366>` 태그(단일 반투명 회색, 기존 `<color=#FF0900>`와 중첩)를 추가했다. Job+Skill
     보너스 텍스트(`supportBonusText` 등)는 사용자 결정으로 배경색 미적용.
  4. **평단가 + 총자산 + 수익률%** : `PlayerManager`에 `averageBuyPrice` 필드를 추가해 매수 시 가중평균
     (`(oldAvg×oldCoins + price×amount) / (oldCoins+amount)`)으로 갱신하고, 매도는 값을 유지하다가
     보유량이 0이 되면 리셋한다(`ResetState()`도 리셋). `PriceChartGrid`(좌측 격자+라벨) 패턴을 그대로
     따라 `PriceChartAvgPriceLine.cs`(신규, 우측 참조선+라벨, 흰색/회색 계열)를 만들어 `PriceChartUI`에
     격자→캔들→이동평균선→평단가선→툴팁 순서로 연결했다(보유 코인 0이면 자동 숨김). 총자산(현금+보유
     코인×현재가)/수익률%((현재가-평단가)/평단가×100)는 사용자가 헤더 근처를 선택해 `CoinPriceHeaderUI`에
     `totalAssetText`/`returnRateText`로 추가했다(수익률 부호에 따라 기존 캔들 상승/하락색과 동일 팔레트로
     색상 전환).
  Unity MCP로 스크립트 컴파일까지 확인(콘솔 에러 0건). 씬 쪽 InputField 오브젝트 교체/신규 TMP 필드
  연결(각 모달 `doubtWarningText`/`tradeAmountInput`/`amountInput`, `CoinPriceHeaderUI`의
  `totalAssetText`)은 사용자가 직접 Figma 목업대로 배치·연결하는 몫으로 남겨뒀다.
  (Notion "2차 피드백 정리" A그룹 5건 중 재생/정지 버튼 통합은 이미 위 항목으로 완료됨 — 나머지 5건 중
  4건이 이번 항목, 엑시트 5억 목표 이유 설명은 튜토리얼/오프닝에서 다루기로 해 범위 제외.)
  - **후속(같은 날, 실사용 피드백 2건)** : ① 평단가 참조선/좌측 격자 라벨이 회색이라 잘 안 보인다는
    피드백으로 `PriceChartUI.avgPriceLineColor`/`gridPriceLabelColor`를 시안색(`(0, 0.9, 0.9)`)으로
    변경. ② "수익률을 어디서 확인하냐"는 질문에 `returnRateText`가 아직 씬에 안 만들어져 안 보이는
    상태였음을 확인 후, 사용자 요청으로 이번엔 직접 Unity MCP로 `CoinPriceHeader`(SampleScene) 밑에
    `ReturnRateText`(TMP, PriceText와 동일 폰트/스타일, 헤더 우측 빈 공간 anchoredPosition (520,0)에
    배치)를 만들어 `CoinPriceHeaderUI.returnRateText`에 연결하고 씬을 저장했다 — Play 모드 스크린샷으로
    코인 가격 옆에 겹침 없이 표시되는 것까지 확인. `totalAssetText`는 이번 요청 범위 밖이라 미배치.
    처음 보유한 무료 코인의 평단가 0원 반영 여부도 재확인했는데, `averageBuyPrice`가 필드 기본값/
    `ResetState()` 양쪽에서 이미 0f로 시작하고 `JobManager.SelectJob`은 `currentCoins`만 덮어쓰고
    `averageBuyPrice`는 건드리지 않아서, 첫 매수 시 가중평균 공식이 자동으로 무료 보유분을 0원 원가로
    희석시킨다 — 별도 수정 불필요했음(코드 변경 없음, 확인만).
- **완료** : "의심도 하락"(DoubtDecline) 재구매 시 중복 적용되도록 수정(2026-08-08). 여론조작 스킬 3종
  (`실시간여론관리`/`수상경력홍보`/`후기마케팅`, 전부 `isReusable: 1`이라 최대 10회 재구매 가능)이 총량을
  200턴에 걸쳐 0.5%씩 나눠 깎는 기능인데, `PlayerStat.DoubtDeclines`가 `Dictionary<SkillID,
  DoubtDeclineBuff>`라 같은 스킬을 재구매해도 기존 진행 중인 항목을 새 값으로 덮어써서 사실상 타이머만
  리셋되고 하락량은 안 쌓이던 문제를 사용자가 지적해 고쳤다. `List<DoubtDeclineBuff>`로 바꿔
  `BuffCalculator.StartDoubtDecline`이 매 구매마다 새 항목을 추가하고, `TickDoubtDeclines`도 리스트를
  순회하며 각 항목을 독립적으로 카운트다운하도록 수정 — 같은 스킬을 여러 번 사면 하락이 중복(합산)
  적용된다. `DoubtDeclines`를 참조하는 곳이 `BuffCalculator`/`PlayerStat` 외에는 없어(grep으로 확인)
  다른 코드 영향 없음. Unity MCP 컴파일 확인(콘솔 에러 0건). 상세 동작/호출 스택은
  `Issue_DoubtDecline.md` 갱신.
- **완료** : 스킬 아이콘 테두리 색 안내 문구 추가(2026-08-08). `SkillPanelUI.RefreshSelectionHighlight`가
  이미 아이콘 테두리를 `profile.isReusable`에 따라 빨강(`EventEffectFormatter.NegativeColor`, 1회성)/
  초록(`PositiveColor`, 재사용형)으로 칠하고 있는데, 그 의미를 설명하는 범례가 없다는 지적으로 추가했다.
  코드가 아니라 씬 오브젝트만 있으면 되는 정적 텍스트라 Unity MCP `execute_code`로 직접
  `SkillPanel/SkillLegendText`(TMP, `<color=#FFBAB1>■</color> 1회성   <color=#B1FFB1>■</color>
  재사용(최대 10회)`, PriceText와 동일 폰트)를 만들어 배치했다. 위치는 실측 좌표 기반 — 탭 4개 줄
  (`Tab4`/`CloseBtn`, y 425~505)과 아이콘 그리드 박스(`SkillPanelBox`, 우측 끝 x=383.1, 위쪽 끝 y=401.5)
  사이의 빈 틈(세로 ~24~29px)에 우측 정렬로 끼워 넣어, 탭이나 아이콘과 안 겹치게 했다(`RectTransform.
  GetWorldCorners()`를 패널 로컬 좌표로 변환해 각 요소의 실제 경계를 먼저 측정한 뒤 배치 — 앵커 기준이
  서로 달라서(Box는 `(0,0.5)`, Tab/CloseBtn은 `(0.5,0.5)`) `anchoredPosition`만으로는 겹침 여부를 알 수
  없었음). Play 모드에서 `SkillPanelUI.Open()`을 직접 호출해 스크린샷으로 위치/가독성 확인 후 씬 저장.
  **후속(같은 날)** : 탭과 박스 사이 틈에 걸쳐있어 흰 배경 밖(핑크 탭 경계)으로 살짝 삐져나와 보인다는
  피드백으로, `SkillLegendText.anchoredPosition`을 `(383, 424)` → `(375, 393)`로 내려 박스 흰 배경
  안쪽(위/오른쪽 가장자리에서 각 8px 여백)으로 옮겼다. 첫 아이콘 행(`Icon6` 기준 yMax=349)이나 그룹
  라벨("공급 구조", x가 좌측이라 x:63~383 범위와 안 겹침)과도 안 겹치는 것까지 좌표로 확인.
- **완료** : `CoinPriceHeader.ReturnRateText` 위치 버그 수정(2026-08-08). 수익률 텍스트가 코인 가격과 너무
  멀어 보인다는 피드백으로 좌표를 재보니, 애초에 이 오브젝트를 처음 만들 때 `localScale`이 `(1,1,1)`이
  아니라 `(1.59,1.59,1.59)`(부모 `Main_Canvas`의 0.63배 스케일을 상쇄해 `lossyScale`이 1.0이 되는 값)로
  잘못 들어가 있던 게 원인이었다 — 형제 오브젝트(`PriceText`/`CoinIcon`)는 전부 `localScale (1,1,1)`,
  `lossyScale ≈0.63`인데 이것만 달라서, 같은 `anchoredPosition` 수치라도 실제 렌더링 크기/위치가 1.59배
  멀리 벌어져 보였다. `localScale`을 `(1,1,1)`로 맞추고, `anchoredPosition`도 `PriceText` 박스 오른쪽
  끝(로컬 x=484)에서 12px만 띄운 `(496, 0)`으로 다시 계산해 붙였다(`sizeDelta`도 320→220으로 축소).
  Unity MCP `execute_code`로 직접 수정 후 Play 모드 스크린샷으로 확인, 씬 저장.
- **완료** : 러쉬막기(매수 폭주 방지, 2026-08-08). Notion "2차 피드백 정리" B그룹, `Next_Tesk.md` 18번 항목.
  1) **매수 1회 상한(발행량 기준)** — `TradeCalculator.MaxTradeAmountBySupply(currentSupply, currentCoins)`
     신규(`Max(0, Supply - currentCoins)`, "이미 보유한 만큼 빼고 시장에 남은 유통량까지만 매수 가능"),
     `TradeModalUI.GetMaxTradeAmount()`의 Long 분기에서 기존 잔고 기준 상한에 `Min`으로 적용(Doubt 캡은
     기존처럼 마지막). 처음 계획한 "고정 10,000개 + 발행량 기준 중 더 작은 쪽"은, `MarketManager.
     InitialSupply=2000`이라 발행량 기준이 게임 시작부터 항상 더 타이트해서 고정 상한이 죽은 코드가
     된다는 걸 확인해 뺐다. Short(매도)는 손 안 댐.
  2) **지지도 → 발행량 증가속도 로그 연동** — `TradeCalculator.GrowSupply`가 고정값
     `SupplyGrowthPerTurn=50` 대신, Support(-100~100)를 `Clamp01((Support+100)/200)`로 정규화한 뒤
     `SupplyGrowthBase(10) + SupplyGrowthLogCoefficient(100) × ln(1+normalizedSupport)`로 계산하도록
     바꿨다 — Support가 낮을수록 10/턴(하한), 0이면 약 50/턴(기존 고정값과 비슷하게 맞춤), 100이면 약
     79/턴까지 완만하게 늘어난다(로그라 무한정 커지지 않음). 두 상수 모두 밸런스용 임시값.
  3) "상폐 상한선 늘리기 5"는 사용자 확인 결과 **폐지(진행 안 함)** — Next_Tesk.md에서도 제외.
  - 검증 : Unity MCP `execute_code`로 `TradeCalculator.MaxTradeAmountBySupply`/`GrowSupply`를 직접 호출해
    수치 확인(Support=-100/0/50/100 → 10/50.5/66.0/79.3, `MaxTradeAmountBySupply(2000,3000)=0` 등 클램프
    포함), Play 모드에서 `PlayerManager.currentMoney`를 100만으로 올려 잔고 상한(100,000)보다 발행량
    상한(2,000)이 실제로 더 작게 적용되는 것까지 확인. 컴파일 에러 없음. 공식은 `Game_Formula.md` 2장
    (Supply)/3장(Long) 갱신.
- **완료** : 의심도(Doubt) 3단계 메인 BGM 전환(2026-08-09). `Next_Tesk.md` 18번 항목. 신규
  `SuspicionBgmController.cs`(`Assets/Scripts/manager/utils/`)가 `StatGaugeUI`와 동일한 패턴으로
  `EventHub.OnMarketUpdated` 구독 + `OnEnable`에서 `MarketManager.Instance.CurrentStat` 초기값 pull.
  Doubt 55/60 경계(상수로 분리)로 티어(Low/Mid/High)를 계산해 55 미만은 `Chiptuna_Sandwich.wav`, 55~60은
  `StopBGM()`(무음), 60 이상은 `leberch-suspense-511168.mp3`를 재생하되, 티어가 바뀔 때만
  `PlayBGM`/`StopBGM`을 호출하도록 `currentTier` 캐싱(매턴 이벤트마다 부르면 크로스페이드가 겹치는 문제
  방지). `GameStarter.Start()`가 씬 시작 시 `happy-tropical.wav`를 직접 재생하던 코드는 제거했다 — 그대로
  뒀으면 새 컨트롤러의 초기값 pull과 겹쳐 시작하자마자 크로스페이드가 두 번 도는 문제가 있었음(이제 새
  컨트롤러의 초기값 pull이 첫 BGM 재생을 담당). Unity Editor MCP로 `SampleScene`의 `/GameStarter`
  오브젝트에 컴포넌트를 붙이고 클립 2개를 연결, 씬 저장까지 완료. (`Issue_SuspicionBgm.md` 참고)
  - 검증 : 컴파일 에러/경고 없음 확인. **Play 모드로 실제 턴을 진행시켜 55/60 경계 전환의 체감(타이밍/볼륨)은
    아직 확인 안 함** — 사용자가 직접 플레이하며 들어봐야 함.
- **완료** : 의심도 55~100 구간 중간 효과음(2026-08-09). `Next_Tesk.md` 19번 항목. `SuspicionBgmController`와
  별개로 신규 `DoubtMidSfxController.cs`(`Assets/Scripts/manager/utils/`)를 추가했다 — Doubt가 55 이상인
  동안 3~6개월(턴, `TurnsPerMonth=365/12` 기준 91~183턴 랜덤) 간격마다 `Assets/Audio/DouptSFX/`의
  `clockdown`/`end-clocksound`/`heartsound` 중 하나를 랜덤으로 골라 앞 3초만 재생한다.
  `AudioManager.PlaySFX()`는 공용 `sfxSource`에 `PlayOneShot`으로 얹는 방식이라 특정 클립만 3초 뒤 끊을 수
  없어서, 이 컨트롤러가 전용 `AudioSource`(Awake에서 `GetComponent`/없으면 `AddComponent`)를 직접
  `Play()`→3초 뒤 코루틴에서 `Stop()`한다. 볼륨은 `AudioManager.Instance.sfxVolume`을 참고한다. 간격
  카운트는 `EventHub.OnDayChanged`(턴)로, Doubt 값은 `SuspicionBgmController`와 동일하게
  `EventHub.OnMarketUpdated`(+`OnEnable`에서 `MarketManager.Instance.CurrentStat` 초기값 pull) 구독으로
  캐싱한다 — Doubt가 55 미만인 턴은 카운트를 멈추되 리셋하지는 않는다(미정이던 항목, 가장 무난한 쪽으로
  결정). `OnDisable`에서 구독 해제 + 진행 중 코루틴 정리. Unity Editor MCP로 `SampleScene`의
  `/GameStarter` 오브젝트(`SuspicionBgmController`와 동일 위치)에 컴포넌트를 붙이고 클립 3개를 연결, 씬
  저장까지 완료.
  - 검증 : 컴파일 에러 없음 확인. **Play 모드로 실제 55 이상 구간을 오래 유지해 재생/3초 컷/랜덤 선택
    체감은 아직 확인 안 함** — 사용자가 직접 플레이하며 들어봐야 함. 사운드 선택 방식(현재: 매번 순수
    랜덤, 직전과 같은 클립이 연속될 수 있음)도 피드백 받으면 조정 필요.
- **완료** : 스킬 구매 가능 여부에 따른 UI 상태 표시(`Next_Tesk.md` 22번). 신규
  `Assets/Scripts/UI/SkillModal/SkillButtonState.cs`(전용 스크립트, 요청대로 상태 계산만 분리) —
  `Evaluate(id)`가 `Used`(1회성 구매 완료)/`MaxedOut`(재사용형 최대 구매 횟수 도달)/`Affordable`(보유 금액
  >= 현재 비용)을 판정해 `Locked`/`Purchasable` 파생값을 반환한다. `SkillManager.IsMaxedOut(id)`을 새로
  추가했고(기존 `MaxPurchaseCount` 10회 도달 시 UI 표시가 전혀 없던 부수 버그를 같은 김에 수정),
  `SkillPanelUI.RefreshDetail()`(구매 버튼)/`RefreshSelectionHighlight()`(그리드 아이콘)가 이 결과로
  회색+비활성 처리한다. 보유 금액 변경 감지용 이벤트나 폴링은 추가하지 않았다 — 스킬 패널이 열려있는 동안은
  `ModalPause`로 거래가 막혀 있어 돈이 바뀌는 유일한 경로가 스킬 구매뿐이고, 구매는 이미
  `EventHub.OnSkillPurchased → RefreshDetail()`로 전체 재계산을 트리거하기 때문에 별도 배선 없이 충분함을
  확인했다.
  - **후속 요청(같은 세션)** : "의심도에 걸려서 구매 못하는 스킬"도 잔액 부족과 동일하게 처리해달라는 요청 —
    확인해보니 기존 코드엔 그런 조건이 아예 없어서(그런 게 있는 줄 알았던 오해) 사용자에게 정확한 조건을
    물어봤고, "이번 구매로 의심도가 100(체포 엔딩 문턱, `EndingCalculator`)을 넘게 되면 막기"로 확정했다.
    `SkillButtonState.State`에 `DoubtSafe` 필드를 추가해 `Purchasable = !Locked && Affordable && DoubtSafe`로
    묶었다 — `PredictDoubtDelta()`가 `StatCalculator.ApplySkillUse`와 동일한 대상(즉시 반영되는
    `DoubtIncrease`/`DoubtDecrease`만, 200턴에 걸쳐 서서히 깎이는 `DoubtDecline`은 제외)을 합산해
    `MarketManager.Instance.CurrentStat.Doubt + delta`가 100 미만인지로 판정한다. 잔액 부족과 완전히 같은
    시각 처리(회색+비활성)를 공유하고 별도 상태 문구는 추가하지 않았다(요청 그대로 "잔액 부족과 똑같이").
  - 검증 : Unity Editor MCP로 컴파일 에러 없음 확인. **Play 모드로 잔액 부족/최대 구매 도달/의심도 초과 시
    실제 회색 전환 체감은 아직 확인 안 함** — 사용자가 직접 플레이하며 확인 필요.
- **완료** : 대사/스토리 데이터 SO 리팩토링(2026-08-11, `Next_Tesk.md` 22번). 21번(오프닝 대화 UI) 착수
  전에 먼저 한 구조 정리 — 대사 데이터가 씬 컴포넌트 인스턴스나 코드에 하드코딩돼 재사용 불가능했던 문제.
  - `JobSO`(`Assets/Scripts/SOs/JobSo.cs`)에 `List<StoryBeat> openingBeats` 필드 추가 — 새 SO 타입을
    만드는 대신, 이미 직업 선택이 `JobSO` 인스턴스 하나를 고르는 구조라 오프닝 데이터도 "선택된 직업의
    프로필 데이터"로 자연스럽게 편입시켰다(`SkillSO`/`EventSO`와 동일한 "직업별 프로필에 여러 필드" 관례).
    21번에서 `StoryDialogueController`가 `JobManager.CurrentJob.openingBeats`를 읽도록 붙이면 됨(아직
    내용은 비어있음 — 6직업×6컷 콘텐츠는 21번에서 목업 받은 뒤 채움).
  - 신규 `StoryBeatSetSO`(`Assets/Scripts/SOs/StoryBeatSetSO.cs`) : `List<StoryBeat> beats`만 갖는
    재사용 가능한 에셋 타입. `StoryDialogueController.beats`(씬에 직접 박혀있던 `List<StoryBeat>`)를
    `StoryBeatSetSO storySet` 참조로 교체해 엑시트/영웅 엔딩 대사가 씬 파일이 아니라 에셋으로 관리되게
    했다. `EndingSceneUI`의 체포/거지 엔딩 문구(`Start()`에 하드코딩된 두 문자열)도 `arrestStory`/
    `delistingStory`(`StoryBeatSetSO`) 필드로 교체하고 `beats[0].line`만 사용한다 — 배경 스프라이트는
    기존 `arrestBgSprite`/`delistingBgSprite` 필드가 이미 데이터 기반이라 그대로 두고 손대지 않았다
    (이 두 에셋은 `background` 필드를 안 씀).
  - `Assets/Profile/엔딩_스토리/`에 에셋 3개를 만들어 기존 씬 데이터를 그대로 이관했다 — `체포.asset`/
    `거지.asset`(각 1비트, 기존 하드코딩 문자열 그대로), `엑시트_영웅.asset`(기존 `StoryDialogueController.
    beats` 2개, 배경 스프라이트 GUID까지 동일하게 포팅). Unity Editor MCP로 씬(`ExitEndingScene.unity`/
    `EndingScene.unity`)의 컴포넌트 필드를 새 에셋으로 재연결하고 저장까지 완료 — 콘텐츠 변화 없이
    순수 구조 이관이라 게임 동작은 이전과 동일하다. 작업 도중 사용자가 스크립트 폴더를
    `Assets/Scripts/UI/EndingScene/` → `StoryScene/`으로 직접 리네임함(Editor 네이티브 리네임이라 `.meta`
    GUID 보존, 참조 안 깨짐). 상세 작동 방식/호출 스택은 `Issue_StoryBeatSO.md` 참고.
- **완료** : 튜토리얼 UI(2026-08-11, `Next_Tesk.md` 15번 — 17번 "엑시트 5억 목표 이유 설명"도 여기서 같이 처리,
  21번 오프닝 대화 UI와는 별개). Notion "튜토리얼UI"/"2차 피드백 정리" 문서와 사용자가 준 실제 인게임
  스크린샷 목업 3장을 기준으로 진행했다.
  - **방향 확정** : 노출 시점은 직업 선택 후 메인 게임(`SampleScene`) 진입 직후, 게임 플레이 시작 전(자동
    표시, 스킵 없음 — 끝까지 클릭해서 넘겨야 종료). UI 형태는 전용 씬이 아니라 `Main_Canvas` 하위 오버레이
    패널(기존 `TradeModalUI`/`CoinControlModalUI`와 동일한 모달 계열). 17번(목표금액 설명)은 별도 문구 없이
    "개요" 말풍선이 가리키는 기존 `EventOverviewUI`(우측 목표금액/현재금액/남은금액 표시)로 커버됨을
    확인해 추가 작업 불필요. 매수/매도 버튼·발행량·스탯 슬라이더 설명은 사용자가 "따로 만들 것"이라고
    확정해 이번 범위에서 제외.
  - 신규 `Assets/Scripts/UI/Tutorial/TutorialUI.cs` : 사용자가 준 초안(딤 패널+말풍선 배열, 클릭 시
    `OnClickNext()`로 순차 노출, 마지막 말풍선 이후 종료) 구조는 그대로 두고, pause/resume만 `TimeManager.
    TogglePause()` 직접 호출 대신 기존 6개 모달과 동일한 `ModalPause.Open()/Close()`(→ `EventHub.
    RaiseGamePaused/RaiseGameResumed`)로 교체했다 — 초안이 `TimeManager.isManuallyPaused`(스페이스바/P
    수동정지 전용 플래그, 모달 정지와 명시적으로 분리되어 있음, `TimeManager.cs` 주석 참고)를 건드리는
    구조라 "UI는 EventHub.Raise*()만 호출" 경계와 충돌해 사용자에게 확인 후 EventHub 경로로 교체하기로
    함. 클릭 연결도 `EventLogButton.cs`와 동일하게 `Start()`에서 `GetComponent<Button>().onClick.
    AddListener(OnClickNext)`로 코드 배선해 Inspector 수동 연결이 필요 없게 했다.
  - 씬 오브젝트(`Main_Canvas/TutorialPanel` — `DimBackground`(검정 alpha 165) + `SpeechBubble1/2/3`)를
    Unity Editor MCP로 생성했다. 말풍선 3개는 목업 스크린샷의 실제 문구/강조색을 그대로 반영했고, 위치는
    각 대상 UI(`EventLogBtn`/`DoubtScorePanel`/`SkillBtn`)의 실제 RectTransform 좌표를 읽어 그 옆에
    배치했다 — "개요, 이벤트 로그, EXIT 버튼(노랑 강조)를 볼 수 있음"(`EventLogBtn` 옆), "의심도가
    100(빨강 강조)이 되면 게임오버"(`DoubtScorePanel` 위), "스킬(빨강 강조)을 구매하러 가는 버튼"(`SkillBtn`
    옆). 말풍선 배경은 사용자 확인 후 꼬리 없는 각진 회색 박스 플레이스홀더로 유지하기로 함(둥근
    테두리+꼬리 스프라이트 에셋이 프로젝트에 아직 없음).
  - **MCP 작업 중 발견한 이슈** : `create_gameobjects`로 `Main_Canvas`(스케일 0.634) 하위에 새
    RectTransform을 `add_component`로 추가할 때마다 로컬 스케일이 1.577로 자동 보정되는데, 기존 형제
    오브젝트(`EventLogBtn` 등)는 전부 스케일 [1,1,1]이라 매번 `set_transform`으로 다시 [1,1,1]로
    맞춰줘야 했다.
  - 검증 : 컴파일 에러 없음, `TutorialUI`/`ModalPause`/`EventHub` 관련 새 콘솔 에러 없음 확인. **`SampleScene`을
    단독으로 Play 모드 진입시키면(Title→CharacterSelect 정상 플로우를 안 거쳐서) HUD 자체가 아무것도 안
    그려져 튜토리얼 패널을 껐다 켜도 스크린샷상 차이가 없었다(기존부터 그런 상태, 이번 작업과 무관 —
    TutorialPanel 비활성 상태에서도 동일하게 빈 화면 재현해 확인함). 정상 플로우(Title부터 시작)로 실제
    화면 배치/텍스트 가독성 확인은 사용자가 직접 해야 함.**
- **완료** : 2차 피드백(빨간 글씨 항목) ①③ (2026-08-12, 옛 `Next_Tesk.md` 22번). Notion ["2차 피드백
  정리"](https://app.notion.com/p/3b694f5130278048a560d0b4232e7bfe) 기준, ②(엑시트 당위성/빚 스토리)는
  20/21번과 겹쳐 3차 피드백(23번)으로 이관 — 이 항목은 완료분 ①③만 기록.
  - ① 시장조작 스킬 9종 전면 개편(`Issue_PriceShockSkills.md` 참고) — 신규 스킬을 만들지 않고 **기존
    9종을 전부 개편**하는 방향으로 작업함(1차 시도였던 신규 스킬 2종은 되돌림). `baseCost` 전체 인상(구
    22,500~90,000 → 신규 50,000~200,000, 여론조작 카테고리보다 비싼 축으로), Doubt 대체로 2배 이상,
    Support/Growth/Volume도 1.3~1.6배로 올려 하이리스크·하이리턴화. `펌핑`(+25%)·`허수매수벽`(+10%)·
    `허수매도벽`(-20%) 3종에 신규 `EffectType.PriceShockPercent`를 얹어 "가격 즉시 변동"을 기존 스킬
    확장으로 구현. 추가 피드백으로 `펌핑`을 `허수매수벽`의 상위호환(Support/Growth/Price 전부 웃돌되
    baseCost 180,000으로 더 비쌈)으로 재조정, `EventEffectFormatter.IsBeneficial`이 SupportIncrease/
    GrowthIncrease도 값 부호로 판정하도록 수정(FOMO유도 -30/-30이 초록으로 잘못 표시되던 버그 수정).
    **스킬 패널 UI 쪽은 손대지 않음** — 기존 9개 아이콘 버튼 재사용, 수치만 바뀌고 화면엔 자동 반영됨.
  - ③ 지지도/상승률/발행량 호버 툴팁 — `SupportPanel`/`IncreaseScorePanel`/`CoinControlPanel/
    TotalSupplyText`(항상 표시되는 발행량 텍스트)에 추가. 신규 `StatTooltipUI`(공용 싱글턴, CanvasGroup.
    alpha로 show/hide)+`HoverTooltipTrigger`(`IPointerEnterHandler`/`IPointerExitHandler`, 인스펙터에
    문구만 채우면 재사용 가능) 두 스크립트로 구현, 툴팁 박스는 튜토리얼 말풍선과 동일 스타일(배경
    `0.2,0.2,0.2,0.92`, 흰 텍스트, 같은 폰트). 의심도(DoubtScorePanel) 게이지는 이 시점엔 범위에서
    제외했다가 3차 피드백 세션(2026-08-12)에서 동일 패턴으로 추가 완료. Play 모드에서 스크립트 직접
    호출로 3개 다 정상 동작 확인했으나, `SampleScene`을 바로 Play하면 부트스트랩 싱글턴이 없어 화면이
    안 그려지는 기존 환경 이슈로 스크린샷 육안 확인은 못 함.
- **완료** : 3차 피드백 및 버그수정 완료분 (2026-08-12, 옛 `Next_Tesk.md` 23번). Notion ["3차 피드백 및
  버그수정"](https://app.notion.com/p/3ba94f51302780c49627e562ce883275) 기준, 완료 항목은 페이지에 취소선(~~)
  표시해둠. 미완료 항목(초기 설명/엑시트 당위성, 추가사항, 빌드 연구)은 `Next_Tesk.md` 23번에 계속 남아있음.
  - **튜토리얼 및 정보 전달** — 의심도(`DoubtScorePanel`)에도 `HoverTooltipTrigger` 추가(+Image
    `raycastTarget` on)해 지지도/상승률/발행량과 동일한 4개 스탯 호버 툴팁 완성.
  - **버그사항 3건** — ① `DoubtMidSfxController.StopAfterDuration`이 `WaitForSeconds`(scaled)를 써서 모달
    중(timeScale=0)엔 SFX 전체 재생, 배속 중(timeScale>1)엔 3초보다 일찍 끊기던 문제를
    `WaitForSecondsRealtime`으로 수정. ② `ButtonPressEffect`가 눌림 시 `localScale`을 줄이는 바람에 클릭
    판정용 히트박스도 같이 줄어 가장자리 클릭이 무시되던 문제 — `Graphic.raycastPadding`을 축소 비율만큼
    음수로 넣어 보정, `extraHitboxPadding`(기본 10) 여유분도 추가해 시각 크기보다 히트박스가 항상 더 크게
    잡히게 함(공유 컴포넌트라 다른 버튼들도 같이 개선됨). ③ `CoinPriceHeaderUI` 총자산 계산이 CashBonus%를
    평가손익(profit)분에만 곱해 실제 매도(`PlayerManager.HandleSellCoin`, 코인 평가액 전체에 곱함)
    결과보다 작게 표시되던 문제 — 매도 공식과 동일하게 통일.
  - **밸런스 개선** — `TradeCalculator.Short`에 매도 전용 `SellPenaltyMultiplier`(1.5x)를 추가해 매도 시
    지지도/상승률이 매수보다 더 크게 떨어지게 함(매수 `Long`은 그대로 유지). Support/Growth는
    `ProbabilityCalculator.UpProbability`에 직결되므로 가격 하락 확률도 같이 늘어남 — 별도 가격 로직은
    손대지 않음. 1.5x는 체감 없이 잡은 임시값, `Next_Tesk.md` 7번(밸런스 수치 조정) 재검토 대상에 포함.
  - **UI개선3 3건** — ① `EventEffectFormatter.DescribeEffect`가 `PriceShockPercent` 라벨을
    "코인 가격(즉시 %)"에서 "코인 가격"으로 바꾸고 숫자 뒤에 suffix("%")를 붙여 "코인 가격 +25%" 형태로
    표시. ② 목표금액 달성 표시(`Issue_AssetGoalOverlay.md` 참고) — 사용자가 준 이미지 목업 기준, 엑시트
    조건(`MarketManager.CanExit`, 현금 5억)이 처음 충족되는 순간을 `AssetGoalNotifier`가 감지해
    `EventHub.OnAssetGoalAchieved`를 1회 발행하고, `AssetGoalNotificationUI`가 배너를 띄운다(X로 닫기,
    재표시 안 함). `CoinPriceHeaderUI`의 "내 자산 합계" 텍스트는 달성 후 계속 강조색으로 표시되고,
    `EventLogButton`은 `Outline` 상시 표시 + `UIBurstParticle` 기반 폭죽 연출(`FireworksBurst()`, 버튼
    주변 랜덤 위치에 시차를 두고 4연발)이 달성 순간과 이후 주기적으로 재생된다. 판정 기준은 처음엔
    총자산(현금+코인평가액) 기준으로 만들었다가 사용자 요청으로 기존 `CanExit`(현금만 봄, EXIT 버튼과 동일
    조건) 재사용으로 정정함. 배너/버튼 GameObject 배치와 SerializeField 연결은 목업 기준으로 사용자가 직접
    진행할 예정 — 아직 씬 미배치, Play 모드 검증 전. ③ 처음엔 "PreviewText 왼쪽에 DoubtIncreaseText를
    재배치"로 잘못 이해해 정렬/위치를 바꿨었으나, 사용자 정정으로 되돌리고(`PreviewText` Center 정렬 복구,
    `DoubtIncreaseText` Center 정렬 + x=460 위치 복구) 대신 새 `TradeCoinCountText`(TextMeshProUGUI)를
    `PreviewText` 박스 바로 왼쪽(x=-460, size 340x50, 폰트 20)에 추가 — `TradeModalUI.tradeCoinCountText`
    필드로 연결, `RefreshPreviewAndConfirmState()`에서 "코인 수: N개"로 갱신. Play 모드 밖에서는 Canvas가
    렌더링 안 되는 환경 문제로 스크린샷 확인은 못 했음 — 실제 플레이 확인 필요.
