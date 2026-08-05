using System;
using UnityEngine;

/// <summary>
/// Long/Short 거래, 발행량 조작 1건이 시장에 주는 영향(Support/Growth/Doubt/Supply)을 계산한다.
/// 수량이 많을수록 영향력이 커지도록 코인/조작 수량에 비례한다.
/// </summary>
public static class TradeCalculator
{
    // 발행량 조작(ManipulateSupply) 전용 가중치. 지지도/상승률 변화가 너무 크다는 피드백으로 5분의 1로 낮춤(2026-08-05).
    private const float SupportWeightPerCoin = 0.02f;
    private const float GrowthWeightPerCoin = 0.02f;
    // 매수/매도(Long/Short) 전용 가중치. 기존 발행량 조작과 같은 값(0.1)을 썼더니 거래만으로 지지도/상승률이
    // 너무 크게 흔들린다는 피드백으로 5분의 1로 낮춤(2026-08-05).
    private const float TradeSupportWeightPerCoin = 0.005f;
    private const float TradeGrowthWeightPerCoin = 0.005f;
    // 발행량 조작 시 의심도가 너무 빨리 오른다는 피드백으로 10분의 1로 낮췄다가(2026-08-05),
    // 다시 1.5배로 올림(2026-08-05).
    private const float DoubtWeightPerSupplyUnit = 0.015f;
    private const float DoubtWeightPerTradeCoin = 0.002f;

    private const float SupportDecayRate = 0.995f;
    private const float GrowthDecayRate = 0.995f;

    // 발행량은 Support/Growth와 반대로 시간이 지날수록(매 턴) 자동으로 늘어난다 — 채굴/인플레이션 개념.
    // ponytail: 밸런스용 임시 수치, 실제 플레이해보고 조정 필요.
    private const float SupplyGrowthPerTurn = 50f;

    // Long : 구매 -> Support/Growth 증가. 거래 자체가 시장에 눈에 띄는 움직임이라 방향과 무관하게 Doubt도
    // 수량에 비례해 조금씩 오른다 (감쇠 없이 그대로 누적, ManipulateSupply와 동일한 설계).
    public static void Long(PlayerStat stat, long amount)
    {
        stat.Support += amount * TradeSupportWeightPerCoin;
        stat.Growth += amount * TradeGrowthWeightPerCoin;
        stat.Doubt += Math.Abs(amount) * DoubtWeightPerTradeCoin;
    }

    // Short : 판매 -> Support/Growth 감소. Doubt는 Long과 동일하게 수량에 비례해 오른다.
    public static void Short(PlayerStat stat, long amount)
    {
        stat.Support -= amount * TradeSupportWeightPerCoin;
        stat.Growth -= amount * TradeGrowthWeightPerCoin;
        stat.Doubt += Math.Abs(amount) * DoubtWeightPerTradeCoin;
    }

    // 거래 확정 전 미리보기용 : 이 수량을 매수/매도하면 Doubt가 얼마나 오르는지.
    public static float PreviewTradeDoubtIncrease(long amount)
    {
        return Math.Abs(amount) * DoubtWeightPerTradeCoin;
    }

    // 이번 거래로 Doubt가 99(체포 엔딩 기준 100 바로 아래)를 넘지 않는 한도 내에서 최대로 거래 가능한 수량.
    // 거래 모달의 슬라이더/+MAX 버튼이 잔고 기준 최대치와 이 값 중 더 작은 쪽을 쓴다.
    public static long MaxTradeAmountByDoubt(float currentDoubt)
    {
        float headroom = 99f - currentDoubt;
        return headroom <= 0f ? 0L : (long)(headroom / DoubtWeightPerTradeCoin);
    }

    // 발행량 조작 확정 전 미리보기용 : 이 수량을 조작하면 Doubt가 얼마나 오르는지. PreviewTradeDoubtIncrease와 동일 설계.
    public static float PreviewSupplyDoubtIncrease(long amount)
    {
        return Math.Abs(amount) * DoubtWeightPerSupplyUnit;
    }

    // 이번 발행량 조작으로 Doubt가 99를 넘지 않는 한도 내에서 최대로 조작 가능한 수량. MaxTradeAmountByDoubt와 동일 설계.
    // 발행량 모달의 슬라이더/+MAX 버튼이 정책 상한(MaxAdjustAmount)과 이 값 중 더 작은 쪽을 쓴다.
    public static long MaxSupplyAmountByDoubt(float currentDoubt)
    {
        float headroom = 99f - currentDoubt;
        return headroom <= 0f ? 0L : (long)(headroom / DoubtWeightPerSupplyUnit);
    }

    // 발행량 조작 : 발행량 증가(희석) -> Support/Growth 감소, 발행량 감소(소각) -> Support/Growth 증가.
    // 늘리든 줄이든 조작 자체가 의심을 키우므로 Doubt는 수량의 절대값에 비례해 증가한다 (감쇠 없이 그대로 누적).
    public static void ManipulateSupply(PlayerStat stat, long amount)
    {
        stat.Supply += amount;
        stat.Support -= amount * SupportWeightPerCoin;
        stat.Growth -= amount * GrowthWeightPerCoin;
        stat.Doubt += Math.Abs(amount) * DoubtWeightPerSupplyUnit;
    }

    // 시간이 지나면 Support/Growth가 0으로 서서히 수렴한다. Doubt는 감쇠 대상이 아니다 (Game_Formula.md 3장 참고).
    // JobSkillSupportBonus/GrowthBonus(UI 표시용, Job+Skill 기여분만 별도 추적)도 Support/Growth와 동일하게 감쇠시킨다.
    public static void Decay(PlayerStat stat)
    {
        stat.Support *= SupportDecayRate;
        stat.Growth *= GrowthDecayRate;
        stat.JobSkillSupportBonus *= SupportDecayRate;
        stat.JobSkillGrowthBonus *= GrowthDecayRate;
    }

    // 매 턴 자동으로 발행량이 늘어난다 (인플레이션). 발행량 조작/이벤트/스킬로 늘고 주는 것과는 별개로 항상 적용.
    // suppressionRatio(0~1)는 추가발행권한/우회발행권한 같은 스킬이 이 증가율 자체를 얼마나 깎는지 — Scarcity
    // 공식(비율 기반)과 같은 방식으로, 고정값을 빼는 게 아니라 증가폭에 곱해서 마이너스로 넘어가지 않게 한다.
    public static void GrowSupply(PlayerStat stat, float suppressionRatio)
    {
        stat.Supply += SupplyGrowthPerTurn * (1f - Mathf.Clamp01(suppressionRatio));
    }
}
