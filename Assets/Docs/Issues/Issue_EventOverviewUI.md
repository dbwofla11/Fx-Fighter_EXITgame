# 이슈 작성 양식 : 작동 방식 / 호출 스택

기능 하나를 다 구현한 뒤 이슈(버그 리포트, 리뷰 요청 등)를 적을 때 쓰는 양식이다. 아래 틀을 그대로 복사해서
`[ ]` 자리만 채우면 된다. 맨 아래 "작성 예시"는 이 양식으로 실제 작성해 본 사례(개요 탭 스탯개요 + 엑시트
버튼, 2026-08-01)다.

---

## 양식

```markdown
# 이슈 : [기능/화면 이름] 작동 방식 / 호출 스택 정리

작성일 : [YYYY-MM-DD]

관련 구현 : [Next_Tesk.md / Completed_Tasks.md 등 관련 항목 링크나 이름]

## 관련 파일

- [파일명.cs](경로) — [역할 한 줄 요약, 씬에서 어디에 부착돼 있는지]
- ...

## 작동 방식

[UI→EventHub→Manager 같은 경계 원칙, 갱신 타이밍(이벤트 구독인지 진입 시점 1회인지) 등
"왜 이렇게 짰는지"를 아는 사람이 코드를 안 봐도 이해할 수 있게 설명]

## 호출 스택

### [시나리오 1 이름, 예: 패널 열기 → OO 표시]

\`\`\`
[트리거] (파일명.cs)
 └─ 메서드1()                         [파일명.cs:줄번호]
     └─ 메서드2()                      [:줄번호]
         ├─ 부수효과/이벤트 발행
         └─ 메서드3()                   [:줄번호]
\`\`\`

### [시나리오 2 이름]

\`\`\`
...
\`\`\`

## 알려진 이슈 / 주의점 후보

- [엣지 케이스나 잠재 버그, 왜 지금은 문제없는지/언제 깨질 수 있는지]
- ...
```

---

## 작성 예시 : 개요 탭(스탯개요 + 엑시트 버튼)

# 이슈 : 개요 탭(스탯개요 + 엑시트 버튼) 작동 방식 / 호출 스택 정리

작성일 : 2026-08-01

관련 구현 : `Next_Tesk.md` "개요 탭 콘텐츠" 항목, `Completed_Tasks.md` "개요 탭 콘텐츠(스탯개요 + 엑시트 버튼)" 참고.

## 관련 파일

- [EventLogPanelUI.cs](../Scripts/UI/EventLogPanelUI.cs) — 패널 전체(오버레이/탭/닫기) 컨트롤러. 씬의 `EventLogPanel` 루트에 부착.
- [EventOverviewUI.cs](../Scripts/UI/EventOverviewUI.cs) — "개요" 탭 콘텐츠 전용. 씬의 `EventLogPanel/ContentArea/OverviewContent`에 부착.
- [EventHub.cs](../Scripts/manager/EventHub.cs) — UI→Manager 중계 이벤트 허브.
- [MarketManager.cs](../Scripts/manager/MarketManager.cs) — `CanExit`/`CurrentStat`/`TargetAsset`, `OnExitRequested` 구독.

## 작동 방식

**경계 원칙** : `EventOverviewUI`는 화면 갱신을 위해 `MarketManager`/`PlayerManager`/`JobManager`/`SkillManager`를
**직접 읽기만** 하고(쓰기는 없음), 상태를 바꾸는 액션(엑시트 시도)만 `EventHub.RaiseExitRequested()`로 발행한다.
`MarketManager`가 그 이벤트를 구독해서 실제 처리한다 (UI는 EventHub만 호출 / Manager는 EventHub 구독하는 기존
경계 유지).

**갱신 타이밍** : 패널이 열려있는 동안은 `RaiseGamePaused()`로 턴 진행이 멈춰서 `OnMarketUpdated`가 나오지
않는다. 그래서 이벤트 구독 대신, 탭이 선택되는 **진입 시점에 한 번만** `Refresh()`를 호출하는 방식이다
(`EventPanelBox`의 `RefreshLog()`와 동일 패턴).

## 호출 스택

