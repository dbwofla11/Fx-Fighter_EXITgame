using UnityEngine;
using UnityEngine.UI;

// "코인 발행" 트리거 버튼. 추가발행권한 스킬을 구매하기 전에는 잠금 표시한다.
// SetActive(false)로 끄면 Update가 멈춰서 나중에 스킬을 사도 다시 안 나타나므로,
// CanvasGroup으로 보이기/누르기만 막는다.
public class MintButtonUI : MonoBehaviour
{
    public Button btnMint;
    public CanvasGroup canvasGroup;
    public CoinControlModalUI coinControlModal;

    private void Start()
    {
        btnMint.onClick.AddListener(() => coinControlModal.Open());
    }

    private void Update()
    {
        bool unlocked = SkillManager.Instance != null && SkillManager.Instance.IsUnlocked(SkillID.추가발행권한);

        canvasGroup.alpha = unlocked ? 1f : 0f;
        canvasGroup.interactable = unlocked;
        canvasGroup.blocksRaycasts = unlocked;
    }
}
