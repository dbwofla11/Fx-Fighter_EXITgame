using System;

/// <summary>
/// Long/Short 거래, 발행량 조작 1건이 시장에 주는 영향(Support/Growth/Doubt/Supply)을 계산한다.
/// 수량이 많을수록 영향력이 커지도록 코인/조작 수량에 비례한다.
/// </summary>
public static class TradeCalculator
{
    private const float SupportWeightPerCoin = 0.1f;
    private const float GrowthWeightPerCoin = 0.1f;
    private const float DoubtWeightPerSupplyUnit = 0.1f;

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

    // 발행량 조작 : 발행량 증가(희석) -> Support/Growth 감소, 발행량 감소(소각) -> Support/Growth 증가.
    // 늘리든 줄이든 조작 자체가 의심을 키우므로 Doubt는 수량의 절대값에 비례해 증가한다 (감쇠 없이 그대로 누적).
    public static void ManipulateSupply(PlayerStat stat, long amount)
    {
        stat.Supply += amount;
        stat.Support -= amount * SupportWeightPerCoin;
        stat.Growth -= amount * GrowthWeightPerCoin;
        stat.Doubt += Math.Abs(amount) * DoubtWeightPerSupplyUnit;
    }

    // 시간이 지나면 Support/Growth/Supply가 0으로 서서히 수렴한다. Doubt는 감쇠 대상이 아니다 (Game_Formula.md 3장 참고).
    // JobSkillSupportBonus/GrowthBonus(UI 표시용, Job+Skill 기여분만 별도 추적)도 Support/Growth와 동일하게 감쇠시킨다.
    public static void Decay(PlayerStat stat)
    {
        stat.Support *= SupportDecayRate;
        stat.Growth *= GrowthDecayRate;
        stat.JobSkillSupportBonus *= SupportDecayRate;
        stat.JobSkillGrowthBonus *= GrowthDecayRate;
    }
}
