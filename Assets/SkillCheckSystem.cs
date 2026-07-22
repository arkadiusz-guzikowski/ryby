using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minigra SkillCheck wzorowana na Dead by Daylight.
/// 
/// ✅ Auto-UI — sam rysuje pasek, strefy i igłę (OnGUI).
/// ✅ Nie wymaga żadnych prefabów, sprite'ów ani Canvasu.
/// ✅ Wystarczy dodać do dowolnego GameObjectu w scenie.
/// 
/// Jak użyć (po implementacji):
///   SkillCheckSystem.Instance.StartSkillCheck(difficulty, onComplete, onFail);
/// 
/// difficulty = 0 (łatwy) do 1 (bardzo trudny)
/// onComplete(SkillCheckResult) — zwraca wynik (Good/Perfect)
/// onFail() — gdy gracz nie kliknął w czasie
/// </summary>
public class SkillCheckSystem : MonoBehaviour
{
    public static SkillCheckSystem Instance { get; private set; }

    [Header("Parametry")]
    [Tooltip("Prędkość igły (pikseli/sek).")]
    [SerializeField] private float needleSpeed = 300f;
    [Tooltip("Szerokość dobrej strefy (w % paska, 0-1).")]
    [SerializeField] private float goodZoneWidth = 0.25f;
    [Tooltip("Szerokość idealnej strefy (w % dobrej strefy, 0-1).")]
    [SerializeField] private float perfectZoneWidth = 0.2f;
    [Tooltip("Czas na kliknięcie (sekundy). 0 = bez limitu.")]
    [SerializeField] private float timeLimit = 3f;
    [Tooltip("Klawisz do kliknięcia.")]
    [SerializeField] private Key skillCheckKey = Key.Space;

    [Header("Dźwięki")]
    [Tooltip("Dźwięk gdy pojawia się skill check.")]
    [SerializeField] private AudioClip dźwiękStart;
    [Tooltip("Dźwięk trafienia w Dobrą strefę.")]
    [SerializeField] private AudioClip dźwiękDobry;
    [Tooltip("Dźwięk trafienia w Idealną strefę.")]
    [SerializeField] private AudioClip dźwiękIdealny;
    [Tooltip("Dźwięk pudła/missa.")]
    [SerializeField] private AudioClip dźwiękPudło;

    [Header("Kolory")]
    [SerializeField] private Color barBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color goodZoneColor = new Color(0f, 1f, 0f, 0.4f);
    [SerializeField] private Color perfectZoneColor = new Color(1f, 1f, 0f, 0.6f);
    [SerializeField] private Color needleColor = Color.white;
    [SerializeField] private Color borderColor = Color.gray;

    // Delegaty
    public delegate void SkillCheckCallback(SkillCheckResult result);
    public delegate void VoidCallback();

    public enum SkillCheckResult
    {
        Miss,       // Nie kliknął w czasie lub poza strefą
        Good,       // W dobrej strefie
        Perfect     // W idealnej strefie
    }

    // Stan
    private bool isActive = false;
    private float needlePos = 0.5f; // 0 = lewo, 1 = prawo
    private float goodStart = 0f;
    private float goodEnd = 0f;
    private float perfectStart = 0f;
    private float perfectEnd = 0f;
    private float timer = 0f;
    private float currentDifficulty = 0.5f;

    private SkillCheckCallback onCompleteCallback;
    private VoidCallback onFailCallback;

    // Wymiary paska (w pikselach ekranu)
    private float barWidth = 400f;
    private float barHeight = 40f;
    private float barX = 0f;
    private float barY = 0f;

