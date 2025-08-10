using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;

    private Unit selectedUnit;

    void Awake() => Instance = this;

    public void SelectUnit(Unit unit)
    {
        if (selectedUnit != null)
            selectedUnit.SetHighlight(false);

        selectedUnit = unit;
        selectedUnit.SetHighlight(true);
    }

    public void Deselect()
    {
        if (selectedUnit != null)
        {
            selectedUnit.SetHighlight(false);
            selectedUnit = null;
        }
    }

    public Unit GetSelectedUnit() => selectedUnit;
}
