# 이슈 : 빌드에서 해상도 바뀌면 UI 깨지는 버그 (앵커 진단 → 수정 완료)

작성일 : 2026-08-04
수정일 : 2026-08-04 — 앵커 재계산 후 적용 완료, Unity MCP로 16:9/16:10 양쪽 좌표 검증까지 마침.

관련 구현 : `Assets/Scenes/SampleScene.unity`의 `Main_Canvas` 하위 오브젝트 RectTransform 앵커 수정.

## 관련 파일 / 오브젝트

- `Assets/Scenes/SampleScene.unity`의 `Main_Canvas` 및 그 직속 자식들 (아래 표 참고)
- `Main_Canvas`의 `CanvasScaler` 컴포넌트

## 증상

빌드 자체의 문제가 아니라 **특정 종횡비 문제** — 사용자가 직접 확인함: 다른 해상도에서는 안 깨지고,
**16:10 해상도로 설정했을 때만** UI 배치가 겹치거나 어긋나는 현상 발생. (16:9 refRes 기준 캔버스가 16:10처럼
세로 비중이 더 큰 종횡비에서 깨진다는 뜻 — 아래 "원인 진단"의 가설과 방향이 일치함.)

## 원인 진단

`Main_Canvas`의 `CanvasScaler`는 `uiScaleMode: ScaleWithScreenSize`, `referenceResolution: 1920x1080`,
`screenMatchMode: MatchWidthOrHeight`, `matchWidthOrHeight: 0.5`로 정상 설정되어 있어 전체 캔버스 자체는
해상도에 맞춰 비율 스케일된다. 문제는 그 안의 개별 UI 오브젝트들 배치 방식이다 — `execute_code`(Unity MCP)로
`Main_Canvas` 직속 자식들의 `RectTransform.anchorMin`/`anchorMax`를 전수 조회한 결과:

**정상 (풀스트레치 0,0~1,1 — 해상도 안 탐)**
`RightPanel`, `SettingsPanel`, `TradeModal`, `MintModal`, `EndingResultUI`, `EventNotification`, `SkillPanel`

**문제 후보 (포인트 앵커(0.5,0.5) + 큰 고정 픽셀 오프셋)**

| 오브젝트 | anchoredPosition | 비고 |
|---|---|---|
| `CoinPriceHeader` | (-701.72, 470) | 좌상단에 붙어야 할 것으로 보이는데 중앙 기준 고정 좌표 |
| `EventLogBtn` | (413, 470) | 상단 우측 고정으로 보임 |
| `SupportPanel` | (294, -448) | 하단 바 형태 |
| `IncreaseScorePanel` | (-82, -448) | 하단 바 형태 |
| `DoubtScorePanel` | (504, -448) | 하단 바 형태 |
| `SkillBtn` | (870, -448) | 하단 바 형태 |
| `ChartPanel` | (-244.48, 122.5), size (1414.48, 575) | 메인 좌측 대형 패널 |
| `CoinControlPanel` | (-244.48, -271.56) | |
| `EventLogPanel` | (0, 86), **size (1920, 908) 고정** | 사이즈 자체가 refRes와 동일하게 하드코딩돼 있어 특히 위험 |

16:9(1920x1080)에서 많이 벗어난 종횡비(세로 화면, 초광폭 등)에서는 `ScaleWithScreenSize`가 전체를 비율
스케일하더라도, 위 오브젝트들의 중앙 기준 고정 오프셋이 실제 화면 경계를 벗어나거나 안 붙어야 할 위치에
뜰 수 있다. 특히 `SupportPanel`/`IncreaseScorePanel`/`DoubtScorePanel`/`SkillBtn`처럼 같은 y값(-448)을 공유하는
하단 바 그룹, `CoinPriceHeader`/`EventLogBtn`처럼 같은 y값(470)을 공유하는 상단 그룹은 "화면 가장자리에
붙어야 하는데 중앙 앵커로 구현된" 전형적인 패턴으로 보인다.

## 진단 방법 (스크린샷 대신 좌표 직접 측정)

