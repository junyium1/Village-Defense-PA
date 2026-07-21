using UnityEngine;
using TMPro;
using Game;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text crystalText;

    private void Update()
    {
        if (Player.Instance == null) return;
        if (goldText != null) goldText.text = Player.Instance.gold.ToString();
        if (crystalText != null) crystalText.text = Player.Instance.crystals.ToString();
    }
}