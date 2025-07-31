using UnityEngine;
using Utils;

public class TimeManager : Singleton<TimeManager>
{
    public float turnDuration = 1f;
    public float multiplicator = 2f;
    public int turn = 1;
    public bool paused = false;
    public bool fastForward = false;
    public delegate void OnTimeAdvanceHandler();
    public event OnTimeAdvanceHandler OnTimeAdvance;
    private float advancedTime;

    protected override void Awake()
    {
        base.Awake();

        advancedTime = turnDuration;
    }

    void Update()
    {
        if (!paused)
        {
            advancedTime -= Time.deltaTime * (fastForward ? multiplicator : 1f);

            if (advancedTime <= 0)
            {
                advancedTime += turnDuration;
                OnTimeAdvance?.Invoke();
                turn++;
                ResourceSystem.ResourceManager.Instance.UpdateWorkForce();
                ProductionManager.Instance.UpdateResources();
                //GlobalVariablesManager.UpdateVariableProduction();
            }
        }
    }

    public void Pause()
    {
        paused = true;
        fastForward = false;
    }
    public void Play()
    {
        paused = false;
        fastForward = false;
    }
    public void FastForward()
    {
        paused = false;
        fastForward = true;
    }
}
