using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CABMED.ViewModels;
using Newtonsoft.Json;

namespace CABMED.Services
{
    public static class AppointmentRequestRepository
    {
        private static readonly object _lock = new object();
        private static readonly string _filePath = GetStoragePath();
        private static int _nextId = 1;

        private static string GetStoragePath()
        {
            var appDataPath = HttpContext.Current?.Server.MapPath("~/App_Data");
            if (string.IsNullOrEmpty(appDataPath))
            {
                appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            }

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            return Path.Combine(appDataPath, "appointmentRequests.json");
        }

        public static List<AppointmentRequestViewModel> GetAll()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        return new List<AppointmentRequestViewModel>();
                    }

                    var json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<AppointmentRequestViewModel>();
                    }

                    var requests = JsonConvert.DeserializeObject<List<AppointmentRequestViewModel>>(json);
                    return requests ?? new List<AppointmentRequestViewModel>();
                }
                catch (Exception)
                {
                    return new List<AppointmentRequestViewModel>();
                }
            }
        }

        public static AppointmentRequestViewModel GetById(int id)
        {
            lock (_lock)
            {
                var requests = GetAll();
                return requests.FirstOrDefault(r => r.RequestId == id);
            }
        }

        public static List<AppointmentRequestViewModel> GetByPatient(int patientId)
        {
            lock (_lock)
            {
                var requests = GetAll();
                return requests.Where(r => r.PatientId == patientId).ToList();
            }
        }

        public static void Add(AppointmentRequestViewModel request)
        {
            lock (_lock)
            {
                var requests = GetAll();
                
                if (request.RequestId == 0)
                {
                    request.RequestId = GenerateNextId(requests);
                }

                requests.Add(request);
                SaveAll(requests);
            }
        }

        public static void ApproveRequest(int requestId, DateTime appointmentDate, TimeSpan appointmentTime, 
            int doctorId, string doctorName, string comments, string processedBy, int rendezVousId)
        {
            lock (_lock)
            {
                var requests = GetAll();
                var request = requests.FirstOrDefault(r => r.RequestId == requestId);

                if (request != null)
                {
                    request.Status = "Confirmé";
                    request.ConfirmedDate = appointmentDate;
                    request.ConfirmedStartTime = appointmentTime;
                    request.ConfirmedEndTime = appointmentTime.Add(TimeSpan.FromMinutes(30));
                    request.AssignedDoctorId = doctorId;
                    request.AssignedDoctor = doctorName;
                    request.SecretaryComments = comments;
                    request.ProcessedDate = DateTime.Now;
                    request.ProcessedBy = processedBy;
                    request.RendezVousId = rendezVousId;

                    SaveAll(requests);
                }
            }
        }

        public static bool DeclineRequest(int requestId, string reason, string processedBy)
        {
            lock (_lock)
            {
                var requests = GetAll();
                var request = requests.FirstOrDefault(r => r.RequestId == requestId);

                if (request != null)
                {
                    request.Status = "Refusé";
                    request.SecretaryComments = reason;
                    request.ProcessedDate = DateTime.Now;
                    request.ProcessedBy = processedBy;

                    SaveAll(requests);
                    return true;
                }

                return false;
            }
        }

        public static int CountByStatus(string status)
        {
            lock (_lock)
            {
                var requests = GetAll();
                return requests.Count(r => string.Equals(r.Status, status, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public static int Count(Func<AppointmentRequestViewModel, bool> predicate)
        {
            lock (_lock)
            {
                var requests = GetAll();
                return requests.Count(predicate);
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
                _nextId = 1;
            }
        }

        private static void SaveAll(List<AppointmentRequestViewModel> requests)
        {
            try
            {
                var json = JsonConvert.SerializeObject(requests, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception)
            {
                // Log error if logging is available
            }
        }

        private static int GenerateNextId(List<AppointmentRequestViewModel> requests)
        {
            if (requests.Any())
            {
                _nextId = requests.Max(r => r.RequestId) + 1;
            }
            return _nextId++;
        }
    }
}
