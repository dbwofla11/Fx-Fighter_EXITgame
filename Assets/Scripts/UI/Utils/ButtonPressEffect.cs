using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 버튼을 누르는 순간 살짝 축소됐다가 떼면 원래 크기로 돌아오는 눌림 효과. 아무 버튼에나 붙여서 쓸 수 있다.
public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressedScale = 0.85f;

    // 눌림 축소 보정분 외에 추가로 히트박스를 넓히는 여유분(픽셀). 버튼이 작아서 여전히 잘 안 눌리면 늘리기.
    public float extraHitboxPadding = 10f;

    // 버튼 밑에 깔린 입체 음영처럼, 버튼의 자식이 아니라 형제로 따로 배치된(그래야 버튼 뒤에 깔림) 오브젝트가
    // 있으면 여기 연결해서 버튼과 같이 눌리게 한다. 없으면 그냥 비워둔다(PauseBtn/SpeedBtn/PlayBtn처럼).
    public Transform linkedShadow;

    private Vector3 originalScale;
    private Vector3 originalShadowScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (linkedShadow != null) originalShadowScale = linkedShadow.localScale;

        // localScale로 축소하면 클릭 판정용 히트박스(RectTransform)도 같이 줄어든다. 누르는 순간 축소된
        // 상태로 떼면, 원래 크기 기준 가장자리를 클릭한 경우 판정 범위를 벗어나 클릭이 무시되는 문제가
        // 있었음 — raycastPadding을 축소 비율만큼 음수로 미리 넣고, 거기에 extraHitboxPadding만큼 더 넓혀
        // 히트박스가 화면에 보이는 크기보다 여유 있게 크도록 한다.
        var graphic = GetComponent<Graphic>();
        if (graphic != null)
        {
            Rect rect = GetComponent<RectTransform>().rect;
            float padX = -(rect.width * (1f - pressedScale) / 2f + extraHitboxPadding);
            float padY = -(rect.height * (1f - pressedScale) / 2f + extraHitboxPadding);
            graphic.raycastPadding = new Vector4(padX, padY, padX, padY);
        }
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
