using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 오프닝씬(OpeningScene) 전용. 직업선택 다음, 메인 게임 진입 전에 재생되는 도입부 스토리.
// StoryDialogueController로 비트를 순서대로 재생한 뒤 메인 게임 씬으로 넘어간다.
// 암전 인트로 연출은 ExitEndingSceneUI와 같은 패턴(코루틴 + unscaledDeltaTime)을 재사용한다.
public class OpeningSceneUI : MonoBehaviour
{
    [Header("스토리")]
    public StoryDialogueController storyController;

    [Header("암전 (인트로 연출)")]
    public CanvasGroup blackOverlay;
    public float blackDelay = 1f;
    public float blackFadeDuration = 1f;

    [Header("사운드 (없으면 재생하지 않는다)")]
    public AudioClip openingBgmClip;

    [Header("오프닝 스킵")]
    [SerializeField] private string skipButtonLabel = "스킵  ▶";
    [SerializeField] private int skipButtonFontSize = 26;

    private Button skipButton;
    private bool isLeavingOpening;

    private void Start()
    {
        if (blackOverlay != null) blackOverlay.alpha = 0f;
        CreateSkipButton();

        if (AudioManager.Instance != null)
        {
            if (openingBgmClip != null) AudioManager.Instance.PlayBGM(openingBgmClip);
            else AudioManager.Instance.StopBGM();
        }

        if (storyController != null)
        {
            storyController.OnSequenceComplete += OnStoryComplete;
            storyController.Begin();
        }

        StartCoroutine(PlaySequence());
    }

    private void OnDestroy()
    {
        if (storyController != null)
            storyController.OnSequenceComplete -= OnStoryComplete;

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipOpening);
    }

    private IEnumerator PlaySequence()
    {
        yield return new WaitForSecondsRealtime(blackDelay);
        yield return FadeCanvasGroup(blackOverlay, 1f, blackFadeDuration);
        yield return FadeCanvasGroup(blackOverlay, 0f, blackFadeDuration);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
    {
        if (group == null) yield break;

        float startAlpha = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = targetAlpha;
    }

    private void OnStoryComplete()
    {
        LeaveOpening();
    }

    // 버튼에서 직접 호출할 수 있도록 public으로 둔다. 메인 게임 씬의 TutorialUI.Start()가 튜토리얼을 자동으로 연다.
    public void SkipOpening()
    {
        LeaveOpening();
    }

    private void LeaveOpening()
    {
        if (isLeavingOpening)
            return;

        isLeavingOpening = true;
        StopAllCoroutines(); // 암전 코루틴이 씬 전환 중 남지 않게 정리한다.
        GameSceneManager.LoadMainGame();
    }

    // PC/Android 오프닝 씬이 같은 스크립트를 쓰므로, 씬마다 수동 배치하지 않고 화면 우상단에 공통 버튼을 만든다.
    private void CreateSkipButton()
    {
        GameObject buttonObject = new GameObject("SkipOpeningButton", typeof(RectTransform),
            typeof(TextMeshProUGUI), typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-36f, -30f);
        rect.sizeDelta = new Vector2(150f, 48f);

        TextMeshProUGUI label = buttonObject.GetComponent<TextMeshProUGUI>();
        label.text = skipButtonLabel;
        label.fontSize = skipButtonFontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 1f, 1f, 0.85f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = true;

        // 오프닝 대사와 같은 폰트를 물려받아 한글이 비거나 플랫폼별 기본 폰트가 달라지는 일을 막는다.
        if (storyController != null && storyController.typewriter != null && storyController.typewriter.label != null)
            label.font = storyController.typewriter.label.font;

        skipButton = buttonObject.GetComponent<Button>();
        skipButton.targetGraphic = label;
        skipButton.onClick.AddListener(SkipOpening);
    }
}
