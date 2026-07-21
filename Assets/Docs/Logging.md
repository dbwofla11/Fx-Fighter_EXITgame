# 작업 로그

## EventHub 도입

UI와 게임 로직을 분리하기 위해 `EventHub`를 추가했다.

### 구조

```
UI
 ↓
EventHub
 ↓
Manager
 ↓
RuntimeData
 ↓
Calculator
 ↓
EventHub
 ↓
UI
```

`EventHub`는 `Assets/Scripts/manager/EventHub.cs`에 위치한 정적(static) 클래스이다.
Manager처럼 씬에 존재하는 오브젝트가 아니라, C# static event만으로 동작한다.

- UI는 `EventHub.Raise*()` 메서드만 호출한다.
- Manager는 `OnEnable`/`OnDisable`에서 `EventHub` 이벤트를 구독/해제한다.
- Manager끼리는 기존처럼 직접 호출할 수 있다 (예: `MarketManager` → `StatCalculator`/`ProbabilityCalculator`/`PriceCalculator`).

### 이벤트 목록

| 이벤트 | 시그니처 | 발행 | 구독 |
|---|---|---|---|
| `OnDayChanged` | `Action` | `TimeManager` (하루가 바뀔 때) | `MarketManager.NextTurn` |
| `OnSkillClicked` | `Action<SkillID>` | UI (예정) | `SkillManager` — 활성화 상태를 토글하여 `EnableSkill`/`DisableSkill` 호출 |
| `OnJobSelected` | `Action<JobSO>` | UI (예정) | `JobManager.SelectJob` |
| `OnBuyCoin` | `Action<long>` | UI (예정) | `PlayerManager` — `AddCoin(amount)` |
| `OnSellCoin` | `Action<long>` | UI (예정) | `PlayerManager` — `AddCoin(-amount)` |
| `OnNewsEvent` | `Action` | UI/시스템 (예정) | `MarketManager` — `EventCalculator.Calculate()` 호출 |
| `OnMarketUpdated` | `Action<PlayerStat>` | `MarketManager.NextTurn` (계산 완료 후) | UI (예정) |

`OnDayChanged`는 기존에 `TimeManager`가 직접 소유하던 static event였으나, 이번 작업으로 `EventHub`로 이전했다.
`TimeManager`는 더 이상 이벤트를 직접 들고 있지 않고 `EventHub.RaiseDayChanged()`만 호출한다.

### 계산 파이프라인

기존 계산 파이프라인은 그대로 유지된다.

```
EventHub.OnDayChanged
 ↓
MarketManager.NextTurn()
 ↓
StatCalculator.Calculate()
 ↓
ProbabilityCalculator.Calculate()
 ↓
PriceCalculator.Calculate()
 ↓
PlayerStat 갱신
 ↓
EventHub.RaiseMarketUpdated(CurrentStat)
```

### 현재 상태 (미완료 부분)

- `OnSkillClicked`, `OnJobSelected`, `OnBuyCoin`, `OnSellCoin`, `OnNewsEvent`, `OnMarketUpdated`를 발행하거나 구독하는 실제 UI(스킬/직업/거래/뉴스 화면)는 아직 존재하지 않는다. 이번 작업에서는 Manager 쪽 구독 로직만 연결했다.
- `TimeUI`, `PlayerUI`, `SettingsUI`는 기존처럼 `TimeManager`/`PlayerManager`를 직접 참조한다. 이 UI들이 사용하는 기능(배속 조절, 일시정지, 자산 폴링)은 `Next_Tesk.md`에 정의된 이벤트 목록에 포함되어 있지 않아 이번 범위에서 변경하지 않았다.
- `EventCalculator`, `TradeCalculator`의 내부 계산 로직은 여전히 빈 스텁이다. `OnNewsEvent`는 `EventCalculator.Calculate()`를 호출하도록 연결만 해두었다.

## 현재 아키텍처 요약

```
Manager
 ↓
RuntimeData
 ↓
Systems (Calculator)
```

- **Manager**: TimeManager, MarketManager, SkillManager, JobManager, PlayerManager — 게임 상태를 관리하며 `EventHub`를 구독한다.
- **RuntimeData**: PlayerStat, RuntimeSkillData, RuntimeJobData — 현재 상태와 계산 결과를 저장한다.
- **Systems**: StatCalculator, ProbabilityCalculator, PriceCalculator — 상태를 변경하지 않고 계산만 수행한다.
- **EventHub**: UI ↔ Manager 사이의 이벤트를 중계한다. (신규)
