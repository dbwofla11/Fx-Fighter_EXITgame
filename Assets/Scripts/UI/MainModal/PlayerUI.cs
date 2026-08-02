using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용

public class PlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI coinText;

    [Header("Trade")]
    public Button btnLong;
    public Button btnShort;
    public TradeModalUI tradeModal;

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
        RefreshTexts();
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
    }

    private void Start()
    {
        btnLong.onClick.AddListener(() => tradeModal.Open(TradeMode.Long));
        btnShort.onClick.AddListener(() => tradeModal.Open(TradeMode.Short));
    }

    private void HandleMarketUpdated(PlayerStat stat) => RefreshTexts();

    private void RefreshTexts()
    {
        if (PlayerManager.Instance == null)
            return;

        moneyText.text = "보유 현금: " + UIFormat.Currency(PlayerManager.Instance.currentMoney);
        coinText.text = "보유 코인: " + PlayerManager.Instance.currentCoins.ToString("N0") + " 개";
    }
}
