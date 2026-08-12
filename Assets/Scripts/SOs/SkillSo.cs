using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Skill",
    menuName = "Game/SkillSO"
)]
public class SkillSO : ScriptableObject
{
    [Header("Info")]
    public SkillID id; // skill아이디는 Enum으로 관리 

    [TextArea]
    public string description;

    public Sprite icon;

    [Header("Category")]
    public SkillCategory category;

    [Header("Price")]
    public int baseCost;

    public float costMultiplier = 1.15f;

    // 재사용형 스킬 1개당 구매 가능한 최대 횟수. 대부분 10(SkillManager 기존 전역값과 동일),
    // 가격 즉시변동(PriceShockPercent) 스킬처럼 더 강하게 제한해야 하는 경우만 개별로 낮춘다.
    public int maxPurchaseCount = 10;

    [Header("State")]
    public bool defaultUnlocked;

    [Header("Reuse")]
    // true  : Support/Growth 부스트형. 클릭할 때마다 비용을 내고 즉시 CurrentStat에 반영된다.
    //         잠기지 않고 계속 재구매 가능하며, 비용은 구매 횟수에 따라 상승한다.
    // false : 영구 효과형(ExitUnlock 등). 구매=영구 활성(SkillManager.EnableSkill), 끄는 UI 없음, 감쇠 없음
    public bool isReusable = true;

    [Header("Effect")]
    public List<EffectData> effects;
}