using UnityEngine;

/// <summary>
/// Sztuczna inteligencja ryby podczas holu.
/// 
/// 1. Automatyczna ucieczka: jeśli ryba została zacięta zbyt blisko (≤ 3m),
///    automatycznie ucieka na 6m od gracza (płynnie, losowy kierunek).
/// 2. Losowe ruchy podczas zwijania: ryba chodzi na boki i czasem ucieka do tyłu.
/// 
/// Skrypt powinien być na tym samym obiekcie co ReelInSystem.
/// ReelInSystem woła FishAI podczas zwijania z rybą.
/// </summary>
public class FishAI : MonoBehaviour
{
    [Header("Ucieczka przy zbyt bliskim zacięciu")]
    [Tooltip("Jeśli ryba zacięta bliżej niż ta odległość, automatycznie ucieka.")]
    [SerializeField] private float minCatchDistance = 3f;
    [Tooltip("Odległość na jaką ucieka ryba.")]
    [SerializeField] private float escapeDistance = 6f;
    [Tooltip("Prędkość płynnej ucieczki po spłoszeniu (jednostki/s).")]
    [SerializeField] private float escapeSpeed = 8f;

    [Header("Losowe ruchy podczas holu")]
    [Tooltip("Maksymalne odchylenie boczne na sekundę (w jednostkach).")]
    [SerializeField] private float maxSideMovement = 1.5f;
    [Tooltip("Szansa na rozpoczęcie ucieczki do tyłu na sekundę (0-1).")]
    [SerializeField] private float backwardEscapeChance = 0.3f;
    [Tooltip("Maksymalna odległość ucieczki do tyłu.")]
    [SerializeField] private float maxBackwardEscape = 2f;
    [Tooltip("Prędkość płynnej ucieczki do tyłu (jednostki/s).")]
    [SerializeField] private float backwardEscapeSpeed = 3f;
    [Tooltip("Minimalny odstęp między ucieczkami do tyłu (sekundy).")]
    [SerializeField] private float backwardEscapeCooldown = 2f;

    [Header("Spięcie ryby")]
    [Tooltip("Szansa na spięcie ryby w ciągu minuty holu (0-100%). Np. 10 = 10% szansy na minutę, że ryba się spnie i ucieknie.")]
    [SerializeField] private float spinkaChancePercentPerMinute = 10f;

    // Referencje
    private ReelInSystem reelInSystem;
    private Transform playerTransform;

    // Stan
    private Vector3 startPosition;
    private bool hasEscaped = false; // Czy już uciekła po zbyt bliskim zacięciu
    private float sideOffset = 0f;
    private float sideChangeTimer = 0f;
    private float sideChangeInterval = 0f;
    private float backwardCooldownTimer = 0f;

    // Spięcie
    private float spinkaCooldownTimer = 0f;

    // Płynna ucieczka (wspólna dla spłoszenia i ucieczki do tyłu)
    private Vector3 smoothEscapeTarget;
    private bool isSmoothEscaping = false;
    private float currentEscapeSpeed = 0f;

    void Awake()
    {
        reelInSystem = GetComponent<ReelInSystem>();
        if (reelInSystem == null)
            Debug.LogError("FishAI wymaga ReelInSystem na tym samym obiekcie!");
    }

    void Start()
    {
        startPosition = transform.position;
        FindPlayer();
    }

    void Update()
    {
        // Szukamy gracza jeśli jeszcze nie znaleziony
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // Odświeżamy pozycję startową (gdzie gracz aktualnie stoi)
        startPosition = playerTransform.position;

        // Odliczamy cooldown ucieczki do tyłu
        if (backwardCooldownTimer > 0f)
            backwardCooldownTimer -= Time.deltaTime;

        // Odliczamy cooldown spięcia
        if (spinkaCooldownTimer > 0f)
            spinkaCooldownTimer -= Time.deltaTime;
    }

    // ===== PUBLICZNE =====

    /// <summary>
    /// Wołane przez ReelInSystem po zacięciu ryby.
    /// Sprawdza czy ryba jest za blisko i ew. uruchamia płynną ucieczkę.
    /// </summary>
    public void OnFishHooked(float fishWeight)
    {
        hasEscaped = false;

        // Sprawdź odległość od gracza
        float distanceToPlayer = Vector3.Distance(transform.position, startPosition);
        if (distanceToPlayer <= minCatchDistance)
        {
            // Losowy kierunek ucieczki (nie zawsze w prawo!)
            Vector3 randomDir = Random.insideUnitCircle.normalized;
            // Wymuszamy kierunek od gracza (jeśli randomDir wskazuje w stronę gracza, odwracamy)
            Vector3 awayFromPlayer = (transform.position - startPosition).normalized;
            if (awayFromPlayer.magnitude > 0.01f)
            {
                // Mieszamy kierunek od gracza z losowym odchyleniem
                randomDir = (awayFromPlayer + randomDir * 0.5f).normalized;
            }
            else
            {
                // Jeśli jesteśmy dokładnie na graczu, czysty losowy kierunek
                randomDir = Random.insideUnitCircle.normalized;
            }

            smoothEscapeTarget = startPosition + (Vector3)randomDir * escapeDistance;
            isSmoothEscaping = true;
            currentEscapeSpeed = escapeSpeed;
            hasEscaped = true;

            Debug.Log($"<color=#FF4500>🐟 Ryba spłoszona! Ucieka na {escapeDistance}m.</color>");
        }

        // Reset timera side change
        sideChangeTimer = 0f;
        sideChangeInterval = Random.Range(0.3f, 1.2f);
        sideOffset = 0f;
    }

