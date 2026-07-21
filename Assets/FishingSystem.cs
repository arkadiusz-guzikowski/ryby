using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Główny system łowienia.
/// Odpowiada za: rozpoczynanie łowienia (SPACJA), oczekiwanie na branie, zacięcie ryby.
/// Komunikuje się z ReelInSystem przez metody publiczne.
/// 
/// Klawisze:
/// - SPACJA: rozpocznij łowienie / zaciśnij rybę / anuluj (gdy ryba na haczyku)
/// - R: zwijanie zestawu (obsługiwane przez ReelInSystem)
/// - Q: rzut / mierzenie odległości (obsługiwane przez PlayerMovement)
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

        // Rozpocznij łowienie
        if (Keyboard.current[actionKey].wasPressedThisFrame)
        {
            Debug.Log("<color=#00FF00>🎣 Zaczynasz łowić! Czekaj na branie...</color>");
            state = FishingState.Waiting;
            timer = 0f;
            waitTime = Random.Range(minWaitTime, maxWaitTime);
            OnFishingStarted?.Invoke();
        }
    }

    private void HandleWaiting()
    {
        // Jeśli zestaw jest zwijany, pauzujemy licznik — gracz może zwijać
        // kawałek i dalej łowić. Dopiero zwinięcie całej żyłki anuluje łowienie.
        if (IsReelingActive())
            return;

        timer += Time.deltaTime;
        if (timer >= waitTime)
        {
            Debug.Log("<color=#FFA500>🎯 BRANIE! Naciśnij SPACJĘ!</color>");
            state = FishingState.Biting;
            OnBiting?.Invoke();
        }
    }

    private void HandleBiting()
    {
        if (Keyboard.current[actionKey].wasPressedThisFrame)
        {
            float fishWeight = GetFishWeight();

            Debug.Log("<color=#FFD700>🎯 HOOKED! Zwijaj (R)!</color>");

            // Informujemy ReelInSystem o wadze ryby (wpłynie na szybkość zwijania)
            if (ReelInSystem.Instance != null)
                ReelInSystem.Instance.SetFishWeight(fishWeight);

            // Wywołujemy event zacięcia
            OnFishHooked?.Invoke(fishWeight);

            // Blokujemy nowe łowienie dopóki ryba nie zostanie zwinięta
            hasHookedFish = true;
            state = FishingState.Idle;
        }
    }

    // ===== METODY PUBLICZNE =====

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
}
