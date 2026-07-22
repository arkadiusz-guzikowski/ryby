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
    [SerializeField] private float predkoscRuchu = 5f;

    [Header("Rzut")]
    [SerializeField] private Key klawiszRzutu = Key.Q;
    [SerializeField] private GameObject prefabSpławika;

    [Header("Siła rzutu")]
    [Tooltip("Minimalna odległość rzutu (szybkie kliknięcie).")]
    [SerializeField] private float minOdległośćRzutu = 2f;
    [Tooltip("Maksymalna odległość rzutu (pełne naładowanie).")]
    [SerializeField] private float maxOdległośćRzutu = 15f;
    [Tooltip("Czas ładowania do pełnej siły (sekundy).")]
    [SerializeField] private float czasŁadowania = 2f;

    // ============================================================
    // 🎣 SYSTEM ŁOWIENIA (FishingSystem)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🎣 SYSTEM ŁOWIENIA")]
    [Header("═══════════════════════════════════════")]
    [Header("Czas oczekiwania na branie")]
    [SerializeField] private float minCzasOczekiwania = 2f;
    [SerializeField] private float maxCzasOczekiwania = 5f;

    [Header("Awaryjna waga ryby")]
    [SerializeField] private float awaryjnaMinWaga = 1f;
    [SerializeField] private float awaryjnaMaxWaga = 40f;

    [Header("Klawisz zacięcia")]
    [SerializeField] private Key klawiszAkcji = Key.Space;

    // ============================================================
    // 🐟 ROZMIARY RYB (FishSizes)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🐟 SZANSE NA WAGĘ RYBY")]
    [Header("═══════════════════════════════════════")]
    [Header("Procentowe szanse (muszą sumować się do 100%)")]
    [SerializeField] [Range(0f, 100f)] private float szansa1_5kg = 30f;
    [SerializeField] [Range(0f, 100f)] private float szansa5_10kg = 30f;
    [SerializeField] [Range(0f, 100f)] private float szansa10_20kg = 20f;
    [SerializeField] [Range(0f, 100f)] private float szansa20_30kg = 15f;
    [SerializeField] [Range(0f, 100f)] private float szansa30_40kg = 5f;

    // ============================================================
    // 🌀 ZWIJANIE (ReelInSystem)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🌀 ZWIJANIE ZESTAWU")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private Key klawiszZwijania = Key.R;
    [SerializeField] private float bazowaPrędkośćZwijania = 5f;
    [Tooltip("Prędkość zwijania podczas holu ryby.")]
    [SerializeField] private float prędkośćZwijaniaZRyba = 3f;

    [Header("Krzywa szybkości zwijania")]
    [Tooltip("X = waga ryby, Y = mnożnik prędkości (0-1).")]
    [SerializeField] private AnimationCurve krzywaPrędkości = AnimationCurve.Linear(0f, 1f, 40f, 0.1f);
    [SerializeField] private float maxWagaRyby = 40f;

    // ============================================================
    // 🎯 SKILL CHECK (SkillCheckSystem)
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🎯 SKILL CHECK")]
    [Header("═══════════════════════════════════════")]
    [Tooltip("Prędkość igły (pikseli/sek).")]
    [SerializeField] private float prędkośćIgły = 300f;
    [Tooltip("Szerokość dobrej strefy (w % paska, 0-1).")]
    [SerializeField] private float szerokośćDobrejStrefy = 0.25f;
    [Tooltip("Szerokość idealnej strefy (w % dobrej strefy, 0-1).")]
    [SerializeField] private float szerokośćIdealnejStrefy = 0.2f;
    [Tooltip("Czas na kliknięcie (sekundy). 0 = bez limitu.")]
    [SerializeField] private float limitCzasu = 3f;
    [Tooltip("Klawisz do kliknięcia.")]
    [SerializeField] private Key klawiszSkillCheck = Key.Space;
    [Tooltip("Czy skill check ma być aktywny podczas holu ryby?")]
    [SerializeField] private bool skillCheckWłączony = true;
    [Tooltip("Minimalna trudność skill checka (0-1).")]
    [SerializeField] private float minTrudność = 0.2f;
    [Tooltip("Maksymalna trudność skill checka (0-1).")]
    [SerializeField] private float maxTrudność = 0.7f;
    [Tooltip("Szansa (%) na pojawienie się skill checka w ciągu minuty. Np. 60 = średnio 0.6/min, 500 = średnio 5/min.")]
    [SerializeField] [Range(0f, 3000f)] private float szansaSkillCheckNaMinutę = 600f;
    [Tooltip("Bonus do prędkości zwijania za Dobry skill check (mnożnik).")]
    [SerializeField] private float bonusZaDobry = 1.3f;
    [Tooltip("Bonus do prędkości zwijania za Idealny skill check (mnożnik).")]
    [SerializeField] private float bonusZaIdealny = 1.8f;
    [Tooltip("Czas trwania bonusu po udanym skill checku (sekundy).")]
    [SerializeField] private float czasTrwaniaBonus = 1.5f;
    [Tooltip("Czy po nieudanym skill checku (miss/czas) ryba ucieka?")]
    [SerializeField] private bool porażkaTraciRybe = true;

    // ============================================================
    // 🎵 MUZYKA W TLE
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("🎵 MUZYKA W TLE")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private AudioClip muzykaWTle;
    [SerializeField] [Range(0f, 1f)] private float głośnośćMuzyki = 0.5f;
    private AudioSource muzykaSource;

    // ============================================================
    // ⏸️ PAUZA I DEBUG
    // ============================================================

    [Header("═══════════════════════════════════════")]
    [Header("⏸️ PAUZA I DEBUG")]
    [Header("═══════════════════════════════════════")]
    [SerializeField] private Key klawiszPauzy = Key.Escape;
    [SerializeField] private bool startZPauza = false;
    [SerializeField] private bool szczegółoweLogi = true;


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

        if (startZPauza)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
        }
    }

    void Start()
    {
        SyncAllSettings();
        UruchomMuzykęWTle();
        Log("🎮 GameManager uruchomiony!");
    }


    void Update()
    {
        if (Keyboard.current[klawiszPauzy].wasPressedThisFrame)
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
            PlayerMovement.Instance.SetSettings(predkoscRuchu, klawiszRzutu, prefabSpławika, minOdległośćRzutu, maxOdległośćRzutu, czasŁadowania);

        if (FishingSystem.Instance != null)
            FishingSystem.Instance.SetSettings(minCzasOczekiwania, maxCzasOczekiwania, awaryjnaMinWaga, awaryjnaMaxWaga, klawiszAkcji);

        if (FishSizes.Instance != null)
            FishSizes.Instance.SetChances(szansa1_5kg, szansa5_10kg, szansa10_20kg, szansa20_30kg, szansa30_40kg);

        if (ReelInSystem.Instance != null)
            ReelInSystem.Instance.SetSettings(klawiszZwijania, bazowaPrędkośćZwijania, prędkośćZwijaniaZRyba, krzywaPrędkości, maxWagaRyby);

        if (ReelInSystem.Instance != null)
            ReelInSystem.Instance.SetSkillCheckSettings(skillCheckWłączony, minTrudność, maxTrudność, szansaSkillCheckNaMinutę, bonusZaDobry, bonusZaIdealny, czasTrwaniaBonus, porażkaTraciRybe);

        if (SkillCheckSystem.Instance != null)
            SkillCheckSystem.Instance.SetSettings(prędkośćIgły, szerokośćDobrejStrefy, szerokośćIdealnejStrefy, limitCzasu, klawiszSkillCheck);

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
    // 🎵 MUZYKA W TLE
    // ============================================================

    private void UruchomMuzykęWTle()
    {
        if (muzykaWTle == null)
        {
            Log("🎵 Brak przypisanego AudioClip dla muzyki w tle.");
            return;
        }

        // Szukamy istniejącego AudioSource lub dodajemy nowy
        muzykaSource = GetComponent<AudioSource>();
        if (muzykaSource == null)
            muzykaSource = gameObject.AddComponent<AudioSource>();

        muzykaSource.clip = muzykaWTle;
        muzykaSource.volume = głośnośćMuzyki;
        muzykaSource.loop = true;
        muzykaSource.playOnAwake = false;
        muzykaSource.Play();

        Log($"🎵 Muzyka w tle uruchomiona (głośność: {głośnośćMuzyki:P0})");
    }

    // ============================================================
    // 📋 LOGOWANIE
    // ============================================================

    private void Log(string message)
    {
        if (szczegółoweLogi)
            Debug.Log($"<color=#FF69B4>[GameManager]</color> {message}");
    }

    void OnDestroy()

    {
        if (Instance == this) Instance = null;
    }
}
