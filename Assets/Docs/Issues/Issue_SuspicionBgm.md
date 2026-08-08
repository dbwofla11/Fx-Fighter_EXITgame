# 이슈 : 의심도(Doubt) 3단계 BGM 전환

작성일 : 2026-08-09

## 배경

메인 화면 BGM을 의심도(`PlayerStat.Doubt`, 0~100) 구간에 따라 3단계로 바꿔달라는 요청. 기존에는
`GameStarter.Start()`가 씬 시작 시 `happy-tropical.wav` 한 곡만 고정 재생하고 있었다.

## 관련 파일

- [SuspicionBgmController.cs](../../Scripts/manager/utils/SuspicionBgmController.cs) — 신규 컴포넌트.
- [GameStart.cs](../../Scripts/Stat/GameStart.cs) — `Start()`의 `PlayBGM` 직접 호출 제거.
- `Assets/Scenes/SampleScene.unity` — `/GameStarter` 오브젝트에 `SuspicionBgmController` 추가, 클립 2개 연결.

## 구간 및 클립

| Doubt 범위 | BGM |
|---|---|
| ~55 미만 | `Assets/Audio/bgm/Chiptuna_Sandwich.wav` |
| 55 이상 ~ 60 미만 | 브금 없음 (`StopBGM()`) |
| 60 이상 | `Assets/Audio/bgm/leberch-suspense-511168.mp3` |

## 작동 방식

`StatGaugeUI.cs`와 동일한 구독 패턴을 그대로 따랐다 : `OnEnable`에서 `EventHub.OnMarketUpdated` 구독 +
`MarketManager.Instance.CurrentStat`으로 초기값을 한 번 pull, `OnDisable`에서 해제.

```csharp
private enum Tier { Low, Mid, High }
private const float MidThreshold = 55f;
private const float HighThreshold = 60f;
private Tier? currentTier;

private void HandleMarketUpdated(PlayerStat stat)
{
    Tier tier = stat.Doubt >= HighThreshold ? Tier.High
        : stat.Doubt >= MidThreshold ? Tier.Mid
        : Tier.Low;

    if (tier == currentTier) return;
    currentTier = tier;
    // tier별로 PlayBGM(lowDoubtClip) / StopBGM() / PlayBGM(highDoubtClip)
}
```

매턴 `OnMarketUpdated`가 발행되지만 `currentTier` 캐싱으로 실제 티어가 바뀔 때만 `PlayBGM`/`StopBGM`을
호출한다 — 안 그러면 같은 구간 안에서도 매턴 크로스페이드가 겹쳐 이상해짐. 경계값(55, 60)은 상수로 빼서
나중에 조정 가능.

**GameStart.cs 충돌 처리** : `GameStarter.Start()`가 `happy-tropical.wav`를 직접 재생하던 걸 그대로 뒀다면
씬 시작 직후 `SuspicionBgmController`의 초기값 pull과 크로스페이드가 이중으로 겹쳤을 것 — 해당 호출을
제거하고, 새 컨트롤러의 초기값 pull(Low 티어 → `Chiptuna_Sandwich.wav`)이 첫 BGM 재생을 담당하도록
바꿨다. `happy-tropical.wav` 파일 자체는 그대로 두고 코드에서만 참조를 뗐다.

## 검증

Unity Editor MCP로 `SampleScene`을 열어 `/GameStarter`에 컴포넌트를 붙이고 클립을 연결한 뒤 저장,
콘솔에 컴파일 에러/경고가 없는 것까지 확인했다. **Play 모드로 실제 턴을 진행시켜 55/60 경계에서 소리가
자연스럽게 전환되는지(체감 타이밍/볼륨)는 아직 확인 안 함** — 사용자가 직접 플레이하며 들어봐야 함.
