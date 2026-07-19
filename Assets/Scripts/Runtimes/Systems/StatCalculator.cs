using UnityEngine;

public static class StatCalculator
{  
    /// <summary>
    /// 이번 턴의 스탯을 계산한다.
    /// </summary>
    public static void Calculate(PlayerStat stat)
    {
        stat.Reset();

        ApplyJob(stat);
        ApplySkills(stat);

        // 추후 구현
        // ApplyEvents(stat);
        // ApplyTrade(stat);
    }

    /// <summary>
    /// 현재 직업의 Effect를 적용한다.
    /// </summary>
    private static void ApplyJob(PlayerStat stat)
    {
        JobSO job = JobManager.Instance.CurrentJob;

        if (job == null)
            return;

        foreach (EffectData effect in job.effects)
        {
            ApplyEffect(stat, effect);
        }
    }

    /// <summary>
    /// 활성화된 스킬들의 Effect를 적용한다.
    /// </summary>
    private static void ApplySkills(PlayerStat stat)
    {
        var activeSkills = SkillManager.Instance.GetActiveSkills();

        foreach (SkillRuntimeInfo skill in activeSkills)
        {
            foreach (EffectData effect in skill.Profile.effects)
            {
                ApplyEffect(stat, effect);
            }
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