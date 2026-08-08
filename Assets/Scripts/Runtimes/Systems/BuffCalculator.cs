/// <summary>
/// N턴에 걸쳐 유지/진행되는 버프(Volume 거래량 버프, DoubtDecline 의심도 하락)를 관리한다.
/// StatCalculator.Calculate()가 매 턴 Tick*을, ApplySkillUse/ApplyEffect가 Start*를 호출한다.
/// </summary>
public static class BuffCalculator
{
    // Volume이 스킬/이벤트로 오른 뒤 자동으로 0으로 리셋되기까지 유지되는 턴 수 (버프 지속시간).
    private const int VolumeBuffDurationTurns = 30;

    // 의심도 하락 총량을 나눠 차감하는 턴 수(=0.5%씩 200턴)와 턴당 차감 비율.
    private const int DoubtDeclineDurationTurns = 200;
    private const float DoubtDeclineRatioPerTurn = 0.005f;

    /// <summary>Volume은 Support/Growth처럼 감쇠하며 이어지는 게 아니라, VolumeBuffTurnsRemaining이 0이
    /// 될 때까지만 "버프"로 유지되다가 만료되면 다음 턴부터 0으로 완전히 리셋된다(PlayerStat.Reset()이
    /// 이미 0으로 잡아둠).</summary>
    public static void TickVolumeBuff(PlayerStat stat, PlayerStat previous)
    {
        if (previous.VolumeBuffTurnsRemaining <= 0)
            return;

        stat.Volume = previous.Volume;
        stat.VolumeBuffTurnsRemaining = previous.VolumeBuffTurnsRemaining - 1;
    }

    /// <summary>N턴짜리 Volume 버프를 시작(또는 재사용 시 갱신)한다 — 값은 쌓이고 지속시간은 새로 갱신된다.</summary>
    public static void StartVolumeBuff(PlayerStat stat, float delta)
    {
        stat.Volume += delta;
        stat.VolumeBuffTurnsRemaining = VolumeBuffDurationTurns;
    }

    /// <summary>진행 중인 의심도 하락 전부를 각각 독립적으로 틱한다 — 같은 스킬을 여러 번 사서 항목이 여러
    /// 개 쌓여있어도(중복 적용) 각자의 PerTurn이 모두 Doubt에서 차감된다(합산). 만료된(TurnsRemaining이
    /// 0이 된) 항목은 새 stat으로 옮기지 않아 자연히 사라진다.</summary>
    public static void TickDoubtDeclines(PlayerStat stat, PlayerStat previous)
    {
        foreach (DoubtDeclineBuff entry in previous.DoubtDeclines)
        {
            if (entry.TurnsRemaining <= 0)
                continue;

            // 스킬로 인한 Doubt 감소는 0 밑으로 안 내려간다(ApplySkillUse의 즉시형 DoubtDecrease와 동일 원칙).
            stat.Doubt = UnityEngine.Mathf.Max(0f, stat.Doubt - entry.PerTurn);
            stat.DoubtDeclines.Add(new DoubtDeclineBuff
            {
                SkillId = entry.SkillId,
                PerTurn = entry.PerTurn,
                TurnsRemaining = entry.TurnsRemaining - 1
            });
        }
    }

    /// <summary>이 스킬의 총량(totalAmount) 0.5%씩 200턴에 걸쳐 균등 분할 차감하는 항목을 새로 추가한다.
    /// 같은 스킬을 재구매해도 기존 진행 중인 항목을 덮어쓰지 않고 별도 항목으로 쌓여 중복 적용된다.</summary>
    public static void StartDoubtDecline(PlayerStat stat, SkillID skillId, float totalAmount)
    {
        stat.DoubtDeclines.Add(new DoubtDeclineBuff
        {
            SkillId = skillId,
            PerTurn = totalAmount * DoubtDeclineRatioPerTurn,
            TurnsRemaining = DoubtDeclineDurationTurns
        });
    }
}
