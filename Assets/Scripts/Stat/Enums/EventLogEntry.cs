using System;
using System.Collections.Generic;

/// <summary>
/// 발생한 시사 이벤트 1건의 기록 (이벤트 로그 UI가 이 리스트를 그대로 그릴 수 있다).
/// </summary>
public class EventLogEntry
{
    public EventSO Profile;

    public DateTime Date;

    public int ChoiceIndex = -1;
    public string ChoiceLabel;
    public bool HasChoiceResult;
    public float SuccessProbability;
    public bool Succeeded;
    public long CashBefore;
    public long CashAfter;
    public long DebtBefore;
    public long DebtAfter;
    // 결과 모달/이벤트 로그에서 선택형 이벤트가 직접 제공하는 요약 문구.
    // 정기 이자 이벤트처럼 일반 EffectData만으로 표현하기 어려운 결과에 사용한다.
    public string ResultTitle;
    public string ResultSummary;
    public List<EffectData> ResultEffects;
    public float ResultSupplyDelta;
    public float ResultPriceRatio;

    // 이자 기록은 시사 이벤트 SO 없이 지급 당시의 현금과 실제 지급액을 보존한다.
    public long? CashInterest;
    public long InterestPrincipal;
}
