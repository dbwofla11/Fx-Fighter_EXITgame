using UnityEngine;
using UnityEngine.UI;

// 코인 가격 헤더 우측의 뉴스(이벤트 로그) 아이콘. 클릭할 때마다 on/off 스프라이트를 토글한다.
// 이벤트 로그 패널 자체는 아직 씬에 없어서 패널을 열고 닫는 연결은 비워뒀다 (Next_Tesk.md "이벤트 로그 패널" 참고).
public class EventLogButton : MonoBehaviour
{
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    private Image icon;
    private bool isOn;

    private void Awake()
    {
        icon = GetComponent<Image>();
        icon.sprite = offSprite;
    }

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void Toggle()
    {
        isOn = !isOn;
        icon.sprite = isOn ? onSprite : offSprite;
    }
}
