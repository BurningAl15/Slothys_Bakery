using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.Modes
{
    /// <summary>Small persistent achievement catalog, following Café's stat-driven unlock model.</summary>
    public static class BakeryAchievements
    {
        public sealed class Definition
        {
            public readonly string Id, Stat, TitleKey, DescriptionKey;
            public readonly int Target;

            public Definition(string id, string stat, int target, string titleKey, string descriptionKey)
            {
                Id = id; Stat = stat; Target = target; TitleKey = titleKey; DescriptionKey = descriptionKey;
            }
        }

        private const string Prefix = "bakery.achievement.";
        private static readonly Definition[] Catalog =
        {
            new("first_order", "chapters.completed", 1, "achievement.first.title", "achievement.first.body"),
            new("ten_orders", "chapters.completed", 10, "achievement.orders.title", "achievement.orders.body"),
            new("perfect_order", "minigame.perfect", 1, "achievement.perfect.title", "achievement.perfect.body"),
            new("first_practice", "practice.finished", 1, "achievement.practice.title", "achievement.practice.body"),
            new("practice_variety", "practice.topics", 3, "achievement.practice3.title", "achievement.practice3.body"),
            new("daily_baker", "daily.finished", 1, "achievement.daily.title", "achievement.daily.body"),
            new("daily_regular", "daily.finished", 5, "achievement.daily5.title", "achievement.daily5.body"),
            new("afternoon_shift", "afternoon.finished", 1, "achievement.shift.title", "achievement.shift.body"),
            new("chef_mode", "chef.finished", 1, "achievement.chef.title", "achievement.chef.body"),
            new("three_days", "days.played", 3, "achievement.days.title", "achievement.days.body")
        };

        public static IReadOnlyList<Definition> All => Catalog;
        public static event Action<Definition> Unlocked;
        public static bool IsUnlocked(string id) => PlayerPrefs.HasKey(Prefix + id);
        public static int Progress(Definition item) => item == null ? 0 : Mathf.Min(item.Target, BakeryStats.Get(item.Stat));

        public static int UnlockedCount
        {
            get
            {
                int count = 0;
                foreach (var item in Catalog) if (IsUnlocked(item.Id)) count++;
                return count;
            }
        }

        public static int Evaluate()
        {
            int fresh = 0;
            foreach (var item in Catalog)
            {
                if (IsUnlocked(item.Id) || BakeryStats.Get(item.Stat) < item.Target) continue;
                PlayerPrefs.SetString(Prefix + item.Id, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                PlayerPrefs.Save();
                fresh++;
                Unlocked?.Invoke(item);
            }
            return fresh;
        }
    }
}
