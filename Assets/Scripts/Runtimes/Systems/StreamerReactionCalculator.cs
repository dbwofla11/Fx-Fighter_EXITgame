/// <summary>
/// 이번 턴 가격 변화량(절대값 delta, PriceCalculator/EventCalculator가 CurrentPrice에 반영한 총량)을
/// 스트리머 반응 단계로 변환한다. 임계값은 Game_Formula.md 2장 기준 정규 가격 변화 범위(대략 ±100)를
/// 참고한 예시치이며, 실제 스프라이트/플레이테스트 결과에 맞춰 조정 가능하다.
/// </summary>
public static class StreamerReactionCalculator
{
    private const float SurgeThreshold = 50f;
    private const float UpThreshold = 10f;
    private const float DownThreshold = -10f;
    private const float CrashThreshold = -50f;

    public static StreamerReactionState Calculate(float priceChange)
    {
        if (priceChange >= SurgeThreshold)
            return StreamerReactionState.Surge;

        if (priceChange >= UpThreshold)
            return StreamerReactionState.Up;

        if (priceChange <= CrashThreshold)
            return StreamerReactionState.Crash;

        if (priceChange <= DownThreshold)
            return StreamerReactionState.Down;

        return StreamerReactionState.Neutral;
    }
}
