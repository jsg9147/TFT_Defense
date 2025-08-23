// IAttackBehaviour.cs
using System.Collections.Generic;
public interface IAttackBehaviour
{
    void Execute(Unit owner, IList<Monster> inRange, IDamageCalculator calc);
}
