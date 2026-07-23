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
| `OnBuyCoin` | `Action<long>` | UI (예정) | `PlayerManager` — 현재가 정산 후 `AddMoney`/`AddCoin` · `MarketManager` — `TradeCalculator.Long`로 시장 영향치 누적 |
| `OnSellCoin` | `Action<long>` | UI (예정) | `PlayerManager` — 현재가 정산 후 `AddMoney`/`AddCoin` · `MarketManager` — `TradeCalculator.Short`로 시장 영향치 누적 |
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

- `OnSkillClicked`, `OnJobSelected`, `OnBuyCoin`, `OnSellCoin`, `OnNewsEvent`, `OnMarketUpdated`를 발행하거나 구독하는 실제 UI(스킬/직업/거래/뉴스 화면)는 아직 존재하지 않는다. Manager 쪽 구독 로직만 연결되어 있다.
- `TimeUI`, `PlayerUI`, `SettingsUI`는 기존처럼 `TimeManager`/`PlayerManager`를 직접 참조한다. 이 UI들이 사용하는 기능(배속 조절, 일시정지, 자산 폴링)은 이벤트 목록에 포함되어 있지 않아 변경하지 않았다.
- `EventCalculator`의 내부 계산 로직은 여전히 빈 스텁이다. `OnNewsEvent`는 `EventCalculator.Calculate()`를 호출하도록 연결만 해두었다.

## Long/Short 거래 로직

`Game_Formula.md` 3장 기준으로 `TradeCalculator`를 구현했다. (기존에는 빈 스텁이었다.)

### 버그 수정 : CurrentPrice가 매 턴 초기화되던 문제

`StatCalculator.Calculate()`가 매 턴 새 `PlayerStat` 인스턴스를 만드는데, `CurrentPrice`는 `Reset()` 대상이 아니면서도
새 인스턴스라 기본값 0으로 시작해버려서 `PriceCalculator`가 항상 0을 기준으로 `±delta`만 계산했다.
`Game_Formula.md`의 `Price(t+1) = Price(t) + delta` 공식과 어긋나는 기존 버그였다.

`StatCalculator.Calculate()` 안에서 새 stat을 만든 직후, 재할당 전이라 아직 이전 값을 들고 있는
`MarketManager.Instance.CurrentStat.CurrentPrice`를 새 stat에 이월하도록 수정했다.

### 미완료 부분

- Long/Short UI 버튼은 아직 없다 (`EventHub.RaiseBuyCoin`/`RaiseSellCoin`을 호출할 UI 미구현).
- 발행량(Supply) 조작, 시사 이벤트(`EventCalculator`) 계산 로직은 별도 작업으로 남아있다.

## Support/Growth 감쇠 (Decay) — 최종 구조

거래·직업 선택으로 오른 Support/Growth가 감소 없이 계속 쌓이기만 하면 재미가 없어서, 시간이 지나면
서서히 (마이너스 포함) 0으로 수렴하도록 만들었다. 이 과정에서 설계를 두 번 갈아엎었다.

**시도 1 — 소스별 누적 데이터(`RuntimeTradeData`, `JobBoostData`)를 따로 관리하고 `StatCalculator`가 매 턴 합산.**
동작은 했지만 소스가 늘어날 때마다 누적 클래스와 합산 코드가 계속 늘어나는 구조라 불필요하게 복잡했다.

**시도 2 — Job에는 감쇠를 안 걸고 "Job이 주는 값 밑으로는 안 내려가는" 바닥값 클램프.**
숏(Short)·부정 이벤트·스킬이 Support/Growth를 마이너스로 만들어야 하는 상황과 충돌해서 폐기.

**최종 — `PlayerStat.Support`/`Growth` 자체를 `CurrentPrice`처럼 턴을 넘어 유지되는 값으로 만들고,
Job 선택/거래가 발생하는 그 순간 이 값에 직접 더하거나 빼고, 매 턴 그 값 자체를 한 번 감쇠시킨다.**
별도 누적 데이터 클래스가 필요 없어졌다 (`RuntimeTradeData`, `JobBoostData` 삭제).

```csharp
// StatCalculator.Calculate()
PlayerStat previous = MarketManager.Instance.CurrentStat;
stat.CurrentPrice = previous.CurrentPrice;   // 이월
stat.Support = previous.Support;             // 이월
stat.Growth = previous.Growth;               // 이월
TradeCalculator.Decay(stat);                 // 그 자리에서 감쇠

ApplyJob(stat);     // Support/Growth 외 효과만 (Doubt, CashBonus 등) — 매 턴 재적용
ApplySkills(stat);  // 토글형 스킬 효과 — 매 턴 재적용 (기존과 동일, 변경 없음)
```

```csharp
// TradeCalculator — 이제 PlayerStat을 직접 받아 그 자리에서 반영한다
public static void Long(PlayerStat stat, long amount)  { stat.Support += amount * 0.1f; stat.Growth += amount * 0.1f; }
public static void Short(PlayerStat stat, long amount) { stat.Support -= amount * 0.1f; stat.Growth -= amount * 0.1f; }
public static void Decay(PlayerStat stat)              { stat.Support *= 0.995f; stat.Growth *= 0.995f; }
```

```csharp
// JobManager.SelectJob() — 선택하는 순간 CurrentStat에 직접 반영
StatCalculator.ApplyJobSelection(MarketManager.Instance.CurrentStat, job);
```

- `MarketManager.HandleBuyCoin/SellCoin`이 `TradeCalculator.Long/Short(CurrentStat, amount)`를 직접 호출한다 (더 이상 별도 누적 데이터를 거치지 않음).
- Job의 Support/Growth **외** 효과(DoubtDecrease, CashBonus, VolumeIncrease/Decrease, ExitUnlock 등)는 기존처럼 직업을 유지하는 동안 매 턴 계속 재적용된다 (감쇠 대상 아님).
- `decayRate = 0.995` (매 턴 0.5%씩 감소, 절반이 되는 데 약 138턴).

공식은 `Game_Formula.md` 3장 "누적치 감쇠", "3-1. 직업(Job)이 Support/Growth에 주는 영향"에 반영했다.

### 미완료 부분

- 스킬(재사용형)의 1회성 사용/재구매 시스템은 아직 구현 전이다 (`Next_Tesk.md` 참고). 구현되면 Skill 사용 시점에도
  동일하게 `CurrentStat`에 직접 반영하고, 감쇠는 이미 있는 `TradeCalculator.Decay(CurrentStat)` 한 번으로 자동 처리된다.

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
- **Systems**: StatCalculator, ProbabilityCalculator, PriceCalculator, TradeCalculator — 상태를 변경하지 않고 계산만 수행한다.
- **EventHub**: UI ↔ Manager 사이의 이벤트를 중계한다.
