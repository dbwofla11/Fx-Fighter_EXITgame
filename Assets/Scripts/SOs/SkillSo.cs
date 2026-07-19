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

    [Header("Effect")]
    public List<EffectData> effects;
}