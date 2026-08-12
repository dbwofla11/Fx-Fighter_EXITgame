using UnityEngine;
using TMPro;

// 차트 상단의 코인명 + 현재가 표시. EventHub.OnMarketUpdated를 구독해 매 턴 갱신한다.
public class CoinPriceHeaderUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI myAssetTotalText;
    [SerializeField] private string coinName = "BitBitCoin(BBIT)";
    [SerializeField] private Color achievedColor = Color.yellow; // 목표금액 달성 시 강조색 (목업 기준으로 조정)

    private Color defaultColor;

    private void Awake()
    {
        if (myAssetTotalText != null) defaultColor = myAssetTotalText.color;
    }

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

        long totalAsset = PlayerManager.Instance.GetTotalAsset();
        myAssetTotalText.text = "내 자산 합계 " + UIFormat.Currency(totalAsset);
        myAssetTotalText.color = MarketManager.Instance.CanExit ? achievedColor : defaultColor;
    }
}
