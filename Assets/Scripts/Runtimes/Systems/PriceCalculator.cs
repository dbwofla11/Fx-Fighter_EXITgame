using UnityEngine;

public static class PriceCalculator
{
    private const float MaxSupply = 20000f;

    public static void Calculate(PlayerStat stat)
    {
        bool isUp = Random.value <= stat.UpProbability;

        float scarcity = CalculateScarcity(stat.Supply);

        float delta =
            stat.Growth * 0.6f +
            stat.Support * 0.25f +
            scarcity * 0.15f;


        if (isUp)
            stat.CurrentPrice += delta;
        else
            stat.CurrentPrice -= delta;

        Debug.Log($"현재가격 : {stat.CurrentPrice}");
    }

    private static float CalculateScarcity(float supply)
    {
        return 100f * (1f - supply / MaxSupply);
    }
}