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

    [Header("Streamer Chat")]
    public StreamerChatPanelUI chatPanelUI;
    public StreamerPanelUI streamerPanelUI;

    [Header("SFX")]
    [SerializeField] private AudioClip clickSfx; // 클릭소리

    // 탭 선택 상태 표시. Figma상 진한 빨강(#FF8686)=눌린(선택된) 탭, 연한 빨강(#FFDBDB)=안 눌린 탭.
    private static readonly Color SelectedTabColor = new Color(1f, 0.5255f, 0.5255f);
    private static readonly Color UnselectedTabColor = new Color(1f, 0.8588f, 0.8588f);

    public bool IsOpen => gameObject.activeSelf;

    private void Start()
    {
        if (overviewTab != null) overviewTab.onClick.AddListener(() => { PlayClickSfx(); ShowOverview(); });
        if (communityTab != null) communityTab.onClick.AddListener(() => { PlayClickSfx(); ShowCommunity(); });
        if (closeBtn != null) closeBtn.onClick.AddListener(() => { PlayClickSfx(); Close(); });

        if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        ModalPause.Open(gameObject);
        ShowOverview();
    }

    public void Close()
    {
        ModalPause.Close(gameObject);
    }

    private void PlayClickSfx()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
    }

    private void ShowOverview()
    {
        if (eventPanelBox != null) eventPanelBox.SetActive(false);
        if (chatPanelUI != null) chatPanelUI.gameObject.SetActive(false);
        if (overviewContent != null) overviewContent.SetActive(true);
        if (overviewUI != null) overviewUI.Refresh();
        SetTabSelected(overviewTab, communityTab);
    }

    private void ShowCommunity()
    {
        if (overviewContent != null) overviewContent.SetActive(false);
        if (eventPanelBox != null) eventPanelBox.SetActive(true);
        if (chatPanelUI != null) chatPanelUI.gameObject.SetActive(true);
        SetTabSelected(communityTab, overviewTab);
        RefreshLog();
        RefreshChat();
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
        Color color = entry.HasChoiceResult
            ? (entry.Succeeded ? EventEffectFormatter.PositiveColor : EventEffectFormatter.NegativeColor)
            : EventEffectFormatter.CategoryColor(entry.Profile.category);
        string effects = EventEffectFormatter.BuildEntryEffectsText(entry);

        EventCardView card = Instantiate(cardTemplate, cardListParent);
        card.gameObject.SetActive(true);
        card.Populate(color, EventEffectFormatter.BuildEntryTitle(entry), UIFormat.DateDot(entry.Date), effects);
    }

    #endregion

    private void RefreshChat()
    {
        if (chatPanelUI != null && streamerPanelUI != null)
            chatPanelUI.SetMessages(streamerPanelUI.ChatLog);
    }
}
