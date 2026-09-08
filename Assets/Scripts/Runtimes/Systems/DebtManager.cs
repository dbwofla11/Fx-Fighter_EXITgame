using UnityEngine;

/// <summary>
/// 단일 대출의 원금과 이자 납부 상태를 관리한다.
/// 기획상 30턴마다 이자만 납부하며, 원금은 선택지의 부족금이 추가될 때 늘어난다.
/// </summary>
public sealed class DebtManager
{
    public const int PaymentIntervalTurns = 30;
    public const float BaseInterestRate = 0.03f;
    public const float InstallmentIncrease = 0.005f;
    public const float MaxInterestRate = 0.08f;
    public const float OverdueSurcharge = 0.01f;

    public long Principal { get; private set; }
    public int PaymentCount { get; private set; }
    public int OverdueCount { get; private set; }
    public long UnpaidInterest { get; private set; }
    public long CurrentInterest { get; private set; }
    public long CurrentDueInterest { get; private set; }

    public bool HasDebt => Principal > 0;
    public long TotalDebt => Principal + UnpaidInterest;

    public void Reset()
    {
        Principal = 0;
        PaymentCount = 0;
        OverdueCount = 0;
        UnpaidInterest = 0;
        CurrentInterest = 0;
        CurrentDueInterest = 0;
    }

    public void AddPrincipal(long amount)
    {
        if (amount > 0)
            Principal += amount;
    }

    public bool IsPaymentDue(int turn)
    {
        return HasDebt && turn > 0 && turn % PaymentIntervalTurns == 0 && CurrentDueInterest == 0;
    }

    public DebtPaymentRequest CreatePaymentRequest(int turn)
    {
        float rate = Mathf.Min(MaxInterestRate,
            BaseInterestRate + PaymentCount * InstallmentIncrease + OverdueCount * OverdueSurcharge);
        CurrentInterest = (long)System.Math.Ceiling(Principal * (double)rate);
        CurrentDueInterest = CurrentInterest + UnpaidInterest;

        return new DebtPaymentRequest(turn, Principal, rate, CurrentInterest, CurrentDueInterest,
            PaymentCount, OverdueCount);
    }

    public void Pay(PlayerManager player)
    {
        if (player == null)
            return;

        player.AddMoney(-CurrentDueInterest);
        CurrentInterest = 0;
        CurrentDueInterest = 0;
        UnpaidInterest = 0;
        PaymentCount++;
    }

    public void Defer()
    {
        UnpaidInterest += CurrentInterest;
        CurrentInterest = 0;
        CurrentDueInterest = 0;
        OverdueCount++;
    }
}