Figma MCP가 막혀있어 목업 대조 대신, **Unity MCP `execute_code`로 Editor GameView 해상도를 실제로 16:10으로
전환**(`GameView.selectedSizeIndex` 리플렉션 + `Canvas.ForceUpdateCanvases()`)한 뒤 `Main_Canvas` 하위
오브젝트들의 `RectTransform.GetWorldCorners()`를 직접 찍어서 화면 밖 클리핑/겹침 여부를 픽셀 좌표로 확정함.
(빌드가 아닌 에디터 GameView라도, `CanvasScaler`가 읽는 `Screen.width/height` 값을 그대로 흉내낸 것이므로
빌드에서의 실제 동작과 동일 — `Screen.SetResolution()`은 에디터에서 무시되기 때문에 이 우회가 필요했음.)

## 확정된 문제 (16:10, 1922x1201 기준 측정)

- `SkillBtn`이 우측 화면 밖으로 약 42px 삐져나감 + `RightPanel`과 실제 겹침
- `CoinControlPanel`/`ChartPanel`/`CoinPriceHeader`가 좌측 화면 밖으로 약 39px씩 삐져나감
- `EventLogPanel`(비활성 상태, 열면 노출)이 좌우 양쪽 화면 밖으로 삐져나감
- (부가로 발견) `SkillBtn`을 우측 앵커로 고정하자 인접한 `DoubtScorePanel`(기존 center 앵커 유지)과 새로 겹치는
  부작용 발생 → `DoubtScorePanel`도 같은 방식으로 재조정

## 적용한 수정 (2026-08-04)

전부 `anchorMin/Max=(0.5,0.5)` 포인트 앵커 + 고정 오프셋 → 화면 가장자리 앵커로 변경. 좌표는 16:9(1920x1080)
에서 기존과 동일한 시각적 위치가 나오도록 역산(`newAnchoredPos = old + refSize*(oldAnchor-newAnchor)`).

| 오브젝트 | 변경 전 anchor | 변경 후 anchor | 변경 후 anchoredPosition |
|---|---|---|---|
| `CoinControlPanel` | (0.5,0.5) | (0,0.5) 좌측 | (715.52, -271.56) |
| `ChartPanel` | (0.5,0.5) | (0,0.5) 좌측 | (715.52, 122.50) |
| `CoinPriceHeader` | (0.5,0.5) | (0,0.5) 좌측 | (258.28, 470.00) |
| `SkillBtn` | (0.5,0.5) | (1,0) 우하단 | (-90.00, 92.00) |
| `DoubtScorePanel` | (0.5,0.5) | (1,0) 우하단 | (-456.00, 92.00) |
| `EventLogPanel` | (0.5,0.5) 고정크기 | (0,1)~(1,1) 가로 풀스트레치, 세로 top 고정 | sizeDelta=(0,908), pos=(0,-454) |

16:9 / 16:10 양쪽 다 재측정해서 화면 밖 클리핑 0건, `RightPanel` 관련 겹침 해소 확인. 씬 저장 완료
(`Assets/Scenes/SampleScene.unity`).

## 2차 수정 (같은 날, 사용자가 에디터 실제 화면 확인 후 지적한 문제)

1차 수정 후 사용자가 에디터에서 직접 확인한 결과, 겹침은 아니지만 (a) 하단 슬라이드 3개가 Y축으로
어긋나고 (b) `RightPanel` 내부의 메뉴버튼/LONG/SHORT 버튼이 오른쪽으로 잘리는 문제를 발견함.

- **Y축 어긋남 원인**: 1차 수정에서 `DoubtScorePanel`만 Y앵커를 bottom으로 바꾸고 `SupportPanel`/
  `IncreaseScorePanel`은 그대로 center-Y로 남겨둔 게 원인 (셋이 같은 줄인데 앵커 기준이 달라져서 해상도
  바뀌면 서로 다르게 밀림). → 세 패널 모두 Y앵커를 bottom으로 통일.
