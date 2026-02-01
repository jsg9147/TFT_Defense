// WaveConfig.cs
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Config/WaveSet")]
public class WaveSet : ScriptableObject
{
    public Wave[] waves;
}

[Serializable]
public struct Wave
{
    public WaveGroup[] groups; // 웨이브 안에 여러 그룹(종류/수/딜레이 등) 
}

[Serializable]
public struct WaveGroup
{
    public MonsterData monster;        // 어떤 몬스터
    public int count;                  // 몇 마리
    public float spawnInterval;        // 마리 간 간격
    public int pathId;                 // 경로분기 쓰면
}
