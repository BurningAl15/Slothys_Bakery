using System;
using System.Collections.Generic;
using INART.SlothyBakery.Base;
using INART.SlothyBakery.SceneTransition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fatcat.Slothy.Bakery.Modes
{
    public enum BakeryModeKind
    {
        Story = 0,
        Practice = 1,
        Daily = 2,
        Afternoon = 3,
        Chef = 4
    }

    [Serializable]
    public sealed class BakeryChapterResult
    {
        public string sceneName;
        public int answers;
        public int mistakes;
        public int stars;
    }

    [Serializable]
    public sealed class BakeryRunSummary
    {
        public BakeryModeKind mode;
        public List<BakeryChapterResult> chapters = new();
        public int totalAnswers;
        public int totalMistakes;
        public int totalStars;
        public bool newlyUnlockedChef;
    }

    /// <summary>
    /// Tracks Bakery lifetime and daily stats in PlayerPrefs (isolated from Story save keys).
    /// </summary>
    public static class BakeryStats
    {
        private const string StatPrefix = "bakery.stat.";
        private const string PlayDaysKey = "bakery.play_days";
        private const string DailyDonePrefix = "bakery.daily.done.";
        private const string PracticeCountFmt = "bakery.practice.{0}.count";
        private const string PracticeTopicPrefix = "bakery.practice.topic.";

        public static int Get(string key) => PlayerPrefs.GetInt(StatPrefix + key, 0);

        public static void Add(string key, int delta = 1)
        {
            if (delta == 0) return;
            PlayerPrefs.SetInt(StatPrefix + key, Get(key) + delta);
            PlayerPrefs.Save();
            BakeryAchievements.Evaluate();
        }

        public static void AtLeast(string key, int value)
        {
            if (value > Get(key))
            {
                PlayerPrefs.SetInt(StatPrefix + key, value);
                PlayerPrefs.Save();
                BakeryAchievements.Evaluate();
            }
        }

        public static bool StoryFinished => Get("story.finished") > 0;
        public static bool AfternoonFinished => Get("afternoon.finished") > 0;
        public static bool ChefFinished => Get("chef.finished") > 0;

        public static int BestStars(string sceneName) => Get("skill.stars." + sceneName);

        public static int PracticeCount(string topicId)
            => PlayerPrefs.GetInt(string.Format(PracticeCountFmt, topicId), 0);

        public static void RecordPractice(string topicId)
        {
            if (string.IsNullOrEmpty(topicId)) return;
            MigratePracticeStats();
            PlayerPrefs.SetInt(string.Format(PracticeCountFmt, topicId), PracticeCount(topicId) + 1);
            if (PlayerPrefs.GetInt(PracticeTopicPrefix + topicId, 0) == 0)
            {
                PlayerPrefs.SetInt(PracticeTopicPrefix + topicId, 1);
                PlayerPrefs.SetInt(StatPrefix + "practice.topics", Get("practice.topics") + 1);
            }
            PlayerPrefs.Save();
            Add("practice.finished");
            BakeryAchievements.Evaluate();
        }

        private static void MigratePracticeStats()
        {
            int total = 0;
            int topics = 0;
            foreach (string id in new[] { "mg1", "mg2", "mg3" })
            {
                int count = PracticeCount(id);
                total += count;
                if (count <= 0 || PlayerPrefs.GetInt(PracticeTopicPrefix + id, 0) != 0) continue;
                PlayerPrefs.SetInt(PracticeTopicPrefix + id, 1);
                topics++;
            }
            if (total > Get("practice.finished")) PlayerPrefs.SetInt(StatPrefix + "practice.finished", total);
            if (topics > 0) PlayerPrefs.SetInt(StatPrefix + "practice.topics", Get("practice.topics") + topics);
        }

        public static bool IsDailyCompleted(DateTime day)
            => PlayerPrefs.GetInt(DailyDonePrefix + day.ToString("yyyyMMdd"), 0) == 1;

        public static void MarkDailyCompleted(DateTime day)
        {
            PlayerPrefs.SetInt(DailyDonePrefix + day.ToString("yyyyMMdd"), 1);
            PlayerPrefs.Save();
        }

        public static void RecordPlayDay(DateTime day)
        {
            string tag = day.ToString("yyyyMMdd");
            string current = PlayerPrefs.GetString(PlayDaysKey, "");
            if (current == tag) return;
            PlayerPrefs.SetString(PlayDaysKey, tag);
            Add("days.played");
        }
    }

    /// <summary>
    /// Shared mode/run coordinator for Bakery: Story, Practice, Daily, Afternoon Shift, and Chef.
    /// Non-Story modes keep Story progress (SaveLoadState) completely untouched across start, restart, exit, and finish.
    /// </summary>
    public static class BakerySession
    {
        public static readonly string[] AllMinigames =
        {
            SceneUtils.FirstMinigameScene,
            SceneUtils.SecondMinigameScene,
            SceneUtils.ThirdMinigameScene
        };

        public static readonly string[] AfternoonScenes =
        {
            SceneUtils.FirstMinigameScene,
            SceneUtils.SecondMinigameScene
        };

        public static readonly string[] ChefScenes =
        {
            SceneUtils.ThirdMinigameScene,
            SceneUtils.SecondMinigameScene,
            SceneUtils.FirstMinigameScene
        };

        public static Func<string, bool> NavigateOverride;

        public static BakeryModeKind Mode { get; private set; } = BakeryModeKind.Story;
        public static bool OffStory => Mode != BakeryModeKind.Story;
        public static bool Active => OffStory;
        public static bool Restarted { get; private set; }
        public static int? Seed { get; private set; }
        public static DateTime SessionDay { get; private set; }
        public static string TopicId { get; private set; }
        public static int ChapterIndex { get; private set; }
        public static IReadOnlyList<string> ChapterScenes => _scenes;
        public static string CurrentScene => OffStory && ChapterIndex < _scenes.Count ? _scenes[ChapterIndex] : SceneManager.GetActiveScene().name;

        public static int Answers { get; private set; }
        public static int Mistakes { get; private set; }

        public static BakeryRunSummary LastSummary { get; private set; }

        private static readonly List<string> _scenes = new();
        private static readonly List<BakeryChapterResult> _results = new();
        private static string _completedPracticeTopic;
        private static BakeryRunSummary _pendingSummary;

        public static bool AfternoonUnlocked => BakeryStats.StoryFinished;
        public static bool ChefUnlocked => BakeryStats.AfternoonFinished;

        public static int StarsFor(int mistakes) => mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1;

        public static int DailySeed(DateTime day) => day.Year * 10000 + day.Month * 100 + day.Day;

        public static string[] DailyScenes(DateTime day)
        {
            var pool = new List<string>(AllMinigames);
            ShuffleWithSeed(pool, DailySeed(day));
            return pool.GetRange(0, Mathf.Min(2, pool.Count)).ToArray();
        }

        public static void SetStoryMode()
        {
            Mode = BakeryModeKind.Story;
            Restarted = false;
            Seed = null;
            TopicId = null;
            ChapterIndex = 0;
            _scenes.Clear();
            _results.Clear();
        }

        public static void BeginStoryNewGame()
        {
            SetStoryMode();
            SaveLoadState.Instance?.ResetForNewStoryGame();
            Navigate(SceneUtils.FirstMinigameScene);
        }

        public static void BeginPractice(string topicId, string sceneName)
        {
            if (OffStory || !IsPracticeTopicScenePair(topicId, sceneName)) return;
            ResetSession(BakeryModeKind.Practice, new[] { sceneName }, null, topicId, DateTime.Now.Date);
            Navigate(sceneName);
        }

        public static void BeginDaily() => BeginDaily(DateTime.Now.Date);

        public static void BeginDaily(DateTime day)
        {
            day = day.Date;
            if (OffStory || BakeryStats.IsDailyCompleted(day)) return;

            int seed = DailySeed(day);
            ResetSession(BakeryModeKind.Daily, DailyScenes(day), seed, null, day);
            Navigate(_scenes[0]);
        }

        public static void BeginAfternoon()
        {
            if (OffStory || !AfternoonUnlocked) return;
            ResetSession(BakeryModeKind.Afternoon, AfternoonScenes, null, null, DateTime.Now.Date);
            Navigate(_scenes[0]);
        }

        public static void BeginChef()
        {
            if (OffStory || !ChefUnlocked) return;
            int seed = DailySeed(DateTime.Now.Date) ^ 0x5A3;
            ResetSession(BakeryModeKind.Chef, ChefScenes, seed, null, DateTime.Now.Date);
            Navigate(_scenes[0]);
        }

        public static void ResetChapterCounters()
        {
            Answers = 0;
            Mistakes = 0;
            BakeryStats.RecordPlayDay(DateTime.Now.Date);
        }

        public static void RecordAnswer(bool correct)
        {
            Answers++;
            if (!correct) Mistakes++;
            Fatcat.Slothy.Bakery.Shell.BakeryFeedbackHooks.PlayAnswerHaptic(correct);
        }

        public static void CompleteCurrentChapter()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            string scene = OffStory && ChapterIndex < _scenes.Count ? _scenes[ChapterIndex] : activeScene;
            int stars = StarsFor(Mistakes);

            BakeryStats.Add("minigame.done." + scene);
            BakeryStats.Add("chapters.completed");
            if (Answers > 0 && Mistakes == 0) BakeryStats.Add("minigame.perfect");
            BakeryStats.AtLeast("skill.stars." + scene, stars);

            if (!OffStory)
            {
                string nextStory = NextStoryDestination(activeScene);
                Navigate(nextStory);
                return;
            }

            if (Mode == BakeryModeKind.Practice)
            {
                BakeryStats.RecordPractice(TopicId);
                _completedPracticeTopic = TopicId;
                _pendingSummary = new BakeryRunSummary
                {
                    mode = BakeryModeKind.Practice,
                    chapters = { new BakeryChapterResult { sceneName = scene, answers = Answers, mistakes = Mistakes, stars = stars } },
                    totalAnswers = Answers,
                    totalMistakes = Mistakes,
                    totalStars = stars
                };
                LastSummary = _pendingSummary;
                SetStoryMode();
                Navigate(SceneUtils.MainMenuScene);
                return;
            }

            _results.Add(new BakeryChapterResult
            {
                sceneName = scene,
                answers = Answers,
                mistakes = Mistakes,
                stars = stars
            });
            ChapterIndex++;
            Restarted = false;

            if (ChapterIndex < _scenes.Count)
            {
                Navigate(_scenes[ChapterIndex]);
                return;
            }

            bool wasChefUnlocked = ChefUnlocked;
            if (Mode == BakeryModeKind.Daily)
            {
                BakeryStats.MarkDailyCompleted(SessionDay);
                BakeryStats.Add("daily.finished");
            }
            else if (Mode == BakeryModeKind.Afternoon)
            {
                BakeryStats.Add("afternoon.finished");
            }
            else if (Mode == BakeryModeKind.Chef)
            {
                BakeryStats.Add("chef.finished");
            }

            var summary = new BakeryRunSummary
            {
                mode = Mode,
                newlyUnlockedChef = !wasChefUnlocked && ChefUnlocked
            };
            summary.chapters.AddRange(_results);
            foreach (var ch in summary.chapters)
            {
                summary.totalAnswers += ch.answers;
                summary.totalMistakes += ch.mistakes;
                summary.totalStars += ch.stars;
            }

            _pendingSummary = summary;
            LastSummary = summary;
            SetStoryMode();
            Navigate(SceneUtils.MainMenuScene);
        }

        public static void RestartCurrentChapter()
        {
            Answers = 0;
            Mistakes = 0;
            if (OffStory && ChapterIndex < _scenes.Count)
            {
                Restarted = true;
                Navigate(_scenes[ChapterIndex]);
                return;
            }

            Restarted = false;
            Navigate(SceneManager.GetActiveScene().name);
        }

        public static void AbandonToMenu()
        {
            Time.timeScale = 1f;
            SetStoryMode();
            _pendingSummary = null;
            _completedPracticeTopic = null;
            Navigate(SceneUtils.MainMenuScene);
        }

        public static bool ConsumePendingSummary(out BakeryRunSummary summary)
        {
            summary = _pendingSummary;
            _pendingSummary = null;
            return summary != null;
        }

        public static bool ConsumeCompletedPractice(out string topicId)
        {
            topicId = _completedPracticeTopic;
            _completedPracticeTopic = null;
            return !string.IsNullOrEmpty(topicId);
        }

        public static void ShuffleWithSeed<T>(IList<T> items, int seed)
        {
            if (items == null || items.Count <= 1) return;
            var rng = new System.Random(seed);
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        private static void ResetSession(BakeryModeKind mode, IList<string> scenes, int? seed, string topicId, DateTime day)
        {
            Mode = mode;
            Seed = seed;
            TopicId = topicId;
            SessionDay = day;
            ChapterIndex = 0;
            Restarted = false;
            Answers = 0;
            Mistakes = 0;
            _completedPracticeTopic = null;
            _pendingSummary = null;
            _scenes.Clear();
            _scenes.AddRange(scenes);
            _results.Clear();
        }

        private static bool IsPracticeTopicScenePair(string topicId, string sceneName)
        {
            return (topicId == "mg1" && sceneName == SceneUtils.FirstMinigameScene)
                || (topicId == "mg2" && sceneName == SceneUtils.SecondMinigameScene)
                || (topicId == "mg3" && sceneName == SceneUtils.ThirdMinigameScene);
        }

        private static string NextStoryDestination(string current)
        {
            if (current == SceneUtils.FirstMinigameScene) return SceneUtils.SecondMinigameScene;
            if (current == SceneUtils.SecondMinigameScene) return SceneUtils.ThirdMinigameScene;
            if (current == SceneUtils.ThirdMinigameScene) return SceneUtils.FinalScene;
            return SceneUtils.MainMenuScene;
        }

        private static void Navigate(string sceneName)
        {
            if (NavigateOverride != null && NavigateOverride(sceneName)) return;
            SceneUtils.LoadScene(sceneName);
        }
    }
}