- **RightPanel 내부 버튼 클리핑 원인**: `RightPanel` 자체가 풀스트레치라도, sizeDelta가 고정 인셋이라 실제
  폭이 해상도별로 달라짐 (16:9=490px, 16:10=391px, 계산상 약 20% 차이). 그 안의 `SettingsBtn`(메뉴 아이콘)은
  중앙 기준 +200 고정 오프셋, `BtnLong`/`BtnShort`는 폭 460 고정 — 둘 다 좁아진 RightPanel 폭 안에 안 들어감
  (BtnLong/Short는 16:9에서도 여유가 30px뿐이라 원래 아슬아슬했음).

### 적용

| 오브젝트 | 변경 |
|---|---|
| `SupportPanel` | Y앵커 center→bottom, anchoredPosition=(294, 92) |
| `IncreaseScorePanel` | Y앵커 center→bottom, anchoredPosition=(-82, 92) |
| `RightPanel/SettingsBtn` | 앵커 center→우상단(1,1), anchoredPosition=(-45, -71.6) |
| `RightPanel/TradePanel/BtnLong` | 고정폭(460)→가로 스트레치, sizeDelta=(-30, 90), anchoredPosition=(0, -20) |
| `RightPanel/TradePanel/BtnShort` | 고정폭(460)→가로 스트레치, sizeDelta=(-30, 90), anchoredPosition=(0, -125) |

16:9/16:10 재검증: 하단 슬라이드 3개 Y좌표 완전 일치, BtnLong/BtnShort가 RightPanel 폭 안에 완전히 들어옴
확인. (`SettingsBtn`은 16:9 원본 디자인에서도 이미 존재하던 ~1.8px짜리 미세 오버플로우가 그대로 유지됨 —
이번에 생긴 회귀 아니고 무시 가능한 수준.)

## 3차 수정 (같은 날) — 하단 바 잔여 겹침 + 스킬창/개요창(EventLogPanel) 내부 클리핑

사용자가 에디터에서 스킬창(`SkillPanel`)/개요창(`EventLogPanel`)을 열었을 때도 확인해달라고 요청. 두 패널
모두 최상위는 풀스트레치라 안 깨지지만, **내부 자식들이 RightPanel 때와 동일한 패턴**(패널 자체 폭은
16:10에서 줄어드는데 내부 요소는 고정 오프셋/폭)으로 클리핑되고 있었음.

### 원인이 된 구조적 문제: "가운데 낀 요소" 캐스케이드

한쪽 끝 요소만 가장자리 앵커로 바꾸면, 반대쪽은 그대로 center 앵커라서 blend 폭이 줄어들 때 서로 다른
속도로 움직여 새로 겹치는 문제가 반복 발생함 (하단 바 3개, EventLogPanel 탭 2개+닫기버튼, SkillPanel
탭 4개+닫기버튼 전부 이 패턴). 안전한 해법은 앵커 타입을 섞지 않는 것 — **같은 그룹은 전부 같은 앵커
타입(center 유지) + 고정 오프셋 값만 재계산**해서 서로 간의 상대 간격이 종횡비와 무관하게 고정되도록 함.

### 적용

| 대상 | 변경 (anchoredPosition.x) |
|---|---|
| `SupportPanel` | 294 → 270 |
| `DoubtScorePanel` | -456 → -435 |
| `EventLogPanel/OverviewTab` | -499 → -478 |
| `EventLogPanel/CommunityTab` | 372 → 383 |
| `EventLogPanel/CloseBtn` | 880 → 863.5 |
| `EventLogPanel/ContentArea` | -64 → -40 |
| `SkillPanel/Tab1~4(+Shadow)` | 전부 +30 |
| `SkillPanel/CloseBtn`, `DetailBox(+Shadow)`, `CurrencyBox(+Shadow)` | 전부 -10 |

앵커 타입은 전부 그대로(center) 유지 — 오프셋 값만 재계산. 16:10에서 실측 검증(각 패널 `SetActive(true)`로
임시 활성화 후 `GetWorldCorners` 확인, 확인 후 원래 활성 상태로 복구):

