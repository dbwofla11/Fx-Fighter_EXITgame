using System.Collections.Generic;
using UnityEngine;

// 엔딩(체포/거지/엑시트/영웅)의 스토리 비트(배경+대사) 시퀀스를 담는 재사용 가능한 에셋.
// StoryDialogueController가 순서대로 재생하거나, EndingSceneUI처럼 비트 하나만 꺼내 쓸 수도 있다.
[CreateAssetMenu(
    fileName = "New Story Beat Set",
    menuName = "Game/StoryBeatSetSO"
)]
public class StoryBeatSetSO : ScriptableObject
{
    public List<StoryBeat> beats;
}
