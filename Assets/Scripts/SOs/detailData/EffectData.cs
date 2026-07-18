using System;
using UnityEngine;

/// <summary>
/// 스킬, 직업, 이벤트 등이 가지는 단일 효과 데이터
/// </summary>
[System.Serializable]
public class EffectData
{
    /// <summary>
    /// 적용할 효과 종류
    /// </summary>
    public EffectType effectType;

    /// <summary>
    /// 효과 수치
    /// (예: Support +20, Cash Bonus +10%)
    /// </summary>
    public float value;
}

public enum EffectType
{
    /// <summary>
    /// 코인 지지도 증가
    /// </summary>
    SupportIncrease,

    /// <summary>
    /// 코인 상승도 증가
    /// </summary>
    GrowthIncrease,

    /// <summary>
    /// 의심도 감소
    /// </summary>
    DoubtDecrease,

    /// <summary>
    /// 긍정 이벤트 등장 확률 증가
    /// </summary>
    PositiveEventRate,

    /// <summary>
    /// 부정 이벤트 등장 확률 감소
    /// </summary>
    NegativeEventRate,

    /// <summary>
    /// 거래 수익 증가
    /// </summary>
    CashBonus,

    /// <summary>
    /// 코인 거래량 증가
    /// </summary>
    VolumeIncrease,

    /// <summary>
    /// 코인 거래량 감소
    /// </summary>
    VolumeDecrease,

    /// <summary>
    /// 엑시트(엔딩) 조건 해금
    /// </summary>
    ExitUnlock
}