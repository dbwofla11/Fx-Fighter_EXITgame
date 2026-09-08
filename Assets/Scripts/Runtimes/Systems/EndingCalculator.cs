/// <summary>
/// 엔딩 판정(체포/거지/엑시트)을 상태 변경 없이 순수하게 결정한다.
/// 실제 게임 종료 처리(IsGameOver, PauseGame, OnGameEnded 발행)는 MarketManager가 담당한다.
/// </summary>
public static class EndingCalculator
{
    /// <summary>가격이 상폐 기준(MarketManager.DelistingPriceThreshold=2) 이하로 이 턴 수만큼 연속으로
    /// 붙어있으면 거지 엔딩(상폐)으로 처리한다 (1주, DaysPerCandle=7 기준).</summary>
    public const int PriceFloorStreakLimit = 7;
    public const int NegativeCashStreakLimit = 30;

    /// <summary>
    /// 매 턴 자동으로 판정되는 엔딩(체포/거지)을 확인한다. 둘 다 성립하면 체포가 우선이다.
    /// 거지 엔딩은 자산 소진(현금+코인 0) 또는 가격이 상폐 기준 이하로 1주 연속 방치된 경우 둘 다 해당한다.
    /// 해당하는 엔딩이 없으면 null.
    /// </summary>
    public static EndingType? CheckAutomatic(PlayerStat stat, long currentMoney, long currentCoins, int priceFloorStreak,
        int negativeCashStreak = 0)
    {
        if (stat.Doubt >= 100f)
            return EndingType.Arrest;

        if ((currentMoney == 0 && currentCoins == 0)
            || priceFloorStreak >= PriceFloorStreakLimit
            || negativeCashStreak >= NegativeCashStreakLimit)
            return EndingType.Broke;

        return null;
    }

    /// <summary>
    /// 엑시트 버튼 클릭 시 호출한다. 과거엔 Doubt/Support로 영웅/엑시트를 갈랐지만 그 구분은 폐지돼
    /// 항상 엑시트 엔딩으로 종료한다. 호출 전에 CanExit(목표 자산 달성 여부)을 확인해야 한다.
    /// </summary>
    public static EndingType CheckExit()
    {
        return EndingType.Exit;
    }
}
