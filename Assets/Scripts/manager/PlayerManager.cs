using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    // 직업별로 다른 시작 자금(JobSO.startingMoney)과 달리, 시작 코인 수량은 전 직업 공통이다.
    public const long StartingCoins = 10000;

    [Header("Player Stats")]
    public long currentMoney = 10000; // JobManager.SelectJob이 직업별 시작 자금으로 곧이어 덮어씀
    public long currentCoins = StartingCoins;
    // 코인 평균 매입단가. 매수 시 가중평균으로 갱신되고(매도는 불변), 보유량이 0이 되면 리셋된다.
    public float averageBuyPrice = 0f;

    // 지지도, 의심도 등은 나중에 여기에 추가

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        EventHub.OnBuyCoin += HandleBuyCoin;
        EventHub.OnSellCoin += HandleSellCoin;
    }

    private void OnDisable()
    {
        EventHub.OnBuyCoin -= HandleBuyCoin;
        EventHub.OnSellCoin -= HandleSellCoin;
    }

    // 코인 매수 요청 수신 : 현재가로 즉시 체결. 평단가는 (기존 평단가×기존 수량 + 매수가×매수량)/합산 수량으로 갱신한다.
    private void HandleBuyCoin(long amount)
    {
        float price = MarketManager.Instance.CurrentStat.CurrentPrice;
        long cost = (long)(amount * price);
        long newCoins = currentCoins + amount;

        averageBuyPrice = newCoins > 0 ? (averageBuyPrice * currentCoins + price * amount) / newCoins : 0f;

        AddMoney(-cost);
        AddCoin(amount);
    }

    // 코인 매도 요청 수신 : 현재가로 즉시 체결. CashBonus(%)만큼 수익에 배율이 붙는다.
    // 매도는 평단가를 바꾸지 않는다(잔여 수량 원가 유지) — 다 팔아 보유량이 0이 되면 리셋한다.
    private void HandleSellCoin(long amount)
    {
        long baseRevenue = (long)(amount * MarketManager.Instance.CurrentStat.CurrentPrice);
        long revenue = (long)(baseRevenue * (1f + MarketManager.Instance.CurrentStat.CashBonus / 100f));

        AddMoney(revenue);
        AddCoin(-amount);

        if (currentCoins == 0)
            averageBuyPrice = 0f;
    }

    // 돈을 벌거나 쓸 때 호출할 함수
    public void AddMoney(long amount)
    {
        currentMoney += amount;
    }

    // 잔액이 충분할 때만 차감한다 (스킬 재구매 등). 부족하면 차감 없이 false 반환.
    public bool TrySpend(long amount)
    {
        if (currentMoney < amount)
            return false;

        currentMoney -= amount;
        return true;
    }

    // 코인을 사거나 팔 때 호출할 함수. 발행량 조작(소각)은 UI에서 보유량 초과 여부를 막지 않으므로
    // 여기서 0 밑으로 내려가지 않도록 막는다 (매도는 TradeModalUI가 이미 보유량 이하로 제한한다).
    public void AddCoin(long amount)
    {
        currentCoins = System.Math.Max(0L, currentCoins + amount);
        GameStatsTracker.Instance?.NotifyCoinsChanged(currentCoins);
    }

    // 현금 + 보유 코인 평가액(현재가 기준, CashBonus% 포함)의 합. 지금 전부 매도했을 때 받을 금액과 같은
    // 공식이다(HandleSellCoin 참고 — CashBonus는 평가손익분이 아니라 코인 평가액 전체에 곱해진다).
    // 헤더의 "내 자산 합계" 표시 전용 — 엑시트 조건(MarketManager.CanExit)은 현금만 보고 별도로 판정한다.
    public long GetTotalAsset()
    {
        if (MarketManager.Instance == null)
            return currentMoney;

        long coinValue = (long)(currentCoins * MarketManager.Instance.CurrentStat.CurrentPrice
            * (1f + MarketManager.Instance.CurrentStat.CashBonus / 100f));

        return currentMoney + coinValue;
    }

    // 새 게임 시작 시 CharacterSelectUI가 호출한다 (DontDestroyOnLoad라 두 번째 플레이부터는 Awake가 다시 안 불림).
    public void ResetState()
    {
        currentMoney = 10000; // JobManager.SelectJob이 곧이어 직업별 값으로 덮어씀
        currentCoins = StartingCoins;
        averageBuyPrice = 0f;
    }
}
