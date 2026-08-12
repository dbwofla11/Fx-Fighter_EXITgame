using UnityEngine;

public static class StatCalculator
{
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

        // StreamerIndex도 매 턴 완만하게 누적되는 값이라 이월이 필요하다 — 안 그러면 매 턴 Reset()의 기본값
        // (50)에서 다시 시작해 완충 효과가 무의미해진다.
        stat.StreamerIndex = previous.StreamerIndex;

        // Support/Growth/Supply는 거래·직업 선택·시사 이벤트로 그 순간 직접 반영되는 값이라, 매 턴 새로 계산하지 않고
        // 이전 값을 그대로 이어받는다 (CurrentPrice와 동일한 이월 패턴). Support/Growth는 매 턴 감쇠하고,
        // Supply는 반대로 매 턴 자동으로 늘어난다(인플레이션, `TradeCalculator.GrowSupply`).
        stat.Support = previous.Support;
        stat.Growth = previous.Growth;
        stat.Supply = previous.Supply;
        stat.JobSkillSupportBonus = previous.JobSkillSupportBonus;
        stat.JobSkillGrowthBonus = previous.JobSkillGrowthBonus;
        stat.JobSkillDoubtBonus = previous.JobSkillDoubtBonus;

        // Volume(N턴 버프)/DoubtDecline(N턴 분할 하락) 카운트다운은 BuffCalculator가 전담한다.
        BuffCalculator.TickVolumeBuff(stat, previous);

        TradeCalculator.Decay(stat);
        TradeCalculator.GrowSupply(stat, CalculateSupplyGrowthSuppression());

        // Doubt는 감쇠하지 않고 계속 쌓이는 값이다 (시간이 지날수록 자동으로 100을 향해 오르다가 100이 되면
        // 게임오버가 되는 기획). 매 턴 새로 계산하지 않고 이전 값을 그대로 이어받는다.
        stat.Doubt = previous.Doubt;
        BuffCalculator.TickDoubtDeclines(stat, previous);

        ApplyJob(stat);
        ApplySkills(stat);

