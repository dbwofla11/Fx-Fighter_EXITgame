using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Event",
    menuName = "Game/EventSO"
)]
public class EventSO : ScriptableObject
{
    [Header("Info")]
    [TextArea]
    public string message; // 이벤트 로그에 표시될 문구 (예: "유명 스트리머가 코인을 소개했습니다!")

    [Header("Category")]
    public EventCategory category; // 긍정/부정 (선택 확률 편향, UI 색상 구분에 사용)

    [Header("Weight")]
    [Tooltip("같은 카테고리 안에서 가중치 랜덤 선택에 사용된다. 값이 클수록 자주 뽑힌다.")]
    public float weight = 1f;

    [Header("Effect")]
    [Tooltip("Support/Growth/Doubt 등. Job/Skill과 동일한 EffectData를 사용하며, 이벤트마다 어떤 스탯을 얼마나 바꿀지 자유롭게 지정한다.")]
    public List<EffectData> effects;

    [Header("Market")]
    [Tooltip("Supply 변화량 (부호 포함). Job/Skill은 Supply에 영향을 주지 않으므로 effects에는 포함하지 않는다.")]
    public float supplyDelta;

    [Tooltip("가격에 즉시 반영되는 변화율 (부호 포함, 예: 0.03 = +3%, -0.06 = -6%).")]
    public float priceRatio;
}
