# 이슈 : 사운드 4종 추가 + 의심도 자동 상승 가속화

작성일 : 2026-08-08

관련 구현 : `Completed_Tasks.md` "사운드 4종 추가", "의심도(Doubt) 자동 상승 가속화" 항목.

## 1. 사운드 4종 추가

사용자가 화면/액션별로 미리 정해둔 목록 4건을 연결. 파일은 전부 기존 `Assets/Audio/`에 있던 것(신규 에셋
추가 없음).

### 관련 파일

- [TypewriterText.cs](../../Scripts/UI/Utils/TypewriterText.cs) — `typeSfx` 필드 추가(기존 파일).
- [EventNotificationUI.cs](../../Scripts/UI/EventModal/EventNotificationUI.cs) — `openSfx` 필드 추가(기존
  파일, `closeSfx`는 이미 있었음).
- [EndingSceneUI.cs](../../Scripts/UI/EndingScene/EndingSceneUI.cs) — 코드 변경 없음, 씬 인스펙터 참조만 교체.
- [ExitEndingSceneUI.cs](../../Scripts/UI/EndingScene/ExitEndingSceneUI.cs) — `bgmClip` 필드 하나를
  `heroBgmClip`/`exitBgmClip` 둘로 분리(기존 파일).
- `Assets/Scenes/SampleScene.unity` / `EndingScene.unity` / `ExitEndingScene.unity` — 아래 표의 인스펙터
  참조 4건 연결.

### 연결 내역

| # | 트리거 | 클립 | 위치 |
|---|---|---|---|
| 1 | 엔딩씬 대사 타이핑 중 글자 하나 출력마다(공백 제외) | `Assets/Audio/EffectSFX/typing.mp3` | `TypewriterText.typeSfx` (`ExitEndingScene/Main_Canvas/CaptionText`) |
| 2 | 시사 이벤트 알림 패널이 뜨는 시점 | `Assets/Audio/EffectSFX/event_alarm_v2.mp3` | `EventNotificationUI.openSfx` (`SampleScene/Main_Canvas/EventNotification`) |
| 3 | 상장폐지(거지) 엔딩 브금 | `Assets/Audio/bgm/failure_sound.wav` | `EndingSceneUI.delistingBgmClip` (`EndingScene/Main_Canvas`) |
| 4 | 엑시트 엔딩 브금 | `Assets/Audio/bgm/true_ending_tension1.mp3` | `ExitEndingSceneUI.exitBgmClip` (`ExitEndingScene/Main_Canvas`) |

### 작동 방식

**타이핑음** : `TypewriterText.Play()`가 `TypeRoutine()` 코루틴에서 한 글자씩 `label.text`에 append하는데,
그 루프 안에서 공백이 아닌 글자마다 `AudioManager.Instance.PlaySFX(typeSfx)`를 같이 호출한다.
`AudioSource.PlayOneShot`은 겹쳐 재생돼도 끊기지 않으므로 `charInterval`(0.04초)마다 계속 울려도 문제없다.
`TypewriterText`는 범용 컴포넌트라 필드는 컴포넌트 자체에 두었고, 사용처는 현재 `StoryDialogueController`
(엑시트/영웅 엔딩 스토리 대사) 하나뿐이다.

**이벤트 알림음** : `EventNotificationUI.HandleEventTriggered()`가 카드 내용을 채우고 `ModalPause.Open(panel)`을
부르기 직전에 `AudioManager.Instance.PlaySFX(openSfx)`를 추가했다. 기존 `Close()`의 `closeSfx` 재생과 대칭
구조.

**상장폐지 브금 교체 발견** : 인스펙터를 열어보니 `EndingSceneUI.delistingBgmClip`이 원래
`police-siren.wav`(체포 엔딩 `arrestBgmClip`과 동일한 파일)를 가리키고 있었다 — 즉 이전까지 상장폐지
엔딩에서도 사이렌 소리가 나고 있었던 상태. `failure_sound.wav`로 교체했고, 체포 엔딩 쪽은 손대지 않았다.

**엑시트/영웅 BGM 분리 필요했던 이유** : `ExitEndingSceneUI`는 `EndingResultUI`가 영웅/엑시트 두 엔딩을
모두 이 씬(`ExitEndingScene`)으로 보내는 구조라(`Issue_ExitEndingStoryStats.md` 참고) 원래 `bgmClip`
필드 하나를 공유했다. "엑시트 엔딩에서만" 재생하라는 요청을 그대로 지키려면 분기가 필요해서,
`EndingSceneUI`가 이미 쓰던 "`EndingType`별 필드 두 개, 없으면 무음" 패턴을 그대로 가져왔다:

