// 메인 게임 씬 -> 엔딩 씬으로 넘어갈 때 확정된 엔딩 타입을 들고 가는 다리 역할.
// JobSelectionHandoff와 같은 이유로 정적 필드를 쓴다 (DontDestroyOnLoad 오브젝트 없이 값 하나만 넘기면 됨).
public static class EndingHandoff
{
    public static EndingType? Ending;
}
