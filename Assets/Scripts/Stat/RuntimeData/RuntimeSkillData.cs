using System.Collections.Generic;

/// <summary>
/// 플레이어가 보유한 모든 스킬의 런타임 상태를 관리한다.
/// </summary>
public class RuntimeSkillData
{
    // 실 조작은 SkillManager에서 관리를함 

    /// <summary>모든 스킬의 런타임 정보</summary>
    /// 액티브의 여부는 스킬 개별관리를 한다. 이것은 오로지 런타임 데이터 유지용 리스트임
    /// 이 리스트에서 액티브된 것만 스탯적용 할 생각임
    /// 스킬 개별에 대한 정보는 skillRuntimeInfo에서 확인 가능함.
    public List<SkillRuntimeInfo> Skills = new();

    /// <summary>아이콘 클릭으로 선택된 스킬(재사용형 전용). 구매 버튼이 이 값을 기준으로 동작한다.</summary>
    public SkillID? SelectedSkillId;
}