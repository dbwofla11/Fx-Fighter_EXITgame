using System.Collections;
using TMPro;
using UnityEngine;

// Main_Canvas 직속 스트리머 말풍선(EventNotification 바로 뒤 sibling — TradeModal 등 팝업보다는 아래,
// ChartPanel보다는 위). Show()/Hide()로 외부에서 제어하는 순수 UI 컨테이너 — 언제/무슨 말을 보여줄지는
// StreamerPanelUI + StreamerLines가 결정해서 넘겨준다.
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
