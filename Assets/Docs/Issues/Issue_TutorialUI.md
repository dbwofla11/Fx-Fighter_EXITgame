# 이슈 : 튜토리얼 UI 작동 방식 정리

작성일 : 2026-08-11

관련 구현 : `Next_Tesk.md` 15번 항목("튜토리얼 작업" — 17번 "엑시트 5억 목표 이유 설명"도 여기서 같이 처리),
`Completed_Tasks.md` 해당 항목 참고. 21번(오프닝 대화 UI, "왜 코인 사기를 하게 됐는가")과는 목적이 달라
완전히 별개로 진행했다 — 15번은 "게임을 어떻게 플레이하는가"를 다루는 조작법 튜토리얼.

## 관련 파일

- [TutorialUI.cs](../../Scripts/UI/Tutorial/TutorialUI.cs) — 신규. 딤 패널+말풍선 배열을 순서대로 켜고
  끄는 컨트롤러. `Main_Canvas/TutorialPanel`에 부착.
- [ModalPause.cs](../../Scripts/UI/Utils/ModalPause.cs) — 기존. `TutorialUI.ShowTutorial()`/
  `CloseTutorial()`이 여기의 `Open()`/`Close()`를 그대로 호출한다.
- [EventHub.cs](../../Scripts/manager/utils/EventHub.cs) — `RaiseGamePaused()`/`RaiseGameResumed()`.
- [TimeManager.cs](../../Scripts/manager/TimeManager.cs) — `isManuallyPaused`(스페이스바/P 수동정지 전용)와
  모달 정지(`HandleModalPaused`, `EventHub.OnGamePaused` 구독)가 분리돼 있다는 근거 코드.
- [EventLogButton.cs](../../Scripts/UI/Utils/EventLogButton.cs) — `TutorialUI.Start()`의 클릭 배선
  (`GetComponent<Button>().onClick.AddListener(...)`)이 그대로 따온 기존 패턴.
- [EventOverviewUI.cs](../../Scripts/UI/EventModal/EventOverviewUI.cs) — 17번(목표금액 설명)이 별도 문구
  없이 여기로 커버된다는 근거(`targetValueText`/`currentValueText`/`remainingValueText`, 27~30행).
- 씬 `Assets/Scenes/SampleScene.unity` — `Main_Canvas/TutorialPanel`(`DimBackground` +
  `SpeechBubble1`/`SpeechBubble2`/`SpeechBubble3`) 신규 배치. 대상 UI는 `Main_Canvas/EventLogBtn`,
  `Main_Canvas/DoubtScorePanel`, `Main_Canvas/SkillBtn`(전부 기존 오브젝트, 좌표만 참조).

## 작동 방식

**노출 시점/UI 형태를 정한 근거** : Notion "튜토리얼UI" 문서가 하이어라키를 `MainCanvas → TutorialPanel
(DimBackground + SpeechBubble_1, 2...)`로 명시하고 있어 전용 씬이 아니라 `Main_Canvas` 오버레이로
확정했다(기존 `TradeModalUI`/`CoinControlModalUI`와 같은 모달 계열). 노출 대상이 차트/발행 버튼/매수매도/
스탯/이벤트로그/EXIT처럼 전부 `SampleScene`(메인 게임) UI라서 타이틀/캐릭터선택 직후가 아니라 메인 게임
진입 직후·플레이 시작 전으로 정했고, 이후 사용자가 "직업선택 다음 메인 UI 들어가기 전에 알려주고 시작하는
게 맞다"고 직접 확정했다. 스킵 버튼은 문서/스크립트 어디에도 없어 넣지 않았다 — 끝까지 눌러야 종료.

**말풍선 3개로 확정된 경위** : 처음엔 Notion 문서의 주제 목록(개요/이벤트로그/EXIT/의심도100/스킬구매/
매수매도/발행량/스탯설명)이 전부 이번 스코프인 줄 알았으나, 사용자가 실제 인게임 스크린샷 3장(개요·
이벤트로그·EXIT 통합, 의심도100 게임오버, 스킬구매버튼)을 필수로 못박고 "매수/매도·발행량·스탯 슬라이더
설명은 따로 만들 것"이라 확정하면서 3개로 좁혀졌다. 목업 근거였던 "엔딩은 그렇다 쳐도 오프닝과 튜토리얼
어찌할까?" 문서는 이 자리에서 "옛날 문서"로 폐기 확인받았다.

**17번(목표금액 설명)이 왜 추가 구현 없이 끝났는가** : 2차 피드백 문서에 "엑시트 5억 이유는 튜토리얼과
스토리로 커버, 개요에 목표금액 설명 넣기"라고 적혀 있었는데, `EventOverviewUI`(개요 탭 콘텐츠)를 다시
확인하니 `targetValueText`/`currentValueText`/`remainingValueText`로 목표·현재·남은 금액을 이미 전부
표시하고 있었다(`Completed_Tasks.md` "개요 탭 콘텐츠" 항목, 2026-08-01 완료분). 즉 말풍선 1번이 플레이어를
그 "개요" 화면으로 안내하기만 하면 목표금액은 플레이어가 직접 보게 되므로 별도 문구 추가가 불필요했다.

