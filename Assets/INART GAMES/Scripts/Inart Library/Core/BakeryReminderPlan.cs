using System;

namespace INART.SlothyBakery.Notifications
{
    /// <summary>At most three gentle return reminders, scheduled for days 1, 2, and 4 after leaving.</summary>
    public static class BakeryReminderPlan
    {
        public const int Hour = 17;
        public const int Minute = 30;
        public static readonly TimeSpan MinimumGap = TimeSpan.FromHours(1);

        public readonly struct Reminder
        {
            public readonly DateTime FireAt;
            public readonly int Index;

            public Reminder(DateTime fireAt, int index)
            {
                FireAt = fireAt;
                Index = index;
            }

            public string BodyKey => Index == 1 ? "reminder.miss" : Index == 2 ? "reminder.comeback" : "reminder.daily";
        }

        public static Reminder[] Series(DateTime now, bool dailyDoneToday)
        {
            DateTime today = now.Date.AddHours(Hour).AddMinutes(Minute);
            DateTime first = !dailyDoneToday && today - now >= MinimumGap ? today : today.AddDays(1);
            return new[]
            {
                new Reminder(first, 0),
                new Reminder(first.AddDays(1), 1),
                new Reminder(first.AddDays(3), 2),
            };
        }
    }
}
