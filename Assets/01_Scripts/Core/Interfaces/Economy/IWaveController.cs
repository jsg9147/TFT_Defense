// IWaveController.cs
using System;
public interface IWaveController
{
    event Action<int> OnWaveStart;
    event Action OnWaveEnd;
    void StartLoop();
    void StopLoop();
}
