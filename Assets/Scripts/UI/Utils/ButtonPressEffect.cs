using UnityEngine;
using UnityEngine.EventSystems;

// 버튼을 누르는 순간 살짝 축소됐다가 떼면 원래 크기로 돌아오는 눌림 효과. 아무 버튼에나 붙여서 쓸 수 있다.
public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressedScale = 0.85f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}
