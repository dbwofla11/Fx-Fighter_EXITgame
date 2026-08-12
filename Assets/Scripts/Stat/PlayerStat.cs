using System.Collections.Generic;
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

    /// <summary>Volume이 버프처럼 유지되는 남은 턴 수. 0이 되면 다음 턴에 Volume이 0으로 리셋된다
    /// (BuffCalculator.TickVolumeBuff 참고). 매 턴 감쇠되는 Support/Growth와 달리 "N턴짜리 임시 효과"라
    /// 별도 카운트다운으로 관리한다.</summary>
    public int VolumeBuffTurnsRemaining;

    /// <summary>진행 중인 "의심도 하락"(여론조작 스킬) 버프 목록. 구매할 때마다(같은 스킬 재구매 포함)
    /// 새 항목이 추가되고, 매 턴 진행 중인 항목 전부의 PerTurn이 합산 차감된다 — 같은 스킬을 여러 번 사면
    /// 하락이 중복 적용된다(재구매 시 덮어쓰지 않음).
    /// BuffCalculator.StartDoubtDecline/TickDoubtDeclines 참고.</summary>
    public List<DoubtDeclineBuff> DoubtDeclines = new List<DoubtDeclineBuff>();

    // ==========================
    // UI 표시용 (Job/Skill 기여분만 별도 추적)
    // ==========================

    /// <summary>Job 선택 + 재사용형 Skill 구매가 준 Support 기여분만 별도 누적 (Trade/Event 제외, Support와 동일하게 감쇠). 개요 화면 표시용.</summary>
    public float JobSkillSupportBonus;

    /// <summary>Job 선택 + 재사용형 Skill 구매가 준 Growth 기여분만 별도 누적 (Trade/Event 제외, Growth와 동일하게 감쇠). 개요 화면 표시용.</summary>
    public float JobSkillGrowthBonus;

    /// <summary>Job 선택 + 재사용형 Skill 구매가 준 Doubt 기여분만 별도 누적 (Trade/Event 제외). Doubt와 동일하게 감쇠하지 않고 계속 누적된다. 개요 화면 표시용.</summary>
    public float JobSkillDoubtBonus;

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

    /// <summary>이번 턴 CurrentPrice 변화량 (정규 가격 변화 + 시사 이벤트 충격 합산). 감쇠/이월 없이 매 턴 새로 계산됨.</summary>
    public float PriceChangeThisTurn;

    /// <summary>PriceChangeThisTurn을 기준으로 한 스트리머 패널 반응 단계. UI가 이 값을 읽어 스프라이트/멘트를 표시한다.</summary>
    public StreamerReactionState StreamerReaction;

    /// <summary>화면에 노출되지 않는 스트리머 감정 지수(0~100, 중립 50 시작). 가격이 크게 흔들려도 매 턴
    /// StreamerReaction이 바로 튀지 않도록, 가격 변화를 완만하게 누적해 반영하는 완충값이다(MarketManager.
    /// UpdateStreamerReaction 참고). 20점 구간별로 StreamerReaction이 정해진다(StreamerReactionCalculator).</summary>
    public float StreamerIndex;

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
        VolumeBuffTurnsRemaining = 0;
        DoubtDeclines = new List<DoubtDeclineBuff>();

        JobSkillSupportBonus = 0;
        JobSkillGrowthBonus = 0;
        JobSkillDoubtBonus = 0;

        PositiveEventRate = 0;
        NegativeEventRate = 0;

        UpProbability = 0.5f;
        DownProbability = 0.5f;

        StreamerIndex = 50f;

        ExitUnlocked = false;
    }
}

/// <summary>진행 중인 "의심도 하락" 한 건의 출처 스킬, 이번 턴 차감량, 남은 턴 수.</summary>
public class DoubtDeclineBuff
{
    public SkillID SkillId;
    public float PerTurn;
    public int TurnsRemaining;
}