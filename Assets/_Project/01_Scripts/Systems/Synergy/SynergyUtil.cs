using UnityEngine;

public static class SynergyUtil
{
    public static bool HasAny(this JobSynergy self, JobSynergy flags) => (self & flags) != 0;
    public static bool HasAll(this JobSynergy self, JobSynergy flags) => (self & flags) == flags;

    public static bool HasAny(this OriginSynergy self, OriginSynergy flags) => (self & flags) != 0;
    public static bool HasAll(this OriginSynergy self, OriginSynergy flags) => (self & flags) == flags;
}
