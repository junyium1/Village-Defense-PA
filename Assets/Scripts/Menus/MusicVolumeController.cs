using UnityEngine;
using UnityEngine.UI;

public class MusicVolumeController : MonoBehaviour
{
    [Header("UI Elements")]
    public Toggle musicToggle;
    public Slider musicSlider;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float currentVolume = 0.5f;

    // TODO implement saving settings
    private void Start()
    {
        musicSlider.value = 0.5f;
        // slider enabled only if toggle is active
        musicSlider.interactable = musicToggle.isOn;
        
        musicToggle.onValueChanged.AddListener(OnToggleChanged);
        musicSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        musicSlider.interactable = isOn;
        currentVolume = isOn ? musicSlider.value : 0f;
        ApplyVolume();
    }

    private void OnSliderChanged(float value)
    {
        currentVolume = value;
        ApplyVolume();
    }

    // TODO handle different audio sources (music VS SFX)
    // currently just changed global volume (｡•́︿•̀｡)
    private void ApplyVolume()
    {
        AudioListener.volume = currentVolume;
    }
}