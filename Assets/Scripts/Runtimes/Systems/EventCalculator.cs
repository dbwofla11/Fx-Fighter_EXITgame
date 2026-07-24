using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시사 이벤트 1건을 골라 PlayerStat에 반영한다 (Trade/Job/Skill과 동일하게 발생 시점에 직접 반영).
/// 방향(긍정/부정)은 PositiveEventRate/NegativeEventRate로 먼저 정하고, 그 카테고리에 속한 EventSO 중
/// 하나를 weight 가중치 랜덤으로 뽑는다. 각 EventSO는 자신의 효과값에 부호를 직접 담고 있다.
/// </summary>
public static class EventCalculator
{
    /// <summary>
    /// 이벤트를 계산해 stat에 반영하고, 실제로 발생한 EventSO를 반환한다 (없으면 null).
    /// </summary>
    public static EventSO Calculate(PlayerStat stat, IReadOnlyList<EventSO> eventDatabase)
    {
        EventCategory category = RollCategory(stat);
        EventSO chosen = PickWeighted(eventDatabase, category);

        if (chosen == null)
            return null;

        Apply(stat, chosen);

        return chosen;
    }

    /// <summary>
    /// 이미 정해진 EventSO를 stat에 그대로 적용한다. 랜덤 선택(Calculate)과 무조건 발생(guaranteedTurn) 양쪽에서 재사용한다.
    /// </summary>
    public static void Apply(PlayerStat stat, EventSO chosen)
    {
        foreach (EffectData effect in chosen.effects)
        {
            StatCalculator.ApplyEffect(stat, effect);
        }

        stat.Supply += chosen.supplyDelta;

        // 그 턴의 PriceCalculator 정규 가격 변화와 별도로, 이벤트 자체로 즉시 발생하는 1회성 가격 충격이다.
        stat.CurrentPrice += stat.CurrentPrice * chosen.priceRatio;
    }

    private static EventCategory RollCategory(PlayerStat stat)
    {
        float pPositive = Mathf.Clamp01(0.5f + stat.PositiveEventRate / 100f - stat.NegativeEventRate / 100f);
        return Random.value <= pPositive ? EventCategory.Positive : EventCategory.Negative;
    }

    private static EventSO PickWeighted(IReadOnlyList<EventSO> eventDatabase, EventCategory category)
    {
        if (eventDatabase == null)
            return null;

        float totalWeight = 0f;

        foreach (EventSO candidate in eventDatabase)
        {
            if (candidate.category == category)
                totalWeight += candidate.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (EventSO candidate in eventDatabase)
        {
            if (candidate.category != category)
                continue;

            cumulative += candidate.weight;

            if (roll <= cumulative)
                return candidate;
        }

        return null;
    }
}
