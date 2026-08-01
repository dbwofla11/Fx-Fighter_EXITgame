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

    public void Populate(Color backgroundColor, string title, string date, string effects)
    {
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
