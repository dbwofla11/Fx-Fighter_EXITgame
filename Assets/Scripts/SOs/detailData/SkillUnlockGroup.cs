/// <summary>
/// 시장조작·여론조작의 세부 카테고리 해금 키.
/// 실제 선행 스킬 연결은 SkillTreeConfigSO에서 데이터로 관리한다.
/// </summary>
public enum SkillUnlockGroup
{
    None,
    MarketVolume,
    MarketPrice,
    MarketPsychology,
    PropagandaSNS,
    PropagandaInfluencer,
    PropagandaMedia
}
