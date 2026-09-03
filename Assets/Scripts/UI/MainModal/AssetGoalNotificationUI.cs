using UnityEngine;

// 현금이 목표 금액(엑시트 조건)에 처음 도달했을 때, 시간을 수동 정지하고 EventLogPanel의 개요 탭으로
// 즉시 이동시킨다. 개요 탭에는 활성화된 엑시트 버튼이 있으므로 달성 직후 다음 행동이 분명해진다.
public class AssetGoalNotificationUI : MonoBehaviour
{
    // 이전 목표 달성 배너. 씬 참조 호환성을 위해 남겨두되, 이제 즉시 EventLogPanel로 이동하므로 표시하지 않는다.
    [SerializeField] private GameObject panel;
    [SerializeField] private EventLogPanelUI eventLogPanel;
    [SerializeField] private AudioClip openSfx;

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
        if (panel != null) panel.SetActive(false);
    }

    private void HandleAssetGoalAchieved()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(openSfx);

        // ModalPause.Open만 쓰면 패널을 닫았을 때 시간이 다시 흐를 수 있다. 목표 달성 뒤에는 플레이어가
        // 엑시트 여부를 확인할 때까지 멈춰 있어야 하므로 수동 정지를 먼저 건다.
        if (TimeManager.Instance != null)
            TimeManager.Instance.PauseGame();

        if (panel != null) panel.SetActive(false);
        if (eventLogPanel != null) eventLogPanel.Open();
    }
}
