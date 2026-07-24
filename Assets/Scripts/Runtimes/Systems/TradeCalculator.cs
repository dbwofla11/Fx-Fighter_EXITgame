/// <summary>
/// Long/Short 거래 1건이 시장에 주는 영향(Support/Growth)을 계산한다.
/// 거래량이 많을수록 영향력이 커지도록 코인 수량에 비례한다.
/// </summary>
public static class TradeCalculator
{
    private const float SupportWeightPerCoin = 0.1f;
    private const float GrowthWeightPerCoin = 0.1f;

    private const float SupportDecayRate = 0.995f;
    private const float GrowthDecayRate = 0.995f;
    private const float SupplyDecayRate = 0.995f;

    // Long : 구매 -> Support/Growth 증가
    public static void Long(PlayerStat stat, long amount)
    {
        stat.Support += amount * SupportWeightPerCoin;
        stat.Growth += amount * GrowthWeightPerCoin;
    }

    // Short : 판매 -> Support/Growth 감소
    public static void Short(PlayerStat stat, long amount)
    {
        stat.Support -= amount * SupportWeightPerCoin;
        stat.Growth -= amount * GrowthWeightPerCoin;
    }

    // 시간이 지나면 Support/Growth/Supply가 0으로 서서히 수렴한다. Doubt는 감쇠 대상이 아니다 (Game_Formula.md 3장 참고).
    public static void Decay(PlayerStat stat)
    {
        stat.Support *= SupportDecayRate;
        stat.Growth *= GrowthDecayRate;
    }
}
