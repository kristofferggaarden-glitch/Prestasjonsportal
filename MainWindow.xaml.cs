using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ABBsPrestasjonsportal.Services;

namespace ABBsPrestasjonsportal
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Employee> employees = new ObservableCollection<Employee>();
        private ObservableCollection<Exercise> exercises = new ObservableCollection<Exercise>();
        private ObservableCollection<Result> results = new ObservableCollection<Result>();
        private ObservableCollection<CombinedResultView> combinedResults = new ObservableCollection<CombinedResultView>();
        private FirebaseService firebaseService;
        private DispatcherTimer timer;
        private IDisposable employeeListener;
        private IDisposable exerciseListener;
        private IDisposable resultListener;

        private bool isAdmin;
        private string currentUser;
        private int minParticipants = 3;

        public MainWindow(bool admin, string user)
        {
            InitializeComponent();

            isAdmin = admin;
            currentUser = user;

            // Show/hide admin-only features
            if (isAdmin)
            {
                BtnPending.Visibility = Visibility.Visible;
                BtnSettings.Visibility = Visibility.Visible;
                PendingCountText.Visibility = Visibility.Visible;
            }
            else
            {
                // Hide delete buttons for non-admin
                EmployeesGrid.Loaded += (s, e) => HideDeleteButtonsForNonAdmin();
            }

            UserText.Text = $"Innlogget som: {currentUser} {(isAdmin ? "(Admin)" : "")}";

            firebaseService = new FirebaseService();
            SetupTimer();
            _ = InitializeDataAsync();
            SetupRealtimeListeners();
        }

        private void HideDeleteButtonsForNonAdmin()
        {
            if (!isAdmin)
            {
                var column = EmployeesGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Handlinger");
                if (column != null)
                    column.Visibility = Visibility.Collapsed;
            }
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                SyncStatusText.Text = "⏳ Synkroniserer...";
                SyncStatusText.Foreground = Brushes.Orange;

                await ReloadAllDataAsync();

                SyncStatusText.Text = "✅ Tilkoblet";
                SyncStatusText.Foreground = Brushes.Green;

                if (employees.Count == 0)
                {
                    await AddSampleDataAsync();
                }
            }
            catch (Exception ex)
            {
                SyncStatusText.Text = "❌ Frakoblet";
                SyncStatusText.Foreground = Brushes.Red;
                MessageBox.Show($"Kunne ikke koble til Firebase.\n\n{ex.Message}",
                    "Tilkoblingsfeil", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task ReloadAllDataAsync()
        {
            var employeesList = await firebaseService.GetEmployeesAsync();
            var exercisesList = await firebaseService.GetExercisesAsync();
            var resultsList = await firebaseService.GetResultsAsync();

            await Dispatcher.InvokeAsync(() =>
            {
                employees.Clear();
                foreach (var emp in employeesList)
                {
                    emp.TotalPoints = CalculateEmployeePoints(emp.Id, resultsList);
                    emp.Badges = CalculateEmployeeBadges(emp.Id, resultsList, exercisesList);
                    employees.Add(emp);
                }

                exercises.Clear();
                foreach (var ex in exercisesList)
                    exercises.Add(ex);

                results.Clear();
                foreach (var res in resultsList)
                    results.Add(res);

                EmployeesGrid.ItemsSource = employees;
                CombinedGrid.ItemsSource = combinedResults;
                PendingGrid.ItemsSource = results.Where(r => r.Status == "Pending");

                UpdateCombos();
                UpdateStatistics();
                UpdateCombinedGrid();
                ShowTopList();
                UpdateDepartmentComparison();
                UpdatePendingCount();
                LoadAchievements();
            });
        }

        private int CalculateEmployeePoints(int employeeId, List<Result> allResults)
        {
            return allResults.Where(r => r.EmployeeId == employeeId && r.Status == "Approved")
                            .Sum(r => r.Points);
        }

        private string CalculateEmployeeBadges(int employeeId, List<Result> allResults, List<Exercise> allExercises)
        {
            var badges = new List<string>();
            var empResults = allResults.Where(r => r.EmployeeId == employeeId && r.Status == "Approved").ToList();

            // First Place badge
            if (empResults.Count(r => r.Points == 10) >= 3)
                badges.Add("🥇×3");

            // Versatile badge
            if (empResults.Select(r => r.ExerciseId).Distinct().Count() >= 5)
                badges.Add("🌟");

            // Consistency badge
            if (empResults.Count >= 10)
                badges.Add("💪");

            return string.Join(" ", badges);
        }

        private void SetupRealtimeListeners()
        {
            employeeListener = firebaseService.ListenToEmployees(emp =>
            {
                Dispatcher.Invoke(() => { _ = ReloadAllDataAsync(); });
            });

            exerciseListener = firebaseService.ListenToExercises(ex =>
            {
                Dispatcher.Invoke(() => { _ = ReloadAllDataAsync(); });
            });

            resultListener = firebaseService.ListenToResults(res =>
            {
                Dispatcher.Invoke(() => { _ = ReloadAllDataAsync(); });
            });
        }

        private async Task AddSampleDataAsync()
        {
            var sampleEmployees = new List<Employee>
            {
                new Employee { Id = 1, Name = "Kristoffer Gaarden", Department = "IT" },
                new Employee { Id = 2, Name = "Ole Nordmann", Department = "Salg" },
                new Employee { Id = 3, Name = "Kari Nordmann", Department = "HR" }
            };

            var sampleExercises = new List<Exercise>
            {
                new Exercise { Id = 1, Name = "3000m løping", Type = "Løping (TT:MM:SS)", Unit = "Løping (TT:MM:SS)" },
                new Exercise { Id = 2, Name = "Benkpress", Type = "Styrke (kg)", Unit = "Styrke (kg)" },
                new Exercise { Id = 3, Name = "Pullups", Type = "Repetisjoner (reps)", Unit = "Repetisjoner (reps)" }
            };

            foreach (var emp in sampleEmployees)
            {
                await firebaseService.AddEmployeeAsync(emp);
            }

            foreach (var ex in sampleExercises)
            {
                await firebaseService.AddExerciseAsync(ex);
            }
        }

        private void SetupTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => DateTimeText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            timer.Start();
        }

        private void UpdateStatistics()
        {
            TotalEmployeesText.Text = $"Ansatte: {employees.Count}";
            TotalExercisesText.Text = $"Øvelser: {exercises.Count}";
            TotalResultsText.Text = $"Resultater: {results.Count(r => r.Status == "Approved")}";
        }

        private void UpdatePendingCount()
        {
            if (isAdmin)
            {
                int pending = results.Count(r => r.Status == "Pending");
                PendingCountText.Text = $"Ventende: {pending}";
                if (pending > 0)
                    PendingCountText.Foreground = Brushes.Red;
                else
                    PendingCountText.Foreground = Brushes.Green;
            }
        }

        private void UpdateCombos()
        {
            ResultEmployeeCombo.ItemsSource = employees;
            ResultEmployeeCombo.DisplayMemberPath = "Name";

            ResultExerciseCombo.ItemsSource = exercises;
            ResultExerciseCombo.DisplayMemberPath = "Name";
        }

        private void UpdateCombinedGrid()
        {
            combinedResults.Clear();

            foreach (var result in results.OrderByDescending(r => r.Date))
            {
                var employee = employees.FirstOrDefault(e => e.Id == result.EmployeeId);
                var exercise = exercises.FirstOrDefault(e => e.Id == result.ExerciseId);

                if (employee != null && exercise != null)
                {
                    string pace = "";
                    if (exercise.Type.Contains("Løping") || exercise.Type.Contains("Tid"))
                    {
                        pace = CalculatePace(result.Value, exercise.Name);
                    }

                    combinedResults.Add(new CombinedResultView
                    {
                        ResultId = result.Id,
                        FirebaseKey = result.FirebaseKey,
                        ExerciseName = exercise.Name,
                        ExerciseType = exercise.Type,
                        EmployeeName = employee.Name,
                        Value = result.Value,
                        Pace = pace,
                        Date = result.Date,
                        Status = result.Status == "Approved" ? "Godkjent" : "Venter",
                        Points = result.Points
                    });
                }
            }
        }

        private string CalculatePace(string timeString, string exerciseName)
        {
            try
            {
                // Extract distance from exercise name if possible
                var match = Regex.Match(exerciseName, @"(\d+)");
                if (!match.Success) return "";

                double distanceMeters = double.Parse(match.Value);
                if (exerciseName.ToLower().Contains("km"))
                    distanceMeters *= 1000;

                double distanceKm = distanceMeters / 1000.0;

                // Parse time
                var parts = timeString.Split(':');
                double totalMinutes = 0;

                if (parts.Length == 3)
                {
                    totalMinutes = int.Parse(parts[0]) * 60 + int.Parse(parts[1]) + double.Parse(parts[2]) / 60.0;
                }
                else if (parts.Length == 2)
                {
                    totalMinutes = int.Parse(parts[0]) + double.Parse(parts[1]) / 60.0;
                }

                if (distanceKm > 0)
                {
                    double paceMinPerKm = totalMinutes / distanceKm;
                    int min = (int)paceMinPerKm;
                    int sec = (int)((paceMinPerKm - min) * 60);
                    return $"{min}:{sec:D2}/km";
                }
            }
            catch { }

            return "";
        }

        private void ShowTopList()
        {
            var topList = CalculateTopList();
            TopListGrid.ItemsSource = topList;

            if (topList.Count > 0)
            {
                Gold_Name.Text = topList[0].Name;
                Gold_Points.Text = $"{topList[0].TotalPoints} poeng";
            }
            if (topList.Count > 1)
            {
                Silver_Name.Text = topList[1].Name;
                Silver_Points.Text = $"{topList[1].TotalPoints} poeng";
            }
            if (topList.Count > 2)
            {
                Bronze_Name.Text = topList[2].Name;
                Bronze_Points.Text = $"{topList[2].TotalPoints} poeng";
            }
        }

        private List<TopListEntry> CalculateTopList()
        {
            var topList = new List<TopListEntry>();

            foreach (var employee in employees)
            {
                var employeeResults = results.Where(r => r.EmployeeId == employee.Id && r.Status == "Approved").ToList();
                int totalPoints = 0;

                foreach (var exercise in exercises)
                {
                    var exerciseResults = results.Where(r => r.ExerciseId == exercise.Id && r.Status == "Approved")
                                                 .OrderByDescending(r => GetResultScore(r, exercise))
                                                 .ToList();

                    var employeeResult = exerciseResults.FirstOrDefault(r => r.EmployeeId == employee.Id);
                    if (employeeResult != null)
                    {
                        int position = exerciseResults.IndexOf(employeeResult) + 1;
                        int points = CalculatePoints(position, exerciseResults.Count);
                        totalPoints += points;

                        if (employeeResult.Points != points)
                        {
                            employeeResult.Points = points;
                            _ = firebaseService.UpdateResultAsync(employeeResult);
                        }
                    }
                }

                topList.Add(new TopListEntry
                {
                    Name = employee.Name,
                    Department = employee.Department,
                    Badges = employee.Badges,
                    TotalPoints = totalPoints
                });
            }

            topList = topList.OrderByDescending(t => t.TotalPoints).ToList();
            for (int i = 0; i < topList.Count; i++)
            {
                topList[i].Rank = i + 1;
            }

            return topList;
        }

        private void UpdateDepartmentComparison()
        {
            var departments = employees.GroupBy(e => e.Department)
                                      .Where(g => g.Count() >= minParticipants)
                                      .Select(g => new DepartmentEntry
                                      {
                                          Department = g.Key,
                                          ParticipantCount = g.Count(),
                                          TotalPoints = g.Sum(e => e.TotalPoints),
                                          AvgPoints = g.Average(e => e.TotalPoints),
                                          ScorePercent = (g.Average(e => e.TotalPoints) / (g.Count() > 0 ? g.Max(e => e.TotalPoints > 0 ? e.TotalPoints : 1) : 1)) * 100
                                      })
                                      .OrderByDescending(d => d.ScorePercent)
                                      .ToList();

            for (int i = 0; i < departments.Count; i++)
            {
                departments[i].Rank = i + 1;
            }

            DepartmentGrid.ItemsSource = departments;
        }

        private double GetResultScore(Result result, Exercise exercise)
        {
            if (exercise.Type.Contains("Tid") || exercise.Type.Contains("Løping"))
            {
                return 10000.0 / ParseTime(result.Value);
            }
            else
            {
                if (double.TryParse(result.Value.Replace(',', '.'), out double value))
                    return value;
            }
            return 0;
        }

        private double ParseTime(string time)
        {
            var parts = time.Split(':');
            if (parts.Length == 3)
            {
                if (int.TryParse(parts[0], out int hours) &&
                    int.TryParse(parts[1], out int minutes) &&
                    int.TryParse(parts[2], out int seconds))
                {
                    return hours * 3600 + minutes * 60 + seconds;
                }
            }
            else if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int minutes) &&
                    int.TryParse(parts[1], out int seconds))
                {
                    return minutes * 60 + seconds;
                }
            }
            return 9999;
        }

        private int CalculatePoints(int position, int totalParticipants)
        {
            switch (position)
            {
                case 1: return 10;
                case 2: return 6;
                case 3: return 4;
                case 4: return 3;
                case 5: return 2;
                default: return 1;
            }
        }

        private bool ValidateResultValue(string value, string exerciseType)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (exerciseType.Contains("Tid") || exerciseType.Contains("Løping"))
            {
                var timeRegex = new Regex(@"^(\d{1,2}):([0-5][0-9]):([0-5][0-9])$");
                return timeRegex.IsMatch(value);
            }
            else if (exerciseType.Contains("Styrke") || exerciseType.Contains("Repetisjoner") || exerciseType.Contains("Distanse"))
            {
                var numberRegex = new Regex(@"^(\d+)([.,]\d+)?$");
                return numberRegex.IsMatch(value);
            }
            else
            {
                return true;
            }
        }

        private void LoadAchievements()
        {
            AchievementsPanel.Children.Clear();

            var achievements = new List<Achievement>
            {
                new Achievement { Icon = "🥇", Name = "Triple Gold", Description = "Få 1. plass i 3 forskjellige øvelser", Points = "3×10p" },
                new Achievement { Icon = "🌟", Name = "Allsidig", Description = "Delta i 5 eller flere forskjellige øvelser", Points = "Bonus" },
                new Achievement { Icon = "💪", Name = "Dedikert", Description = "Registrer 10 eller flere resultater", Points = "Bonus" },
                new Achievement { Icon = "🔥", Name = "På topp", Description = "Hold 1. plass i 7 dager", Points = "Kommende" },
                new Achievement { Icon = "🚀", Name = "Comeback", Description = "Forbedre ditt eget resultat med 20%", Points = "Kommende" }
            };

            foreach (var ach in achievements)
            {
                var border = new Border
                {
                    Style = (Style)FindResource("Card"),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new TextBlock { Text = ach.Icon, FontSize = 36, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(icon, 0);

                var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(stack, 1);
                stack.Children.Add(new TextBlock { Text = ach.Name, FontWeight = FontWeights.Bold, FontSize = 16 });
                stack.Children.Add(new TextBlock { Text = ach.Description, Foreground = Brushes.Gray });

                var points = new TextBlock { Text = ach.Points, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(points, 2);

                grid.Children.Add(icon);
                grid.Children.Add(stack);
                grid.Children.Add(points);
                border.Child = grid;

                AchievementsPanel.Children.Add(border);
            }
        }

        // Navigation
        private void BtnTopList_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            TopListView.Visibility = Visibility.Visible;
            ShowTopList();
        }

        private void BtnDepartments_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            DepartmentsView.Visibility = Visibility.Visible;
            UpdateDepartmentComparison();
        }

        private void BtnEmployees_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            EmployeesView.Visibility = Visibility.Visible;
            NewEmployeeName.Focus();
        }

        private void BtnResults_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ResultsView.Visibility = Visibility.Visible;
        }

        private void BtnAchievements_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            AchievementsView.Visibility = Visibility.Visible;
            LoadAchievements();
        }

        private void BtnPending_Click(object sender, RoutedEventArgs e)
        {
            if (!isAdmin) return;
            HideAllViews();
            PendingView.Visibility = Visibility.Visible;

            // Oppdater pending grid
            PendingGrid.ItemsSource = null;
            PendingGrid.ItemsSource = results.Where(r => r.Status == "Pending").ToList();
        }

        private void BtnPaceCalc_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            PaceCalcView.Visibility = Visibility.Visible;
        }

        private void BtnPointSystem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "🏆 POENGSYSTEM\n\n" +
                "1. plass = 10 poeng\n" +
                "2. plass = 6 poeng\n" +
                "3. plass = 4 poeng\n" +
                "4. plass = 3 poeng\n" +
                "5. plass = 2 poeng\n" +
                "6+ plass = 1 poeng\n\n" +
                "For tidsbaserte øvelser: lavest tid = best\n" +
                "For andre øvelser: høyest verdi = best",
                "Poengsystem",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!isAdmin) return;
            HideAllViews();
            SettingsView.Visibility = Visibility.Visible;
        }

        private void HideAllViews()
        {
            TopListView.Visibility = Visibility.Collapsed;
            DepartmentsView.Visibility = Visibility.Collapsed;
            EmployeesView.Visibility = Visibility.Collapsed;
            ResultsView.Visibility = Visibility.Collapsed;
            AchievementsView.Visibility = Visibility.Collapsed;
            PendingView.Visibility = Visibility.Collapsed;
            PaceCalcView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
        }

        // Employee Management
        private void NewEmployeeName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DepartmentCombo.Focus();
                e.Handled = true;
            }
        }

        private void DepartmentCombo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddEmployee_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewEmployeeName.Text))
            {
                NewEmployeeName.Focus();
                return;
            }

            if (DepartmentCombo.SelectedItem == null)
            {
                DepartmentCombo.Focus();
                return;
            }

            var employee = new Employee
            {
                Id = employees.Count > 0 ? employees.Max(emp => emp.Id) + 1 : 1,
                Name = NewEmployeeName.Text,
                Department = (DepartmentCombo.SelectedItem as ComboBoxItem)?.Content.ToString()
            };

            await firebaseService.AddEmployeeAsync(employee);

            // Oppdater UI umiddelbart
            await ReloadAllDataAsync();

            NewEmployeeName.Clear();
            DepartmentCombo.SelectedIndex = -1;
            NewEmployeeName.Focus();
        }

        private async void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (!isAdmin)
            {
                MessageBox.Show("Kun admin kan slette brukere!", "Ingen tilgang",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            if (button?.Tag is Employee employee)
            {
                if (MessageBox.Show($"Slett {employee.Name}?", "Bekreft",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await firebaseService.DeleteEmployeeAsync(employee.FirebaseKey);

                    // Oppdater UI umiddelbart
                    await ReloadAllDataAsync();
                }
            }
        }

        // Exercise Management
        private async void AddExercise_Click(object sender, RoutedEventArgs e)
        {
            // Kun admin kan legge til øvelser
            if (!isAdmin)
            {
                MessageBox.Show("Kun admin kan legge til øvelser!", "Ingen tilgang",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewExerciseName.Text) || ExerciseTypeCombo.SelectedItem == null)
                return;

            var type = (ExerciseTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Annet";

            var exercise = new Exercise
            {
                Id = exercises.Count > 0 ? exercises.Max(ex => ex.Id) + 1 : 1,
                Name = NewExerciseName.Text,
                Type = type,
                Unit = type
            };

            await firebaseService.AddExerciseAsync(exercise);

            // Oppdater UI umiddelbart
            await ReloadAllDataAsync();

            NewExerciseName.Clear();
            ExerciseTypeCombo.SelectedIndex = -1;
        }

        // Result Management
        private void ResultExerciseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultExerciseCombo.SelectedItem is Exercise exercise)
            {
                if (exercise.Type.Contains("Tid") || exercise.Type.Contains("Løping"))
                {
                    ResultLabel.Text = "Resultat (TT:MM:SS)";
                    ResultHintText.Text = "";
                }
                else if (exercise.Type.Contains("Styrke"))
                {
                    ResultLabel.Text = "Resultat (kg)";
                    ResultHintText.Text = "";
                }
                else if (exercise.Type.Contains("Repetisjoner"))
                {
                    ResultLabel.Text = "Resultat (reps)";
                    ResultHintText.Text = "";
                }
                else if (exercise.Type.Contains("Distanse"))
                {
                    ResultLabel.Text = "Resultat (m)";
                    ResultHintText.Text = "";
                }
                else
                {
                    ResultLabel.Text = "Resultat";
                    ResultHintText.Text = "";
                }
            }
        }

        private void ResultValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddResult_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void AddResult_Click(object sender, RoutedEventArgs e)
        {
            if (ResultEmployeeCombo.SelectedItem == null || ResultExerciseCombo.SelectedItem == null ||
                string.IsNullOrWhiteSpace(ResultValue.Text))
                return;

            var employee = ResultEmployeeCombo.SelectedItem as Employee;
            var exercise = ResultExerciseCombo.SelectedItem as Exercise;

            if (!ValidateResultValue(ResultValue.Text, exercise.Type))
            {
                MessageBox.Show("Ugyldig format!", "Feil", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = new Result
            {
                Id = results.Count > 0 ? results.Max(r => r.Id) + 1 : 1,
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                ExerciseId = exercise.Id,
                ExerciseName = exercise.Name,
                Value = ResultValue.Text,
                Date = DateTime.Now,
                Status = isAdmin ? "Approved" : "Pending",
                Points = 0
            };

            await firebaseService.AddResultAsync(result);

            // Oppdater UI umiddelbart
            await ReloadAllDataAsync();

            // Vis melding til gjest
            if (!isAdmin)
            {
                MessageBox.Show("Resultatet ditt er sendt til godkjenning!", "Suksess",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ResultEmployeeCombo.SelectedIndex = -1;
            ResultExerciseCombo.SelectedIndex = -1;
            ResultValue.Clear();
            ResultHintText.Text = "";
        }

        private async void ApproveResult_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Result result)
            {
                result.Status = "Approved";
                await firebaseService.UpdateResultAsync(result);

                // Oppdater UI umiddelbart
                await ReloadAllDataAsync();

                MessageBox.Show("Resultat godkjent!", "Suksess",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void RejectResult_Click(object sender, RoutedEventArgs e)
        {
            if (!isAdmin) return;

            var button = sender as Button;
            if (button?.Tag is Result result)
            {
                if (MessageBox.Show("Er du sikker på at du vil avslå dette resultatet?", "Bekreft",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await firebaseService.DeleteResultAsync(result.FirebaseKey);

                    // Oppdater UI umiddelbart
                    await ReloadAllDataAsync();

                    MessageBox.Show("Resultat avslått og slettet!", "Suksess",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Pace Calculator
        private void PaceCalc_Changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(PaceDistance.Text) && !string.IsNullOrWhiteSpace(PaceTime.Text))
                {
                    double distance = double.Parse(PaceDistance.Text);
                    var timeParts = PaceTime.Text.Split(':');
                    double totalMinutes = 0;

                    if (timeParts.Length == 3)
                        totalMinutes = int.Parse(timeParts[0]) * 60 + int.Parse(timeParts[1]) + double.Parse(timeParts[2]) / 60.0;
                    else if (timeParts.Length == 2)
                        totalMinutes = int.Parse(timeParts[0]) + double.Parse(timeParts[1]) / 60.0;

                    double paceMin = totalMinutes / distance;
                    double speed = (distance / totalMinutes) * 60;

                    int min = (int)paceMin;
                    int sec = (int)((paceMin - min) * 60);

                    PaceResult1.Text = $"Pace: {min}:{sec:D2} per km";
                    PaceResult2.Text = $"Fart: {speed:F2} km/h";
                    PaceResult3.Text = $"Total tid: {PaceTime.Text}";
                }
                else if (!string.IsNullOrWhiteSpace(PaceDistance.Text) && !string.IsNullOrWhiteSpace(PaceSpeed.Text))
                {
                    double distance = double.Parse(PaceDistance.Text);
                    double speed = double.Parse(PaceSpeed.Text);

                    double totalMinutes = (distance / speed) * 60;
                    int hours = (int)(totalMinutes / 60);
                    int min = (int)(totalMinutes % 60);
                    int sec = (int)((totalMinutes - Math.Floor(totalMinutes)) * 60);

                    PaceResult1.Text = $"Tid: {hours:D2}:{min:D2}:{sec:D2}";
                    PaceResult2.Text = $"Pace: {60.0 / speed:F2} min/km";
                    PaceResult3.Text = $"Fart: {speed} km/h";
                }
            }
            catch { }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(MinParticipantsBox.Text, out int value))
            {
                minParticipants = value;
                UpdateDepartmentComparison();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            employeeListener?.Dispose();
            exerciseListener?.Dispose();
            resultListener?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // Model classes
    public class TopListEntry
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public string Badges { get; set; }
        public int TotalPoints { get; set; }
    }

    public class DepartmentEntry
    {
        public int Rank { get; set; }
        public string Department { get; set; }
        public int ParticipantCount { get; set; }
        public int TotalPoints { get; set; }
        public double AvgPoints { get; set; }
        public double ScorePercent { get; set; }
    }

    public class CombinedResultView
    {
        public int ResultId { get; set; }
        public string FirebaseKey { get; set; }
        public string ExerciseName { get; set; }
        public string ExerciseType { get; set; }
        public string EmployeeName { get; set; }
        public string Value { get; set; }
        public string Pace { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public int Points { get; set; }
    }

    public class Achievement
    {
        public string Icon { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Points { get; set; }
    }
}