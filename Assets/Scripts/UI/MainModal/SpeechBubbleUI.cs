using System.Collections;
using TMPro;
using UnityEngine;

// RightPanel의 스트리머 말풍선. StreamerPanel 옆에 배치. Show()/Hide()로 외부에서 제어.
// 표시할 멘트 텍스트/트리거 로직은 별도 작업 범위 — 이 컴포넌트는 UI 컨테이너만 담당한다.
public class SpeechBubbleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text messageText;

    private Coroutine hideRoutine;

    public void Show(string speakerName, string message, float duration = 3f)
    {
        nameText.text = speakerName;
        messageText.text = message;
        gameObject.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfter(duration));
    }

    public void Hide()
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = null;
        gameObject.SetActive(false);
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        hideRoutine = null;
        gameObject.SetActive(false);
    }
}
