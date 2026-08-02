# 이슈 : UI 아키텍처 리팩토링 작동 방식 / 호출 스택 정리

작성일 : 2026-08-02

관련 구현 : `Completed_Tasks.md` "UI 스크립트 아키텍처 리팩토링" 항목 (구 `Next_Tesk.md` 10번, 지난 세션
진단 → 이번 세션 구현). ponytail(최소/게으른 해법 강제) 모드로 진행.

## 관련 파일

- [UIFormat.cs](../../Scripts/UI/Utils/UIFormat.cs) — 신규. 통화/퍼센트/부호/날짜 포맷, `EffectType` 증가·감소
  방향 판정을 모은 static 유틸. 네임스페이스 없음(프로젝트 전역 컨벤션).
- [TimeUI.cs](../../Scripts/UI/Utils/TimeUI.cs) — 날짜 텍스트, 일시정지/재생/배속 버튼. `TimeManager`에
  직접 액션을 호출(싱글턴 참조는 유지)하지만 화면 갱신은 `EventHub.OnDayChanged` 구독으로 전환.
- [SettingsUI.cs](../../Scripts/UI/Utils/SettingsUI.cs) — 설정 팝업. `Time.timeScale` 직접 조작 대신
  `EventHub.RaiseGamePaused/RaiseGameResumed` 사용.
- [MintButtonUI.cs](../../Scripts/UI/Utils/MintButtonUI.cs) — "코인 발행" 트리거 버튼. `EventHub.
  OnSkillPurchased` 구독으로 해금 상태 갱신.
- [PlayerUI.cs](../../Scripts/UI/MainModal/PlayerUI.cs) — 현금/코인 텍스트, 매수/매도 버튼.
- [CoinPriceHeaderUI.cs](../../Scripts/UI/MainModal/CoinPriceHeaderUI.cs) — 차트 상단 코인명+현재가.
- [PriceChartUI.cs](../../Scripts/UI/MainModal/PriceChartUI.cs) — 캔들 차트.
- [TradeModalUI.cs](../../Scripts/UI/FeatherModal/TradeModalUI.cs) — 매수/매도 모달.
- [CoinControlModalUI.cs](../../Scripts/UI/FeatherModal/CoinControlModalUI.cs) — 발행량 조작 모달.
- [EventOverviewUI.cs](../../Scripts/UI/EventModal/EventOverviewUI.cs) — 개요 탭(스탯개요+엑시트).
- [EventLogPanelUI.cs](../../Scripts/UI/EventModal/EventLogPanelUI.cs) — 이벤트 로그 패널(커뮤니티 탭).
- [EventHub.cs](../../Scripts/manager/EventHub.cs) — UI→Manager 요청 이벤트 + Manager→UI 결과 이벤트 허브.
- [MarketManager.cs](../../Scripts/manager/MarketManager.cs) / [PlayerManager.cs](../../Scripts/manager/PlayerManager.cs) /
  [TimeManager.cs](../../Scripts/manager/TimeManager.cs) / [SkillManager.cs](../../Scripts/manager/SkillManager.cs) — Manager 측.

## 작동 방식

**경계 원칙** : 기존 프로젝트 관례 그대로 — UI는 `EventHub`만 호출(요청), Manager는 `EventHub`를 구독해서
실제 상태를 바꾸고 결과 이벤트(`OnMarketUpdated`/`OnDayChanged`/`OnSkillPurchased` 등)를 다시 쏜다. 이번
리팩토링은 이 경계 자체를 바꾼 게 아니라, `TimeUI`/`PlayerUI`/`CoinControlModalUI`가 이 경계를 우회해서
`Update()`에서 Manager 싱글턴 프로퍼티를 매 프레임 직접 읽던 것을 전부 이벤트 구독으로 옮긴 것이다.

