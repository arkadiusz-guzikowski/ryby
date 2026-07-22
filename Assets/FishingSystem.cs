using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Główny system łowienia.
/// 
/// Flow:
/// 1. Gracz rzuca spławik (LPM/Q) → spławik w wodzie → automatycznie zaczyna łowić
/// 2. Po 2-5s → "BRANIE!" → gracz naciska SPACJĘ → ryba na haczyku
/// 3. Gracz przytrzymuje R → zwijanie spławika do siebie
/// 4. Ryba dopływa → złowiona! Spławik znika.
/// 
/// Klawisze:
/// - SPACJA: zaciśnij rybę (gdy branie)
/// - R: zwijanie zestawu (obsługiwane przez ReelInSystem)
/// - LPM/Q: rzut spławikiem (obsługiwane przez PlayerMovement)
/// </summary>
public class FishingSystem : MonoBehaviour
{
    public static FishingSystem Instance;

    public enum FishingState { Idle, Waiting, Biting }
    private FishingState state = FishingState.Idle;
    private float timer = 0f;
    private float waitTime = 0f;

    public FishingState CurrentState => state;

    [Header("Czas oczekiwania na branie")]
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;

    [Header("Awaryjna waga ryby (gdy FishSizes nie istnieje)")]
    [SerializeField] private float fallbackMinWeight = 1f;
    [SerializeField] private float fallbackMaxWeight = 40f;

    [Header("Klawisz akcji")]
    [SerializeField] private Key actionKey = Key.Space;

    // Eventy
    public delegate void FishCaughtHandler(float weight);
    public event FishCaughtHandler OnFishCaught;

    public delegate void FishingStartedHandler();
    public event FishingStartedHandler OnFishingStarted;

    public delegate void BitingHandler();
    public event BitingHandler OnBiting;

    public delegate void FishHookedHandler(float weight);
    public event FishHookedHandler OnFishHooked;

