/// <summary>
/// 스트리머 패널이 이번 턴 가격 변화량(PlayerStat.PriceChangeThisTurn)에 따라 보일 반응 단계.
/// 값이 클수록(양의 방향) 더 크게 기뻐하는 반응, 작을수록(음의 방향) 더 크게 실망하는 반응이다.
/// </summary>
public enum StreamerReactionState
{
    /// <summary>폭락 : 큰 폭의 하락</summary>
    Crash = -2,

    /// <summary>하락</summary>
    Down = -1,

    /// <summary>보합 : 가격 변화가 거의 없음</summary>
    Neutral = 0,

    /// <summary>상승</summary>
    Up = 1,

    /// <summary>폭등 : 큰 폭의 상승</summary>
    Surge = 2,
}
