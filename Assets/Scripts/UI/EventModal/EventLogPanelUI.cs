using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 이벤트 로그 패널(전체화면 오버레이). Figma가 프레임 2개로 나뉘어 있다(EjUw2LdqxAYhL2180OAXHo) —
// node 1253:2 "뉴스,이벤트 페이지 - 스탯개요"(개요 탭 콘텐츠, 아래 overviewContent/EventOverviewUI)와
// node 1261:195 "뉴스,이벤트 페이지 - 이벤트 패널"(커뮤니티 탭 콘텐츠, 아래 eventPanelBox).
// 탭/닫기버튼/ContentArea/eventPanelBox/overviewContent/카드 템플릿은 전부 씬에 직접 배치된 오브젝트를 참조한다
// (TradeModalUI/CoinControlModalUI와 동일한 관례 — 스크립트는 로직/참조만 담당).
// "커뮤니티" 탭에는 MarketManager.EventLog(Profile.message/Date/effects)를 카드로 나열한다. 카드 개수가
// 가변적이라 이 부분만 "cardTemplate"을 Instantiate로 복제해서 채운다(TradeModalUI가 BtnPlus1을 복제해
// "+/-" 버튼을 만든 것과 동일한 방식).
// "개요" 탭(스탯개요 + 엑시트 버튼)은 EventOverviewUI가 담당 — 이 스크립트는 열 때마다 Refresh()만 호출한다.
public class EventLogPanelUI : MonoBehaviour
{
    [Header("Tabs / Close")]
    public Button overviewTab;
    public Button communityTab;
    public Button closeBtn;
    public GameObject eventPanelBox;

    [Header("Overview Tab")]
    public GameObject overviewContent;
    public EventOverviewUI overviewUI;

    [Header("Event List")]
    public RectTransform cardListParent;
    public EventCardView cardTemplate;

    private static readonly Color PositiveCardColor = new Color(0.6941f, 1f, 0.6941f); // #B1FFB1
    private static readonly Color NegativeCardColor = new Color(1f, 0.7294f, 0.6941f); // #FFBAB1

    // 탭 선택 상태 표시. Figma상 진한 빨강(#FF8686)=눌린(선택된) 탭, 연한 빨강(#FFDBDB)=안 눌린 탭.
    private static readonly Color SelectedTabColor = new Color(1f, 0.5255f, 0.5255f);
    private static readonly Color UnselectedTabColor = new Color(1f, 0.8588f, 0.8588f);

    public bool IsOpen => gameObject.activeSelf;

    private void Start()
    {
        if (overviewTab != null) overviewTab.onClick.AddListener(ShowOverview);
        if (communityTab != null) communityTab.onClick.AddListener(ShowCommunity);
        if (closeBtn != null) closeBtn.onClick.AddListener(Close);

        if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        // 롱숏 거래 모달(TradeModalUI)과 동일하게, 이 패널이 떠 있는 동안은 게임 시간을 멈춘다.
        EventHub.RaiseGamePaused();

        gameObject.SetActive(true);
        ShowOverview();
    }

    public void Close()
    {
        EventHub.RaiseGameResumed();

        gameObject.SetActive(false);
    }

    private void ShowOverview()
    {
        if (eventPanelBox != null) eventPanelBox.SetActive(false);
        if (overviewContent != null) overviewContent.SetActive(true);
        if (overviewUI != null) overviewUI.Refresh();
        SetTabSelected(overviewTab, communityTab);
    }

    private void ShowCommunity()
    {
        if (overviewContent != null) overviewContent.SetActive(false);
        if (eventPanelBox != null) eventPanelBox.SetActive(true);
        SetTabSelected(communityTab, overviewTab);
        RefreshLog();
    }

    private void SetTabSelected(Button selected, Button unselected)
    {
        if (selected != null) selected.image.color = SelectedTabColor;
        if (unselected != null) unselected.image.color = UnselectedTabColor;
    }

    #region 로그 목록

    private void RefreshLog()
    {
        if (cardListParent == null || cardTemplate == null || MarketManager.Instance == null)
            return;

        foreach (Transform child in cardListParent)
            if (child != cardTemplate.transform)
                Destroy(child.gameObject);

        foreach (EventLogEntry entry in MarketManager.Instance.EventLog)
            CreateCard(entry);
    }

    private void CreateCard(EventLogEntry entry)
    {
        bool isPositive = entry.Profile.category == EventCategory.Positive;
        Color color = isPositive ? PositiveCardColor : NegativeCardColor;
        string effects = entry.Profile.effects != null ? BuildEffectsText(entry.Profile.effects) : "";

        EventCardView card = Instantiate(cardTemplate, cardListParent);
        card.gameObject.SetActive(true);
        card.Populate(color, entry.Profile.message, UIFormat.DateDot(entry.Date), effects);
    }

    private string BuildEffectsText(List<EffectData> effects)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < effects.Count; i++)
        {
            (string label, float delta) = DescribeEffect(effects[i]);
            if (i > 0) sb.Append('\n');
            sb.Append(label).Append(' ').Append(delta >= 0 ? "+" : "").Append(delta.ToString("0.#"));
        }

        return sb.ToString();
    }

    // 부호(+ = 증가, - = 감소)는 UIFormat.SignedEffectValue가 판정한다. 여기서는 라벨만 고른다.
    private (string label, float delta) DescribeEffect(EffectData effect)
    {
        string label = effect.effectType switch
        {
            EffectType.SupportIncrease => "코인 지지도",
            EffectType.GrowthIncrease => "코인 상승률",
            EffectType.DoubtDecrease or EffectType.DoubtIncrease => "의심도",
            EffectType.PositiveEventRate => "긍정 이벤트 확률",
            EffectType.NegativeEventRate => "부정 이벤트 확률",
            EffectType.CashBonus => "거래 수익",
            EffectType.VolumeIncrease or EffectType.VolumeDecrease => "코인 거래량",
            EffectType.ExitUnlock => "엑시트 조건",
            EffectType.SupplyIncrease or EffectType.SupplyDecrease => "발행량",
            _ => effect.effectType.ToString(),
        };

        return (label, UIFormat.SignedEffectValue(effect));
    }

    #endregion
}
