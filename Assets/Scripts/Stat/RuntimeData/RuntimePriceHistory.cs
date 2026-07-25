using System.Collections.Generic;

/// <summary>
/// 지금까지 지난 턴들의 가격 캔들 기록을 관리한다.
/// </summary>
public class RuntimePriceHistory
{
    public List<PricePoint> Points = new();
}
