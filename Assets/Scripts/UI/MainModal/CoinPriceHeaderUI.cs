using UnityEngine;
using TMPro;

// 차트 상단의 코인명 + 현재가 표시. EventHub.OnMarketUpdated를 구독해 매 턴 갱신한다.
public class CoinPriceHeaderUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI myAssetTotalText;
    [SerializeField] private string coinName = "BitBitCoin(BBIT)";

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        Refresh();
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
    }

    private void HandleMarketUpdated(PlayerStat stat) => Refresh();

    private void Refresh()
    {
        if (MarketManager.Instance == null || priceText == null)
            return;

        float currentPrice = MarketManager.Instance.CurrentStat.CurrentPrice;
        priceText.text = $"{coinName}  {UIFormat.CurrencyTight(currentPrice)}";

        if (PlayerManager.Instance == null || myAssetTotalText == null)
            return;

        long coinValue = (long)(PlayerManager.Instance.currentCoins * currentPrice);

        // 보유 코인 평가손익에 CashBonus(%, 거래수익+X% 직업 스탯)를 반영해 총자산에 얹는다 — 매도 전에도 보너스가 보이도록.
        float avgPrice = PlayerManager.Instance.averageBuyPrice;
        long bonusProfit = 0;
        if (avgPrice > 0f)
        {
            float profit = (currentPrice - avgPrice) * PlayerManager.Instance.currentCoins;
            bonusProfit = (long)(profit * MarketManager.Instance.CurrentStat.CashBonus / 100f);
        }

        long totalAsset = PlayerManager.Instance.currentMoney + coinValue + bonusProfit;
        myAssetTotalText.text = "내 자산 합계 " + UIFormat.Currency(totalAsset);
    }
}
