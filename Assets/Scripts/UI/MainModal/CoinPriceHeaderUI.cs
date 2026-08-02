using UnityEngine;
using TMPro;

// 차트 상단의 코인명 + 현재가 표시. EventHub.OnMarketUpdated를 구독해 매 턴 갱신한다.
public class CoinPriceHeaderUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI priceText;
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

        priceText.text = $"{coinName}  {UIFormat.CurrencyTight(MarketManager.Instance.CurrentStat.CurrentPrice)}";
    }
}
