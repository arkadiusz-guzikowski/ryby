using UnityEngine;

/// <summary>
/// Reprezentuje spławik/zestaw w wodzie.
/// PlayerMovement spawnuje go po rzucie (LPM lub Q).
/// Zawiera ReelInSystem i FishAI.
/// 
/// Rzut: spławik leci płynnie do celu, po drodze skaluje się (większy = wyżej).
/// </summary>
public class Bobber : MonoBehaviour
{
    public static Bobber Instance { get; private set; }

    [Header("Rzut")]
    [Tooltip("Prędkość lotu spławika podczas rzutu.")]
    [SerializeField] private float castSpeed = 15f;

    [Header("Skalowanie podczas lotu")]
    [Tooltip("Maksymalna skala podczas lotu (1 = normalna).")]
    [SerializeField] private float maxFlyScale = 1.8f;
    [Tooltip("Wysokość paraboli lotu (0 = brak, większa = wyższy łuk).")]
    [SerializeField] private float arcHeight = 3f;

    [Header("Dźwięki")]
    [SerializeField] private AudioClip dźwiękPlusk;
    [SerializeField] private AudioSource audioSource;

    [Header("Ikona ryby po zacięciu")]
    [Tooltip("Sprite rybki, który pojawi się po naciśnięciu SPACJI (zaciśnięcie ryby).")]
    [SerializeField] private Sprite fishIcon;
    [Tooltip("Kolor ikony rybki (możesz ustawić przezroczystość przez alpha).")]
    [SerializeField] private Color fishIconColor = Color.white;
    [Header("Skalowanie ikony od wagi ryby")]
    [Tooltip("Skala przy minimalnej wadze ryby (np. 1kg).")]
    [SerializeField] private float minFishScale = 0.5f;
    [Tooltip("Skala przy maksymalnej wadze ryby (np. 40kg).")]
    [SerializeField] private float maxFishScale = 2.5f;
    [Tooltip("Maksymalna waga ryby do skalowania (powyżej tej wagi skala = maxFishScale).")]
    [SerializeField] private float maxWeightForScale = 40f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flyProgress = 0f; // 0 = start, 1 = cel
    private bool isFlying = false;
    private bool isInWater = false;
    private Vector3 baseScale; // skala ustawiona w prefabie/Inspectorze
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite; // oryginalny sprite spławika
    private Color originalColor; // oryginalny kolor spławika

    void Awake()
    {
        Instance = this;
        baseScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            originalColor = spriteRenderer.color;
        }

        // Automatycznie znajdź AudioSource jeśli nie przypisany
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }


    void Update()
    {
        if (isFlying)
        {
            flyProgress += castSpeed * Time.deltaTime / Vector3.Distance(startPosition, targetPosition);

            if (flyProgress >= 1f)
            {
                flyProgress = 1f;
                transform.position = targetPosition;
                transform.localScale = baseScale;
                isFlying = false;
                isInWater = true;
                Debug.Log($"<color=#00BFFF>🎣 Spławik w wodzie! Pozycja: {targetPosition}</color>");

                // Dźwięk plusku
                if (dźwiękPlusk != null && audioSource != null)
                    audioSource.PlayOneShot(dźwiękPlusk);

                return;

            }

            // Pozycja: lerp po linii prostej + parabola w górę
            Vector3 basePos = Vector3.Lerp(startPosition, targetPosition, flyProgress);
            float arc = Mathf.Sin(flyProgress * Mathf.PI) * arcHeight;
            transform.position = basePos + Vector3.up * arc;

            // Skalowanie: największy w połowie lotu
            float scaleProgress = Mathf.Sin(flyProgress * Mathf.PI); // 0 → 1 → 0
            float currentScale = 1f + (maxFlyScale - 1f) * scaleProgress;
            transform.localScale = new Vector3(currentScale, currentScale, 1f);
        }
    }

    /// <summary>
    /// Wyrzuca spławik w docelowe miejsce z płynnym lotem.
    /// </summary>
    public void CastTo(Vector3 target)
    {
        startPosition = transform.position;
        targetPosition = target;
        flyProgress = 0f;
        isFlying = true;
        isInWater = false;
        transform.localScale = baseScale;

        Debug.Log($"<color=#00BFFF>🎣 Rzut spławikiem! Cel: {target}</color>");
    }

    public bool IsInWater => isInWater;
    public bool IsFlying => isFlying;

    /// <summary>
    /// Zmienia przezroczystość spławika (symulacja zanurzenia).
    /// </summary>
    public void SetBobberAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// Zmienia sprite spławika na ikonę rybki (po zaciśnięciu ryby).
    /// Skaluje ikonę w zależności od wagi ryby.
    /// </summary>
    /// <param name="fishWeight">Waga ryby w kg (wpływa na skalę ikony).</param>
    public void ChangeToFishIcon(float fishWeight)
    {
        if (spriteRenderer != null && fishIcon != null)
        {
            spriteRenderer.sprite = fishIcon;
            // Ustawiamy kolor i przezroczystość z Inspectora
            spriteRenderer.color = fishIconColor;

            // Skalowanie od wagi ryby
            float t = Mathf.Clamp01(fishWeight / maxWeightForScale);
            float scale = Mathf.Lerp(minFishScale, maxFishScale, t);
            Vector3 newScale = new Vector3(scale, scale, 1f);
            transform.localScale = newScale;

            // Log usunięty — nie chcemy psuć niespodzianki pokazując wagę przed złowieniem
        }
        else
        {
            Debug.LogWarning($"Brak spriteRenderer={spriteRenderer != null} lub fishIcon={fishIcon != null} w Bobber! " +
                             $"Obiekt: {gameObject.name}, Instance: {(Instance != null ? Instance.gameObject.name : "null")}");
        }
    }

    /// <summary>
    /// Przywraca oryginalny sprite spławika (gdy ryba uciekła).
    /// </summary>
    public void ResetToBobberSprite()
    {
        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
            spriteRenderer.color = originalColor;
            transform.localScale = baseScale;
            Debug.Log("<color=#87CEEB>🔄 Przywrócono sprite spławika (ryba uciekła).</color>");
        }
    }
}
