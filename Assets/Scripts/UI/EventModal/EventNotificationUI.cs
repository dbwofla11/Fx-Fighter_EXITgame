using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 시사 이벤트가 발생했을 때 메인 게임 화면 위에 뜨는 알림 패널. Figma 목업 없이 사용자가 준 스크린샷 2장
// (긍정/부정) 기준으로 만들었다 — 배경색만 다르고 구조(제목/날짜/효과 목록/닫기 버튼)는 EventCardView와 동일해
// 그대로 재사용한다. 이 오브젝트(EventNotification)는 항상 활성 상태를 유지하며 EventHub.OnEventTriggered를
// 상시 구독하고, 실제로 보이는 패널(panel)만 켜고 끈다 — StatGaugeUI 등 다른 상시 구독 UI와 동일한 관례.
public class EventNotificationUI : MonoBehaviour
{
    public GameObject panel;
    public EventCardView cardView;
    public Button closeButton;
    [SerializeField] private AudioClip openSfx; // 이벤트알림뜨는소리
    [SerializeField] private AudioClip closeSfx; // 이벤트알림끄는소리
    private readonly Queue<System.Action> pendingEvents = new();
    private bool showingEvent;

    private readonly List<GameObject> runtimeButtons = new();
    private GameObject runtimeButtonContainer;
    private RectTransform panelRect;
    private Vector2 originalPanelSize;
    private Vector2 originalPanelPosition;
    private RectLayout originalTitleLayout;
    private RectLayout originalDateLayout;
    private RectLayout originalEffectsLayout;
    private TextAlignmentOptions originalEffectsAlignment;
    private float originalTitleFontSize;
    private float originalDateFontSize;
    private float originalEffectsFontSize;
    private float originalTitleFontSizeMin;
    private float originalTitleFontSizeMax;
    private bool originalTitleWordWrapping;
    private bool originalTitleAutoSizing;
    private TextOverflowModes originalTitleOverflowMode;
    private bool cardLayoutCached;
    private bool awaitingChoice;
    private bool awaitingDebtPayment;

    // Figma event panels are 1100x700 (choice) and 1100x500 (result).
    // The scene panel has its own width, so only ratios are kept here.
    private const float ChoicePanelAspect = 1100f / 700f;
    private const float ResultPanelAspect = 1100f / 500f;
    // Reference choice frame: #FFA6AA. The result frame keeps its event-category color.
    private static readonly Color ChoicePanelColor = new Color(1f, 0.651f, 0.667f, 1f);
    private static readonly Color ChoiceCardColor = Color.white;
    private static readonly Color ChoiceCardHoverColor = Color.white;
    private static readonly Color ChoiceCardPressedColor = new Color(1f, 0.94f, 0.95f, 1f);
    private static readonly Color ChoiceTextColor = Color.black;
    private static readonly Color SuccessColor = new Color(0f, 0.95f, 0.04f, 1f);
    private static readonly Color FailureColor = new Color(1f, 0.16f, 0.18f, 1f);
    private static readonly Color HoverOutlineColor = new Color(0.93f, 0.29f, 0.40f, 1f);

    private void OnEnable()
    {
        EventHub.OnEventTriggered += HandleEventTriggered;
        EventHub.OnEventChoiceRequired += HandleEventChoiceRequired;
        EventHub.OnDebtPaymentRequired += HandleDebtPaymentRequired;
        EventHub.OnMarketUpdated += HandleMarketUpdated;
    }

