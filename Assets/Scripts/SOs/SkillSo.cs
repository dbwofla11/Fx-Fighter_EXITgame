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

    [Header("State")]
    public bool defaultUnlocked;

    [Header("Reuse")]
    // true  : Support/Growth 부스트형. 클릭할 때마다 비용을 내고 즉시 CurrentStat에 반영된다.
    //         잠기지 않고 계속 재구매 가능하며, 비용은 구매 횟수에 따라 상승한다.
    // false : 영구 효과형(ExitUnlock 등). 기존 토글(EnableSkill/DisableSkill) 방식 유지, 감쇠 없음
    public bool isReusable = true;

    [Header("Effect")]
    public List<EffectData> effects;
}