using UnityEngine;

// 모달/패널을 열고 닫을 때마다 반복되던 "게임 시간 멈추기+패널 표시" / "재개+패널 숨기기" 패턴을 하나로 묶는다
// (TradeModalUI/CoinControlModalUI/SkillPanelUI/EventLogPanelUI/EventNotificationUI/SettingsUI 6곳에서 각자
// 구현하던 걸 통합). UI 필드 중 하나가 끊겨 있어도 일시정지/재개만은 항상 걸리도록 EventHub 호출을 먼저 한다.
public static class ModalPause
{
    public static void Open(GameObject panel)
    {
        EventHub.RaiseGamePaused();
        if (panel != null) panel.SetActive(true);
    }

    public static void Close(GameObject panel)
    {
        EventHub.RaiseGameResumed();
        if (panel != null) panel.SetActive(false);
    }
}
