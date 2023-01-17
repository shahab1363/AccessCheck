namespace CheckerLib.Common.Helpers
{
    /// <summary>
    /// Sample configuration:
    /// Saturday[01:00-03:00,23:00-23:30]|Monday[*]|*[12:00-18:00]
    /// </summary>
    public class Scheduler
    {
        public const string DefaultSchedule = "*[*]";
        private const string defaultDaySchedule = "*";
        private readonly string settings;
        private readonly DateTimeKind dateTimeKind;
        private Dictionary<DayOfWeek, List<TimePeriod>> TimePeriods;

        public Scheduler(string settings = DefaultSchedule, DateTimeKind dateTimeKind = DateTimeKind.Local)
        {
            if (string.IsNullOrEmpty(settings))
            {
                settings = DefaultSchedule;
            }
            this.settings = settings.Replace(" ", string.Empty).Replace("]", string.Empty);
            ParseSchedulerSettings();
            this.dateTimeKind = dateTimeKind;
        }

        public bool IsInTimeWindows()
            => IsInTimeWindows(DateTimeOffset.Now);

        public bool IsInTimeWindows(DateTimeOffset dateTimeOffset)
        {
            var dateTime = dateTimeKind switch
            {
                DateTimeKind.Local => dateTimeOffset.ToLocalTime(),
                DateTimeKind.Utc => dateTimeOffset.ToUniversalTime(),
                _ => dateTimeOffset
            };

            var dayTimePeriods = TimePeriods[dateTime.DayOfWeek];
            foreach (var timePeriod in dayTimePeriods)
            {
                if (timePeriod.FromTime < dateTime.TimeOfDay && timePeriod.ToTime > dateTime.TimeOfDay)
                {
                    return true;
                }
            }

            return false;
        }

        private void ParseSchedulerSettings()
        {
            var daysOfWeekEnum = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();
            TimePeriods = daysOfWeekEnum.ToDictionary(x => x, x => new List<TimePeriod>());
            // Sample Value: Saturday[16:10-17:00,22:00-23:59,00:00-06:00]|*[01:30-08:00]
            if (!string.IsNullOrWhiteSpace(settings))
            {
                try
                {
                    var rawDayConfigurations = settings.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    var rawWeekDaySchedules = daysOfWeekEnum.ToDictionary(x => x, x => defaultDaySchedule);

                    var wildCharDaysSchedule = rawDayConfigurations.SingleOrDefault(x => x.StartsWith("*["));
                    if (!string.IsNullOrEmpty(wildCharDaysSchedule))
                    {
                        var wildCharDaysScheduleParts = wildCharDaysSchedule.Split(new[] { '[' }, StringSplitOptions.RemoveEmptyEntries);
                        if (wildCharDaysScheduleParts.Length == 2)
                        {
                            var defaultSchedule = wildCharDaysScheduleParts[1];
                            rawWeekDaySchedules = daysOfWeekEnum.ToDictionary(x => x, x => defaultSchedule);
                        }
                    }

                    foreach (var dailySchedule in rawDayConfigurations)
                    {
                        if (dailySchedule.StartsWith("*["))
                            continue;

                        var dailyScheduleParts = dailySchedule.Split(new[] { '[' }, StringSplitOptions.RemoveEmptyEntries);

                        if (dailyScheduleParts.Length == 2)
                        {
                            if (Enum.TryParse<DayOfWeek>(dailyScheduleParts[0], true, out var dayOfWeek))
                            {
                                rawWeekDaySchedules[dayOfWeek] = dailyScheduleParts[1];
                            }
                        }
                    }

                    foreach (var dayOfWeek in daysOfWeekEnum)
                    {
                        TimePeriods[dayOfWeek].AddRange(ParseDaySchedule(rawWeekDaySchedules[dayOfWeek]));
                    }
                }
                catch (Exception exc)
                {
                    throw new Exception("Syntax error in Scheduler configuration. Working Time Windows value is invalid. It should be formatted like this: \"23:00-23:59,00:00-04:00\"", exc);
                }
            }
        }

        private IEnumerable<TimePeriod> ParseDaySchedule(string schedule)
        {
            var windows = schedule.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var window in windows)
            {
                if (string.IsNullOrEmpty(window))
                {
                    continue;
                }
                if (window.Trim() == "*")
                {
                    yield return new TimePeriod(TimeSpan.Zero, TimeSpan.FromHours(24));
                    continue;
                }
                var windowParts = window.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (windowParts.Length != 2)
                {
                    throw new Exception("Each time window should only have two parts");
                }

                if (TimeSpan.TryParse(windowParts[0], out var fromTime))
                {
                    if (TimeSpan.TryParse(windowParts[1], out var toTime))
                    {
                        if (fromTime > toTime)
                        {
                            throw new Exception($"From time ({fromTime}) can not be larger than to time ({toTime})");
                        }
                        yield return new TimePeriod(fromTime, toTime);
                    }
                    else
                    {
                        throw new Exception("To Time value is invalid");
                    }
                }
                else
                {
                    throw new Exception("From Time value is invalid");
                }
            }
        }

        private class TimePeriod
        {
            public TimeSpan FromTime { get; private set; }
            public TimeSpan ToTime { get; private set; }

            public TimePeriod(TimeSpan fromTime, TimeSpan toTime)
            {
                FromTime = fromTime;
                ToTime = toTime;
            }
        }
    }
}