- 하단 바: Support-Increase 여유 8.3px, Increase-Doubt 여유 7.3px (겹침 해소)
- `EventLogPanel`: 탭 간 9.4px, 탭-닫기버튼 14.1px, 좌우 여백 6.9px, `ContentArea` 좌우 여백 9.2/84.6px
- `SkillPanel`: **전부 통과했지만 여유가 0.1~0.7px로 매우 타이트함** — Tab1~4+CloseBtn이 차지하는 폭이
  reference(1920)에서도 이미 여유가 40.64 유닛뿐이라, 재배치만으로 낼 수 있는 최대 여유가 딱 이 정도임.
  **더 편안한 여유가 필요하면 탭 폭(현재 420) 자체를 줄이는 등 크기 조정이 필요** — 이번엔 좌표 재계산만
  했음.

## 4차 수정 (같은 날) — 사용자 육안 피드백 4건

에디터에서 직접 본 사용자 피드백 4건 처리:

1. **RightPanel에서 글자가 패널 밖으로 나감** — 범인은 `DateText`가 아니라 `TradePanel/MoneyText`,
   `CoinText`(둘 다 auto-size는 이미 켜져 있었음)와 그 옆 `Money_Image`/`Coin_Image` 아이콘. 박스 자체가
   고정 오프셋이라 `TradePanel`이 16:10에서 좁아지면서(490→391) 박스가 패널 밖으로 나가고 있었음.
   → 아이콘 두 개 오른쪽으로 +40 이동, `MoneyText`/`CoinText` 박스폭 360→280로 축소(위치는 그대로,
   auto-size가 남은 폭에 맞춰 글자 크기를 알아서 줄임).
2. **스킬창 메인 패널을 왼쪽에 붙여줘** — `SkillPanelBox`/`SkillPanelBoxShadow`가 원래 좌측 여백이
   19.36 유닛 있었음 → 완전히 flush(여백 0)로 이동.
3. **개요창 열었을 때 스탯 슬라이드+스킬버튼만 보이게** — `EventLogPanel`의 배경(상단 고정+높이 908
   고정)이 `RightPanel`(높이 스트레치)과 종횡비별로 반대 방향으로 움직여서, 16:10에서 RightPanel 하단
   내용이 배경 밖으로 삐져나오고 있었음. → `EventLogPanel`을 `RightPanel`과 **완전히 동일한 세로
   스트레치 수식**(anchorMin/Max=(0,0)~(1,1), sizeDelta.y=-171.87, anchoredPos.y=85.94)으로 변경해서
   모든 종횡비에서 RightPanel 하단과 정확히 같은 선에서 잘리도록 함(실측: 상하 차이 0.0). 이 과정에서
   `SkillBtn`이 배경 하단에 4.8px 살짝 덮이는 것도 발견해서 `SkillBtn.y`를 92→82로 더 내려서 해소.
4. **뉴스 아이콘(`EventLogBtn`)을 오른쪽으로 조금** — x: 413→453.

### 부작용: DoubtScorePanel이 IncreaseScorePanel/SkillBtn 사이에 낀 문제 재발

3번 처리 중 `SkillBtn.y`를 건드리면서 확인차 재검증했더니, 3차 수정 때 고친 `DoubtScorePanel`↔`SkillBtn`
간격이 어느새 음수(-15 유닛, 항상 겹침)가 되어 있었음 — 3차 수정에서 `IncreaseScorePanel`과의 겹침을
없애려고 `DoubtScorePanel`을 오른쪽으로 옮긴 게 반대편(`SkillBtn`) 쪽 여유를 깎아먹은 것. `IncreaseScorePanel`
(고정, 안 건드림)과 `SkillBtn`(이미 화면 우측 여백이 얇아서 더 못 움직임) 사이에 `DoubtScorePanel`(폭 550)이
낄 공간이 16:10 기준 542.83 유닛뿐인데 필요한 건 560 유닛 — **재배치만으로는 수학적으로 불가능**했음.
→ `DoubtScorePanel` 폭을 550→525로 축소(4.5%)하고 위치 재계산(anchoredPosition.x: -435→-447.33)해서
양쪽 다 해소. 16:10 실측: Increase-Doubt 여유 6.9px, Doubt-SkillBtn 여유 8.6px, SkillBtn 화면 우측 여유
4.4px — 전부 통과.

