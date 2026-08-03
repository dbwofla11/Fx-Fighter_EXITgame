using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 스킬 아이콘/구매 버튼 패널. SkillBtn 클릭으로 토글되는 오버레이(EventLogPanelUI와 동일한 관례).
// 사용자가 준 실제 Figma 스크린샷("스킬구상도 - 디테일1") 기준으로 다시 잡은 버전 — 카테고리 탭 4개 +
// 선택된 스킬을 크게 보여주는 미리보기 박스 + 이름/설명/효과/비용 + 구매 버튼.
// 현재 SkillSO 데이터가 6개 전부 SkillCategory.CoinDesign이라 나머지 3개 탭은 당장은 빈 화면이다(데이터 문제,
// Next_Tesk.md 참고).
public class SkillPanelUI : MonoBehaviour
{
    [System.Serializable]
    public class IconSlot
    {
        public SkillID id;
        public Button button;
    }

    [System.Serializable]
    public class TabSlot
    {
        public SkillCategory category;
        public Button button;
    }

    [Header("Toggle")]
    public Button closeBtn; // SkillBtn 쪽 토글은 SkillPanelButton이 담당(아이콘 on/off 스프라이트 갱신 포함)

    [Header("Tabs")]
    public TabSlot[] tabs;

    [Header("Icons")]
    public IconSlot[] icons;
    public Image previewIcon; // 선택된 스킬을 크게 보여주는 미리보기 박스(PreviewFrame)

    [Header("Detail")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button purchaseBtn;

    [Header("Currency")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI coinText;

    // EventLogPanelUI의 탭 색상 관례와 동일 : 진한 빨강(#FF8686)=선택된 탭, 연한 빨강(#FFDBDB)=안 눌린 탭.
    private static readonly Color SelectedTabColor = new Color(1f, 0.5255f, 0.5255f);
    private static readonly Color UnselectedTabColor = new Color(1f, 0.8588f, 0.8588f);
    private static readonly Color SelectedIconColor = new Color(1f, 0.68f, 0.68f);
    // 1회성 스킬은 구매(사용) 전엔 흰색, 구매 후엔 회색으로 표시해 이미 썼다는 걸 구분.
    private static readonly Color UsedOneTimeIconColor = new Color(0.75f, 0.75f, 0.75f);

    public bool IsOpen => gameObject.activeSelf;

    private void Start()
    {
        if (closeBtn != null) closeBtn.onClick.AddListener(Close);
        if (purchaseBtn != null) purchaseBtn.onClick.AddListener(() => EventHub.RaiseSkillPurchased());

        foreach (IconSlot slot in icons)
        {
            SkillID id = slot.id;

            SkillSO profile = SkillManager.Instance.GetSkillProfile(id);
            if (profile != null && slot.button.image != null)
                slot.button.image.sprite = profile.icon;

            slot.button.onClick.AddListener(() => EventHub.RaiseSkillClicked(id));
        }

        foreach (TabSlot tab in tabs)
        {
            SkillCategory category = tab.category;
            tab.button.onClick.AddListener(() => SelectTab(category));
        }
    }

    private void OnEnable()
    {
        EventHub.OnSkillClicked += HandleSkillClicked;
        EventHub.OnSkillPurchased += RefreshDetail;
    }

    private void OnDisable()
    {
        EventHub.OnSkillClicked -= HandleSkillClicked;
        EventHub.OnSkillPurchased -= RefreshDetail;
    }

    private void HandleSkillClicked(SkillID id) => RefreshDetail();

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        EventHub.RaiseGamePaused();
        gameObject.SetActive(true);
        SelectTab(SkillCategory.CoinDesign);
        RefreshDetail();
    }

    public void Close()
    {
        EventHub.RaiseGameResumed();
        gameObject.SetActive(false);
    }

    // 탭 전환 : 선택된 카테고리에 속한 스킬 아이콘만 보여주고, 나머지는 숨긴다.
    private void SelectTab(SkillCategory category)
    {
        foreach (TabSlot tab in tabs)
            tab.button.image.color = tab.category == category ? SelectedTabColor : UnselectedTabColor;

        foreach (IconSlot slot in icons)
        {
            SkillSO profile = SkillManager.Instance.GetSkillProfile(slot.id);
            slot.button.gameObject.SetActive(profile != null && profile.category == category);
        }
    }

    private void RefreshDetail()
    {
        RefreshCurrency();

        SkillID? selected = SkillManager.Instance.SelectedSkillId;

        RefreshSelectionHighlight(selected);

        if (selected == null)
        {
            nameText.text = "";
            descriptionText.text = "";
            purchaseBtn.gameObject.SetActive(false);
            return;
        }

        SkillSO profile = SkillManager.Instance.GetSkillProfile(selected.Value);
        long cost = SkillManager.Instance.GetCurrentCost(selected.Value);
        int purchaseCount = SkillManager.Instance.GetPurchaseCount(selected.Value);
        // 1회성 스킬은 구매 후 다시 살 수 없다 (재사용형은 계속 재구매 가능).
        bool locked = !profile.isReusable && SkillManager.Instance.IsUnlocked(selected.Value);

        nameText.text = selected.Value.ToString();
        descriptionText.text = (locked ? "구매 완료" : "비용 : " + UIFormat.Currency(cost)) + "\n" +
            "구매 횟수 : " + purchaseCount + "회" + "\n\n" +
            EventEffectFormatter.BuildEffectsText(profile.effects) + "\n\n\n" +
            profile.description;
        purchaseBtn.gameObject.SetActive(true);
        purchaseBtn.interactable = !locked;
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
    // 1회성 스킬은 이미 구매(사용)했으면 회색으로, 아직 안 샀으면 흰색으로 표시한다.
    private void RefreshSelectionHighlight(SkillID? selected)
    {
        foreach (IconSlot slot in icons)
        {
            if (slot.id == selected)
            {
                slot.button.image.color = SelectedIconColor;
                continue;
            }

            SkillSO profile = SkillManager.Instance.GetSkillProfile(slot.id);
            bool used = profile != null && !profile.isReusable && SkillManager.Instance.IsUnlocked(slot.id);
            slot.button.image.color = used ? UsedOneTimeIconColor : Color.white;
        }

        if (previewIcon != null)
            previewIcon.sprite = selected.HasValue ? SkillManager.Instance.GetSkillProfile(selected.Value).icon : null;
    }
}
