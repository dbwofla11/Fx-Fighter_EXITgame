using UnityEngine;
using TMPro;

// 스탯 패널에 마우스를 올리면 뜨는 설명 툴팁. HoverTooltipTrigger가 호출하는 공용 싱글턴.
public class StatTooltipUI : MonoBehaviour
{
    public static StatTooltipUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI text;

    private RectTransform rect;
    private CanvasGroup group;

    // 시작부터 비활성 오브젝트로 두면 Awake가 안 돌아 Instance가 안 잡히므로, 루트는 항상 켜둔 채
    // CanvasGroup.alpha로만 보이기/숨기기를 제어한다.
    private void Awake()
    {
        Instance = this;
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(string message, RectTransform anchor)
    {
        text.text = message;
        rect.position = anchor.position + new Vector3(0f, anchor.rect.height / 2f + rect.rect.height / 2f + 8f, 0f);
        group.alpha = 1f;
        rect.SetAsLastSibling();
    }

    public void Hide()
    {
        group.alpha = 0f;
    }
}
