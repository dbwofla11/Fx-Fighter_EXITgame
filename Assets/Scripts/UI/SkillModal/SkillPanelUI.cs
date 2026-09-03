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

    public bool IsOpen => gameObject.activeSelf;

    private void Start()
    {
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
        EventHub.OnSkillPurchased += RefreshDetail;
        EventHub.OnSkillPurchaseSucceeded += HandlePurchaseSucceeded;
    }

    private void OnDisable()
    {
        EventHub.OnSkillClicked -= HandleSkillClicked;
        EventHub.OnSkillPurchased -= RefreshDetail;
        EventHub.OnSkillPurchaseSucceeded -= HandlePurchaseSucceeded;
    }

    private void HandleSkillClicked(SkillID id) => RefreshDetail();

    private void HandlePurchaseSucceeded(SkillID id)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(confirmSfx);
        RefreshDetail();
    }

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
        foreach (TabSlot tab in tabs)
            tab.button.image.color = tab.category == category ? SelectedTabColor : UnselectedTabColor;

        foreach (IconSlot slot in icons)
        {
            SkillSO profile = SkillManager.Instance.GetSkillProfile(slot.id);
            slot.button.gameObject.SetActive(profile != null && profile.category == category);
        }

        foreach (GroupLabelSlot group in groupLabels)
            group.label.gameObject.SetActive(group.category == category);
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
            if (purchaseBtnShadow != null) purchaseBtnShadow.SetActive(false);
            return;
        }

        SkillSO profile = SkillManager.Instance.GetSkillProfile(selected.Value);
        SkillButtonState.State state = SkillButtonState.Evaluate(selected.Value);
        int purchaseCount = SkillManager.Instance.GetPurchaseCount(selected.Value);

        string statusLine = state.Used ? "구매 완료"
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
