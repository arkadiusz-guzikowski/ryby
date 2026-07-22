using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Ruch")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rzut")]
    [SerializeField] private Key castKey = Key.Q;
    [SerializeField] private GameObject bobberPrefab;

    [Header("Siła rzutu")]
    [Tooltip("Minimalna odległość rzutu (przy szybkim kliknięciu).")]
    [SerializeField] private float minCastDistance = 2f;
    [Tooltip("Maksymalna odległość rzutu (przy pełnym naładowaniu).")]
    [SerializeField] private float maxCastDistance = 15f;
    [Tooltip("Czas ładowania pełnej siły (sekundy).")]
    [SerializeField] private float maxChargeTime = 2f;

    private bool hasStarted = false;
    private GameObject currentBobber;
    private SpriteRenderer playerSprite;

    // Stan ładowania rzutu
    private bool isCharging = false;
    private float chargeTimer = 0f;

    void Awake()
    {
        Instance = this;
        playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite != null)
            playerSprite.enabled = false;
    }

    void Start()
    {
        Debug.Log("<color=#00FF00>══════════════════════════════════════════════════════════════════</color>");
        Debug.Log("<color=#00FF00>  🎣 RYBY — Gra wędkarska  |  📍 Kliknij LPM, by ustawić pozycję  |  📖 Strzałki:ruch | Przytrzymaj LPM/Q:ładuj siłę i rzuć | SPACJA: TNIJ!!! | R:zwijaj</color>");
        Debug.Log("<color=#00FF00>══════════════════════════════════════════════════════════════════</color>");
    }

    void Update()
    {
        // --- Ustawianie pozycji startowej myszką ---
        if (!hasStarted)
        {
            HandleStartPlacement();
            return;
        }

        // --- RUCH (strzałki) ---
        Vector2 movement = Vector2.zero;

        if (Keyboard.current.upArrowKey.isPressed)
            movement.y = 1;
        else if (Keyboard.current.downArrowKey.isPressed)
            movement.y = -1;

        if (Keyboard.current.leftArrowKey.isPressed)
            movement.x = -1;
        else if (Keyboard.current.rightArrowKey.isPressed)
            movement.x = 1;

        transform.Translate(movement.normalized * moveSpeed * Time.deltaTime);

        // --- RZUT Z ŁADOWANIEM (LPM lub Q) ---
        bool isCastKeyDown = Mouse.current.leftButton.isPressed || Keyboard.current[castKey].isPressed;
        bool isCastKeyPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current[castKey].wasPressedThisFrame;
        bool isCastKeyReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame || Keyboard.current[castKey].wasReleasedThisFrame;

        if (isCastKeyPressedThisFrame && !isCharging && currentBobber == null)
        {
            // Rozpocznij ładowanie siły
            isCharging = true;
            chargeTimer = 0f;
            Debug.Log("<color=#00BFFF>🎣 Ładowanie siły rzutu... (puść, by rzucić)</color>");
        }

        if (isCharging)
        {
            if (isCastKeyDown)
            {
                // Ładujemy siłę (im dłużej trzymamy, tym większa)
                chargeTimer += Time.deltaTime;
                if (chargeTimer > maxChargeTime)
                    chargeTimer = maxChargeTime;

                // Wizualne wskazanie siły — pasek postępu
                float chargePercent = chargeTimer / maxChargeTime;
                int progressBars = Mathf.FloorToInt(chargePercent * 10f);
                string progressStr = new string('█', progressBars) + new string('░', 10 - progressBars);

                if (chargePercent >= 1f)
                {
                    Debug.Log($"<color=#FF4500>⚡ Maksymalna siła! {progressStr} 100%</color>");
                }
            }

            if (isCastKeyReleasedThisFrame)
            {
                // Puściliśmy klawisz — wykonaj rzut z naładowaną siłą
                isCharging = false;
                float chargePercent = Mathf.Clamp01(chargeTimer / maxChargeTime);
                CastFishingRodWithPower(chargePercent);
            }
        }
    }

    private void HandleStartPlacement()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(Mouse.current.position.x.ReadValue(),
                            Mouse.current.position.y.ReadValue(),
                            0f)
            );
            mouseWorldPos.z = 0f;
            transform.position = mouseWorldPos;
            hasStarted = true;

            // Pokazujemy gracza
            if (playerSprite != null)
                playerSprite.enabled = true;

            Debug.Log($"<color=#00FF00>📍 Gracz ustawiony na pozycji: {mouseWorldPos}</color>");
            Debug.Log("<color=#00FF00>🎣 Przytrzymaj LPM lub Q, żeby naładować siłę i rzucić spławik!</color>");
        }
    }

    /// <summary>
    /// Rzuca spławik w kierunku myszki z siłą zależną od czasu przytrzymania.
    /// </summary>
    /// <param name="powerPercent">0 = minimalna siła, 1 = maksymalna</param>
    private void CastFishingRodWithPower(float powerPercent)
    {
        // Blokada: jeśli spławik już istnieje (w wodzie lub leci), nie można rzucić ponownie
        if (currentBobber != null)
        {
            Debug.Log("<color=#FFA500>⛔ Najpierw zwiń zestaw (R) zanim rzucisz ponownie!</color>");
            return;
        }

        // Pobierz pozycję myszki w świecie gry
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Mouse.current.position.x.ReadValue(),
                        Mouse.current.position.y.ReadValue(),
                        0f)
        );
        mouseWorldPos.z = 0f;

        // Oblicz kierunek od gracza do myszki
        Vector3 direction = (mouseWorldPos - transform.position).normalized;
        if (direction.magnitude < 0.01f)
            direction = Vector3.right; // awaryjnie, jeśli myszka na graczu

        // Oblicz odległość rzutu na podstawie naładowanej siły
        float castDistance = Mathf.Lerp(minCastDistance, maxCastDistance, powerPercent);

        // Oblicz docelową pozycję
        Vector3 targetPosition = transform.position + direction * castDistance;

        // Sprawdź czy cel jest w water zone
        WaterZoneChecker waterZone = FindAnyObjectByType<WaterZoneChecker>();
        if (waterZone == null || !waterZone.IsPointInWater(targetPosition))
        {
            Debug.Log($"<color=#FFA500>⛔ Cel poza wodą! Rzut anulowany. (siła: {powerPercent * 100f:F0}%)</color>");
            return;
        }

        Debug.Log($"<color=#00BFFF>🎣 Rzut! Siła: <b>{powerPercent * 100f:F0}%</b>, Odległość: <b>{castDistance:F1}m</b>, Kierunek: {direction}</color>");

        // Spawn spławika
        if (bobberPrefab != null)
        {
            currentBobber = Instantiate(bobberPrefab, transform.position, Quaternion.identity);
            Debug.Log($"<color=#00BFFF>🆕 Spławik utworzony na pozycji gracza: {transform.position}</color>");
            Bobber bobber = currentBobber.GetComponent<Bobber>();
            if (bobber != null)
            {
                bobber.CastTo(targetPosition);
                Debug.Log($"<color=#00BFFF>📌 Spławik leci do: {targetPosition}</color>");
            }
            else
            {
                Debug.LogError("Prefab spławika nie ma komponentu Bobber!");
            }
        }
        else
        {
            Debug.LogError("Brak prefaba spławika! Przypisz BobberPrefab w Inspectorze.");
        }
    }

    /// <summary>
    /// Czy gracz już ustawił swoją pozycję startową.
    /// </summary>
    public bool HasStarted => hasStarted;

    /// <summary>
    /// Czyści referencję do spławika (gdy spławik został zniszczony przez FishingSystem).
    /// </summary>
    public void ClearBobberReference()
    {
        currentBobber = null;
    }

    // ============================================================
    // ⚙️ USTAWIENIA Z GAMEMANAGERA
    // ============================================================

    /// <summary>
    /// Synchronizuje ustawienia z GameManagera.
    /// </summary>
    public void SetSettings(float speed, Key cast, GameObject bobber, float minDist, float maxDist, float chargeTime)
    {
        moveSpeed = speed;
        castKey = cast;
        if (bobber != null) bobberPrefab = bobber;
        minCastDistance = minDist;
        maxCastDistance = maxDist;
        maxChargeTime = chargeTime;
    }
}
