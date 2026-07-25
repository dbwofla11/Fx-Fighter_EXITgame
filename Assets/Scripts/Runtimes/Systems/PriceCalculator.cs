using UnityEngine;

public static class PriceCalculator
{
    private const float MaxSupply = 20000f;

    /// <summary>가격이 내려갈 수 있는 최소값. 0 이하로 내려가면 거래(수량×가격) 계산이 깨지고
    /// 이벤트의 priceRatio(가격에 곱하는 충격)도 0에 곱해 무력화되므로 하한선을 둔다.</summary>
    public const float MinPrice = 1f;

    public static void Calculate(PlayerStat stat)
    {
        bool isUp = Random.value <= stat.UpProbability;

        float scarcity = CalculateScarcity(stat.Supply);

        float delta = Mathf.Abs(
            stat.Growth * 0.6f +
            stat.Support * 0.25f +
            scarcity * 0.15f);

        if (isUp)
            stat.CurrentPrice += delta;
        else
            stat.CurrentPrice -= delta;

        ClampPrice(stat);

        Debug.Log($"현재가격 : {stat.CurrentPrice}, 변동폭: {delta}, stat표시<growth>: {stat.Growth},stat표시<Support>:{ stat.Support} 확률:{stat.UpProbability}");
    }

    /// <summary>CurrentPrice가 MinPrice 밑으로 내려가지 않도록 고정한다. EventCalculator도 가격 충격 적용 후 재사용한다.</summary>
    public static void ClampPrice(PlayerStat stat)
    {
        stat.CurrentPrice = Mathf.Max(MinPrice, stat.CurrentPrice);
    }

    private static float CalculateScarcity(float supply)
    {
        return 100f * (1f - supply / MaxSupply);
    }
}