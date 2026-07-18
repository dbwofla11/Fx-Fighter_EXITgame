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
                IsUnlocked = skill.defaultUnlocked,
                IsEnabled = false,
                PurchaseCount = 0
            });
        }
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


    public void PurchaseSkill(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null)
            return;

        skill.IsUnlocked = true;
        skill.PurchaseCount++;
    }

    public void EnableSkill(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null || !skill.IsUnlocked)
            return;

        skill.IsEnabled = true;
    }

    public void DisableSkill(SkillID id)
    {
        SkillRuntimeInfo skill = GetSkill(id);

        if (skill == null)
            return;

        skill.IsEnabled = false;
    }

    // 활성화 된거 IsEnabled = true인것만 가지고 오는거
    // 이거 대충 계산기에서 가지고 가서 사용할거임 
    public List<SkillRuntimeInfo> GetActiveSkills()
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