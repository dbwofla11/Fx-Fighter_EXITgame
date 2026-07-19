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

    /// <summary>
    /// 직업 선택
    /// </summary>
    public void SelectJob(JobSO job)
    {
        if (job == null)
            return;

        runtimeJobData.CurrentJob = job;
        runtimeJobData.selected = true;
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