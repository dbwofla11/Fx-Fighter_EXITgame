using System;
using System.Collections.Generic;

/// <summary>
/// 선택형 시사 이벤트의 선택지와 성공/실패 결과를 정의한다.
/// 확률/비용은 선택 순간의 상태로 계산하고, 효과는 결과가 확정된 뒤 한 번만 적용한다.
/// </summary>
[Serializable]
public class EventChoice
{
    public string label;

    [UnityEngine.Range(0f, 1f)]
    public float baseSuccessProbability = 0.5f;
    public float skillBonus;
    public float jobBonus;

    [UnityEngine.Range(0f, 1f)]
    public float doubtPenaltyAt100 = 0.25f;
    [UnityEngine.Range(0f, 1f)]
    public float overduePenaltyPerCount = 0.01f;

    public long baseCost;
    public float costMultiplier = 1.5f;
    public long maxCost;
    public bool allowDebt;

    public List<EffectData> successEffects = new();
    public float successSupplyDelta;
    public float successPriceRatio;
    public long successCashDelta;

    public List<EffectData> failureEffects = new();
    public float failureCashRate;
    public float failurePriceRate;
    public float failureSupplyDelta;
    public long failureCashDelta;
}

public sealed class EventChoiceRequest
{
    public EventSO Profile { get; }
    public DateTime Date { get; }
    public int Turn { get; }

    public EventChoiceRequest(EventSO profile, DateTime date, int turn)
    {
        Profile = profile;
        Date = date;
        Turn = turn;
    }
}

public sealed class EventChoiceResolution
{
    public bool Valid { get; set; }
    public int ChoiceIndex { get; set; }
    public string ChoiceLabel { get; set; }
    public float SuccessProbability { get; set; }
    public bool Succeeded { get; set; }
    public long Cost { get; set; }
    public long CashPaid { get; set; }
    public long DebtAdded { get; set; }
    public long FailureCashLoss { get; set; }
    public long CashDelta { get; set; }
}

public sealed class DebtPaymentRequest
{
    public int Turn { get; }
    public long Principal { get; }
    public float InterestRate { get; }
    public long Interest { get; }
    public long TotalDue { get; }
    public int PaymentCount { get; }
    public int OverdueCount { get; }

    public DebtPaymentRequest(int turn, long principal, float interestRate, long interest, long totalDue,
        int paymentCount, int overdueCount)
    {
        Turn = turn;
        Principal = principal;
        InterestRate = interestRate;
        Interest = interest;
        TotalDue = totalDue;
        PaymentCount = paymentCount;
        OverdueCount = overdueCount;
    }
}

/// <summary>
/// 정기 이자 이벤트에서 플레이어가 고른 선택의 처리 결과.
/// 일반 선택형 이벤트의 EventChoiceResolution과 달리, 납부/연체/도주에 따른
/// 부채 상태 변화와 실제 납부 금액을 함께 보관한다.
/// </summary>
public sealed class DebtPaymentResolution
{
    public int ChoiceIndex { get; set; }
    public string ChoiceLabel { get; set; }
    public float SuccessProbability { get; set; }
    public bool Succeeded { get; set; }
    public long RequestedAmount { get; set; }
    public long CashPaid { get; set; }
    public long UnpaidAmount { get; set; }
    public long CashBefore { get; set; }
    public long CashAfter { get; set; }
    public long DebtBefore { get; set; }
    public long DebtAfter { get; set; }
    public string ResultTitle { get; set; }
    public string ResultSummary { get; set; }
}
