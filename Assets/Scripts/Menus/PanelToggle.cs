using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public void Toggle()
    {
        Debug.Log("Toggle appelé !");   // ← ligne de test
        panel.SetActive(!panel.activeSelf);
    }
}