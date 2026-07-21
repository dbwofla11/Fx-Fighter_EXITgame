/// <summary>
/// 플레이어 거래(Long/Short)로 누적된 시장 영향치를 저장하는 런타임 데이터.
/// StatCalculator가 매 턴 이 값을 PlayerStat에 합산한다.
/// </summary>
public class RuntimeTradeData
{
    public float Support;

    public float Growth;
}
