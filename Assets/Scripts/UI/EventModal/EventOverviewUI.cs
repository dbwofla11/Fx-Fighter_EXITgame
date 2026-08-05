using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "개요" 탭 콘텐츠(스탯개요 + 엑시트 버튼). Figma EjUw2LdqxAYhL2180OAXHo node 1253:2 ("뉴스,이벤트 페이지 -
// 스탯개요") 기준. EventLogPanel/ContentArea 밑에 씬 오브젝트로 배치돼 있고, 이 스크립트는 TMP/Button 참조와
// 갱신 로직만 담당한다(TradeModalUI/CoinControlModalUI와 동일한 관례). EventLogPanelUI.ShowOverview()가
// 이 오브젝트를 켤 때마다 Refresh()를 호출한다(EventPanelBox의 RefreshLog()와 동일한 패턴 — 게임이 일시정지된
// 상태로 열려있는 동안은 매 턴 갱신될 필요가 없어 이벤트 구독 대신 진입 시점 1회 갱신만 한다).
public class EventOverviewUI : MonoBehaviour
{
    [Header("좌측 : 현재 스탯")]
    public TextMeshProUGUI supportText;
    public TextMeshProUGUI growthText;
    public TextMeshProUGUI doubtText;

    [Header("좌측 : Job+Skill 보너스")]
    public TextMeshProUGUI supportBonusText;
    public TextMeshProUGUI growthBonusText;
    public TextMeshProUGUI doubtBonusText;

    [Header("좌측 : 이벤트 확률 / 현금 증가량")]
    public TextMeshProUGUI positiveRateText;
    public TextMeshProUGUI negativeRateText;
    public TextMeshProUGUI cashBonusText;

    [Header("우측 : 목표 금액 / 엑시트")]
    public TextMeshProUGUI targetValueText;
    public TextMeshProUGUI currentValueText;
    public TextMeshProUGUI remainingValueText;
    public Button exitButton;

    // Figma상 지지도/상승도(빨강, 현재값)와 Job+Skill 상승률(초록, 보너스)이 부호와 무관하게 고정 색으로
    // 표시돼 있어(예시 수치가 우연히 음수/양수였던 게 아니라 카테고리별 고정 색) 그대로 따랐다.
    private const string CurrentValueColor = "#FF0900";
    private const string BonusValueColor = "#00FF00";

    private void Start()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(() => EventHub.RaiseExitRequested());
    }

    public void Refresh()
    {
        if (MarketManager.Instance == null || PlayerManager.Instance == null)
            return;

        PlayerStat stat = MarketManager.Instance.CurrentStat;

        if (supportText != null) supportText.text = $"코인 지지도 <color={CurrentValueColor}>{UIFormat.Signed(stat.Support)}</color>";
        if (growthText != null) growthText.text = $"코인 상승도 <color={CurrentValueColor}>{UIFormat.Signed(stat.Growth)}</color>";
        if (doubtText != null) doubtText.text = $"의심도 <color={CurrentValueColor}>{UIFormat.Signed(stat.Doubt)}</color>";

        if (supportBonusText != null) supportBonusText.text = $"코인 지지도 상승률 <color={BonusValueColor}>{UIFormat.Signed(stat.JobSkillSupportBonus)}</color>";
        if (growthBonusText != null) growthBonusText.text = $"코인 상승도 상승률 <color={BonusValueColor}>{UIFormat.Signed(stat.JobSkillGrowthBonus)}</color>";
        if (doubtBonusText != null) doubtBonusText.text = $"의심도 상승률 <color={BonusValueColor}>{UIFormat.Signed(stat.JobSkillDoubtBonus)}</color>";

        // PositiveEventRate/NegativeEventRate는 기본 50%에 더해지는 보너스분만 담고 있어 그대로 표시하면
        // 항상 0%로 보인다 — EventCalculator.PositiveEventChancePercent로 실제 판정 확률을 구해 표시한다.
        float positivePercent = EventCalculator.PositiveEventChancePercent(stat);
        if (positiveRateText != null) positiveRateText.text = $"긍정 이벤트 확률 {UIFormat.Percent(positivePercent)}";
        if (negativeRateText != null) negativeRateText.text = $"부정 이벤트 확률 {UIFormat.Percent(100f - positivePercent)}";
        if (cashBonusText != null) cashBonusText.text = $"현금 증가량 {UIFormat.SignedPercent(stat.CashBonus)}";

        long targetAsset = MarketManager.TargetAsset;
        long currentMoney = PlayerManager.Instance.currentMoney;
        long remaining = System.Math.Max(0L, targetAsset - currentMoney);

        if (targetValueText != null) targetValueText.text = UIFormat.Currency(targetAsset);
        if (currentValueText != null) currentValueText.text = UIFormat.Currency(currentMoney);
        if (remainingValueText != null) remainingValueText.text = UIFormat.Currency(remaining);

        if (exitButton != null) exitButton.interactable = MarketManager.Instance.CanExit;
    }
}
