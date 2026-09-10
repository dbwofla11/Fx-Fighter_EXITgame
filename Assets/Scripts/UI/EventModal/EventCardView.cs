using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 이벤트 로그 카드 1장의 뷰. EventLogPanel/ContentArea/OverviewBox 밑의 "EventCardTemplate"에 붙어 있고,
// EventLogPanelUI가 이 오브젝트를 Instantiate로 복제해 목록을 채운다(TradeModalUI가 BtnPlus1을 복제해
// "+/-" 버튼을 만든 것과 동일한 방식). 텍스트/색상 계산(EffectType 라벨링 등)은 EventLogPanelUI가 맡고,
// 이 스크립트는 만들어진 문자열/색을 화면에 꽂아 넣기만 한다.
public class EventCardView : MonoBehaviour
{
    public Image background;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI effectsText;

    private Color? titleDefaultColor;
    private Color? dateDefaultColor;
    private Color? effectsDefaultColor;
    private Outline interestBorder;

    public void Populate(EventLogEntry entry, bool compactChoiceResult = false)
    {
        bool interest = entry.CashInterest.HasValue;
        string effects = interest
            ? $"보유 현금: ₩ {entry.InterestPrincipal:N0}\n고정 연 이율 0.1% ÷ 12개월\n이자 지급: +₩ {entry.CashInterest.Value:N0}\n1원 미만 이자는 다음 달로 이월됩니다."
            : compactChoiceResult ? EventEffectFormatter.BuildChoiceResultText(entry)
            : EventEffectFormatter.BuildEntryEffectsText(entry);
        Color color = interest ? Color.white : entry.HasChoiceResult
            ? (entry.Succeeded ? EventEffectFormatter.PositiveColor : EventEffectFormatter.NegativeColor)
            : EventEffectFormatter.CategoryColor(entry.Profile.category);
        Populate(color, interest ? "월간 현금 이자 지급" : EventEffectFormatter.BuildEntryTitle(entry),
            UIFormat.DateDot(entry.Date), effects);

        if (interest && background != null && interestBorder == null)
        {
            interestBorder = background.gameObject.AddComponent<Outline>();
            interestBorder.effectColor = Color.black;
            interestBorder.effectDistance = new Vector2(1.5f, -1.5f);
        }
        if (interestBorder != null) interestBorder.enabled = interest;

        if (interest)
        {
            if (titleText != null) titleText.color = Color.black;
            if (dateText != null) dateText.color = Color.black;
            if (effectsText != null) effectsText.color = Color.black;
        }
    }

    public void Populate(Color backgroundColor, string title, string date, string effects)
    {
        if (interestBorder != null) interestBorder.enabled = false;
        if (titleText != null)
        {
            titleDefaultColor ??= titleText.color;
            titleText.color = titleDefaultColor.Value;
        }
        if (dateText != null)
        {
            dateDefaultColor ??= dateText.color;
            dateText.color = dateDefaultColor.Value;
        }
        if (effectsText != null)
        {
            effectsDefaultColor ??= effectsText.color;
            effectsText.color = effectsDefaultColor.Value;
        }

        if (background != null) background.color = backgroundColor;
        if (titleText != null) titleText.text = title;
        if (dateText != null) dateText.text = date;

        if (effectsText != null)
        {
            effectsText.text = effects;
            effectsText.gameObject.SetActive(!string.IsNullOrEmpty(effects));
        }
    }
}
