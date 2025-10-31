using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;

namespace ABBsPrestasjonsportal.Services
{
    public class FirebaseService
    {
        // BYTT UT DENNE URL-EN MED DIN EGEN FIREBASE URL
        private const string FirebaseDatabaseUrl = "https://abbs-prestasjonsportal-default-rtdb.europe-west1.firebasedatabase.app/";
        private FirebaseClient client;

        public FirebaseService()
        {
            Connect();
        }

        private void Connect()
        {
            if (client == null)
            {
                client = new FirebaseClient(FirebaseDatabaseUrl);
            }
        }

        // Employees
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            try
            {
                var employees = await client
                    .Child("employees")
                    .OnceAsync<Employee>();

                return employees.Select(e =>
                {
                    e.Object.FirebaseKey = e.Key;
                    return e.Object;
                }).ToList();
            }
            catch
            {
                return new List<Employee>();
            }
        }

        public async Task<Employee> AddEmployeeAsync(Employee employee)
        {
            var result = await client
                .Child("employees")
                .PostAsync(employee);

            employee.FirebaseKey = result.Key;
            return employee;
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            if (!string.IsNullOrEmpty(employee.FirebaseKey))
            {
                await client
                    .Child("employees")
                    .Child(employee.FirebaseKey)
                    .PutAsync(employee);
            }
        }

        public async Task DeleteEmployeeAsync(string firebaseKey)
        {
            if (!string.IsNullOrEmpty(firebaseKey))
            {
                await client
                    .Child("employees")
                    .Child(firebaseKey)
                    .DeleteAsync();
            }
        }

        // Exercises
        public async Task<List<Exercise>> GetExercisesAsync()
        {
            try
            {
                var exercises = await client
                    .Child("exercises")
                    .OnceAsync<Exercise>();

                return exercises.Select(e =>
                {
                    e.Object.FirebaseKey = e.Key;
                    return e.Object;
                }).ToList();
            }
            catch
            {
                return new List<Exercise>();
            }
        }

        public async Task<Exercise> AddExerciseAsync(Exercise exercise)
        {
            var result = await client
                .Child("exercises")
                .PostAsync(exercise);

            exercise.FirebaseKey = result.Key;
            return exercise;
        }

        public async Task UpdateExerciseAsync(Exercise exercise)
        {
            if (!string.IsNullOrEmpty(exercise.FirebaseKey))
            {
                await client
                    .Child("exercises")
                    .Child(exercise.FirebaseKey)
                    .PutAsync(exercise);
            }
        }

        public async Task DeleteExerciseAsync(string firebaseKey)
        {
            if (!string.IsNullOrEmpty(firebaseKey))
            {
                await client
                    .Child("exercises")
                    .Child(firebaseKey)
                    .DeleteAsync();
            }
        }

        // Results
        public async Task<List<Result>> GetResultsAsync()
        {
            try
            {
                var results = await client
                    .Child("results")
                    .OnceAsync<Result>();

                return results.Select(r =>
                {
                    r.Object.FirebaseKey = r.Key;
                    return r.Object;
                }).ToList();
            }
            catch
            {
                return new List<Result>();
            }
        }

        public async Task<Result> AddResultAsync(Result result)
        {
            var resultObj = await client
                .Child("results")
                .PostAsync(result);

            result.FirebaseKey = resultObj.Key;
            return result;
        }

        public async Task UpdateResultAsync(Result result)
        {
            if (!string.IsNullOrEmpty(result.FirebaseKey))
            {
                await client
                    .Child("results")
                    .Child(result.FirebaseKey)
                    .PutAsync(result);
            }
        }

        public async Task DeleteResultAsync(string firebaseKey)
        {
            if (!string.IsNullOrEmpty(firebaseKey))
            {
                await client
                    .Child("results")
                    .Child(firebaseKey)
                    .DeleteAsync();
            }
        }

        // Real-time listeners
        public IDisposable ListenToEmployees(Action<Employee> onEmployeeChanged)
        {
            return client
                .Child("employees")
                .AsObservable<Employee>()
                .Subscribe(emp =>
                {
                    if (emp.Object != null)
                    {
                        emp.Object.FirebaseKey = emp.Key;
                        onEmployeeChanged(emp.Object);
                    }
                });
        }

        public IDisposable ListenToExercises(Action<Exercise> onExerciseChanged)
        {
            return client
                .Child("exercises")
                .AsObservable<Exercise>()
                .Subscribe(ex =>
                {
                    if (ex.Object != null)
                    {
                        ex.Object.FirebaseKey = ex.Key;
                        onExerciseChanged(ex.Object);
                    }
                });
        }

        public IDisposable ListenToResults(Action<Result> onResultChanged)
        {
            return client
                .Child("results")
                .AsObservable<Result>()
                .Subscribe(res =>
                {
                    if (res.Object != null)
                    {
                        res.Object.FirebaseKey = res.Key;
                        onResultChanged(res.Object);
                    }
                });
        }
    }

    // Model classes
    public class Employee
    {
        [JsonIgnore]
        public string FirebaseKey { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        [JsonIgnore]
        public int TotalPoints { get; set; }
        [JsonIgnore]
        public string Badges { get; set; }
    }

    public class Exercise
    {
        [JsonIgnore]
        public string FirebaseKey { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Unit { get; set; }
    }

    public class Result
    {
        [JsonIgnore]
        public string FirebaseKey { get; set; }
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = "Pending";
        public int Points { get; set; }
    }
}