// 캐릭터 선택 씬 -> 메인 게임 씬으로 넘어갈 때 선택된 직업을 들고 가는 다리 역할.
// 정적 필드라 씬이 바뀌어도(도메인 리로드 전까지) 값이 유지된다 — DontDestroyOnLoad
// 오브젝트를 새로 만들 필요 없이 JobSO 참조 하나만 넘기면 되므로 이 방식을 택했다.
public static class JobSelectionHandoff
{
    public static JobSO SelectedJob;
}
