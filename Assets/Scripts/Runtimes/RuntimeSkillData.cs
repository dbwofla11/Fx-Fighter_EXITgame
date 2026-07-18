using System.Collections.Generic;

/// <summary>
/// 플레이어가 보유한 스킬의 런타임 상태를 관리한다.
/// </summary>
public class RuntimeSkillData
{
    /// <summary>활성화된 스킬 목록</summary>
    public List<SkillSO> ActiveSkills = new();

    /// <summary>구매 횟수</summary>
    public Dictionary<SkillID, int> PurchaseCounts = new();
}

