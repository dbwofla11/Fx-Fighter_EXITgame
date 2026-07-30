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
    public Button btnPlusMinus;
    public Image btnPlusMinusImage;
    public Slider tradeAmountSlider;

    [Header("Preview / Confirm")]
    public TextMeshProUGUI previewText;
    public Button btnConfirm;
    public Button btnCancel;

    [Header("Stats Block (Figma 통계블록)")]
    public TextMeshProUGUI currentCashText;
    public TextMeshProUGUI currentCoinText;
    public TextMeshProUGUI afterCashText;
    public TextMeshProUGUI afterCoinText;

    private static readonly Color SubtractModeColor = new Color(0.85f, 0.35f, 0.3f);
    private static readonly Color AddModeColor = new Color(0.7f, 0.7f, 0.74f);

    private TradeMode mode;
    private long tradeAmount = 0;
    private bool isSubtractMode = false;

    private void Start()
    {
        if (btnPlus1 != null) btnPlus1.onClick.AddListener(() => AddTradeAmount(1));
        if (btnPlus10 != null) btnPlus10.onClick.AddListener(() => AddTradeAmount(10));
        if (btnPlus100 != null) btnPlus100.onClick.AddListener(() => AddTradeAmount(100));
        if (btnPlusMax != null) btnPlusMax.onClick.AddListener(SetTradeAmountToMax);
        if (btnPlusMinus != null) btnPlusMinus.onClick.AddListener(ToggleSubtractMode);
        if (tradeAmountSlider != null) tradeAmountSlider.onValueChanged.AddListener(OnSliderChanged);
        if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirmClicked);
        if (btnCancel != null) btnCancel.onClick.AddListener(Close);

        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        RefreshPreviewAndConfirmState();
    }

    public void Open(TradeMode tradeMode)
    {
        // UI 필드 중 하나가 끊겨 있어도 일시정지만은 항상 걸리도록 제일 먼저 호출한다.
        EventHub.RaiseGamePaused();

        mode = tradeMode;
        tradeAmount = 0;
        isSubtractMode = false;
        RefreshPlusMinusVisual();

        if (titleText != null)
            titleText.text = mode == TradeMode.Long ? "코인을 얼마나 매수할까요?" : "코인을 얼마나 매도할까요?";

        if (panel != null)
            panel.SetActive(true);

        RefreshSliderRange();
        RefreshTradeAmountText();
        RefreshPreviewAndConfirmState();
    }

    private void Close()
    {
        // 일시정지 해제도 다른 필드 상태와 무관하게 항상 먼저 호출한다.
        EventHub.RaiseGameResumed();

        if (panel != null)
            panel.SetActive(false);
        tradeAmount = 0;
    }

    private void AddTradeAmount(long amount)
    {
        tradeAmount = isSubtractMode ? System.Math.Max(0L, tradeAmount - amount) : tradeAmount + amount;
        RefreshTradeAmountText();
    }

    // 지금 매수/매도 가능한 최대 수치. Long은 현재 현금으로 살 수 있는 최대 수량, Short는 보유 코인 전량.
    // 슬라이더 오른쪽 끝 값과 +MAX 버튼이 이 값을 공유한다.
    private long GetMaxTradeAmount()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return 0;

        if (mode == TradeMode.Long)
        {
            float price = MarketManager.Instance.CurrentStat.CurrentPrice;
            if (price <= 0f)
                return 0;

            return (long)(PlayerManager.Instance.currentMoney / price);
        }

        return PlayerManager.Instance.currentCoins;
    }

    private void SetTradeAmountToMax()
    {
        tradeAmount = GetMaxTradeAmount();
        RefreshTradeAmountText();
    }

    // 모달을 열 때나 잔고가 바뀔 때 슬라이더의 오른쪽 끝(=최대 거래 가능 수량)을 다시 계산한다.
    private void RefreshSliderRange()
    {
        if (tradeAmountSlider == null)
            return;

        tradeAmountSlider.minValue = 0;
        tradeAmountSlider.maxValue = Mathf.Max(1, GetMaxTradeAmount());
        tradeAmountSlider.SetValueWithoutNotify(tradeAmount);
    }

    private void OnSliderChanged(float value)
    {
        tradeAmount = (long)value;
        RefreshTradeAmountText(skipSlider: true);
    }

    private void ToggleSubtractMode()
    {
        isSubtractMode = !isSubtractMode;
        RefreshPlusMinusVisual();
    }

    private void RefreshPlusMinusVisual()
    {
        if (btnPlusMinusImage != null)
            btnPlusMinusImage.color = isSubtractMode ? SubtractModeColor : AddModeColor;
    }

    // PlayerManager.HandleBuyCoin/HandleSellCoin과 동일한 공식으로 확정 전 미리보기만 계산한다.
    private void RefreshPreviewAndConfirmState()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return;

        float price = MarketManager.Instance.CurrentStat.CurrentPrice;
        long currentCash = PlayerManager.Instance.currentMoney;
        long currentCoin = PlayerManager.Instance.currentCoins;

        if (currentCashText != null) currentCashText.text = currentCash.ToString("N0") + "won";
        if (currentCoinText != null) currentCoinText.text = currentCoin.ToString("N0") + "개";

        // 시세가 바뀌면 매수 가능 최대치도 바뀌므로 슬라이더 오른쪽 끝을 매 프레임 맞춰준다.
        if (tradeAmountSlider != null)
            tradeAmountSlider.maxValue = Mathf.Max(1, GetMaxTradeAmount());

        if (mode == TradeMode.Long)
        {
            long cost = (long)(tradeAmount * price);
            bool canAfford = tradeAmount > 0 && cost <= currentCash;

            if (previewText != null)
            {
                previewText.text = "예상 지출: ₩ " + cost.ToString("N0");
                previewText.color = canAfford ? Color.white : Color.red;
            }
            if (btnConfirm != null)
                btnConfirm.interactable = canAfford;

            if (currentCashText != null) currentCashText.color = cost > currentCash ? Color.red : Color.white;
            if (currentCoinText != null) currentCoinText.color = Color.white;

            if (afterCashText != null) afterCashText.text = "→ " + (currentCash - cost).ToString("N0") + "won";
            if (afterCoinText != null) afterCoinText.text = "→ " + (currentCoin + tradeAmount).ToString("N0") + "개";
        }
        else
        {
            long baseRevenue = (long)(tradeAmount * price);
            long revenue = (long)(baseRevenue * (1f + MarketManager.Instance.CurrentStat.CashBonus / 100f));
            bool canAfford = tradeAmount > 0 && tradeAmount <= currentCoin;

            if (previewText != null)
            {
                previewText.text = "예상 수익: ₩ " + revenue.ToString("N0");
                previewText.color = canAfford ? Color.white : Color.red;
            }
            if (btnConfirm != null)
                btnConfirm.interactable = canAfford;

            if (currentCoinText != null) currentCoinText.color = tradeAmount > currentCoin ? Color.red : Color.white;
            if (currentCashText != null) currentCashText.color = Color.white;

            if (afterCashText != null) afterCashText.text = "→ " + (currentCash + revenue).ToString("N0") + "won";
            if (afterCoinText != null) afterCoinText.text = "→ " + (currentCoin - tradeAmount).ToString("N0") + "개";
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

    // skipSlider: 슬라이더 드래그가 값을 바꾼 경우, 그 값으로 다시 슬라이더를 덮어써서 튀는 것을 막는다.
    private void RefreshTradeAmountText(bool skipSlider = false)
    {
        if (tradeAmountText != null)
            tradeAmountText.text = tradeAmount.ToString("N0");

        if (!skipSlider && tradeAmountSlider != null)
            tradeAmountSlider.SetValueWithoutNotify(tradeAmount);
    }
}