**pause 방식을 초안에서 바꾼 이유** : 사용자가 준 초안 스크립트는 `TimeManager.Instance.TogglePause()`를
직접 불러 `isManuallyPaused` 플래그를 썼다. `TimeManager.cs` 13행 주석("정지 버튼으로 직접 멈춘 상태인지
(모달 열고 닫는 것과 구분하기 위함)")과 47~63행(`EventHub.OnGamePaused → HandleModalPaused`, `Time.
timeScale`만 건드리고 `isManuallyPaused`는 안 건드림)을 보면 이 프로젝트는 "수동 정지"와 "모달 정지"를
의도적으로 분리해뒀다 — 초안은 튜토리얼을 모달이 아니라 수동 정지로 취급하는 셈이라 `Assets/Docs/
CLAUDE.md`의 "UI는 EventHub.Raise*()만 호출" 경계와도 충돌했다. 사용자에게 확인 후 기존 6개 모달과 동일한
`ModalPause.Open()/Close()` 경로로 교체했다(`TutorialUI.cs` 20행/51행) — 스크립트의 나머지 로직(말풍선
인덱스 순회, 클릭 시 다음으로)은 초안 그대로 유지했다.

## 호출 스택

```
TutorialUI.Start()                                        [TutorialUI.cs:12]
 ├─ GetComponent<Button>().onClick.AddListener(OnClickNext)  [:14]  (EventLogButton.cs와 동일 패턴)
 └─ ShowTutorial()                                          [:18]
     ├─ ModalPause.Open(tutorialPanel)                      [:20]
     │   ├─ EventHub.RaiseGamePaused()  → TimeManager.HandleModalPaused() → Time.timeScale = 0
     │   └─ tutorialPanel.SetActive(true)
     └─ UpdateBubbles()                                     [:40]  (index 0만 활성화)

(TutorialPanel 클릭) → Button.onClick → OnClickNext()        [:25]
 ├─ currentBubbleIndex++
 ├─ 마지막 말풍선이었으면 → CloseTutorial()                  [:49]
 │   └─ ModalPause.Close(tutorialPanel)                      [:51]
 │       ├─ EventHub.RaiseGameResumed() → TimeManager.ResumeGame()
 │       └─ tutorialPanel.SetActive(false)
 └─ 아니면 → UpdateBubbles()                                 [:40]  (다음 index만 활성화)
```

`TutorialUI`는 `TutorialPanel` 자기 자신에 부착돼 있어, `TutorialPanel`이 씬에 비활성 상태로 저장되면
`Start()`가 아예 안 불려 튜토리얼이 영영 안 뜬다 — 씬 저장 시 `TutorialPanel.activeSelf`는 반드시 `true`
여야 한다(현재 그렇게 저장돼 있음).

## Unity 측 작업 (Unity Editor MCP로 처리)

1. `create_folder` + `write_text_file`로 `TutorialUI.cs` 작성 → 자동 컴파일, 에러 없음 확인.
2. `create_gameobjects`로 `Main_Canvas` 밑에 `TutorialPanel`(Image+Button, 전체 스트레치) →
   `DimBackground`(Image, 검정 alpha 0.647, 전체 스트레치) → `SpeechBubble1/2/3`(Image+자식 `Text`
   TextMeshProUGUI) 순서로 생성.
3. `EventLogBtn`/`DoubtScorePanel`/`SkillBtn`의 `RectTransform`(`get_component_properties`)을 먼저 읽어
   각 말풍선의 `anchoredPosition`을 그 옆으로 계산해 배치(예: `EventLogBtn`이 `(430, 470)`이면 말풍선은
   `(0, 330)`처럼 화면 안쪽으로 오프셋).
4. `set_serialized_field`로 `TutorialUI.tutorialPanel`/`speechBubbles[0..2]`를 각 오브젝트에 연결.
5. `save_scene`.

Play 모드로 진입해 콘솔에 `TutorialUI`/`ModalPause`/`EventHub` 관련 새 에러가 없는 것까지는 확인했다.
단, `SampleScene`을 Title→CharacterSelect 정상 플로우 없이 단독으로 Play 진입시키면 `PlayerManager`/
`MarketManager`가 초기화되지 않아 HUD 자체가 아무것도 안 그려지는 상태였다(`TutorialPanel`을 껐다 켜도
스크린샷상 차이 없음 — 이번 작업과 무관한 기존 동작임을 대조 확인). 그래서 실제 배치/가독성/클릭 흐름은
정상 플로우로 사용자가 직접 확인해야 한다.

## 알려진 이슈 / 주의점 후보

- 말풍선 배경이 둥근 테두리+꼬리가 있는 목업과 달리 각진 회색 박스 플레이스홀더다(프로젝트에 말풍선
  스프라이트 에셋이 아직 없음). 사용자가 "꼬리는 없어도 됨"이라고 확인해 지금 상태 그대로 유지하기로
  했다 — 나중에 스프라이트가 생기면 `Image.sprite`만 채우면 된다.
- `Main_Canvas`(스케일 0.634) 밑에 새 `RectTransform`을 `add_component`로 추가할 때마다 로컬 스케일이
  1.577로 자동 보정되는 MCP 동작이 있었다 — 기존 형제 오브젝트는 전부 `[1,1,1]`이라 매번 `set_transform`
  으로 되돌렸다. 이 씬에 새 UI 오브젝트를 MCP로 추가할 때 계속 나올 수 있는 동작이니 유의할 것.
- 매수/매도·발행량·스탯 슬라이더 설명 말풍선은 사용자가 별도로 만들기로 확정했으므로, 이 문서의 3개
  말풍선 구조에 새 말풍선을 추가하는 방식으로 이어가면 된다(`speechBubbles` 배열에 순서대로 추가 후
  `Array.size` 갱신).
- 정상 플로우(Title부터 시작)로 실제 화면에서 말풍선 위치가 대상 아이콘과 겹치거나 벗어나지 않는지,
  텍스트 줄바꿈이 박스 안에 잘 맞는지는 아직 육안 확인 전이다.
