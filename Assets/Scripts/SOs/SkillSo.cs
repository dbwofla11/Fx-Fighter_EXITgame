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

    [Header("Skill Tree")]
    [Tooltip("이 목록의 모든 스킬을 먼저 구매해야 이 스킬을 구매할 수 있습니다.")]
    public List<SkillID> prerequisites = new List<SkillID>();

    [Tooltip("이 스킬이 속한 시장조작·여론조작 세부 카테고리 해금 키. None이면 별도 카테고리 해금 조건이 없습니다.")]
    public SkillUnlockGroup unlockGroup;

    // 기존 asset 호환용 필드. v0.4부터는 공통 단계 해금에 사용하지 않는다.
    [HideInInspector]
    public int unlockStage;

    [Header("Reuse")]
    // true  : Support/Growth 부스트형. 클릭할 때마다 비용을 내고 즉시 CurrentStat에 반영된다.
    //         잠기지 않고 계속 재구매 가능하며, 비용은 구매 횟수에 따라 상승한다.
    // false : 영구 효과형(ExitUnlock 등). 구매=영구 활성(SkillManager.EnableSkill), 끄는 UI 없음, 감쇠 없음
    public bool isReusable = true;

    [Header("Effect")]
    public List<EffectData> effects;
}