**교훈**: 이 씬의 하단 바/탭 로우처럼 여러 요소가 촘촘하게 붙어있는 그룹은, 한쪽을 고치면 반대쪽 여유가
줄어드는 시소 관계가 계속 발생함 — 요소를 옮길 때마다 그 요소의 "양쪽 이웃" 모두와의 간격을 다시
확인해야 함 (이번에 세 번 정도 이걸 놓쳐서 재작업함).

## 남은 사항 (이번 수정 범위 밖, 미해결)

`RightPanel/TimePanel`의 `DateText`도 비슷한 계열 위험이 있음 (폭 348.29 고정, TimePanel 자체 폭이
16:10에서 287.71까지 줄어듦 — 텍스트 절반폭(174)이 TimePanel 절반폭(143.86)보다 커서 이론상 겹칠 수 있음).
다만 Text는 Mask 없이는 하드 클리핑되지 않고, 사용자가 지적한 범위(메뉴/LONG/SHORT)에도 없어서 이번엔
안 건드림 — 날짜 텍스트가 잘려 보이면 추가로 처리 필요.

`SettingsPanel`, `TradeModal`, `MintModal`, `EndingResultUI` 등 나머지 모달들의 내부 자식은 이번에 확인 안
함 — `SkillPanel`/`EventLogPanel`과 같은 패턴(내부 고정 오프셋)이 있을 수 있음, 열어봤을 때 문제 있으면
알려주면 같은 방식으로 처리 가능.

## 안드로이드(2400x1080) 후속 수정 (2026-08-14)

`AndroidSampleScene.unity`에서 ChartPanel/CoinControlPanel/CoinPriceHeader 겹침을 고친 뒤(별도 세션),
아직 안 열어본 모달들을 같은 방식(Play 모드 + `eval` + `GetWorldCorners`, 2400x1080 기준)으로 이어서 점검함.

- **TradeModal(매수/매도 수량창), CoinModalPanel**: 둘 다 `ModalBox`가 중앙 고정 크기(1397x760)라 화면
  안에 완전히 들어옴 — 문제 없음, 수정 안 함.
- **SkillPanel**: `Tab1~4`/`CloseBtn`(중앙 앵커 + 상단 쪽 고정 오프셋)이 화면 위로 최대 24.6px 잘려나감.
  원인: `CanvasScaler`가 `ScaleWithScreenSize`+`matchWidthOrHeight=0.5`라서, 2400x1080처럼 참조
  해상도(1920x1080)보다 옆으로 넓은 화면에서는 스케일이 커지는 대신 세로로 "보이는" 기준 영역이
  960대로 줄어듦 — 중앙에서 위/아래로 고정 오프셋 잡은 요소들이 그만큼 화면 위로 밀려나감(하단 바
  겹침과 같은 근본 원인, 이번엔 세로 방향). `Tab1~4`+Shadow+`CloseBtn`을 y로 -28, `CurrencyBox`+Shadow를
  -16 내려서 화면 상단 클리핑과 `SkillPanelBox`/`DetailBox`와의 겹침 모두 해소.
- **EventLogPanel(개요/커뮤니티)**: 같은 원인으로 `OverviewTab`/`CommunityTab`/`CloseBtn`이 최대 30px
  화면 위로 잘림. y로 -34 내리고, 동시에 `ContentArea`(중앙 콘텐츠 박스, 715 고정 높이)가 패널 하단
  경계보다 24px 더 아래로 내려가 있던 것도 발견(마스크가 없어서 시각적으로 하단 바 쪽과 겹칠 수 있는
  상태)해서 `ContentArea` 높이를 -62 줄여 상하 여유를 동시에 확보. 그 안의 `EventPanelBox`/`ChatPanelBox`도
  높이 -8 줄여서 줄어든 배경 박스 안에 다시 들어오게 정리.
