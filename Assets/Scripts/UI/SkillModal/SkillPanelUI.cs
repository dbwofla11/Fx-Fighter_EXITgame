using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 스킬 아이콘/구매 버튼 패널. SkillBtn 클릭으로 토글되는 오버레이(EventLogPanelUI와 동일한 관례).
// 사용자가 준 실제 Figma 스크린샷("스킬구상도 - 디테일1") 기준으로 다시 잡은 버전 — 카테고리 탭 4개 +
// 선택된 스킬을 크게 보여주는 미리보기 박스 + 이름/설명/효과/비용 + 구매 버튼.
// 세부 카테고리 해금과 선행 스킬 잠금은 SkillManager가 판정하고, 이 UI는 상태를 표시한다.
public class SkillPanelUI : MonoBehaviour
{
    [System.Serializable]
    public class IconSlot
    {
        public SkillID id;
        public Button button;
        public Outline border; // 1회성/재사용형 구분 테두리(Outline 이펙트, 선택 사항 — 비워두면 표시 안 함)
        public TextMeshProUGUI label; // 아이콘 밑에 스킬 이름 + 구매 횟수 표시(선택 사항 — 비워두면 표시 안 함)
    }

    [System.Serializable]
    public class TabSlot
    {
        public SkillCategory category;
        public Button button;
    }

    [System.Serializable]
    public class GroupLabelSlot
    {
        public SkillCategory category;
        public TextMeshProUGUI label; // 아이콘 그리드 내 서브카테고리(예: "공급 구조") 소제목. 탭 전환 시 아이콘과 같이 보이기/숨기기 처리.
    }

    [Header("Toggle")]
    public Button closeBtn; // SkillBtn 쪽 토글은 SkillPanelButton이 담당(아이콘 on/off 스프라이트 갱신 포함)

    [Header("Tabs")]
    public TabSlot[] tabs;

    [Header("Icons")]
    public IconSlot[] icons;
    public GroupLabelSlot[] groupLabels;
    public Image previewIcon; // 선택된 스킬을 크게 보여주는 미리보기 박스(PreviewFrame)

