using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// Single source of truth: visible money, hidden score, owned item IDs. Saves to JSON.
public static class PlayerState
{
    [Serializable]
    class SaveData
    {
        public int money;
        public float score;
        public List<string> owned = new List<string>();
    }

    static SaveData data;
    static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static event Action<int> OnMoneyChanged;   // new money total
    public static event Action<string> OnItemOwned;   // item id

    public static int Money { get { Ensure(); return data.money; } }
    public static float Score { get { Ensure(); return data.score; } } // hidden from the player

    // Keeps statics clean when "Enter Play Mode" skips domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        data = null;
        OnMoneyChanged = null;
        OnItemOwned = null;
    }

    static void Ensure()
    {
        if (data != null) return;
        try
        {
            data = File.Exists(SavePath)
                ? JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath))
                : new SaveData();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not read save, starting fresh: " + e.Message);
            data = new SaveData();
        }
        if (data == null) data = new SaveData();
    }

    static void Save()
    {
        try { File.WriteAllText(SavePath, JsonUtility.ToJson(data)); }
        catch (Exception e) { Debug.LogWarning("Could not write save: " + e.Message); }
    }

    public static bool Owns(string id) { Ensure(); return data.owned.Contains(id); }

    public static bool Own(string id)
    {
        Ensure();
        if (string.IsNullOrEmpty(id) || data.owned.Contains(id)) return false;
        data.owned.Add(id);
        Save();
        OnItemOwned?.Invoke(id);
        return true;
    }

    public static void AddMoney(int amount)
    {
        Ensure();
        data.money = Mathf.Max(0, data.money + amount);
        Save();
        OnMoneyChanged?.Invoke(data.money);
    }

    public static bool TrySpend(int amount)
    {
        Ensure();
        if (data.money < amount) return false;
        AddMoney(-amount);
        return true;
    }

    public static void AddScore(float amount)
    {
        Ensure();
        data.score = Mathf.Max(0f, data.score + amount);
        Save();
    }

    public static void ResetAll()
    {
        data = new SaveData();
        Save();
    }
}
