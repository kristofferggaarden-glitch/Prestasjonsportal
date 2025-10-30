using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ABBsPrestasjonsportal.Services;

namespace ABBsPrestasjonsportal
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Employee> employees = new ObservableCollection<Employee>();
        private ObservableCollection<Exercise> exercises = new ObservableCollection<Exercise>();
        private ObservableCollection<Result> results = new ObservableCollection<Result>();
        private FirebaseService firebaseService;
        private DispatcherTimer timer;
        private IDisposable employeeListener;
        private IDisposable exerciseListener;
        private IDisposable resultListener;

        public MainWindow()
        {
            InitializeComponent();
            firebaseService = new FirebaseService();
            SetupTimer();
            _ = InitializeDataAsync();
            SetupRealtimeListeners();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                SyncStatusText.Text = "⏳ Synkroniserer...";
                SyncStatusText.Foreground = System.Windows.Media.Brushes.Orange;

                // Load data from Firebase
                var employeesList = await firebaseService.GetEmployeesAsync();
                var exercisesList = await firebaseService.GetExercisesAsync();
                var resultsList = await firebaseService.GetResultsAsync();

                // Update collections on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    employees.Clear();
                    foreach (var emp in employeesList)
                        employees.Add(emp);

                    exercises.Clear();
                    foreach (var ex in exercisesList)
                        exercises.Add(ex);

                    results.Clear();
                    foreach (var res in resultsList)
                        results.Add(res);

                    // Bind to grids
                    EmployeesGrid.ItemsSource = employees;
                    ExercisesGrid.ItemsSource = exercises;
                    ResultsGrid.ItemsSource = results;

                    // Setup combos
                    UpdateCombos();
                    UpdateStatistics();
                    ShowTopList();

                    SyncStatusText.Text = "✅ Tilkoblet";
                    SyncStatusText.Foreground = System.Windows.Media.Brushes.Green;
                });

                // Add sample data if empty (first time setup)
                if (employees.Count == 0)
                {
                    await AddSampleDataAsync();
                }
            }
            catch (Exception ex)
            {
                SyncStatusText.Text = "❌ Frakoblet";
                SyncStatusText.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Kunne ikke koble til Firebase. Sjekk internettforbindelse og Firebase URL.\n\nFeil: {ex.Message}", 
                    "Tilkoblingsfeil", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetupRealtimeListeners()
        {
            // Listen for real-time updates
            employeeListener = firebaseService.ListenToEmployees(emp =>
            {
                Dispatcher.Invoke(() =>
                {
                    var existing = employees.FirstOrDefault(e => e.FirebaseKey == emp.FirebaseKey);
                    if (existing == null)
                    {
                        employees.Add(emp);
                    }
                    else
                    {
                        var index = employees.IndexOf(existing);
                        employees[index] = emp;
                    }
                    UpdateStatistics();
                    UpdateCombos();
                    ShowTopList();
                });
            });

            exerciseListener = firebaseService.ListenToExercises(ex =>
            {
                Dispatcher.Invoke(() =>
                {
                    var existing = exercises.FirstOrDefault(e => e.FirebaseKey == ex.FirebaseKey);
                    if (existing == null)
                    {
                        exercises.Add(ex);
                    }
                    else
                    {
                        var index = exercises.IndexOf(existing);
                        exercises[index] = ex;
                    }
                    UpdateStatistics();
                    UpdateCombos();
                });
            });

            resultListener = firebaseService.ListenToResults(res =>
            {
                Dispatcher.Invoke(() =>
                {
                    var existing = results.FirstOrDefault(r => r.FirebaseKey == res.FirebaseKey);
                    if (existing == null)
                    {
                        results.Add(res);
                    }
                    else
                    {
                        var index = results.IndexOf(existing);
                        results[index] = res;
                    }
                    UpdateStatistics();
                    ShowTopList();
                });
            });
        }

        private async Task AddSampleDataAsync()
        {
            var sampleEmployees = new List<Employee>
            {
                new Employee { Id = 1, Name = "Kristoffer Gaarden", Email = "kristoffer.gaarden@no.abb.com", Department = "IT" },
                new Employee { Id = 2, Name = "Ole Nordmann", Email = "ole.nordmann@no.abb.com", Department = "Salg" },
                new Employee { Id = 3, Name = "Kari Nordmann", Email = "kari.nordmann@no.abb.com", Department = "HR" }
            };

            var sampleExercises = new List<Exercise>
            {
                new Exercise { Id = 1, Name = "3000m løping", Type = "Løping", Unit = "Tid (mm:ss)" },
                new Exercise { Id = 2, Name = "Benkpress", Type = "Styrke", Unit = "Vekt (kg)" },
                new Exercise { Id = 3, Name = "10km løping", Type = "Løping", Unit = "Tid (mm:ss)" }
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
            TotalResultsText.Text = $"Resultater: {results.Count}";
        }

        private void UpdateCombos()
        {
            ResultEmployeeCombo.ItemsSource = employees;
            ResultEmployeeCombo.DisplayMemberPath = "Name";
            
            ResultExerciseCombo.ItemsSource = exercises;
            ResultExerciseCombo.DisplayMemberPath = "Name";
        }

        private void ShowTopList()
        {
            var topList = CalculateTopList();
            TopListGrid.ItemsSource = topList;

            // Update podium
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
                var employeeResults = results.Where(r => r.EmployeeId == employee.Id).ToList();
                int totalPoints = 0;
                
                // Calculate points for each exercise
                foreach (var exercise in exercises)
                {
                    var exerciseResults = results.Where(r => r.ExerciseId == exercise.Id)
                                                 .OrderByDescending(r => GetResultScore(r, exercise))
                                                 .ToList();
                    
                    var employeeResult = exerciseResults.FirstOrDefault(r => r.EmployeeId == employee.Id);
                    if (employeeResult != null)
                    {
                        int position = exerciseResults.IndexOf(employeeResult) + 1;
                        int points = CalculatePoints(position, exerciseResults.Count);
                        totalPoints += points;
                    }
                }
                
                topList.Add(new TopListEntry
                {
                    Name = employee.Name,
                    Department = employee.Department,
                    ExerciseCount = employeeResults.Select(r => r.ExerciseId).Distinct().Count(),
                    TotalPoints = totalPoints
                });
            }
            
            // Sort and add rank
            topList = topList.OrderByDescending(t => t.TotalPoints).ToList();
            for (int i = 0; i < topList.Count; i++)
            {
                topList[i].Rank = i + 1;
            }
            
            return topList;
        }

        private double GetResultScore(Result result, Exercise exercise)
        {
            // Convert result to score (higher is better)
            if (exercise.Unit.Contains("Tid"))
            {
                // For time, lower is better, so invert
                return 10000.0 / ParseTime(result.Value);
            }
            else
            {
                // For weight, reps, distance - higher is better
                if (double.TryParse(result.Value, out double value))
                    return value;
            }
            return 0;
        }

        private double ParseTime(string time)
        {
            // Parse time format mm:ss to seconds
            var parts = time.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int seconds))
                {
                    return minutes * 60 + seconds;
                }
            }
            return 9999;
        }

        private int CalculatePoints(int position, int totalParticipants)
        {
            // Points system
            switch (position)
            {
                case 1: return 100;
                case 2: return 80;
                case 3: return 60;
                case 4: return 50;
                case 5: return 40;
                case 6: return 30;
                case 7: return 20;
                case 8: return 15;
                case 9: return 10;
                case 10: return 5;
                default: return 1;
            }
        }

        // Navigation
        private void BtnTopList_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            TopListView.Visibility = Visibility.Visible;
            ShowTopList();
        }

        private void BtnEmployees_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            EmployeesView.Visibility = Visibility.Visible;
        }

        private void BtnExercises_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ExercisesView.Visibility = Visibility.Visible;
        }

        private void BtnResults_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ResultsView.Visibility = Visibility.Visible;
        }

        private void HideAllViews()
        {
            TopListView.Visibility = Visibility.Collapsed;
            EmployeesView.Visibility = Visibility.Collapsed;
            ExercisesView.Visibility = Visibility.Collapsed;
            ResultsView.Visibility = Visibility.Collapsed;
        }

        // Employee Management
        private async void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewEmployeeName.Text))
            {
                MessageBox.Show("Vennligst fyll inn navn", "Feil", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var employee = new Employee
            {
                Id = employees.Count > 0 ? employees.Max(e => e.Id) + 1 : 1,
                Name = NewEmployeeName.Text,
                Email = NewEmployeeEmail.Text,
                Department = (DepartmentCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Ukjent"
            };

            await firebaseService.AddEmployeeAsync(employee);
            
            NewEmployeeName.Clear();
            NewEmployeeEmail.Clear();
            DepartmentCombo.SelectedIndex = -1;
            
            MessageBox.Show($"Ansatt {employee.Name} lagt til!", "Suksess", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Employee employee)
            {
                if (MessageBox.Show($"Er du sikker på at du vil slette {employee.Name}?", "Bekreft sletting", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await firebaseService.DeleteEmployeeAsync(employee.FirebaseKey);
                    employees.Remove(employee);
                }
            }
        }

        // Exercise Management
        private async void AddExercise_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewExerciseName.Text))
            {
                MessageBox.Show("Vennligst fyll inn øvelsesnavn", "Feil", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exercise = new Exercise
            {
                Id = exercises.Count > 0 ? exercises.Max(ex => ex.Id) + 1 : 1,
                Name = NewExerciseName.Text,
                Type = (ExerciseTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Annet",
                Unit = (ExerciseUnitCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Poeng"
            };

            await firebaseService.AddExerciseAsync(exercise);
            
            NewExerciseName.Clear();
            ExerciseTypeCombo.SelectedIndex = -1;
            ExerciseUnitCombo.SelectedIndex = -1;
            
            MessageBox.Show($"Øvelse {exercise.Name} lagt til!", "Suksess", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteExercise_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Exercise exercise)
            {
                if (MessageBox.Show($"Er du sikker på at du vil slette {exercise.Name}?", "Bekreft sletting", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await firebaseService.DeleteExerciseAsync(exercise.FirebaseKey);
                    exercises.Remove(exercise);
                }
            }
        }

        // Result Management
        private async void AddResult_Click(object sender, RoutedEventArgs e)
        {
            if (ResultEmployeeCombo.SelectedItem == null || ResultExerciseCombo.SelectedItem == null || 
                string.IsNullOrWhiteSpace(ResultValue.Text))
            {
                MessageBox.Show("Vennligst fyll inn alle felt", "Feil", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var employee = ResultEmployeeCombo.SelectedItem as Employee;
            var exercise = ResultExerciseCombo.SelectedItem as Exercise;

            var result = new Result
            {
                Id = results.Count > 0 ? results.Max(r => r.Id) + 1 : 1,
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                ExerciseId = exercise.Id,
                ExerciseName = exercise.Name,
                Value = ResultValue.Text,
                Date = DateTime.Now,
                Points = 0 // Will be calculated
            };

            await firebaseService.AddResultAsync(result);
            
            ResultEmployeeCombo.SelectedIndex = -1;
            ResultExerciseCombo.SelectedIndex = -1;
            ResultValue.Clear();
            
            MessageBox.Show("Resultat registrert!", "Suksess", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                EmployeesGrid.ItemsSource = employees;
            }
            else
            {
                var filtered = employees.Where(emp => 
                    emp.Name.ToLower().Contains(searchText) || 
                    emp.Email.ToLower().Contains(searchText) ||
                    emp.Department.ToLower().Contains(searchText)).ToList();
                EmployeesGrid.ItemsSource = filtered;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Clean up listeners
            employeeListener?.Dispose();
            exerciseListener?.Dispose();
            resultListener?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // Model class for TopList
    public class TopListEntry
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public int ExerciseCount { get; set; }
        public int TotalPoints { get; set; }
    }
}