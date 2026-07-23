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

    // appelé par le bouton "Defenses"
    public void ShowDefenses()
    {
        unitsPanel.SetActive(false);
        defensesPanel.SetActive(true);
    }

    // affiche Units par défaut à l'ouverture
    private void OnEnable()
    {
        ShowUnits();
    }
}