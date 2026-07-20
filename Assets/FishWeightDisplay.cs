using UnityEngine;

public class FishWeightDisplay : MonoBehaviour
{
    public static FishWeightDisplay Instance;

    void Awake()
    {
        Instance = this;
    }

    // Wywołaj to z FishingSystem po złowieniu ryby
    public void OnFishCaught(float weight)
    {
        Debug.Log($"<color=#FFD700>Złowiłeś karpia!</color> Waga: <b>{weight}kg</b>");

        if (weight >= 30f)
            Debug.Log("<color=red>🏆 REKORD! Gigantyczny karp!</color>");
        else if (weight >= 20f)
            Debug.Log("<color=orange>Duża ryba!</color>");
        else if (weight >= 10f)
            Debug.Log("Niezły karpik.");
        else
            Debug.Log("Mały karpik.");
    }
}