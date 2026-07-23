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

        // Support/Growth는 Job 선택/거래로 그 순간 직접 반영되는 값이라, 매 턴 새로 계산하지 않고
        // 이전 값을 그대로 이어받아 감쇠시킨다 (CurrentPrice와 동일한 이월 패턴).
        stat.Support = previous.Support;
        stat.Growth = previous.Growth;
        TradeCalculator.Decay(stat);

        ApplyJob(stat);
        ApplySkills(stat);

        return stat;
    }

    /// <summary>
    /// 현재 직업의 Effect를 적용한다. Support/Growth는 선택 시점에 직접 반영되므로 여기서는 제외한다.
    /// </summary>
    private static void ApplyJob(PlayerStat stat)
    {
        JobSO job = JobManager.Instance.CurrentJob;

        if (job == null)
            return;

        foreach (EffectData effect in job.effects)
        {
            if (effect.effectType == EffectType.SupportIncrease || effect.effectType == EffectType.GrowthIncrease)
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
                stat.Support += effect.value;
            else if (effect.effectType == EffectType.GrowthIncrease)
                stat.Growth += effect.value;
        }
    }

    /// <summary>
    /// 활성화된 토글형(재사용 불가) 스킬들의 Effect를 매 턴 재적용한다.
    /// 재사용형 스킬은 사용 시점에 ApplySkillUse로 1회 반영되고 이후 감쇠하므로 여기서 제외한다.
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
                ApplyEffect(stat, effect);
            }
        }
    }

    /// <summary>
    /// 재사용형 스킬을 사용하는 순간, 그 스킬의 Support/Growth 효과를 stat에 직접 반영한다.
    /// </summary>
    public static void ApplySkillUse(PlayerStat stat, SkillSO skill)
    {
        if (skill == null)
            return;

        foreach (EffectData effect in skill.effects)
        {
            if (effect.effectType == EffectType.SupportIncrease)
                stat.Support += effect.value;
            else if (effect.effectType == EffectType.GrowthIncrease)
                stat.Growth += effect.value;
        }
    }

    /// <summary>
    /// Effect 하나를 PlayerStat에 적용한다.
    /// </summary>
    private static void ApplyEffect(PlayerStat stat, EffectData effect)
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

            default:
                Debug.LogWarning($"처리되지 않은 EffectType : {effect.effectType}");
                break;
        }
    }
}
