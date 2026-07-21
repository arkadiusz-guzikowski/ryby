using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Odpowiada za zwijanie spławika (Bobber) z powrotem do gracza.
/// 
/// Klawisze:
/// - R (przytrzymaj): zwijanie spławika
/// - R (puść): przerwanie zwijania
/// 
/// Jeśli na haczyku jest ryba, zwijanie jest wolniejsze (im cięższa ryba, tym wolniej).
/// Po dotarciu do celu: jeśli była ryba → informuje FishingSystem o złowieniu i niszczy spławik.
/// Jeśli zwijanie zostało przerwane z rybą → resetuje stan w FishingSystem.
/// 
/// Skrypt powinien być na obiekcie spławika (Bobber).
/// </summary>
public class ReelInSystem : MonoBehaviour
{
    public static ReelInSystem Instance;

    [Header("Zwijanie")]
    [SerializeField] private Key reelKey = Key.R;
    [SerializeField] private float baseReelSpeed = 5f;
    [Tooltip("Prędkość zwijania podczas holu ryby (gdy ryba na haczyku).")]
    [SerializeField] private float reelSpeedWithFish = 3f;

    [Header("Krzywa szybkości zwijania")]
    [Tooltip("X = waga ryby (0 do maxFishWeight), Y = mnożnik prędkości (0-1).")]
    [SerializeField] private AnimationCurve speedMultiplierCurve = AnimationCurve.Linear(0f, 1f, 40f, 0.1f);
    [SerializeField] private float maxFishWeight = 40f;

    // FishAI na tym samym obiekcie (lub znaleziony automatycznie)
    private FishAI fishAI;

    [Header("Logowanie")]
    [SerializeField] private float distanceLogInterval = 0.5f;
    private float distanceLogTimer = 0f;

    // Gracz (do niego zwijamy)
    private Transform playerTransform;

    // Stan zwijania
    private bool isReeling = false;

    // Waga ryby na haczyku (0 = brak ryby)
    private float fishWeight = 0f;
    private bool hasFish = false;

    // Flaga: czy zwijanie zostało przerwane gdy była ryba
    private bool wasReelingWithFish = false;

    // Blokada: po dotarciu do celu, blokujemy ponowne zwijanie dopóki gracz nie puści R
    private bool waitingForRelease = false;

    void Awake()
    {
        Instance = this;

        // Automatycznie znajdź FishAI na tym samym obiekcie lub w scenie
        if (fishAI == null)
            fishAI = GetComponent<FishAI>();
        if (fishAI == null)
            fishAI = FindAnyObjectByType<FishAI>();
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // Obsługa blokady po dotarciu do celu
        if (waitingForRelease)
        {
            if (!Keyboard.current[reelKey].isPressed)
                waitingForRelease = false;
            return; // Nie reagujemy na R dopóki gracz nie puści klawisza
        }

        // Rozpocznij zwijanie (R przytrzymane, a nie zwijamy)
        if (Keyboard.current[reelKey].isPressed && !isReeling)
        {
            StartReeling();
        }

        // Zatrzymaj zwijanie (R puszczone, a zwijamy)
        if (!Keyboard.current[reelKey].isPressed && isReeling)
        {
            StopReeling();
        }

        // Wykonaj zwijanie
        if (isReeling)
        {
            ReelIn();
        }
    }

    // ===== PUBLICZNE =====

    /// <summary>
    /// Ustawia wagę ryby na haczyku. Wołane przez FishingSystem po zacięciu.
    /// </summary>
    public void SetFishWeight(float weight)
    {
        fishWeight = weight;
        hasFish = true;

        // Informujemy FishAI o zacięciu ryby
        if (fishAI != null)
            fishAI.OnFishHooked(fishWeight);
    }

    public bool IsReeling => isReeling;

    // ===== PRYWATNE =====

    private float GetCurrentReelSpeed()
    {
        if (!hasFish || fishWeight <= 0f)
            return baseReelSpeed;

        float t = Mathf.Clamp01(fishWeight / maxFishWeight);
        float multiplier = Mathf.Clamp01(speedMultiplierCurve.Evaluate(t * maxFishWeight));
        return Mathf.Lerp(0f, reelSpeedWithFish, multiplier);
    }

