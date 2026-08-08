# 이슈 : 엑시트 엔딩 — 스토리 2비트 + 통계 요약 화면 작동 방식 / 호출 스택 정리

작성일 : 2026-08-08

관련 구현 : `Next_Tesk.md` "9. 엑시트 엔딩 — 스토리 + 통계 화면" 항목. 기존 `ExitEndingScene`/`ExitEndingSceneUI`(2026-08-08
생성분)를 새로 만들지 않고 그대로 확장했다.

## 관련 파일

- [ExitEndingSceneUI.cs](ExitEndingSceneUI.cs) — 씬 전체 오케스트레이션(암전 인트로 → 스토리 시퀀스 → 통계 화면
  전환 → 자동 스크롤 → 버튼 활성화). `Main_Canvas`에 부착.
- [StoryDialogueController.cs](StoryDialogueController.cs) — 스토리 비트(배경 스프라이트 + 대사) 목록과 진행 순서만
  관리. `Main_Canvas`에 부착.
- [TypewriterText.cs](TypewriterText.cs) — 한 글자씩 출력하는 범용 타이핑 이펙트. `CaptionText`에 부착, 재사용 가능한
  독립 컴포넌트.
- [GameStatsTracker.cs](GameStatsTracker.cs) — 통계 6종 집계 싱글턴(`Assets/Scripts/DataScience/`). `Managers`
  오브젝트(SampleScene)에 부착.
- [PlayerManager.cs](PlayerManager.cs) `AddCoin()` — 코인 보유량 변경 유일 지점, 여기서 `GameStatsTracker.NotifyCoinsChanged`
  호출.
- [MarketManager.cs](MarketManager.cs) `EndGame()` — 엔딩 확정 순간 `GameStatsTracker.CaptureExitCash` 호출.
- [EventHub.cs](EventHub.cs) — `OnBuyCoin`/`OnSellCoin`/`OnManipulateSupply`/`OnSkillPurchaseSucceeded`를
  `GameStatsTracker`가 직접 구독.

## 작동 방식

**경계 원칙** : `StoryDialogueController`/`TypewriterText`는 서로의 존재를 모른다 — `StoryDialogueController`가
`TypewriterText.Play(line)`을 호출하고 `OnTypingComplete`만 구독하는 식으로 느슨하게 결합했다. 둘 다 재사용
가능한 범용 컴포넌트로 설계했고, 씬 전환/버튼 활성화 같은 이 씬 전용 로직은 전부 `ExitEndingSceneUI`가 갖는다.

**통계 집계** : `GameStatsTracker`는 기존 `PlayerManager`/`SkillManager` 싱글턴과 동일한 `DontDestroyOnLoad`
패턴이다. 매수/매도/발행조작/스킬구매 4종은 `EventHub` 이벤트를 직접 구독해서 카운트만 올리고(발행조작은
`MintButtonUI`가 스킬 미해금 시 버튼 자체를 잠가서 이벤트가 항상 유효한 조작으로 이어짐), 최대 보유 코인은
`PlayerManager.AddCoin()`이 유일한 변경 지점이라 거기서 훅으로 최고값을 갱신한다. 엑시트 현금은
`MarketManager.EndGame()`(엔딩 확정 순간)에 캡처.

**통계 화면 스크롤** : 콘텐츠 높이를 직접 재는 대신 `ScrollRect.verticalNormalizedPosition`을 `scrollDuration`
동안 1→0으로 선형 보간하는 방식을 썼다(콘텐츠 길이가 바뀌어도 코드 수정 불필요). `ScrollRect.horizontal`/`vertical`을
모두 false로 꺼서 스크립트 외의 드래그 조작이 아예 불가능하게 막았다 — "스크롤이 끝나야 조작 가능"이라는 요구사항을
버튼 `interactable` 하나로만 게이트하면 되게 하기 위함. `-End-`/`-타이틀로 돌아가기-` 버튼은 별도 트리거가 아니라
`Content`(VerticalLayoutGroup)의 마지막 자식으로 배치했다 — 스크롤이 그 위치까지 오면 자연히 화면에 들어온다.

**Time.timeScale 0 대응** : 이 씬은 `MarketManager.EndGame()`이 이미 `TimeManager.PauseGame()`을 호출한 채로
진입하므로(`EndingSceneUI`와 동일한 이유), 타이핑/암전/스크롤 코루틴 전부 `Time.unscaledDeltaTime`/
`WaitForSecondsRealtime`을 쓴다.

## 호출 스택

### 1) 씬 진입 → 스토리 비트 진행

