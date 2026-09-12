using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    //여기 스킬메니저에서는 스킬의 Active여부만 판단해서 적용시킬뿐
    // Active를 직접 건들지 않음
    public static SkillManager Instance { get; private set; }

    [SerializeField] // 스킬에 대한 모든 정보를 미리 가지고옴
    private List<SkillSO> skillDatabase;

    [SerializeField]
    private SkillTreeConfigSO skillTreeConfig;

    // 거기서 런타임 Active된것만 필터링해서 스킬 적용시킴
    private RuntimeSkillData runtimeSkillData = new();

    // 재사용형 스킬별 1턴 구매 쿨타임. 다른 스킬 구매에는 영향을 주지 않는다.
    private readonly HashSet<SkillID> reusableSkillsOnCooldown = new();

    /// <summary>
    /// 아이콘 클릭으로 선택된 스킬(재사용형 전용). UI가 정보 패널을 그릴 때 참조한다.
    /// </summary>
    public SkillID? SelectedSkillId => runtimeSkillData.SelectedSkillId;

    #region 초기화

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 새 게임 시작 시 CharacterSelectUI가 호출한다 (DontDestroyOnLoad라 두 번째 플레이부터는 Awake가 다시 안 불림).
    public void ResetState()
    {
        Initialize();
        EventHub.RaiseSkillTreeChanged();
    }

    // 다 불러오는 초기화
    private void Initialize()
    {
        runtimeSkillData.Skills.Clear();
        runtimeSkillData.SelectedSkillId = null;
        reusableSkillsOnCooldown.Clear();

        if (skillDatabase == null)
            return;

        foreach (SkillSO skill in skillDatabase)
        {
            if (skill == null)
                continue;

            runtimeSkillData.Skills.Add(new SkillRuntimeInfo
            {
                Profile = skill,
                // 재사용형 스킬은 defaultUnlocked와 무관하게 항상 구매해야 사용 가능하다.
                IsUnlocked = skill.isReusable ? false : skill.defaultUnlocked,
                IsEnabled = false,
                PurchaseCount = 0
            });
        }
    }

    private void OnEnable()
    {
        EventHub.OnDayChanged += HandleDayChanged;
        EventHub.OnSkillClicked += HandleSkillClicked;
        EventHub.OnSkillPurchased += HandlePurchase;
    }

    private void OnDisable()
    {
        EventHub.OnDayChanged -= HandleDayChanged;
        EventHub.OnSkillClicked -= HandleSkillClicked;
        EventHub.OnSkillPurchased -= HandlePurchase;
    }

    #endregion

    #region 이벤트 처리 (클릭 / 구매)

    private void HandleDayChanged()
    {
        if (reusableSkillsOnCooldown.Count == 0)
            return;

        reusableSkillsOnCooldown.Clear();
        EventHub.RaiseSkillTreeChanged();
    }

    // 스킬 아이콘 클릭 요청 수신 : 재사용형/1회성 모두 선택 상태만 저장한다. 구매(잠금 해제)는 구매 버튼(HandlePurchase) 전용.
    private void HandleSkillClicked(SkillID id)
    {
        if (GetSkill(id) == null)
            return;

        runtimeSkillData.SelectedSkillId = id;
    }

    // 스킬 구매 버튼 클릭 요청 수신 : 선택된 스킬을 구매+적용한다.
    // 재사용형은 선행조건/최대횟수/쿨타임 안에서 재구매 가능. 1회성은 최초 구매로 영구 해금되고 이후 재구매가 막힌다.
    private void HandlePurchase()
    {
        if (runtimeSkillData.SelectedSkillId == null)
            return;

        SkillID id = runtimeSkillData.SelectedSkillId.Value;
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null)
        {
            EventHub.RaiseSkillPurchaseRejected(id, SkillPurchaseFailureReason.NotFound);
            return;
        }

        if (!CanPurchase(id, out SkillPurchaseFailureReason failureReason))
        {
            EventHub.RaiseSkillPurchaseRejected(id, failureReason);
            return;
        }

        long cost = CalculateCost(skill);

        if (!PlayerManager.Instance.TrySpend(cost))
        {
            EventHub.RaiseSkillPurchaseRejected(id, SkillPurchaseFailureReason.InsufficientFunds);
            return;
        }

        StatCalculator.ApplySkillUse(MarketManager.Instance.CurrentStat, skill.Profile);
        StatCalculator.ClampStat(MarketManager.Instance.CurrentStat);

        // 재사용형은 즉시 현금 지급(GrantCashBonus), 재사용 불가(영구형)는 "현금증가량"(PlayerStat.CashBonus) %
        // 버프로 반영한다 — 후자를 여기서 즉시 한 번 채워두지 않으면 이번 턴이 끝나기 전까지 0으로 비어있다.
        if (skill.Profile.isReusable)
        {
            GrantCashBonus(skill.Profile);
            skill.IsUnlocked = true;
            reusableSkillsOnCooldown.Add(skill.Profile.id);
        }
        else
        {
            skill.IsUnlocked = true;
            // 1회성 스킬은 끄는 UI가 없다 — 구매=영구 활성으로 취급해 ApplySkills()가 매 턴 재적용하게 한다.
            EnableSkill(skill.Profile.id);
            // 방금 산 이 스킬 하나만 이번 턴에 바로 반영(전체 활성 스킬을 다시 돌리면 기존 스킬 값이 중복 적용됨).
            StatCalculator.ApplyToggleSkillEffects(MarketManager.Instance.CurrentStat, skill.Profile);
        }

        skill.PurchaseCount++;

        EventHub.RaiseSkillPurchaseSucceeded(skill.Profile.id);
        EventHub.RaiseSkillTreeChanged();
        EventHub.RaiseMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    // 재사용형 스킬의 CashBonus 효과는 Support/Growth와 동일하게 "구매 시점 1회성"으로 처리한다.
    // CashBonus는 매 턴 새로 계산되는 PlayerStat.CashBonus에 이월/감쇠되지 않으므로(Job/토글형 스킬처럼 매 턴
    // 재적용되는 값이 아니라서), 구매 즉시 현금을 직접 지급하는 방식으로 반영한다.
    private void GrantCashBonus(SkillSO skill)
    {
        foreach (EffectData effect in skill.effects)
        {
            if (effect.effectType != EffectType.CashBonus)
                continue;

            long bonus = (long)(PlayerManager.Instance.currentMoney * (effect.value / 100f));
            PlayerManager.Instance.AddMoney(bonus);
        }
    }

    #endregion

    #region 조회 (Unlocked / Profile / Cost)

    // 해당 스킬을 구매(재사용형)했거나 해금(토글형)했는지 여부. 다른 기능의 사용 가능 조건으로 참조된다
    // (예: 발행량 조작 버튼은 추가발행권한을 구매하기 전까지 사용할 수 없다).
    public bool IsUnlocked(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        return skill != null && skill.IsUnlocked;
    }

    /// <summary>
    /// 해당 스킬의 카테고리 해금 조건과 개별 선행 스킬이 모두 충족됐는지 반환한다.
    /// 카테고리별 연결은 SkillTreeConfigSO가 담당하며, UI나 개별 스킬에 하드코딩하지 않는다.
    /// </summary>
    public bool ArePrerequisitesMet(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        if (skill == null)
            return false;

        if (!IsCategoryUnlockMet(id))
            return false;

        if (skill.Profile.prerequisites == null)
            return true;

        foreach (SkillID prerequisite in skill.Profile.prerequisites)
        {
            if (!IsUnlocked(prerequisite))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 시장조작·여론조작 세부 카테고리의 전용 코인설계 선행 조건을 검사한다.
    /// 코인설계 고유 기능, 방어, 추가발행권한처럼 unlockGroup이 None인 스킬은 독립 구매 가능하다.
    /// </summary>
    public bool IsCategoryUnlockMet(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        if (skill == null)
            return false;

        SkillUnlockGroup group = skill.Profile.unlockGroup;
        if (group == SkillUnlockGroup.None)
            return true;

        return skillTreeConfig != null &&
            skillTreeConfig.TryGetPrerequisite(group, out SkillID prerequisite) &&
            IsUnlocked(prerequisite);
    }

    // 기존 호출부 호환용. v0.4에서는 단계가 아니라 세부 카테고리 해금 여부를 반환한다.
    public bool IsUnlockStageMet(SkillID id) => IsCategoryUnlockMet(id);

    public List<SkillID> GetMissingPrerequisites(SkillID id)
    {
        List<SkillID> missing = new();
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null)
            return missing;

        if (!IsCategoryUnlockMet(id) && skillTreeConfig != null &&
            skillTreeConfig.TryGetPrerequisite(skill.Profile.unlockGroup, out SkillID categoryPrerequisite) &&
            !IsUnlocked(categoryPrerequisite))
        {
            missing.Add(categoryPrerequisite);
        }

        if (skill.Profile.prerequisites == null)
            return missing;

        foreach (SkillID prerequisite in skill.Profile.prerequisites)
        {
            if (!IsUnlocked(prerequisite) && !missing.Contains(prerequisite))
                missing.Add(prerequisite);
        }

        return missing;
    }

    /// <summary>
    /// 구매 가능 여부를 SkillManager 한 곳에서 판정한다. UI는 이 결과를 표시만 하고,
    /// EventHub 구매 요청도 반드시 이 판정을 거친다.
    /// </summary>
    public bool CanPurchase(SkillID id, out SkillPurchaseFailureReason failureReason)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null)
        {
            failureReason = SkillPurchaseFailureReason.NotFound;
            return false;
        }

        if (!ArePrerequisitesMet(id))
        {
            failureReason = SkillPurchaseFailureReason.MissingPrerequisite;
            return false;
        }

        if (!skill.Profile.isReusable && skill.IsUnlocked)
        {
            failureReason = SkillPurchaseFailureReason.AlreadyPurchased;
            return false;
        }

        if (skill.Profile.isReusable && skill.PurchaseCount >= skill.Profile.maxPurchaseCount)
        {
            failureReason = SkillPurchaseFailureReason.MaxPurchaseCount;
            return false;
        }

        if (skill.Profile.isReusable && reusableSkillsOnCooldown.Contains(skill.Profile.id))
        {
            failureReason = SkillPurchaseFailureReason.OnCooldown;
            return false;
        }

        if (PlayerManager.Instance == null || PlayerManager.Instance.currentMoney < CalculateCost(skill))
        {
            failureReason = SkillPurchaseFailureReason.InsufficientFunds;
            return false;
        }

        if (!IsSkillDoubtSafe(id))
        {
            failureReason = SkillPurchaseFailureReason.DoubtUnsafe;
            return false;
        }

        failureReason = default;
        return true;
    }

    /// <summary>
    /// 구매 즉시 적용되는 Doubt 변화만 기준으로 체포 임계치 초과 여부를 판정한다.
    /// DoubtDecline은 매 턴 분할 적용되므로 구매 시점 판정에서 제외한다.
    /// </summary>
    public bool IsSkillDoubtSafe(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        if (skill == null || MarketManager.Instance == null || MarketManager.Instance.CurrentStat == null)
            return true;

        float predictedDoubt = MarketManager.Instance.CurrentStat.Doubt;
        if (skill.Profile.effects == null)
            return true;

        foreach (EffectData effect in skill.Profile.effects)
        {
            if (effect == null)
                continue;

            if (effect.effectType == EffectType.DoubtIncrease)
                predictedDoubt += effect.value;
            else if (effect.effectType == EffectType.DoubtDecrease)
                predictedDoubt -= effect.value;
        }

        return predictedDoubt < EndingCalculator.ArrestDoubtThreshold;
    }

    /// <summary>현재 활성화된 영구형 스킬에서 특정 효과의 총합을 읽는다.</summary>
    public float GetActiveEffectTotal(EffectType effectType)
    {
        float total = 0f;

        foreach (SkillRuntimeInfo skill in runtimeSkillData.Skills)
        {
            if (skill == null || !skill.IsEnabled || skill.Profile == null || skill.Profile.effects == null)
                continue;

            foreach (EffectData effect in skill.Profile.effects)
            {
                if (effect != null && effect.effectType == effectType)
                    total += effect.value;
            }
        }

        return total;
    }

    // 스킬 정보 패널용 : 해당 스킬의 정적 데이터(설명/아이콘/효과 등)를 조회한다. UI가 SelectedSkillId로 조회.
    public SkillSO GetSkillProfile(SkillID id)
    {
        return GetSkill(id)?.Profile;
    }

    // 스킬 정보 패널용 : 구매 횟수가 반영된 현재 비용을 조회한다 (baseCost × costMultiplier^PurchaseCount).
    public long GetCurrentCost(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        return skill == null ? 0 : CalculateCost(skill);
    }

    // 스킬 정보 패널용 : 재사용형 스킬이 최대 구매 횟수(Profile.maxPurchaseCount)에 도달했는지 조회.
    public bool IsMaxedOut(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        return skill != null && skill.Profile.isReusable && skill.PurchaseCount >= skill.Profile.maxPurchaseCount;
    }

    public bool IsOnCooldown(SkillID id)
    {
        return reusableSkillsOnCooldown.Contains(id);
    }

    // 스킬 정보 패널용 : 해당 스킬을 몇 번 구매했는지 조회한다.
    public int GetPurchaseCount(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        return skill == null ? 0 : skill.PurchaseCount;
    }

    // 활성화 된거 IsEnabled = true인것만 가지고 오는거
    // 이거 대충 계산기에서 가지고 가서 사용할거임
    public IReadOnlyList<SkillRuntimeInfo> GetActiveSkills()
    {
        List<SkillRuntimeInfo> active = new();

        foreach (var skill in runtimeSkillData.Skills)
        {
            if (skill != null && skill.IsEnabled)
            // 이거의 여부로 스킬을 찍엇는지 안찍었는지 판단하고
            // 액티브 리스트에다가 넣음
                active.Add(skill);
        }

        return active;
    }

    private SkillRuntimeInfo GetSkill(SkillID id)
    {
        foreach (var skill in runtimeSkillData.Skills)
        {
            if (skill != null && skill.Profile != null && skill.Profile.id == id)
                return skill;
        }

        return null;
    }

    // cost = baseCost × costMultiplier^PurchaseCount
    private long CalculateCost(SkillRuntimeInfo skill)
    {
        return (long)(skill.Profile.baseCost * Mathf.Pow(skill.Profile.costMultiplier, skill.PurchaseCount));
    }

    #endregion

    #region 활성화 토글

    // 1회성(재사용 불가) 스킬 구매 시 HandlePurchase가 호출한다 — 끄는 UI가 없으므로 구매=영구 활성.
    public void EnableSkill(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null || !skill.IsUnlocked)
            return;

        skill.IsEnabled = true;
    }

    #endregion
}
