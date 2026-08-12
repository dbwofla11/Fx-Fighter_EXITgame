using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    public TMP_InputField tradeAmountInput;
    public Button btnPlus1;
    public Button btnPlus10;
    public Button btnPlus100;
    public Button btnPlusMax;
    public Button btnPlusMinus;
    public Image btnPlusMinusImage;
    public Slider tradeAmountSlider;

    [Header("Preview / Confirm")]
    public TextMeshProUGUI previewText;
    public TextMeshProUGUI tradeCoinCountText; // 예상 지출(수입) 왼쪽에 거래되는 코인 수를 표시 (3차 피드백)
    public TextMeshProUGUI doubtIncreaseText;
    public TextMeshProUGUI doubtWarningText; // 상시 노출 안내문. TradeCalculator.MaxTradeAmountByDoubt가 이미 한도를 0까지 깎으므로 문구만 담당한다.
    public Button btnConfirm;
    public Button btnCancel;

    [Header("SFX")]
    [SerializeField] private AudioClip clickSfx;   // 일반버튼소리 (+1/10/100/Max, +-, 취소)
    [SerializeField] private AudioClip confirmSfx; // 확인버튼소리

    [Header("Stats Block (Figma 통계블록)")]
    public TextMeshProUGUI currentCashText;
    public TextMeshProUGUI currentCoinText;
    public TextMeshProUGUI afterCashText;
    public TextMeshProUGUI afterCoinText;

    private static readonly Color SubtractModeColor = new Color(0.85f, 0.35f, 0.3f);
    private static readonly Color AddModeColor = new Color(0.7f, 0.7f, 0.74f);
    // PriceChartUI의 upColor/downColor(BtnLong/BtnShort)와 동일 팔레트.
    private static readonly Color LongBurstColor = new Color(0.2941176f, 0.4117647f, 0.1843137f);
    private static readonly Color ShortBurstColor = new Color(0.6745098f, 0.1960784f, 0.1960784f);

    private TradeMode mode;
    private long tradeAmount = 0;
    private bool isSubtractMode = false;
    private bool isOpen = false;

    // panel이 이 스크립트 자신의 GameObject라, 씬에서 TradeModal을 비활성 상태로 두고 작업하면
    // Open()이 panel.SetActive(true)로 처음 활성화시키는 시점에 Unity가 Start()를 다음 프레임으로
    // 미룬다. 그 뒤늦은 Start()가 무조건 panel.SetActive(false)를 하면 방금 연 모달이 바로 닫혀버려서
    // 첫 클릭은 시간정지만 되고 모달이 안 열리는 버그가 생긴다. isOpen으로 그 사이에 Open()이 먼저
    // 호출됐는지 기억해서, 그런 경우엔 Start()가 다시 닫지 않게 막는다.
    private void Start()
    {
        if (btnPlus1 != null) btnPlus1.onClick.AddListener(() => { PlayClickSfx(); AddTradeAmount(1); });
        if (btnPlus10 != null) btnPlus10.onClick.AddListener(() => { PlayClickSfx(); AddTradeAmount(10); });
        if (btnPlus100 != null) btnPlus100.onClick.AddListener(() => { PlayClickSfx(); AddTradeAmount(100); });
        if (btnPlusMax != null) btnPlusMax.onClick.AddListener(() => { PlayClickSfx(); SetTradeAmountToMax(); });
        if (btnPlusMinus != null) btnPlusMinus.onClick.AddListener(() => { PlayClickSfx(); ToggleSubtractMode(); });
        if (tradeAmountSlider != null)
        {
            tradeAmountSlider.onValueChanged.AddListener(OnSliderChanged);
            AddPointerDownSfx(tradeAmountSlider.gameObject);
        }
        if (btnConfirm != null) btnConfirm.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(confirmSfx); OnConfirmClicked(); });
        if (btnCancel != null) btnCancel.onClick.AddListener(() => { PlayClickSfx(); Close(); });
        if (tradeAmountInput != null)
        {
            tradeAmountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            tradeAmountInput.onEndEdit.AddListener(OnTradeAmountInputChanged);
        }

        if (!isOpen && panel != null)
            panel.SetActive(false);

        if (doubtWarningText != null)
            doubtWarningText.text = "의심도가 MAX(99)에 가까워지면 매수/매도가 불가능해집니다.";
    }

    private void PlayClickSfx()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
    }

    // 슬라이더는 onValueChanged가 드래그 중 매 프레임 울려서 그걸로 소리를 걸면 시끄럽다 —
    // 잡는 순간(PointerDown) 한 번만 재생한다.
    private void AddPointerDownSfx(GameObject target)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(_ => PlayClickSfx());
        trigger.triggers.Add(entry);
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        RefreshPreviewAndConfirmState();
    }

    public void Open(TradeMode tradeMode)
    {
        ModalPause.Open(panel);

        isOpen = true;
        mode = tradeMode;
        tradeAmount = 0;
        isSubtractMode = false;
        RefreshPlusMinusVisual();

        if (titleText != null)
            titleText.text = mode == TradeMode.Long ? "코인을 얼마나 매수할까요?" : "코인을 얼마나 매도할까요?";

        RefreshSliderRange();
        RefreshTradeAmountText();
        RefreshPreviewAndConfirmState();
    }

    private void Close()
    {
        ModalPause.Close(panel);

        isOpen = false;
        tradeAmount = 0;
    }

    private void AddTradeAmount(long amount)
    {
        tradeAmount = isSubtractMode ? System.Math.Max(0L, tradeAmount - amount) : tradeAmount + amount;
        RefreshTradeAmountText();
    }

    // 지금 매수/매도 가능한 최대 수치. Long은 현재 현금으로 살 수 있는 최대 수량, Short는 보유 코인 전량.
    // Long에는 추가로 발행량(Supply) 기준 상한(러쉬막기, TradeCalculator.MaxTradeAmountBySupply)까지 적용한다 —
    // 이미 보유한 만큼 빼고 시장에 남은 유통량까지만 매수 가능. 마지막으로 Doubt가 100(체포 엔딩)을 넘지
    // 않는 한도까지 더해 가장 작은 쪽을 쓴다. 슬라이더 오른쪽 끝 값과 +MAX 버튼이 이 값을 공유한다.
    private long GetMaxTradeAmount()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return 0;

        long maxByBalance;

        if (mode == TradeMode.Long)
        {
            float price = MarketManager.Instance.CurrentStat.CurrentPrice;
            maxByBalance = price <= 0f ? 0 : (long)(PlayerManager.Instance.currentMoney / price);

            long maxBySupply = TradeCalculator.MaxTradeAmountBySupply(MarketManager.Instance.CurrentStat.Supply, PlayerManager.Instance.currentCoins);
            maxByBalance = System.Math.Min(maxByBalance, maxBySupply);
        }
        else
        {
            maxByBalance = PlayerManager.Instance.currentCoins;
        }

        long maxByDoubt = TradeCalculator.MaxTradeAmountByDoubt(MarketManager.Instance.CurrentStat.Doubt);
        return System.Math.Min(maxByBalance, maxByDoubt);
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

        if (doubtIncreaseText != null)
            doubtIncreaseText.text = "의심도 +" + TradeCalculator.PreviewTradeDoubtIncrease(tradeAmount).ToString("N2");

        if (tradeCoinCountText != null)
            tradeCoinCountText.text = "코인 수: " + tradeAmount.ToString("N0") + "개";

        if (mode == TradeMode.Long)
        {
            long cost = (long)(tradeAmount * price);
            bool canAfford = tradeAmount > 0 && cost <= currentCash;

            if (previewText != null)
            {
                previewText.text = "예상 지출: " + UIFormat.Currency(cost);
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
                previewText.text = "예상 수익: " + UIFormat.Currency(revenue);
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

        // Close()가 tradeAmount를 0으로 리셋하기 전에, 확정 규모 대비 비율로 파티클 크기를 먼저 계산한다.
        float intensity = Mathf.Clamp01((float)tradeAmount / Mathf.Max(1, GetMaxTradeAmount()));
        Color burstColor = mode == TradeMode.Long ? LongBurstColor : ShortBurstColor;
        RectTransform burst = UIBurstParticle.Spawn((RectTransform)btnConfirm.transform, Vector2.zero, burstColor, intensity);
        // Close()가 곧바로 panel(=이 오브젝트 자신)을 비활성화하는데, 버스트가 그 자식으로 남아있으면
        // 애니메이션이 끝나기 전에 얼어붙어 다음에 열 때 안 사라진 조각(빨간/초록 점)이 남는다 —
        // 화면 최상위(root)로 옮겨서 모달이 닫혀도 끝까지 재생되고 스스로 정리되게 한다.
        if (burst != null)
            burst.SetParent(btnConfirm.transform.root, true);

        if (mode == TradeMode.Long)
            EventHub.RaiseBuyCoin(tradeAmount);
        else
            EventHub.RaiseSellCoin(tradeAmount);

        // 거래 처리(PlayerManager/MarketManager 핸들러)는 위 Raise 호출 시 이미 동기적으로 끝나있으므로,
        // 여기서 쏘면 PlayerUI/CoinControlModalUI가 최신 잔고로 갱신된다.
        if (MarketManager.Instance != null)
            EventHub.RaiseMarketUpdated(MarketManager.Instance.CurrentStat);

        Close();
    }

    // skipSlider: 슬라이더 드래그가 값을 바꾼 경우, 그 값으로 다시 슬라이더를 덮어써서 튀는 것을 막는다.
    private void RefreshTradeAmountText(bool skipSlider = false)
    {
        if (tradeAmountInput != null)
            tradeAmountInput.SetTextWithoutNotify(tradeAmount.ToString());

        if (!skipSlider && tradeAmountSlider != null)
            tradeAmountSlider.SetValueWithoutNotify(tradeAmount);
    }

    // 직접 입력 확정(포커스 아웃/Enter) 시 기존 슬라이더/버튼과 동일한 한도로 Clamp한다.
    private void OnTradeAmountInputChanged(string text)
    {
        if (!long.TryParse(text, out long value))
            value = 0;

        tradeAmount = System.Math.Min(GetMaxTradeAmount(), System.Math.Max(0L, value));
        RefreshTradeAmountText();
    }
}
