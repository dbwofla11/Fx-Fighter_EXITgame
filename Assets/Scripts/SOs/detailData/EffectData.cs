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
    ExitUnlock,

    // 아래는 이번 세션에서 추가한 항목이다. Unity는 enum을 선언 순서(정수값)로 직렬화하므로,
    // 기존에 저장된 .asset 데이터(예: 추가발행권한.asset의 effectType: 5 = CashBonus)가 깨지지 않도록
    // 반드시 기존 항목 다음, 맨 끝에만 추가한다 (중간 삽입 금지).

    /// <summary>
    /// 의심도 증가
    /// </summary>
    DoubtIncrease,

    /// <summary>
    /// 발행량 증가
    /// </summary>
    SupplyIncrease,

    /// <summary>
    /// 발행량 감소
    /// </summary>
    SupplyDecrease,

    /// <summary>
    /// 매 턴 자동 발행량 증가분(TradeCalculator.SupplyGrowthPerTurn)을 억제하는 비율(%). 재사용 불가
    /// (영구 해금형) 스킬 전용 — StatCalculator.CalculateSupplyGrowthSuppression이 별도로 읽어서 매 턴
    /// 반영하고, SupplyIncrease/Decrease와 마찬가지로 ApplySkills()의 매 턴 재적용 루프에서는 제외한다.
    /// </summary>
    SupplyGrowthSuppress
}