- 전부 Play 모드 재검증 완료(여유 6.7~15.3px), 씬 저장함.

### 남은 사항
- `SettingsPanel`, `MintModal`, `EndingResultUI`는 이번에도 확인 안 함.
- `EventLogPanel/ContentArea`가 패널 하단보다 아래로 내려가 있던 문제(마스크 없음)는 이번에 여유만
  만들어서 해소했지만, 근본적으로 이 패널엔 `RectMask2D`가 없어서 비슷한 하단 오버플로우가 다른
  요소에서도 생길 수 있음 — 다음에 비슷한 증상 보이면 마스크 추가를 고려.

## 안드로이드 메인 화면(RightPanel 등) 수정 (2026-08-14, 같은 날 3번째)

모달이 아니라 상시 노출되는 메인 화면 쪽에서 사용자가 육안으로 직접 짚어준 5건을 같은 방식(Play 모드 +
`GetWorldCorners`, 2400x1080)으로 수정.

| 증상 | 원인 | 수정 |
|---|---|---|
| 시간/날짜 안 보임 | `RightPanel/TimePanel`이 `sizeDelta.y=-784.13`짜리 거의 찌그러진 컨테이너였고, 그 안의 `DateText`가 화면 위로 46px 벗어나 있었음(`PlayPauseBtn`/`SpeedBtn`도 화면 끝에 거의 붙어있었음) | `TimePanel` 전체(anchoredPosition.y)를 -59 내려서 자식들(Date/Play/Speed) 상대 배치는 그대로 두고 화면 안으로 이동 |
| 매수/매도 버튼이 패널 가로폭에 안 맞음 | `BtnLong`/`BtnShort` 자체는 이미 가로 스트레치(686.62 유닛)인데, 그 안의 장식 그래픽(`green1/2/3`, `red1/2/3`)이 고정폭(합쳐서 342 유닛)이라 버튼 실제 폭의 41%만 채우고 있었음 | 장식 그래픽들을 버튼 폭의 ~95%(스케일 ×1.907)로 균등 확대, 상대 배치(겹침 비율)는 유지 |
| 내 자산 합계 + 코인아이콘이 ChartPanel에 겹침 | `CoinPriceHeader`(좌상단 고정)와 `ChartPanel`(수직 중앙 고정 높이)이 각자 다른 기준점에서 고정 오프셋을 쓰다 보니, 2400x1080에서 세로 방향 여유가 줄면서 서로 46.9px 겹침 | `CoinPriceHeader`를 위로 +19, `ChartPanel` 높이를 -60(위쪽에서만 줄어들도록 자동 대칭 축소 — 아래는 `CoinControlPanel`과의 여유가 충분해서 문제 없음) — 둘 다 7~8px 여유로 재검증 |
| 뉴스패널아이콘(`EventLogBtn`)이 화면 밖 | 중앙 앵커 + 고정 오프셋(`pos.y=470`)이 2400x1080에서 화면 위로 30.2px 벗어남 | `anchoredPosition.y`를 -36 내려서 10px 여유로 화면 안에 들어옴(`TimePanel`의 실제 텍스트/버튼들과는 가로 위치가 겹치지 않아 새 충돌 없음, 배경 바 컨테이너와만 가로로 겹치는데 그건 문제 없음) |
| 아래 슬라이드 패널(Support/Increase/Doubt)이 매도하기(`BtnShort`)와 겹침 | `DoubtScorePanel`이 `BtnShort`와 가로로 겹치는 위치에 있는데 세로로도 39px 겹침 | 사용자가 지정한 방식대로 세 패널 전부(+그림자) 높이를 균일하게 줄임 — 바닥선은 고정하고 위쪽만 줄어들도록 `anchoredPosition.y -22`/`sizeDelta.y -44`를 같이 적용해서 셋 다 동일한 높이로 유지(시각적 일관성), `BtnShort`와 10.2px 여유 확보 |

전부 Play 모드 재검증 완료(여유 6.7~20px), 씬 저장함. `SettingsPanel`/`MintModal`/`EndingResultUI`는 여전히
미확인.
