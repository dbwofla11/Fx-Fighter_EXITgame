using UnityEngine;
using UnityEngine.UI;

// "코인 발행" 트리거 버튼. 추가발행권한 스킬을 구매하기 전에는 잠금 표시한다.
// GameObject 자체는 계속 켜두고(끄면 이벤트 구독도 같이 죽어서 나중에 스킬을 사도 다시 안 나타남),
// CanvasGroup으로 보이기/누르기만 막는다.
public class MintButtonUI : MonoBehaviour
{
    public Button btnMint;
    public CanvasGroup canvasGroup;
    public CoinControlModalUI coinControlModal;
    [SerializeField] private AudioClip clickSfx; // 일반버튼소리

    private void OnEnable()
    {
        EventHub.OnSkillPurchased += RefreshUnlockState;
        RefreshUnlockState();
    }

    private void OnDisable()
    {
        EventHub.OnSkillPurchased -= RefreshUnlockState;
    }

    private void Start()
    {
        btnMint.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
            coinControlModal.Open();
        });
    }

    private void RefreshUnlockState()
    {
        bool unlocked = SkillManager.Instance != null && SkillManager.Instance.IsUnlocked(SkillID.추가발행권한);

        canvasGroup.alpha = unlocked ? 1f : 0f;
        canvasGroup.interactable = unlocked;
        canvasGroup.blocksRaycasts = unlocked;
    }
}