**핵심 전제 하나가 깨져 있었다** : `PlayerUI`(현금/코인)와 `CoinControlModalUI`(보유 코인수량)를 이벤트
구독으로 바꾸려면 매수/매도/발행량 조작 시점에 `OnMarketUpdated`가 발행돼야 하는데, 실제로는
`MarketManager.HandleBuyCoin/HandleSellCoin/HandleManipulateSupply`가 `RaiseMarketUpdated`를 호출하지
않고 있었다(하루가 지날 때의 `NextTurn()`과 수동 뉴스 이벤트`HandleNewsEvent()`만 호출). 그래서 `TradeModalUI`/
`CoinControlModalUI`의 확정 버튼(`OnConfirmClicked`)이 `RaiseBuyCoin`/`RaiseSellCoin`/`RaiseManipulateSupply`
호출 **직후**(두 이벤트 모두 동기 호출이라 이 시점엔 `PlayerManager`/`MarketManager` 상태가 이미 갱신 완료된
상태) `EventHub.RaiseMarketUpdated(MarketManager.Instance.CurrentStat)`를 한 줄 추가로 쏘는 방식을 택했다.
Manager 코드(`MarketManager`/`PlayerManager`)는 건드리지 않고 UI 쪽에서 "트리거 뒤에 결과 이벤트를 한 번 더
쏜다"는 형태라 관례를 벗어나지 않는다.

**포맷 통합 원칙** : `UIFormat`은 기존 각 파일이 이미 쓰던 정확한 출력 형식(공백 있는 `"₩ "` vs 없는 `"₩"`,
날짜 포맷 3종 등)을 그대로 보존하는 메서드만 제공한다 — 통합 과정에서 시각적으로 달라진 곳은 없다.

## 호출 스택

### 1) 매수/매도 확정 → PlayerUI/CoinControlModalUI/StatGaugeUI 등 갱신

```
[BtnConfirm 클릭] (TradeModalUI.cs)
 └─ OnConfirmClicked()                                       [TradeModalUI.cs:217]
     ├─ EventHub.RaiseBuyCoin(tradeAmount) / RaiseSellCoin(tradeAmount)
     │   ├─ PlayerManager.HandleBuyCoin/HandleSellCoin         [PlayerManager.cs:39] → currentMoney/currentCoins 갱신
     │   └─ MarketManager.HandleBuyCoin/HandleSellCoin         [MarketManager.cs:271] → TradeCalculator.Long/Short
     ├─ EventHub.RaiseMarketUpdated(MarketManager.Instance.CurrentStat)   ← 이번에 추가
     │   ├─ PlayerUI.HandleMarketUpdated → RefreshTexts()       [PlayerUI.cs:33]
     │   ├─ CoinControlModalUI.HandleMarketUpdated              [CoinControlModalUI.cs:64] (totalSupplyText/heldCoinText)
     │   └─ StatGaugeUI×3 / CoinPriceHeaderUI / PriceChartUI (기존 구독자들도 같이 갱신됨)
     └─ Close() → EventHub.RaiseGameResumed()
```

### 2) 발행량 조작 확정 → 동일 패턴 (+ 기존 버그 수정)

```
[BtnConfirm 클릭] (CoinControlModalUI.cs)
 └─ OnConfirmClicked()                                        [CoinControlModalUI.cs:127]
     ├─ EventHub.RaiseManipulateSupply(signedAmount) → MarketManager.HandleManipulateSupply → CurrentStat.Supply 반영
     ├─ EventHub.RaiseMarketUpdated(...)   ← 이번에 추가
     │   └─ totalSupplyText가 "다음 날"까지 안 기다리고 즉시 갱신됨 (수정 전엔 발행량을 조작해도
     │      다음 `NextTurn()`까지 화면 수치가 안 바뀌던 잠재 버그였음 — 이번에 같이 발견/수정)
     └─ Close() → EventHub.RaiseGameResumed()
```

### 3) 날짜 변경 → TimeUI 갱신

```
TimeManager.Update() → CurrentGameDate.Date 바뀜               [TimeManager.cs:55]
 └─ EventHub.RaiseDayChanged()
     └─ TimeUI.RefreshDateText()                                [TimeUI.cs:65] (OnEnable에서 구독, UIFormat.DateDash 사용)
```

### 4) TradeModal 첫 클릭 무반응 버그 (발견 및 수정)

`TradeModalUI.panel` 필드는 스크립트 자신이 붙어있는 GameObject(`TradeModal`)를 가리킨다. 씬에서 이 오브젝트가
비활성 상태로 시작하면(사용자가 UI 편집을 위해 꺼둔 상태 포함) 아래 순서로 문제가 생겼다.

```
[BtnLong/Short 클릭] (PlayerUI.cs) → tradeModal.Open(TradeMode.Long)   [TradeModalUI.cs:68]
 ├─ EventHub.RaiseGamePaused()
 ├─ panel.SetActive(true)   ← 오브젝트가 "처음 활성화"되는 순간이라 Unity가 Start()를 다음 프레임으로 미룸
 └─ (이번 프레임은 여기까지 정상 — 모달이 보임)

[다음 프레임] 미뤄졌던 Start() 실행
 └─ (수정 전) if (panel != null) panel.SetActive(false);   ← 무조건 실행 → 방금 연 모달이 바로 닫힘
```

