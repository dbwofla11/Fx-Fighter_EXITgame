using System.Collections;
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
    [SerializeField] private AudioClip clickSfx; // 클릭소리

    // 목표금액 달성(MarketManager.CanExit, 현금 5억) 후 계속 표시되는 강조 효과 (목업 기준 : 테두리 상시 +
    // 파티클 주기 재생). 파티클 애셋이 프로젝트에 없어 VFX 1차 작업(Issue_VFXPhase1.md)과 동일하게
    // UIBurstParticle(코드 생성 uGUI 버스트)을 재사용한다. 테두리도 SkillPanelUI와 동일하게 Outline 재사용.
    [SerializeField] private Outline achievedOutline;
    [SerializeField] private Color achievedParticleColor = Color.yellow;
    [SerializeField] private float particleIntervalSeconds = 2f;

    // 폭죽처럼 버튼 주변 여러 지점에서 시차를 두고 터지도록 하는 설정 (UIBurstParticle 자체는 한 점에서만 방사).
    [SerializeField] private int fireworksBurstCount = 4;
    [SerializeField] private float fireworksSpreadRadius = 70f;
    [SerializeField] private float fireworksStaggerSeconds = 0.08f;

    private Image icon;
    private float particleTimer;

    private void Awake()
    {
        icon = GetComponent<Image>();
        icon.sprite = offSprite;
        if (achievedOutline != null) achievedOutline.enabled = false;
    }

    private void OnEnable()
    {
        EventHub.OnAssetGoalAchieved += HandleAssetGoalAchieved;
    }

    private void OnDisable()
    {
        EventHub.OnAssetGoalAchieved -= HandleAssetGoalAchieved;
    }

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    // 달성 순간(엣지) 즉시 폭죽 1세트 — 이후 주기 폭죽은 UpdateAchievedEffect가 이어서 담당.
    private void HandleAssetGoalAchieved()
    {
        particleTimer = 0f;
        StartCoroutine(FireworksBurst());
    }

    // 버튼 주변 랜덤 위치에 UIBurstParticle을 여러 번, 살짝 시차를 두고 스폰해 폭죽처럼 여기저기서 터지게 한다.
    // ModalPause로 timeScale=0인 동안(달성 알림 배너가 뜨는 순간)에도 재생돼야 하므로 unscaled time을 쓴다.
    private IEnumerator FireworksBurst()
    {
        RectTransform rect = (RectTransform)transform;

        for (int i = 0; i < fireworksBurstCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * fireworksSpreadRadius;
            UIBurstParticle.Spawn(rect, offset, achievedParticleColor, 0.6f);
            yield return new WaitForSecondsRealtime(fireworksStaggerSeconds);
        }
    }

    private void Update()
    {
        icon.sprite = eventLogPanel.IsOpen ? onSprite : offSprite;
        UpdateAchievedEffect();
    }

    private void UpdateAchievedEffect()
    {
        bool achieved = MarketManager.Instance != null && MarketManager.Instance.CanExit;
        if (achievedOutline != null) achievedOutline.enabled = achieved;

        if (!achieved)
        {
            particleTimer = 0f;
            return;
        }

        particleTimer += Time.deltaTime;
        if (particleTimer < particleIntervalSeconds)
            return;

        particleTimer = 0f;
        StartCoroutine(FireworksBurst());
    }

    private void Toggle()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
        eventLogPanel.Toggle();
    }
}
