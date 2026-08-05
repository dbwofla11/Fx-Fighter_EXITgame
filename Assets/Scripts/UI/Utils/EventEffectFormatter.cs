using System.Collections.Generic;
using System.Text;
using UnityEngine;

// EventSO.category(Positive/Negative)에 대응하는 색상과, EventSO.effects를 사람이 읽는 문자열로 바꾸는 로직.
// EventLogPanelUI(이벤트 로그 카드)와 EventNotificationUI(메인 화면 알림)가 동일한 표시 규칙을 쓰도록 공유한다.
public static class EventEffectFormatter
{
    public static readonly Color PositiveColor = new Color(0.6941f, 1f, 0.6941f); // #B1FFB1
    public static readonly Color NegativeColor = new Color(1f, 0.7294f, 0.6941f); // #FFBAB1

    public static Color CategoryColor(EventCategory category) =>
        category == EventCategory.Positive ? PositiveColor : NegativeColor;

    public static string BuildEffectsText(List<EffectData> effects)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < effects.Count; i++)
        {
            (string label, float delta) = DescribeEffect(effects[i]);
            if (i > 0) sb.Append('\n');
            sb.Append(label).Append(' ').Append(delta >= 0 ? "+" : "").Append(delta.ToString("0.#"));
        }

        return sb.ToString();
    }

    // 부호(+ = 증가, - = 감소)는 UIFormat.SignedEffectValue가 판정한다. 여기서는 라벨만 고른다.
    private static (string label, float delta) DescribeEffect(EffectData effect)
    {
        string label = effect.effectType switch
        {
            EffectType.SupportIncrease => "코인 지지도",
            EffectType.GrowthIncrease => "코인 상승률",
            EffectType.DoubtDecrease or EffectType.DoubtIncrease => "의심도",
            EffectType.PositiveEventRate => "긍정 이벤트 확률",
            EffectType.NegativeEventRate => "부정 이벤트 확률",
            EffectType.CashBonus => "거래 수익",
            EffectType.VolumeIncrease or EffectType.VolumeDecrease => "코인 거래량",
            EffectType.ExitUnlock => "엑시트 조건",
            EffectType.SupplyIncrease or EffectType.SupplyDecrease => "발행량",
            EffectType.SupplyGrowthSuppress => "발행량 증가 억제",
            _ => effect.effectType.ToString(),
        };

        return (label, UIFormat.SignedEffectValue(effect));
    }
}
