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

    // 일봉 레벨에서는 항상 Open/Close 중 하나와 같아 의미가 없다(PriceCalculator가 하루에 한 번만 가격을 바꿈).
    // 여러 일봉을 묶은 주봉/월봉 캔들에서만 실제 값이 채워진다 — PriceChartUI.AggregateHistory() 참고.
    public float High;

    public float Low;

    /// <summary>해당 기간의 거래량 지수. 일봉은 기본 100에 Volume 효과·가격 변동·실제 거래량을 반영하고,
    /// 주봉/월봉은 포함된 일봉 거래량을 합산한다.</summary>
    public float Volume;
}
