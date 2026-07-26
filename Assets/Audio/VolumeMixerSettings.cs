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
        Bind(masterSlider, "MasterVolume");
        Bind(musicSlider, "MusicVolume");
        Bind(sfxSlider, "SFXVolume");
    }

    void Bind(Slider slider, string param)
    {
        if (slider == null || mixer == null) return;

        slider.SetValueWithoutNotify(mixer.GetFloat(param, out float dB) ? Mathf.Pow(10f, dB / 20f) : 1f);
        slider.onValueChanged.AddListener(value => SetVolume(param, value));
    }

    void SetVolume(string param, float value)
    {
        if (mixer == null) return;

        mixer.SetFloat(param, Mathf.Log10(Mathf.Max(value, MinVolume)) * 20f);
    }
}
