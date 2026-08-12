/// <summary>
/// 화면에 보이지 않는 스트리머 감정 지수(PlayerStat.StreamerIndex, 0~100)를 20점 단위 5개 구간으로 나눠
/// 반응 단계로 변환한다. 지수 자체는 MarketManager.UpdateStreamerReaction이 가격 변화를 완만하게 누적해
/// 갱신하므로, 가격이 크게 흔들려도 반응이 매 턴 바로 바뀌지 않는다. 구간 경계는 예시치이며 플레이테스트로
/// 조정 가능하다.
/// </summary>
public static class StreamerReactionCalculator
{
    private const float DownBoundary = 20f;
    private const float NeutralBoundary = 40f;
    private const float UpBoundary = 60f;
    private const float SurgeBoundary = 80f;

    public static StreamerReactionState Calculate(float streamerIndex)
    {
        if (streamerIndex < DownBoundary)
            return StreamerReactionState.Crash;

        if (streamerIndex < NeutralBoundary)
            return StreamerReactionState.Down;

        if (streamerIndex < UpBoundary)
            return StreamerReactionState.Neutral;

        if (streamerIndex < SurgeBoundary)
            return StreamerReactionState.Up;

        return StreamerReactionState.Surge;
    }
}
