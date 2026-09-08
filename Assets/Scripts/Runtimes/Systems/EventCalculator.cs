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
        EventSO chosen = SelectEvent(stat, eventDatabase);

        if (chosen == null)
            return null;

        // 선택형 이벤트는 플레이어 입력 뒤 ResolveChoice에서 한 번만 적용한다.
        if (chosen.choices == null || chosen.choices.Count == 0)
            Apply(stat, chosen);

        return chosen;
    }

    /// <summary>이벤트만 선택하고 적용은 호출자에게 맡긴다. 선택형 이벤트는 이 단계에서 효과를 적용하지 않는다.</summary>
    public static EventSO SelectEvent(PlayerStat stat, IReadOnlyList<EventSO> eventDatabase)
    {
        EventCategory category = RollCategory(stat);
        return PickWeighted(eventDatabase, category);
    }

    /// <summary>
    /// 이미 정해진 EventSO를 stat에 그대로 적용한다. 랜덤 선택(Calculate)과 무조건 발생(guaranteedTurn) 양쪽에서 재사용한다.
    /// </summary>
    public static void Apply(PlayerStat stat, EventSO chosen)
    {
        if (chosen == null)
            return;

        if (chosen.effects != null)
        foreach (EffectData effect in chosen.effects)
        {
            StatCalculator.ApplyEffect(stat, effect);
        }

        stat.Supply += chosen.supplyDelta;

        // 그 턴의 PriceCalculator 정규 가격 변화와 별도로, 이벤트 자체로 즉시 발생하는 1회성 가격 충격이다.
        stat.CurrentPrice += stat.CurrentPrice * chosen.priceRatio;
        PriceCalculator.ClampPrice(stat);
    }

    public static float CalculateChoiceSuccessProbability(PlayerStat stat, EventChoice choice, int overdueCount)
    {
        if (choice == null)
            return 0f;

        float doubtPenalty = Mathf.Max(0f, stat.Doubt) / 100f * choice.doubtPenaltyAt100;
        float overduePenalty = Mathf.Max(0, overdueCount) * choice.overduePenaltyPerCount;
        return Mathf.Clamp01(choice.baseSuccessProbability + choice.skillBonus + choice.jobBonus
            - doubtPenalty - overduePenalty);
    }

    public static long CalculateChoiceCost(EventChoice choice, int previousSelectionCount)
    {
        if (choice == null || choice.baseCost <= 0)
            return 0;

        double multiplier = Mathf.Max(0f, choice.costMultiplier);
        double rawCost = choice.baseCost * System.Math.Pow(multiplier, Mathf.Max(0, previousSelectionCount));
        long cost = rawCost >= long.MaxValue ? long.MaxValue : (long)System.Math.Floor(rawCost);

        if (choice.maxCost > 0)
            cost = System.Math.Min(cost, choice.maxCost);

        return System.Math.Max(0L, cost);
    }

    public static EventChoiceResolution ResolveChoice(PlayerStat stat, EventChoice choice, int choiceIndex,
        int previousSelectionCount, long currentCash, int overdueCount)
    {
        EventChoiceResolution resolution = new EventChoiceResolution
        {
            ChoiceIndex = choiceIndex,
            ChoiceLabel = choice?.label ?? string.Empty,
            Cost = CalculateChoiceCost(choice, previousSelectionCount)
        };

        if (choice == null)
            return resolution;

        long availableCash = System.Math.Max(0L, currentCash);
        if (availableCash < resolution.Cost && !choice.allowDebt)
            return resolution;

        resolution.Valid = true;
        resolution.CashPaid = System.Math.Min(availableCash, resolution.Cost);
        resolution.DebtAdded = resolution.Cost - resolution.CashPaid;
        resolution.SuccessProbability = CalculateChoiceSuccessProbability(stat, choice, overdueCount);
        resolution.Succeeded = Random.value <= resolution.SuccessProbability;

        if (resolution.Succeeded)
        {
            ApplyEffects(stat, choice.successEffects);
            stat.Supply += choice.successSupplyDelta;
            stat.CurrentPrice *= 1f + choice.successPriceRatio;
            resolution.CashDelta = choice.successCashDelta;
        }
        else
        {
            ApplyEffects(stat, choice.failureEffects);
            stat.Supply += choice.failureSupplyDelta;
            stat.CurrentPrice *= 1f - Mathf.Clamp01(choice.failurePriceRate);
            long cashAfterCost = availableCash - resolution.CashPaid;
            resolution.FailureCashLoss = (long)System.Math.Floor(cashAfterCost * Mathf.Max(0f, choice.failureCashRate));
            resolution.CashDelta = choice.failureCashDelta;
        }

        PriceCalculator.ClampPrice(stat);
        return resolution;
    }

    private static void ApplyEffects(PlayerStat stat, List<EffectData> effects)
    {
        if (effects == null)
            return;

        foreach (EffectData effect in effects)
            StatCalculator.ApplyEffect(stat, effect);
    }

    /// <summary>
    /// 실제 긍정 이벤트 발생 확률(%, 0~100). 기본 50%에 PositiveEventRate/NegativeEventRate 보너스를 더한 값 —
    /// RollCategory와 EventOverviewUI가 이 값을 공유해서 표시 확률이 실제 판정 확률과 항상 일치하게 한다.
    /// </summary>
    public static float PositiveEventChancePercent(PlayerStat stat)
    {
        return Mathf.Clamp(50f + stat.PositiveEventRate - stat.NegativeEventRate, 0f, 100f);
    }

    private static EventCategory RollCategory(PlayerStat stat)
    {
        float pPositive = PositiveEventChancePercent(stat) / 100f;
        return Random.value <= pPositive ? EventCategory.Positive : EventCategory.Negative;
    }

    private static EventSO PickWeighted(IReadOnlyList<EventSO> eventDatabase, EventCategory category)
    {
        if (eventDatabase == null)
            return null;

        float totalWeight = 0f;

        foreach (EventSO candidate in eventDatabase)
        {
            if (candidate != null && candidate.category == category)
                totalWeight += candidate.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (EventSO candidate in eventDatabase)
        {
            if (candidate == null || candidate.category != category)
                continue;

            cumulative += candidate.weight;

            if (roll <= cumulative)
                return candidate;
        }

        return null;
    }
}
