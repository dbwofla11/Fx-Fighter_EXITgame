# 이슈 : 스트리머 반응 — 말풍선 + 반응 완충용 숨은 지수

작성일 : 2026-08-12

관련 구현 : 기존 스트리머 표정 시스템(`StreamerPanelUI`/`StreamerReactionCalculator`, RightPanel/StreamerPanel)에서
`Completed_Tasks.md`가 다음 세션으로 미뤄뒀던 "말풍선/멘트 텍스트" 범위를 진행. 참고 레퍼런스(스트리머 웹캠 오버레이
스크린샷)를 기준으로 말풍선 UI + 멘트 데이터를 새로 만들고, 겸사겸사 표정이 매 턴 너무 자주 바뀌는 문제를
숨은 지수(StreamerIndex)로 완충하도록 반응 계산 로직도 갈아엎었다.

## 관련 파일

- [PlayerStat.cs](../../Scripts/Stat/PlayerStat.cs) — `StreamerIndex`(0~100, 중립 50) 필드 추가.
- [StatCalculator.cs](../../Scripts/Runtimes/Systems/StatCalculator.cs) `Calculate()` — `StreamerIndex`를
  `CurrentPrice`와 동일하게 매 턴 이월.
- [MarketManager.cs](../../Scripts/manager/MarketManager.cs) `UpdateStreamerReaction()` — 가격 변화량을
  `StreamerIndex`에 완만하게(턴당 최대 ±6) 누적. `Awake()`/`ResetState()`에서 초깃값 50 세팅.
- [StreamerReactionCalculator.cs](../../Scripts/Runtimes/Systems/StreamerReactionCalculator.cs) — 원래
  가격 변화량(delta)에 바로 임계값을 매기던 방식에서, `StreamerIndex`를 20점 단위 5구간으로 나누는 방식으로 교체.
- [StreamerReactionState.cs](../../Scripts/Stat/Enums/StreamerReactionState.cs) — 주석만 `StreamerIndex` 기준으로 갱신.
- [StreamerLines.cs](../../Scripts/Runtimes/Systems/StreamerLines.cs) — 반응 5단계 × 멘트 3개씩, 총 15개 대사.
- [StreamerPanelUI.cs](../../Scripts/UI/MainModal/StreamerPanelUI.cs) — 반응 단계가 바뀌고 쿨타임(30~50턴
  랜덤)이 지났을 때만 말풍선을 띄우도록 트리거.
- [SpeechBubbleUI.cs](../../Scripts/UI/MainModal/SpeechBubbleUI.cs) — `Show(name, message, duration)`/`Hide()`만
  제공하는 순수 UI 컨테이너.
- `Assets/Scenes/SampleScene.unity` — `Main_Canvas/StreamerSpeechBubble` GameObject 신규(배경 Image +
  `NameText`/`MessageText`(TMP) + `Tail`(45도 회전 사각형) + `Nobi.UiRoundedCorners.ImageWithRoundedCorners`
  radius 16, 색상 `#dddddd`).

## 작동 방식

**표정이 너무 자주 바뀌는 문제** : 원래는 이번 턴 가격 변화량(delta)을 바로 ±10/±50 임계값에 매핑해서, 가격이
조금만 흔들려도 매 턴 표정이 튀었다. 지금은 화면에 안 보이는 `StreamerIndex`(0~100)를 따로 두고, 매 턴
`가격변화 × 0.15`를 계산하되 턴당 최대 ±6점만 움직이게 캡을 건 뒤 지수에 누적한다. 이 지수를 20점 구간
(0~20 Crash / 20~40 Down / 40~60 Neutral / 60~80 Up / 80~100 Surge)으로 나눠 `StreamerReaction`을 정한다 —
한 턴 최대 ±6점이라 구간 하나(20점) 넘으려면 같은 방향으로 최소 4턴은 이어져야 한다. `PlayerStat`이 매 턴
`new PlayerStat()` + `Reset()`으로 통째로 새로 생성되는 구조라(`StatCalculator.Calculate()`), `CurrentPrice`와
동일하게 `StreamerIndex`도 명시적으로 이월해줘야 한다는 점을 놓치기 쉽다.

**말풍선 트리거** : `StreamerPanelUI.HandleMarketUpdated()`가 반응 단계가 실제로 바뀐 시점에만
`SpeechBubbleUI.Show()`를 호출한다. 최소 쿨타임(30턴)을 걸고 실제 쿨타임은 매번 30~50 사이 랜덤으로 다시 뽑는다
(`turnsSinceLastBubble`/`bubbleCooldownTarget`). 멘트는 `StreamerLines.GetRandom(state)`가 상태별 3개 중 하나를
무작위로 고른다.