    /// <summary>
    /// Wołane przez ReelInSystem w każdej klatce podczas zwijania z rybą.
    /// Modyfikuje pozycję zestawu (boczne ruchy i ucieczki do tyłu).
    /// </summary>
    public void UpdateFishMovement()
    {
        if (playerTransform == null) return;

        // --- Płynna ucieczka (spłoszenie lub ucieczka do tyłu) ---
        if (isSmoothEscaping)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                smoothEscapeTarget,
                currentEscapeSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, smoothEscapeTarget) < 0.05f)
                isSmoothEscaping = false;

            // Jeśli trwa ucieczka, nie wykonujemy innych ruchów (boki, zwijanie)
            return;
        }

        // --- Ruchy boczne ---
        sideChangeTimer += Time.deltaTime;
        if (sideChangeTimer >= sideChangeInterval)
        {
            sideChangeTimer = 0f;
            sideChangeInterval = Random.Range(0.3f, 1.2f);
            sideOffset = Random.Range(-maxSideMovement, maxSideMovement);
        }

        // Płynnie przesuwamy na bok
        Vector3 rightDir = Vector3.Cross((startPosition - transform.position).normalized, Vector3.forward).normalized;
        if (rightDir.magnitude < 0.01f)
            rightDir = Vector3.right;

        Vector3 targetSidePos = transform.position + rightDir * sideOffset * Time.deltaTime;
        // Nie pozwalamy oddalić się zbyt daleko na boki od linii prostej
        float maxSideDeviation = 3f;
        Vector3 toStart = targetSidePos - startPosition;
        float forwardDist = Vector3.Dot(toStart, (startPosition - transform.position).normalized);
        Vector3 projectedPos = startPosition + (startPosition - transform.position).normalized * forwardDist;
        float sideDeviation = Vector3.Distance(targetSidePos, projectedPos);

        if (sideDeviation <= maxSideDeviation)
            transform.position = targetSidePos;

        // --- Ucieczka do tyłu ---
        if (backwardCooldownTimer <= 0f && Random.value < backwardEscapeChance * Time.deltaTime)
        {
            float escapeAmount = Random.Range(0.5f, maxBackwardEscape);
            Vector3 escapeDir = (transform.position - startPosition).normalized;
            if (escapeDir.magnitude < 0.01f)
                escapeDir = -transform.right;

            smoothEscapeTarget = transform.position + escapeDir * escapeAmount;
            isSmoothEscaping = true;
            currentEscapeSpeed = backwardEscapeSpeed;
            backwardCooldownTimer = backwardEscapeCooldown;

            Debug.Log($"<color=#FFA500>🐟 Ryba ucieka! Cel: {escapeAmount:F1}m do tyłu.</color>");
        }

        // --- Spięcie ryby (losowe, ryba ucieka definitywnie) ---
        // Przeliczamy % na minutę na szansę na sekundę
        float spinkaChancePerSecond = (spinkaChancePercentPerMinute / 100f) / 60f;
        if (spinkaCooldownTimer <= 0f && Random.value < spinkaChancePerSecond * Time.deltaTime)
        {
            Debug.Log($"<color=#FF4500>⚡ SPINKA! Ryba się spięła i uciekła!</color>");

            // Przywracamy sprite spławika
            Bobber bobber = GetComponent<Bobber>();
            if (bobber != null)
                bobber.ResetToBobberSprite();

            // Informujemy FishEscapeZone że ryba uciekła (blokuje fałszywy komunikat "uciekła na mieliznę")
            FishEscapeZone escapeZone = FindAnyObjectByType<FishEscapeZone>();
            if (escapeZone != null)
                escapeZone.OnFishResolved();

            // Resetujemy stan w ReelInSystem
            if (reelInSystem != null)
                reelInSystem.ResetFish();

            // Resetujemy stan w FishingSystem
            if (FishingSystem.Instance != null)
                FishingSystem.Instance.OnFishLanded();

            // Niszczymy spławik (ryba uciekła)
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Czy ryba uciekła po zbyt bliskim zacięciu.
    /// </summary>
    public bool HasEscaped => hasEscaped;

    // ===== PRYWATNE =====

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }
}
