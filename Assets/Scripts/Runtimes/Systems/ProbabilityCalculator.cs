using UnityEngine;

public static class ProbabilityCalculator
{
    // Game_Formula.md 1장 : Pup = Clamp(0.5 + ws×Support/100 + wg×Growth/100 - wd×Doubt/100, 0, 1).
    // ws=wg=1.0이면 Support+Growth 합이 50만 넘어도 score가 포화(=1)돼버려 한 번 0.5로 낮췄었다.
    // 그런데 TradeCalculator.Long/Short가 거래 1건마다 Support/Growth를 항상 동일한 양만큼 같이 움직이므로
    // (SupportWeightPerCoin == GrowthWeightPerCoin), 실질적으로는 독립된 두 신호가 아니라 "거래 신호" 하나가
    // ws+wg로 두 배 반영되는 셈이라 0.5에서도 몇 번만 거래하면 여전히 바로 포화됐다. 0.25로 다시 절반 낮춰서
    // Support/Growth가 둘 다 클램프 상한(100)까지 차야만 포화되도록 완화했다.
    // wd(DoubtWeight)도 원래 1.0이라 Doubt=100(자동 상승으로 후반에 쉽게 도달)이면 Support/Growth가 최대치여도
    // Pup이 0까지 눌려버려 후반 게임이 사실상 진행 불가능했다. ws/wg와 동일하게 0.25로 낮춰서, Doubt가 완전히
    // 차도 Support/Growth가 좋으면 Pup이 최대 0.75까지는 유지되도록(= 완전히 압도하지 않도록) 완화했다.
    private const float SupportWeight = 0.25f;
    private const float GrowthWeight = 0.25f;
    private const float DoubtWeight = 0.25f;

    public static void Calculate(PlayerStat stat)
    {
        float score = CalculateScore(stat);

        stat.UpProbability = Mathf.Clamp01(score);
        stat.DownProbability = 1f - stat.UpProbability;
    }

    private static float CalculateScore(PlayerStat stat)
    {
        float score = 0.5f;

        score += stat.Support * 0.01f * SupportWeight;
        score += stat.Growth * 0.01f * GrowthWeight;
        score -= stat.Doubt * 0.01f * DoubtWeight;

        return score;
    }
}