using UnityEngine;
using UnityEngine.EventSystems;

// 버튼을 누르는 순간 살짝 축소됐다가 떼면 원래 크기로 돌아오는 눌림 효과. 아무 버튼에나 붙여서 쓸 수 있다.
public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressedScale = 0.85f;

    // 버튼 밑에 깔린 입체 음영처럼, 버튼의 자식이 아니라 형제로 따로 배치된(그래야 버튼 뒤에 깔림) 오브젝트가
    // 있으면 여기 연결해서 버튼과 같이 눌리게 한다. 없으면 그냥 비워둔다(PauseBtn/SpeedBtn/PlayBtn처럼).
    public Transform linkedShadow;

    private Vector3 originalScale;
    private Vector3 originalShadowScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (linkedShadow != null) originalShadowScale = linkedShadow.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * pressedScale;
        if (linkedShadow != null) linkedShadow.localScale = originalShadowScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;
        if (linkedShadow != null) linkedShadow.localScale = originalShadowScale;
    }
}
