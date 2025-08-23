// ITargetSelector.cs
using System.Collections.Generic;
using UnityEngine;
public interface ITargetSelector
{
    Monster SelectPrimary(IList<Monster> candidates, Vector3 from);
    IEnumerable<Monster> SelectMultiple(IList<Monster> candidates, Vector3 from, int count);
}
