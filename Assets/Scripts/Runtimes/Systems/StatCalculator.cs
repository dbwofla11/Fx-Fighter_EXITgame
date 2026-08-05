using UnityEngine;

public static class StatCalculator
{
    // Volume이 스킬/이벤트로 오른 뒤 자동으로 0으로 리셋되기까지 유지되는 턴 수 (버프 지속시간).
    private const int VolumeBuffDurationTurns = 30;

    #region 턴 계산

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
        // 이전 값을 그대로 이어받는다 (CurrentPrice와 동일한 이월 패턴). Support/Growth는 매 턴 감쇠하고,
        // Supply는 반대로 매 턴 자동으로 늘어난다(인플레이션, `TradeCalculator.GrowSupply`).
        stat.Support = previous.Support;
        stat.Growth = previous.Growth;
        stat.Supply = previous.Supply;
        stat.JobSkillSupportBonus = previous.JobSkillSupportBonus;
        stat.JobSkillGrowthBonus = previous.JobSkillGrowthBonus;
        stat.JobSkillDoubtBonus = previous.JobSkillDoubtBonus;

        // Volume은 Support/Growth처럼 감쇠하며 이어지는 게 아니라, VolumeBuffTurnsRemaining이 0이 될 때까지만
        // "버프"로 유지되다가 만료되면 다음 턴부터 0으로 완전히 리셋된다(위 Reset()이 이미 0으로 잡아둠).
        if (previous.VolumeBuffTurnsRemaining > 0)
        {
            stat.Volume = previous.Volume;
            stat.VolumeBuffTurnsRemaining = previous.VolumeBuffTurnsRemaining - 1;
        }

        TradeCalculator.Decay(stat);
        TradeCalculator.GrowSupply(stat, CalculateSupplyGrowthSuppression());

        // Doubt는 감쇠하지 않고 계속 쌓이는 값이다 (시간이 지날수록 자동으로 100을 향해 오르다가 100이 되면
        // 게임오버가 되는 기획). 매 턴 새로 계산하지 않고 이전 값을 그대로 이어받는다.
        stat.Doubt = previous.Doubt;

        ApplyJob(stat);
        ApplySkills(stat);
        ApplyUnlockedPermanentSkillCashBonus(stat);

