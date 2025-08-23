// ISpawner.cs
public interface ISpawner
{
    void StartWave(int waveIndex);
    void StopSpawning();
    bool IsWaveFinished();
}
