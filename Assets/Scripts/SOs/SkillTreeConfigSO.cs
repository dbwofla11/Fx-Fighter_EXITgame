using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세부 스킬 카테고리와 전용 코인설계 선행 스킬의 연결을 보관하는 데이터 자산.
/// 스킬 개별 asset이나 UI에 해금 관계를 중복해서 하드코딩하지 않는다.
/// </summary>
[CreateAssetMenu(
    fileName = "SkillTreeConfig",
    menuName = "Game/SkillTreeConfig"
)]
public class SkillTreeConfigSO : ScriptableObject
{
    [Serializable]
    public class UnlockRule
    {
        public SkillUnlockGroup group;
        public SkillID prerequisite;
    }

    [SerializeField]
    private List<UnlockRule> unlockRules = new();

    public bool TryGetPrerequisite(SkillUnlockGroup group, out SkillID prerequisite)
    {
        foreach (UnlockRule rule in unlockRules)
        {
            if (rule != null && rule.group == group)
            {
                prerequisite = rule.prerequisite;
                return true;
            }
        }

        prerequisite = default;
        return false;
    }
}
