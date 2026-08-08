using UnityEngine;
using TMPro;

// 차트 상단의 코인명 + 현재가 표시. EventHub.OnMarketUpdated를 구독해 매 턴 갱신한다.
public class CoinPriceHeaderUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI totalAssetText;
    [SerializeField] private TextMeshProUGUI returnRateText;
    [SerializeField] private string coinName = "BitBitCoin(BBIT)";

    // PriceChartUI의 upColor/downColor(BtnLong/BtnShort)와 동일 팔레트.
    private static readonly Color ProfitColor = new Color(0.2941176f, 0.4117647f, 0.1843137f);
    private static readonly Color LossColor = new Color(0.6745098f, 0.1960784f, 0.1960784f);

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

        if (PlayerManager.Instance == null)
            return;

        if (totalAssetText != null)
        {
            long totalAsset = PlayerManager.Instance.currentMoney + (long)(PlayerManager.Instance.currentCoins * currentPrice);
            totalAssetText.text = "총자산 " + UIFormat.Currency(totalAsset);
        }

        if (returnRateText != null)
        {
            float avgPrice = PlayerManager.Instance.averageBuyPrice;
            if (avgPrice > 0f)
            {
                float rate = (currentPrice - avgPrice) / avgPrice * 100f;
                returnRateText.text = "수익률 " + UIFormat.SignedPercent(rate);
                returnRateText.color = rate >= 0f ? ProfitColor : LossColor;
            }
            else
            {
                returnRateText.text = "수익률 -";
                returnRateText.color = Color.white;
            }
        }
    }
}