    /// <summary>
    /// Flaga blokująca nowe łowienie dopóki ryba nie zostanie zwinięta.
    /// </summary>
    private bool hasHookedFish = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        switch (state)
        {
            case FishingState.Idle:
                HandleIdle();
                break;

            case FishingState.Waiting:
                HandleWaiting();
                break;

            case FishingState.Biting:
                HandleBiting();
                break;
        }
    }

    // ===== STANY =====

    private void HandleIdle()
    {
        // Jeśli jest ryba na haczyku, SPACJA anuluje (ryba ucieka)
        if (hasHookedFish)
        {
            if (Keyboard.current[actionKey].wasPressedThisFrame)
            {
                Debug.Log("<color=#FF4500>🐟 Anulowano - ryba uciekła!</color>");
                ResetFishing();
            }
            return;
        }

        // Sprawdź czy spławik jest w wodzie
        bool bobberInWater = Bobber.Instance != null && Bobber.Instance.IsInWater;

        // Rozpocznij łowienie (automatycznie lub przez SPACJĘ)
        if (bobberInWater && state == FishingState.Idle && !hasHookedFish)
        {
            // Automatycznie zaczynamy łowić, jeśli spławik w wodzie i nie mamy ryby na haczyku
            StartFishing();
        }
    }

    private void HandleWaiting()
    {
        // Jeśli zestaw jest zwijany, pauzujemy licznik
        if (IsReelingActive())
            return;

        timer += Time.deltaTime;
        if (timer >= waitTime)
        {
            Debug.Log("<color=#FFA500>🎯 BRANIE! Naciśnij SPACJĘ!</color>");
            state = FishingState.Biting;
            OnBiting?.Invoke();

            // Spławik "zanurza się" - staje się prawie niewidoczny
            if (Bobber.Instance != null)
                Bobber.Instance.SetBobberAlpha(0.1f);
        }
    }

    private void HandleBiting()
    {
        if (Keyboard.current[actionKey].wasPressedThisFrame)
        {
            float fishWeight = GetFishWeight();

            Debug.Log("<color=#FFD700>🎯 Ryba na haczyku! Przytrzymaj R, żeby zwijać!</color>");

            // Zmieniamy ikonę spławika na rybkę (z skalowaniem od wagi)
            Bobber currentBobber = FindAnyObjectByType<Bobber>();
            if (currentBobber != null)
            {
                currentBobber.ChangeToFishIcon(fishWeight);
            }
            else
            {
                Debug.LogWarning("Nie znaleziono Bobber w scenie!");
            }

            // Informujemy ReelInSystem o wadze ryby (wpłynie na szybkość zwijania)
            if (ReelInSystem.Instance != null)
                ReelInSystem.Instance.SetFishWeight(fishWeight);

            // Informujemy FishEscapeZone o zacięciu (ryba ucieknie jeśli spławik opuści wodę)
            FishEscapeZone escapeZone = FindAnyObjectByType<FishEscapeZone>();
            if (escapeZone != null)
                escapeZone.OnFishHooked();

            // Wywołujemy event zacięcia
            OnFishHooked?.Invoke(fishWeight);

            // Blokujemy nowe łowienie dopóki ryba nie zostanie zwinięta
            hasHookedFish = true;
            state = FishingState.Idle;
        }
    }

    // ===== METODY PUBLICZNE =====

    /// <summary>
    /// Rozpoczyna łowienie (automatycznie po wylądowaniu spławika).
    /// </summary>
    public void StartFishing()
    {
        if (state != FishingState.Idle || hasHookedFish)
            return;

        Debug.Log("<color=#00FF00>🎣 Zaczynasz łowić! Czekaj na branie...</color>");
        state = FishingState.Waiting;
        timer = 0f;
        waitTime = Random.Range(minWaitTime, maxWaitTime);

        // Jeśli jesteśmy w hotspocie, skracamy czas oczekiwania
        if (FishSizes.Instance != null && FishSizes.Instance.IsInHotSpot)
        {
            waitTime *= FishSizes.Instance.HotSpotWaitTimeMultiplier;
            Debug.Log($"<color=#00FF00>🔥 HotSpot: czas oczekiwania x{FishSizes.Instance.HotSpotWaitTimeMultiplier}</color>");
        }

        OnFishingStarted?.Invoke();
    }

    /// <summary>
    /// ReelInSystem woła to gdy ryba dotrze do gracza (zostanie zwinięta).
    /// </summary>
    public void OnFishLanded()
    {
        hasHookedFish = false;
    }

    /// <summary>
    /// ReelInSystem woła to gdy ryba została pomyślnie zwinięta.
    /// </summary>
    public void InvokeFishCaught(float weight)
    {
        OnFishCaught?.Invoke(weight);
    }

    /// <summary>
    /// Resetuje cały system łowienia do stanu początkowego.
    /// </summary>
    public void ResetFishing()
    {
        state = FishingState.Idle;
        hasHookedFish = false;
        timer = 0f;
        waitTime = 0f;
    }

    // ===== METODY POMOCNICZE =====

    private bool IsReelingActive()
    {
        if (ReelInSystem.Instance != null)
            return ReelInSystem.Instance.IsReeling;
        return false;
    }

    private float GetFishWeight()
    {
        if (FishSizes.Instance != null)
            return FishSizes.Instance.GetRandomCarpWeight();
        return Random.Range(fallbackMinWeight, fallbackMaxWeight);
    }

    // ============================================================
    // ⚙️ USTAWIENIA Z GAMEMANAGERA
    // ============================================================

    /// <summary>
    /// Synchronizuje ustawienia z GameManagera.
    /// </summary>
    public void SetSettings(float minWait, float maxWait, float fallbackMin, float fallbackMax, Key action)
    {
        minWaitTime = minWait;
        maxWaitTime = maxWait;
        fallbackMinWeight = fallbackMin;
        fallbackMaxWeight = fallbackMax;
        actionKey = action;
    }

}
