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
| `OnSkillClicked` | `Action<SkillID>` | UI (예정, 스킬 아이콘 클릭) | `SkillManager.HandleSkillClicked` — 토글형은 `EnableSkill`/`DisableSkill`, 재사용형은 `RuntimeSkillData.SelectedSkillId`에 선택만 저장 |
| `OnSkillPurchased` | `Action` (인자 없음) | UI (예정, 구매 버튼 클릭) | `SkillManager.HandlePurchase` — 재사용형 전용, `SelectedSkillId`를 조회해 구매(`PlayerManager.TrySpend`)+`StatCalculator.ApplySkillUse` 즉시 반영 |
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

## Skill 재사용 시스템

Support/Growth 감쇠 구조가 갖춰진 뒤, Skill 쪽도 Job/Trade와 동일한 "발생 시점에 `CurrentStat`에 직접 반영 →
매 턴 감쇠" 패턴을 따르도록 마무리했다. 다만 `ExitUnlock`처럼 영구 해금 성격의 스킬까지 이 방식에 끼워 넣으면
해금했다가 다시 잠기는 문제가 생기므로, `SkillSO.isReusable` 플래그로 두 그룹을 나눠 처리한다.

### 구조

```csharp
// SkillSO
[Header("Reuse")]
public bool isReusable = true;
// true  : Support/Growth 부스트형. 클릭할 때마다 비용을 내고 즉시 CurrentStat에 반영된다.
//         잠기지 않고 계속 재구매 가능하며, 비용은 구매 횟수에 따라 상승한다.
// false : 영구 효과형(ExitUnlock 등). 기존 토글(EnableSkill/DisableSkill) 방식 유지, 감쇠 없음
```

이 과정에서 설계를 세 번 갈아엎었다.

**시도 1 — "사용 시 1회 반영 후 잠김 → 별도의 재구매 이벤트로 잠금 해제"라는 2단계(구매/사용 분리) 구조.**
스킬 아이콘 클릭 한 번으로 구매와 적용이 함께 일어나는 UI(전염병 주식회사류 업그레이드 트리)와 맞지 않아 폐기.

**시도 2 — 아이콘 클릭(`OnSkillClicked`) 한 번에 구매+적용을 동시에 처리, 잠금 자체를 없앰.**
클릭만으로 바로 결제가 나가버려서, 스킬 아이콘을 눌러 정보 패널을 보는 것과 실제 구매를 확정하는 행동이
UI상 구분되지 않는 문제가 있어 폐기.

**시도 3 — 아이콘 클릭과 구매 버튼 클릭을 별개 이벤트로 분리하되, 둘 다 `SkillID`를 각자 직접 실어 보냄.**
구매 버튼 이벤트(`OnSkillPurchased(SkillID id)`)가 "어떤 스킬을 살지"를 매번 인자로 직접 받는 구조라, 실제
UI가 스킬마다 구매 버튼을 따로 두는 게 아니라 우측 정보 패널에 구매 버튼이 하나만 있는 구조(스크린샷 참고)와
맞지 않아 폐기. 이 구조에서는 "지금 뭐가 선택돼 있는지"를 게임 로직이 전혀 모른 채, UI가 알아서 기억했다가
버튼 클릭 시 넘겨줘야 했다.

**최종 — 아이콘 클릭이 재사용형 스킬의 선택 상태를 `RuntimeSkillData.SelectedSkillId`에 저장하고,
구매 버튼(`OnSkillPurchased`, 파라미터 없음)은 그 저장된 값을 읽어서 구매한다.**
"선택"이라는 상태 자체가 UI가 아니라 게임 로직(RuntimeData) 쪽에 있는 것으로 확정했다.

- **재사용형 스킬(`isReusable == true`)**: 아이콘 클릭(`OnSkillClicked`) 시 `SkillManager.HandleSkillClicked`가
  `runtimeSkillData.SelectedSkillId = id`만 저장하고 끝난다 (효과 적용 없음). 구매 버튼(`OnSkillPurchased`, 인자 없음)을
  누르면 `SkillManager.HandlePurchase`가 `SelectedSkillId`로 스킬을 조회해
  `cost = baseCost × costMultiplier^PurchaseCount`를 계산, `PlayerManager.TrySpend(cost)`를 시도하고,
  성공하면 `StatCalculator.ApplySkillUse`로 `MarketManager.CurrentStat`에 Support/Growth 효과를 즉시 반영한 뒤
  `PurchaseCount++`(다음 구매 비용 상승). 잔액이 부족하거나 선택된 스킬이 없으면 아무 것도 바뀌지 않는다.
  잠그는 단계가 없으므로 같은 스킬을 몇 번이고 연속으로 다시 구매할 수 있다.
  `SkillManager.SelectedSkillId` 프로퍼티로 UI가 현재 선택을 읽어 정보 패널(Cost 등)을 그릴 수 있다.
  `JobManager.SelectJob` → `StatCalculator.ApplyJobSelection`과 동일한 패턴으로 stat에 직접 반영되고,
  감쇠는 이미 있는 `TradeCalculator.Decay(CurrentStat)`가 매 턴 자동 처리하며, 이를 위한 별도 누적 데이터 클래스는
  만들지 않았다 (Job/Trade와 동일한 이유, `Support/Growth 감쇠` 절 참고).
- **재사용 불가 스킬(`isReusable == false`, 예: `ExitUnlock`)**: 기존과 동일하게 아이콘 클릭(`OnSkillClicked`) →
  `SkillManager.HandleSkillClicked` → `EnableSkill`/`DisableSkill` 토글 방식, `StatCalculator.ApplySkills`가
  활성 상태를 매 턴 재적용하는 로직 그대로 유지한다. 감쇠 대상이 아니고, 선택 상태 저장이나 구매 버튼과는 무관하다.
- `StatCalculator.ApplySkills`는 `skill.Profile.isReusable == false`인 활성 스킬만 대상으로 한다 (재사용형 스킬은
  `IsEnabled`를 사용하지 않으므로 실질적으로 항상 제외됨).
- `PlayerManager.TrySpend(long amount)`를 추가했다. 잔액 부족 시 차감 없이 `false`를 반환, 충분하면 차감 후 `true`.

```
EventHub.OnSkillClicked (아이콘 클릭, id)
 ↓
SkillManager.HandleSkillClicked
 ├─ 재사용형 → RuntimeSkillData.SelectedSkillId = id 저장, 끝
 └─ 토글형   → EnableSkill / DisableSkill

EventHub.OnSkillPurchased (구매 버튼, 인자 없음)
 ↓
SkillManager.HandlePurchase
 ↓
RuntimeSkillData.SelectedSkillId로 스킬 조회 → 없으면 종료
 ↓
PlayerManager.TrySpend(cost) 실패 → 아무 효과 없음
 ↓ 성공
StatCalculator.ApplySkillUse(CurrentStat, skill) + PurchaseCount++ (잠기지 않음, 다음 구매는 비용만 상승)
```

이로써 Support/Growth에 영향을 주는 세 소스(Job 선택, Trade, 재사용형 Skill 사용) 모두 "발생 시점에 직접 반영 →
매 턴 감쇠" 패턴으로 통일되었다.

## 시사 이벤트(EventCalculator) 구현 & 발행량(Supply)/Scarcity 활성화

`EventCalculator.Calculate()`가 빈 스텁이었던 것을 구현했다. 설계 과정에서 두 가지가 확정됐다.

- **발행량(Supply)의 소스를 시사 이벤트로 한정**한다. Job/Skill은 Supply에 영향을 주지 않는다. 덕분에
  `Next_Tesk.md`에 별도 후보로 있던 "발행량 조작"과 "시사 이벤트"가 이번 작업 하나로 함께 끝났다 —
  `PriceCalculator.CalculateScarcity`(`100 × (1 - Supply / 20000)`)는 이미 코드에 있었지만 `Supply`를 바꾸는
  소스가 없어 항상 0(→ Scarcity 100 고정)이었던 죽은 값이었다.
- **Supply도 Support/Growth와 동일한 "직접 반영 → 매 턴 감쇠" 패턴**을 따른다. 이벤트가 발생한 순간
  `PlayerStat.Supply`에 직접 반영되고, 매 턴 0을 향해 감쇠한다 (Job/Skill처럼 활성 상태 동안 계속 재적용되는
  Doubt/CashBonus 패턴이 아니라, 뉴스가 만든 "일시적 공급 충격"이 시간이 지나며 잦아드는 것으로 설계했다).

### 필요했던 선행 수정 : `StatCalculator.Calculate()`가 Supply를 이월하지 않던 문제

Support/Growth 감쇠 구조를 만들 때(위 "Support/Growth 감쇠" 절)는 `Supply` 필드가 아직 쓰이지 않아 이월 대상에서
빠져 있었다. `StatCalculator.Calculate()`가 매 턴 새 `PlayerStat`을 만들면서 `Supply`를 이전 값에서 이어받지 않으면,
이벤트가 반영한 값이 다음 턴에 `Reset()`의 기본값 0으로 사라져버린다. `CurrentPrice`/`Support`/`Growth`와 동일하게
`stat.Supply = previous.Supply;`를 이월 목록에 추가했다.

### 이벤트 설계

- **발생 시점** : `MarketManager.NextTurn()`에서 7턴마다(`turnCount % 7 == 0`) 40% 확률로 자동 발생을 판정한다.
  기존에 연결만 되어 있던 `EventHub.OnNewsEvent`(수동 트리거)도 동일한 `EventCalculator.Calculate(CurrentStat)`를
  호출하도록 그대로 유지했다 — 자동/수동 두 경로 모두 같은 계산 로직을 공유한다.
- **방향(긍정/부정)** : 그동안 어디서도 읽히지 않던 `PlayerStat.PositiveEventRate`/`NegativeEventRate`(Job/Skill의
  기존 `EffectType`)를 여기서 처음 사용한다. `Pup_event = Clamp(0.5 + PositiveEventRate/100 - NegativeEventRate/100, 0, 1)`.
- **크기(등급)** : Small(60%, ±8) / Medium(30%, ±16) / Large(10%, ±30) 세 등급 중 무작위 선택. 등급이 클수록 Support/Growth/
  Supply/가격 변화량이 커진다.
- **가격에 대한 즉시 충격** : `Game_Formula.md` 4장의 "큰 이벤트일수록 긴 양봉/음봉"을 표현하기 위해, 그 턴의
  `PriceCalculator` 정규 가격 변화와는 별도로 `stat.CurrentPrice`에 `±3%~±12%`의 1회성 가격 변화를 즉시 더한다
  (감쇠하지 않음 — 그 턴에만 반영되는 충격이라 다음 턴부터는 남지 않는다).
- **Supply 부호는 이벤트 방향과 반대** : 긍정 이벤트 = 공급 감소(→ Scarcity 상승 → 가격에 우호적), 부정 이벤트 =
  공급 증가(→ Scarcity 하락 → 가격에 불리). Scarcity 공식과 앞뒤가 맞아야 하므로 부호를 반전시켰다.
