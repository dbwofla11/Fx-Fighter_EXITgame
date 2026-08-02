using UnityEngine;
using UnityEngine.UI;

// 시사 이벤트가 발생했을 때 메인 게임 화면 위에 뜨는 알림 패널. Figma 목업 없이 사용자가 준 스크린샷 2장
// (긍정/부정) 기준으로 만들었다 — 배경색만 다르고 구조(제목/날짜/효과 목록/닫기 버튼)는 EventCardView와 동일해
// 그대로 재사용한다. 이 오브젝트(EventNotification)는 항상 활성 상태를 유지하며 EventHub.OnEventTriggered를
// 상시 구독하고, 실제로 보이는 패널(panel)만 켜고 끈다 — StatGaugeUI 등 다른 상시 구독 UI와 동일한 관례.
public class EventNotificationUI : MonoBehaviour
{
    public GameObject panel;
    public EventCardView cardView;
    public Button closeButton;

    private void OnEnable()
    {
        EventHub.OnEventTriggered += HandleEventTriggered;
    }

    private void OnDisable()
    {
        EventHub.OnEventTriggered -= HandleEventTriggered;
    }

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panel != null) panel.SetActive(false);
    }

    private void HandleEventTriggered(EventLogEntry entry)
    {
        Color color = EventEffectFormatter.CategoryColor(entry.Profile.category);
        string effects = entry.Profile.effects != null ? EventEffectFormatter.BuildEffectsText(entry.Profile.effects) : "";

        if (cardView != null) cardView.Populate(color, entry.Profile.message, UIFormat.DateDot(entry.Date), effects);
        if (panel != null) panel.SetActive(true);

        // 알림이 떠 있는 동안은 TradeModalUI/EventLogPanelUI와 동일하게 게임 시간을 멈춘다.
        EventHub.RaiseGamePaused();
    }

    private void Close()
    {
        if (panel != null) panel.SetActive(false);

        EventHub.RaiseGameResumed();
    }
}
