using TMPro;
using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Planche d'option à bascule ON/OFF (menu 3D) — Musique ou Effets sonores.
    /// Lit/écrit <see cref="SettingsStore"/> (persisté PlayerPrefs, appliqué au mixer).
    /// Se rafraîchit à l'activation et sur tout changement de réglage.
    /// </summary>
    public class SignOptionToggle : SignPlankBase
    {
        public enum Setting { MusicOn, SfxOn }

        [SerializeField] TextMeshPro label;
        [SerializeField] string labelFormat = "Musique : {0}";
        [SerializeField] Setting setting = Setting.MusicOn;

        void OnEnable()
        {
            SettingsStore.Changed += RefreshLabel;
            RefreshLabel();
        }

        void OnDisable()
        {
            SettingsStore.Changed -= RefreshLabel;
        }

        public override void OnClicked()
        {
            if (setting == Setting.MusicOn) SettingsStore.MusicOn = !SettingsStore.MusicOn;
            else SettingsStore.SfxOn = !SettingsStore.SfxOn;
        }

        void RefreshLabel()
        {
            if (label == null) return;
            bool isOn = setting == Setting.MusicOn ? SettingsStore.MusicOn : SettingsStore.SfxOn;
            label.text = string.Format(labelFormat, isOn ? "ON" : "OFF");
        }
    }
}
