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
        runtimeJobData.selected = true;

        StatCalculator.ApplyJobSelection(MarketManager.Instance.CurrentStat, job);
    }

    /// <summary>
    /// 직업 선택 여부
    /// </summary>
    public bool HasJob()
    {
        return runtimeJobData.selected;
    }

    /// <summary>
    /// 현재 직업 제거
    /// </summary>
    public void ClearJob()
    {
        runtimeJobData.CurrentJob = null;
        runtimeJobData.selected = false;
    }
}