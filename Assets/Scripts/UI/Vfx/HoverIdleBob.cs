using UnityEngine;
using UnityEngine.EventSystems;

// 마우스가 올라가 있는 동안에만 공중에 뜬 것처럼 살짝살짝 위아래로 흔들리는 idle 애니메이션.
// AddComponent로 아무 UI 오브젝트에나 붙여 쓴다(PlayerUI의 btnLong/btnShort).
// 모달이 열려 Time.timeScale=0인 동안에도 보일 수 있으니 unscaled time을 쓴다.
public class HoverIdleBob : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // ponytail: 밸런스용 임시 수치, 플레이 후 조정 필요.
    [SerializeField] private float amplitude = 5f;
    [SerializeField] private float speed = 3f;

    private RectTransform rect;
    private Vector2 basePos;
    private bool hovering;

    private void Awake()
    {
        rect = (RectTransform)transform;
        basePos = rect.anchoredPosition;
    }

    private void Update()
    {
        if (!hovering)
            return;

        float offsetY = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
        rect.anchoredPosition = basePos + new Vector2(0f, offsetY);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovering = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        rect.anchoredPosition = basePos;
    }
}
