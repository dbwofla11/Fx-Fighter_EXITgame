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

    // colorize : 스킬 패널(SkillPanelUI)에서만 켜서 스탯이 플레이어에게 좋은지/나쁜지 색으로 구분한다.
    // 이벤트 로그/알림(EventLogPanelUI, EventNotificationUI)은 기존처럼 무색 그대로 둔다.
    public static string BuildEffectsText(List<EffectData> effects, bool colorize = false)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < effects.Count; i++)
        {
            (string label, float delta) = DescribeEffect(effects[i]);
            if (i > 0) sb.Append('\n');
            string line = label + " " + (delta >= 0 ? "+" : "") + delta.ToString("0.#");
            if (colorize)
            {
                string color = IsBeneficial(effects[i]) ? "#009900" : "#CC0000";
                line = $"<b><color={color}>{line}</color></b>";
            }
            sb.Append(line);
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
            EffectType.DoubtDecrease or EffectType.DoubtIncrease => "의심도(즉시)",
            EffectType.DoubtDecline => "의심도(200턴에 걸쳐 하락)",
            EffectType.PositiveEventRate => "긍정 이벤트 확률",
            EffectType.NegativeEventRate => "부정 이벤트 확률",
            EffectType.CashBonus => "거래 수익",
            EffectType.VolumeIncrease or EffectType.VolumeDecrease => "코인 거래량",
            EffectType.ExitUnlock => "엑시트 조건",
            EffectType.SupplyIncrease or EffectType.SupplyDecrease => "발행량",
            EffectType.SupplyGrowthSuppress => "발행량 증가 억제",
            EffectType.PriceShockPercent => "코인 가격(즉시 %)",
            _ => effect.effectType.ToString(),
        };

        return (label, UIFormat.SignedEffectValue(effect));
    }

    // 이 효과가 플레이어에게 좋은 효과인지(스탯 색상 구분용). EffectType 이름 자체가 방향을 담고 있으므로
    // 효과 종류별로 고정 판정한다 — Supply/Volume 쪽은 Game_Formula.md 설명(발행량 증가=희석, 거래량
    // 증가=영향력 증가) 기준의 추정치라 실제 플레이 감각과 다르면 나중에 조정 필요.
    // SupportIncrease/GrowthIncrease/PriceShockPercent는 예외 — 같은 EffectType으로 증가(+)/감소(-) 둘 다
    // 표현하므로(예: FOMO유도의 Support -30) 값 부호로 판정한다.
    private static bool IsBeneficial(EffectData effect) => effect.effectType switch
    {
        EffectType.DoubtIncrease or EffectType.NegativeEventRate
            or EffectType.VolumeDecrease or EffectType.SupplyIncrease => false,
        EffectType.SupportIncrease or EffectType.GrowthIncrease or EffectType.PriceShockPercent => effect.value >= 0,
        _ => true,
    };
}