    private void OnDisable()
    {
        EventHub.OnEventTriggered -= HandleEventTriggered;
        EventHub.OnEventChoiceRequired -= HandleEventChoiceRequired;
        EventHub.OnDebtPaymentRequired -= HandleDebtPaymentRequired;
        EventHub.OnMarketUpdated -= HandleMarketUpdated;
        pendingEvents.Clear();
        showingEvent = false;
        awaitingChoice = false;
        awaitingDebtPayment = false;
    }

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        panelRect = panel != null ? panel.GetComponent<RectTransform>() : null;
        if (panelRect != null)
        {
            originalPanelSize = panelRect.sizeDelta;
            originalPanelPosition = panelRect.anchoredPosition;
        }
        CacheCardLayout();
        if (panel != null) panel.SetActive(false);
    }

    private void HandleEventTriggered(EventLogEntry entry)
    {
        if (entry == null) return;
        // A resolved choice/debt continues the current dialog; other notifications wait.
        if ((awaitingChoice && entry.HasChoiceResult)
            || (awaitingDebtPayment && !MarketManager.Instance.IsAwaitingDebtPayment))
        {
            ShowEvent(entry);
            return;
        }
        pendingEvents.Enqueue(() => ShowEvent(entry));
        if (!showingEvent) ShowNextEvent();
    }

    private void ShowNextEvent()
    {
        showingEvent = true;
        pendingEvents.Dequeue().Invoke();
    }

    private void ShowEvent(EventLogEntry entry)
    {
        bool isChoiceResult = entry != null && entry.HasChoiceResult;
        awaitingChoice = false;
        awaitingDebtPayment = false;
        ClearRuntimeButtons();
        if (isChoiceResult)
        {
            ResizePanelForResult();
            RestoreCardLayout();
            ApplyResultCardLayout();
        }
        else
        {
            RestorePanelSize();
        }
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (cardView != null) cardView.Populate(entry, isChoiceResult);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(openSfx);
        ModalPause.Open(panel);
    }

    private void HandleEventChoiceRequired(EventChoiceRequest request)
    {
        if (request == null || request.Profile == null)
            return;

        if (awaitingDebtPayment && !MarketManager.Instance.IsAwaitingDebtPayment)
        {
            ShowEventChoice(request);
            return;
        }
        pendingEvents.Enqueue(() => ShowEventChoice(request));
        if (!showingEvent) ShowNextEvent();
    }

    private void ShowEventChoice(EventChoiceRequest request)
    {
        awaitingChoice = true;
        awaitingDebtPayment = false;
        ClearRuntimeButtons();
        ResizePanelForChoiceCards();
        RestoreCardLayout();
        ApplyChoiceHeaderStyle();
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        if (cardView != null)
            cardView.Populate(ChoicePanelColor, request.Profile.message,
                UIFormat.DateDot(request.Date), string.Empty);

        CreateChoiceButtons(request);
        PlayOpenSfx();
        ModalPause.Open(panel);
    }

    private void HandleDebtPaymentRequired(DebtPaymentRequest request)
    {
        if (request == null)
            return;

        pendingEvents.Enqueue(() => ShowDebtPayment(request));
        if (!showingEvent) ShowNextEvent();
    }

    private void ShowDebtPayment(DebtPaymentRequest request)
    {
        awaitingChoice = false;
        awaitingDebtPayment = true;
        ClearRuntimeButtons();
        ResizePanelForChoiceCards();
        RestoreCardLayout();
        ApplyChoiceHeaderStyle();
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        if (cardView != null)
            cardView.Populate(ChoicePanelColor, "정기 이자를 내야할 시간입니다...",
                UIFormat.DateDot(TimeManager.Instance.CurrentGameDate), string.Empty);

        CreateDebtPaymentCards(request);
        PlayOpenSfx();
        ModalPause.Open(panel);
    }

    private void CreateDebtPaymentCards(DebtPaymentRequest request)
    {
        CreateChoiceContainer();

        // Reference copy uses a fixed 100%/100%/20% presentation. The payment
        // card remains selectable with insufficient cash because DebtManager
        // safely performs a partial payment and carries the remainder forward.
        CreateChoiceCard("정직하게 낸다", 1f, request.TotalDue,
            "정기 이자를 갚는다.", string.Empty,
            () => OnDebtPaymentChoiceClicked(DebtManager.PayChoiceIndex), true);

        CreateChoiceCard("나중에 낸다", 1f, 0,
            "나중에 갚는다.\n대신 다음 이자비용이 증가한다.", string.Empty,
            () => OnDebtPaymentChoiceClicked(DebtManager.DeferChoiceIndex), true);

        CreateChoiceCard("도망간다", DebtManager.EscapeSuccessProbability, 0,
            "도망을 성공적으로 함", "현금 20% 차감\nDoubt +15",
            () => OnDebtPaymentChoiceClicked(DebtManager.EscapeChoiceIndex), true);
    }

    private void CreateChoiceButtons(EventChoiceRequest request)
    {
        CreateChoiceContainer();

        for (int i = 0; i < request.Profile.choices.Count; i++)
        {
            int choiceIndex = i;
            EventChoice choice = request.Profile.choices[i];
            long cost = EventCalculator.CalculateChoiceCost(choice,
                MarketManager.Instance.GetChoiceSelectionCount(request.Profile, choiceIndex));
            float probability = EventCalculator.CalculateChoiceSuccessProbability(
                MarketManager.Instance.CurrentStat, choice, MarketManager.Instance.Debt.OverdueCount);
            CreateChoiceCard(choice, probability, cost,
                () => OnChoiceClicked(choiceIndex),
                choice.allowDebt || System.Math.Max(0L, PlayerManager.Instance.currentMoney) >= cost);
        }
    }

    private void CreateChoiceContainer()
    {
        runtimeButtonContainer = new GameObject("RuntimeEventChoiceCards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        runtimeButtonContainer.transform.SetParent(panel.transform, false);
        RectTransform containerRect = runtimeButtonContainer.GetComponent<RectTransform>();
        // 30px side margins, 25px gaps and 330px cards on the 1100px reference frame.
        containerRect.anchorMin = new Vector2(30f / 1100f, 35f / 700f);
        // The choice cards occupy the lower 69% of the 1100x700 reference panel.
        containerRect.anchorMax = new Vector2(1070f / 1100f, 0.737f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        HorizontalLayoutGroup layout = runtimeButtonContainer.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 25f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private void CreateChoiceCard(EventChoice choice, float probability, long cost,
        UnityEngine.Events.UnityAction action, bool interactable)
    {
        CreateChoiceCard(choice != null ? choice.label : string.Empty,
            probability,
            cost,
            EventEffectFormatter.BuildChoiceEffectsText(choice, true),
            EventEffectFormatter.BuildChoiceEffectsText(choice, false),
            action,
            interactable);
    }

    private void CreateChoiceCard(string label, float probability, long cost,
        string successText, string failureText,
        UnityEngine.Events.UnityAction action, bool interactable)
    {
        GameObject cardObject = new GameObject("RuntimeEventChoiceCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardObject.transform.SetParent(runtimeButtonContainer.transform, false);

        Image image = cardObject.GetComponent<Image>();
        image.color = ChoiceCardColor;
        Nobi.UiRoundedCorners.ImageWithRoundedCorners roundedCorners =
            cardObject.AddComponent<Nobi.UiRoundedCorners.ImageWithRoundedCorners>();
        roundedCorners.radius = 15f;
        roundedCorners.Refresh();

        Outline hoverOutline = cardObject.AddComponent<Outline>();
        hoverOutline.effectColor = Color.clear;
        hoverOutline.effectDistance = new Vector2(3f, -3f);
        Button button = cardObject.GetComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(action);
        AddHoverOutline(cardObject, hoverOutline, button);
        ColorBlock colors = button.colors;
        colors.normalColor = ChoiceCardColor;
        colors.highlightedColor = ChoiceCardHoverColor;
        colors.pressedColor = ChoiceCardPressedColor;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.70f, 0.70f, 0.64f, 0.7f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        LayoutElement cardLayout = cardObject.GetComponent<LayoutElement>();
        cardLayout.minWidth = 0f;
        cardLayout.minHeight = 0f;
        cardLayout.flexibleWidth = 1f;
        cardLayout.flexibleHeight = 1f;

        VerticalLayoutGroup content = cardObject.AddComponent<VerticalLayoutGroup>();
        content.padding = new RectOffset(18, 18, 18, 18);
        content.spacing = 5f;
        content.childControlWidth = true;
        content.childControlHeight = true;
        content.childForceExpandWidth = true;
        content.childForceExpandHeight = false;
        content.childAlignment = TextAnchor.UpperLeft;

        AddChoiceText(cardObject, label, 34f, true, ChoiceTextColor);
        AddChoiceText(cardObject, "성공확률: " + probability.ToString("P0"), 24f, false, ChoiceTextColor);
        AddChoiceText(cardObject, "비용: ₩ " + cost.ToString("N0"), 24f, false, ChoiceTextColor);
        if (!string.IsNullOrEmpty(successText))
        {
            AddChoiceText(cardObject, "성공시", 30f, true, SuccessColor);
            AddChoiceText(cardObject, successText, 23f, false, ChoiceTextColor);
        }
        AddFlexibleSpacer(cardObject);
        if (!string.IsNullOrEmpty(failureText))
        {
            AddChoiceText(cardObject, "실패시", 30f, true, FailureColor);
            AddChoiceText(cardObject, failureText, 23f, false, ChoiceTextColor);
        }
        runtimeButtons.Add(cardObject);
    }

    private void AddChoiceText(GameObject parent, string value, float fontSize, bool bold, Color color)
    {
        GameObject textObject = new GameObject("ChoiceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        // Runtime-created choice text must use the same Korean font/material as
        // the authored event panel; TMP's default font changes both glyphs and weight.
        if (cardView != null && cardView.titleText != null)
        {
            text.font = cardView.titleText.font;
            text.fontSharedMaterial = cardView.titleText.fontSharedMaterial;
        }
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(16f, fontSize * 0.65f);
        text.fontSizeMax = fontSize;
        text.overflowMode = TextOverflowModes.Overflow;
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
    }

    private void AddFlexibleSpacer(GameObject parent)
    {
        GameObject spacerObject = new GameObject("ChoiceFlexibleSpacer", typeof(RectTransform), typeof(LayoutElement));
        spacerObject.transform.SetParent(parent.transform, false);
        spacerObject.GetComponent<LayoutElement>().flexibleHeight = 1f;
    }

    private void CreateButtonContainer()
    {
        runtimeButtonContainer = new GameObject("RuntimeEventChoiceButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        runtimeButtonContainer.transform.SetParent(panel.transform, false);
        RectTransform containerRect = runtimeButtonContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.04f, 0.04f);
        containerRect.anchorMax = new Vector2(0.96f, 0.22f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        HorizontalLayoutGroup layout = runtimeButtonContainer.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private void CreateButton(string title, string subtitle, UnityEngine.Events.UnityAction action, bool interactable = true)
    {
        if (panel == null)
            return;

        GameObject buttonObject = new GameObject("RuntimeEventChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(runtimeButtonContainer != null ? runtimeButtonContainer.transform : panel.transform, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 0.86f, 0.86f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        button.onClick.AddListener(action);
        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.minWidth = 220f;
        layout.minHeight = 64f;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 20f;
        text.text = title + "\n<size=14>" + subtitle + "</size>";
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        runtimeButtons.Add(buttonObject);
    }

    private void ResizePanelForButtons(int buttonCount)
    {
        if (panelRect == null)
            return;

        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, Mathf.Max(originalPanelSize.y, 430f + buttonCount * 12f));
    }

    private void ResizePanelForChoiceCards()
    {
        if (panelRect == null)
            return;

        float width = 1100f;
        float height = width / ChoicePanelAspect;
        panelRect.sizeDelta = new Vector2(width, height);
        PositionExpandedPanel();
    }

    private void ResizePanelForResult()
    {
        if (panelRect == null)
            return;

        float width = 1100f;
        float height = width / ResultPanelAspect;
        panelRect.sizeDelta = new Vector2(width, height);
        PositionExpandedPanel();
    }

    private void ClearRuntimeButtons()
    {
        if (runtimeButtonContainer != null)
            Destroy(runtimeButtonContainer);
        else
            foreach (GameObject button in runtimeButtons)
                if (button != null) Destroy(button);

        runtimeButtonContainer = null;
        runtimeButtons.Clear();
    }

    private void RestorePanelSize()
    {
        if (panelRect != null)
        {
            panelRect.sizeDelta = originalPanelSize;
            panelRect.anchoredPosition = originalPanelPosition;
        }
        RestoreCardLayout();
    }

    private void PositionExpandedPanel()
    {
        if (panelRect == null)
            return;

        float addedHeight = Mathf.Max(0f, panelRect.sizeDelta.y - originalPanelSize.y);
        // Keep the expanded frame inside the viewport instead of growing equally upward.
        panelRect.anchoredPosition = originalPanelPosition + Vector2.down * (addedHeight * 0.45f);
    }

    private void AddHoverOutline(GameObject cardObject, Outline outline, Button button)
    {
        EventTrigger trigger = cardObject.AddComponent<EventTrigger>();
        trigger.triggers = new List<EventTrigger.Entry>();
        AddPointerTrigger(trigger, EventTriggerType.PointerEnter,
            () => outline.effectColor = button.interactable ? HoverOutlineColor : Color.clear);
        AddPointerTrigger(trigger, EventTriggerType.PointerExit, () => outline.effectColor = Color.clear);
        AddPointerTrigger(trigger, EventTriggerType.PointerDown,
            () => outline.effectColor = button.interactable ? HoverOutlineColor : Color.clear);
    }

    private static void AddPointerTrigger(EventTrigger trigger, EventTriggerType eventType,
        UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private void CacheCardLayout()
    {
        if (cardView == null || cardView.titleText == null || cardView.dateText == null || cardView.effectsText == null)
            return;

        originalTitleLayout = new RectLayout(cardView.titleText.rectTransform);
        originalDateLayout = new RectLayout(cardView.dateText.rectTransform);
        originalEffectsLayout = new RectLayout(cardView.effectsText.rectTransform);
        originalEffectsAlignment = cardView.effectsText.alignment;
        originalTitleFontSize = cardView.titleText.fontSize;
        originalDateFontSize = cardView.dateText.fontSize;
        originalEffectsFontSize = cardView.effectsText.fontSize;
        originalTitleFontSizeMin = cardView.titleText.fontSizeMin;
        originalTitleFontSizeMax = cardView.titleText.fontSizeMax;
        originalTitleWordWrapping = cardView.titleText.enableWordWrapping;
        originalTitleAutoSizing = cardView.titleText.enableAutoSizing;
        originalTitleOverflowMode = cardView.titleText.overflowMode;
        cardLayoutCached = true;
    }

    private void ApplyChoiceHeaderStyle()
    {
        if (!cardLayoutCached || cardView == null)
            return;

        // Scene 1 uses a two-line event headline above a separate right-aligned date.
        RectTransform titleRect = cardView.titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(30f, -30f);
        titleRect.sizeDelta = new Vector2(panelRect.sizeDelta.x - 150f, 110f);

        RectTransform dateRect = cardView.dateText.rectTransform;
        dateRect.anchorMin = new Vector2(1f, 1f);
        dateRect.anchorMax = new Vector2(1f, 1f);
        dateRect.pivot = new Vector2(1f, 1f);
        dateRect.anchoredPosition = new Vector2(-40f, -142f);
        dateRect.sizeDelta = new Vector2(400f, 38f);

        cardView.titleText.fontSize = 50f;
        cardView.titleText.enableWordWrapping = false;
        cardView.titleText.enableAutoSizing = true;
        cardView.titleText.fontSizeMin = 30f;
        cardView.titleText.fontSizeMax = 50f;
        cardView.titleText.overflowMode = TextOverflowModes.Ellipsis;
        cardView.dateText.fontSize = 28f;
    }

    private void ApplyResultCardLayout()
    {
        if (!cardLayoutCached || cardView == null || cardView.effectsText == null)
            return;

        // Results use a single, centered summary row like Figma scenes 3 and 4.
        RectTransform effectsRect = cardView.effectsText.rectTransform;
        effectsRect.anchorMin = new Vector2(0.5f, 0.5f);
        effectsRect.anchorMax = new Vector2(0.5f, 0.5f);
        effectsRect.pivot = new Vector2(0.5f, 0.5f);
        effectsRect.anchoredPosition = new Vector2(0f, -35f);
        effectsRect.sizeDelta = new Vector2(Mathf.Max(0f, panelRect.sizeDelta.x - 96f), 120f);
        cardView.effectsText.alignment = TextAlignmentOptions.Center;
        cardView.titleText.fontSize = 44f;
        cardView.dateText.fontSize = 28f;
        cardView.effectsText.fontSize = 38f;
    }

    private void RestoreCardLayout()
    {
        if (!cardLayoutCached || cardView == null)
            return;

        originalTitleLayout.Apply(cardView.titleText.rectTransform);
        originalDateLayout.Apply(cardView.dateText.rectTransform);
        originalEffectsLayout.Apply(cardView.effectsText.rectTransform);
        cardView.effectsText.alignment = originalEffectsAlignment;
        cardView.titleText.fontSize = originalTitleFontSize;
        cardView.dateText.fontSize = originalDateFontSize;
        cardView.effectsText.fontSize = originalEffectsFontSize;
        cardView.titleText.fontSizeMin = originalTitleFontSizeMin;
        cardView.titleText.fontSizeMax = originalTitleFontSizeMax;
        cardView.titleText.enableWordWrapping = originalTitleWordWrapping;
        cardView.titleText.enableAutoSizing = originalTitleAutoSizing;
        cardView.titleText.overflowMode = originalTitleOverflowMode;
    }

    private readonly struct RectLayout
    {
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 pivot;

        public RectLayout(RectTransform rect)
        {
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            anchoredPosition = rect.anchoredPosition;
            sizeDelta = rect.sizeDelta;
            pivot = rect.pivot;
        }

        public void Apply(RectTransform rect)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.pivot = pivot;
        }
    }

    private void OnChoiceClicked(int choiceIndex)
    {
        if (!awaitingChoice)
            return;
        DisableRuntimeButtons();
        EventHub.RaiseEventChoiceSelected(choiceIndex);
    }

    private void OnDebtPaymentChoiceClicked(int choiceIndex)
    {
        if (!awaitingDebtPayment)
            return;
        DisableRuntimeButtons();
        EventHub.RaiseDebtPaymentChoiceSelected(choiceIndex);
    }

    private void OnDebtPaymentClicked(bool pay)
    {
        if (!awaitingDebtPayment)
            return;
        DisableRuntimeButtons();
        EventHub.RaiseDebtPaymentSelected(pay);
    }

    private void DisableRuntimeButtons()
    {
        foreach (GameObject buttonObject in runtimeButtons)
        {
            Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            if (button != null) button.interactable = false;
        }
    }

    private void HandleMarketUpdated(PlayerStat stat)
    {
        if (awaitingDebtPayment && MarketManager.Instance != null
            && !MarketManager.Instance.IsAwaitingDebtPayment && !MarketManager.Instance.IsAwaitingEventChoice)
        {
            awaitingDebtPayment = false;
            Close();
        }
    }

    private void PlayOpenSfx()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(openSfx);
    }

    private void Close()
    {
        if (awaitingChoice || awaitingDebtPayment)
            return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(closeSfx);
        ClearRuntimeButtons();
        RestorePanelSize();
        if (pendingEvents.Count > 0)
        {
            ShowNextEvent();
            return;
        }
        showingEvent = false;
        ModalPause.Close(panel);
    }
}
