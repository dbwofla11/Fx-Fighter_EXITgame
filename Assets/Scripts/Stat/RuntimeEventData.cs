using System.Collections.Generic;

/// <summary>
/// 지금까지 발생한 시사 이벤트의 기록을 관리한다.
/// </summary>
public class RuntimeEventData
{
    public List<EventLogEntry> Log = new();
}
