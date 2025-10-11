// IShopOdds.cs
using System.Collections.Generic;
public interface IShopOdds
{
    UnitData RollOne(int playerLevel);
    IReadOnlyList<UnitData> RollMany(int count, int playerLevel);
}
