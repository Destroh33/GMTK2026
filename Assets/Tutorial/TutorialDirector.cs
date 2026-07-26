using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialDirector : MonoBehaviour
{
    public enum BeatCondition
    {
        None,
        Move,
        Dash,
        Attack,
        KillEnemy,
        SwitchWeapon,
        StrikeHandForTime,
        WaveCleared,
        FloorCleared
    }

    [Serializable]
    public class Beat
    {
        [TextArea(2, 4)] public string line;
        public BeatCondition condition = BeatCondition.None;
        public bool holdWorldWhileReading = true;
        public float minReadTime = 0.4f;
        public bool releaseFloorGate;
        public bool unlockClockStrikes;
        public bool repeatIfIdle = true;
        public bool skipIfAlreadyMet = true;
        public BeatCondition skipIf = BeatCondition.None;
    }

    public const string PrefsKey = "tutorial_done";

    [Header("References")]
    [SerializeField] private TypewriterText typewriter;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private GameObject dialoguePrefab;

    [Header("Trigger")]
    [SerializeField] private int tutorialFloorIndex = 0;
    [SerializeField] private bool runOnce = true;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private bool lockClockUntilTaught = true;

    [Header("Conditions")]
    [SerializeField] private float moveHoldRequired = 0.5f;
    [SerializeField] private float conditionTimeout = 0f;
    [SerializeField] private float delayBeforeNextBeat = 1f;
    [SerializeField] private float repeatAfterSeconds = 10f;

    [Header("Pausing")]
    [SerializeField] private bool pauseGameWhileSpeaking = true;

    [Header("Skipping")]
    [SerializeField] private bool allowSkipInput = true;
    [SerializeField] private bool requireDismissInput = true;
    [SerializeField] private float skipLockout = 0.2f;

    [Header("Beats")]
    [SerializeField]
    private List<Beat> beats = new List<Beat>
    {
        new Beat { line = "Welcome to the clock tower. Move with WASD.", condition = BeatCondition.Move },
        new Beat { line = "Space dodges. You will need it.", condition = BeatCondition.Dash },
        new Beat { line = "Left click to attack.", condition = BeatCondition.Attack },
        new Beat { line = "Press 1, 2 or 3 to swap weapons. 1 is your pizza cutter.", condition = BeatCondition.SwitchWeapon },
        new Beat { line = "Now clear them out.", condition = BeatCondition.WaveCleared },
        new Beat { line = "That clock is your life. When it runs out, so do you.", condition = BeatCondition.None },
        new Beat { line = "Bullets do nothing to it. Only the pizza cutter can move the hands.", condition = BeatCondition.None },
        new Beat { line = "Press 1, then swing the cutter into a hand against its sweep. That pays you back in time.", condition = BeatCondition.StrikeHandForTime },
        new Beat { line = "But the tower learns. Every strike after the first is worth far less, until the next floor.", condition = BeatCondition.None, releaseFloorGate = true }
    };

    bool running;
    bool finished;
    bool holding;
    float scaleBeforeHold = 1f;

    public static bool WorldPaused { get; private set; }

    float moveHeldTime;
    bool dashed;
    bool attacked;
    bool killedEnemy;
    bool switchedWeapon;
    bool struckHandForTime;
    bool waveCleared;
    bool floorCleared;

    PlayerMovement playerMovement;
    WeaponController weaponController;

    public bool IsRunning => running;

    public static bool Completed => PlayerPrefs.GetInt(PrefsKey, 0) == 1;

    public static void SetCompleted(bool completed)
    {
        PlayerPrefs.SetInt(PrefsKey, completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    void Start()
    {
        if (runOnce && Completed)
        {
            ClockHand.StrikesLocked = false;
            ShowDialogue(false);
            enabled = false;
            return;
        }

        ClockHand.StrikesLocked = lockClockUntilTaught;

        ResolveDialogue();
        ShowDialogue(false);
        Subscribe();

        if (GameManager.Instance != null
            && GameManager.Instance.CurrentFloorIndex == tutorialFloorIndex
            && GameManager.Instance.CurrentWaveIndexInFloor >= 0)
        {
            HandleFloorStarted(tutorialFloorIndex);
        }
    }

    void OnDestroy()
    {
        Unsubscribe();
        ReleaseHold();
        ClockHand.StrikesLocked = false;
    }

    void Subscribe()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFloorStarted += HandleFloorStarted;
            GameManager.Instance.OnWaveCleared += HandleWaveCleared;
            GameManager.Instance.OnFloorCleared += HandleFloorCleared;
            GameManager.Instance.OnRunReset += HandleRunReset;
        }

        EnemyBase.OnAnyDied += HandleEnemyDied;
        WeaponBase.OnAnyWeaponUsed += HandleWeaponUsed;
        StageClock.OnStrikeTimeGained += HandleStrikeTimeGained;

        weaponController = FindAnyObjectByType<WeaponController>();
        if (weaponController != null)
            weaponController.OnWeaponChanged += HandleWeaponChanged;
    }

    void Unsubscribe()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFloorStarted -= HandleFloorStarted;
            GameManager.Instance.OnWaveCleared -= HandleWaveCleared;
            GameManager.Instance.OnFloorCleared -= HandleFloorCleared;
            GameManager.Instance.OnRunReset -= HandleRunReset;
        }

        EnemyBase.OnAnyDied -= HandleEnemyDied;
        WeaponBase.OnAnyWeaponUsed -= HandleWeaponUsed;
        StageClock.OnStrikeTimeGained -= HandleStrikeTimeGained;

        if (weaponController != null)
            weaponController.OnWeaponChanged -= HandleWeaponChanged;
    }

    void HandleFloorStarted(int floorIndex)
    {
        if (floorIndex != tutorialFloorIndex)
        {
            if (running) Abort();
            return;
        }

        if (running || finished) return;

        StartCoroutine(Run());
    }

    void Abort()
    {
        StopAllCoroutines();
        ReleaseHold();
        ShowDialogue(false);
        ClockHand.StrikesLocked = false;

        running = false;
        finished = true;
    }

    void HandleWaveCleared(int waveIndex) => waveCleared = true;
    void HandleFloorCleared(int floorIndex) => floorCleared = true;
    void HandleEnemyDied(EnemyBase enemy) => killedEnemy = true;
    void HandleWeaponUsed(WeaponBase weapon) => attacked = true;
    void HandleWeaponChanged(int index) => switchedWeapon = true;
    void HandleStrikeTimeGained(float amount) => struckHandForTime = true;

    void HandleRunReset()
    {
        if (!running) return;

        StopAllCoroutines();
        ReleaseHold();
        ClockHand.StrikesLocked = false;
        running = false;
        ShowDialogue(false);
    }

    void Update()
    {
        if (!running) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentFloorIndex != tutorialFloorIndex)
        {
            Abort();
            return;
        }

        if (PlayerMovementRef() != null)
        {
            if (playerMovement.HasMoveInput) moveHeldTime += Time.unscaledDeltaTime;
            if (playerMovement.IsDashing) dashed = true;
        }
    }

    IEnumerator Run()
    {
        running = true;
        ShowDialogue(false);
        ResetAllConditions();

        if (lockClockUntilTaught) ClockHand.StrikesLocked = true;

        yield return new WaitForSecondsRealtime(startDelay);

        foreach (Beat beat in beats)
        {
            if (beat == null || string.IsNullOrWhiteSpace(beat.line)) continue;
            yield return PlayBeat(beat);
        }

        ShowDialogue(false);
        ClockHand.StrikesLocked = false;
        running = false;
        finished = true;

        if (runOnce) SetCompleted(true);
    }

    IEnumerator PlayBeat(Beat beat)
    {
        if (ShouldSkipBeat(beat)) yield break;

        bool resolved = false;

        while (!resolved)
        {
            yield return Speak(beat);

            ResetCondition(beat.condition);

            if (beat.condition == BeatCondition.None) break;

            float waited = 0f;
            float timeout = conditionTimeout;

            while (true)
            {
                if (ConditionMet(beat.condition) || SkipConditionMet(beat))
                {
                    resolved = true;
                    break;
                }

                if (conditionTimeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    if (timeout <= 0f)
                    {
                        resolved = true;
                        break;
                    }
                }

                waited += Time.unscaledDeltaTime;
                if (beat.repeatIfIdle && repeatAfterSeconds > 0f && waited >= repeatAfterSeconds) break;

                yield return null;
            }
        }

        if (delayBeforeNextBeat > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeNextBeat);
    }

    bool ShouldSkipBeat(Beat beat)
    {
        if (SkipConditionMet(beat)) return true;

        return beat.skipIfAlreadyMet
            && beat.condition != BeatCondition.None
            && ConditionMet(beat.condition);
    }

    bool SkipConditionMet(Beat beat)
    {
        return beat.skipIf != BeatCondition.None && ConditionMet(beat.skipIf);
    }

    IEnumerator Speak(Beat beat)
    {
        if (beat.unlockClockStrikes)
            ClockHand.StrikesLocked = false;

        if (beat.releaseFloorGate && GameManager.Instance != null)
            GameManager.Instance.ReleaseFloorGate();

        if (beat.holdWorldWhileReading) TakeHold();

        ShowDialogue(true);

        if (typewriter != null)
        {
            typewriter.Play(beat.line);

            float lockout = skipLockout;
            while (typewriter.IsTyping)
            {
                lockout -= Time.unscaledDeltaTime;
                if (lockout <= 0f && SkipPressed())
                {
                    typewriter.Skip();
                    break;
                }
                yield return null;
            }
        }

        float minRead = beat.minReadTime;
        while (minRead > 0f)
        {
            minRead -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (requireDismissInput)
        {
            while (!SkipPressed()) yield return null;
        }

        ShowDialogue(false);
        ReleaseHold();
    }

    bool ConditionMet(BeatCondition condition)
    {
        switch (condition)
        {
            case BeatCondition.Move: return moveHeldTime >= moveHoldRequired;
            case BeatCondition.Dash: return dashed;
            case BeatCondition.Attack: return attacked;
            case BeatCondition.KillEnemy: return killedEnemy;
            case BeatCondition.SwitchWeapon: return switchedWeapon;
            case BeatCondition.StrikeHandForTime: return struckHandForTime;
            case BeatCondition.WaveCleared: return waveCleared;
            case BeatCondition.FloorCleared: return floorCleared;
            default: return true;
        }
    }

    void ResetAllConditions()
    {
        moveHeldTime = 0f;
        dashed = false;
        attacked = false;
        killedEnemy = false;
        switchedWeapon = false;
        struckHandForTime = false;
        waveCleared = false;
        floorCleared = false;
    }

    void ResetCondition(BeatCondition condition)
    {
        switch (condition)
        {
            case BeatCondition.Move: moveHeldTime = 0f; break;
            case BeatCondition.Dash: dashed = false; break;
            case BeatCondition.Attack: attacked = false; break;
            case BeatCondition.KillEnemy: killedEnemy = false; break;
            case BeatCondition.SwitchWeapon: switchedWeapon = false; break;
            case BeatCondition.StrikeHandForTime: struckHandForTime = false; break;
            case BeatCondition.WaveCleared: waveCleared = false; break;
            case BeatCondition.FloorCleared: floorCleared = false; break;
        }
    }

    void TakeHold()
    {
        if (holding) return;

        holding = true;

        if (GameManager.Instance != null)
            GameManager.Instance.BeginTutorialHold();

        if (pauseGameWhileSpeaking)
        {
            scaleBeforeHold = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            WorldPaused = true;
        }
    }

    void ReleaseHold()
    {
        if (!holding) return;

        holding = false;

        if (GameManager.Instance != null)
            GameManager.Instance.EndTutorialHold();

        if (WorldPaused)
        {
            WorldPaused = false;

            if (SettingsButton.Instance == null || !SettingsButton.Instance.gamePaused)
                Time.timeScale = scaleBeforeHold;
        }
    }

    bool SkipPressed()
    {
        if (!allowSkipInput) return false;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

        return false;
    }

    PlayerMovement PlayerMovementRef()
    {
        if (playerMovement != null) return playerMovement;

        if (PlayerRef.Instance != null)
            playerMovement = PlayerRef.Instance.GetComponent<PlayerMovement>();

        return playerMovement;
    }

    void ResolveDialogue()
    {
        if (typewriter == null)
            typewriter = FindAnyObjectByType<TypewriterText>(FindObjectsInactive.Include);

        if (typewriter == null && dialoguePrefab != null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            GameObject instance = canvas != null
                ? Instantiate(dialoguePrefab, canvas.transform, false)
                : Instantiate(dialoguePrefab);

            instance.name = dialoguePrefab.name;
            dialogueRoot = instance;
            typewriter = instance.GetComponentInChildren<TypewriterText>(true);
        }

        if (dialogueRoot == null && typewriter != null)
            dialogueRoot = typewriter.gameObject;
    }

    void ShowDialogue(bool visible)
    {
        if (dialogueRoot != null) dialogueRoot.SetActive(visible);
    }
}
