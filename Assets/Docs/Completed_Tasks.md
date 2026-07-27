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
  - **범위 밖에서 발견한 기존 문제(손대지 않음)** : `추가발행권한` `SkillSO`가 `SkillManager.skillDatabase`
    (Inspector 직렬화 리스트)에 애초에 등록돼 있지 않아, 정상 플레이로는 이 스킬을 영구히 구매할 수 없는
    상태였다. 스킬 UI 연결 자체가 별도 미완료 작업(`Next_Tesk.md` "후보 : UI 연결" 참고)이라 이번 스코프에서는
    수정하지 않았다.
