using System;

[Serializable]
public struct ResultSnapshot
{
    public int reachedWave;
    public float elapsedSeconds;
    public int goldCurrent;    // 현재 보유(간단 버전)
    public int essenceCurrent; // 현재 보유

    public static ResultSnapshot CaptureNow(int currentWave, float elapsed, int gold, int essence)
    {
        return new ResultSnapshot
        {
            reachedWave = currentWave,
            elapsedSeconds = elapsed,
            goldCurrent = gold,
            essenceCurrent = essence
        };
    }
}