    private void StartReeling()
    {
        isReeling = true;
        distanceLogTimer = 0f;

        if (hasFish)
        {
            wasReelingWithFish = true;
            Debug.Log("<color=#87CEEB>🎣 Zwijanie zestawu (ryba na haczyku)...</color>");
        }
        else
        {
            Debug.Log("<color=#87CEEB>🎣 Zwijanie zestawu...</color>");
        }
    }

    private void StopReeling()
    {
        if (!isReeling) return;

        isReeling = false;

        float remainingDistance = Vector3.Distance(transform.position, playerTransform.position);
        Debug.Log($"<color=#FFA500>⏸️ Przerwano zwijanie. Pozostało: <b>{remainingDistance:F1}m</b></color>");

        // Jeśli przerwano zwijanie z rybą na haczyku, resetujemy stan łowienia
        if (wasReelingWithFish)
        {
            Debug.Log("<color=#FF4500>🐟 Ryba uciekła! Możesz zacząć łowić od nowa.</color>");

            // Przywracamy sprite spławika (ryba uciekła)
            Bobber bobber = GetComponent<Bobber>();
            if (bobber != null)
                bobber.ResetToBobberSprite();

            NotifyFishingSystemFishLost();
        }
    }

    private void ReelIn()
    {
        float currentSpeed = GetCurrentReelSpeed();

        // Jeśli jest ryba, najpierw symulujemy jej ruchy (boki, ucieczki)
        if (hasFish && fishAI != null)
            fishAI.UpdateFishMovement();

        // Zwijamy spławik do gracza
        transform.position = Vector3.MoveTowards(
            transform.position,
            playerTransform.position,
            currentSpeed * Time.deltaTime
        );

        // Logowanie dystansu
        distanceLogTimer += Time.deltaTime;
        if (distanceLogTimer >= distanceLogInterval)
        {
            distanceLogTimer = 0f;
            float remainingDistance = Vector3.Distance(transform.position, playerTransform.position);
            Debug.Log($"<color=#87CEEB>📏 Pozostało: <b>{remainingDistance:F1}m</b></color>");
        }

        // Dotarcie do celu
        if (Vector3.Distance(transform.position, playerTransform.position) < 0.01f)
        {
            transform.position = playerTransform.position;
            isReeling = false;

            if (hasFish)
            {
                OnFishCaught();
            }
            else
            {
                Debug.Log("<color=#87CEEB>✅ Zestaw zwinięty! Łowienie anulowane.</color>");
                // Zwinięcie całej żyłki bez ryby = anulowanie łowienia
                if (FishingSystem.Instance != null)
                    FishingSystem.Instance.ResetFishing();
            }

            // Reset stanu
            hasFish = false;
            fishWeight = 0f;
            wasReelingWithFish = false;

            // Blokada: gracz musi puścić R zanim będzie mógł znowu zwijać
            waitingForRelease = true;

            // Zniszcz spławik po dotarciu do celu
            DestroyBobber();
        }
    }

    private void OnFishCaught()
    {
        string fishEmoji = fishWeight >= 20f ? "🐋" : "🐟";
        Debug.Log($"<color=#00FF00>═══════════════════════════════════</color>");
        Debug.Log($"<color=#00FF00>  {fishEmoji}  ZŁOWIONO RYBĘ!  {fishEmoji}</color>");
        Debug.Log($"<color=#00FF00>  Waga: <b>{fishWeight:F1} kg</b></color>");
        Debug.Log($"<color=#00FF00>═══════════════════════════════════</color>");

        // Informujemy FishingSystem
        if (FishingSystem.Instance != null)
        {
            FishingSystem.Instance.InvokeFishCaught(fishWeight);
            FishingSystem.Instance.OnFishLanded();
        }
    }

    private void NotifyFishingSystemFishLost()
    {
        hasFish = false;
        fishWeight = 0f;
        wasReelingWithFish = false;

        if (FishingSystem.Instance != null)
            FishingSystem.Instance.OnFishLanded();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    /// <summary>
    /// Niszczy spławik po dotarciu do celu (ryba złowiona lub anulowano).
    /// </summary>
    private void DestroyBobber()
    {
        // Resetuj Instancę jeśli to ten spławik
        if (Bobber.Instance != null && Bobber.Instance.gameObject == gameObject)
        {
            // Nie ustawiamy null, bo może być potrzebny w innych skryptach
        }

        // Niszczymy cały obiekt spławika
        Destroy(gameObject);
    }
}