- `EventCalculator`를 다른 Calculator(StatCalculator, TradeCalculator, PriceCalculator)와 동일하게 `static` 클래스로
  바꿨다 (기존에는 `new EventCalculator().Calculate()`로 인스턴스를 생성해 호출하는 유일한 예외였다).
  `MarketManager.HandleNewsEvent`도 `EventCalculator.Calculate(CurrentStat)` 호출로 맞춰 수정했다.

공식은 `Game_Formula.md` 2장(Scarcity), 4장(시사 이벤트)에 반영했다.

### Doubt(의심도)도 이벤트에 포함 — 감쇠는 없다는 것을 뒤늦게 확인하고 되돌림

처음에는 이벤트가 Support/Growth/Supply/가격만 건드리도록 구현했는데, "이벤트가 Doubt도 바꾸는 게 맞지 않냐"는
질문을 받고 Doubt를 포함시키는 과정에서 한 차례 설계를 잘못 짚었다가 되돌렸다.

**1차 시도(잘못됨)** : Doubt를 Support/Growth와 완전히 같은 "1회 반영 → 매 턴 감쇠" 그룹으로 통합했다. Job의
`DoubtDecrease`를 "선택 시점 1회 반영"으로 바꾸고(`ApplyJobSelection`), `TradeCalculator.Decay`에 Doubt 감쇠를
추가했다. Doubt가 매 턴 이월되지 않던 것을 이월하게 만드는 김에, Job의 기존 "매 턴 재적용" 방식과 감쇠가
충돌하는(무한정 내려가는) 문제까지 같이 잡으려다 벌어진 일이었다.

**되돌린 이유** : 이 프로젝트의 실제 기획은 "Doubt는 감쇠하지 않고, 오히려 시간이 지날수록 자동으로 100을 향해
올라가다가 100이 되면 게임오버가 되는 지표"였다. 감쇠가 있으면 안 되는 값이었으므로, 애초에 "Job의 DoubtDecrease가
매 턴 재적용되면 감쇠와 충돌한다"는 문제 자체가 성립하지 않았다 — 감쇠를 넣지 않으면 충돌도 없다.

**최종** : `TradeCalculator.Decay`에서 Doubt 관련 코드를 제거하고, `ApplyJob`/`ApplyJobSelection`/`ApplySkills`/
`ApplySkillUse`의 `DoubtDecrease` 처리를 전부 원래대로(Job/토글형 스킬이 활성 상태인 동안 매 턴 계속 재적용,
CashBonus/Volume과 같은 그룹) 되돌렸다. 다만 `StatCalculator.Calculate()`가 `stat.Doubt = previous.Doubt;`로
이전 값을 이어받는 것만은 유지했다 — 이게 없으면 매 턴 `Reset()`으로 0에서 다시 시작해버려서, 이벤트가 반영한
Doubt도, "시간이 지날수록 100에 가까워진다"는 기획도 애초에 성립할 수 없기 때문이다. `EventCalculator`가 Doubt에
주는 영향(이벤트 방향과 반대 부호, `Game_Formula.md` 4장)은 그대로 유지된다 — 감쇠가 없으니 이벤트로 바뀐 값도
다음 턴에 그대로 남는다.

### 후보로 남은 것 : 2년 경과 후 자동 Doubt 상승 + 게임오버 판정

이 대화 중에 "게임 시간 2년이 지나면 자동으로 Doubt +20, 그 이후 매 턴 자동 증가, Doubt가 100이 되면 게임오버"라는
기획 의도를 확인했다. 자동 상승 로직도, `Doubt >= 100` 게임오버 판정도 현재는 구현되어 있지 않다. 트리거 턴
수·매 턴 증가량 등 세부 수치가 미확정이라 이번 작업 범위에서는 제외하고 `Next_Tesk.md`에 다음 후보로 남겼다.

## EventSO 도입 — 이벤트 크기를 코드 상수에서 데이터로 분리

게임오버 작업에 들어가기 전에, `EventCalculator`에 하드코딩돼 있던 Small/Medium/Large 등급 상수
(`SmallStatDelta`, `MediumSupplyDelta`, `LargePriceRatio` 등)를 `EventSO`(ScriptableObject)로 빼달라는 요청을
받았다. Job/Skill이 이미 `JobSO`/`SkillSO` + `List<EffectData>` 구조를 쓰고 있어서 같은 패턴을 따랐다.

**1차 시도(수정됨)** : 처음에는 `weight`/`statDelta`(Support/Growth/Doubt 공통 변화량)/`supplyDelta`/`priceRatio`
네 필드만 가진 SO로 만들었다. 그런데 실제로 원하는 그림은 "유명 스트리머가 코인을 소개했습니다! → 지지도+15,
상승률+10 (Doubt는 안 건드림)" / "금융당국이 조사 시작 → 의심도+20, 상승률-15 (지지도는 안 건드림)" 처럼
**이벤트마다 어떤 스탯을 얼마나 바꿀지 제각각**이고, 이벤트 로그에 표시할 **문구(message)**도 필요했다.
`statDelta` 하나로 세 스탯을 항상 같은 크기로 묶어 움직이는 구조로는 이걸 표현할 수 없어서 다시 설계했다.

**최종** :

- **`EventSO`**(`Assets/Scripts/SOs/EventSo.cs`) : `message`(이벤트 로그 문구), `category`(`Positive`/`Negative`,
  신규 enum `Assets/Scripts/SOs/detailData/EventCategory.cs`), `weight`(같은 카테고리 안에서의 가중치 랜덤),
  `effects`(`List<EffectData>` — Job/Skill과 동일한 구조를 재사용해 이벤트마다 원하는 스탯만 부호 있는 값으로
  지정), `supplyDelta`/`priceRatio`(부호 포함, Job/Skill이 다루지 않는 이벤트 전용 값이라 `effects`와는 별도
  필드로 유지)로 구성된다. `effects`에서 "의심도 +20"처럼 Doubt를 늘리는 경우를 표현하려고 `EffectType`에
  `DoubtIncrease`를 추가했다 (기존엔 `DoubtDecrease`만 있어서 증가를 못 나타냈다).
  `StatCalculator.ApplyEffect`를 `private`에서 `public`으로 바꿔 `EventCalculator`가 그대로 재사용한다
  (Job/Skill과 같은 스위치문을 또 만들지 않기 위해).
- **방향(긍정/부정) 결정과 선택 로직 분리** : `EventCalculator.Calculate`가 먼저 `RollCategory`(기존
  `PositiveEventRate`/`NegativeEventRate` 기반 확률)로 `Positive`/`Negative` 중 하나를 정하고, 그 다음
  `eventDatabase` 중 **그 카테고리에 속한 EventSO만** 대상으로 `weight` 가중치 랜덤(누적 합 룰렛 휠 방식)을 돌려
  하나를 뽑는다. 뽑힌 SO의 `effects`/`supplyDelta`/`priceRatio`는 이미 authored된 부호를 그대로 쓰므로, 기존에
  있던 "sign을 곱한다" 로직은 전부 제거했다. 해당 카테고리에 이벤트가 하나도 없으면 그 턴은 조용히 아무 일도
  안 일어난다.
- **`MarketManager`** : `SkillManager.skillDatabase`와 동일한 패턴으로 `[SerializeField] private List<EventSO>
  eventDatabase;`를 들고 있다. `NextTurn()`의 자동 발생 체크와 `HandleNewsEvent()`(수동 트리거) 모두 새로 추가한
  `TriggerNewsEvent()`를 거친다 — `EventCalculator.Calculate`가 반환한 `EventSO`(발생 안 했으면 null)를 받아서
  발생했을 때만 로그에 기록한다.
- **이벤트 로그** : `RuntimeSkillData`/`RuntimeJobData`와 같은 위치(`Assets/Scripts/Stat/`)에 `RuntimeEventData`
  (`List<EventLogEntry> Log`)와 `EventLogEntry`(`EventSO Profile` + `DateTime Date`)를 새로 만들었다.
  `MarketManager.EventLog`(`IReadOnlyList<EventLogEntry>`)로 노출해서, 이벤트 로그 UI(스크린샷의 좌측 패널)가
  나중에 이 리스트를 그대로 순회하며 `Profile.message`/`Date`/`Profile.effects`를 그릴 수 있게 했다. 아직 UI는
  없다 (`Next_Tesk.md` "UI 연결" 후보).
- **`.asset` 데이터는 여전히 비어 있음** : `EventSO` 클래스와 `eventDatabase` 배선은 끝났지만, 실제 이벤트
  에셋(스크린샷의 "유명 스트리머가 코인을 소개했습니다!" 같은 것들)은 아직 하나도 만들어지지 않았다. Unity
  에디터에서 `Create > Game > EventSO`로 직접 만들고 `MarketManager` Inspector의 `Event Database`에 등록해야
  실제로 이벤트가 발생한다 (`Next_Tesk.md` 참고).
- 작업 중 `MarketManager.NewsEventIntervalTurns`가 이전 세션에서 정한 7이 아니라 30으로 바뀌어 있는 것을
  발견했다 — 대화 밖에서 직접 수정된 것으로 보여 그대로 두고 `Game_Formula.md` 4장의 발생 주기 설명만 30으로
  맞춰 고쳤다.

## 발행량 조작 기능 추가, 그리고 그 과정에서 발견한 EffectType 직렬화 버그

"발행량 조작은 이벤트가 아니라 플레이어가 버튼으로 따로 한다"는 지적을 받고 기존 스킬 에셋
(`Assets/Scripts/Profile/스킬_프로파일/`)을 열어보니, 이름부터 발행량 관련인 스킬 3개
(`추가발행권한`, `우회발행권한`, `발행량은폐`)가 이미 존재했다. 그런데 이 스킬들을 살펴보다가 직접 원인이 된
**심각한 버그**를 하나 발견했다.

### 버그 : EffectType enum 중간 삽입으로 기존 .asset 데이터 손상

Doubt 작업(위 "Doubt(의심도)도 이벤트에 포함" 절)에서 `EffectType.DoubtIncrease`를 `DoubtDecrease` **바로
다음(중간)**에 추가했었다. Unity는 enum을 이름이 아니라 선언 순서(정수값)로 직렬화하기 때문에, 중간에 끼워
넣으면 그 뒤에 있던 모든 항목의 정수값이 하나씩 밀린다.

실제로 `추가발행권한.asset`이 `effectType: 5`(원래 `CashBonus`, "위기 상황에서 자금을 빠르게 마련"이라는
설명과 일치)를 쓰고 있었는데, 내가 중간 삽입을 하면서 5번이 `NegativeEventRate`로 뒤바뀌어 있었다 — 코드는
멀쩡히 컴파일되지만 실제 스킬 효과가 조용히 다른 의미로 바뀌는, 발견하기 매우 어려운 종류의 버그다. 다른
스킬(지갑분산/락업/독약조항/발행량은폐/우회발행권한)은 0~2번 값만 써서 우연히 무사했다.

**수정** : `DoubtIncrease`를 기존 항목들 뒤, enum 맨 끝으로 옮겨서 `추가발행권한.asset`의 `effectType: 5`가
다시 `CashBonus`를 가리키도록 원상복구했다. 이번에 새로 추가한 `SupplyIncrease`/`SupplyDecrease`도 반드시
끝에만 추가했다. **앞으로 `EffectType`에 새 항목을 추가할 때는 항상 맨 끝에 추가해야 하며, 중간 삽입은
기존 저장된 `.asset` 데이터를 조용히 손상시키므로 절대 금지**라는 원칙을 세웠다.

