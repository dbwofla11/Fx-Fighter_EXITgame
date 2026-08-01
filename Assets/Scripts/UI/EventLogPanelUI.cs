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
        card.Populate(color, entry.Profile.message, FormatDate(entry.Date), effects);
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

    // EffectType은 Job/Skill과 공유하는 부호 있는 값이라, 증가형(Increase)은 값 그대로, 감소형(Decrease)은
    // 부호를 뒤집어서 "스탯이 실제로 변한 방향"을 표시한다(+ = 증가, - = 감소, 긍정/부정 여부와 무관).
    private (string label, float delta) DescribeEffect(EffectData effect)
    {
        switch (effect.effectType)
        {
            case EffectType.SupportIncrease: return ("코인 지지도", effect.value);
            case EffectType.GrowthIncrease: return ("코인 상승률", effect.value);
            case EffectType.DoubtDecrease: return ("의심도", -effect.value);
            case EffectType.DoubtIncrease: return ("의심도", effect.value);
            case EffectType.PositiveEventRate: return ("긍정 이벤트 확률", effect.value);
            case EffectType.NegativeEventRate: return ("부정 이벤트 확률", effect.value);
            case EffectType.CashBonus: return ("거래 수익", effect.value);
            case EffectType.VolumeIncrease: return ("코인 거래량", effect.value);
            case EffectType.VolumeDecrease: return ("코인 거래량", -effect.value);
            case EffectType.ExitUnlock: return ("엑시트 조건", effect.value);
            case EffectType.SupplyIncrease: return ("발행량", effect.value);
            case EffectType.SupplyDecrease: return ("발행량", -effect.value);
            default: return (effect.effectType.ToString(), effect.value);
        }
    }

    // DateTime.ToString("yyyy.MM.dd")은 문화권에 따라 구분자가 바뀔 수 있어(PriceChartUI와 동일한 이유) 직접 포맷한다.
    private static string FormatDate(System.DateTime date) => $"{date.Year}.{date.Month:00}.{date.Day:00}";

    #endregion
}
