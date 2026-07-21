using UnityEngine;
using TMPro;
using Game;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text crystalText;

    private int _lastGold = -1;
    private int _lastCrystals = -1;

    private void Update()
    {
        if (Player.Instance == null) return;

        int currentGold = Player.Instance.gold;
        int currentCrystals = Player.Instance.crystals;

        if (goldText != null && currentGold != _lastGold)
        {
            goldText.text = currentGold.ToString();
            _lastGold = currentGold;
        }

        if (crystalText != null && currentCrystals != _lastCrystals)
        {
            crystalText.text = currentCrystals.ToString();
            _lastCrystals = currentCrystals;
        }
    }
}