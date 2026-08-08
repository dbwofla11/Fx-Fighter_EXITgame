using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 발행량 조작 모달. TotalSupplyText는 기본 화면에서도 항상 보이며 EventHub.OnMarketUpdated로 갱신된다.
// 보유 코인수량은 PlayerUI.coinText가 이미 항상 표시하고 있어 여기서는 중복 표시하지 않는다.
// panel(딤 오버레이+팝업 박스)만 "코인 발행" 트리거 버튼(MintButtonUI)이 열고 닫는다.
// 확인 버튼을 눌러야만 EventHub.RaiseManipulateSupply가 호출된다.
public class CoinControlModalUI : MonoBehaviour
{
    [Header("항상 표시")]
    public TextMeshProUGUI totalSupplyText;

    [Header("모달 (평소 숨김)")]
    public GameObject panel;
    public TMP_InputField amountInput;
    public Button btnPlusMinus;
    public Image btnPlusMinusImage;
    public Slider amountSlider;
    public Button btnAmount1;
    public Button btnAmount10;
    public Button btnAmount100;
    public Button btnAmountMax;

    [Header("Preview / Confirm")]
    public TextMeshProUGUI previewText;
    public TextMeshProUGUI doubtIncreaseText;
    public TextMeshProUGUI doubtWarningText; // 상시 노출 안내문. TradeCalculator.MaxSupplyAmountByDoubt가 이미 한도를 0까지 깎으므로 문구만 담당한다.
    public Button btnConfirm;
    public Button btnCancel;

    [Header("SFX")]
    [SerializeField] private AudioClip clickSfx;   // 일반버튼소리 (+1/10/100/Max, +-, 취소)
    [SerializeField] private AudioClip confirmSfx; // 확인버튼소리

    [Header("Stats Block (발행 전/후)")]
    public TextMeshProUGUI currentSupplyText;
    public TextMeshProUGUI afterSupplyText;

    // TradeModalUI(AddModeColor)와 동일 팔레트. 발행량은 증가만 허용하므로(감소/소각 없음) 항상 이 색 고정.
    private static readonly Color AddModeColor = new Color(0.7f, 0.7f, 0.74f);

    // Max 상한은 잔고가 아닌 정책값. 슬라이더 오른쪽 끝과 +MAX 버튼이 이 값을 공유한다.
    private const long MaxAdjustAmount = 100000;

