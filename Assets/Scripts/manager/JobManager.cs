using UnityEngine;

public class JobManager : MonoBehaviour
{
    public static JobManager Instance { get; private set; }

    private RuntimeJobData runtimeJobData = new();

    /// <summary>
    /// 현재 선택된 직업
    /// </summary>
    public JobSO CurrentJob => runtimeJobData.CurrentJob;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 캐릭터 선택 씬에서 넘어온 선택 정보가 있으면(정적 필드라 씬 전환 후에도 남아있음) 반영한다.
        if (JobSelectionHandoff.SelectedJob != null)
        {
            SelectJob(JobSelectionHandoff.SelectedJob);
            JobSelectionHandoff.SelectedJob = null;
        }
    }

    private void OnEnable()
    {
        EventHub.OnJobSelected += SelectJob;
    }

    private void OnDisable()
    {
        EventHub.OnJobSelected -= SelectJob;
    }

    /// <summary>
    /// 직업 선택 -> 여기서 씬 넘어온걸 런타임으로 넘겨줌 
    /// </summary>
    public void SelectJob(JobSO job)
    {
        if (job == null)
            return;

        runtimeJobData.CurrentJob = job;
        PlayerManager.Instance.currentMoney = job.startingMoney;
        PlayerManager.Instance.currentCoins = job.startingCoins;

        StatCalculator.ApplyJobSelection(MarketManager.Instance.CurrentStat, job);
        // CashBonus/PositiveEventRate/NegativeEventRate/Volume/ExitUnlock은 ApplyJob이 매 턴 다시 계산하는
        // 값이라, 선택 시점에 한 번 더 불러두지 않으면 첫 턴이 지나기 전까지는 0으로 비어있다(예: 거래 모달이
        // 시간을 멈추므로 선택 직후 바로 거래하면 CashBonus 보너스가 안 붙는 버그가 있었다).
        StatCalculator.ApplyJob(MarketManager.Instance.CurrentStat);
    }
}