using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
//AUDIO ATTRIBUTIONS 
//CLOCKTick-Blue Snowball Microphone, CU_Large, Alarm, Looped_Nicholas Judy_TDC by designerschoice -- https://freesound.org/s/805330/ -- License: Attribution 4.0
//Sword 5.wav by CpawsMusic -- https://freesound.org/s/437119/ -- License: Attribution 3.0
//10SWORD05.aif by lostchocolatelab -- https://freesound.org/s/1468/ -- License: Creative Commons 0
//Sword_Clash (7).wav by JohnBuhr -- https://freesound.org/s/326868/ -- License: Creative Commons 0
//Metal 06.wav by Debsound -- https://freesound.org/s/168822/ -- License: Attribution NonCommercial 4.0
//Rattling Bones.wav by spookymodem -- https://freesound.org/s/202102/ -- License: Creative Commons 0

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
    [SerializeField] private List<AudioClip> clockTickSFX;
    [SerializeField] private float clockTickVolume = 0.5f;
    [SerializeField] private float clockTickMaxVolumeMultiplier = 5f;
    [SerializeField] private List<AudioClip> reflectSFX;
    [SerializeField] private AudioClip enemyShootSFX;
    [SerializeField] private AudioClip enemyDeathSFX;
    [SerializeField] private AudioClip clockHandHitSFX;

    [Header("Boss")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip bossHitSFX;
    [SerializeField] private AudioClip bossDeathSFX;
    [SerializeField] private AudioClip bossTeleportSFX;
    [SerializeField] private AudioClip bossDashSFX;
    [SerializeField] private AudioClip bossSpawnBatsSFX;
    [SerializeField] private AudioClip bossSpawnSkeletonSFX;

    [Header("Gibberish (Dialogue)")]
    [SerializeField] private List<AudioClip> gibberishSFX;
    [SerializeField] private float gibberishPitch = 2f;

    private float pitchMin = 0.9f;
    private float pitchMax = 1.1f;

    private int clockTickIndex;
    private Coroutine gibberishRoutine;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (GameManager.Instance != null)
            GameManager.Instance.OnFloorStarted -= HandleFloorStarted;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFloorStarted += HandleFloorStarted;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFloorStarted += HandleFloorStarted;
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

    public void PlayClockTickSFX()
    {
        if (clockTickSFX == null || clockTickSFX.Count == 0) return;

        AudioClip clip = clockTickSFX[clockTickIndex % clockTickSFX.Count];
        clockTickIndex++;

        PitchVariatedClip(clip, clockTickVolume * ClockTickUrgencyMultiplier());
    }

    float ClockTickUrgencyMultiplier()
    {
        if (GameManager.Instance == null || GameManager.Instance.StartTime <= 0f) return 1f;

        float remainingFraction = Mathf.Clamp01(GameManager.Instance.TimeRemaining / GameManager.Instance.StartTime);
        return Mathf.Lerp(clockTickMaxVolumeMultiplier, 1f, remainingFraction);
    }

    public void PlayReflectSFX() => PitchVariatedClip(RandomClip(reflectSFX));

    AudioClip RandomClip(List<AudioClip> clips)
    {
        return clips[Random.Range(0, clips.Count)];
    }

    public void PlayEnemyShootSFX() => PitchVariatedClip(enemyShootSFX);

    public void PlayEnemyDeathSFX(AudioClip clip = null) => PitchVariatedClip(clip != null ? clip : enemyDeathSFX);

    public void PlayClockHandHitSFX() => PitchVariatedClip(clockHandHitSFX, 0.5f);

    public void PlayBossMusic() => PlayMusic(bossMusic);

    public void PlayBossHitSFX() => PitchVariatedClip(bossHitSFX);

    public void PlayBossDeathSFX() => PitchVariatedClip(bossDeathSFX);

    public void PlayBossTeleportSFX() => PitchVariatedClip(bossTeleportSFX);

    public void PlayBossDashSFX() => PitchVariatedClip(bossDashSFX);

    public void PlayBossSpawnBatsSFX() => PitchVariatedClip(bossSpawnBatsSFX, 0.7f);

    public void PlayBossSpawnSkeletonSFX() => PitchVariatedClip(bossSpawnSkeletonSFX, 2f);

    public void StartGibberish()
    {
        StopGibberish();

        if (gibberishSFX == null || gibberishSFX.Count == 0) return;

        gibberishRoutine = StartCoroutine(GibberishLoop());
    }

    public void StopGibberish()
    {
        if (gibberishRoutine != null)
        {
            StopCoroutine(gibberishRoutine);
            gibberishRoutine = null;
        }
    }

    IEnumerator GibberishLoop()
    {
        while (true)
        {
            AudioClip clip = RandomClip(gibberishSFX);
            float duration = clip.length / gibberishPitch;

            GameObject tempAudioObject = new GameObject("GibberishSFX");
            AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
            tempAudioSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            tempAudioSource.pitch = gibberishPitch;
            tempAudioSource.PlayOneShot(clip);
            Destroy(tempAudioObject, duration);

            yield return new WaitForSecondsRealtime(duration);
        }
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
        GameObject tempAudioObject = new GameObject("VaritatedSFX");
        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
        tempAudioSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        tempAudioSource.pitch = Random.Range(pitchMin, pitchMax);        
        tempAudioSource.PlayOneShot(clip, vol);
        Destroy(tempAudioObject, clip.length/tempAudioSource.pitch);
    }
}
