# 다음 작업

완료된 작업 목록은 `Completed_Tasks.md`로 분리했다. 이 문서에는 아직 안 된 것(후보/버그)만 남긴다.

---

## 최우선 후보 : 거래(Long/Short)/발행량 조작 모달 UI 리뉴얼 — 기능/연결 단계

메인 UI가 살짝 리뉴얼됐다 — 목업 스크린샷 4장(기본 화면, 매수 모달, 매도 모달, 추가 발행 모달) 확인 결과,
지금처럼 패널에 수량 조작 UI가 항상 보이는 방식이 아니라 **long/short 버튼(또는 "코인 발행" 버튼)을 누르면
모달 팝업이 뜨고, 그 안에서 수량을 고른 뒤 확인 버튼을 눌러야 실제로 실행되는 방식**으로 바뀐다.

**진행 상황(2026-07-28 기준) : 배치(레이아웃)까지만 끝났고, 모달 기능/연결은 아직 손도 안 댔다.** 처음에
모달 3종(TradeModal 등)을 직접 만들다가, 작업을 배치 단계와 기능 단계로 나누기로 하면서 그 WIP는 전부
삭제하고 원상복구했다. 대신 기본 화면(모달이 닫혀있는 상태)이 목업과 같은 모양이 되도록 기존 오브젝트만
재배치했다:

- `TradePanel` : `BtnPlus1`/`BtnPlus10`/`BtnPlus100`/`BtnPlusMAX`/`TradeAmountText`를 `SetActive(false)`로
  숨김. `BtnLong`/`BtnShort`는 세로로 쌓은 전체폭 바(초록/빨강)로 리사이즈 + 색상 변경. 내부 장식용 자식
  오브젝트(`green1/2/3`, `red1/2/3`)도 넓어진 폭에 맞춰 스케일 조정. `MoneyText`/`CoinText`는 줄바꿈되던
  버그를 고쳐서 한 줄로 표시되게 함. **`PlayerUI.cs` 스크립트 자체는 손대지 않고 원본 그대로 되돌려놨다** —
  즉 지금 Play해보면 수량 버튼이 안 보여서 `tradeAmount`를 올릴 방법이 없고, 그 결과 `BtnLong`/`BtnShort`가
  계속 비활성화 상태로 보인다 (`RefreshTradeButtons`의 `tradeAmount > 0` 조건 때문).
- `CoinControlPanel` : `BtnPlus/Minus`/`BtnAmount1,10,100,MAX`/`BtnAdjust` 전부 `SetActive(false)`로 숨기고,
  패널 자신의 배경 `Image`도 `enabled=false`로 꺼서 테두리 없이 텍스트만 뜨도록 만듦. `TotalSupplyText`/
  `AdjustAmountText`를 좌측 두 줄로 재배치(화면 밖으로 잘리던 것도 수정). 코인 아이콘은 원래 1개뿐이라
  줄마다 하나씩 필요해서 하나 복제해 `CoinIcon1`(위, "코인거래량" 스프라이트)/`CoinIcon2`(아래, 기존 코인
  스프라이트)로 이름 붙여 배치했다. **이 패널을 참조하는 스크립트는 여전히 프로젝트에 하나도 없다** — 버튼을
  숨겨놓기만 했을 뿐 `EventHub.RaiseManipulateSupply`를 호출하는 코드는 아직 없다.
- 모달 열기/닫기 참고할 기존 패턴 : `Assets/Scripts/UI/SettingsUI.cs` — `settingsPanel.SetActive(true/false)`를
  open/close 버튼으로 토글하는 가장 단순한 형태 (`SettingsPanel`, "전체화면 설정 모달"). 새 모달 3개도 이
  show/hide 골격을 재사용하면 될 듯.
- 슬라이더(`UnityEngine.UI.Slider`)는 프로젝트에 처음 쓰이는 컴포넌트로 보임(기존엔 버튼 조합만 사용).
  `com.unity.ugui` 패키지에 포함돼 있어 추가 패키지 설치는 불필요. Slider의 `fillRect`/`handleRect`/
  `targetGraphic` 같은 컴포넌트 참조 프로퍼티는 Unity MCP `manage_components.set_property`로 설정할 때 필드명을
  camelCase(`fillRect`)가 아니라 직렬화 필드명(`m_FillRect`)으로, value는 `{"instanceID": <RectTransform의
  컴포넌트 인스턴스ID>}` 형태로 넘겨야 먹혔다 (camelCase나 GameObject 인스턴스ID로는 계속 변환 실패).
- 확정 전 "거래 후 예상 현금/코인" 미리보기 계산 공식은 `PlayerManager.HandleBuyCoin`/`HandleSellCoin`과
  동일하게 맞춰야 함 : Long은 `cost = amount × CurrentPrice`, Short는
  `revenue = amount × CurrentPrice × (1 + CashBonus/100)`. **미리보기는 계산만 하고, 확정 버튼을 누르기
  전까지 `EventHub.RaiseBuyCoin` 등을 호출하면 안 된다** (지금은 이 미리보기 로직 자체가 없음).
- 스트리머 패널(캐릭터 이미지+말풍선)은 목업엔 있지만 씬에 없는 완전히 새 요소라 이번 배치 작업에서
  의도적으로 제외했다. 아래 "후보 : UI 연결" 항목과 함께 별도로 처리.

할 일 (기능/연결 단계, 순서대로) :
1. 모달 3종(매수/매도/추가발행) GameObject를 실제로 만든다 (패널+슬라이더+수량버튼+미리보기 텍스트+확인/취소
   버튼). 매수/매도는 구조가 거의 같으니 모드(Long/Short)만 다른 공용 모달 하나로 만드는 걸 권장.
