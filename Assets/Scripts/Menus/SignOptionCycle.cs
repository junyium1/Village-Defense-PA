using TMPro;
using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Planche d'option à valeur cyclique (menu 3D) : chaque clic avance d'un cran
    /// (retour au début après le dernier). Sert pour les volumes (paliers
    /// 0/25/50/75/100 %) et la qualité (Mobile/PC, noms lus dans QualitySettings).
    /// Lit/écrit <see cref="SettingsStore"/> (persisté PlayerPrefs).
    /// </summary>
    public class SignOptionCycle : SignPlankBase
    {
        public enum Setting { MusicVolume, SfxVolume, Quality }

        [SerializeField] TextMeshPro label;
        [SerializeField] string labelFormat = "Volume musique : {0}";
        [SerializeField] Setting setting = Setting.MusicVolume;

        [Tooltip("Paliers pour les réglages de volume (0..1). Ignoré en mode Qualité.")]
        [SerializeField] float[] volumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

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
            switch (setting)
            {
                case Setting.MusicVolume:
                    SettingsStore.MusicVolume = NextStep(SettingsStore.MusicVolume);
                    break;
                case Setting.SfxVolume:
                    SettingsStore.SfxVolume = NextStep(SettingsStore.SfxVolume);
                    break;
                case Setting.Quality:
                    int count = QualitySettings.names.Length;
                    if (count > 0) SettingsStore.QualityIndex = (SettingsStore.QualityIndex + 1) % count;
                    break;
            }
        }

        /// <summary>Palier suivant (avec retour au premier après le dernier).</summary>
        float NextStep(float current)
        {
            if (volumeSteps == null || volumeSteps.Length == 0) return current;
            // Tolérance : la valeur persistée est toujours un palier exact, mais on
            // sécurise contre toute valeur intermédiaire (ex. réglage legacy 0.5).
            for (int i = 0; i < volumeSteps.Length; i++)
            {
                if (current < volumeSteps[i] - 0.001f)
                    return volumeSteps[i];
            }
            return volumeSteps[0];
        }

        void RefreshLabel()
        {
            if (label == null) return;
            string value;
            switch (setting)
            {
                case Setting.MusicVolume:
                    value = Mathf.RoundToInt(SettingsStore.MusicVolume * 100f) + " %";
                    break;
                case Setting.SfxVolume:
                    value = Mathf.RoundToInt(SettingsStore.SfxVolume * 100f) + " %";
                    break;
                default:
                    string[] names = QualitySettings.names;
                    int index = Mathf.Clamp(SettingsStore.QualityIndex, 0, Mathf.Max(0, names.Length - 1));
                    value = names.Length > 0 ? names[index] : "?";
                    break;
            }
            label.text = string.Format(labelFormat, value);
        }
    }
}
