using System;
using UnityEngine;

[System.Serializable]
public class EffectData
{
    public EffectType effectType;
    public float value;
}

public enum EffectType
{
    SupportIncrease,
    IncreaseScore,

    DoubtDecrease,

    PositiveEventRate,
    NegativeEventRate,

    CashBonus,

    SupplyDecrease,
    VolumeIncrease,

    ExitUnlock
}