```csharp
AudioClip bgmClip = EndingHandoff.Ending == EndingType.Hero ? heroBgmClip : exitBgmClip;
```

`heroBgmClip`은 비워뒀다 — 영웅 엔딩은 기존처럼 브금 없이 진행된다(사용자가 영웅 엔딩 사운드는 요청하지
않음).

### 검증

Unity MCP Play 모드로 `SampleScene`/`EndingScene`/`ExitEndingScene` 3개 씬 모두 콘솔 에러 없이 로드되는
것을 확인했고, `SampleScene`에서 `execute_code`로 `EventHub.RaiseEventTriggered(...)`를 직접 호출해
`HandleEventTriggered` → `PlaySFX(openSfx)` 경로가 예외 없이 도는 것까지 확인했다. **실제 소리가 원하는
느낌으로 나는지(볼륨/타이밍 체감)는 에디터 스피커로 직접 들어봐야 함** — 이 세션에서는 청취 확인을 하지
않았다.

## 2. 의심도(Doubt) 자동 상승 가속화

### 배경

직전 세션(2026-08-08 앞부분)에 스킬 `costMultiplier`/로비 가격/구매 횟수 상한 등으로 밸런스 패치를 했는데,
"그래도 의심도 조절이 쉽다"는 재피드백을 받음. 원인 : `MarketManager.ApplyDoubtAutoRise()`가 2년(730턴)째
이후 매 턴 **고정** `+0.1`만 더해서, 연차가 아무리 지나도 상승 "속도"가 빨라지지 않았음(1년치 누적량이
항상 36.5로 동일).

### 관련 파일

- [MarketManager.cs](../../Scripts/manager/MarketManager.cs) — `DoubtAutoRiseAccelPerYear`/`TurnsPerYear`
  상수 추가, `ApplyDoubtAutoRise()` 수정.

### 작동 방식

턴당 증가량을 고정값 대신 "경과 연차에 비례하는 값"으로 바꿨다. 지수 증가가 아니라 **선형** 가속을
원한다는 요청이라, 증가량 자체가 연차에 비례해서 커지되 그 비례 기울기(가속도)는 고정이다.

```csharp
private const int DoubtAutoRiseStartTurn = 730;
private const float DoubtAutoRiseInitialAmount = 4f;   // 2년째 진입 시 1회
private const float DoubtAutoRisePerTurn = 0.1f;        // 기본 턴당 증가량 (기존과 동일)
private const float DoubtAutoRiseAccelPerYear = 0.05f;  // 신규 : 연차당 증가량 가속
private const float TurnsPerYear = 365f;

private void ApplyDoubtAutoRise()
{
    if (turnCount == DoubtAutoRiseStartTurn)
    {
        CurrentStat.Doubt += DoubtAutoRiseInitialAmount;
    }
    else if (turnCount > DoubtAutoRiseStartTurn)
    {
        float yearsElapsed = (turnCount - DoubtAutoRiseStartTurn) / TurnsPerYear;
        CurrentStat.Doubt += DoubtAutoRisePerTurn + DoubtAutoRiseAccelPerYear * yearsElapsed;
    }
}
```

`DoubtAutoRiseAccelPerYear = 0.05`는 사용자가 세션 중 즉석에서 확정한 값(추후 실제 플레이 체감으로
재조정 가능 — `Next_Tesk.md` "밸런스 수치 조정" 후보에 포함).

### 결과 곡선 (턴당 증가량)

| 연차 | 턴당 증가량 |
|---|---|
| 2년째 진입 순간 | +4 (1회성) |
| 2~3년차 | 0.10 → 0.15 |
| 4~5년차 | 0.20 → 0.25 |
| 6~7년차 | 0.30 → 0.35 |

### 검증

Unity MCP Play 모드에서 `execute_code`로 `MarketManager.Instance`의 private `turnCount` 필드와
`ApplyDoubtAutoRise()` 메서드를 리플렉션으로 직접 호출해, turn 730/731/1095/1096/1460/2555에서
`Doubt` 증가량이 각각 4 / 0.100137 / 0.15 / 0.150137 / 0.2 / 0.35로 공식과 정확히 일치하는 것을
확인했다(0.000137 오차는 실행 프레임 간 미세한 `Time` 계산이 아니라 float 부동소수점 오차 수준).
실제 게임 플레이(1~7년 이상 진행)로 체감 난이도까지 확인하는 것은 이 세션 범위 밖 — 사용자가 직접
플레이 후 피드백 필요.
