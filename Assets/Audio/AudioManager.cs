using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private List<AudioClip> floorMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip rollSFX;
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private AudioClip swingSFX;

    private float pitchMin = 0.9f;
    private float pitchMax = 1.1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFloorStarted -= HandleFloorStarted;
        }
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFloorStarted += HandleFloorStarted;
        }
    }

    void HandleFloorStarted(int floorIndex)
    {
        PlayFloorMusic(floorIndex + 1);
    }

    public void PlayMusic(AudioClip clip) {
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlayFloorMusic(int floor)
    {
        int index = floor - 1;

        if (index < 0 || index >= floorMusic.Count)
        {
            index = floorMusic.Count - 1;
        }

        PlayMusic(floorMusic[index]);
    }

    public void PlaySFX(AudioClip clip, float vol = 1f) {
        sfxSource.PlayOneShot(clip, vol);
    }

    public void PlayHitSFX() => PitchVariatedClip(hitSFX);

    public void PlayRollSFX() => PitchVariatedClip(rollSFX);

    public void PlayShootSFX() => PitchVariatedClip(shootSFX);

    public void PlaySwingSFX() => PitchVariatedClip(swingSFX);

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
        GameObject tempAudioObject = new GameObject("VaritatedSFX");
        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
        tempAudioSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        tempAudioSource.pitch = Random.Range(pitchMin, pitchMax);        
        tempAudioSource.PlayOneShot(clip, vol);
        Destroy(tempAudioObject, clip.length/tempAudioSource.pitch);
    }
}
