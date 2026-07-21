using UnityEngine;
using TMPro;
using Game;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text crystalText;

    private void Update()
    {
        goldText.text = Player.Instance.gold.ToString();
        crystalText.text = Player.Instance.crystals.ToString();
    }
}