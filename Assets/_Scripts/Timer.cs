using System;

public abstract class Timer
{
    protected float InitialTime;

    public float Time{ get; protected set; }
    public bool IsRunning{ get; protected set; }

    public Action OnTimerStarted;
    public Action OnTimerStopped;

    public float Progress => Time/InitialTime;

    public Timer(float _InitialTime)
    {
        InitialTime = _InitialTime;
        IsRunning = false;
    }

    public void Start()
    {
        Time = InitialTime;
        if (!IsRunning)
        {
            IsRunning = true;
            OnTimerStarted?.Invoke();
        }
    }

    public void Stop()
    {
        if (IsRunning)
        {
            IsRunning = false;
            OnTimerStopped?.Invoke();
        }
    }

    public void Resume() => IsRunning = true;

    public void Pause() => IsRunning = false;

    public abstract void Tick(float deltaTime);
}