**말풍선 배치/레이어** : 처음엔 `RightPanel` 하위에 뒀는데, `RightPanel`이 `Main_Canvas`의 첫 자식이라 그
안에 뭘 넣어도 `ChartPanel` 등 나중 sibling들한테 항상 가려졌다 — sibling 순서는 부모가 같은 것끼리만 비교되고
하위 트리 전체가 먼저/나중에 그려지는 방식이라, `RightPanel` 밖으로 꺼내야 했다. `Main_Canvas` 바로 밑으로
옮긴 뒤, `EventNotification` 바로 다음 sibling 인덱스로 고정했다(`ChartPanel`/`CoinPriceHeader`보다는 위,
`TradeModal`/`EventLogPanel`/`SkillPanel`/`CoinModalPanel`/`SettingsPanel`/`TutorialPanel` 같은 모달들보다는
아래) — "이벤트 알림이랑 동일한 레이어" 요구사항을 sibling 인덱스로 그대로 구현한 것.

## 호출 스택

```
MarketManager.NextTurn()                                    [MarketManager.cs:137]
 ├─ CurrentStat = StatCalculator.Calculate()                 (StreamerIndex 이월 포함)
 ├─ ...(이벤트/가격 계산)
 └─ UpdateStreamerReaction(priceBefore)                      [:306]
     ├─ priceChange = CurrentPrice - priceBefore
     ├─ StreamerIndex = Clamp(StreamerIndex + Clamp(priceChange*0.15, -6, 6), 0, 100)
     └─ StreamerReaction = StreamerReactionCalculator.Calculate(StreamerIndex)   (20점 구간 매핑)

EventHub.RaiseMarketUpdated(CurrentStat)
 └─ StreamerPanelUI.HandleMarketUpdated(stat)                [StreamerPanelUI.cs:59]
     ├─ streamerImage.sprite = 반응 단계별 스프라이트 교체
     ├─ turnsSinceLastBubble++
     └─ if (반응 단계 변경 && turnsSinceLastBubble >= bubbleCooldownTarget)
         ├─ speechBubble.Show(streamerName, StreamerLines.GetRandom(stat.StreamerReaction))
         ├─ turnsSinceLastBubble = 0
         └─ bubbleCooldownTarget = Random.Range(30, 51)
```

## 알려진 이슈 / 주의점 후보

- 감도(0.15)/턴당 최대폭(±6)/구간 경계(20점 단위)/쿨타임(30~50턴)/말풍선 노출 시간(기본 3초) 전부 플레이테스트로
  조정 필요한 예시치. 실제 한 판 끝까지 돌리며 표정 전환 빈도가 체감상 적당한지 검증 안 됨.
- 멘트 15개는 참고 영상 톤을 흉내 낸 초안 — 워딩 다듬을 여지 있음.
- **캐릭터 모션(말풍선 아님, 캐릭터 자체가 숨쉬거나 눈 깜빡이는 것)은 이번 세션 범위 밖으로 다시 빠짐.**
  처음에 `StreamerPanelUI`에 코드로 상하 바운스 + 스케일 스퀴시 방식 idle 모션을 시도했으나, "패널 전체가
  움직이는 거지 캐릭터 내부가 움직이는 게 아니다"라는 피드백으로 전량 되돌림(`git checkout`). 스트리머 아트가
  단일 평면 PNG 5장뿐이라(부위별 레이어 없음) 진짜 캐릭터 모션을 넣으려면 (a) Live2D/Spine용으로 파츠를 다시
  분리하거나 (b) 이미지→영상 AI 툴(Layer, DomoAI 등)로 외부에서 영상/움짤을 뽑아와 Unity VideoPlayer로 붙이는
  방법 중 하나가 필요 — 사용자가 Seedance 2.0으로 표정 5장을 각각 영상화하는 작업을 별도로 진행 중(프롬프트는
  이번 세션에서 같이 작성). 영상 파일이 나오면 다음 세션에서 붙이는 작업 진행.
- `StreamerSpeechBubble`을 `RightPanel`→`Main_Canvas`로 재부모화할 때 `world_position_stays=true`를 썼더니
  `RightPanel`의 로컬 스케일(1.68배 — 원인 미확인)이 그대로 눌러붙어 실제 렌더 크기가 의도보다 훨씬 컸다.
  로컬 스케일을 (1,1,1)로 리셋하고 `sizeDelta`를 다시 잡아서 해결 — 앞으로 이 계층에서 뭔가를 재부모화할 땐
  스케일도 같이 확인할 것.
- 초기 구현에서 `turnsSinceLastBubble` 시작값을 `int.MaxValue`로 뒀다가 첫 프레임 `++`에서 바로 오버플로해
  음수가 되는 버그가 있었다(말풍선이 영원히 안 뜨는 상태로 이어짐) — `MaxBubbleCooldownTurns`(50)로 시작하도록
  수정해서 해결.
- 이 환경의 Unity MCP 스크린샷 도구(`capture_game_view`/`screenshot`)가 Edit/Play 모드, 씬 상태와 무관하게
  완전히 동일한(MD5 일치) 이미지만 반환하는 버그를 확인함 — 이번 세션 UI 배치/레이어 작업은 전부 사용자가 에디터에서
  직접 확인해준 피드백에 의존해서 진행함, 스크린샷으로 자체 검증 불가능한 상태.
