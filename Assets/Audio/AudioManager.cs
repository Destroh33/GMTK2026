using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    private float pitchMin = 0.9f;
    private float pitchMax = 1.1f;

    public void PlayMusic(AudioClip clip) {
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float vol = 1f) {
        sfxSource.PlayOneShot(clip, vol);
    }

    public void OnMasterVolumeChange(float value) {
        mixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
    }

    public void OnMusicVolumeChange(float value) {
        mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
    }

    public void OnSFXVolumeChange(float value) {
        mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
    }

    public void PitchVariatedClip(AudioClip clip, float vol = 1f) {
        float origPitch = sfxSource.pitch;
        sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip, vol);
        sfxSource.pitch = origPitch;
    }
}
