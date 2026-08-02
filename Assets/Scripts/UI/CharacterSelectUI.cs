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

    private int selectedIndex;

    private void Start()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => SelectJob(index));
        }
        nextButton.onClick.AddListener(ConfirmSelection);

        SelectJob(0);
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
        JobSelectionHandoff.SelectedJob = jobs[selectedIndex];
        GameSceneManager.LoadMainGame();
    }
}
