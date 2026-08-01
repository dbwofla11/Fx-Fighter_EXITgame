using UnityEngine;
using UnityEngine.UI;

// 코인 가격 헤더 우측의 뉴스(이벤트 로그) 아이콘. 클릭하면 EventLogPanelUI를 토글한다.
// 패널이 자체 X 닫기 버튼으로도 닫힐 수 있어서, 아이콘 스프라이트는 클릭 시점이 아니라 매 프레임
// 패널의 실제 활성 상태(IsOpen)를 그대로 반영한다(다른 화면들의 기존 폴링 방식과 동일).
public class EventLogButton : MonoBehaviour
{
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private EventLogPanelUI eventLogPanel;

    private Image icon;

    private void Awake()
    {
        icon = GetComponent<Image>();
        icon.sprite = offSprite;
    }

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void Update()
    {
        icon.sprite = eventLogPanel.IsOpen ? onSprite : offSprite;
    }

    private void Toggle()
    {
        eventLogPanel.Toggle();
    }
}
