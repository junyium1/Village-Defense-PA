using TMPro;
using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Planche d'option à bascule (menu 3D) — ex. Musique ON/OFF.
    /// Version 3D simple du réglage volume (AudioListener.volume).
    /// </summary>
    public class SignOptionToggle : SignPlankBase
    {
        [SerializeField] TextMeshPro label;
        [SerializeField] string labelFormat = "Musique : {0}";
        [SerializeField, Range(0f, 1f)] float onVolume = 0.5f;

        bool _isOn = true;

        void OnEnable()
        {
            _isOn = AudioListener.volume > 0.001f;
            RefreshLabel();
        }

        public override void OnClicked()
        {
            _isOn = !_isOn;
            AudioListener.volume = _isOn ? onVolume : 0f;
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (label != null) label.text = string.Format(labelFormat, _isOn ? "ON" : "OFF");
        }
    }
}