    private long amount = 0;

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
        if (btnPlusMinus != null) btnPlusMinus.interactable = false;
        if (btnAmount1 != null) btnAmount1.onClick.AddListener(() => { PlayClickSfx(); AddAmount(1); });
        if (btnAmount10 != null) btnAmount10.onClick.AddListener(() => { PlayClickSfx(); AddAmount(10); });
        if (btnAmount100 != null) btnAmount100.onClick.AddListener(() => { PlayClickSfx(); AddAmount(100); });
        if (btnAmountMax != null) btnAmountMax.onClick.AddListener(() => { PlayClickSfx(); SetAmount(GetMaxAdjustAmount()); });
        if (amountSlider != null)
        {
            amountSlider.onValueChanged.AddListener(OnSliderChanged);
            AddPointerDownSfx(amountSlider.gameObject);
        }
        if (btnConfirm != null) btnConfirm.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(confirmSfx); OnConfirmClicked(); });
        if (btnCancel != null) btnCancel.onClick.AddListener(() => { PlayClickSfx(); Close(); });
        if (amountInput != null)
        {
            amountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            amountInput.onEndEdit.AddListener(OnAmountInputChanged);
        }

        panel.SetActive(false);
        RefreshSliderRange();
        RefreshAmountText();
        if (btnPlusMinusImage != null) btnPlusMinusImage.color = AddModeColor;
        if (doubtWarningText != null) doubtWarningText.text = "의심도가 MAX(99)에 가까워지면 발행이 불가능해집니다.";

        if (MarketManager.Instance != null)
            HandleMarketUpdated(MarketManager.Instance.CurrentStat);
    }

    private void Update()
    {
        if (panel.activeSelf)
            RefreshPreviewAndConfirmState();
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

    private void HandleMarketUpdated(PlayerStat stat)
    {
        totalSupplyText.text = "현재 발행량 : " + stat.Supply.ToString("N0") + "개";
    }

    public void Open()
    {
        ModalPause.Open(panel);

        amount = 0;
        RefreshSliderRange();
        RefreshAmountText();
        RefreshPreviewAndConfirmState();
    }

    private void Close()
    {
        ModalPause.Close(panel);

        amount = 0;
        RefreshAmountText();
    }

    private void AddAmount(long delta)
    {
        amount = System.Math.Min(GetMaxAdjustAmount(), amount + delta);
        RefreshAmountText();
    }

    private void SetAmount(long value)
    {
        amount = value;
        RefreshAmountText();
    }

    // 지금 조작 가능한 최대 수치. 정책 상한(MaxAdjustAmount)과 Doubt가 99를 넘지 않는 한도 중 더 작은 쪽 —
    // TradeModalUI.GetMaxTradeAmount와 동일 설계. 슬라이더 오른쪽 끝과 +MAX 버튼이 이 값을 공유한다.
    private long GetMaxAdjustAmount()
    {
        if (MarketManager.Instance == null)
            return MaxAdjustAmount;

        long maxByDoubt = TradeCalculator.MaxSupplyAmountByDoubt(MarketManager.Instance.CurrentStat.Doubt);
        return System.Math.Min(MaxAdjustAmount, maxByDoubt);
    }

    // 모달을 열 때 슬라이더의 오른쪽 끝을 맞춘다.
    private void RefreshSliderRange()
    {
        if (amountSlider == null)
            return;

        amountSlider.minValue = 0;
        amountSlider.maxValue = Mathf.Max(1, GetMaxAdjustAmount());
        amountSlider.SetValueWithoutNotify(amount);
    }

    private void OnSliderChanged(float value)
    {
        amount = (long)value;
        RefreshAmountText(skipSlider: true);
    }

    // skipSlider: 슬라이더 드래그가 값을 바꾼 경우, 그 값으로 다시 슬라이더를 덮어써서 튀는 것을 막는다.
    private void RefreshAmountText(bool skipSlider = false)
    {
        if (amountInput != null)
            amountInput.SetTextWithoutNotify(amount.ToString());

        if (!skipSlider && amountSlider != null)
            amountSlider.SetValueWithoutNotify(amount);
    }

    // 직접 입력 확정(포커스 아웃/Enter) 시 기존 슬라이더/버튼과 동일한 한도로 Clamp한다.
    private void OnAmountInputChanged(string text)
    {
        if (!long.TryParse(text, out long value))
            value = 0;

        amount = System.Math.Min(GetMaxAdjustAmount(), System.Math.Max(0L, value));
        RefreshAmountText();
    }

    // 확정 전 "조정 후 예상 발행량" 미리보기 + Before/After 발행량 텍스트를 계산한다.
    // 발행량은 증가만 허용하므로(감소/소각 없음) 유효성 조건은 수량이 0보다 큰지만 본다.
    private void RefreshPreviewAndConfirmState()
    {
        if (MarketManager.Instance == null)
            return;

        float currentSupply = MarketManager.Instance.CurrentStat.Supply;
        float resultSupply = currentSupply + amount;
        bool isValid = amount > 0;

        // Doubt가 바뀌면 조작 가능 최대치도 바뀌므로 슬라이더 오른쪽 끝을 매 프레임 맞춰준다 (Trade와 동일).
        if (amountSlider != null)
            amountSlider.maxValue = Mathf.Max(1, GetMaxAdjustAmount());

        if (doubtIncreaseText != null)
            doubtIncreaseText.text = "의심도 +" + TradeCalculator.PreviewSupplyDoubtIncrease(amount).ToString("N2");

        if (currentSupplyText != null)
        {
            currentSupplyText.text = currentSupply.ToString("N0") + "개";
            currentSupplyText.color = isValid ? Color.white : Color.red;
        }
        if (afterSupplyText != null)
            afterSupplyText.text = "→ " + resultSupply.ToString("N0") + "개";

        if (previewText != null)
        {
            previewText.text = "조정 후 예상 발행량: " + resultSupply.ToString("N0") + "개";
            previewText.color = isValid ? Color.white : Color.red;
        }
        if (btnConfirm != null)
            btnConfirm.interactable = isValid;
    }

    private void OnConfirmClicked()
    {
        if (amount <= 0)
            return;

        float intensity = Mathf.Clamp01((float)amount / Mathf.Max(1, GetMaxAdjustAmount()));
        RectTransform burst = UIBurstParticle.Spawn((RectTransform)btnConfirm.transform, Vector2.zero, AddModeColor, intensity);
        // Close()가 곧바로 panel을 비활성화하는데, 버스트가 그 자식으로 남아있으면 애니메이션이 끝나기
        // 전에 얼어붙어 다음에 열 때 안 사라진 조각이 남는다 — TradeModalUI와 동일하게 root로 옮긴다.
        if (burst != null)
            burst.SetParent(btnConfirm.transform.root, true);

        EventHub.RaiseManipulateSupply(amount);

        if (MarketManager.Instance != null)
            EventHub.RaiseMarketUpdated(MarketManager.Instance.CurrentStat);

        Close();
    }
}
