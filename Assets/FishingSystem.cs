using UnityEngine;
using UnityEngine.InputSystem;

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

    // Eventy - inne skrypty mogą się na nie subskrybować
    public delegate void FishCaughtHandler(float weight);
    public event FishCaughtHandler OnFishCaught;

    public delegate void FishingStartedHandler();
    public event FishingStartedHandler OnFishingStarted;

    public delegate void BitingHandler();
    public event BitingHandler OnBiting;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        switch (state)
        {
            case FishingState.Idle:
                if (Keyboard.current[actionKey].wasPressedThisFrame)
                {
                    Debug.Log("Zaczynasz łowić! Czekaj na branie...");
                    state = FishingState.Waiting;
                    timer = 0f;
                    waitTime = Random.Range(minWaitTime, maxWaitTime);
                    OnFishingStarted?.Invoke();
                }
                break;

            case FishingState.Waiting:
                timer += Time.deltaTime;
                if (timer >= waitTime)
                {
                    Debug.Log("BRANIE! Naciśnij SPACJĘ!");
                    state = FishingState.Biting;
                    OnBiting?.Invoke();
                }
                break;

            case FishingState.Biting:
                if (Keyboard.current[actionKey].wasPressedThisFrame)
                {
                    float fishWeight = 0f;

                    if (FishSizes.Instance != null)
                        fishWeight = FishSizes.Instance.GetRandomCarpWeight();
                    else
                        fishWeight = Random.Range(fallbackMinWeight, fallbackMaxWeight);

                    // Wywołujemy event - ktoś inny może zareagować (np. FishWeightDisplay)
                    OnFishCaught?.Invoke(fishWeight);

                    state = FishingState.Idle;
                }
                break;
        }
    }
}