        return stat;
    }

    /// <summary>
    /// Support/Growth/Doubt(모두 -100~100)가 거래·이벤트·스킬 등으로 문서 범위를 벗어나지 않도록 강제하고,
    /// Supply가 보유 코인수량 밑으로 내려가지 않도록 막는다(발행량 조작/이벤트/스킬로 소각하다가 마이너스가
    /// 되는 버그가 있었다 — 플레이어가 들고 있는 코인보다 발행량이 적을 수는 없다는 게 최소 전제).
    /// 매 턴/시사 이벤트 수동 트리거가 끝나고 EventHub.OnMarketUpdated를 발행하기 직전에 호출한다.
    /// Doubt 하한을 0이 아니라 -100으로 둔 이유 : 하한이 0이면 "일반인" 직업의 초기 -10% 감소 효과가 기본값
    /// 0에서 즉시 0으로 다시 잘려 사실상 무효화됐다. Support/Growth와 동일한 하한으로 맞춰 그 효과가 실제로
    /// 보이게 했다.
    /// </summary>
    public static void ClampStat(PlayerStat stat)
    {
        stat.Support = Mathf.Clamp(stat.Support, -100f, 100f);
        stat.Growth = Mathf.Clamp(stat.Growth, -100f, 100f);
        stat.Doubt = Mathf.Clamp(stat.Doubt, -100f, 100f);
        stat.Supply = Mathf.Max(stat.Supply, PlayerManager.Instance.currentCoins);
    }

    #endregion

    #region 직업 효과

    /// <summary>
    /// 현재 직업의 Effect를 적용한다. Support/Growth/Doubt는 선택 시점에 한 번만(ApplyJobSelection) 직접
    /// 반영되므로 여기서는 제외한다 — Doubt는 감쇠 없이 계속 누적되는 값이라 매 턴 재적용하면 무한정 깎이거나
    /// 오르는 버그가 생긴다(실제로 겪은 버그, 최초 -10% 같은 1회성 초기 효과가 매 턴 반복 적용됐었음).
    /// Supply도 매 턴 재적용하면 동일한 문제가 생기므로 제외한다(Job은 애초에 Supply를 다루지 않는 설계지만,
    /// 방어적으로 막아둔다).
    /// `JobManager.SelectJob`이 `ApplyJobSelection` 직후 한 번 더 호출한다 — CashBonus 등은 이 함수에서만
    /// 채워지는데, 여기서 직접 안 부르면 선택 후 첫 턴이 지나기 전까지 0으로 비어있는 상태가 된다.
    /// </summary>
    public static void ApplyJob(PlayerStat stat)
    {
        JobSO job = JobManager.Instance.CurrentJob;

        if (job == null)
            return;

        foreach (EffectData effect in job.effects)
        {
            if (effect.effectType == EffectType.SupportIncrease
                || effect.effectType == EffectType.GrowthIncrease
                || effect.effectType == EffectType.DoubtDecrease
                || effect.effectType == EffectType.DoubtIncrease
                || effect.effectType == EffectType.SupplyIncrease
                || effect.effectType == EffectType.SupplyDecrease)
                continue;

            ApplyEffect(stat, effect);
        }
    }

    /// <summary>
    /// 직업을 선택하는 순간, 그 직업의 Support/Growth/Doubt 효과를 stat에 1회만 직접 반영한다.
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
            else if (effect.effectType == EffectType.DoubtDecrease)
            {
                stat.Doubt -= effect.value;
                stat.JobSkillDoubtBonus -= effect.value;
            }
            else if (effect.effectType == EffectType.DoubtIncrease)
            {
                stat.Doubt += effect.value;
                stat.JobSkillDoubtBonus += effect.value;
            }
        }
    }

    #endregion

    #region 스킬 효과

    /// <summary>
    /// 활성화된 토글형(재사용 불가) 스킬들의 Effect를 매 턴 재적용한다.
    /// 재사용형 스킬은 사용 시점에 ApplySkillUse로 1회 반영되고 이후 감쇠하므로 여기서 제외한다.
    /// Supply/Volume도 감쇠·버프 만료 대상이라 토글형 스킬에서 매 턴 재적용하면 무한정 증가하므로 제외한다.
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
                if (effect.effectType == EffectType.SupplyIncrease || effect.effectType == EffectType.SupplyDecrease
                    || effect.effectType == EffectType.SupplyGrowthSuppress
                    || effect.effectType == EffectType.VolumeIncrease || effect.effectType == EffectType.VolumeDecrease)
                    continue;

                // 스킬로 인한 감소는 Job과 달리 0 밑으로 내려가지 않는다 (ApplyEffect는 Event에서도 재사용되므로 여기서만 분기).
                if (effect.effectType == EffectType.DoubtDecrease)
                {
                    stat.Doubt = Mathf.Max(0f, stat.Doubt - effect.value);
                    continue;
                }

                ApplyEffect(stat, effect);
            }
        }
    }

    /// <summary>
    /// 재사용 불가(영구형) 스킬의 CashBonus 효과를 매 턴 다시 채운다. `추가발행권한`처럼 CashBonus를 가진
    /// 재사용 불가 스킬은 "구매하면 영구 해금"이라 `SkillManager.IsUnlocked`가 곧 활성 상태다 — 토글(`IsEnabled`)
    /// 시스템은 아직 아무 스킬도 쓰지 않는 죽은 기능이라(`SkillManager.GetActiveSkills` 참고) 여기 의존하지 않는다.
    /// 현재 CashBonus를 가진 재사용 불가 스킬이 `추가발행권한` 하나뿐이라 최소 범위로 이것만 확인한다 — 토글형
    /// 스킬이 여러 개로 늘어나면 일반화된 활성화 시스템으로 다시 정리해야 한다.
    /// </summary>
    public static void ApplyUnlockedPermanentSkillCashBonus(PlayerStat stat)
    {
        if (SkillManager.Instance == null || !SkillManager.Instance.IsUnlocked(SkillID.추가발행권한))
            return;

        SkillSO skill = SkillManager.Instance.GetSkillProfile(SkillID.추가발행권한);

        if (skill == null)
            return;

        foreach (EffectData effect in skill.effects)
        {
            if (effect.effectType == EffectType.CashBonus)
                stat.CashBonus += effect.value;
        }
    }

    /// <summary>
    /// 발행량 관련 재사용 불가 스킬(추가발행권한/우회발행권한)이 해금돼있으면 그 SupplyGrowthSuppress 효과를
    /// 합산해, 매 턴 자동 발행량 증가분(TradeCalculator.GrowSupply)을 얼마나 억제할지(0~1 비율)를 계산한다.
    /// 위 CashBonus와 동일한 이유로 최소 범위(알려진 두 스킬만)로 직접 확인한다 — 토글형 스킬이 여러 개로
    /// 늘어나면 일반화된 활성화 시스템으로 다시 정리해야 한다.
    /// </summary>
    private static float CalculateSupplyGrowthSuppression()
    {
        if (SkillManager.Instance == null)
            return 0f;

        float suppressPercent = 0f;
        suppressPercent += SumSupplyGrowthSuppress(SkillID.추가발행권한);
        suppressPercent += SumSupplyGrowthSuppress(SkillID.우회발행권한);

        return Mathf.Clamp01(suppressPercent / 100f);
    }

    private static float SumSupplyGrowthSuppress(SkillID id)
    {
        if (!SkillManager.Instance.IsUnlocked(id))
            return 0f;

        SkillSO skill = SkillManager.Instance.GetSkillProfile(id);
        if (skill == null)
            return 0f;

        float sum = 0f;
        foreach (EffectData effect in skill.effects)
            if (effect.effectType == EffectType.SupplyGrowthSuppress)
                sum += effect.value;

        return sum;
    }

    /// <summary>
    /// 스킬을 구매(재사용형은 매 구매, 1회성은 최초 구매)하는 순간, 그 스킬의 Support/Growth/Doubt/Supply/Volume 효과를 stat에 직접 반영한다.
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
            {
                // 스킬로 인한 감소는 Job과 달리 0 밑으로 내려가지 않는다.
                stat.Doubt = Mathf.Max(0f, stat.Doubt - effect.value);
                stat.JobSkillDoubtBonus -= effect.value;
            }
            else if (effect.effectType == EffectType.DoubtIncrease)
            {
                stat.Doubt += effect.value;
                stat.JobSkillDoubtBonus += effect.value;
            }
            else if (effect.effectType == EffectType.SupplyIncrease)
                stat.Supply += effect.value;
            else if (effect.effectType == EffectType.SupplyDecrease)
                stat.Supply -= effect.value;
            else if (effect.effectType == EffectType.VolumeIncrease)
            {
                // N턴짜리 버프 — 다시 쓰면 값은 쌓이고 지속시간은 새로 갱신된다.
                stat.Volume += effect.value;
                stat.VolumeBuffTurnsRemaining = VolumeBuffDurationTurns;
            }
            else if (effect.effectType == EffectType.VolumeDecrease)
            {
                stat.Volume -= effect.value;
                stat.VolumeBuffTurnsRemaining = VolumeBuffDurationTurns;
            }
        }
    }

    #endregion

    #region 공용 Effect 적용

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
                // N턴짜리 버프 — 다시 적용되면 값은 쌓이고 지속시간은 새로 갱신된다.
                stat.Volume += effect.value;
                stat.VolumeBuffTurnsRemaining = VolumeBuffDurationTurns;
                break;

            case EffectType.VolumeDecrease:
                stat.Volume -= effect.value;
                stat.VolumeBuffTurnsRemaining = VolumeBuffDurationTurns;
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

            case EffectType.SupplyGrowthSuppress:
                // stat 직접 반영 없음 — CalculateSupplyGrowthSuppression()이 매 턴 별도로 읽어서 GrowSupply에 넘긴다.
                break;

            default:
                Debug.LogWarning($"처리되지 않은 EffectType : {effect.effectType}");
                break;
        }
    }

    #endregion
}