### 1) 패널 열기 → 개요 탭 표시

```
[EventLogButton 클릭] (EventLogButton.cs)
 └─ EventLogPanelUI.Toggle()                         [EventLogPanelUI.cs:49]
     └─ Open()                                        [:55]
         ├─ EventHub.RaiseGamePaused()                → TimeManager 구독자가 시간 정지
         ├─ gameObject.SetActive(true)
         └─ ShowOverview()                             [:71]
             ├─ eventPanelBox.SetActive(false)
             ├─ overviewContent.SetActive(true)
             ├─ overviewUI.Refresh()                    [EventOverviewUI.cs:45]
             │   ├─ MarketManager.Instance.CurrentStat 읽기 → Support/Growth/Doubt/
             │   │   JobSkillSupportBonus/JobSkillGrowthBonus/PositiveEventRate/
             │   │   NegativeEventRate/CashBonus → 9개 TMP 텍스트 갱신
             │   ├─ ComputeDoubtBonus()                 [:79]
             │   │   ├─ JobManager.Instance.CurrentJob.effects 합산 (SumDoubtEffects)
             │   │   └─ SkillManager.Instance.GetActiveSkills() 각 effects 합산
             │   ├─ PlayerManager.Instance.currentMoney / MarketManager.TargetAsset
             │   │   → 목표금액/현재금액/남은금액 텍스트 갱신
             │   └─ exitButton.interactable = MarketManager.Instance.CanExit
             └─ SetTabSelected(overviewTab, communityTab) → 탭 색상 토글
```

### 2) 엑시트 버튼 클릭 (CanExit == true일 때만 클릭 가능)

```
[ExitButton 클릭]
 └─ (EventOverviewUI.Start()에서 등록한 리스너)         [EventOverviewUI.cs:41-42]
     └─ EventHub.RaiseExitRequested()                   [EventHub.cs:75]
         └─ MarketManager.HandleExitRequested()          [MarketManager.cs:221] (OnEnable에서 구독)
             ├─ IsGameOver || currentMoney < TargetAsset → true면 무시하고 종료
             └─ EndGame(EndingCalculator.CheckExit(CurrentStat))  [:230]
                 ├─ IsGameOver = true
                 ├─ TimeManager.Instance.PauseGame()
                 └─ EventHub.RaiseGameEnded(ending)       → 엔딩 화면 구독자 없음(미구현, Next_Tesk.md 항목)
```

> 참고 : 확인 팝업 없이 클릭 즉시 실행되도록 확정한 사양 (2026-08-01 결정).

### 3) 탭 전환 (개요 ↔ 커뮤니티)

```
communityTab 클릭 → ShowCommunity()                     [EventLogPanelUI.cs:79]
 ├─ overviewContent.SetActive(false)
 ├─ eventPanelBox.SetActive(true)
 ├─ SetTabSelected(communityTab, overviewTab)
 └─ RefreshLog()  → MarketManager.EventLog 순회, 카드 Instantiate
```

### 4) 패널 닫기

```
closeBtn 클릭 → Close()                                  [EventLogPanelUI.cs:64]
 ├─ EventHub.RaiseGameResumed() → TimeManager 구독자가 시간 재개
 └─ gameObject.SetActive(false)
```

## 알려진 이슈 / 주의점 후보

- `exitButton.interactable`은 `Refresh()` 호출 시점(탭 진입 시)에만 갱신됨 — 패널이 열려있는 동안
  `currentMoney`가 바뀔 경로가 없어(게임 일시정지 + 다른 모달과 동시 상호작용 불가) 정합성 문제는 없지만,
  향후 이 가정이 깨지면(예 : 패널이 열린 채로 다른 소스가 `currentMoney`를 변경) 버튼 상태가 stale해질 수
  있다.
- `ComputeDoubtBonus()`는 `Refresh()`가 호출될 때마다 매번 O(n) 순회로 재계산 — 스킬/이펙트 개수가 적어
  현재는 성능 문제 없음.
- 엑시트 클릭 시 확인 팝업이 없어 오조작 가능성 있음 (사양으로 확정됨, 재검토 필요 시 참고).
