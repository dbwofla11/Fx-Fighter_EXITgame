# 이슈 : 이벤트 알림 모달 작동 방식 / 호출 스택 정리

작성일 : 2026-08-02

관련 구현 : `Completed_Tasks.md` "이벤트 알림 모달" 항목. Figma 프레임 없이 사용자가 준 스크린샷 2장(긍정/부정
이벤트 알림)을 기준으로 만들었다.

## 관련 파일

- [EventHub.cs](../../Scripts/manager/utils/EventHub.cs) — `OnEventTriggered` 이벤트 추가(기존 파일).
- [MarketManager.cs](../../Scripts/manager/MarketManager.cs) — `LogEvent()`에서 `EventHub.RaiseEventTriggered`
  호출 추가(기존 파일).
- [EventEffectFormatter.cs](../../Scripts/UI/Utils/EventEffectFormatter.cs) — 신규. `EventLogPanelUI`에 있던
  긍정/부정 색상(`#B1FFB1`/`#FFBAB1`)과 효과 문구 포맷(`BuildEffectsText`/`DescribeEffect`)을 공용으로 뽑아냄.
- [EventLogPanelUI.cs](../../Scripts/UI/EventModal/EventLogPanelUI.cs) — 위 로직을 `EventEffectFormatter` 호출로
  교체(기존 파일, 동작은 그대로).
- [EventCardView.cs](../../Scripts/UI/EventModal/EventCardView.cs) — 참고, 수정 없음. `background`/`titleText`/
  `dateText`/`effectsText` 4개 필드만 있는 순수 뷰라 알림 패널에도 그대로 재사용했다.
- [EventNotificationUI.cs](../../Scripts/UI/EventModal/EventNotificationUI.cs) — 신규. 알림 표시/숨김 + 게임
  일시정지 로직.
- `Assets/Scenes/SampleScene.unity` — `Main_Canvas` 밑에 `EventNotification`/`Panel` 오브젝트 신규 배치(아래
  "씬 구조" 참고).

## 작동 방식

**알림이 뜨는 조건** : 시사 이벤트가 실제로 발생하는 모든 경로(자동 확률 발생 `MarketManager.TriggerNewsEvent`,
수동 트리거 `HandleNewsEvent`, 무조건 발생 `TriggerGuaranteedEvent`)가 전부 `LogEvent()`를 거치므로, 그 안에서
한 번만 `EventHub.RaiseEventTriggered`를 호출하면 세 경로 모두 커버된다.

**왜 `EventCardView`를 그대로 재사용했는가** : 이벤트 로그 패널의 카드(제목/날짜/효과 목록 + 배경색)와 알림
패널이 보여줘야 하는 정보가 완전히 같다(`EventSO.message`/`Date`/`effects`). 배치(카드는 좁고 세로로 쌓임,
알림은 화면 중앙 위 큰 배너)만 다르므로, 뷰 스크립트는 그대로 두고 씬에 다른 크기/위치의 오브젝트를 새로
만들어 같은 컴포넌트를 붙였다.

**게임 일시정지** : `TradeModalUI`/`EventLogPanelUI`와 동일한 관례로 UI 쪽에서 `EventHub.RaiseGamePaused()`/
`RaiseGameResumed()`를 직접 호출한다(Manager가 판단하는 게 아니라 "이 UI가 떠 있다"는 사실 자체가 일시정지
조건).

**항상 구독 vs 토글되는 패널** : `EventNotificationUI`가 붙은 루트 오브젝트(`EventNotification`)는 항상
활성 상태로 두고, 실제로 보이는 배너(`Panel`)만 켜고 끈다. `StatGaugeUI`처럼 상시 `EventHub` 구독이 필요한
다른 UI와 동일한 패턴 — 패널 자체를 비활성화해두면 `EventHub.OnEventTriggered` 구독이 끊겨서 다음 이벤트를
못 받기 때문.

## 호출 스택