### 발행량 조작 기능

스크린샷 기준으로, Long/Short와 완전히 동일한 구조의 새 액션이다.

- `EventHub.OnManipulateSupply(long amount)` 신규 (양수 = 발행량 증가, 음수 = 발행량 감소).
- `TradeCalculator.ManipulateSupply(stat, amount)` : `stat.Supply += amount`, Long/Short와 동일한 가중치(0.1)로
  `Support`/`Growth`는 **반대 부호**로 반영(발행량 증가=희석=Support/Growth 감소), `Doubt`는 `amount`의
  **절대값**에 비례해 항상 증가(늘리든 줄이든 "조작했다는 사실"이 의심을 키운다는 설정, 감쇠하지 않으므로 그대로
  누적).
- 현금 비용 없음 (Long/Short와 달리 `PlayerManager`를 거치지 않고 `MarketManager`가 바로 처리).
- **`추가발행권한` 스킬을 구매하기 전에는 사용할 수 없다.** `SkillManager`에 `IsUnlocked(SkillID id)`를
  추가했다 — 재사용형 스킬도 `HandlePurchase` 성공 시 `IsUnlocked = true`가 되는 걸 그대로 활용한 것이라 별도
  잠금 상태를 새로 만들 필요가 없었다. `MarketManager.HandleManipulateSupply`가 이 값을 확인해서 잠겨 있으면
  조용히 무시한다.
- `StatCalculator.ApplyEffect`/`ApplySkillUse`에 `SupplyIncrease`/`SupplyDecrease`(그리고 김에 빠져 있던
  `DoubtDecrease`/`DoubtIncrease`도 `ApplySkillUse`에) 처리를 추가했다 — 재사용형 스킬이 구매 시점에 이 효과를
  낼 수 있게 하기 위해서다. `ApplyJob`/`ApplySkills`(매 턴 재적용 루프)에서는 `SupplyIncrease`/`SupplyDecrease`를
  방어적으로 제외했다 — Supply는 감쇠 대상이라 매 턴 재적용하면 Doubt 때 겪은 것과 같은 무한정 증가 문제가
  생기기 때문이다 (지금은 Job/토글형 스킬 어디에도 이 효과를 쓰지 않지만, 나중에 실수로 넣어도 안전하게 막힌다).
- `추가발행권한`/`우회발행권한`/`발행량은폐` 세 스킬의 `.asset` 데이터 자체에는 아직 `SupplyIncrease`/
  `SupplyDecrease` 값을 넣지 않았다 — 이 세 스킬이 발행량 조작 **버튼의 해금 조건**이라는 것만 확정됐고, 스킬
  구매 자체가 추가로 Supply를 직접 바꿀지는 아직 논의되지 않았다 (`Next_Tesk.md` 참고).

공식은 `Game_Formula.md` 3장 "발행량 조작"에 반영했다.

## Doubt 자동 상승 + 엔딩 시스템(4종)

"2년 경과 후 자동 Doubt 상승 + 게임오버 판정" 후보 작업을 진행하면서, 사용자가 엔딩 조건 3가지(체포/엑시트/영웅)
전체 기획을 전달했고, 여기에 거지 엔딩(현금 0 + 코인 0)이 하나 더 추가됐다. 기존 문서(`Game_Formula.md`,
`PROJECT_OVERVIEW.md`)에는 이 엔딩 기획이 전혀 없어서 5장으로 새로 반영했다.

### Doubt 자동 상승

`MarketManager.ApplyDoubtAutoRise()`가 `NextTurn()`마다 호출된다. 1턴=1일 기준으로 게임 시간 2년을 730턴으로
환산했다 : 730턴째 Doubt +20 (1회), 731턴부터는 매 턴 +0.5. Doubt는 감쇠하지 않으므로(이전 절 참고) 그대로
누적된다.

### 엔딩 4종

- **체포(Bad)** : `Doubt >= 100`. 자동 판정(매 턴 `CheckAutomaticEndings`).
- **거지** : 현금 0 + 코인 0. 자동 판정. 결과 서사는 아직 미정 (`Next_Tesk.md` 참고).
- **엑시트(Neutral)** / **영웅(True)** : 둘 다 새로 만든 `EventHub.OnExitRequested`(엑시트 버튼) 트리거로
  판정한다. `MarketManager.CanExit`(`PlayerManager.currentMoney >= TargetAsset`)를 만족해야 하고, 그 순간의
  `Doubt <= 50 && Support >= 80`이면 영웅, 아니면 엑시트로 갈린다.

### 설계 확정 과정에서 바뀐 것들 (스크린샷 기반)

기획 원문에는 엑시트/영웅 조건에 "보유 코인 전량 매도"가 포함돼 있었는데, 실제 UI 스크린샷(목표금액/현재금액
패널)을 보여주면서 "이 화면에서 목표금액 달성하면 눌리는 버튼 하나로 만든다"는 요구가 추가로 들어왔다. 확인
결과:

- 목표 자산(`TargetAsset`)은 화면상 100,000,000으로 보였지만, 실제로는 이전에 정한 1,000,000,000(10억)이 맞는
  것으로 확인함 (스크린샷은 예시 값이었음).
- 버튼 활성화 조건은 **현금만** 본다 (`currentMoney >= TargetAsset`). "코인 전량 매도" 조건은 이 버튼에서는
  빠졌다 — 코인을 안 팔고 들고 있어도 현금만 충분하면 버튼이 활성화된다.
- 기존에 있던 `PlayerStat.ExitUnlocked`(스킬의 `EffectType.ExitUnlock`으로 해금되는 플래그)는 이 버튼과 **연결하지
  않기로** 했다. 즉 `ExitUnlocked`는 지금 이 엔딩 판정 로직 어디에서도 쓰이지 않는 상태로 남아 있다 (다른 용도로
  쓰일 수도 있으니 삭제하지는 않음).

### 구현

- `Assets/Scripts/Stat/EndingType.cs` 신규 : `Arrest`/`Exit`/`Hero`/`Broke` 4종 enum.
- `EventHub`에 `OnExitRequested`(엑시트 버튼, 인자 없음)와 `OnGameEnded(EndingType)`(엔딩 확정 통지) 추가.
- `MarketManager` :
  - `TargetAsset`(1,000,000,000) 상수, `IsGameOver`/`CanExit` 프로퍼티 추가.
  - `NextTurn()`이 `IsGameOver`면 아무 것도 안 하도록 가드하고, `RaiseMarketUpdated` 직후 `CheckAutomaticEndings()`를
    호출해 체포/거지를 확인한다 (동시 성립 시 체포 우선).
  - `HandleExitRequested()`가 `OnExitRequested`를 받아 `CanExit` 확인 후 영웅/엑시트를 가른다.
  - `EndGame(EndingType)`이 `IsGameOver = true` 설정, `TimeManager.Instance.PauseGame()` 호출, `EventHub.RaiseGameEnded`
    발행을 한 곳에서 처리한다.
