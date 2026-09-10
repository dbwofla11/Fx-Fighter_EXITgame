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
    public List<EffectData> ResultEffects;
    public float ResultSupplyDelta;
    public float ResultPriceRatio;

    // 이자 기록은 시사 이벤트 SO 없이 지급 당시의 현금과 실제 지급액을 보존한다.
    public long? CashInterest;
    public long InterestPrincipal;
}
