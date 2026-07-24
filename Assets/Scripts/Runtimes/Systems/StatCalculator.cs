using UnityEngine;

public static class StatCalculator
{
    /// <summary>
    /// 이번 턴의 스탯을 계산한다.
    /// </summary>
    public static PlayerStat Calculate()
    {
        PlayerStat stat = new PlayerStat();
        stat.Reset();

        PlayerStat previous = MarketManager.Instance.CurrentStat;

        // PlayerStat이 매 턴 새로 생성되므로, PriceCalculator가 이어서 계산할 수 있도록 이전 턴 가격을 이월한다.
        stat.CurrentPrice = previous.CurrentPrice;

        // Support/Growth/Supply는 거래·직업 선택·시사 이벤트로 그 순간 직접 반영되는 값이라, 매 턴 새로 계산하지 않고
        // 이전 값을 그대로 이어받아 감쇠시킨다 (CurrentPrice와 동일한 이월 패턴).
        stat.Support = previous.Support;
        stat.Growth = previous.Growth;
        stat.Supply = previous.Supply;
        stat.JobSkillSupportBonus = previous.JobSkillSupportBonus;
        stat.JobSkillGrowthBonus = previous.JobSkillGrowthBonus;
        TradeCalculator.Decay(stat);

        // Doubt는 감쇠하지 않고 계속 쌓이는 값이다 (시간이 지날수록 자동으로 100을 향해 오르다가 100이 되면
        // 게임오버가 되는 기획). 매 턴 새로 계산하지 않고 이전 값을 그대로 이어받는다.
        stat.Doubt = previous.Doubt;

        ApplyJob(stat);
        ApplySkills(stat);

        return stat;
    }

    /// <summary>
    /// 현재 직업의 Effect를 적용한다. Support/Growth는 선택 시점에 직접 반영되므로 여기서는 제외한다.
    /// Supply도 감쇠 대상이라 매 턴 재적용하면 Doubt에서 겪었던 것과 동일한 문제(무한정 증가)가 생기므로 제외한다
    /// (Job은 애초에 Supply를 다루지 않는 설계지만, 방어적으로 막아둔다).
    /// </summary>
    private static void ApplyJob(PlayerStat stat)
    {
        JobSO job = JobManager.Instance.CurrentJob;

        if (job == null)
            return;

        foreach (EffectData effect in job.effects)
        {
            if (effect.effectType == EffectType.SupportIncrease
                || effect.effectType == EffectType.GrowthIncrease
                || effect.effectType == EffectType.SupplyIncrease
                || effect.effectType == EffectType.SupplyDecrease)
                continue;

            ApplyEffect(stat, effect);
        }
    }

    /// <summary>
    /// 직업을 선택하는 순간, 그 직업의 Support/Growth 효과를 stat에 직접 반영한다.
    /// </summary>
    public static void ApplyJobSelection(PlayerStat stat, JobSO job)
    {
        if (job == null)
            return;

        foreach (EffectData effect in job.effects)
        {
            if (effect.effectType == EffectType.SupportIncrease)
            {
                stat.Support += effect.value;
                stat.JobSkillSupportBonus += effect.value;
            }
            else if (effect.effectType == EffectType.GrowthIncrease)
            {
                stat.Growth += effect.value;
                stat.JobSkillGrowthBonus += effect.value;
            }
        }
    }

    /// <summary>
    /// 활성화된 토글형(재사용 불가) 스킬들의 Effect를 매 턴 재적용한다.
    /// 재사용형 스킬은 사용 시점에 ApplySkillUse로 1회 반영되고 이후 감쇠하므로 여기서 제외한다.
    /// Supply도 감쇠 대상이라 토글형 스킬에서 매 턴 재적용하면 무한정 증가하므로 제외한다.
    /// </summary>
    private static void ApplySkills(PlayerStat stat)
    {
        var activeSkills = SkillManager.Instance.GetActiveSkills();

        foreach (SkillRuntimeInfo skill in activeSkills)
        {
            if (skill.Profile.isReusable)
                continue;

            foreach (EffectData effect in skill.Profile.effects)
            {
                if (effect.effectType == EffectType.SupplyIncrease || effect.effectType == EffectType.SupplyDecrease)
                    continue;

                ApplyEffect(stat, effect);
            }
        }
    }

    /// <summary>
    /// 재사용형 스킬을 사용하는 순간, 그 스킬의 Support/Growth/Doubt/Supply 효과를 stat에 직접 반영한다.
    /// </summary>
    public static void ApplySkillUse(PlayerStat stat, SkillSO skill)
    {
        if (skill == null)
            return;

        foreach (EffectData effect in skill.effects)
        {
            if (effect.effectType == EffectType.SupportIncrease)
            {
                stat.Support += effect.value;
                stat.JobSkillSupportBonus += effect.value;
            }
            else if (effect.effectType == EffectType.GrowthIncrease)
            {
                stat.Growth += effect.value;
                stat.JobSkillGrowthBonus += effect.value;
            }
            else if (effect.effectType == EffectType.DoubtDecrease)
                stat.Doubt -= effect.value;
            else if (effect.effectType == EffectType.DoubtIncrease)
                stat.Doubt += effect.value;
            else if (effect.effectType == EffectType.SupplyIncrease)
                stat.Supply += effect.value;
            else if (effect.effectType == EffectType.SupplyDecrease)
                stat.Supply -= effect.value;
        }
    }

    /// <summary>
    /// Effect 하나를 PlayerStat에 적용한다. EventCalculator도 EventSO의 효과를 적용할 때 재사용한다.
    /// </summary>
    public static void ApplyEffect(PlayerStat stat, EffectData effect)
    {
        switch (effect.effectType)
        {
            case EffectType.SupportIncrease:
                stat.Support += effect.value;
                break;

            case EffectType.GrowthIncrease:
                stat.Growth += effect.value;
                break;

            case EffectType.DoubtDecrease:
                stat.Doubt -= effect.value;
                break;

            case EffectType.DoubtIncrease:
                stat.Doubt += effect.value;
                break;

            case EffectType.PositiveEventRate:
                stat.PositiveEventRate += effect.value;
                break;

            case EffectType.NegativeEventRate:
                stat.NegativeEventRate += effect.value;
                break;

            case EffectType.CashBonus:
                stat.CashBonus += effect.value;
                break;

            case EffectType.VolumeIncrease:
                stat.Volume += effect.value;
                break;

            case EffectType.VolumeDecrease:
                stat.Volume -= effect.value;
                break;

            case EffectType.ExitUnlock:
                stat.ExitUnlocked = true;
                break;

            case EffectType.SupplyIncrease:
                stat.Supply += effect.value;
                break;

            case EffectType.SupplyDecrease:
                stat.Supply -= effect.value;
                break;

            default:
                Debug.LogWarning($"처리되지 않은 EffectType : {effect.effectType}");
                break;
        }
    }
}
