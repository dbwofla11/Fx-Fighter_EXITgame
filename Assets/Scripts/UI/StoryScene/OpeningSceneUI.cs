using System.Collections;
using UnityEngine;

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

    private void Start()
    {
        if (blackOverlay != null) blackOverlay.alpha = 0f;

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
        GameSceneManager.LoadMainGame();
    }
}
