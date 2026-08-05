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
}
