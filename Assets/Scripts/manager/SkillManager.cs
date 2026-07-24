using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    //여기 스킬메니저에서는 스킬의 Active여부만 판단해서 적용시킬뿐 
    // Active를 직접 건들지 않음
    public static SkillManager Instance { get; private set; }

    [SerializeField] // 스킬에 대한 모든 정보를 미리 가지고옴 
    private List<SkillSO> skillDatabase;

    // 거기서 런타임 Active된것만 필터링해서 스킬 적용시킴
    private RuntimeSkillData runtimeSkillData = new();

    /// <summary>
    /// 아이콘 클릭으로 선택된 스킬(재사용형 전용). UI가 정보 패널을 그릴 때 참조한다.
    /// </summary>
    public SkillID? SelectedSkillId => runtimeSkillData.SelectedSkillId;

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

    // 다 불러오는 초기화 
    private void Initialize()
    {
        runtimeSkillData.Skills.Clear();

        foreach (SkillSO skill in skillDatabase)
        {
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
        EventHub.OnSkillClicked += HandleSkillClicked;
        EventHub.OnSkillPurchased += HandlePurchase;
    }

    private void OnDisable()
    {
        EventHub.OnSkillClicked -= HandleSkillClicked;
        EventHub.OnSkillPurchased -= HandlePurchase;
    }

    // 스킬 아이콘 클릭 요청 수신 : 재사용형은 선택 상태만 저장, 토글형은 활성화/비활성화한다.
    private void HandleSkillClicked(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null)
            return;

        if (skill.Profile.isReusable)
        {
            runtimeSkillData.SelectedSkillId = id;
            return;
        }

        if (!skill.IsUnlocked)
            return;

        if (skill.IsEnabled)
            DisableSkill(id);
        else
            EnableSkill(id);
    }

    // 스킬 구매 버튼 클릭 요청 수신 (재사용형 스킬 전용) : 선택된 스킬을 구매+적용한다. 잠기지 않는다.
    private void HandlePurchase()
    {
        if (runtimeSkillData.SelectedSkillId == null)
            return;

        SkillRuntimeInfo skill = GetSkill(runtimeSkillData.SelectedSkillId.Value);

        if (skill == null || !skill.Profile.isReusable)
            return;

        long cost = CalculateCost(skill);

        if (!PlayerManager.Instance.TrySpend(cost))
            return;

        StatCalculator.ApplySkillUse(MarketManager.Instance.CurrentStat, skill.Profile);
        skill.IsUnlocked = true;
        skill.PurchaseCount++;
    }

    // 해당 스킬을 구매(재사용형)했거나 해금(토글형)했는지 여부. 다른 기능의 사용 가능 조건으로 참조된다
    // (예: 발행량 조작 버튼은 추가발행권한을 구매하기 전까지 사용할 수 없다).
    public bool IsUnlocked(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);
        return skill != null && skill.IsUnlocked;
    }

    private SkillRuntimeInfo GetSkill(SkillID id)
    {
        foreach (var skill in runtimeSkillData.Skills)
        {
            if (skill.Profile.id == id)
                return skill;
        }

        return null;
    }

    // cost = baseCost × costMultiplier^PurchaseCount
    private long CalculateCost(SkillRuntimeInfo skill)
    {
        return (long)(skill.Profile.baseCost * Mathf.Pow(skill.Profile.costMultiplier, skill.PurchaseCount));
    }
    // UI에서 불러다 쓰기 
    public void EnableSkill(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null || !skill.IsUnlocked)
            return;

        skill.IsEnabled = true;
    }
    // UI에서 불러다 쓰기 -> 스킬 봔환시 사용  
    public void DisableSkill(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null)
            return;

        skill.IsEnabled = false;
    }

    // 활성화 된거 IsEnabled = true인것만 가지고 오는거
    // 이거 대충 계산기에서 가지고 가서 사용할거임 
    public IReadOnlyList<SkillRuntimeInfo> GetActiveSkills()
    {
        List<SkillRuntimeInfo> active = new();

        foreach (var skill in runtimeSkillData.Skills)
        {
            if (skill.IsEnabled) 
            // 이거의 여부로 스킬을 찍엇는지 안찍었는지 판단하고 
            // 액티브 리스트에다가 넣음 
                active.Add(skill);
        }

        return active;
    }


}