```
ExitEndingSceneUI.Start()                              [ExitEndingSceneUI.cs:33]
 ├─ statsPanel.SetActive(false)
 ├─ backToTitleButton.interactable = false               (스크롤 끝나기 전까지 조작 불가)
 ├─ storyController.OnSequenceComplete += ShowStatsScreen
 ├─ storyController.Begin()                              [StoryDialogueController.cs:35]
 │   └─ ShowNext()                                        [:52]
 │       ├─ bgImage.sprite = beats[0].background
 │       └─ typewriter.Play(beats[0].line)                [TypewriterText.cs:23]
 │           └─ TypeRoutine() 코루틴, 1글자씩 label.text에 append
 └─ StartCoroutine(PlaySequence())                        → 암전 페이드(연출용, 스토리 진행과 병렬)

[ContinueButton 클릭] (사용자 입력, 또는 execute_code로 onClick.Invoke() 직접 호출해 테스트함)
 └─ StoryDialogueController.OnContinueClicked()           [:44]
     ├─ typewriter.IsTyping == true  → CompleteImmediately() (타이핑 중이면 전체 텍스트 즉시 표시만)
     └─ typewriter.IsTyping == false → ShowNext()           (다음 비트로, 마지막 비트 다음엔 OnSequenceComplete)
```

### 2) 스토리 종료 → 통계 화면 → 스크롤 → 버튼 활성화

```
StoryDialogueController.OnSequenceComplete
 └─ ExitEndingSceneUI.ShowStatsScreen()                   [:83]
     ├─ statsPanel.SetActive(true)
     ├─ GameStatsTracker.Instance 6개 필드 → 6개 TMP 텍스트에 "라벨\n값" 형식으로 채움
     └─ StartCoroutine(AutoScrollStats())                  [:103]
         ├─ statsScrollRect.verticalNormalizedPosition = 1f → 0f (scrollDuration초 동안 선형 보간)
         └─ 완료 후 backToTitleButton.interactable = true + HoverIdleBob 부착
```

### 3) 통계 카운터 갱신 (게임 진행 중, 메인 씬에서)

```
EventHub.RaiseBuyCoin(amount)          → GameStatsTracker.HandleBuyCoin           → BuyCount++
EventHub.RaiseSellCoin(amount)         → GameStatsTracker.HandleSellCoin          → SellCount++
EventHub.RaiseManipulateSupply(amount) → GameStatsTracker.HandleManipulateSupply  → SupplyManipulateCount++
EventHub.RaiseSkillPurchaseSucceeded() → GameStatsTracker.HandleSkillPurchaseSucceeded → SkillPurchaseCount++
PlayerManager.AddCoin(amount)          → GameStatsTracker.NotifyCoinsChanged(currentCoins) → MaxCoins 갱신(최고값만)
MarketManager.EndGame(ending)          → GameStatsTracker.CaptureExitCash(currentMoney)    → ExitCash 캡처
```

## 알려진 이슈 / 주의점 후보

- 기존 코드는 영웅/엑시트 엔딩마다 다른 문구를 썼는데(`descriptionText`), 이번에 2비트 대사를 두 엔딩 공용
  고정 문구로 바꾸면서 그 구분이 사라졌다 — 사용자 확인 후 그대로 진행하기로 함. 나중에 Hero/Exit별로 다시
  나누고 싶으면 `StoryDialogueController.beats`를 `EndingHandoff.Ending` 기준으로 다르게 채우면 된다.
- `GameStatsTracker.Instance`가 null이면(예: `ExitEndingScene`을 메인 게임 플로우 없이 단독으로 열었을 때)
  통계 6개 텍스트가 디자인 타임 placeholder("0 원" 등) 그대로 남는다 — 실제 플레이 경로에서는 `Managers`가
  항상 먼저 존재하므로 문제없음, 씬 단독 테스트 시에만 해당.
- 통계 6종은 이번 세션에 처음 집계를 시작했기 때문에, 실제로 한 판 끝까지 플레이하며 표시값이 맞는지는
  아직 검증되지 않았다(Play 모드에서 흐름 자체는 정상 동작 확인함, `GameStatsTracker.Instance`가 없는
  단독 씬 상태로만 스크린샷 테스트함).
- `ContinueButton`(대사넘김버튼) 최초 배치 시 110×128px로 잡았다가 화면에서 너무 크게/밖으로 튀어나와 보인다는
  피드백을 받아 55×64px로 축소 + 앵커를 (0.92, 0.08)→(0.90, 0.10)으로 안쪽으로 조정함(2026-08-08). 다른
  해상도/종횡비에서 다시 확인 필요할 수 있음.
