using UnityEngine;

public class FishSizes : MonoBehaviour
{
    public static FishSizes Instance;

    public bool IsInHotSpot { get; set; } = false;

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
        // Gorące miejsce = 100% szansa na rybę 30-40kg
        if (IsInHotSpot)
        {
            float weight = Random.Range(30f, 40f);
            return weight;
        }


        float total = chance1_5kg + chance5_10kg + chance10_20kg + chance20_30kg + chance30_40kg;
        if (total <= 0f) return 1f;

        float roll = Random.Range(0f, total);

        if (roll < chance1_5kg)
            return Random.Range(1f, 5f);
        roll -= chance1_5kg;

        if (roll < chance5_10kg)
            return Random.Range(5f, 10f);
        roll -= chance5_10kg;

        if (roll < chance10_20kg)
            return Random.Range(10f, 20f);
        roll -= chance10_20kg;

        if (roll < chance20_30kg)
            return Random.Range(20f, 30f);

        return Random.Range(30f, 40f);
    }
}

