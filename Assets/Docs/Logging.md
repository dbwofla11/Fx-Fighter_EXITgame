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

### 문제: 거래 효과의 지속성

`StatCalculator.Calculate()`는 매 턴마다 `PlayerStat`을 새로 생성하고 Job/Skill 효과만 다시 적용한다.
거래로 발생한 Support/Growth 증감을 그대로 두면 다음 턴에 사라지므로, 별도 RuntimeData에 누적한다.

- `RuntimeTradeData` (Assets/Scripts/Stat) — 거래 누적 Support/Growth 보관
- `MarketManager.TradeData`가 소유
- `StatCalculator.Calculate()`가 `ApplyJob` → `ApplySkills` → `ApplyTrade` 순으로 합산

### 처리 흐름

```
EventHub.RaiseBuyCoin(amount) / RaiseSellCoin(amount)
 ↓
┌─ PlayerManager  : 현재가(MarketManager.CurrentStat.CurrentPrice) 기준 현금 정산 + AddCoin
└─ MarketManager  : TradeCalculator.Long/Short → TradeData(Support/Growth) 누적
```

`TradeCalculator`는 기존 `StatCalculator`/`ProbabilityCalculator`/`PriceCalculator`와 동일하게 static Calculator로 변경했다(상태 없음, 계산만 수행).

거래 1회당 Support/Growth 가중치는 `Game_Formula.md` 3장 "시장 영향치 계산"에 정의되어 있다 (거래량 비례, 코인당 0.05).

### 버그 수정 : CurrentPrice가 매 턴 초기화되던 문제

`StatCalculator.Calculate()`가 매 턴 새 `PlayerStat` 인스턴스를 만드는데, `CurrentPrice`는 `Reset()` 대상이 아니면서도
새 인스턴스라 기본값 0으로 시작해버려서 `PriceCalculator`가 항상 0을 기준으로 `±delta`만 계산했다.
`Game_Formula.md`의 `Price(t+1) = Price(t) + delta` 공식과 어긋나는 기존 버그였다.

`StatCalculator.Calculate()` 안에서 새 stat을 만든 직후, 재할당 전이라 아직 이전 값을 들고 있는
`MarketManager.Instance.CurrentStat.CurrentPrice`를 새 stat에 이월하도록 수정했다 (`ApplyTrade`가
`MarketManager.Instance.TradeData`를 읽는 것과 동일한 타이밍 패턴).

### 미완료 부분

- Long/Short UI 버튼은 아직 없다 (`EventHub.RaiseBuyCoin`/`RaiseSellCoin`을 호출할 UI 미구현).
- 발행량(Supply) 조작, 시사 이벤트(`EventCalculator`) 계산 로직은 별도 작업으로 남아있다.

## 현재 아키텍처 요약

```
Manager
 ↓
RuntimeData
 ↓
Systems (Calculator)
```

- **Manager**: TimeManager, MarketManager, SkillManager, JobManager, PlayerManager — 게임 상태를 관리하며 `EventHub`를 구독한다.
- **RuntimeData**: PlayerStat, RuntimeSkillData, RuntimeJobData, RuntimeTradeData — 현재 상태와 계산 결과를 저장한다.
- **Systems**: StatCalculator, ProbabilityCalculator, PriceCalculator, TradeCalculator — 상태를 변경하지 않고 계산만 수행한다.
- **EventHub**: UI ↔ Manager 사이의 이벤트를 중계한다.
