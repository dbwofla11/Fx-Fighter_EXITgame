using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용

public class PlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI coinText;

    [Header("Trade")]
    public TextMeshProUGUI tradeAmountText;
    public Button btnPlus1;
    public Button btnPlus10;
    public Button btnPlus100;
    public Button btnPlusMax;
    public Button btnLong;
    public Button btnShort;

    private long tradeAmount = 0;

    private void Start()
    {
        btnPlus1.onClick.AddListener(() => AddTradeAmount(1));
        btnPlus10.onClick.AddListener(() => AddTradeAmount(10));
        btnPlus100.onClick.AddListener(() => AddTradeAmount(100));
        btnPlusMax.onClick.AddListener(SetTradeAmountToMax);
        btnLong.onClick.AddListener(OnLongClicked);
        btnShort.onClick.AddListener(OnShortClicked);

        RefreshTradeAmountText();
    }

    private void Update()
    {
        // PlayerManager가 존재할 때만 화면에 텍스트 업데이트
        if (PlayerManager.Instance != null)
        {
            // "N0"은 천 단위마다 콤마(,)를 찍어주는 포맷입니다.
            moneyText.text = "보유 현금: ₩ " + PlayerManager.Instance.currentMoney.ToString("N0");
            coinText.text = "보유 코인: " + PlayerManager.Instance.currentCoins.ToString("N0") + " 개";
        }

        RefreshTradeButtons();
    }

    private void AddTradeAmount(long amount)
    {
        tradeAmount += amount;
        RefreshTradeAmountText();
    }

    // 현재 현금으로 살 수 있는 최대 수량 (Long 기준).
    private void SetTradeAmountToMax()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return;

        float price = MarketManager.Instance.CurrentStat.CurrentPrice;
        if (price <= 0f)
            return;

        tradeAmount = (long)(PlayerManager.Instance.currentMoney / price);
        RefreshTradeAmountText();
    }

    private void OnLongClicked()
    {
        if (tradeAmount <= 0)
            return;

        EventHub.RaiseBuyCoin(tradeAmount);
        ResetTradeAmount();
    }

    private void OnShortClicked()
    {
        if (tradeAmount <= 0)
            return;

        EventHub.RaiseSellCoin(tradeAmount);
        ResetTradeAmount();
    }

    private void ResetTradeAmount()
    {
        tradeAmount = 0;
        RefreshTradeAmountText();
    }

    private void RefreshTradeAmountText()
    {
        tradeAmountText.text = tradeAmount.ToString("N0");
    }

    // Long은 결제 가능한 금액인지, Short는 보유 코인만큼인지에 따라 버튼을 비활성화한다
    // (PlayerManager.HandleBuyCoin/HandleSellCoin은 잔액/보유량 검증 없이 그대로 반영하므로, 마이너스로
    // 빠지지 않도록 UI 쪽에서 막아준다).
    private void RefreshTradeButtons()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return;

        float price = MarketManager.Instance.CurrentStat.CurrentPrice;
        long cost = (long)(tradeAmount * price);

        btnLong.interactable = tradeAmount > 0 && cost <= PlayerManager.Instance.currentMoney;
        btnShort.interactable = tradeAmount > 0 && tradeAmount <= PlayerManager.Instance.currentCoins;
    }
}