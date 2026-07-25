# 씬 오브젝트 구조

`SampleScene` 기준, Unity MCP로 직접 조회해 정리한 문서. 좌표는 `Main_Canvas`(1920x1080, Scale With Screen Size,
match=0.5) 기준 절대 좌표(캔버스 좌하단이 0,0)로 표기하고, 괄호 안에 각 오브젝트의 `anchoredPosition`(캔버스
중심 960,540 기준 상대값)을 병기한다.

이 문서는 UI 작업 시 기존 레이아웃과 겹치지 않게 배치를 잡거나, 기존 오브젝트를 참조하는 스크립트를 짤 때
참고용으로 만들었다. 실제 값은 Inspector가 항상 최신 기준이며, 이 문서는 스냅샷이다.

---

## 루트 오브젝트

| 이름 | 컴포넌트 | 비고 |
|---|---|---|
| Main Camera | Camera, AudioListener, UniversalAdditionalCameraData | |
| Global Light 2D | Light2D | |
| EventSystem | EventSystem, InputSystemUIInputModule | |
| Managers | TimeManager, JobManager, MarketManager, PlayerManager, SkillManager, SettingsUI | 게임 로직 전부가 이 오브젝트 하나에 부착됨. `DontDestroyOnLoad` 대상(`MarketManager.Awake` 등) |
| Main_Canvas | RectTransform, Canvas, CanvasScaler(1920x1080, ScaleWithScreenSize, match=0.5), GraphicRaycaster | 아래 UI 전체의 루트 |

---

## Main_Canvas 하위 트리

```
Main_Canvas (1920x1080)
├─ RightPanel                  (x:1430~1920, y:172~1080  — 우측 스트레치 패널)
│   ├─ TimePanel  [TimeUI]     (날짜/배속 컨트롤, 우측 상단)
│   │   ├─ DateText
│   │   ├─ PauseBtn
│   │   ├─ PlayBtn
│   │   └─ SpeedBtn
│   ├─ TradePanel [PlayerUI]   (자산/코인 표시 + Long/Short 거래 연결 완료, 우측 중단)
│   │   ├─ MoneyText / CoinText
│   │   ├─ Money_Image / Coin_Image
│   │   ├─ TradeAmountText
│   │   ├─ BtnPlus1 / BtnPlus10 / BtnPlus100 / BtnPlusMAX  (거래 수량 조절)
│   │   ├─ BtnShort   (빨강 #AC3232)
│   │   └─ BtnLong    (초록 #4B692F)
│   └─ SettingsBtn
├─ CoinControlPanel             (x:8~1423, y:172~365 — "코인 발행량 조작" 오버레이)
│   ├─ TotalSupplyText / AdjustAmountText
│   ├─ BtnPlus/Minus, BtnAmount1/10/100/MAX, BtnAdjust(파랑 #5865F2)
│   └─ Image (코인 아이콘)
├─ SettingsPanel                 (전체화면 설정 모달)
│   ├─ SettingsCloseBtn
│   └─ SetttingsQuitBtn
├─ SupportPanel  [StatGaugeUI] (x:19~569,   y:30~155 — "코인 지지도" 게이지, 좌측, 연결 완료)
│   ├─ BaseWhiteBar / PositiveBar / NegativeBar / REDBAR
│   ├─ SupportImage / SupportText
│   ├─ SupValueText, "+100"/"-100" 라벨
├─ IncreaseScorePanel [StatGaugeUI] (x:603~1153, y:30~155 — "코인 상승률" 게이지, 중앙, 연결 완료)
│   └─ SupportPanel와 동일 구조 (IncreaseText/IncValueText/IncreaseImage)
├─ DoubtScorePanel [StatGaugeUI] (x:1189~1739, y:30~155 — "의심도" 게이지, 우측, 연결 완료)
│   └─ 동일 구조 단순화 버전 (PositiveBar만, NegativeBar 없음 — Doubt는 0~100만 존재)
│   ├─ DoubtImage / DoubtText / DoubtValueText
├─ SkillBtn                      (x:1830, y:92 — 스킬 패널 진입 버튼, 최우측 하단)
├─ ChartPanel        [PriceChartUI]              (x:8~1423, y:375~950 — 중앙 캔들스틱 차트)
│   └─ (Candle_0 ~ Candle_13, 런타임에 코드로 생성 — 씬 파일엔 없음)
├─ CoinPriceHeader   [CoinPriceHeaderUI]          (x:8~508, y:960~1050 — 코인명/현재가 표시)
│   ├─ CoinIcon (Image)
│   └─ PriceText (TextMeshProUGUI)
└─ EventLogBtn       [EventLogButton]             (x:1333~1413, y:970~1050 — 이벤트 로그 진입 버튼)
```

인스턴스ID(참고용, 씬 재저장 시 바뀔 수 있음): Main_Canvas=53600, RightPanel=53478, TimePanel=52864,
TradePanel=52826, CoinControlPanel=53024, SettingsPanel=52908, SupportPanel=52944, IncreaseScorePanel=53280,
DoubtScorePanel=53074, SkillBtn=53578.

---

## 캔들 차트 + 코인명/가격 헤더 (구현 완료)

`Main_Canvas` 하위에 다음 2개가 추가됐다 (`CoinControlPanel`과 동일한 x폭 1414.48로 좌우 정렬을 맞춤).

- **ChartPanel** (`PriceChartUI`, `ImageWithRoundedCorners` radius 30) : anchoredPosition (-244.48, 122.5),
  sizeDelta (1414.48, 575) → x: 8~1423, y: 375~950. 최근 14개 캔들을 오브젝트 풀링으로 그린다.
- **CoinPriceHeader** (`CoinPriceHeaderUI`) : anchoredPosition (-701.72, 470), sizeDelta (500, 90) →
  `ChartPanel` 바로 위(y: 960~1050)에 배치, 코인 아이콘 + "코인명 ₩현재가" 텍스트만 배경 없이 캔버스 위에
  떠 있는 형태 (목업과 동일). 자식으로 `CoinIcon`(Image)과 `PriceText`(TextMeshProUGUI)를 갖는다.
- **EventLogBtn** (`EventLogButton`) : `Main_Canvas` 직속 자식, anchoredPosition (413, 470), sizeDelta
  (80, 80) → 헤더 줄 우측 끝(절대좌표 x:1333~1413, y:970~1050). 처음엔 48x48로 만들었다가 목업 비율 대비
  너무 작다는 피드백을 받고 80x80으로 키웠다. 클릭할 때마다 "UI뉴스아이콘_on"/"_off" 스프라이트를 토글한다.
  이벤트 로그 패널 본체는 아직 없어서 패널을 열고 닫는 연결은 없음.

(스트리머 패널, X축 날짜 라벨, 헤더 우측 아이콘은 아직 씬에 없음 — `Next_Tesk.md` "캔들 차트 후속 작업" 참고)

---

## 참고 — 기존 UI 스크립트 위치

`Assets/Scripts/UI/`에 `TimeUI.cs`(TimePanel), `PlayerUI.cs`(TradePanel), `SettingsUI.cs`(Managers에 부착,
SettingsPanel 제어)가 있다. 둘 다 `EventHub`를 거치지 않고 `TimeManager.Instance`/`PlayerManager.Instance`를
`Update()`에서 직접 폴링하는 구식 패턴이다 (`PROJECT_ARCHITECTURE.md`가 정의한 "UI는 EventHub만 호출" 원칙보다
이전에 만들어진 코드). 신규 UI는 `EventHub.OnMarketUpdated` 구독 방식을 따른다.
