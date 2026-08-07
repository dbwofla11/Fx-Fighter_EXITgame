using System;
using System.Collections;
using TMPro;
using UnityEngine;

// 한 글자씩 출력하는 범용 타이핑 이펙트. 대사/자막 어디서든 재사용 가능.
// 타임스케일 0(일시정지)에서도 재생돼야 하는 화면(엔딩씬 등)을 위해 unscaledDeltaTime을 쓴다.
public class TypewriterText : MonoBehaviour
{
    public TextMeshProUGUI label;
    public float charInterval = 0.04f;

    public bool IsTyping { get; private set; }
    public event Action OnTypingComplete;

    private Coroutine routine;
    private string fullText;

    public void Play(string text)
    {
        if (routine != null) StopCoroutine(routine);
        fullText = text;
        routine = StartCoroutine(TypeRoutine());
    }

    // 타이핑 도중 클릭 등으로 스킵할 때 : 전체 텍스트를 즉시 채우고 완료 처리한다.
    public void CompleteImmediately()
    {
        if (!IsTyping) return;

        StopCoroutine(routine);
        label.text = fullText;
        IsTyping = false;
        OnTypingComplete?.Invoke();
    }

    private IEnumerator TypeRoutine()
    {
        IsTyping = true;
        label.text = "";

        foreach (char c in fullText)
        {
            label.text += c;
            yield return new WaitForSecondsRealtime(charInterval);
        }

        IsTyping = false;
        OnTypingComplete?.Invoke();
    }
}
