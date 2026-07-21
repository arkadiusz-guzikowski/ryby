using UnityEngine;

public class FishingHotSpot : MonoBehaviour
{
    [Header("Boost — zakres wag")]
    [SerializeField] private float boostMinWeight = 30f;
    [SerializeField] private float boostMaxWeight = 40f;

    [Header("Boost — czas oczekiwania")]
    [Tooltip("Mnożnik czasu oczekiwania na branie. 0.5 = 2x szybciej, 2.0 = 2x wolniej.")]
    [SerializeField] [Range(0.1f, 3f)] private float waitTimeMultiplier = 0.5f;

    [Header("Boost — mnożniki szans na ryby")]
    [Tooltip("Mnożnik szansy na rybę 1-5kg")]
    [SerializeField] [Range(0f, 5f)] private float chanceMultiplier1_5kg = 0.5f;
    [Tooltip("Mnożnik szansy na rybę 5-10kg")]
    [SerializeField] [Range(0f, 5f)] private float chanceMultiplier5_10kg = 0.5f;
    [Tooltip("Mnożnik szansy na rybę 10-20kg")]
    [SerializeField] [Range(0f, 5f)] private float chanceMultiplier10_20kg = 1f;
    [Tooltip("Mnożnik szansy na rybę 20-30kg")]
    [SerializeField] [Range(0f, 5f)] private float chanceMultiplier20_30kg = 2f;
    [Tooltip("Mnożnik szansy na rybę 30-40kg")]
    [SerializeField] [Range(0f, 5f)] private float chanceMultiplier30_40kg = 3f;

    [Header("Konfiguracja")]
    [SerializeField] private string playerTag = "Player";

    [Header("Komunikaty")]
    [SerializeField] private string enterMessage = "🔥 Jesteś w gorącym miejscu! Szansa na gigantycznego karpia 30-40kg!";
    [SerializeField] private string exitMessage = "Opuszczasz gorące miejsce. Szanse wracają do normy.";
    [SerializeField] private string enterColorHex = "#00FF00";
    [SerializeField] private string exitColorHex = "#FFFFFF";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Reagujemy zarówno na gracza, jak i na spławik (Bobber)
        if (other.CompareTag(playerTag) || other.GetComponent<Bobber>() != null)
        {
            if (FishSizes.Instance != null)
            {
                FishSizes.Instance.IsInHotSpot = true;
                FishSizes.Instance.HotSpotMinWeight = boostMinWeight;
                FishSizes.Instance.HotSpotMaxWeight = boostMaxWeight;
                FishSizes.Instance.HotSpotWaitTimeMultiplier = waitTimeMultiplier;
                FishSizes.Instance.SetHotSpotChanceMultipliers(
                    chanceMultiplier1_5kg,
                    chanceMultiplier5_10kg,
                    chanceMultiplier10_20kg,
                    chanceMultiplier20_30kg,
                    chanceMultiplier30_40kg
                );
                Debug.Log($"<color={enterColorHex}>{enterMessage}</color>");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Reagujemy zarówno na gracza, jak i na spławik (Bobber)
        if (other.CompareTag(playerTag) || other.GetComponent<Bobber>() != null)
        {
            if (FishSizes.Instance != null)
            {
                FishSizes.Instance.IsInHotSpot = false;
                Debug.Log($"<color={exitColorHex}>{exitMessage}</color>");
            }
        }
    }
}
