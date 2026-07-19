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

    public List<EffectData> effects;
}