    // Styl GUI
    private GUIStyle labelStyle;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Inicjalizacja stylu
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;
    }

    void Update()
    {
        if (!isActive) return;

        // Ruch igły (w przeliczeniu na pozycję 0-1)
        needlePos += needleSpeed * Time.deltaTime / barWidth;
        if (needlePos > 1f)
            needlePos = 0f; // Zapętlenie

        // Timer
        if (timeLimit > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= timeLimit)
            {
                FailSkillCheck();
                return;
            }
        }

        // Input
        if (Keyboard.current[skillCheckKey].wasPressedThisFrame)
        {
            ResolveSkillCheck();
        }
    }

    void OnGUI()
    {
        if (!isActive) return;

        // Wyśrodkuj pasek
        barX = (Screen.width - barWidth) * 0.5f;
        barY = Screen.height * 0.6f;

        // Tło paska
        DrawRect(barX, barY, barWidth, barHeight, barBgColor);

        // Strefa dobra
        float goodPixelStart = barX + goodStart * barWidth;
        float goodPixelWidth = (goodEnd - goodStart) * barWidth;
        DrawRect(goodPixelStart, barY, goodPixelWidth, barHeight, goodZoneColor);

        // Strefa idealna
        float perfectPixelStart = barX + perfectStart * barWidth;
        float perfectPixelWidth = (perfectEnd - perfectStart) * barWidth;
        DrawRect(perfectPixelStart, barY, perfectPixelWidth, barHeight, perfectZoneColor);

        // Obramowanie paska
        DrawRect(barX, barY, barWidth, 2f, borderColor); // góra
        DrawRect(barX, barY + barHeight - 2f, barWidth, 2f, borderColor); // dół
        DrawRect(barX, barY, 2f, barHeight, borderColor); // lewo
        DrawRect(barX + barWidth - 2f, barY, 2f, barHeight, borderColor); // prawo

        // Igła
        float needlePixelX = barX + needlePos * barWidth;
        DrawRect(needlePixelX - 2f, barY - 5f, 4f, barHeight + 10f, needleColor);

        // Tekst "SPACJA!" nad paskiem
        labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(barX, barY - 30f, barWidth, 20f), $"🎯 NACIŚNIJ {skillCheckKey}!", labelStyle);

        // Timer
        if (timeLimit > 0f)
        {
            float remaining = Mathf.Max(0f, timeLimit - timer);
            GUI.Label(new Rect(barX + barWidth + 10f, barY, 60f, barHeight), $"{remaining:F1}s", labelStyle);
        }
    }

    // ============================================================
    // 🔄 PUBLICZNE API
    // ============================================================

    /// <summary>
    /// Rozpoczyna skill check.
    /// </summary>
    /// <param name="difficulty">0 (łatwy) do 1 (bardzo trudny)</param>
    /// <param name="onComplete">Callback z wynikiem (Good/Perfect)</param>
    /// <param name="onFail">Callback gdy nie kliknął w czasie</param>
    public void StartSkillCheck(float difficulty, SkillCheckCallback onComplete, VoidCallback onFail)
    {
        if (isActive) return;

        currentDifficulty = Mathf.Clamp01(difficulty);
        onCompleteCallback = onComplete;
        onFailCallback = onFail;

        // Generowanie strefy
        GenerateZones();

        // Reset stanu
        needlePos = Random.Range(0f, 1f);
        timer = 0f;
        isActive = true;

        // 🔊 Dźwięk startu skill checka
        if (dźwiękStart != null)
            AudioSource.PlayClipAtPoint(dźwiękStart, Camera.main?.transform.position ?? Vector3.zero);

        Debug.Log($"<color=#FFD700>🎯 SkillCheck rozpoczęty! (trudność: {difficulty:F2})</color>");
    }

    /// <summary>
    /// Przerywa skill check (np. ryba uciekła).
    /// </summary>
    public void CancelSkillCheck()
    {
        if (!isActive) return;
        isActive = false;
        onCompleteCallback = null;
        onFailCallback = null;
    }

    public bool IsActive => isActive;

    // ============================================================
    // 🔧 WEWNĘTRZNE
    // ============================================================

    private void GenerateZones()
    {
        // Trudność wpływa na szerokość dobrej strefy
        float adjustedGoodWidth = Mathf.Lerp(goodZoneWidth, goodZoneWidth * 0.3f, currentDifficulty);

        // Losowa pozycja startowa dobrej strefy
        float margin = adjustedGoodWidth * 0.5f;
        goodStart = Random.Range(margin, 1f - margin);
        goodEnd = goodStart + adjustedGoodWidth;
        if (goodEnd > 1f)
        {
            goodEnd = 1f;
            goodStart = 1f - adjustedGoodWidth;
        }

        // Idealna strefa wewnątrz dobrej
        float perfectWidth = adjustedGoodWidth * perfectZoneWidth;
        float perfectMargin = (adjustedGoodWidth - perfectWidth) * 0.5f;
        perfectStart = goodStart + perfectMargin;
        perfectEnd = perfectStart + perfectWidth;
    }

    private void ResolveSkillCheck()
    {
        if (!isActive) return;
        isActive = false;

        SkillCheckResult result;

        if (needlePos >= perfectStart && needlePos <= perfectEnd)
        {
            result = SkillCheckResult.Perfect;
            Debug.Log("<color=#FFD700>🌟🌟🌟 PERFECT! Idealny skill check! 🌟🌟🌟</color>");
        }
        else if (needlePos >= goodStart && needlePos <= goodEnd)
        {
            result = SkillCheckResult.Good;
            Debug.Log("<color=#00FF00>✅ Dobry skill check!</color>");
        }
        else
        {
            result = SkillCheckResult.Miss;
            Debug.Log("<color=#FF4500>❌ Spudłowałeś! Igła poza strefą.</color>");
        }

        // 🔊 Dźwięki trafienia/pudła
        if (result == SkillCheckResult.Perfect && dźwiękIdealny != null)
            AudioSource.PlayClipAtPoint(dźwiękIdealny, Camera.main?.transform.position ?? Vector3.zero);
        else if (result == SkillCheckResult.Good && dźwiękDobry != null)
            AudioSource.PlayClipAtPoint(dźwiękDobry, Camera.main?.transform.position ?? Vector3.zero);
        else if (result == SkillCheckResult.Miss && dźwiękPudło != null)
            AudioSource.PlayClipAtPoint(dźwiękPudło, Camera.main?.transform.position ?? Vector3.zero);

        if (result == SkillCheckResult.Miss)
            onFailCallback?.Invoke();
        else
            onCompleteCallback?.Invoke(result);

        onCompleteCallback = null;
        onFailCallback = null;
    }

    private void FailSkillCheck()
    {
        if (!isActive) return;
        isActive = false;

        // 🔊 Dźwięk pudła (minął czas)
        if (dźwiękPudło != null)
            AudioSource.PlayClipAtPoint(dźwiękPudło, Camera.main?.transform.position ?? Vector3.zero);

        Debug.Log("<color=#FF4500>⏰ Minął czas! SkillCheck nieudany.</color>");
        onFailCallback?.Invoke();
        onCompleteCallback = null;
        onFailCallback = null;
    }

    // ============================================================
    // 🎨 RYSOWANIE
    // ============================================================

    private void DrawRect(float x, float y, float width, float height, Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        GUI.DrawTexture(new Rect(x, y, width, height), texture);
    }

    // ============================================================
    // ⚙️ USTAWIENIA Z GAMEMANAGERA
    // ============================================================

    public void SetSettings(float speed, float goodWidth, float perfectW, float time, Key key)
    {
        needleSpeed = speed;
        goodZoneWidth = goodWidth;
        perfectZoneWidth = perfectW;
        timeLimit = time;
        skillCheckKey = key;
    }
}
