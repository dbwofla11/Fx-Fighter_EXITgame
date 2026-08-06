using UnityEngine;
using UnityEngine.UI;

// 메인 화면의 스킬 아이콘. 클릭하면 SkillPanelUI를 토글하고, EventLogButton과 동일하게 패널의 실제 활성
// 상태를 매 프레임 폴링해서 아이콘 스프라이트(꺼짐/켜짐)에 반영한다.
public class SkillPanelButton : MonoBehaviour
{
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private SkillPanelUI skillPanel;
    [SerializeField] private AudioClip openSfx; // 스킬버튼구매소리

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
        icon.sprite = skillPanel.IsOpen ? onSprite : offSprite;
    }

    private void Toggle()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(openSfx);
        skillPanel.Toggle();
    }
}
