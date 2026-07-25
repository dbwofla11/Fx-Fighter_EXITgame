using System;

/// <summary>
/// 하루(한 턴) 동안의 코인 가격 캔들 1개 (Open = 턴 시작 전 가격, Close = 턴 계산 후 가격).
/// 캔들 차트 UI가 이 리스트를 그대로 그린다.
/// </summary>
public class PricePoint
{
    public DateTime Date;

    public float Open;

    public float Close;
}
