using System;
using System.Collections.Generic;
using System.IO;

namespace Sessia
{
    public static class GreenhouseController
    {
        private static Random random = new Random();
        public static double SoilHumidity { get; private set; } = 30.0;
        public static double WaterUsed { get; private set; } = 0.0;
        public static double FertilizerUsed { get; private set; } = 0.0;
        public static string CurrentAction { get; private set; } = "Ожидание";
        public static string LastDatabaseRecord { get; private set; } = "Нет записей";
        public static bool IsPumpOn { get; private set; } = false;
        public static bool IsValveOpen { get; private set; } = false;
        public static bool IsFertilizerOn { get; private set; } = false;

        private static DateTime lastHumidityRecord = DateTime.MinValue;
        private static DateTime lastWateringStart = DateTime.MinValue;
        private static DateTime lastHourlyWatering = DateTime.MinValue;
        private static DateTime lastDailyWatering = DateTime.MinValue;
        private static DateTime lastFertilizerUsage = DateTime.MinValue;
        private static bool isWateringNow = false;
        private static double wateringDuration = 0;
        private static string wateringReason = "";
        private static List<string> databaseLog = new List<string>();
        private static string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "greenhouse_log.txt");

        private const double WATER_FLOW_RATE = 10.0;
        private const double FERTILIZER_FLOW_RATE = 2.0;
        private const double HUMIDITY_INCREASE_PER_MINUTE = 1.5;
        private const double HUMIDITY_DECREASE_PER_MINUTE = 0.2;
        private const double MAX_HUMIDITY = 45.0;
        private const double MIN_HUMIDITY = 30.0;

        public static void Update(DateTime currentTime)
        {
            RecordHumidityToDatabase(currentTime);
            CheckAndControlWatering(currentTime);
            UpdateHumidity(currentTime);
            UpdateWatering(currentTime);
        }

        private static void RecordHumidityToDatabase(DateTime currentTime)
        {
            if (currentTime - lastHumidityRecord >= TimeSpan.FromMinutes(10))
            {
                lastHumidityRecord = currentTime;
                string record = $"{currentTime:HH:mm:ss} - Влажность почвы: {SoilHumidity:F1}%";
                databaseLog.Add(record);
                LastDatabaseRecord = record;
                LogToFile(record);
            }
        }

        private static void CheckAndControlWatering(DateTime currentTime)
        {
            if (SoilHumidity > MAX_HUMIDITY && isWateringNow)
            {
                StopWatering(currentTime, "Превышена максимальная влажность (45%)");
                return;
            }

            if (isWateringNow)
            {
                DateTime wateringEnd = lastWateringStart.AddMinutes(wateringDuration);
                if (currentTime >= wateringEnd)
                {
                    StopWatering(currentTime, $"Завершение полива ({wateringReason})");
                }
                return;
            }

            bool shouldWater = false;
            string reason = "";

            if (SoilHumidity < MIN_HUMIDITY)
            {
                shouldWater = true;
                reason = "Низкая влажность почвы";
            }

            if (IsHourlyWateringTime(currentTime))
            {
                shouldWater = true;
                reason = "Ежечасный полив";
            }

            if (IsDailyWateringTime(currentTime))
            {
                shouldWater = true;
                reason = "Ежедневный полив в 6:00";
            }

            if (shouldWater)
            {
                StartWatering(currentTime, reason);
            }
        }

        private static bool IsHourlyWateringTime(DateTime currentTime)
        {
            if (currentTime.Hour >= 6 && currentTime.Hour <= 20)
            {
                if (currentTime.Minute == 0)
                {
                    if (lastHourlyWatering == DateTime.MinValue ||
                        currentTime - lastHourlyWatering >= TimeSpan.FromHours(1))
                    {
                        lastHourlyWatering = currentTime;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsDailyWateringTime(DateTime currentTime)
        {
            if (currentTime.Hour == 6 && currentTime.Minute == 0)
            {
                if (lastDailyWatering == DateTime.MinValue ||
                    currentTime.Date > lastDailyWatering.Date)
                {
                    lastDailyWatering = currentTime;
                    return true;
                }
            }
            return false;
        }

        private static void StartWatering(DateTime currentTime, string reason)
        {
            isWateringNow = true;
            lastWateringStart = currentTime;
            wateringReason = reason;

            if (reason == "Ежедневный полив в 6:00")
            {
                wateringDuration = 7.0;
            }
            else if (reason == "Ежечасный полив")
            {
                wateringDuration = 10.0;
            }
            else
            {
                wateringDuration = 5.0;
            }

            IsPumpOn = true;
            IsValveOpen = true;

            bool isOddDay = currentTime.Day % 2 == 1;
            bool isSevenAM = currentTime.Hour == 7 && currentTime.Minute == 0;

            if (isOddDay && isSevenAM)
            {
                IsFertilizerOn = true;
            }
            else
            {
                IsFertilizerOn = false;
            }

            CurrentAction = $"Полив: {reason} ({wateringDuration} мин)";
        }

        private static void StopWatering(DateTime currentTime, string reason)
        {
            isWateringNow = false;
            IsPumpOn = false;
            IsValveOpen = false;
            IsFertilizerOn = false;

            CurrentAction = "Ожидание";
        }

        private static void UpdateHumidity(DateTime currentTime)
        {
            if (!isWateringNow)
            {
                SoilHumidity -= HUMIDITY_DECREASE_PER_MINUTE;
                if (SoilHumidity < 0) SoilHumidity = 0;
            }
        }

        private static void UpdateWatering(DateTime currentTime)
        {
            if (isWateringNow)
            {
                SoilHumidity += HUMIDITY_INCREASE_PER_MINUTE;
                WaterUsed += WATER_FLOW_RATE / 60.0;

                if (IsFertilizerOn)
                {
                    FertilizerUsed += FERTILIZER_FLOW_RATE / 60.0;
                }

                if (SoilHumidity > MAX_HUMIDITY)
                {
                    SoilHumidity = MAX_HUMIDITY;
                }
            }
        }

        public static string GetNextWateringInfo(DateTime currentTime)
        {
            if (SoilHumidity < MIN_HUMIDITY)
            {
                return "Немедленно (низкая влажность)";
            }

            DateTime nextHour = currentTime.AddHours(1);
            nextHour = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 0);

            if (nextHour.Hour >= 6 && nextHour.Hour <= 20)
            {
                TimeSpan timeToNext = nextHour - currentTime;
                int minutes = (int)timeToNext.TotalMinutes;
                return $"Через {minutes} мин (ежечасный)";
            }

            DateTime nextSixAM = currentTime.Date.AddDays(1).AddHours(6);
            TimeSpan timeToSixAM = nextSixAM - currentTime;
            int minutesToSix = (int)timeToSixAM.TotalMinutes;

            return $"Через {minutesToSix} мин (утренний)";
        }

        public static void ResetCounters()
        {
            WaterUsed = 0;
            FertilizerUsed = 0;
        }

        private static void LogToFile(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                File.AppendAllText(logFilePath, $"{message}\n");
            }
            catch { }
        }
    }
}