using System.Windows;
using System.Windows.Controls;

namespace ChestTracker;

public partial class MainWindow
{
    private bool refreshingDays;

    private void RefreshHistoryDays()
    {
        var date = (HistoryDayList.SelectedItem as HistoryDay)?.Date;
        var matchId = (HistoryGrid.SelectedItem as MatchEntry)?.Id;
        var days = store.HistoryDays();
        refreshingDays = true;
        HistoryDayList.ItemsSource = days;
        HistoryDayList.SelectedItem = days.FirstOrDefault(day => day.Date == date) ?? days.FirstOrDefault();
        refreshingDays = false;
        DisplayHistoryDay(matchId);
    }

    private void SelectHistoryDay(object sender, SelectionChangedEventArgs e)
    {
        if (refreshingDays) return;
        if (!watcher.Busy) RecountFeedback.Text = "";
        DisplayHistoryDay();
    }

    private void DisplayHistoryDay(string? matchId = null)
    {
        var day = HistoryDayList.SelectedItem as HistoryDay;
        HistoryGrid.ItemsSource = day?.Matches;
        HistoryGrid.SelectedItem = day?.Matches.FirstOrDefault(match => match.Id == matchId);
        HistoryDaySummary.Text = day == null ? "Пока нет записей" : $"За день · {day.Total:N0} сундуков";
        if (day?.Manual > 0) HistoryDaySummary.Text += $"   ·   вручную {day.Manual:N0}";
        RefreshMatchButtons();
    }

    private void SelectHistoryMatch(object sender, SelectionChangedEventArgs e) => RefreshMatchButtons();

    private void RefreshMatchButtons()
    {
        bool hasSelection = HistoryGrid.SelectedItem is MatchEntry;
        MatchDetailsButton.IsEnabled = hasSelection;
        RecountButton.IsEnabled = hasSelection && !watcher.Busy;
        DeleteMatchButton.IsEnabled = hasSelection && !watcher.Busy;
    }

    private async void RecountSelectedMatch(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not MatchEntry match) return;
        RecountFeedback.Text = $"Анализирую повтор от {match.DateLabel}…";
        try
        {
            long delta = await watcher.RecountMatchAsync(match);
            RecountFeedback.Text = delta == 0
                ? "Пересчёт завершён. Количество сундуков совпало."
                : $"Пересчёт завершён. Изменение общего количества {delta:+#,0;-#,0}.";
        }
        catch (OperationCanceledException) { RecountFeedback.Text = "Пересчёт остановлен."; }
        catch (Exception error) { RecountFeedback.Text = error.Message; }
    }
}