두 번째 클릭부터는 `Start()`가 이미 끝난 뒤라 이 경합이 없어서 정상 동작했다 — "1클릭 = 시간정지만 되고
모달 안 열림, 2클릭 = 정상 열림" 증상과 정확히 일치.

**수정** : `isOpen` bool 플래그 추가.

```
Open()  → isOpen = true  (panel.SetActive(true)보다 먼저 실행됨, Open()은 Start()보다 항상 먼저 동기 실행됨)
Close() → isOpen = false
Start() → if (!isOpen && panel != null) panel.SetActive(false);   ← Open()이 먼저 실행됐으면 스킵
```

씬에서 `TradeModal`의 초기 활성/비활성 상태와 무관하게 동작 — 사용자가 UI 편집을 위해 오브젝트를 꺼둔 채로
작업해도 더 이상 이 버그가 재현되지 않는다.

### 5) TimeUI 재생 버튼 무반응(배속 상태) 버그 (발견 및 수정)

```
[playButton 클릭] (TimeUI.cs:38)
 ├─ (수정 전) TimeManager.Instance.ResumeGame() → Time.timeScale = currentTimeScale
 │            (이미 2/4/8x로 재생 중이면 currentTimeScale이 그대로라 클릭해도 값이 안 바뀜 → 무반응처럼 보임)
 └─ (수정 후) currentSpeedIndex = 0; ApplySpeed();  → TimeManager.SetTimeScale(1f) + speedButtonText "x1"
              (정지 상태에서 눌러도, 배속 상태에서 눌러도 항상 1배속으로 통일)
```

### 6) MintButtonUI 해금 상태 갱신

```
SkillManager.HandlePurchase() (재사용형 스킬 아무거나 구매 완료 시)   [SkillManager.cs:96]
 └─ EventHub.RaiseSkillPurchased()
     └─ MintButtonUI.RefreshUnlockState()                        [MintButtonUI.cs] (OnEnable에서 구독)
         └─ SkillManager.IsUnlocked(SkillID.추가발행권한) 재확인 → CanvasGroup alpha/interactable/blocksRaycasts 갱신
```

다른 스킬을 구매해도 이 이벤트가 뜨지만, 매번 추가발행권한 해금 여부만 다시 확인하는 것뿐이라 문제없다.

## 알려진 이슈 / 주의점 후보

- `PriceChartUI.upColor`/`downColor`는 여전히 하드코딩 RGB다. `[SerializeField]`라 씬에 이미 구체값이
  직렬화돼 있어서, 코드 쪽 기본값을 상수화해도 "버튼 색이 바뀌면 조용히 어긋난다"는 실제 문제는 해결되지
  않는다(이미 저장된 씬 값은 코드 기본값과 무관하게 유지됨). 진짜 고치려면 실제 버튼의 `Image` 컴포넌트를
  씬에서 참조로 연결해 런타임에 색을 읽어오는 구조가 필요 — 이번 스코프에서는 손대지 않았다.
- `TradeModalUI`/`CoinControlModalUI`의 모달이 열려있는 동안의 `Update()`(슬라이더/버튼 입력에 따른 실시간
  미리보기)는 의도적으로 폴링을 유지했다. 사용자 입력에 즉시 반응해야 하는 용도라 이벤트 구독으로 바꿀
  대상이 아님.
- `EventLogButton.IsOpen` 폴링도 그대로 남겨뒀다 — 여러 모달이 `OnGamePaused`/`OnGameResumed`를 공유해서
  "패널이 열렸는지"를 이벤트만으로 판별할 수 없어 우선순위를 낮게 봄(지난 세션 진단에서도 동일 결론).
- `TradeModal`의 `isOpen` 플래그는 이 컴포넌트가 씬에서 비활성 상태로 시작할 수 있다는 전제(사용자가 UI
  편집 중 꺼두는 워크플로우)에서 나온 방어 코드다. 비슷한 구조(스크립트 = panel 자기 자신)를 다른 모달에
  새로 만들 경우 동일한 `Start()`/활성화 시점 경합이 재현될 수 있으니 주의.
