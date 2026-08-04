# 이슈 : 발행량(Supply) 방향/하한 버그 2건 수정

작성일 : 2026-08-04

관련 구현 : `Assets/Scripts/Runtimes/Systems/TradeCalculator.cs`,
`Assets/Scripts/Runtimes/Systems/StatCalculator.cs`, `Assets/Scripts/manager/MarketManager.cs`,
`Assets/Scripts/manager/PlayerManager.cs`

## 증상 (사용자 제보)

1. 발행량이 턴이 지날수록 늘어나야 하는데, 실제로는 줄어들고 있었다.
2. 발행량 조작(소각)을 반복하면 발행량이 마이너스가 될 때가 있었다 — 최소 발행량은 플레이어가 보유한
   코인수만큼은 되어야 한다.

## 1. 발행량 자동 증가

### 원인

바로 이전 세션에서 "`TradeCalculator.SupplyDecayRate` 상수가 선언만 되고 실제로 안 쓰여서 Supply가 전혀
감쇠하지 않는다"는 걸 버그로 보고 `Decay()`에 `Supply *= SupplyDecayRate`를 추가해뒀었다(Support/Growth와
동일하게 0을 향해 감쇠). 그런데 실제 의도는 반대였다 — 발행량은 시간이 지날수록 **늘어나야** 하는
게 맞는 설계였다.

### 수정

- `TradeCalculator.cs` : `SupplyDecayRate` 삭제. 대신 `SupplyGrowthPerTurn = 50f` 상수와
  `GrowSupply(PlayerStat stat)` 메서드를 추가해 매 턴 고정량만큼 Supply를 더한다(채굴/인플레이션 개념).
- `StatCalculator.Calculate()` : `TradeCalculator.Decay(stat)` 바로 다음에 `TradeCalculator.GrowSupply(stat)`
  호출 추가.

```csharp
// TradeCalculator.cs
private const float SupplyGrowthPerTurn = 50f; // ponytail: 밸런스용 임시 수치, 플레이 후 조정 필요

public static void GrowSupply(PlayerStat stat)
{
    stat.Supply += SupplyGrowthPerTurn;
}
```

### 검증 (Unity MCP Play 모드)

```
turn 1: Supply=11900
turn 2: Supply=11650   ← 시사 이벤트(1턴차 guaranteed event)가 supplyDelta로 한 번 깎음
turn 3: Supply=11700   (+50)
turn 4: Supply=11750   (+50)
```

3턴차부터 정확히 +50/턴으로 증가하는 것 확인.

## 2. 발행량 하한 = 보유 코인수

### 원인

`TradeCalculator.ManipulateSupply`(발행량 조작)는 amount를 그대로 `Supply += amount`로 더할 뿐 하한 체크가
없었다. `추가발행권한` 스킬 해금 후 소각(음수 amount)을 반복하면 Supply가 그대로 마이너스까지 내려갔다.

### 수정

`StatCalculator.ClampStat`(Support/Growth/Doubt 범위 강제와 동일한 지점, 매 턴/거래/스킬구매 확정 직후
호출됨)에 Supply 하한을 추가했다.

```csharp
// StatCalculator.cs
public static void ClampStat(PlayerStat stat)
{
    stat.Support = Mathf.Clamp(stat.Support, -100f, 100f);
    stat.Growth = Mathf.Clamp(stat.Growth, -100f, 100f);
    stat.Doubt = Mathf.Clamp(stat.Doubt, -100f, 100f);
    stat.Supply = Mathf.Max(stat.Supply, PlayerManager.Instance.currentCoins);
}
```

`MarketManager.HandleManipulateSupply`에서는 `ClampStat`이 **소각 반영 후, 코인 차감 전** 순서로 호출되고
있어서 "방금 줄어든 최신 보유량"이 아니라 "소각 직전 보유량" 기준으로 하한이 잡히는 순서 버그가 있었다.
`PlayerManager.Instance.AddCoin(amount)` → `StatCalculator.ClampStat(CurrentStat)` 순서로 바꿔서 고쳤다.

### 부가로 발견한 연쇄 버그 : 보유 코인 자체가 마이너스로 내려감

위 수정을 검증하던 중, `PlayerManager.AddCoin`도 하한이 없어서 소각을 계속 반복하면 **보유 코인수 자체가
마이너스**로 내려가는 걸 발견했다(매도는 `TradeModalUI`가 이미 보유량 이하로 막아두지만, 발행량 조작 UI는
그런 제한이 없음). Supply 하한이 `currentCoins`를 참조하는데 그 값 자체가 깨지면 하한도 같이 깨지므로 함께
고쳤다.

```csharp
// PlayerManager.cs
public void AddCoin(long amount)
{
    currentCoins = System.Math.Max(0L, currentCoins + amount);
}
```

### 검증 (Unity MCP Play 모드, 발행량 조작 -2000을 8회 반복)

```
before burns: Supply=11750 coins=10000
burn #1: Supply=9750  coins=8000
burn #2: Supply=7750  coins=6000
burn #3: Supply=5750  coins=4000
burn #4: Supply=3750  coins=2000
burn #5: Supply=1750  coins=0      ← 코인이 0에서 멈춤
burn #6: Supply=0     coins=0      ← Supply도 0에서 멈춤 (더 이상 안 내려감)
burn #7: Supply=0     coins=0
burn #8: Supply=0     coins=0
```

둘 다 0 밑으로 절대 안 내려가는 것 확인.

## 문서 갱신

`Assets/Docs/Game_Formula.md` 2장/3장/4장의 "Supply도 매 턴 감쇠한다"는 서술을 전부 "매 턴 자동 증가 +
`currentCoins` 하한"으로 정정. `Next_Tesk.md`의 "보유 코인-발행량 연동 여부 결정" 후보 항목(13번)은 이번
수정으로 해소되어 제거.
