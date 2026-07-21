using UnityEngine;

public class FishSizes : MonoBehaviour
{
    public static FishSizes Instance;

    public bool IsInHotSpot { get; set; } = false;
    public float HotSpotMinWeight { get; set; } = 30f;
    public float HotSpotMaxWeight { get; set; } = 40f;
    public float HotSpotWaitTimeMultiplier { get; set; } = 1f;

    // Mnożniki szans dla hotspota (domyślnie 1 = brak zmiany)
    private float[] hotSpotChanceMultipliers = new float[] { 1f, 1f, 1f, 1f, 1f };

    public void SetHotSpotChanceMultipliers(float m1, float m2, float m3, float m4, float m5)
    {
        hotSpotChanceMultipliers[0] = m1;
        hotSpotChanceMultipliers[1] = m2;
        hotSpotChanceMultipliers[2] = m3;
        hotSpotChanceMultipliers[3] = m4;
        hotSpotChanceMultipliers[4] = m5;
    }

    [SerializeField] [Range(0f, 100f)] private float chance1_5kg = 30f;
    [SerializeField] [Range(0f, 100f)] private float chance5_10kg = 30f;
    [SerializeField] [Range(0f, 100f)] private float chance10_20kg = 20f;
    [SerializeField] [Range(0f, 100f)] private float chance20_30kg = 15f;
    [SerializeField] [Range(0f, 100f)] private float chance30_40kg = 5f;

    void Awake()
    {
        Instance = this;
    }

    void OnValidate()
    {
        float total = chance1_5kg + chance5_10kg + chance10_20kg + chance20_30kg + chance30_40kg;
        if (total > 100f)
        {
            float scale = 100f / total;
            chance1_5kg = Mathf.Round(chance1_5kg * scale * 10f) / 10f;
            chance5_10kg = Mathf.Round(chance5_10kg * scale * 10f) / 10f;
            chance10_20kg = Mathf.Round(chance10_20kg * scale * 10f) / 10f;
            chance20_30kg = Mathf.Round(chance20_30kg * scale * 10f) / 10f;
            chance30_40kg = Mathf.Round(chance30_40kg * scale * 10f) / 10f;
        }
    }

    public float GetRandomCarpWeight()
    {
        float c1, c2, c3, c4, c5;

        if (IsInHotSpot)
        {
            // W hotspocie: bazowe szanse * mnożniki hotspota
            c1 = chance1_5kg * hotSpotChanceMultipliers[0];
            c2 = chance5_10kg * hotSpotChanceMultipliers[1];
            c3 = chance10_20kg * hotSpotChanceMultipliers[2];
            c4 = chance20_30kg * hotSpotChanceMultipliers[3];
            c5 = chance30_40kg * hotSpotChanceMultipliers[4];
        }
        else
        {
            c1 = chance1_5kg;
            c2 = chance5_10kg;
            c3 = chance10_20kg;
            c4 = chance20_30kg;
            c5 = chance30_40kg;
        }

        float total = c1 + c2 + c3 + c4 + c5;
        if (total <= 0f) return 1f;

        float roll = Random.Range(0f, total);

        if (roll < c1)
            return Random.Range(1f, 5f);
        roll -= c1;

        if (roll < c2)
            return Random.Range(5f, 10f);
        roll -= c2;

        if (roll < c3)
            return Random.Range(10f, 20f);
        roll -= c3;

        if (roll < c4)
            return Random.Range(20f, 30f);

        return Random.Range(30f, 40f);
    }
}

