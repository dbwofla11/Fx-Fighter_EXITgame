/// <summary>
/// 게임 종료(엔딩) 종류.
/// </summary>
public enum EndingType
{
    /// <summary>체포 엔딩 (Bad Ending) : Doubt 100 도달</summary>
    Arrest,

    /// <summary>엑시트 엔딩 (Neutral Ending) : 목표 자산 달성 후 엑시트, Hero 조건 미충족</summary>
    Exit,
    /// <summary>거지 엔딩 : 현금 0 + 코인 0</summary>
    Broke
}
