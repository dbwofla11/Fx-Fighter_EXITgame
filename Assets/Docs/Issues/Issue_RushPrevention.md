# 이슈 : 러쉬막기 (매수 폭주 방지)

작성일 : 2026-08-08

관련 구현 : `Assets/Scripts/Runtimes/Systems/TradeCalculator.cs`,
`Assets/Scripts/UI/FeatherModal/TradeModalUI.cs`

## 배경

Notion "여름 공모전 > 2차 피드백 정리" B그룹 — 매수(Long)를 한 번에 대량으로 넣어 시장을 흔드는
"러쉬"를 막아달라는 요청. 원문에 확정형 문장과 브레인스토밍 문장이 섞여 있어 사용자 확인을 거쳐 아래
3개 항목으로 범위를 정리했다 (`Next_Tesk.md` 18번 항목).

1. 매수 1회 상한 (발행량 기준)
2. "상폐 상한선 늘리기 5"
3. 지지도 → 발행량 증가속도 로그 연동

## 1. 매수 1회 상한 (발행량 기준)

### 설계

처음엔 "고정 10,000개 + 발행량 기준 중 더 작은 쪽"으로 계획했다. 그런데
`MarketManager.InitialSupply = 2000`이라 게임 시작 시점 `Supply(2000+시작코인) - currentCoins = 2000`부터
시작하고, 매 턴 발행량 증가분(수십 단위, 2번 항목이 로그 연동으로 바꾸기 전 고정값 기준으로도 +50/턴)만큼만
늘어나 10,000을 넘기까지 최소 백 턴 이상 걸린다 — 발행량 기준 상한이 사실상 항상 더 타이트해서 고정
10,000개 상한은 죽은 코드가 된다는 걸 사용자가 지적, 고정 상한은 빼기로 했다.

"이미 보유한 만큼 빼고, 시장에 남은 유통량까지만 매수 가능"하다는 게 최종 규칙이다.

```csharp
// TradeCalculator.cs
public static long MaxTradeAmountBySupply(float currentSupply, long currentCoins)
{
    return (long)Math.Max(0f, currentSupply - currentCoins);
}
```

```csharp
// TradeModalUI.cs, GetMaxTradeAmount() Long 분기
if (mode == TradeMode.Long)
{
    float price = MarketManager.Instance.CurrentStat.CurrentPrice;
    maxByBalance = price <= 0f ? 0 : (long)(PlayerManager.Instance.currentMoney / price);

    long maxBySupply = TradeCalculator.MaxTradeAmountBySupply(MarketManager.Instance.CurrentStat.Supply, PlayerManager.Instance.currentCoins);
    maxByBalance = System.Math.Min(maxByBalance, maxBySupply);
}
```

잔고 기준 상한(`maxByBalance`)에 발행량 상한을 `Min`으로 적용한 뒤, 기존처럼 Doubt 캡
(`MaxTradeAmountByDoubt`)이 마지막으로 한 번 더 `Min` 적용된다. 매도(Short)는 손 안 댔다 — 매도는 원래도
보유 코인 전량이 상한이라 시장에 코인을 더 풀 위험이 없다.

### 검증 (Unity MCP Play 모드)

게임 시작 시점(`Supply=12000, coins=10000`)은 잔고 상한(1000)이 더 타이트해서 발행량 캡이 안 보이길래,
`PlayerManager.currentMoney`를 100만으로 올려 잔고 상한을 크게 만든 뒤 재확인했다.

```
maxByBalance = 100000 (잔고 기준)
maxBySupply  = 2000   (발행량 기준)
finalMax(Min) = 2000  ← 발행량 캡이 실제로 더 타이트하게 걸림
```

`MaxTradeAmountBySupply(2000, 3000)`처럼 보유 코인이 발행량보다 많은 경우(0 밑으로 안 내려가는지)도
`Math.Max(0f, ...)` 클램프로 0을 반환하는 것 확인.

## 2. "상폐 상한선 늘리기 5"

원문 의미가 불명확해 착수 전 사용자에게 다시 물어봤다 — **폐지(진행 안 함)**로 확정. 관련 계획을
`Next_Tesk.md`에서 제거했다.

## 3. 지지도 → 발행량 증가속도 로그 연동

### 설계

`TradeCalculator.GrowSupply`가 지금까지는 고정값(`SupplyGrowthPerTurn = 50`)만 매 턴 더했다
(`Issue_SupplyGrowthAndFloor.md` 참고). 여기에 "지지도가 높을수록(코인을 지지하는 여론이 강할수록)
발행이 빨라지지만, 무한정 커지지 않고 완만해진다"는 로그 곡선을 추가했다.

```csharp
// TradeCalculator.cs
// ponytail: 밸런스용 임시 수치, 실제 플레이해보고 조정 필요.
private const float SupplyGrowthBase = 10f;
private const float SupplyGrowthLogCoefficient = 100f;

public static void GrowSupply(PlayerStat stat, float suppressionRatio)
{
    float normalizedSupport = Mathf.Clamp01((stat.Support + 100f) / 200f);
    float growthPerTurn = SupplyGrowthBase + SupplyGrowthLogCoefficient * Mathf.Log(1f + normalizedSupport);
    stat.Supply += growthPerTurn * (1f - Mathf.Clamp01(suppressionRatio));
}
```

`stat`은 원래도 `GrowSupply`가 받는 파라미터라 별도 인자 추가 없이 `stat.Support`를 그대로 읽었다.
`suppressionRatio`(추가발행권한/우회발행권한 스킬의 억제 효과)는 기존과 동일하게 마지막에 곱해진다.

계수는 Support=0(중립)일 때 기존 고정값(50)과 비슷하게 맞추고, Support=-100(최하)이면 하한(10),
Support=100(최상)이면 상한(~79) 근처로 수렴하도록 임시로 잡았다.

### 검증 (Unity MCP `execute_code`, Edit 모드 직접 호출)

```
Support=-100 -> +10.00 /턴
Support=-50  -> +32.31 /턴
Support=0    -> +50.55 /턴  (기존 고정값 50과 근접)
Support=50   -> +65.96 /턴
Support=100  -> +79.31 /턴  (로그라 완만하게 수렴, 무한정 안 커짐)
```

## 문서 갱신

- `Assets/Docs/Game_Formula.md` 2장(Supply/`GrowSupply` 공식), 3장("Long" 절 매수 상한, "누적치 감쇠" 절의
  `GrowSupply` 참조) 갱신.
- `Assets/Docs/Next_Tesk.md` 18번 항목("러쉬막기") 제거, `Assets/Docs/Completed_Tasks.md`로 이관.

## 컴파일/동작 확인

Unity MCP `refresh_unity`(force recompile) + `read_console`로 에러 없음 확인. Play 모드에서 실제 상한 값이
바뀌는 것까지 `execute_code`로 직접 확인 (위 검증 항목 참고).
