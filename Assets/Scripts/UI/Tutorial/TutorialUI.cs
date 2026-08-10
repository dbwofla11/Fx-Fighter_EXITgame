using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    [Header("Tutorial UI Elements")]
    public GameObject tutorialPanel; // 화면 전체를 덮는 반투명 검은 패널 (Dim)
    public GameObject[] speechBubbles; // 튜토리얼 말풍선들을 순서대로 넣을 배열

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
    }

    private void CloseTutorial()
    {
        ModalPause.Close(tutorialPanel);
    }
}