```
[시사 이벤트 발생] (자동 NextTurn / 수동 HandleNewsEvent / 무조건 TriggerGuaranteedEvent 중 하나)
 └─ LogEvent(fired)                                            [MarketManager.cs]
     ├─ runtimeEventData.Log.Add(entry)        ← 기존 "이벤트 로그" 패널용
     └─ EventHub.RaiseEventTriggered(entry)     ← 신규
         └─ EventNotificationUI.HandleEventTriggered(entry)     [EventNotificationUI.cs]
             ├─ EventEffectFormatter.CategoryColor / BuildEffectsText
             ├─ cardView.Populate(color, message, UIFormat.DateDot(date), effects)
             ├─ panel.SetActive(true)
             └─ EventHub.RaiseGamePaused() → TimeManager.PauseGame() (Time.timeScale = 0)

[X 버튼 클릭]
 └─ EventNotificationUI.Close()
     ├─ panel.SetActive(false)
     └─ EventHub.RaiseGameResumed() → TimeManager.ResumeGame() (Time.timeScale = 이전 배속)
```

## 씬 구조

`Main_Canvas` 직속 자식 `EventNotification`(캔버스 전체 스트레치, 항상 활성) 밑에 `Panel`(기본
`activeSelf = false`) 하나만 있다.

- `Panel` : anchoredPosition (-126.5, 330), sizeDelta (1133, 320), pivot/anchor 전부 (0.5,0.5) — `ChartPanel`
  등 다른 `Main_Canvas` 직속 자식과 동일한 중심 기준 앵커 관례. `Image` + `ImageWithRoundedCorners`(radius
  24) + `EventCardView`.
  - `TitleText` : 좌상단 앵커(0,1), anchoredPosition (40,-30), 34pt Bold, 검정, 왼쪽 정렬.
  - `DateText` : 우상단 앵커(1,1), anchoredPosition (-40,-100), 22pt, 검정, 오른쪽 정렬.
  - `EffectsText` : 우상단 앵커(1,1), anchoredPosition (-40,-150), sizeDelta (400,100) — 2줄 기준, 28pt
    Bold, 검정, 오른쪽 정렬.
  - `CloseBtn` : 우상단 앵커(1,1), anchoredPosition (-24,-24), 48x48, `Assets/Sprites/UI아이콘/취소버튼.png`를
    진한 회색(#4d4d4d)으로 틴트(연한 배경에서 잘 보이게).

## 알려진 이슈 / 주의점

- **위치·크기는 눈대중** : Figma 프레임이 없어 사용자가 준 스크린샷 2장을 육안으로 재서 좌표를 계산했다
  (스크린샷을 1512x982 목업 기준으로 가정하고 `ChartPanel` 등 기존 요소와 동일한 scaleX/Y 변환을 적용). 정확한
  픽셀 일치는 보장 못 함 — 실제 Play로 확인 후 조정 필요할 수 있음.
- **이번 세션 자동화 환경 한계** : `Issue_CharacterSelect.md`와 동일하게 Editor 창이 OS 포커스를 안정적으로
  못 받아 Play 모드 스크린샷이 대부분 빈 화면으로 나왔다. `execute_code`로 실제 로직(패널 활성화/
  `Time.timeScale` 변화/닫기 버튼 클릭)은 전부 검증했지만, 텍스트가 실제로 채워진 최종 화면을 스크린샷으로는
  못 봤다 — Edit 모드에서 `UnityEditor.SceneView.RepaintAll()`을 강제로 호출해 플레이스홀더 텍스트("이벤트
  제목" 등) 상태의 레이아웃만 스크린샷으로 확인했다.
- **닫기 버튼 색** : 목업 원본은 흰색 계열 X 아이콘인데, 이 패널의 밝은 배경(연두/연분홍)에서는 흰색이 잘 안
  보여서 진한 회색으로 틴트해 대체했다. 사용자 확인 후 원하는 색으로 조정 가능.
- **`EventCardView` 재사용의 부작용 없음 확인** : `EventLogPanelUI`(커뮤니티 탭 카드 목록)는 `EventEffectFormatter`
  추출 후에도 동일하게 동작한다 — 로직을 그대로 옮긴 것뿐이라 컴파일 확인 외 별도 회귀 테스트는 하지 않았음
  (문구/색 계산 자체가 100% 동일한 코드 이동이라 회귀 위험 낮음).
