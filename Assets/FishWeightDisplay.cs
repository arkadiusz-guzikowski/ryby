using UnityEngine;

public class FishWeightDisplay : MonoBehaviour
{
    public static FishWeightDisplay Instance;

    [Header("Progi wagowe")]
    [SerializeField] private float recordWeight = 30f;
    [SerializeField] private float bigFishWeight = 20f;
    [SerializeField] private float mediumFishWeight = 10f;

    [Header("Komunikaty")]
    [SerializeField] private string caughtMessage = "Złowiłeś karpia!";
    [SerializeField] private string recordMessage = "🏆 REKORD! Gigantyczny karp!";
    [SerializeField] private string bigFishMessage = "Duża ryba!";
    [SerializeField] private string mediumFishMessage = "Niezły karpik.";
    [SerializeField] private string smallFishMessage = "Mały karpik.";

    [Header("Kolory (hex)")]
    [SerializeField] private string caughtColorHex = "#FFD700";
    [SerializeField] private string recordColorHex = "red";
    [SerializeField] private string bigFishColorHex = "orange";

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        if (FishingSystem.Instance != null)
            FishingSystem.Instance.OnFishCaught += OnFishCaught;
    }

    void OnDisable()
    {
        if (FishingSystem.Instance != null)
            FishingSystem.Instance.OnFishCaught -= OnFishCaught;
    }

    private void OnFishCaught(float weight)
    {
        Debug.Log($"<color={caughtColorHex}>{caughtMessage}</color> Waga: <b>{weight}kg</b>");

        if (weight >= recordWeight)
            Debug.Log($"<color={recordColorHex}>{recordMessage}</color>");
        else if (weight >= bigFishWeight)
            Debug.Log($"<color={bigFishColorHex}>{bigFishMessage}</color>");
        else if (weight >= mediumFishWeight)
            Debug.Log(mediumFishMessage);
        else
            Debug.Log(smallFishMessage);
    }
}
