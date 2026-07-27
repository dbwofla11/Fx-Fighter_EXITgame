using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TradeMode
{
    Long,
    Short
}

// 매수/매도 공용 모달. 확인 버튼을 눌러야만 EventHub.RaiseBuyCoin/RaiseSellCoin이 호출된다.
public class TradeModalUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;
    public TextMeshProUGUI titleText;

    [Header("Amount")]
    public TextMeshProUGUI tradeAmountText;
    public Button btnPlus1;
    public Button btnPlus10;
    public Button btnPlus100;
    public Button btnPlusMax;

    [Header("Preview / Confirm")]
    public TextMeshProUGUI previewText;
    public Button btnConfirm;
    public Button btnCancel;

    private TradeMode mode;
    private long tradeAmount = 0;

    private void Start()
    {
        btnPlus1.onClick.AddListener(() => AddTradeAmount(1));
        btnPlus10.onClick.AddListener(() => AddTradeAmount(10));
        btnPlus100.onClick.AddListener(() => AddTradeAmount(100));
        btnPlusMax.onClick.AddListener(SetTradeAmountToMax);
        btnConfirm.onClick.AddListener(OnConfirmClicked);
        btnCancel.onClick.AddListener(Close);

        panel.SetActive(false);
    }

    private void Update()
    {
        if (!panel.activeSelf)
            return;

        RefreshPreviewAndConfirmState();
    }

    public void Open(TradeMode tradeMode)
    {
        mode = tradeMode;
        tradeAmount = 0;

        titleText.text = mode == TradeMode.Long ? "매수" : "매도";
        panel.SetActive(true);
        RefreshTradeAmountText();
    }

    private void Close()
    {
        panel.SetActive(false);
        tradeAmount = 0;
    }

    private void AddTradeAmount(long amount)
    {
        tradeAmount += amount;
        RefreshTradeAmountText();
    }

    // Long은 현재 현금으로 살 수 있는 최대 수량, Short는 보유 코인 전량.
    private void SetTradeAmountToMax()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return;

        if (mode == TradeMode.Long)
        {
            float price = MarketManager.Instance.CurrentStat.CurrentPrice;
            if (price <= 0f)
                return;

            tradeAmount = (long)(PlayerManager.Instance.currentMoney / price);
        }
        else
        {
            tradeAmount = PlayerManager.Instance.currentCoins;
        }

        RefreshTradeAmountText();
    }

    // PlayerManager.HandleBuyCoin/HandleSellCoin과 동일한 공식으로 확정 전 미리보기만 계산한다.
    private void RefreshPreviewAndConfirmState()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return;

        float price = MarketManager.Instance.CurrentStat.CurrentPrice;

        if (mode == TradeMode.Long)
        {
            long cost = (long)(tradeAmount * price);
            previewText.text = "예상 지출: ₩ " + cost.ToString("N0");
            btnConfirm.interactable = tradeAmount > 0 && cost <= PlayerManager.Instance.currentMoney;
        }
        else
        {
            long baseRevenue = (long)(tradeAmount * price);
            long revenue = (long)(baseRevenue * (1f + MarketManager.Instance.CurrentStat.CashBonus / 100f));
            previewText.text = "예상 수익: ₩ " + revenue.ToString("N0");
            btnConfirm.interactable = tradeAmount > 0 && tradeAmount <= PlayerManager.Instance.currentCoins;
        }
    }

    private void OnConfirmClicked()
    {
        if (tradeAmount <= 0)
            return;

        if (mode == TradeMode.Long)
            EventHub.RaiseBuyCoin(tradeAmount);
        else
            EventHub.RaiseSellCoin(tradeAmount);

        Close();
    }

    private void RefreshTradeAmountText()
    {
        tradeAmountText.text = tradeAmount.ToString("N0");
    }
}