2. `TradePanel`의 숨겨둔 `BtnPlus1/10/100/MAX`/`TradeAmountText`를 매수/매도 모달 안으로 이동(또는 모달 쪽에
   새로 만들고 기존 건 삭제)하고, `PlayerUI.cs`의 즉시 실행 로직(`OnLongClicked`/`OnShortClicked`)을 "모달
   열기"로 바꾼 뒤, 모달의 확인 버튼 클릭 시점에만 `EventHub.RaiseBuyCoin`/`RaiseSellCoin`을 호출하도록 옮긴다.
3. "코인 발행" 트리거 버튼(신규, `RightPanel` 빈 공간에 배치하면 됨)을 만들어 클릭 시 `CoinControlPanel`을
   모달로 띄우고, `EventHub.RaiseManipulateSupply(amount)` 연결 (지금까지 아예 연결된 적 없음).
4. "코인 발행" 버튼은 `SkillManager.IsUnlocked(SkillID.추가발행권한)`가 false면 비활성화/숨김 처리해야
   한다 (기존 "후보 : UI 연결"의 발행량 조작 버튼 항목과 동일한 조건). `gameObject.SetActive(false)`로 끄면
   `Update()`가 멈춰서 나중에 스킬을 사서 조건이 true가 돼도 다시 안 나타나니, `CanvasGroup.alpha`/
   `interactable`/`blocksRaycasts` 토글 방식을 쓸 것 (한 번 이 실수로 만들었다가 고쳤음).

---

## 후보 : UI 연결

`EventHub`의 이벤트 대부분은 Manager 쪽 구독 로직만 갖춰져 있고, 이를 발행하는 실제 UI가 아직 없다.

- 발행량 조작 버튼 (`EventHub.RaiseManipulateSupply`) — `SkillManager.IsUnlocked(SkillID.추가발행권한)`가 false면
  버튼을 비활성화/숨김 처리해야 한다 (게임 로직은 이미 막고 있지만 UI에서도 표시해줘야 함)
- 스킬 아이콘/구매 버튼 (`EventHub.RaiseSkillClicked`/`RaiseSkillPurchased`)
- 직업 선택 화면 (`EventHub.RaiseJobSelected`)
- 시사 이벤트 수동 트리거가 필요한 경우의 UI (`EventHub.RaiseNewsEvent`) — 자동 발생은 이미 `MarketManager`에 구현됨
- 이벤트 로그 패널 — 진입 버튼(`EventLogBtn`/`EventLogButton.cs`)은 완료됨(`Completed_Tasks.md` 참고), **패널
  본체가 아직 없다.** `MarketManager.EventLog`(`IReadOnlyList<EventLogEntry>`)를 순회하며
  `Profile.message`/`Date`/`Profile.effects`를 표시하면 된다. 목업 확인 결과 "개요"/"커뮤니티" 탭과 X 닫기
  버튼이 있는 전체화면 모달이고(하단엔 기존 스탯 게이지 3종이 그대로 보임 — `SupportPanel` 등과 같은 레이어에
  오버레이되는 형태로 추정), 긍정 이벤트는 연두색/부정 이벤트는 빨간색 배경 카드로 구분해서 보여준다. 패널이
  만들어지면 `EventLogButton.Toggle()`에서 on/off 아이콘 전환과 함께 패널 표시/숨김도 같이 처리하도록 이어서
  연결하면 된다.
- 엑시트 버튼 (`EventHub.RaiseExitRequested`) — `MarketManager.CanExit`(현금 >= `TargetAsset`=10억)가 true일 때만
  누를 수 있도록 활성화 처리. 목표 금액 진행률 표시(스크린샷의 "목표금액/현재금액/목표까지 남은 금액")도 이
  값들을 그대로 읽으면 된다.
- 엔딩 결과 화면 — `EventHub.OnGameEnded(EndingType)`을 구독해 4종 엔딩(체포/엑시트/영웅/거지)에 맞는 결과 문구를
  표시. 각 엔딩의 설명 텍스트는 `Game_Formula.md` 5장에 정리되어 있음.

`TimeUI`, `PlayerUI`, `SettingsUI`만 기존처럼 `TimeManager`/`PlayerManager`를 직접 참조하는 상태이고, 나머지는
설계는 끝났으나 화면이 없다.

- 스트리머 패널의 가격 반응(스프라이트 전환/멘트) — `PlayerStat.StreamerReaction`(5단계)을 이미 읽을 수 있음.
  `Assets/Sprites/스트리머상태` 폴더에 실제 스프라이트가 들어오면 UI 팀원이 연결하면 된다. 임계값(50/10) 밸런스는
  실제 플레이 후 조정 필요할 수 있음.

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

## 후보 : 캔들 차트 후속 작업

- `PriceChartUI.visibleCandleCount`(기본 16)/`candleWidthRatio`(0.95) 등 밸런스 수치는 실제 플레이 후 추가
  조정 필요할 수 있음

## 버그 후보 : SkillManager NullReferenceException

캔들 차트 작업 중 Play 모드 진입 시 콘솔에서 `SkillManager.cs:43`(`SkillManager.Initialize()`)에서 발생하는
`NullReferenceException`을 발견했다. 이번 작업과 무관한 기존 코드라 손대지 않았다 — `skillDatabase`가 비어있거나
참조가 안 걸린 것으로 추정되나 확인 필요.

