using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// CENTRALNY MANAGER GRY — wszystkie ważne ustawienia w jednym miejscu.
/// 
/// Jak używać:
/// 1. Utwórz pusty GameObject w scenie → nazwij "GameManager"
/// 2. Dodaj ten skrypt
/// 3. W Inspectorze ustaw wszystkie parametry (automatycznie zsynchronizują się z innymi skryptami)
/// 4. Dostęp przez GameManager.Instance z każdego skryptu
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ============================================================
    // 🏃 RUCH GRACZA (PlayerMovement)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🏃 RUCH GRACZA")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rzut")]
    [SerializeField] private Key castKey = Key.Q;
    [SerializeField] private GameObject bobberPrefab;

    [Header("Siła rzutu")]
    [Tooltip("Minimalna odległość rzutu (szybkie kliknięcie).")]
    [SerializeField] private float minCastDistance = 2f;
    [Tooltip("Maksymalna odległość rzutu (pełne naładowanie).")]
    [SerializeField] private float maxCastDistance = 15f;
    [Tooltip("Czas ładowania do pełnej siły (sekundy).")]
    [SerializeField] private float maxChargeTime = 2f;

    // ============================================================
    // 🎣 SYSTEM ŁOWIENIA (FishingSystem)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🎣 SYSTEM ŁOWIENIA")]
    [Header("═══════════════════════════════════════")]
    [Header("Czas oczekiwania na branie")]
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;

    [Header("Awaryjna waga ryby")]
    [SerializeField] private float fallbackMinWeight = 1f;
    [SerializeField] private float fallbackMaxWeight = 40f;

    [Header("Klawisz zacięcia")]
    [SerializeField] private Key actionKey = Key.Space;

    // ============================================================
    // 🐟 ROZMIARY RYB (FishSizes)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🐟 SZANSE NA WAGĘ RYBY")]
    [Header("═══════════════════════════════════════")]
    [Header("Procentowe szanse (muszą sumować się do 100%)")]
    [SerializeField] [Range(0f, 100f)] private float chance1_5kg = 30f;
    [SerializeField] [Range(0f, 100f)] private float chance5_10kg = 30f;
    [SerializeField] [Range(0f, 100f)] private float chance10_20kg = 20f;
    [SerializeField] [Range(0f, 100f)] private float chance20_30kg = 15f;
    [SerializeField] [Range(0f, 100f)] private float chance30_40kg = 5f;

    // ============================================================
    // 🌀 ZWIJANIE (ReelInSystem)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🌀 ZWIJANIE ZESTAWU")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private Key reelKey = Key.R;
    [SerializeField] private float baseReelSpeed = 5f;
    [Tooltip("Prędkość zwijania podczas holu ryby.")]
    [SerializeField] private float reelSpeedWithFish = 3f;

    [Header("Krzywa szybkości zwijania")]
    [Tooltip("X = waga ryby, Y = mnożnik prędkości (0-1).")]
    [SerializeField] private AnimationCurve speedMultiplierCurve = AnimationCurve.Linear(0f, 1f, 40f, 0.1f);
    [SerializeField] private float maxFishWeight = 40f;

    // ============================================================
    // ⏸️ PAUZA I DEBUG
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("⏸️ PAUZA I DEBUG")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private Key pauseKey = Key.Escape;
    [SerializeField] private bool startPaused = false;
    [SerializeField] private bool verboseLogs = true;

    // ============================================================
    // 📊 STAN GRY
    // ============================================================

    public enum GameState { Playing, Paused, Fishing, Reeling, GameOver }
    private GameState currentState = GameState.Playing;

    public delegate void GameStateHandler(GameState newState);
    public event GameStateHandler OnGameStateChanged;

    public delegate void SimpleHandler();
    public event SimpleHandler OnGamePaused;
    public event SimpleHandler OnGameResumed;
    public event SimpleHandler OnGameReset;

    private List<Bobber> activeBobbers = new List<Bobber>();

    // ============================================================
    // 🔄 INIT
    // ============================================================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameManager już istnieje! Usuwam duplikat.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (startPaused)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    void Start()
    {
        SyncAllSettings();
        Log("🎮 GameManager uruchomiony!");
    }

    void Update()
    {
        if (Keyboard.current[pauseKey].wasPressedThisFrame)
            TogglePause();

        UpdateGameState();
    }

    // ============================================================
    // 🔄 SYNCHRONIZACJA USTAWIEŃ → SYSTEMY
    // ============================================================

    /// <summary>
    /// Kopiuje wszystkie ustawienia z GameManagera do odpowiednich systemów.
    /// Wołane w Start(). Możesz też wołać ręcznie po zmianie w Inspectorze.
    /// </summary>
    public void SyncAllSettings()
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.SetSettings(moveSpeed, castKey, bobberPrefab, minCastDistance, maxCastDistance, maxChargeTime);

        if (FishingSystem.Instance != null)
            FishingSystem.Instance.SetSettings(minWaitTime, maxWaitTime, fallbackMinWeight, fallbackMaxWeight, actionKey);

        if (FishSizes.Instance != null)
            FishSizes.Instance.SetChances(chance1_5kg, chance5_10kg, chance10_20kg, chance20_30kg, chance30_40kg);

        if (ReelInSystem.Instance != null)
            ReelInSystem.Instance.SetSettings(reelKey, baseReelSpeed, reelSpeedWithFish, speedMultiplierCurve, maxFishWeight);

        Log("🔄 Ustawienia zsynchronizowane z systemami!");
    }

    // ============================================================
    // 📊 STAN GRY
    // ============================================================

    private void UpdateGameState()
    {
        if (currentState == GameState.Paused) return;

        if (FishingSystem.Instance != null)
        {
            switch (FishingSystem.Instance.CurrentState)
            {
                case FishingSystem.FishingState.Waiting:
                case FishingSystem.FishingState.Biting:
                    SetState(GameState.Fishing);
                    break;
            }
        }

        if (ReelInSystem.Instance != null && ReelInSystem.Instance.IsReeling)
            SetState(GameState.Reeling);

        if (Bobber.Instance == null && !IsReeling())
            SetState(GameState.Playing);
    }

    private void SetState(GameState newState)
    {
        if (currentState == newState) return;
        GameState oldState = currentState;
        currentState = newState;
        Log($"📊 Stan gry: {oldState} → {newState}");
        OnGameStateChanged?.Invoke(newState);
    }

    public GameState CurrentState => currentState;

    // ============================================================
    // ⏸️ PAUZA
    // ============================================================

    public void TogglePause()
    {
        if (currentState == GameState.Paused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        Log("⏸️ Gra zapauzowana");
        OnGamePaused?.Invoke();
        OnGameStateChanged?.Invoke(GameState.Paused);
    }

    public void ResumeGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        Log("▶️ Gra wznowiona");
        OnGameResumed?.Invoke();
        OnGameStateChanged?.Invoke(GameState.Playing);
    }

    // ============================================================
    // 🔄 RESET GRY
    // ============================================================

    public void ResetGame()
    {
        Log("🔄 Resetowanie gry...");
        if (FishingSystem.Instance != null) FishingSystem.Instance.ResetFishing();
        DestroyAllBobbers();
        if (PlayerMovement.Instance != null) PlayerMovement.Instance.ClearBobberReference();
        if (currentState == GameState.Paused) ResumeGame();
        currentState = GameState.Playing;
        OnGameReset?.Invoke();
        OnGameStateChanged?.Invoke(GameState.Playing);
        Log("✅ Gra zresetowana!");
    }

    // ============================================================
    // 🎣 ZARZĄDZANIE SPŁAWIKAMI
    // ============================================================

    public void RegisterBobber(Bobber bobber)
    {
        if (!activeBobbers.Contains(bobber))
        {
            activeBobbers.Add(bobber);
            Log($"📝 Zarejestrowano spławik: {bobber.gameObject.name}");
        }
    }

    public void UnregisterBobber(Bobber bobber)
    {
        activeBobbers.Remove(bobber);
    }

    private void DestroyAllBobbers()
    {
        Bobber[] bobbers = FindObjectsByType<Bobber>();
        foreach (Bobber b in bobbers)
        {
            if (b != null) Destroy(b.gameObject);
        }
        activeBobbers.Clear();
        Log("🗑️ Usunięto wszystkie spławiki");
    }

    // ============================================================
    // ℹ️ METODY POMOCNICZE
    // ============================================================

    public bool IsReeling() => ReelInSystem.Instance != null && ReelInSystem.Instance.IsReeling;
    public bool IsBobberInWater() => Bobber.Instance != null && Bobber.Instance.IsInWater;
    public bool HasPlayerStarted() => PlayerMovement.Instance != null && PlayerMovement.Instance.HasStarted;

    // ============================================================
    // 📋 LOGOWANIE
    // ============================================================

    private void Log(string message)
    {
        if (verboseLogs)
            Debug.Log($"<color=#FF69B4>[GameManager]</color> {message}");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
