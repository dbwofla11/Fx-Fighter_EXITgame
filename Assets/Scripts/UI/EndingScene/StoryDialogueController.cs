using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StoryBeat
{
    public Sprite background;
    [TextArea] public string line;
}

// 엑시트/영웅 엔딩의 스토리 비트(배경+대사)를 순서대로 넘겨준다. 타이핑 자체는 TypewriterText가 전담하고,
// 여기서는 진행 순서와 "계속" 입력만 관리한다. 마지막 비트 다음엔 OnSequenceComplete를 발행해
// ExitEndingSceneUI가 통계 요약 화면으로 넘어가게 한다.
public class StoryDialogueController : MonoBehaviour
{
    public Image bgImage;
    public TypewriterText typewriter;
    public Button continueButton;
    public List<StoryBeat> beats;

    public event Action OnSequenceComplete;

    private int index = -1;

    private void OnEnable()
    {
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void OnDisable()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    public void Begin()
    {
        index = -1;
        ShowNext();
    }

    private void OnContinueClicked()
    {
        if (typewriter.IsTyping)
        {
            typewriter.CompleteImmediately();
            return;
        }

        ShowNext();
    }

    private void ShowNext()
    {
        index++;

        if (index >= beats.Count)
        {
            OnSequenceComplete?.Invoke();
            return;
        }

        StoryBeat beat = beats[index];
        if (bgImage != null) bgImage.sprite = beat.background;
        typewriter.Play(beat.line);
    }
}
