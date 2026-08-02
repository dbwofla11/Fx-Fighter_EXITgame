using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 발행량 조작 모달. TotalSupplyText/HeldCoinText는 기본 화면에서도 항상 보이며 EventHub.OnMarketUpdated로
// 갱신된다. panel(딤 오버레이+팝업 박스)만 "코인 발행" 트리거 버튼(MintButtonUI)이 열고 닫는다.
// 확인 버튼을 눌러야만 EventHub.RaiseManipulateSupply가 호출된다.
public class CoinControlModalUI : MonoBehaviour
{
    [Header("항상 표시")]
    public TextMeshProUGUI totalSupplyText;
    public TextMeshProUGUI heldCoinText;

    [Header("모달 (평소 숨김)")]
    public GameObject panel;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI previewText;
    public Button btnPlusMinus;
    public Button btnAmount1;
    public Button btnAmount10;
    public Button btnAmount100;
    public Button btnAmountMax;
    public Button btnConfirm;
    public Button btnCancel;

    private const long MaxAdjustAmount = 20000;

    private long amount = 0;
    private bool isIncrease = true;

    private void OnEnable()
    {
        EventHub.OnMarketUpdated += HandleMarketUpdated;
    }

    private void OnDisable()
    {
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
    }

    private void Start()
    {
        btnPlusMinus.onClick.AddListener(ToggleDirection);
        btnAmount1.onClick.AddListener(() => AddAmount(1));
        btnAmount10.onClick.AddListener(() => AddAmount(10));
        btnAmount100.onClick.AddListener(() => AddAmount(100));
        btnAmountMax.onClick.AddListener(() => SetAmount(MaxAdjustAmount));
        btnConfirm.onClick.AddListener(OnConfirmClicked);
        btnCancel.onClick.AddListener(Close);

        panel.SetActive(false);
        RefreshAmountText();

        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void Update()
    {
        if (panel.activeSelf)
            RefreshPreviewText();
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        totalSupplyText.text = "현재 발행량 : " + stat.Supply.ToString("N0") + "개";

        if (PlayerManager.Instance != null)
            heldCoinText.text = "보유 코인수량 : " + PlayerManager.Instance.currentCoins.ToString("N0") + "개";
    }

    public void Open()
    {
        // TradeModalUI/EventLogPanelUI와 동일하게, 모달이 떠 있는 동안은 게임 시간을 멈춘다.
        EventHub.RaiseGamePaused();

        amount = 0;
        isIncrease = true;
        panel.SetActive(true);
        RefreshAmountText();
    }

    private void Close()
    {
        EventHub.RaiseGameResumed();

        panel.SetActive(false);
        amount = 0;
        RefreshAmountText();
    }

    private void ToggleDirection()
    {
        isIncrease = !isIncrease;
        RefreshAmountText();
    }

    private void AddAmount(long delta)
    {
        amount += delta;
        RefreshAmountText();
    }

    private void SetAmount(long value)
    {
        amount = value;
        RefreshAmountText();
    }

    private void RefreshAmountText()
    {
        string sign = isIncrease ? "+" : "-";
        amountText.text = sign + amount.ToString("N0");
    }

    // 확정 전 "조정 후 예상 발행량" 미리보기만 계산한다.
    private void RefreshPreviewText()
    {
        if (MarketManager.Instance == null)
            return;

        long signedAmount = isIncrease ? amount : -amount;
        float resultSupply = MarketManager.Instance.CurrentStat.Supply + signedAmount;
        previewText.text = "조정 후 예상 발행량: " + resultSupply.ToString("N0") + "개";
    }

    private void OnConfirmClicked()
    {
        if (amount <= 0)
            return;

        long signedAmount = isIncrease ? amount : -amount;
        EventHub.RaiseManipulateSupply(signedAmount);

        if (MarketManager.Instance != null)
            EventHub.RaiseMarketUpdated(MarketManager.Instance.CurrentStat);

        Close();
    }
}
