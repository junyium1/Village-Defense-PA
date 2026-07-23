using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    // Dernier panneau ouvert via un PanelToggle : permet à Échap (PauseMenuManager)
    // de le fermer en priorité, sans aucun câblage manuel.
    public static GameObject OpenPanel { get; private set; }

    private void Awake()
    {
        // Couvre le cas où le panneau serait déjà affiché au démarrage.
        if (panel != null && panel.activeSelf) OpenPanel = panel;
    }

    public void Toggle()
    {
        bool willOpen = !panel.activeSelf;
        panel.SetActive(willOpen);
        OpenPanel = willOpen ? panel : null;
    }
}