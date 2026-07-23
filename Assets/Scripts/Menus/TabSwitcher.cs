using UnityEngine;

public class TabSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject unitsPanel;
    [SerializeField] private GameObject defensesPanel;

    public void ShowUnits()
    {
        unitsPanel.SetActive(true);
        defensesPanel.SetActive(false);
    }

    public void ShowDefenses()
    {
        unitsPanel.SetActive(false);
        defensesPanel.SetActive(true);
    }

    private void OnEnable()
    {
        ShowUnits();
    }
}