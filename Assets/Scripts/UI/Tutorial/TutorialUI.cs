using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    [Header("Tutorial UI Elements")]
    public GameObject tutorialPanel; // 화면 전체를 덮는 반투명 검은 패널 (Dim)
    public GameObject[] speechBubbles; // 튜토리얼 말풍선들을 순서대로 넣을 배열

    [Header("EXIT 버튼 소개 스텝")]
    public EventLogPanelUI eventLogPanelUI;
    private const int ExitButtonStepIndex = 1; // speechBubbles[1] = SpeechBubbleExit

    private int currentBubbleIndex = 0;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClickNext);
        ShowTutorial();
    }

    public void ShowTutorial()
    {
        ModalPause.Open(tutorialPanel);
        UpdateBubbles();
    }

    // 화면(패널)을 클릭했을 때 다음 말풍선으로 넘어가는 함수
    public void OnClickNext()
    {
        // EXIT 버튼 소개 스텝을 벗어날 때 EventLogPanel을 닫는다. Close()가 ModalPause.Close ->
        // TimeManager.ResumeGame()을 부르는데, isManuallyPaused가 false라 튜토리얼이 아직 떠있어도
        // 시간이 그대로 재개돼버린다 — 다시 멈춰서 튜토리얼 정지 상태를 유지한다.
        if (currentBubbleIndex == ExitButtonStepIndex && eventLogPanelUI != null)
        {
            eventLogPanelUI.Close();
            EventHub.RaiseGamePaused();
        }

        currentBubbleIndex++;

        // 준비된 말풍선을 다 봤다면 튜토리얼 종료
        if (currentBubbleIndex >= speechBubbles.Length)
        {
            CloseTutorial();
        }
        else
        {
            UpdateBubbles();
        }
    }

    private void UpdateBubbles()
    {
        // 모든 말풍선을 끄고, 현재 순서(Index)의 말풍선만 켭니다.
        for (int i = 0; i < speechBubbles.Length; i++)
        {
            speechBubbles[i].SetActive(i == currentBubbleIndex);
        }

        // 실제 EXIT 버튼을 보여주기 위해 EventLogPanel을 개요 탭으로 연다.
        if (currentBubbleIndex == ExitButtonStepIndex && eventLogPanelUI != null)
        {
            eventLogPanelUI.Open();
        }
    }

    private void CloseTutorial()
    {
        ModalPause.Close(tutorialPanel);
    }
}