    [Header("Detail")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button purchaseBtn;
    public GameObject purchaseBtnShadow; // 구매 버튼 아래 깔린 입체 음영(빨강 고정). 잠기면 숨겨서 "다 썼다"는 느낌을 준다.

    [Header("Currency")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI coinText;

    [Header("SFX")]
    [SerializeField] private AudioClip clickSfx;   // 클릭소리 (아이콘 클릭)
    [SerializeField] private AudioClip confirmSfx; // 확인버튼소리 (구매 버튼)

    // EventLogPanelUI의 탭 색상 관례와 동일 : 진한 빨강(#FF8686)=선택된 탭, 연한 빨강(#FFDBDB)=안 눌린 탭.
    private static readonly Color SelectedTabColor = new Color(1f, 0.5255f, 0.5255f);
    private static readonly Color UnselectedTabColor = new Color(1f, 0.8588f, 0.8588f);
    private static readonly Color SelectedIconColor = new Color(1f, 0.68f, 0.68f);
    private static readonly Color FallingSkillIconColor = new Color(1f, 0.64f, 0.64f);
    private static readonly Color FallingSkillSelectedColor = new Color(1f, 0.38f, 0.38f);
    private const SkillID FakeSellWallSkillId = (SkillID)15;
    private const SkillID FomoSkillId = (SkillID)19;
    // 1회성 스킬은 구매(사용) 전엔 흰색, 구매 후엔 연한 파란색으로 표시해 이미 썼다는 걸 구분한다.
    // 초록 -> 연한 파랑으로 교체 (피드백, 2026-08-05).
    private static readonly Color UsedOneTimeIconColor = new Color(0.55f, 0.75f, 1f);
    // 코인 발행 기능을 해금하는 스킬은 재사용/1회성 상태와 별도로 보라색 테두리로 구분한다.
    private static readonly Color MintUnlockBorderColor = new Color(0.66f, 0.47f, 1f);
    // 구매 완료(잠김) 상태의 구매 버튼 색. ColorTint의 disabledColor는 원래 버튼 색(핑크 계열)을 그대로
    // 어둡게만 하는 정도라 회색으로 안 보여서, 잠기면 이 색을 직접 덮어쓴다.
    private static readonly Color PurchasedButtonColor = new Color(0.6f, 0.6f, 0.6f);
    private Color purchaseBtnDefaultColor;
    private bool purchaseBtnDefaultColorCaptured;

    private SkillCategory selectedCategory;
    private readonly HashSet<string> selectedEffects = new();
    private readonly Dictionary<string, Button> effectButtons = new();
    private GameObject filterPanel;
    private TextMeshProUGUI filterButtonText;
    private TextMeshProUGUI filterResultText;

    public bool IsOpen => gameObject.activeSelf;

    private void Start()
    {
        EnsureAdditionalCoinDesignSlots();
        RefreshCoinDesignGroupLabels();
        BuildFilterUI();
        if (closeBtn != null) closeBtn.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
            Close();
        });

        if (purchaseBtn != null)
        {
            purchaseBtn.onClick.AddListener(() => EventHub.RaiseSkillPurchased());

            // Button의 기본 ColorTint 전환은 포인터 상태(hover/press/disabled)가 바뀔 때마다 자체 색으로
            // 되돌아가 RefreshDetail이 지정한 잠금 상태 색(회색)을 덮어써 버린다. 전환 자체를 꺼서
            // purchaseBtn.image.color를 RefreshDetail만 제어하도록 한다.
            purchaseBtn.transition = Selectable.Transition.None;
        }

        foreach (IconSlot slot in icons)
        {
            SkillID id = slot.id;

            SkillSO profile = SkillManager.Instance.GetSkillProfile(id);
            if (profile != null && slot.button.image != null)
                slot.button.image.sprite = profile.icon;

            slot.button.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
                EventHub.RaiseSkillClicked(id);
            });
        }

        foreach (TabSlot tab in tabs)
        {
            SkillCategory category = tab.category;

            HoverTooltipTrigger tooltip = tab.button.GetComponent<HoverTooltipTrigger>();
            if (tooltip == null)
                tooltip = tab.button.gameObject.AddComponent<HoverTooltipTrigger>();
            tooltip.SetTooltipText(GetCategoryTooltip(category));

            tab.button.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
                SelectTab(category);
            });
        }
    }

    private void OnEnable()
    {
        EventHub.OnSkillClicked += HandleSkillClicked;
        EventHub.OnSkillPurchaseSucceeded += HandlePurchaseSucceeded;
        EventHub.OnSkillTreeChanged += HandleSkillTreeChanged;
    }

    private void OnDisable()
    {
        if (filterPanel != null)
        {
            filterPanel.SetActive(false);
            filterButtonText.text = filterButtonText.text.Replace(" −", " +");
        }
        EventHub.OnSkillClicked -= HandleSkillClicked;
        EventHub.OnSkillPurchaseSucceeded -= HandlePurchaseSucceeded;
        EventHub.OnSkillTreeChanged -= HandleSkillTreeChanged;
    }

    private void HandleSkillClicked(SkillID id) => RefreshDetail();

    private void HandlePurchaseSucceeded(SkillID id)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(confirmSfx);
        RefreshDetail();
    }

    private void HandleSkillTreeChanged() => RefreshDetail();

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        ModalPause.Open(gameObject);
        SelectTab(SkillCategory.CoinDesign);
        RefreshDetail();
    }

    public void Close()
    {
        ModalPause.Close(gameObject);
    }

    // 탭 전환 : 선택된 카테고리에 속한 스킬 아이콘만 보여주고, 나머지는 숨긴다.
    private void SelectTab(SkillCategory category)
    {
        selectedCategory = category;
        int visibleCount = 0;
        foreach (TabSlot tab in tabs)
            tab.button.image.color = tab.category == category ? SelectedTabColor : UnselectedTabColor;

        foreach (IconSlot slot in icons)
        {
            SkillSO profile = SkillManager.Instance.GetSkillProfile(slot.id);
            bool visible = MatchesFilters(profile);
            slot.button.gameObject.SetActive(visible);
            if (visible) visibleCount++;
        }

        foreach (GroupLabelSlot group in groupLabels)
        {
            if (group == null || group.label == null)
                continue;

            bool hasLabel = !string.IsNullOrWhiteSpace(group.label.text);
            group.label.gameObject.SetActive(group.category == category && visibleCount > 0 && hasLabel);
        }

        if (filterResultText != null)
            filterResultText.text = visibleCount == 0 ? "조건에 맞는 스킬이 없습니다" : $"스킬 {visibleCount}개 · 선택한 효과 중 하나라도 포함";
        RefreshDetail();
        if (visibleCount == 0) nameText.text = "조건에 맞는 스킬이 없습니다";
    }

    private bool MatchesFilters(SkillSO profile)
    {
        if (profile == null || profile.category != selectedCategory) return false;
        if (selectedEffects.Count == 0) return true;
        return profile.effects != null && profile.effects.Exists(effect =>
            effect != null && selectedEffects.Contains(EventEffectFormatter.GetEffectLabel(effect.effectType)));
    }

    private void BuildFilterUI()
    {
        // 기존 탭 위치를 기준으로 PC/Android 씬에 동일한 필터를 붙인다.
        RectTransform lastTab = (RectTransform)tabs[tabs.Length - 1].button.transform;
        Button expand = CreateFilterButton(lastTab.parent, "FilterButton", "필터 +",
            Vector2.zero, new Vector2(76, lastTab.rect.height));
        expand.image.color = new Color(0.12f, 0.12f, 0.12f);
        RectTransform expandRect = (RectTransform)expand.transform;
        expandRect.anchorMin = lastTab.anchorMin;
        expandRect.anchorMax = lastTab.anchorMax;
        expandRect.pivot = new Vector2(0, lastTab.pivot.y);
        expandRect.anchoredPosition = lastTab.anchoredPosition +
            new Vector2(lastTab.rect.width * (1 - lastTab.pivot.x) + 8, 0);
        filterButtonText = expand.GetComponentInChildren<TextMeshProUGUI>();

        var labels = new List<string>();
        foreach (IconSlot slot in icons)
        {
            SkillSO profile = SkillManager.Instance.GetSkillProfile(slot.id);
            if (profile == null || profile.effects == null) continue;
            foreach (EffectData effect in profile.effects)
            {
                if (effect == null) continue;
                string label = EventEffectFormatter.GetEffectLabel(effect.effectType);
                if (!labels.Contains(label)) labels.Add(label);
            }
        }

        float height = 176 + Mathf.Ceil(labels.Count / 2f) * 64;
        RectTransform panel = CreateFilterRect(expand.transform.parent, "EffectFilter", Vector2.zero, new Vector2(540, height));
        panel.anchorMin = lastTab.anchorMin;
        panel.anchorMax = lastTab.anchorMax;
        panel.pivot = new Vector2(1, 1);
        panel.anchoredPosition = expandRect.anchoredPosition + new Vector2(76, -lastTab.rect.height * lastTab.pivot.y - 10);
        panel.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f, 0.98f);
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.3f);
        outline.effectDistance = new Vector2(2, -3);
        filterPanel = panel.gameObject;
        CreateFilterText(panel, "스킬 효과 필터", new Vector2(20, -16), new Vector2(310, 34), 28, Color.black);
        CreateFilterButton(panel, "Reset", "초기화", new Vector2(348, -14), new Vector2(110, 40))
            .onClick.AddListener(() => { selectedEffects.Clear(); RefreshFilters(); });
        CreateFilterButton(panel, "Close", "X", new Vector2(472, -14), new Vector2(48, 40))
            .onClick.AddListener(ToggleFilter);
        CreateFilterText(panel, "효과 선택 · 복수 선택 가능", new Vector2(20, -64), new Vector2(500, 30), 22, Color.black);
        for (int i = 0; i < labels.Count; i++)
        {
            string label = labels[i];
            Button button = CreateFilterButton(panel, "Effect_" + i, label,
                new Vector2(20 + i % 2 * 254, -104 - i / 2 * 64), new Vector2(246, 54));
            effectButtons.Add(label, button);
            button.onClick.AddListener(() =>
            {
                if (!selectedEffects.Add(label)) selectedEffects.Remove(label);
                RefreshFilters();
            });
        }
        filterResultText = CreateFilterText(panel, "", new Vector2(20, -height + 54), new Vector2(500, 38), 20, Color.black);
        expand.onClick.AddListener(ToggleFilter);
        filterPanel.SetActive(false);
        RefreshFilters();
    }

    private void ToggleFilter()
    {
        filterPanel.SetActive(!filterPanel.activeSelf);
        RefreshFilters();
    }

    private void RefreshFilters()
    {
        foreach (var entry in effectButtons)
        {
            bool selected = selectedEffects.Contains(entry.Key);
            entry.Value.image.color = selected ? new Color(0, 0.65f, 0.86f) : new Color(0.2f, 0.2f, 0.2f);
            entry.Value.GetComponentInChildren<TextMeshProUGUI>().text = (selected ? "[선택] " : "") + entry.Key;
        }
        filterButtonText.text = (selectedEffects.Count == 0 ? "필터\n" : $"필터\n{selectedEffects.Count}") +
            (filterPanel.activeSelf ? " −" : " +");
        SelectTab(selectedCategory);
    }

    private static RectTransform CreateFilterRect(Transform parent, string objectName, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private TextMeshProUGUI CreateFilterText(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        TextMeshProUGUI label = CreateFilterRect(parent, "Label", position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = nameText.font;
        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16;
        label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateFilterButton(Transform parent, string objectName, string text, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateFilterRect(parent, objectName, position, size);
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        TextMeshProUGUI label = CreateFilterText(rect, text, new Vector2(6, -4), size - new Vector2(12, 8), 24, Color.white);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void RefreshDetail()
    {
        RefreshCurrency();

        SkillID? selected = SkillManager.Instance.SelectedSkillId;

        if (selected.HasValue && !MatchesFilters(SkillManager.Instance.GetSkillProfile(selected.Value)))
            selected = null;
        RefreshSelectionHighlight(selected);

        if (selected == null)
        {
            nameText.text = "";
            descriptionText.text = "";
            purchaseBtn.gameObject.SetActive(false);
            if (purchaseBtnShadow != null) purchaseBtnShadow.SetActive(false);
            return;
        }

        SkillSO profile = SkillManager.Instance.GetSkillProfile(selected.Value);
        SkillButtonState.State state = SkillButtonState.Evaluate(selected.Value);
        int purchaseCount = SkillManager.Instance.GetPurchaseCount(selected.Value);

        List<SkillID> missingPrerequisites = SkillManager.Instance.GetMissingPrerequisites(selected.Value);
        string statusLine = !state.PrerequisitesMet ? "선행 스킬 필요 : " + string.Join(", ", missingPrerequisites)
            : state.Used ? "구매 완료"
            : state.MaxedOut ? "최대 구매 횟수 도달"
            : state.OnCooldown ? "쿨타임 : 다음 턴에 구매 가능"
            : "비용 : " + UIFormat.Currency(state.Cost);

        nameText.text = selected.Value.ToString();
        descriptionText.text = (profile.isReusable ? "[재사용형]" : "[1회성]") + "\n" +
            statusLine + "\n" +
            "구매 횟수 : " + purchaseCount + "회" + "\n\n" +
            EventEffectFormatter.BuildEffectsText(profile.effects, colorize: true) + "\n\n\n" +
            profile.description;
        purchaseBtn.gameObject.SetActive(true);
        purchaseBtn.interactable = state.Purchasable;
        if (purchaseBtnShadow != null) purchaseBtnShadow.SetActive(state.Purchasable);

        if (!purchaseBtnDefaultColorCaptured)
        {
            purchaseBtnDefaultColor = purchaseBtn.image.color;
            purchaseBtnDefaultColorCaptured = true;
        }
        purchaseBtn.image.color = state.Purchasable ? purchaseBtnDefaultColor : PurchasedButtonColor;
    }

    // 우측 상단 보유 현금/코인 실시간 표시.
    private void RefreshCurrency()
    {
        if (moneyText != null)
            moneyText.text = UIFormat.Currency(PlayerManager.Instance.currentMoney);
        if (coinText != null)
            coinText.text = "보유 코인수량 : " + PlayerManager.Instance.currentCoins.ToString("N0") + "개";
    }

    // 선택된 아이콘은 그리드에서 살짝 하이라이트 + 미리보기 박스에 크게 표시한다.
    // 지금 살 수 없는 스킬(1회성 구매 완료 / 잔액 부족 / 재사용형 최대 구매 도달 / 쿨타임)은 SkillButtonState로 판정해
    // 회색 처리한다(구매 완료는 별도로 연한 파랑).
    private void RefreshSelectionHighlight(SkillID? selected)
    {
        foreach (IconSlot slot in icons)
        {
            SkillSO profile = SkillManager.Instance.GetSkillProfile(slot.id);

            // 테두리 색으로 재사용형/1회성 구분 (선택 여부와 무관하게 항상 표시).
            if (slot.border != null && profile != null)
                slot.border.effectColor = slot.id == SkillID.추가발행권한
                    ? MintUnlockBorderColor
                    : (profile.isReusable ? EventEffectFormatter.PositiveColor : EventEffectFormatter.NegativeColor);

            // 아이콘 밑에 스킬 이름 표시. 재사용형만 이름 옆에 구매 횟수를 괄호로 붙인다
            // (1회성은 최대 1회라 표시 의미가 없어 생략).
            if (slot.label != null && profile != null)
                slot.label.text = profile.isReusable
                    ? slot.id + " (" + SkillManager.Instance.GetPurchaseCount(slot.id) + "회)"
                    : slot.id.ToString();

            if (slot.id == selected)
            {
                slot.button.image.color = IsFallingSkill(slot.id)
                    ? FallingSkillSelectedColor
                    : SelectedIconColor;
                continue;
            }

            Color color = IsFallingSkill(slot.id) ? FallingSkillIconColor : Color.white;
            if (profile != null)
            {
                SkillButtonState.State state = SkillButtonState.Evaluate(slot.id);
                // 이미 구매(사용)한 1회성은 연한 파랑, 그 외 지금 살 수 없는 상태(잔액 부족/최대 구매 도달)는 회색.
                color = state.Used ? UsedOneTimeIconColor : !state.Purchasable ? PurchasedButtonColor : Color.white;
            }
            slot.button.image.color = color;
        }

        if (previewIcon != null)
            previewIcon.sprite = selected.HasValue ? SkillManager.Instance.GetSkillProfile(selected.Value).icon : null;
    }

    private static bool IsFallingSkill(SkillID id)
    {
        return id == FakeSellWallSkillId || id == FomoSkillId;
    }

    /// <summary>
    /// 기존 코인설계 8종 아이콘을 유지하면서 신규 해금 스킬 5종을 추가한다.
    /// 씬의 기존 아이콘 슬롯 수를 늘리지 않고 동일한 버튼 프리팹을 런타임 복제해 PC/Android 씬의
    /// 중복 배치를 방지한다.
    /// </summary>
    private void EnsureAdditionalCoinDesignSlots()
    {
        if (icons == null || icons.Length == 0 || SkillManager.Instance == null)
            return;

        SkillID[] additionalIds =
        {
            SkillID.토큰기획,
            SkillID.분배구조설계,
            SkillID.시장진입설계,
            SkillID.신뢰도구축설계,
            SkillID.통합운영설계
        };

        List<IconSlot> slots = new(icons);
        HashSet<SkillID> existingIds = new();
        foreach (IconSlot slot in slots)
            existingIds.Add(slot.id);

        IconSlot source = null;
        foreach (IconSlot slot in slots)
        {
            SkillSO profile = SkillManager.Instance.GetSkillProfile(slot.id);
            if (profile != null && profile.category == SkillCategory.CoinDesign && slot.button != null)
            {
                source = slot;
                break;
            }
        }

        if (source == null)
            return;

        RectTransform sourceRect = source.button.transform as RectTransform;
        Transform parent = source.button.transform.parent;
        if (sourceRect == null || parent == null)
            return;

        // 기존 배치 슬롯의 레이아웃을 유지하고, 새 5개는 제거된 스킬의 빈 칸에 배치한다.
        Vector2[] positions =
        {
            new(-290f, -110f),
            new(-130f, -110f),
            new(30f, -110f),
            new(190f, -110f),
            new(-290f, -295f)
        };

        for (int i = 0; i < additionalIds.Length; i++)
        {
            SkillID id = additionalIds[i];
            if (existingIds.Contains(id) || SkillManager.Instance.GetSkillProfile(id) == null)
                continue;

            GameObject clone = Instantiate(source.button.gameObject, parent, false);
            clone.name = "RuntimeSkillIcon_" + id;
            RectTransform cloneRect = clone.transform as RectTransform;
            if (cloneRect != null)
                cloneRect.anchoredPosition = positions[i];

            Button button = clone.GetComponent<Button>();
            if (button == null)
            {
                Destroy(clone);
                continue;
            }

            Outline border = clone.GetComponent<Outline>();
            TextMeshProUGUI label = clone.GetComponentInChildren<TextMeshProUGUI>();
            slots.Add(new IconSlot
            {
                id = id,
                button = button,
                border = border,
                label = label
            });
            existingIds.Add(id);
        }

        icons = slots.ToArray();
    }

    private void RefreshCoinDesignGroupLabels()
    {
        if (groupLabels == null)
            return;

        string[] coinDesignLabels =
        {
            "기존 고유 기능",
            "발행량·공급 구조",
            "시장·여론 카테고리 해금"
        };
        int coinDesignIndex = 0;

        foreach (GroupLabelSlot group in groupLabels)
        {
            if (group == null || group.category != SkillCategory.CoinDesign || group.label == null)
                continue;

            bool hasLabel = coinDesignIndex < coinDesignLabels.Length;
            group.label.text = hasLabel ? coinDesignLabels[coinDesignIndex++] : string.Empty;
            group.label.gameObject.SetActive(hasLabel);
        }
    }

    private static string GetCategoryTooltip(SkillCategory category)
    {
        switch (category)
        {
            case SkillCategory.CoinDesign:
                return "코인 설계\n코인의 구조와 발행량을 설계해 장기적인 성장 기반을 만듭니다.";
            case SkillCategory.Marketing:
                return "시장 조작\n거래량과 시세 흐름에 개입해 시장의 움직임을 조절합니다.";
            case SkillCategory.Propaganda:
                return "여론 조작\n홍보와 여론 활동으로 지지도와 성장 기대를 높입니다.";
            case SkillCategory.Depence_Exit:
                return "방어 및 엑시트\n의심을 관리하고 위험에 대비해 안전한 엑시트를 준비합니다.";
            default:
                return string.Empty;
        }
    }
}
