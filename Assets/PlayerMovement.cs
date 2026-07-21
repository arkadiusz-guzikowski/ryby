using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ruch")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rzut")]
    [SerializeField] private Key castKey = Key.Q;
    [SerializeField] private GameObject bobberPrefab;

    private bool hasStarted = false;
    private GameObject currentBobber;
    private SpriteRenderer playerSprite;

    void Awake()
    {
        playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite != null)
            playerSprite.enabled = false;
    }

    void Start()
    {
        Debug.Log("<color=#00FF00>══════════════════════════════════════════════════════════════════</color>");
        Debug.Log("<color=#00FF00>  🎣 RYBY — Gra wędkarska  |  📍 Kliknij LPM, by ustawić pozycję  |  📖 Strzałki:ruch | LPM/Q:rzut | SPACJA: TNIJ!!! | R:zwijaj</color>");
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

        // --- RZUT (LPM lub Q) ---
        if (Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current[castKey].wasPressedThisFrame)
        {
            CastFishingRod();
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
            Debug.Log("<color=#00FF00>🎣 Kliknij LPM lub Q, żeby rzucić spławik!</color>");
        }
    }

    private void CastFishingRod()
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

        // Sprawdź czy cel jest w water zone
        WaterZoneChecker waterZone = FindAnyObjectByType<WaterZoneChecker>();
        if (waterZone == null || !waterZone.IsPointInWater(mouseWorldPos))
        {
            Debug.Log("<color=#FFA500>⛔ Rzut poza wodę! Powtórz rzut w obrębie jeziora.</color>");
            return;
        }

        float distance = Vector3.Distance(transform.position, mouseWorldPos);
        Debug.Log($"<color=#00BFFF>🎣 Rzut! Odległość: <b>{distance:F1}m</b></color>");

        // Spawn spławika
        if (bobberPrefab != null)
        {
            currentBobber = Instantiate(bobberPrefab, transform.position, Quaternion.identity);
            Debug.Log($"<color=#00BFFF>🆕 Spławik utworzony na pozycji gracza: {transform.position}</color>");
            Bobber bobber = currentBobber.GetComponent<Bobber>();
            if (bobber != null)
            {
                bobber.CastTo(mouseWorldPos);
                Debug.Log($"<color=#00BFFF>📌 Spławik leci do: {mouseWorldPos}</color>");
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
}
