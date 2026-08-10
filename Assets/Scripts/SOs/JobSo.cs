using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New Job",
    menuName = "Game/JobSO"
)]
public class JobSO : ScriptableObject
{
    public string jobName;

    public Sprite icon;

    [TextArea]
    public string description;

    public long startingMoney = 10000; // 직업별 시작 자금
    public long startingCoins = 10000; // 직업별 시작 코인 수량

    public List<EffectData> effects;

    [Header("오프닝 스토리 (비트별 배경+대사, StoryDialogueController가 재생)")]
    public List<StoryBeat> openingBeats;
}