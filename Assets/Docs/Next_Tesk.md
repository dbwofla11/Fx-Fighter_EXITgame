# 다음 작업

완료된 작업 목록은 `Completed_Tasks.md`로 분리했다. 이 문서에는 아직 안 된 것(후보/버그)만 남긴다.

---

## 후보 : UI 연결

`EventHub`의 이벤트 대부분은 Manager 쪽 구독 로직만 갖춰져 있고, 이를 발행하는 실제 UI가 아직 없다.

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

## 후보 : 매수/매도 모달(TradeModal) Figma 목업 후속 작업

Figma "코인발행" 팝업(피그마 파일 `EjUw2LdqxAYhL2180OAXHo`, node `1515:1038`) 대비 아직 구조적으로 없는 항목들.
통계블록(현재/예상 현금·코인 아이콘 표시)은 이번에 추가함 — 아래는 남은 것.

- **슬라이더**: 드래그로 매수/매도 수량을 조절하는 가로 슬라이더(트랙+손잡이). 아직 `TradeModalUI.cs`에 관련
  로직 없음.
  - **슬라이더 맨 우측 숫자(현재 "0개"로 고정된 placeholder)는 "지금 살 수 있는/팔 수 있는 최대 수치"를
    뜻한다** (사용자 확인, 2026-07-31). Long이면 현재 현금으로 살 수 있는 최대 개수, Short면 보유 코인
    전량 — `TradeModalUI.SetTradeAmountToMax()`가 이미 계산하는 값과 동일한 로직이므로 그대로 재사용하면 됨.
- **"+/-" 버튼**: Figma엔 +1/+10/+100/+MAX 외에 "+/-" 버튼이 하나 더 있음(용도 추정: 부호 반전 또는 리셋).
  현재 스크립트엔 이 버튼도 관련 로직도 없음 — 정확한 동작은 확인 필요.
- **닫기 X 버튼 + 단일 확인버튼 구조**: Figma는 우상단 X로 닫고 하단엔 "매수하기" 버튼 하나뿐. 지금은
  `BtnConfirm`+`BtnCancel`이 나란히 있는 구조라서, 구조를 맞추려면 X 버튼 추가 + Cancel 제거(또는 X와 통합)가
  필요함.

## 후보 : 캔들 차트 후속 작업

- `PriceChartUI.visibleCandleCount`(기본 16)/`candleWidthRatio`(0.95) 등 밸런스 수치는 실제 플레이 후 추가
  조정 필요할 수 있음


