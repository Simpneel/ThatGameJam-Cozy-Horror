using System.Collections.Generic;
using UnityEngine;

/// The run's hidden state. Nothing here is ever shown to the player.
/// No save file: every launch is a fresh run (see DESIGN.md).
public static class PlayerState
{
    public static int Day { get; private set; }
    public static int Points { get; private set; }          // spendable, hidden
    public static int PointsEarned { get; private set; }    // lifetime from anomalies (arms the win streak)
    public static int PurchasesToday { get; private set; }
    public static int NoPurchaseStreak { get; private set; } // counted only once the streak is armed

    static readonly HashSet<string> owned = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Reset(0);

    public static void Reset(int startingPoints)
    {
        Day = 0;
        Points = startingPoints;
        PointsEarned = 0;
        PurchasesToday = 0;
        NoPurchaseStreak = 0;
        owned.Clear();
    }

    public static bool Owns(string id) => owned.Contains(id);
    public static int OwnedCount => owned.Count;

    public static void EarnPoint(int amount = 1)
    {
        Points += amount;
        PointsEarned += amount;
    }

    public static bool TryBuy(string id, int price)
    {
        if (owned.Contains(id) || Points < price) return false;
        Points -= price;
        PurchasesToday++;
        owned.Add(id);
        return true;
    }

    /// Call at bedtime, before StartNextDay. Updates the hidden win streak.
    public static void EndDay(int pointsToArmStreak)
    {
        if (PointsEarned >= pointsToArmStreak)
            NoPurchaseStreak = PurchasesToday == 0 ? NoPurchaseStreak + 1 : 0;
    }

    public static void StartNextDay()
    {
        Day++;
        PurchasesToday = 0;
    }
}
