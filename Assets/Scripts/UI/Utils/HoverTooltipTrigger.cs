using UnityEngine;
using UnityEngine.EventSystems;

// 마우스를 올리면 StatTooltipUI에 설명 텍스트를 띄우는 범용 트리거. 지지도/상승률/발행량 패널에 붙여 쓴다.
public class HoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] [SerializeField] private string tooltipText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        StatTooltipUI.Instance.Show(tooltipText, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StatTooltipUI.Instance.Hide();
    }
}
