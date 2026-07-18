using UnityEngine;

/// <summary>
/// 플레이어의 최종 스탯을 관리하는 런타임 데이터.
/// 직업 및 활성화된 스킬 효과를 모두 계산한 결과를 저장한다.
/// </summary>
public class PlayerStat
{
    /// <summary>지지도</summary>
    public float Support;

    /// <summary>의심도</summary>
    public float Doubt;

    /// <summary>발행량</summary>
    public float Supply;

    /// <summary>거래량</summary>
    public float Volume;

    /// <summary>코인 상승률</summary>
    public float CoinGrowthRate;

    /// <summary>긍정 이벤트 등장 확률</summary>
    public float PositiveEventRate;

    /// <summary>부정 이벤트 등장 확률</summary>
    public float NegativeEventRate;

    /// <summary>보유 자금</summary>
    public float Cash;

    /// <summary>엑시트 가능 여부</summary>
    public bool ExitUnlocked;

    /// <summary>
    /// 모든 스탯을 기본값으로 초기화한다.
    /// 직업 및 스킬 효과를 다시 계산하기 전에 호출한다.
    /// </summary>
    public void Reset()
    {
        Support = 0f;
        Doubt = 0f;

        Supply = 0f;
        Volume = 0f;

        PositiveEventRate = 0f;
        NegativeEventRate = 0f;

        Cash = 0f;

        ExitUnlocked = false;
    }
}