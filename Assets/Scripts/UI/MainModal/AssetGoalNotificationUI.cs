using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 현금이 목표 금액(엑시트 조건)에 처음 도달했을 때 이벤트 로그 버튼 옆에 뜨는 알림 배너(목업 기준). 오브젝트는 항상
// 활성 상태를 유지하고 EventHub.OnAssetGoalAchieved를 상시 구독하며, panel만 켜고 끈다
// (EventNotificationUI와 동일한 관례). X 버튼으로 닫으면 끝 — 달성 순간 1회성 알림이라 다시 뜨지 않는다.
// 뜬 지 AutoOpenDelay초가 지나면 자동으로 닫히고 이벤트 로그 패널로 넘어간다(닫기 버튼으로 먼저 닫으면 취소).
public class AssetGoalNotificationUI : MonoBehaviour
{
    private const float AutoOpenDelay = 2f;

    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    [SerializeField] private EventLogPanelUI eventLogPanel;
    [SerializeField] private AudioClip openSfx;

    private Coroutine autoOpenRoutine;

    private void OnEnable()
    {
        EventHub.OnAssetGoalAchieved += HandleAssetGoalAchieved;
    }

    private void OnDisable()
    {
        EventHub.OnAssetGoalAchieved -= HandleAssetGoalAchieved;
    }

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (panel != null) panel.SetActive(false);
    }

    private void HandleAssetGoalAchieved()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(openSfx);
        ModalPause.Open(panel);
        autoOpenRoutine = StartCoroutine(AutoOpenEventLog());
    }

    // 게임이 정지(Time.timeScale=0)된 상태에서도 흘러야 하는 타이머라 실시간 대기를 쓴다.
    private IEnumerator AutoOpenEventLog()
    {
        yield return new WaitForSecondsRealtime(AutoOpenDelay);
        autoOpenRoutine = null;
        if (panel != null) panel.SetActive(false);
        if (eventLogPanel != null) eventLogPanel.Open();
    }

    private void Close()
    {
        if (autoOpenRoutine != null)
        {
            StopCoroutine(autoOpenRoutine);
            autoOpenRoutine = null;
        }
        ModalPause.Close(panel);
    }
}