        return stat;
    }

    // Doubt 하한. 0이 아니라 -10인 이유 : 정치인 직업의 초기 Doubt -10 효과(현재 직업 밸런스 중 가장 큰
    // 음수 효과)가 0 하한에서는 선택 직후 바로 잘려 무효화되기 때문 — 이 값까지는 허용해 그 효과가 보이게 한다.
    private const float DoubtFloor = -10f;

    /// <summary>
    /// Support/Growth(-100~100)/Doubt(DoubtFloor~100)가 거래·이벤트·스킬 등으로 문서 범위를 벗어나지 않도록
    /// 강제하고, Supply가 보유 코인수량 밑으로 내려가지 않도록 막는다(발행량 조작/이벤트/스킬로 소각하다가
    /// 마이너스가 되는 버그가 있었다 — 플레이어가 들고 있는 코인보다 발행량이 적을 수는 없다는 게 최소 전제).
    /// 매 턴/시사 이벤트 수동 트리거가 끝나고 EventHub.OnMarketUpdated를 발행하기 직전에 호출한다.
    /// Doubt는 DoubtFloor 밑으로 내려가지 않는다(2026-08-09 버그 수정 — 기존엔 -100까지 허용해 이벤트로 깎일
    /// 때 한없이 마이너스로 내려가는 문제가 있었다).
    /// </summary>
    public static void ClampStat(PlayerStat stat)
    {
        stat.Support = Mathf.Clamp(stat.Support, -100f, 100f);
        stat.Growth = Mathf.Clamp(stat.Growth, -100f, 100f);
        stat.Doubt = Mathf.Clamp(stat.Doubt, DoubtFloor, 100f);
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
            ApplyOneShotBonusEffect(stat, effect, clampDoubtFloor: false);
    }

    /// <summary>
    /// 직업 선택(`ApplyJobSelection`)/스킬 사용(`ApplySkillUse`) 시점에 1회만 반영되는 Support/Growth/Doubt
    /// 효과의 공통 로직 — 개요 화면 표시용 그림자 필드(`JobSkillXBonus`)까지 같이 갱신한다. Doubt 감소만
    /// 스킬 쪽이 Job과 달리 0 밑으로 안 내려가는 차이가 있어(설계상 의도) `clampDoubtFloor`로 분기한다.
    /// Support/Growth/Doubt 외 타입(Supply/Volume 등)은 호출자가 각자 처리하므로 여기서 다루지 않는다.
    /// </summary>
    private static void ApplyOneShotBonusEffect(PlayerStat stat, EffectData effect, bool clampDoubtFloor)
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
            stat.Doubt = clampDoubtFloor ? Mathf.Max(0f, stat.Doubt - effect.value) : stat.Doubt - effect.value;
            stat.JobSkillDoubtBonus -= effect.value;
        }
        else if (effect.effectType == EffectType.DoubtIncrease)
        {
            stat.Doubt += effect.value;
            stat.JobSkillDoubtBonus += effect.value;
        }
    }

    #endregion

    #region 스킬 효과

    /// <summary>
    /// 활성화된(`IsEnabled`) 토글형(재사용 불가) 스킬들의 Effect를 매 턴 재적용한다.
    /// 재사용형 스킬은 사용 시점에 ApplySkillUse로 1회 반영되고 이후 감쇠하므로 여기서 제외한다.
    /// Support/Growth/DoubtIncrease/DoubtDecrease도 ApplyJob과 동일한 이유로 제외한다 — 구매 시점에
    /// ApplySkillUse가 이미 1회 반영했고, 이후 감쇠(Support/Growth)하거나 계속 누적(Doubt)되는 게 의도된
    /// 동작이라 매 턴 다시 더하면 무한정 쌓이는 버그가 된다(과거 Job의 DoubtDecrease 매 턴 재적용 버그와 동일
    /// 패턴, `Completed_Tasks.md` 참고). Supply/Volume도 감쇠·버프 만료 대상이라 제외한다. 남는 건
    /// CashBonus/PositiveEventRate/NegativeEventRate/ExitUnlock처럼 매 턴 새로 계산되는(감쇠 없는) 값들뿐이라
    /// 그대로 재적용해도 안전하다.
    /// </summary>
    private static void ApplySkills(PlayerStat stat)
    {
        var activeSkills = SkillManager.Instance.GetActiveSkills();

        foreach (SkillRuntimeInfo skill in activeSkills)
        {
            if (skill.Profile.isReusable)
                continue;

            ApplyToggleSkillEffects(stat, skill.Profile);
        }
    }

    /// <summary>
    /// 토글형 스킬 하나의 "매 턴 재적용" 대상 Effect만 stat에 반영한다. `ApplySkills()`(매 턴 전체 순회)와
    /// `SkillManager.HandlePurchase()`(방금 구매한 스킬 하나만 이번 턴에 바로 반영, `ApplySkillUse`가 못
    /// 채우는 CashBonus 등을 즉시 채우기 위함)가 같이 쓴다 — 전체 순회를 다시 부르면 이미 활성화된 다른
    /// 스킬들의 값까지 이번 턴에 중복으로 더해지므로, 방금 구매한 스킬 하나로 범위를 좁혀야 한다.
    /// </summary>
    public static void ApplyToggleSkillEffects(PlayerStat stat, SkillSO skillProfile)
    {
        foreach (EffectData effect in skillProfile.effects)
        {
            if (effect.effectType == EffectType.SupportIncrease || effect.effectType == EffectType.GrowthIncrease
                || effect.effectType == EffectType.DoubtIncrease || effect.effectType == EffectType.DoubtDecrease
                || effect.effectType == EffectType.DoubtDecline
                || effect.effectType == EffectType.SupplyIncrease || effect.effectType == EffectType.SupplyDecrease
                || effect.effectType == EffectType.SupplyGrowthSuppress
                || effect.effectType == EffectType.VolumeIncrease || effect.effectType == EffectType.VolumeDecrease
                || effect.effectType == EffectType.PriceShockPercent)
                continue;

            ApplyEffect(stat, effect);
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
            if (effect.effectType == EffectType.SupportIncrease || effect.effectType == EffectType.GrowthIncrease
                || effect.effectType == EffectType.DoubtIncrease || effect.effectType == EffectType.DoubtDecrease)
            {
                // 스킬로 인한 Doubt 감소는 Job과 달리 0 밑으로 내려가지 않는다(clampDoubtFloor: true).
                ApplyOneShotBonusEffect(stat, effect, clampDoubtFloor: true);
            }
            else if (effect.effectType == EffectType.SupplyIncrease)
                stat.Supply += effect.value;
            else if (effect.effectType == EffectType.SupplyDecrease)
                stat.Supply -= effect.value;
            else if (effect.effectType == EffectType.VolumeIncrease)
                BuffCalculator.StartVolumeBuff(stat, effect.value);
            else if (effect.effectType == EffectType.VolumeDecrease)
                BuffCalculator.StartVolumeBuff(stat, -effect.value);
            else if (effect.effectType == EffectType.DoubtDecline)
                // 여론조작 스킬 전용 — 총량(effect.value)을 즉시 깎지 않고 200턴에 걸쳐 0.5%씩 분할 차감 시작.
                // 스킬별로 독립적이라(BuffCalculator.StartDoubtDecline) 다른 스킬의 진행 중인 하락과 합산된다.
                BuffCalculator.StartDoubtDecline(stat, skill.id, effect.value);
            else if (effect.effectType == EffectType.PriceShockPercent)
            {
                // 시장조작 전용 — 구매 즉시 가격을 %만큼 그대로 흔든다(감쇠 없음, 이후 가격은 그 값에서 이어서 계산됨).
                stat.CurrentPrice *= 1f + effect.value / 100f;
                PriceCalculator.ClampPrice(stat);
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
                BuffCalculator.StartVolumeBuff(stat, effect.value);
                break;

            case EffectType.VolumeDecrease:
                BuffCalculator.StartVolumeBuff(stat, -effect.value);
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

            case EffectType.DoubtDecline:
                // stat 직접 반영 없음 — ApplySkillUse가 BuffCalculator.StartDoubtDecline으로 시작을 전담한다.
                break;

            case EffectType.PriceShockPercent:
                // stat 직접 반영 없음 — ApplySkillUse가 구매 시점에 직접 처리한다(1회성이라 매 턴 재적용 대상에서 제외).
                break;

            default:
                Debug.LogWarning($"처리되지 않은 EffectType : {effect.effectType}");
                break;
        }
    }

    #endregion
}
