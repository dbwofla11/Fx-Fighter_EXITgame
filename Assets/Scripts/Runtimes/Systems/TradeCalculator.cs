/// <summary>
/// Long/Short 거래 1건이 시장에 주는 영향(Support/Growth)을 계산한다.
/// 거래량이 많을수록 영향력이 커지도록 코인 수량에 비례한다.
/// </summary>
public static class TradeCalculator
{
    private const float SupportWeightPerCoin = 0.1f;
    private const float GrowthWeightPerCoin = 0.1f;

    // Long : 구매 -> Support/Growth 증가
    public static void Long(RuntimeTradeData tradeData, long amount)
    {
        tradeData.Support += amount * SupportWeightPerCoin;
        tradeData.Growth += amount * GrowthWeightPerCoin;
    }

    // Short : 판매 -> Support/Growth 감소
    public static void Short(RuntimeTradeData tradeData, long amount)
    {
        tradeData.Support -= amount * SupportWeightPerCoin;
        tradeData.Growth -= amount * GrowthWeightPerCoin;
    }
}
