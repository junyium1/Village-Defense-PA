using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Menus
{
    /// <summary>
    /// Réglages du jeu (options) : musique, effets sonores, qualité.
    /// Statique + PlayerPrefs : persisté entre sessions, appliqué automatiquement
    /// au lancement (avant le chargement de la première scène).
    /// Les volumes passent par l'AudioMixer Resources/MainMixer (groupes Music/SFX,
    /// paramètres exposés "MusicVolume"/"SfxVolume", en dB). Qualité = index
    /// QualitySettings (0 = Mobile, 1 = PC).
    /// Les planches du menu 3D écoutent <see cref="Changed"/> pour se rafraîchir.
    /// </summary>
    public static class SettingsStore
    {
        const string KeyMusicOn = "settings.musicOn";
        const string KeyMusicVol = "settings.musicVol";
        const string KeySfxOn = "settings.sfxOn";
        const string KeySfxVol = "settings.sfxVol";
        const string KeyQuality = "settings.quality";

        const string MixerResourcePath = "MainMixer";
        const string MusicVolumeParam = "MusicVolume";
        const string SfxVolumeParam = "SfxVolume";

        /// <summary>Notifié après tout changement de réglage (les planches se rafraîchissent).</summary>
        public static event Action Changed;

        static AudioMixer _mixer;
        static bool _mixerSearched;

        // ----------------------- valeurs (persistées) -----------------------

        public static bool MusicOn
        {
            get { return PlayerPrefs.GetInt(KeyMusicOn, 1) == 1; }
            set { PlayerPrefs.SetInt(KeyMusicOn, value ? 1 : 0); PlayerPrefs.Save(); ApplyAudio(); Notify(); }
        }

        /// <summary>Volume musique 0..1 (par paliers — voir SignOptionCycle).</summary>
        public static float MusicVolume
        {
            get { return PlayerPrefs.GetFloat(KeyMusicVol, 0.75f); }
            set { PlayerPrefs.SetFloat(KeyMusicVol, Mathf.Clamp01(value)); PlayerPrefs.Save(); ApplyAudio(); Notify(); }
        }

        public static bool SfxOn
        {
            get { return PlayerPrefs.GetInt(KeySfxOn, 1) == 1; }
            set { PlayerPrefs.SetInt(KeySfxOn, value ? 1 : 0); PlayerPrefs.Save(); ApplyAudio(); Notify(); }
        }

        /// <summary>Volume effets sonores 0..1.</summary>
        public static float SfxVolume
        {
            get { return PlayerPrefs.GetFloat(KeySfxVol, 0.75f); }
            set { PlayerPrefs.SetFloat(KeySfxVol, Mathf.Clamp01(value)); PlayerPrefs.Save(); ApplyAudio(); Notify(); }
        }

        /// <summary>Index QualitySettings (0 = Mobile, 1 = PC). Borné à l'application.</summary>
        public static int QualityIndex
        {
            get { return PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()); }
            set { PlayerPrefs.SetInt(KeyQuality, value); PlayerPrefs.Save(); ApplyQuality(); Notify(); }
        }

        // ----------------------- application -----------------------

        /// <summary>Applique tous les réglages persistés (audio + qualité).
        /// Appelé au lancement par <see cref="SettingsApplier"/> (différé de quelques
        /// frames : le moteur audio applique le snapshot par défaut du mixer APRES
        /// les RuntimeInitializeOnLoadMethod, ce qui écraserait nos SetFloat).</summary>
        public static void ApplyAll()
        {
            ApplyAudio();
            ApplyQuality();
        }

        static void ApplyAudio()
        {
            AudioMixer mixer = GetMixer();
            if (mixer == null) return;
            mixer.SetFloat(MusicVolumeParam, ToDecibels(MusicOn ? MusicVolume : 0f));
            mixer.SetFloat(SfxVolumeParam, ToDecibels(SfxOn ? SfxVolume : 0f));
        }

        static void ApplyQuality()
        {
            int count = QualitySettings.names.Length;
            if (count == 0) return;
            int index = Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()), 0, count - 1);
            QualitySettings.SetQualityLevel(index, true);
        }

        /// <summary>Conversion linéaire 0..1 → dB mixer (-80 dB = silence).</summary>
        static float ToDecibels(float linear)
        {
            return linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
        }

        static AudioMixer GetMixer()
        {
            if (_mixer == null && !_mixerSearched)
            {
                _mixerSearched = true;
                _mixer = Resources.Load<AudioMixer>(MixerResourcePath);
                if (_mixer == null)
                    Debug.LogWarning("[SettingsStore] Resources/" + MixerResourcePath + ".mixer introuvable — volumes musique/SFX inactifs.");
            }
            return _mixer;
        }

        static void Notify()
        {
            Action handler = Changed;
            if (handler != null) handler();
        }
    }
}
