using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Sessia
{
    public partial class MainWindow : Window
    {
        private DateTime simulatedTime;
        private DateTime startTime;
        private int totalMinutesAdded = 0;
        private DispatcherTimer timer;
        private int timeSpeedMultiplier = 10;

        public MainWindow()
        {
            InitializeComponent();

            startTime = DateTime.Now;
            simulatedTime = DateTime.Now;

            timer = new DispatcherTimer();
            timer.Tick += DispatcherTimer_Tick;
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Start();

            UpdateUI();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsedRealTime = DateTime.Now - startTime;
            int targetMinutes = (int)(elapsedRealTime.TotalSeconds / (60.0 / timeSpeedMultiplier));

            int minutesToAdd = targetMinutes - totalMinutesAdded;
            if (minutesToAdd > 0)
            {
                for (int i = 0; i < minutesToAdd; i++)
                {
                    DateTime previousTime = simulatedTime;
                    simulatedTime = simulatedTime.AddMinutes(1);
                    GreenhouseController.Update(previousTime);
                }
                totalMinutesAdded = targetMinutes;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            TimeHHMM.Text = simulatedTime.ToString("HH:mm");
            DateText.Text = simulatedTime.ToString("dd.MM.yyyy (ddd)");

            SoilHumidity.Content = $"Влажность почвы: {GreenhouseController.SoilHumidity:F1}%";
            WaterUsed.Content = $"Расход воды: {GreenhouseController.WaterUsed:F1} л";
            FertilizerUsed.Content = $"Удобрений: {GreenhouseController.FertilizerUsed:F1} мл";

            string pumpState = GreenhouseController.IsPumpOn ? "ВКЛ" : "ВЫКЛ";
            string valveState = GreenhouseController.IsValveOpen ? "ОТКР" : "ЗАКР";
            string fertilState = GreenhouseController.IsFertilizerOn ? "ВКЛ" : "ВЫКЛ";

            PumpStatus.Content = $"Насос: {pumpState}";
            ValveStatus.Content = $"Клапан: {valveState}";
            FertilizerStatus.Content = $"Дозатор: {fertilState}";

            NextWatering.Content = $"След. полив: {GreenhouseController.GetNextWateringInfo(simulatedTime)}";
            CurrentAction.Content = GreenhouseController.CurrentAction;
            LastRecord.Content = $"Последняя запись в БД: {GreenhouseController.LastDatabaseRecord}";
        }

        private void ResetData_Click(object sender, RoutedEventArgs e)
        {
            GreenhouseController.ResetCounters();
            UpdateUI();
        }

        private void SpeedButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;

            // Сброс цвета всех кнопок
            Speed1x.Background = Brushes.Transparent;
            Speed10x.Background = Brushes.Transparent;
            Speed60x.Background = Brushes.Transparent;
            Speed600x.Background = Brushes.Transparent;

            // Установка выбранной скорости
            if (clickedButton.Name == "Speed1x")
            {
                timeSpeedMultiplier = 1;
                clickedButton.Background = Brushes.Green;
            }
            else if (clickedButton.Name == "Speed10x")
            {
                timeSpeedMultiplier = 10;
                clickedButton.Background = Brushes.Green;
            }
            else if (clickedButton.Name == "Speed60x")
            {
                timeSpeedMultiplier = 60;
                clickedButton.Background = Brushes.Green;
            }
            else if (clickedButton.Name == "Speed600x")
            {
                timeSpeedMultiplier = 600;
                clickedButton.Background = Brushes.Green;
            }

            // Сброс времени для нового ускорения
            startTime = DateTime.Now;
            totalMinutesAdded = 0;
        }
    }
}