using System.Collections.Generic;

/// <summary>
/// 지금까지 발생한 시사 이벤트의 기록을 관리한다.
/// </summary>
public class RuntimeEventData
{
    public List<EventLogEntry> Log = new();

    // 같은 선택지를 실제로 고른 횟수만 저장한다. 다른 선택지 선택은 해당 선택지의 비용에 영향을 주지 않는다.
    private readonly Dictionary<EventSO, int[]> choiceSelectionCounts = new();

    public int GetChoiceSelectionCount(EventSO profile, int choiceIndex)
    {
        if (profile == null || choiceIndex < 0 || profile.choices == null || choiceIndex >= profile.choices.Count)
            return 0;

        if (!choiceSelectionCounts.TryGetValue(profile, out int[] counts))
            return 0;

        return choiceIndex < counts.Length ? counts[choiceIndex] : 0;
    }

    public void IncrementChoiceSelection(EventSO profile, int choiceIndex)
    {
        if (profile == null || choiceIndex < 0 || profile.choices == null || choiceIndex >= profile.choices.Count)
            return;

        if (!choiceSelectionCounts.TryGetValue(profile, out int[] counts)
            || counts.Length != profile.choices.Count)
        {
            counts = new int[profile.choices.Count];
            choiceSelectionCounts[profile] = counts;
        }

        counts[choiceIndex]++;
    }
}
