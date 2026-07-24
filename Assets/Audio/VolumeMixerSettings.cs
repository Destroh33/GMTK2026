using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeMixerSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    const float MinVolume = 0.0001f;

    void Start()
    {
        InitSlider(masterSlider, "MasterVolume");
        InitSlider(musicSlider, "MusicVolume");
        InitSlider(sfxSlider, "SFXVolume");

        masterSlider.onValueChanged.AddListener(value => SetVolume("MasterVolume", value));
        musicSlider.onValueChanged.AddListener(value => SetVolume("MusicVolume", value));
        sfxSlider.onValueChanged.AddListener(value => SetVolume("SFXVolume", value));
    }

    void InitSlider(Slider slider, string param)
    {
        slider.SetValueWithoutNotify(mixer.GetFloat(param, out float dB) ? Mathf.Pow(10f, dB / 20f) : 1f);
    }

    void SetVolume(string param, float value)
    {
        mixer.SetFloat(param, Mathf.Log10(Mathf.Max(value, MinVolume)) * 20f);
    }
}
