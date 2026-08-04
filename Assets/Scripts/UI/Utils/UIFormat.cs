using System;

// UI 여러 곳에서 조금씩 다르게 중복 구현되던 통화/퍼센트/날짜 포맷과 EffectType 부호 판정을 모아둔 곳.
public static class UIFormat
{
    // "₩ 1,234" — PlayerUI/TradeModalUI/EventOverviewUI의 금액 표시.
    public static string Currency(float value) => "₩ " + value.ToString("N0");

    // "₩1,234.567" — 코인 가격 전용(CoinPriceHeaderUI/PriceChartUI), 공백 없이 붙여 쓰고 소수 3자리까지 보여준다.
    public static string CurrencyTight(float value) => "₩" + value.ToString("N3");

    // "+12" / "-12". numberFormat으로 소수 자리수를 맞춘다(기본은 EventOverviewUI가 쓰던 "0.#").
    public static string Signed(float value, string numberFormat = "0.#") =>
        (value >= 0 ? "+" : "") + value.ToString(numberFormat);

    public static string Percent(float value) => value.ToString("0.#") + "%";

    public static string SignedPercent(float value) => Signed(value) + "%";

    // DateTime.ToString의 "/"는 문화권에 따라 구분자가 바뀔 수 있어 직접 포맷한다.
    public static string DateSlash(DateTime date) => $"{date.Month:00}/{date.Day:00}";
    public static string DateDot(DateTime date) => $"{date.Year}.{date.Month:00}.{date.Day:00}";
    public static string DateDash(DateTime date) => $"{date.Year}-{date.Month:00}-{date.Day:00}";

    // StatCalculator.ApplyEffect와 동일한 부호 규칙: 감소형(Decrease)만 부호를 뒤집고 나머지는 값 그대로.
    public static float SignedEffectValue(EffectData effect) =>
        effect.effectType is EffectType.DoubtDecrease or EffectType.VolumeDecrease or EffectType.SupplyDecrease
            ? -effect.value
            : effect.value;
}
