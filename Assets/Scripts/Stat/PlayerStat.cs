using UnityEngine;

/// <summary>
/// 플레이어와 코인의 현재 상태를 저장하는 런타임 데이터.
/// 계산은 Calculator가 수행하고, 이 클래스는 결과를 보관한다.
/// </summary>
public class PlayerStat
{
    // ==========================
    // Player Status
    // ==========================
    /// <summary>코인 지지도 (-100 ~ 100)</summary>
    public float Support;

    /// <summary>코인 상승률 (-100 ~ 100)</summary>
    public float Growth;

    /// <summary>의심도</summary>
    public float Doubt;

    /// <summary>발행량</summary>
    public float Supply;

    /// <summary>거래량</summary>
    public float Volume;

    // ==========================
    // UI 표시용 (Job/Skill 기여분만 별도 추적)
    // ==========================

    /// <summary>Job 선택 + 재사용형 Skill 구매가 준 Support 기여분만 별도 누적 (Trade/Event 제외, Support와 동일하게 감쇠). 개요 화면 표시용.</summary>
    public float JobSkillSupportBonus;

    /// <summary>Job 선택 + 재사용형 Skill 구매가 준 Growth 기여분만 별도 누적 (Trade/Event 제외, Growth와 동일하게 감쇠). 개요 화면 표시용.</summary>
    public float JobSkillGrowthBonus;

    // ==========================
    // Event
    // ==========================

    /// <summary>긍정 이벤트 확률</summary>
    public float PositiveEventRate;

    /// <summary>부정 이벤트 확률</summary>
    public float NegativeEventRate;

    // ==========================
    // Market
    // ==========================

    /// <summary>현재 코인 가격</summary>
    public float CurrentPrice;
    public float CashBonus; // 현금 보너스 

    /// <summary>가격 상승 확률</summary>
    /// <summary>가격 하락 확률</summary>
    public float UpProbability;
    public float DownProbability;

    // ==========================
    // ETC
    // ==========================

    /// <summary>엑시트 가능 여부</summary>
    public bool ExitUnlocked;

    public void Reset()
    {
        Support = 0;
        Growth = 0;
        Doubt = 0;

        Supply = 0;
        Volume = 0;

        JobSkillSupportBonus = 0;
        JobSkillGrowthBonus = 0;

        PositiveEventRate = 0;
        NegativeEventRate = 0;

        UpProbability = 0.5f;
        DownProbability = 0.5f;

        ExitUnlocked = false;
    }
}