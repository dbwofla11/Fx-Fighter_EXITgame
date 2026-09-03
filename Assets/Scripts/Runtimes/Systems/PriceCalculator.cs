using UnityEngine;

public static class PriceCalculator
{
    private const float MaxSupply = 100000f;

    // ponytail: 거래량(Volume) 1당 변동폭 배율 조정치. 밸런스용 임시 수치, 실제 플레이 후 조정 필요.
    // 거래량부풀리기(+25) 하나만 썼을 때 대략 +25% 변동폭이 되도록 잡음.
    private const float VolumeDeltaWeight = 0.01f;
    private const float MinVolumeDeltaMultiplier = 0.2f; // Volume이 크게 마이너스여도 변동폭이 0에 너무 가까워지지 않게.
    private const float BaseVolumeIndex = 100f;
    private const float DailyVolumeVariationMin = 0.6f;
    private const float DailyVolumeVariationMax = 1.4f;
    private const float VolatilityVolumeWeight = 0.35f;
    private const float PlayerTradeVolumeWeight = 15f;

    /// <summary>가격이 내려갈 수 있는 최소값. 0 이하로 내려가면 거래(수량×가격) 계산이 깨지고
    /// 이벤트의 priceRatio(가격에 곱하는 충격)도 0에 곱해 무력화되므로 하한선을 둔다.</summary>
    public const float MinPrice = 1f;

    public static void Calculate(PlayerStat stat)
    {
        bool isUp = Random.value <= stat.UpProbability;

        float scarcity = CalculateScarcity(stat.Supply);
        float volumeMultiplier = CalculateVolumeMultiplier(stat.Volume);

        float delta = Mathf.Abs(
            stat.Growth * 0.6f +
            stat.Support * 0.25f +
            scarcity * 0.15f) * volumeMultiplier;

        if (isUp)
            stat.CurrentPrice += delta;
        else
            stat.CurrentPrice -= delta;

        ClampPrice(stat);

        Debug.Log($"현재가격 : {stat.CurrentPrice}, 변동폭: {delta}, stat표시<growth>: {stat.Growth},stat표시<Support>:{ stat.Support} 확률:{stat.UpProbability}");
    }

    /// <summary>차트에 기록할 양수 거래량 지수. Volume 효과를 기본값에 반영하고 가격 변동폭과 플레이어의
    /// 실제 매수/매도 수량을 시장 활동량으로 더한다. 턴 번호 해시는 Unity의 전역 Random 상태를 소비하지 않아
    /// 차트용 일별 편차가 가격·이벤트 확률 결과를 바꾸지 않는다.</summary>
    public static float CalculateVolumeIndex(float volume, float open, float close,
        float playerTradeAmount, int turn)
    {
        float dailyVariation = CalculateDailyVolumeVariation(turn);

        float priceMoveRatio = Mathf.Abs(close - open) / Mathf.Max(open, MinPrice);
        float volatilityMultiplier = 1f + Mathf.Log10(1f + priceMoveRatio * 10f) * VolatilityVolumeWeight;

        // 거래량이 큰 경우에도 막대 하나가 전체 스케일을 압도하지 않도록 로그 스케일로 보너스를 더한다.
        float playerTradeBonus = Mathf.Log10(1f + Mathf.Max(0f, playerTradeAmount)) * PlayerTradeVolumeWeight;

        return CalculateVolumeMultiplier(volume) * BaseVolumeIndex * dailyVariation * volatilityMultiplier
            + playerTradeBonus;
    }

    private static float CalculateDailyVolumeVariation(int turn)
    {
        // 인접한 턴도 서로 다른 값을 갖는 결정적 해시. 실행 순서나 세이브 재시작과 무관하게 같은 턴은 같은 값이다.
        unchecked
        {
            uint hash = (uint)turn + 0x9E3779B9u;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;

            float normalized = (hash & 0x00FFFFFFu) / 16777215f;
            return Mathf.Lerp(DailyVolumeVariationMin, DailyVolumeVariationMax, normalized);
        }
    }

    private static float CalculateVolumeMultiplier(float volume)
    {
        return Mathf.Max(MinVolumeDeltaMultiplier, 1f + volume * VolumeDeltaWeight);
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
