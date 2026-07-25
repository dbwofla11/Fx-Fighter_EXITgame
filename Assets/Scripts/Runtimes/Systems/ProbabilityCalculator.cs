using UnityEngine;

public static class ProbabilityCalculator
{
    // Game_Formula.md 1장 : Pup = Clamp(0.5 + ws×Support/100 + wg×Growth/100, 0, 1).
    // ws=wg=1.0이면 Support+Growth 합이 50만 넘어도 score가 포화(=1)돼버려 0.5로 낮췄다.
    private const float SupportWeight = 0.5f;
    private const float GrowthWeight = 0.5f;
    private const float DoubtWeight = 1f;

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