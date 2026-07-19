using UnityEngine;

public static class ProbabilityCalculator
{
    public static void Calculate(PlayerStat stat)
    {
        float score = CalculateScore(stat);

        stat.UpProbability = Mathf.Clamp01(score);
        stat.DownProbability = 1f - stat.UpProbability;
    }

    private static float CalculateScore(PlayerStat stat)
    {
        float score = 0.5f;

        score += stat.Support * 0.01f;
        score += stat.Growth * 0.01f;
        score -= stat.Doubt * 0.01f;

        return score;
    }
}