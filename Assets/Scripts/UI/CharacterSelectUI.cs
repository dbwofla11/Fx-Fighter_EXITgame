using UnityEngine;
using UnityEngine.UI;
using TMPro;

// "초반 캐릭터 선택" 씬(Figma node 1202:186) 전용. 직업 슬롯 클릭 시 우측 상세 패널을 갱신하고,
// "다음으로" 클릭 시 선택한 JobSO를 JobSelectionHandoff에 담아 메인 게임 씬으로 넘긴다.
public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private JobSO[] jobs;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private Image[] slotImages;
    [SerializeField] private GameObject[] slotShadows;
    [SerializeField] private Color selectedColor = new Color(1f, 0.631f, 0.404f); // Figma #ffa167
    [SerializeField] private Color unselectedColor = new Color(1f, 0.796f, 0.616f); // Figma #ffcb9d

    [Header("Detail Panel")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject[] traitRows;
    [SerializeField] private Image[] traitIcons;
    [SerializeField] private TextMeshProUGUI[] traitTexts;

    [Header("Trait Icons")]
    [SerializeField] private Sprite supportIcon;
    [SerializeField] private Sprite growthIcon;
    [SerializeField] private Sprite doubtIcon;
    [SerializeField] private Sprite cashIcon;

    [SerializeField] private Button nextButton;
    [SerializeField] private AudioClip clickSfx; // 일반버튼소리

    private int selectedIndex;

    private void Start()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => { PlayClickSfx(); SelectJob(index); });
        }
        nextButton.onClick.AddListener(() => { PlayClickSfx(); ConfirmSelection(); });

        SelectJob(0);
    }

    private void PlayClickSfx()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clickSfx);
    }

    private void SelectJob(int index)
    {
        selectedIndex = index;

        for (int i = 0; i < slotImages.Length; i++)
        {
            bool isSelected = i == index;
            slotImages[i].color = isSelected ? selectedColor : unselectedColor;
            // 선택된 슬롯은 눌린 상태로 보이도록 그림자를 숨긴다 (Figma 목업의 selected 슬롯엔 테두리/그림자가 없음).
            slotShadows[i].SetActive(!isSelected);
        }

        RefreshDetail(jobs[index]);
    }

    private void RefreshDetail(JobSO job)
    {
        nameText.text = job.jobName;
        descriptionText.text = job.description;

        for (int i = 0; i < traitRows.Length; i++)
        {
            bool hasEffect = i < job.effects.Count;
            traitRows[i].SetActive(hasEffect);
            if (!hasEffect)
                continue;

            var effect = job.effects[i];
            traitIcons[i].sprite = EffectIcon(effect.effectType);
            traitTexts[i].text = $"{EffectLabel(effect.effectType)} {UIFormat.SignedPercent(UIFormat.SignedEffectValue(effect))}";
        }
    }

    private Sprite EffectIcon(EffectType type) => type switch
    {
        EffectType.GrowthIncrease => growthIcon,
        EffectType.DoubtDecrease => doubtIcon,
        EffectType.CashBonus => cashIcon,
        _ => supportIcon,
    };

    private string EffectLabel(EffectType type) => type switch
    {
        EffectType.SupportIncrease => "지지도 증가",
        EffectType.GrowthIncrease => "상승도 증가",
        EffectType.DoubtDecrease => "의심도 감소",
        EffectType.CashBonus => "거래 수익 증가",
        _ => type.ToString(),
    };

    private void ConfirmSelection()
    {
        // Managers는 SampleScene에만 있어서 최초 실행(1회차)엔 아직 안 만들어져 있다 — 그땐 각 Manager의
        // Awake가 알아서 초기값을 세팅하므로 리셋이 필요 없다. "게임종료"로 타이틀에 돌아왔다가 다시
        // 시작하는 2회차부터는 DontDestroyOnLoad로 남아있는 이전 판 데이터를 여기서 명시적으로 지워야 한다.
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ResetState();
            MarketManager.Instance.ResetState();
            TimeManager.Instance.ResetState();
            SkillManager.Instance.ResetState();

            // JobManager도 DontDestroyOnLoad라 Awake/Start가 다시 안 불린다 — 아래 핸드오프(JobSelectionHandoff)는
            // JobManager.Start()가 소비하는데, 그 Start()는 최초 1회만 실행되므로 2회차부터는 직업이 반영되지
            // 않는 버그가 있었다. 여기서 직접 선택해 반영한다 (1회차는 Managers가 아직 없어 이 분기를 안 타므로
            // 계속 핸드오프+JobManager.Start()에 맡긴다).
            JobManager.Instance.SelectJob(jobs[selectedIndex]);
        }

        JobSelectionHandoff.SelectedJob = jobs[selectedIndex];
        GameSceneManager.LoadMainGame();
    }
}