- 체포/거지 엔딩 모두 `PlayerManager`의 실제 현금/코인 수치는 건드리지 않는다 (사용자가 "몰수는 서사일 뿐,
  어차피 게임이 끝나니 수치는 안 건드려도 된다"고 확인함).

공식/기획은 `Game_Formula.md` 3장(Doubt 자동 상승), 5장(엔딩 조건)에 신규 반영했다.

## Supply 스케일 재검토 — 결론은 "코드 값이 맞다, UI가 예시값"

`Next_Tesk.md`에 후보로 있던 "Supply 스케일 재검토"를 확인했다. `PriceCalculator.MaxSupply`가 코드상 `20000f`인데
UI 스크린샷은 "현재 발행량 : 20,000,000개"(1000배 차이)를 보여줘서 실제 밸런스를 어느 쪽에 맞출지가 쟁점이었다.

Supply를 바꾸는 값들(`EventSO.supplyDelta`, 발행량 조작 버튼의 `amount`, 스킬 `SupplyIncrease`/`SupplyDecrease`)이
전부 아직 실제 데이터가 입력되지 않은 상태(이벤트 에셋 자체가 없고, 스킬 3종에도 Supply 효과가 없음)라 순수
밸런스 수치 문제였고, 이전에 `TargetAsset`에서 "스크린샷은 예시값, 실제 값은 다르다"로 정리했던 것과 동일한
패턴인지 확인 차 물어봤다.

**결론** : `MaxSupply = 20000`이 실제 기준값이 맞고, UI의 "20,000,000"은 `TargetAsset` 때와 마찬가지로 예시/목업
수치였다. 코드 변경 없음. 앞으로 이벤트 `supplyDelta`/발행량 조작 버튼 `amount`/스킬 Supply 효과 수치를 정할 때는
이 20000 스케일을 기준으로 잡아야 한다. `Game_Formula.md` 2장에 반영했다.

## EventSO 예시 데이터 작성 + Inspector 연결

`EventSO` 클래스와 `MarketManager.eventDatabase` 배선은 끝나 있었지만 실제 이벤트 에셋이 하나도 없어서, 시사
이벤트가 확률상으로만 존재하고 실제로는 절대 발생하지 않는 상태였다. Unity MCP(Unity 에디터와의 실시간 연결)가
이 세션 중에 연결되어서, 예시 데이터를 직접 만들어 등록했다.

- `Assets/Scripts/Profile/이벤트_프로파일/`(스킬의 `스킬_프로파일` 폴더와 동일한 명명 규칙)에 `EventSO` 6개를
  생성했다.
  - **긍정 3개** : 스트리머_소개(Support+15/Growth+10), 거래소_상장(Support+20/Growth+25, weight 0.6로 더
    희귀하게), 규제_완화(Support+10/Doubt-8)
  - **부정 3개** : 당국_조사(Doubt+20/Growth-15), 해킹_사고(Support-25/Growth-20/Doubt+15, weight 0.5), 인플루언서_폭로(Support-15/Doubt+25)
  - `supplyDelta`는 긍정은 음수(-100~-300), 부정은 양수(+150~+400)로 4장 규칙(긍정=공급 감소, 부정=공급 증가)을
    따랐고, `priceRatio`는 ±0.03~±0.11 범위로 등급에 따라 다르게 잡았다. 값 자체는 예시/플레이스홀더이므로
    밸런스 조정은 자유롭게 해도 된다.
- 만든 6개를 `Managers` GameObject의 `MarketManager.eventDatabase`에 전부 등록했다 (씬 저장 완료). Positive/
  Negative 각 카테고리에 최소 1개 이상 있어야 그 카테고리가 뽑혔을 때 실제로 이벤트가 발생하는데(4장), 이제
  양쪽 다 채워졌으므로 시사 이벤트가 정상적으로 발생한다.
- `EventSO.effects`(`List<EffectData>`)에 배열 항목을 채울 때, Unity MCP의 `manage_scriptable_object` patch로
  `effects.Array.size`를 직접 지정하는 건 지원되지 않았다(`Unsupported SerializedPropertyType: ArraySize`
  에러) — 대신 `effects.Array.data[0].effectType`처럼 인덱스를 가진 항목을 바로 set하면 배열이 필요한
  크기까지 자동으로 늘어나므로, size patch 없이 data 항목만 순서대로 지정하면 된다.

## 시사 이벤트 수동 트리거가 가격 변화를 즉시 반영하지 않던 문제 수정

이벤트 SO를 실제로 만들어 등록한 뒤 확인하는 과정에서, 이벤트가 발생해도 가격이 바로 반영되지 않는다는
지적을 받았다. 확인해보니 `EventCalculator.Calculate()`는 설계(Game_Formula.md 4장)대로 `stat.CurrentPrice`를
그 자리에서 바로 바꾸고 있었다 — 문제는 값 자체가 아니라 **전파**였다.

- 자동 발생 경로(`MarketManager.NextTurn()`이 30턴마다 확률 체크) : `TriggerNewsEvent()` 호출 이후 같은
  함수 안에서 `EventHub.RaiseMarketUpdated(CurrentStat)`을 이미 호출하고 있어서 문제없었다.
- 수동 트리거 경로(`EventHub.OnNewsEvent` → `MarketManager.HandleNewsEvent()`) : `TriggerNewsEvent()`만 호출하고
  끝나서, 이벤트가 `CurrentStat.CurrentPrice`를 바꿔도 그 사실을 알리는 브로드캐스트가 없었다. 다음 자연스러운
  턴이 지나야만(`NextTurn()`이 `RaiseMarketUpdated`를 호출할 때) 비로소 반영된 게 보이는 상태였다.

**수정** : `MarketManager.HandleNewsEvent()`에 `TriggerNewsEvent()` 직후 `EventHub.RaiseMarketUpdated(CurrentStat)`
한 줄만 추가했다. 자동 경로는 건드리지 않았다 — 거기서 똑같이 추가하면 `NextTurn()` 끝에서 한 번 더
브로드캐스트가 나가 턴 중간의 미완성 상태(Probability/Price 정규 계산 전)가 한 번 더 노출되는 중복이 생기기
때문이다. `PROJECT_ARCHITECTURE.md`의 `OnNewsEvent` 설명에 반영했다.

## 스킬/직업 정보 패널을 위한 UI 훅 정리

UI 기획안(개요/스킬/직업 선택 화면) 스크린샷을 보고 지금 상태로 각 화면을 띄울 수 있는지 점검했다.

- **기본 스탯(개요 화면 대부분)** : `MarketManager.CurrentStat`이 `EventHub.OnMarketUpdated(PlayerStat stat)`로
  통째로 나가므로 UI는 리스너 하나만 걸면 바로 사용 가능. `PlayerManager.currentMoney`(목표금액 진행률)는
  이벤트가 아니라 기존처럼 폴링 방식.
- **직업 선택 화면** : 추가 작업 불필요. "다음으로" 버튼이 `EventHub.RaiseJobSelected(JobSO)`에 넘길 `JobSO`를
  UI가 이미 들고 있어야 하므로, 그 참조로 `description`/`효과`를 직접 읽으면 된다 (이벤트 로그 UI가
  `EventSO.message`를 직접 읽는 것과 동일한 패턴).
- **스킬 화면** : `SkillManager.SelectedSkillId`로 어떤 스킬이 선택됐는지는 이미 알 수 있었지만, 정보 패널에
  필요한 **현재 Cost**(`baseCost × costMultiplier^PurchaseCount`)는 `PurchaseCount`가 `SkillManager` 내부
  런타임 상태라 UI가 계산할 방법이 없었다. `GetSkillProfile(SkillID id)`(SkillSO 반환)와 `GetCurrentCost(SkillID
  id)`(현재 비용 반환) 두 개를 추가해서 막힌 부분을 풀었다. 새 DTO 클래스는 만들지 않고 기존 `GetSkill`/
  `CalculateCost`를 재사용하는 얇은 public 래퍼로만 처리했다.
- **뒤로 미룬 것** : 개요 화면 하단의 "코인 지지도 상승률 +20" 같은, Job/Skill발 보너스만 따로 뽑아 보여주는
  항목은 `CurrentStat`이 거래·감쇠·이벤트를 다 합친 값이라 지금 분리해서 읽을 방법이 없다. 별도 설계가
  필요해서 이번 작업 범위에서 제외했다.

## 개요 화면 Job+Skill 보너스 표시 구현

Next_Tesk.md에 후보로 남겨뒀던 "개요 화면의 Job/Skill 보너스 분리 표시"를 바로 이어서 진행했다. 먼저 표시
방식부터 확정해야 했다 — Job과 Skill을 따로따로 보여줄지, 합쳐서 스탯당 하나로 보여줄지 물어봤고 **합쳐서
하나**로 정해졌다.

그다음 걸림돌은 재사용형 스킬(구매하면 1회 반영 후 서서히 감쇠하는 타입)이었다. Job의 Support/Growth 효과는
선택 시 1회 반영 후 감쇠하고, 재사용형 스킬도 구매 시 1회 반영 후 감쇠한다(3-1/3-2장) — 즉 시간이 지나면
둘 다 자연히 줄어드는 값이라, 이걸 표시에 포함시키려면 감쇠를 반영할지부터 정해야 했다. 감쇠를 반영하기로
확정했다 — Trade/Event가 섞이지 않은 "순수 Job+Skill 몫이 지금 얼마나 남아있는지"를 보여주는 게 목적이므로.

### 구현

`PlayerStat`에 `JobSkillSupportBonus`/`JobSkillGrowthBonus`(float)를 추가해서, `Support`/`Growth`와 나란히
가지만 **Trade(Long/Short)와 시사 이벤트는 반영하지 않고 Job 선택·재사용형 Skill 구매만** 반영하는 그림자
값으로 만들었다.

- `StatCalculator.ApplyJobSelection`/`ApplySkillUse`에서 `Support`/`Growth`를 올릴 때 이 두 필드에도 동일한
  값을 같이 더한다.
- `StatCalculator.Calculate()`의 이월(carry-over) 목록에 추가해서 매 턴 이전 값을 이어받는다.
- `TradeCalculator.Decay`에서 `Support`/`Growth`와 동일한 `decayRate`(0.995)로 같이 감쇠시킨다.
- 게임 계산(가격, 확률 등)에는 전혀 관여하지 않는 순수 UI 표시용 값이다.

Doubt("의심도 상승률" 행)는 손대지 않았다 — Doubt는 애초에 감쇠하지 않는 값이고 Job/토글형 스킬의
`DoubtDecrease`는 매 턴 그 시점의 `effects`를 다시 읽어 재적용하는 방식(3-1장)이라, 별도 누적 없이 UI가
`JobManager.CurrentJob.effects` + `SkillManager.GetActiveSkills()`의 Doubt 관련 효과를 그대로 합산해서 보여주면
된다 (이미 둘 다 public).

이전에 시도했다 폐기한 "소스별 누적 데이터"(`RuntimeTradeData`, `JobBoostData`, "Support/Growth 감쇠" 절 참고)와
겉보기엔 비슷해 보이지만, 그때는 이 누적치가 실제 게임 계산 파이프라인에 다시 합류해야 해서 복잡했던 반면
이번 값은 순수 표시 전용이라 계산에 전혀 관여하지 않는다는 점이 달라서 훨씬 단순하게 끝났다.

공식은 `Game_Formula.md` 3장 "누적치 감쇠"에 반영했다.

## CashBonus를 실제 현금 증가로 연결

이벤트 SO 작업 중에 "CashBonus가 실제로 돈에 반영이 안 된다"는 지적을 받았었는데, 그때는 가격 반영 버그 얘기로
넘어갔다가 이번에 다시 짚었다. `StatCalculator.ApplyEffect`가 `stat.CashBonus += effect.value`만 하고 그 값을
소비하는 곳이 어디에도 없었던 게 원인 — Job/Skill의 CashBonus 효과가 조용히 죽어있었다.

**요구사항** : "매 턴마다 올라가는 현금의 양이 CashBonus%만큼 증가"로 확정. 별도 누적/이월 로직 없이, 이미
매 턴 `StatCalculator.Calculate()`(`ApplyJob`/`ApplySkills`)가 다시 채워주는 `CurrentStat.CashBonus`를 그대로
가져다 쓰면 충분했다.

**구현** : `MarketManager.NextTurn()`에 `ApplyDoubtAutoRise()`와 같은 자리에 `ApplyCashBonus()`를 추가했다.

```csharp
private void ApplyCashBonus()
{
    long bonus = (long)(PlayerManager.Instance.currentMoney * (CurrentStat.CashBonus / 100f));
    PlayerManager.Instance.AddMoney(bonus);
}
```

재사용형 스킬 구매 시점의 CashBonus(`ApplySkillUse`)는 그 턴에만 1회 반영되고 다음 턴에 사라진다는 것도
확인했다 — `CashBonus`는 Support/Growth와 달리 감쇠/이월 대상이 아니라 매 턴 `Calculate()`가 만드는 새
`PlayerStat`에서 0부터 다시 Job/토글형 스킬 효과만으로 채워지기 때문이다. 지금 요구사항(Job/토글형 스킬의
CashBonus를 매 턴 반영)에는 이 동작으로 충분해서 손대지 않았고, 알려진 동작으로 문서에 남겨뒀다.

공식은 `Game_Formula.md` 3-3장(신규)에 반영했다.

### 재사용형 스킬의 CashBonus가 실제로는 0턴도 반영 안 되던 문제 수정

라이브로 시나리오를 확인해보려다(Play 모드 진입 직전에 사용자가 "돌리지 말고 설명만" 요청해서 코드를 다시
따라가며 손으로 검증) 위에서 "다음 턴에 사라진다"고 적었던 게 부정확했다는 걸 발견했다. 실제로는:

1. 구매 시점에 `ApplySkillUse`가 그 순간의 `CurrentStat`에 `CashBonus += 값`을 해준다.
2. 근데 이 값을 소비하는 `ApplyCashBonus()`는 `NextTurn()` 안에서만 호출된다.
3. `NextTurn()`의 첫 줄 `CurrentStat = StatCalculator.Calculate();`가 완전히 새 `PlayerStat`을 만드는데,
   `CashBonus`는 이월 대상이 아니라서 새 오브젝트는 0에서 시작한다.
4. `ApplyJob`/`ApplySkills`(매 턴 재적용 루프)는 재사용형 스킬을 명시적으로 건너뛰므로 그 15가 다시 채워지지도
   않는다.
5. 그래서 `ApplyCashBonus()`가 실행되는 시점엔 이미 `CashBonus == 0`— **단 한 턴도 실제 현금 증가에 기여하지
   못했다.**

사용자가 "케이스1(Job/토글형)도 매 턴 보유 현금의 %를 복리로 불리는 건 버프가 아니라 그냥 이자 아니냐"는
지적을 했다 — 맞는 말이라 Job/토글형 쪽 공식은 나중에 다시 설계하기로 하고, 재사용형 스킬 쪽만 먼저 고쳤다.

**수정 방향** : `CashBonus`를 매 턴 순환시키는 인프라(이월+감쇠)를 새로 만드는 대신, 재사용형 스킬의
Support/Growth와 동일한 "구매 시점 1회성" 철학을 그대로 따랐다 — 다만 Support/Growth처럼 감쇠하며 남아있을
방법이 없으니(`CashBonus`엔 이월/감쇠가 없음), **구매하는 순간 즉시 현금을 지급**하는 방식으로 바꿨다.
`SkillManager.HandlePurchase()`에 `GrantCashBonus(skill.Profile)` 호출을 추가했다 — 스킬의 `effects` 중
`CashBonus` 타입만 골라 `currentMoney × (값/100)`을 그 자리에서 바로 `AddMoney`한다. `ApplySkillUse`가 하던
`stat.CashBonus += 값` 자체는 그대로 남겨뒀다 (해가 없고, 구매 직후~다음 턴 사이 UI가 참고할 수도 있어서).

Job/토글형 스킬 쪽 `ApplyCashBonus()`(매 턴 보유 현금 대비 % 복리 증가)는 손대지 않았다 — "이자 문제"의 올바른
해법(고정액 버프? 거래 수익 배율? 등)이 아직 안 정해져서 별도 후보로 남겨뒀다.

### Job/토글형 스킬 CashBonus 재설계 — "이자"에서 "거래 수익 배율"로

바로 이어서 위 후보를 처리했다. "거래(Long/Short) 수익에 배율로 적용"으로 방향을 정했고, Long(매수)은 지출이라
"수익"이 아니므로 대상에서 제외했다.

**수정** :
- `MarketManager.NextTurn()`에서 `ApplyCashBonus()` 호출과 메서드 자체를 제거했다 (매 턴 보유 현금 전체에
  곱하던 옛 방식 폐기).
- `PlayerManager.HandleSellCoin()`이 판매 수익(`Amount × CurrentPrice`)에 그 순간의
  `CurrentStat.CashBonus`%를 배율로 곱해서 지급하도록 바꿨다 — `Revenue = Amount × CurrentPrice × (1 +
  CashBonus / 100)`.
- `PlayerStat.CashBonus` 자체(값이 매 턴 Job/토글형 스킬 효과로 다시 채워지는 방식)는 그대로 뒀다 — 그 값을
  "언제 어떻게 쓰는지"만 바꿨다.

이제 CashBonus는 실제로 거래를 해야만 체감되는 버프가 됐다 (가만히 있으면 늘지 않음).

공식은 `Game_Formula.md` 3장 "Short", 3-3장에 반영했다.

## 초반 이벤트 무조건 발생 + 스트리머 UI 힌트

UI 기획안(플레이 화면 스크린샷)을 보니 우측에 캐릭터 초상화+가짜 채팅으로 반응하는 "스트리머" 패널이 있었다.
"초기 이벤트가 발생하면 그게 켜지게 하자"는 요청을 받았는데, 확인해보니 이건 이전에 본 3개(개요/스킬/직업)
화면에는 없던 새 UI 요소였다.

**스트리머 패널** : 새 코드 필요 없음으로 결론. `스트리머_소개` 이벤트가 발생하면 `MarketManager.EventLog`에
그대로 기록되므로(이미 public), UI가 `스트리머_소개` `EventSO` 에셋 참조를 들고 있다가 `EventLog`에 그 항목이
있는지 확인하면 패널을 켤 수 있다.

**초반 이벤트 무조건 발생** : "거래소 상장", "스트리머 소개" 같은 이벤트는 확률에 맡기지 않고 게임 시작 직후
정해진 턴에 반드시 발생하게 해달라는 요청. 순서는 1턴 스트리머_소개, 2턴 거래소_상장으로 확정했다.

**구현** :
- `EventSO`에 `guaranteedTurn`(int, 기본 0) 필드 추가 — 0이면 기존처럼 확률 발생, N이면 해당 턴에 무조건 발생.
- `EventCalculator.Calculate()`에서 "골라진 EventSO를 stat에 적용하는 부분"을 `EventCalculator.Apply(stat,
  chosen)`으로 분리했다 — 랜덤 선택 경로(`Calculate`)와 무조건 발생 경로가 같은 적용 로직을 재사용하도록.
- `MarketManager.NextTurn()`이 매 턴 먼저 `FindGuaranteedEvent(turnCount)`로 이번 턴에 무조건 발생할 이벤트가
  있는지 찾는다. 있으면 `TriggerGuaranteedEvent()`(확률 체크 없이 `EventCalculator.Apply` 호출 + 로그 기록)로
  처리하고, 이번 턴의 기존 30턴 확률 체크는 건너뛴다(같은 턴에 이벤트가 두 번 겹치는 걸 방지).
- 로그 기록 부분(`runtimeEventData.Log.Add(...)`)이 `TriggerNewsEvent`/`TriggerGuaranteedEvent` 양쪽에서
  중복돼서 `LogEvent(EventSO)` 헬퍼로 뺐다.
- `스트리머_소개.guaranteedTurn = 1`, `거래소_상장.guaranteedTurn = 2`로 설정 (Unity MCP로 작업).

공식은 `Game_Formula.md` 4장 "발생 시점"/"EventSO — 이벤트 하나의 정의"에 반영했다.

## (곁가지) MCP for Unity 연결 트러블슈팅

이 세션에서 처음으로 Unity MCP가 연결됐는데, 그 과정에서 겪은 문제와 원인을 기록해둔다 (다음에 또 끊기면
참고용).

- 증상 : `unityMCP` 도구는 목록에 잡히는데 `mcpforunity://instances`가 계속 `instance_count: 0`을 반환.
- 원인 1 — **설정 중복** : 프로젝트 `.mcp.json`이 Claude Code용으로 `uvx ... mcp-for-unity --transport stdio`를
  직접 스폰하는 방식으로 되어 있었다. 이건 Unity 쪽 MCP 패널의 "Local Server"(Unity 자신이 8080 포트에 HTTP
  허브를 직접 띄우는 방식, `MCPForUnity.Editor.Services.ServerManagementService`)와 별개의 프로세스라, 둘 다
  8080을 쓰려고 하면 충돌한다. **해결** : `.mcp.json`을 `{"mcpServers": {"unityMCP": {"url":
  "http://127.0.0.1:8080/mcp", "type": "http"}}}`로 바꿔서, Claude Code가 별도 프로세스를 띄우지 않고 Unity가
  이미 띄운 서버에 클라이언트로 붙게 했다. (Unity 전역 VSCode 설정 `AppData\Roaming\Code\User\mcp.json`에는
  이미 이 형태로 올바르게 들어 있었다 — "Configure All Detected Clients"가 VSCode 계열 클라이언트용으로 써준
  것으로 보이며, 프로젝트 `.mcp.json`은 별도 경로로 만들어진 듯하다.)
- 원인 2 — **실수로 정상 프로세스를 kill함** : 디버깅 중 `netstat`으로 8080을 잡고 있는 PID를 "Claude Code가 띄운
  좀비 프로세스"로 오판해서 강제 종료했는데, 실제로는 Unity가 정상적으로 띄워서 이미 34개 도구까지 등록
  완료된 살아있는 허브 서버였다(`Library/MCPForUnity/Logs/server-launch-8080.log`에 그 증거가 남아있었다).
  이후 Unity가 WebSocket 연결 끊김/재연결 실패를 반복했다. **교훈** : 포트를 잡고 있는 프로세스를 죽이기 전에
  반드시 `Library/MCPForUnity/Logs/server-launch-8080.log`와 Unity의 `Editor.log`(`AppData\Local\Unity\Editor\
  Editor.log`)를 먼저 확인해서 그게 진짜 좀비인지 확인해야 한다.
- 최종 해결 순서 : Unity MCP 패널에서 Stop Server → Start Server로 허브를 재기동 → VSCode 창 리로드로 새
  `.mcp.json`(http 클라이언트 방식) 반영 → `mcpforunity://instances`에서 `instance_count: 1` 확인.

## 스트리머 반응(가격 변화 연동) 로직 설계

플레이 화면 우측 "스트리머" 패널이 지금은 `스트리머_소개` 이벤트 발생 여부로 패널을 켜는 것까지만 되어 있는데
(위 "초반 이벤트 무조건 발생 + 스트리머 UI 힌트" 절 참고), 가격이 오르내림에 따라 표정/멘트가 실시간으로
바뀌게 해달라는 요청을 받았다. 실제 스프라이트(`Assets/Sprites/스트리머상태` 폴더)는 확인해보니 폴더만 만들어져
있고 아직 UI 팀원이 이미지를 넣기 전이라, 이번엔 로직만 먼저 설계·구현했다.

**정해야 했던 것 두 가지** :
1. 반응 단계 수 — 스프라이트 개수와 직결되는 문제라 미리 정해야 했다. "5단계 이상(강도별)"로 확정.
2. 판정 기준 — `PriceCalculator`가 굴리는 `isUp`(방향 판정)을 그대로 쓸지, 실제 `CurrentPrice` 변화량(delta)
   기준으로 할지. `isUp`이 true여도 `delta`가 음수면(Growth/Support가 크게 마이너스일 때) 실제로는 가격이
   내려가는 경우가 있어("스트리머 반응 = 실제 체감 가격 변화"가 목적이므로), **실제 가격 변화량(delta) 기준**으로
   확정.

**구현** :
- `Assets/Scripts/Stat/StreamerReactionState.cs` 신규 : `Crash`/`Down`/`Neutral`/`Up`/`Surge` 5단계 enum
  (`EndingType.cs`와 동일한 스타일).
- `Assets/Scripts/Runtimes/Systems/StreamerReactionCalculator.cs` 신규 : `PriceChangeThisTurn`(절대값 delta)을
  받아 5단계 중 하나를 반환하는 static 클래스. 임계값(Surge >= 50, Up >= 10, Down <= -10, Crash <= -50)은
  `Game_Formula.md` 2장 정규 가격 변화의 이론상 범위(약 ±85~100)를 참고한 예시치 — 퍼센트가 아니라 절대값
  기준으로 잡았다. 게임 진행에 따라 가격 자체는 커져도 2장 공식상 한 턴의 정규 변화량은 Support/Growth/
  Scarcity 범위(-100~100)에 묶여 있어 절대값 기준이 흔들리지 않기 때문이다 (시사 이벤트의 `priceRatio` 충격은
  가격에 비례해 커질 수 있지만, 그것까지 포함해 "그 턴에 실제로 얼마나 움직였는가"를 그대로 보여주는 게
  스트리머 반응의 목적과 맞다고 판단).
- `PlayerStat`에 `PriceChangeThisTurn`(float)/`StreamerReaction`(`StreamerReactionState`) 필드 추가. 감쇠/이월
  없이 매 턴 새로 계산되는 UI 표시 전용 값 — `CashBonus`와 같은 성격.
- `MarketManager.NextTurn()`과 `HandleNewsEvent()`(수동 트리거) 양쪽에 계산 전 `CurrentPrice`를 기억해뒀다가
  계산 후와 비교해 `UpdateStreamerReaction()` 헬퍼로 두 필드를 갱신하는 코드를 추가했다. 새 `EventHub` 이벤트는
  만들지 않았다 — `PlayerStat` 전체가 이미 `OnMarketUpdated`로 나가므로 UI는 그 안의 두 필드만 읽으면 된다.

**남은 것** : 실제 UI(스프라이트 전환, 멘트 표시)는 UI 팀원 담당이라 만들지 않았다. 스프라이트가 들어오고
실제로 플레이해보면 임계값(50/10) 밸런스 조정이 필요할 수 있다 (`Next_Tesk.md` 참고).

공식은 `Game_Formula.md` 2-1장(신규)에 반영했다.

## 버그 수정 : PriceCalculator가 delta 부호를 그대로 반영해 방향이 뒤집히던 문제

라이브로 플레이 테스트를 돌리던 중 `Debug.Log`(사용자가 직접 delta/Growth/Support까지 찍도록 로그를 늘려둔
상태)를 보다가, Growth/Support가 둘 다 크게 마이너스(예: Growth -25, Support -16)인데도 `CurrentPrice`가
매 턴 계속 오르는 걸 발견했다.

**원인** : `PriceCalculator.Calculate()`의 `delta = Growth*0.6 + Support*0.25 + Scarcity*0.15`는 Growth/Support가
음수면 `delta` 자체가 음수가 될 수 있는데, 코드는 이 값을 부호 없는 "변화 크기"로 취급해서
`isUp ? CurrentPrice += delta : CurrentPrice -= delta`를 하고 있었다. `isUp == false`(하락 방향)인데 `delta`가
음수면 `CurrentPrice -= (음수)` = `CurrentPrice + |delta|`가 되어 오히려 가격이 오른다 — 방향 판정과 실제 결과가
정반대로 나오는 버그였다. `Game_Formula.md` 2장이 애초에 "가격 변화량은 방향성과 별도로 계산된다"고 명시하고
있었는데, 실제 코드는 그 크기(magnitude) 계산에 Growth/Support의 부호가 그대로 새어 들어가 있어 문서 의도와
어긋나 있었다.

**수정** : `delta` 계산에 `Mathf.Abs()`를 씌워 순수 크기로만 쓰도록 고쳤다 (`PriceCalculator.cs`). 방향은 이제
오직 `isUp`(1장의 `UpProbability` 굴림) 하나로만 결정된다.

**영향** : 이번 스트리머 반응 작업에서 "실제 가격 변화량(delta) 기준"으로 반응을 판정하기로 한 것과 별개로,
이 버그 자체가 게임 밸런스에 직접 영향을 준 심각한 문제였다 — Support/Growth를 열심히 올려도 방향 판정과
무관하게 가격이 뒤죽박죽으로 움직였을 것이다. 수정 후에는 `UpProbability`가 낮으면 실제로 가격이 내려가는
빈도가 높아진다.

공식은 `Game_Formula.md` 2장에 반영했다 (delta는 절대값, 방향은 isUp만으로 결정한다는 점을 명시).

## 버그 수정 : 코인 가격이 시작부터 0원 근처로 떨어져 못 벗어나던 문제

위 delta 부호 버그를 고치고 나서, 사용자가 "코인 가격이 너무 빨리 0원이 되어버린다"는 걸 지적했다. 확인해보니
별개의 구조적 문제 두 개가 겹쳐 있었다.

1. **`CurrentPrice` 초기값이 없었다** : `MarketManager.Awake()`가 `CurrentStat = new PlayerStat();`만 하고
   `CurrentPrice`를 따로 설정하지 않아서, 게임 시작 시점 가격이 C# 기본값인 **0원**이었다.
2. **가격 하한선이 없었다** : `PriceCalculator`/`EventCalculator` 어디도 `CurrentPrice`가 0 이하로 못 내려가게
   막지 않았다. 게다가 매 턴 변동폭(`delta`)이 가격 크기와 무관한 절대값(대략 0~100)이라, 가격이 0 근처에서
   시작하면 "하락" 판정 한 번만으로도 가격이 마이너스로 꺼져버리고, 한 번 꺼지면 이벤트의 `priceRatio`(가격에
   곱하는 충격)도 0 근처 값에 곱해져 사실상 무력화되어 회복이 잘 안 되는 상태였다.

**정해야 했던 값** : 초기 가격과 하한선 둘 다 임의의 밸런스 수치라 사용자에게 확인했다. **초기 가격 1000원**,
**하한선 1원**으로 확정.

**구현** :
- `MarketManager`에 `InitialPrice`(1000f) 상수를 추가하고, `Awake()`에서 `CurrentStat` 생성 직후
  `CurrentStat.CurrentPrice = InitialPrice;`로 설정한다.
- `PriceCalculator`에 `MinPrice`(1f) 상수와 `ClampPrice(PlayerStat stat)`(`Mathf.Max(MinPrice, CurrentPrice)`)를
  추가했다. `PriceCalculator.Calculate()`가 정규 가격 변화를 반영한 직후, `EventCalculator.Apply()`가 이벤트
  `priceRatio` 충격을 반영한 직후 양쪽 모두에서 이 메서드를 호출해 가격이 절대 1원 밑으로 안 내려가게 막는다.
  (중복 상수 대신 `PriceCalculator.MinPrice`/`ClampPrice`를 `EventCalculator`가 그대로 재사용 — Job/Skill이
  `StatCalculator.ApplyEffect`를 공유하는 것과 같은 패턴.)

공식은 `Game_Formula.md` 2장에 반영했다 (초기 가격/하한선 값 명시).

## 캔들스틱 가격 차트 UI 구현

목업 이미지를 기준으로 플레이 화면 중앙의 캔들스틱 차트를 만들어달라는 요청을 받았다. 이번엔 "UI 쪽은 팀원
담당"이라는 기존 원칙과 별개로 사용자가 UI 작업(씬 GameObject 구성 포함)까지 직접 해달라고 명시적으로
요청했다 — 팀원과 사전에 합의됐다고 확인받았다.

### 작업 전 : 씬 오브젝트 관계 문서화

코드를 짜기 전에 Unity MCP로 씬(`Main_Canvas` 하위 트리)을 전부 조회해 `Scene_Hierarchy.md`(신규)에 정리했다.
`RightPanel`(TimePanel/TradePanel/SettingsBtn), `CoinControlPanel`(발행량 조작 오버레이),
`SupportPanel`/`IncreaseScorePanel`/`DoubtScorePanel`(하단 스탯 게이지 3종), `SettingsPanel`, `SkillBtn` 7개가
`Main_Canvas` 직계 자식이었고, "그래프/차트" 관련 오브젝트는 씬에 전혀 없었다. `CoinControlPanel`의 가로 폭
(1414)과 좌우 정렬을 그대로 재사용하고, 그 위쪽부터 캔버스 상단까지의 빈 공간(y: 375~1060)에 차트를 배치하기로
계산했다 (자세한 좌표는 `Scene_Hierarchy.md` 참고).

### 데이터 설계 : 캔들 1개 = 턴(하루) 1개

"1턴 = 1일" 기획을 그대로 살려 캔들 1개를 그 턴의 Open(턴 시작 전 `CurrentPrice`)→Close(턴 계산 후
`CurrentPrice`)로 정의했다. 목업 이미지의 캔들도 심지(High/Low) 없이 단순 사각 막대였어서 Open-Close 몸통만
그리기로 하고 별도 High/Low 추적은 만들지 않았다.

- `Assets/Scripts/Stat/Enums/PricePoint.cs`(신규) : `Date`/`Open`/`Close`만 가진 캔들 1개 데이터.
- `Assets/Scripts/Stat/RuntimeData/RuntimePriceHistory.cs`(신규) : `List<PricePoint>` 보관, `RuntimeEventData`와
  동일한 패턴.
- `MarketManager`에 `PriceHistory`(`IReadOnlyList<PricePoint>`) 프로퍼티를 추가하고, `NextTurn()`이 이미 갖고
  있던 `priceBefore`(스트리머 반응 계산용으로 이미 기억해두던 값)를 그대로 재사용해 `LogPricePoint()`로 매 턴
  캔들 하나씩 기록한다. 시사 이벤트 수동 트리거(`HandleNewsEvent`)는 턴을 넘기지 않으므로 캔들을 만들지 않는다
  (1턴=1캔들 유지).
- 이력은 잘라내지 않고 전부 보관한다 — 수천 턴이 지나도 `List<PricePoint>` 메모리 부담은 무시할 수준이라
  캡을 두는 건 불필요한 복잡도라고 판단했다.

### 렌더링 : 오브젝트 풀링 + 자동 스케일링

`Assets/Scripts/UI/PriceChartUI.cs`(신규)를 만들었다. `TimeUI`/`PlayerUI`처럼 Manager를 직접 폴링하는 구식
패턴 대신, `PROJECT_ARCHITECTURE.md`가 정의한 "UI는 EventHub만 구독" 원칙대로 `EventHub.OnMarketUpdated`를
구독해 매 턴 다시 그린다.

- 이력 전체가 아니라 최근 `visibleCandleCount`개(기본 14, Inspector로 조절 가능)만 그린다. `Awake()`에서 Image
  GameObject를 미리 14개 만들어두고(오브젝트 풀링), `Redraw()`는 이 풀의 위치(`anchoredPosition`)/크기
  (`sizeDelta`)/색상만 갱신한다 — 이력이 수천 턴 쌓여도 매 프레임 부담이 없다.
  프리팹 없이 코드로 직접 `new GameObject(...)`해서 만든다 (에셋 하나 추가하는 것보다 단순).
- Y축은 화면에 보이는 캔들들의 Open/Close 최소~최대 값 기준으로 매번 자동 스케일링한다 (위아래 10% 여백).
  X축은 왼쪽부터 오래된 캔들을 채우고, 이력이 `visibleCandleCount`를 넘으면 오래된 캔들이 왼쪽으로 밀려나며
  사라지는 슬라이딩 윈도우 방식이다.
- 색상은 `BtnLong`(#4B692F)/`BtnShort`(#AC3232)와 동일한 값을 그대로 재사용해 기존 거래 버튼과 색 통일감을
  맞췄다.
- 이번 범위에서 제외한 것 : X축 날짜 라벨, 상단 코인명/현재가 헤더, 스트리머 패널 스프라이트 연결. 셋 다
  대응하는 UI 오브젝트가 씬에 아직 없어서(헤더/스트리머 패널) 또는 우선순위 밖(날짜 라벨)이라 이번 작업에서는
  만들지 않았다 (`Next_Tesk.md` 참고).

### 씬 배치 및 검증

Unity MCP로 `Main_Canvas` 하위에 `ChartPanel`(RectTransform + Image + `PriceChartUI`) GameObject를 생성해
`Scene_Hierarchy.md`에서 계산한 위치(anchoredPosition -244.48, 172.5 / sizeDelta 1414.48 x 674)로 배치하고
씬을 저장했다. Play 모드에 진입해 배속을 8배로 올리고 실제로 며칠 지나가는 걸 스크린샷으로 확인했다 — 캔들이
좌에서 우로 순서대로 쌓이고, 상승/하락에 따라 초록/빨강이 정확히 갈리며, Y축 자동 스케일링과 슬라이딩 윈도우
모두 의도대로 동작함을 확인했다. 검증 스크린샷은 확인 후 삭제했다 (`Assets/Screenshots/` 폴더 자체는 남음).

플레이 중 `SkillManager.cs:43`에서 발생한 `NullReferenceException`을 콘솔에서 발견했는데, 이번에 건드리지 않은
기존 코드에서 나는 에러라 이번 작업과 무관한 별개의 기존 버그로 판단해 손대지 않았다 (`Next_Tesk.md`에 후보로
남김).

### 후속 요청 : 코인명/가격 헤더 + 차트 모서리 둥글게

같은 세션에서 이어서 두 가지를 추가로 요청받았다 — 목업의 좌측 상단 "코인명 + 현재가" 표시, 그리고 차트
패널을 다른 패널들처럼 둥근 모서리로 바꿔달라는 것.

- **차트 모서리** : 기존 패널들이 쓰는 `Nobi.UiRoundedCorners.ImageWithRoundedCorners` 컴포넌트를 `ChartPanel`에
  그대로 추가했다. 재질(`UI/RoundedCorners/RoundedCorners`)은 에셋으로 존재하지 않고 컴포넌트가 런타임에 직접
  생성해 `Image.material`에 꽂아준다는 걸 확인했다 — 다른 패널들의 `materialForRendering` 인스턴스ID가 모두
  음수(런타임 생성 오브젝트)였던 것으로 짐작했고, `radius`(30, `CoinControlPanel`과 동일)만 지정하면 나머지는
  컴포넌트가 알아서 처리했다.
- **코인명/가격 헤더** : `Assets/Scripts/UI/CoinPriceHeaderUI.cs`(신규)를 만들었다. `PriceChartUI`와 동일하게
  `EventHub.OnMarketUpdated`를 구독해 `MarketManager.CurrentStat.CurrentPrice`를 `"BitBitCoin(BBIT)  ₩1,000"`
  형식으로 표시한다. 목업은 "$"였지만 이 프로젝트의 다른 화면(`PlayerUI` 등)이 전부 "₩"를 쓰고 있어 통화 표기를
  기존 관례에 맞춰 통일했다.
- 헤더는 목업처럼 차트 위쪽에 겹치지 않는 별도 줄로 배치하기 위해 `ChartPanel`의 세로 크기를 살짝 줄이고
  (기존 674 → 575, 상단에 헤더 자리 확보) 그 위 공간에 `CoinPriceHeader`(아이콘 + 텍스트) GameObject를 새로
  만들어 배치했다. 배경 패널 없이 아이콘+텍스트만 캔버스 배경 위에 직접 떠 있는 형태로, 목업과 동일하다.
- Play 모드로 실제 가격이 갱신되며 헤더 숫자가 바뀌는 것과 차트 모서리가 둥글게 렌더링되는 것을 스크린샷으로
  확인했다. 이 과정에서 에디터 창이 포커스를 잃으면(`EditorApplication.isFocused == false`) 8배속을 걸어도
  실제 게임 시간이 거의 안 흐르는(프레임이 크게 throttle되는) 현상을 발견했다 — 코드 버그가 아니라 Unity
  에디터가 백그라운드일 때 프레임을 줄이는 동작이라, 다음에 Play 모드로 검증할 땐 대기 시간을 넉넉히 잡아야
  한다는 걸 기록해둔다.

### 캔들 간격 좁히기

목업과 비교해 캔들 사이 간격이 넓다는 피드백을 받고 `PriceChartUI.candleWidthRatio`를 0.6 → 0.85로 올렸다
(슬롯 폭 대비 캔들 몸통이 차지하는 비율 — 값이 클수록 옆 캔들과의 틈이 좁아진다). 씬에 이미 저장된
`ChartPanel` 컴포넌트 값도 함께 갱신했다.

검증 중 에디터가 포커스를 잃은 상태에서 Play 모드 진입 직후 "playmode_transition" 상태에 멈춰
`TimeManager`의 `Update()`가 전혀 진행되지 않고 `manage_camera` 스크린샷도 그 시점의 정지 프레임만 반환하는
현상을 겪었다 (배속을 8배로 걸어도 게임 날짜가 전혀 안 흐름). 코드 문제인지 확인하려고 `execute_code`로
`MarketManager.Instance.NextTurn()`을 직접 20회 호출해 턴을 강제로 진행시켰더니 `PriceHistory.Count`가
정상적으로 20까지 올라갔고, 런타임에 생성된 `Candle_0`/`Candle_1` GameObject의 `RectTransform`을 직접 조회해
슬롯 폭(101.03px) 대비 캔들 폭(85.88px)이 새 비율(0.85)대로 정확히 반영된 것을 확인했다 — 로직 자체는
정상이고, 화면 캡처만 멈춰있던 것으로 결론지었다. **다음에 비슷한 상황(스크린샷이 하나도 안 바뀜)을 겪으면**
스크린샷에만 의존하지 말고 `execute_code`로 관련 오브젝트의 컴포넌트 값을 직접 조회해서 로직과 렌더링
문제를 구분할 것.

## 캔들 차트 후속 작업 : X축 날짜 라벨 + 호버 툴팁

`Next_Tesk.md`에 후보로 남겨뒀던 캔들 차트 후속 작업을 이어서 진행했다.

- **X축 날짜 라벨** : `PriceChartUI`가 캔들과 동일한 방식(오브젝트 풀링)으로 각 캔들 아래에 `TextMeshProUGUI`
  라벨을 만들어 `"MM/dd"` 형식으로 날짜를 표시한다. 차트 영역 하단 `dateLabelAreaHeight`(32px)만큼을 라벨
  전용 공간으로 비워두고, 캔들은 그 위쪽 영역에만 그리도록 `Redraw()`의 Y 계산에 오프셋을 추가했다.
- **버그 발견** : `DateTime.ToString("MM/dd")`처럼 형식 문자열에 `/`를 직접 쓰면, 실제 출력되는 구분자는
  리터럴 슬래시가 아니라 **현재 시스템 문화권(Culture)의 날짜 구분자**로 치환된다는 걸 Play 모드 검증 중
  발견했다 — 이 환경에서는 `/` 대신 `-`가 나와 "01/01"이 "01-01"로 표시되고 있었다. `CultureInfo.InvariantCulture`를
  쓰거나 직접 포맷하는 방법 중, 별도 라이브러리 using 추가 없이 가장 단순한 `$"{date.Month:00}/{date.Day:00}"`
  형태의 `FormatDate()` 헬퍼를 만들어 고정했다. 캔들 호버 툴팁에도 동일한 함수를 재사용한다.
- **호버 툴팁** : 캔들 위에 마우스를 올리면 그 캔들의 날짜/시가/종가("01/04  Open ₩2,098 → Close ₩2,140")를
  차트 좌상단에 띄운다. `PriceChartUI` 내부에 `CandleHoverTarget`(`IPointerEnterHandler`/`IPointerExitHandler`
  구현) private nested class를 만들어 각 캔들 GameObject에 붙이고, 캔들의 `Image.raycastTarget`을
  `false`→`true`로 바꿔 포인터 이벤트를 받게 했다. 어떤 풀 슬롯(index)이 지금 어떤 `PricePoint`를 표시
  중인지는 `Redraw()`가 매번 채우는 `boundPoints` 리스트로 추적한다 — 풀링 구조상 슬롯 i가 매 턴 다른
  데이터를 가리키므로, 생성 시점 값을 캡처해두는 방식은 쓸 수 없었다.
- **밸런스 동기화** : 사용자가 직접 스크립트 필드 기본값을 `visibleCandleCount` 14→16, `candleWidthRatio`
  0.85→0.95로 조정했다. 스크립트 기본값과 별개로 씬에 이미 저장된 `ChartPanel` 컴포넌트 값은 명시적으로
  덮어써야 반영되므로(스크립트 기본값 변경만으로는 기존 씬의 직렬화된 값을 갱신 못함) Unity MCP로 같이
  동기화했다.
- **검증 트러블슈팅** : 스크립트를 Play 모드 도중에 수정하면 Unity가 플레이 상태를 유지한 채 도메인 리로드를
  수행하는데, 이 과정에서 `EventHub`의 static 이벤트나 각 Manager의 `Instance` 정적 필드가 일시적으로
  초기화 순서가 꼬여 `NullReferenceException`/`ArgumentOutOfRangeException`이 발생하는 걸 겪었다 — 실제
  게임 로직 버그가 아니라 "플레이 중 스크립트 편집"이라는 비정상적인 테스트 절차 자체의 부작용이었다.
  **교훈** : 스크립트를 고칠 일이 생기면 반드시 Play 모드를 완전히 종료한 뒤 편집하고, 컴파일이 끝난 걸
  확인한 다음 새로 Play 모드에 진입해서 검증할 것.

### 헤더 우측 아이콘 — 햄버거 메뉴로 잘못 만들었다가 되돌림

목업에서 코인명/가격 헤더 우측에 있는 아이콘을 "햄버거 메뉴 → 설정창 열기"로 오해하고, `SettingsUI`에
`static Instance`를 추가하고 `OpenSettings()`를 public으로 바꾼 뒤 `HamburgerMenuButton.cs`(신규)를 만들어
`game-icons_hamburger-menu` 아이콘 버튼을 헤더 우측에 배치했었다. Play 모드에서 클릭 시 실제로 설정창이
열리는 것까지 확인했는데, 사용자가 스크린샷을 보여주며 그 아이콘은 설정이 아니라 **이벤트 로그(개요) 전체화면
패널로 넘어가는 버튼**이고 아이콘도 "UI뉴스아이콘"을 써야 한다고 정정했다.

이벤트 로그 패널 자체가 씬에 아직 없어서(확인 완료 — `find_gameobjects`로 검색해도 없음) 지금 당장 그
패널까지 만드는 건 이번 범위를 벗어난다고 판단해, 잘못 만든 햄버거 버튼(`MenuBtn` GameObject,
`HamburgerMenuButton.cs`)과 `SettingsUI`의 관련 변경(`Instance`, `OpenSettings` public화)을 전부 원상복구했다.
아이콘/연결 대상 정보는 `Next_Tesk.md`의 "이벤트 로그 패널" 항목에 남겨서, 나중에 그 패널을 실제로 만들 때
헤더 우측에 "UI뉴스아이콘"으로 진입 버튼을 놓으면 된다는 걸 알 수 있게 했다.

공식/설계는 `Game_Formula.md` 2-2장에 반영했다.

## 이벤트 로그 패널 진입 버튼

정정받은 대로 헤더 우측 자리에 "이벤트 로그 패널로 넘어가는 버튼"을 다시 만들었다. 다만 이번엔 진입 버튼만
요청받았고 이벤트 로그 패널 본체(개요/커뮤니티 탭, X 닫기, 카드 리스트)는 만들지 않았다 — 아직 씬에 그
패널이 없는 상태에서 버튼만 먼저 준비해두는 게 범위에 맞다고 판단했다.

- `Assets/Scripts/UI/EventLogButton.cs`(신규) : `Awake()`에서 `offSprite`로 초기화하고, 클릭할 때마다
  `onSprite`/`offSprite`를 토글한다. 스킬 아이콘 클릭이 `RuntimeSkillData.SelectedSkillId`만 저장하고 끝나는
  것과 비슷하게, 지금은 시각적 토글만 하고 실제로 패널을 열고 닫는 로직은 없다 — 패널이 생기면 `Toggle()`
  안에서 패널 `SetActive`도 같이 처리하면 된다.
  - `햄버거 버튼` 실수 이후 이번엔 아이콘부터 정확히 확인했다 — `Assets/Sprites/UI아이콘/UI뉴스아이콘_on.png`/
    `_off.png` 두 상태가 이미 있었다 (다른 스킬/발행량 조작 버튼들도 이런 두 상태 아이콘 패턴을 이미 쓰고
    있음).
- 씬에 `EventLogBtn` GameObject를 헤더 우측(x:1383, y:1010 근방 — 이전 햄버거 버튼과 같은 자리)에 배치했다.
- Play 모드에서 `Button.onClick.Invoke()`를 두 번 호출해 `off → on → off` 토글이 스프라이트 레벨까지 정확히
  반영되는 걸 확인했다.

공식/설계는 `Game_Formula.md` 2-2장에 반영했다.

## 가격 상승 확률이 항상 100%(`확률:1`)로 찍히는 문제 수정

플레이 로그(`PriceCalculator.Calculate`의 디버그 출력)에서 `확률:1`이 여러 턴 연속으로 찍히는 게 이상하다는
제보를 받고 `ProbabilityCalculator`를 확인했다.

`CalculateScore`가 `score += stat.Support * 0.01f; score += stat.Growth * 0.01f;`처럼 `Game_Formula.md`의
`ws`/`wg` 가중치를 사실상 1.0으로 하드코딩하고 있었다. 문제는 Support/Growth가 각각 -100~100 범위인데, 가중치가
1.0이면 **둘의 합이 50만 넘어도** `score = 0.5 + Support/100 + Growth/100`이 1.0을 초과해 `Clamp01`에 걸려버린다는
점이다. 거래(Long 1회당 ±0.1) 몇 번이나 시사 이벤트 한 번만으로도 쉽게 도달하는 수준인데, `decayRate=0.995`
(반감기 약 138턴)로 감쇠가 아주 느려서 한 번 포화되면 수십~수백 턴 동안 확률이 100%에 그대로 고정된 채 보였다.

`ws`/`wg`를 0.5로 낮춰(`ProbabilityCalculator.SupportWeight`/`GrowthWeight` 상수 신설) 포화에 필요한 Support+Growth
합의 임계값을 50 → 100으로 올렸다. 코드에는 이미 있었지만 `Game_Formula.md` 1장 공식에는 빠져 있던 Doubt 차감
항(`wd`, 기존 동작 그대로 1.0 유지, 동작 변경 없음)도 이번에 문서에 반영해 공식과 코드를 일치시켰다.

Play 모드에서 `MarketManager.Instance.NextTurn()`을 `execute_code`로 반복 호출해 검증했다 — 수정 전 재현 조건과
비슷하게 거래로 Support/Growth를 50 안팎까지 올린 뒤 턴을 진행시키자, 수정 전이라면 즉시 1.000에 고정됐을
상황에서 확률이 0.774 → 0.998 → 0.995 → 0.993 → 0.990 → 0.988처럼 자연스럽게 변동하며 서서히 감쇠하는 것을
확인했다.

공식은 `Game_Formula.md` 1장에 반영했다.

## Long/Short 거래 버튼 + Support/Growth/Doubt 게이지 연결

`Next_Tesk.md`의 "후보 : UI 연결" 중 Long/Short 거래 버튼과, 그동안 손대지 않았던 좌측 상단 3종 게이지(Support/
Growth/Doubt)를 연결했다.

**TradePanel(`PlayerUI.cs`, 기존 파일 확장)** — `BtnPlus1/10/100/MAX`가 하나의 `tradeAmount`를 공유하고
`BtnLong`/`BtnShort` 둘 다 그 수량으로 실행되는 구조라, `BtnPlusMAX`가 뭘 의미해야 하는지와 거래 후 수량을
어떻게 할지 사용자에게 먼저 물어봤다 — "현재 현금으로 살 수 있는 최대 수량(Long 기준)", "거래 후 0으로
초기화"로 답을 받고 그대로 구현했다. `PlayerManager.HandleBuyCoin`/`HandleSellCoin`을 다시 확인해보니 잔액/
보유량 검증이 전혀 없어서(무조건 반영) Long/Short를 그냥 연결하면 현금이나 코인이 마이너스로 빠질 수 있었다.
`PlayerManager` 쪽 로직을 고치는 건 이번 범위를 벗어난다고 판단해, 대신 UI에서 `Update()`마다 `BtnLong`은
`amount×price <= currentMoney`, `BtnShort`는 `amount <= currentCoins`일 때만 `interactable=true`가 되도록
막았다 (발행량 조작 버튼을 스킬 해금 여부로 비활성화하기로 했던 기존 방침과 동일한 패턴).

**Support/Growth/Doubt 게이지(신규 `StatGaugeUI.cs`)** — 씬을 까보니 슬라이더가 아니라 `BaseWhiteBar`(트랙) +
`PositiveBar`/`NegativeBar`(Filled 타입 Image) + `REDBAR`(중앙 고정 마커, 손댈 필요 없음) 조합이었다.
`PositiveBar`는 fillOrigin=Left로 패널 중앙에서 오른쪽으로, `NegativeBar`는 fillOrigin=Right로 중앙에서
왼쪽으로 채워지는 구조(각각 절반 폭)라 `Mathf.Max(0, value)/100`, `Mathf.Max(0, -value)/100`로 매핑했다.
Doubt 패널은 애초에 `NegativeBar`가 없고 `PositiveBar`가 패널 전체 폭(0~100)이라, `negativeBar`를
Inspector에서 비워두면 자동으로 "전체 폭 하나만 채우는" 모드로 분기하도록 만들어 Support/Growth/Doubt 세
패널에 같은 컴포넌트를 재사용했다. 값 텍스트는 기존 씬에 이미 있던 "+0" placeholder를 보고 부호를 항상
표시하는 포맷("+34", "-12")으로 맞췄다.

Play 모드에서 검증하는 도중 `MarketManager.Instance.NextTurn()`을 직접 호출한 직후 게이지가 갱신 안 되는
것처럼 보이는 순간이 있었는데, 원인을 파고들어보니 그 시점에 Play 모드가 이미 (테스트 스크립트 밖에서)
정지된 상태였다 — 정지된 플레이 세션의 `MarketManager` C# 객체가 좀비 상태로 계속 `NextTurn()`을 실행해
`CurrentStat` 필드는 계속 바뀌는데, 실제 씬의 UI 오브젝트는 이미 에디터 상태로 되돌아가 있어서 발행되는
`EventHub.OnMarketUpdated`에 아무도 구독하고 있지 않았던 것. Play 모드를 다시 깨끗하게 시작해서 재현했더니
구독자 수(5개)와 게이지 갱신(`fill=0.3475, text="+35", raw=34.75` 등)이 정확히 일치함을 확인했다 — 실제
버그는 아니었다.

공식/구조는 `Game_Formula.md` 3장, `PROJECT_ARCHITECTURE.md`에 이미 있는 내용을 그대로 따랐다 (신규 공식
추가 없음).

## Support/Growth/Doubt 상한선 초과 버그 수정

게이지를 연결하고 나니 사용자가 "Support/Growth/Doubt가 -100~100(Doubt는 0~100) 범위를 넘어서 찍힌다"고
제보했다. 코드를 훑어보니 `TradeCalculator.Long/Short/ManipulateSupply`, `StatCalculator.ApplyEffect`(Job/
Skill/이벤트가 공유하는 함수), `EventCalculator` 등 값을 가감하는 모든 경로 중 어디에도 클램프가 없었다 —
`StatGaugeUI`가 게이지 막대 길이를 `Clamp01`로 누르고 있어서 막대는 안 터졌지만, 텍스트와 실제 계산에 쓰이는
원본 값(`PlayerStat.Support` 등)은 그대로 범위를 벗어난 채였다. 실제로 아주 큰 수량으로 Long을 몇 번만 해도
Support/Growth가 수백~수천까지 쉽게 올라갔다.

수정 지점을 어디로 할지 고민했다. 가감이 일어나는 모든 지점(`TradeCalculator`, `StatCalculator.ApplyEffect`,
`EventCalculator`)마다 개별적으로 클램프를 넣는 건 산발적이고 놓치기 쉬워서, 대신 "값이 실제로 화면에 나가기
직전"인 `EventHub.OnMarketUpdated` 발행 지점 한 곳(정확히는 두 곳 — 자동 턴 `NextTurn()`과 수동 시사 이벤트
`HandleNewsEvent()`)에서 그 턴에 있었던 모든 변경(감쇠+Job+Skill+이벤트+Doubt 자동 상승)이 끝난 뒤 한 번에
클램프하기로 했다. 신규 `StatCalculator.ClampStat(stat)`을 추가해 두 지점 모두 `EventHub.RaiseMarketUpdated`
호출 직전에 넣었는데, `NextTurn()`에서는 `ProbabilityCalculator`/`PriceCalculator`가 Support/Growth를 그대로
읽어 확률/가격 변동폭을 계산하므로 그 계산 전에(이벤트 처리 다음, 확률 계산 이전) 클램프를 넣어야 값이 크게
부풀려진 채로 가격에 영향을 주지 않는다는 점도 함께 고려했다.

Play 모드에서 `EventHub.RaiseBuyCoin(999999999)`처럼 극단적인 수량으로 검증했다 — 클램프 전이었다면
Support/Growth가 수만 단위로 튀었을 상황에서 정확히 100/-100에서 멈췄고, 발행량 조작으로 Doubt를 100 이상으로
밀어붙이자 100에서 멈추면서 체포 엔딩(`IsGameOver=true`)까지 정상적으로 발동하는 것을 확인했다.

공식은 `Game_Formula.md`가 이미 정의한 범위(Support/Growth -100~100, Doubt 0~100)를 그대로 따랐다 — 문서
변경 없음, 코드가 문서를 못 따라가고 있던 걸 맞춘 것.

---

## 현재 아키텍처 요약

```
Manager
 ↓
RuntimeData
 ↓
Systems (Calculator)
```

- **Manager**: TimeManager, MarketManager, SkillManager, JobManager, PlayerManager — 게임 상태를 관리하며 `EventHub`를 구독한다.
- **RuntimeData**: PlayerStat, RuntimeSkillData, RuntimeJobData, RuntimeEventData, RuntimePriceHistory — 현재 상태와 계산 결과를 저장한다.
- **Systems**: StatCalculator, ProbabilityCalculator, PriceCalculator, TradeCalculator, EventCalculator — 상태를 변경하지 않고 계산만 수행한다 (단, TradeCalculator/EventCalculator는 `PlayerStat`을 인자로 받아 그 자리에서 값을 직접 갱신한다).
- **EventHub**: UI ↔ Manager 사이의 이벤트를 중계한다.
