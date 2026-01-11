using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Script.Serialization;
using CABMED.ViewModels;

namespace CABMED.Services
{
    /// <summary>
    /// Simple repository that keeps track of appointment requests so that
    /// patient and secretary dashboards stay in sync.
    /// </summary>
    public static class AppointmentRequestRepository
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<AppointmentRequestViewModel> Requests = new List<AppointmentRequestViewModel>();
        private static int _sequence = 1;
        private static bool _initialized;

        public static AppointmentRequestViewModel Add(AppointmentRequestViewModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            lock (SyncRoot)
            {
                EnsureLoaded();

                if (request.RequestId == 0)
                {
                    request.RequestId = _sequence++;
                }

                Requests.Add(request);
                SaveChanges();
                return request;
            }
        }

        public static List<AppointmentRequestViewModel> GetAll()
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                return new List<AppointmentRequestViewModel>(Requests);
            }
        }

        public static List<AppointmentRequestViewModel> GetByPatient(int patientId)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                return Requests
                    .Where(r => r.PatientId == patientId)
                    .OrderByDescending(r => r.DateDemande)
                    .ToList();
            }
        }

        public static AppointmentRequestViewModel GetById(int requestId)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                return Requests.FirstOrDefault(r => r.RequestId == requestId);
            }
        }

        public static int CountByStatus(string status)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                return Requests.Count(r => string.Equals(r.Status, status, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public static int Count(Func<AppointmentRequestViewModel, bool> predicate)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                return Requests.Count(predicate);
            }
        }

        public static List<AppointmentRequestViewModel> GetRecent(int take)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                return Requests
                    .OrderByDescending(r => r.DateDemande)
                    .Take(take)
                    .ToList();
            }
        }

        public static bool ApproveRequest(int requestId, DateTime appointmentDate, TimeSpan appointmentTime, int assignedDoctorId, string assignedDoctor, string comments, string processedBy, int? rendezVousId = null)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                var request = Requests.FirstOrDefault(r => r.RequestId == requestId);
                if (request == null)
                {
                    return false;
                }

                request.Status = "Approuvé";
                request.AssignedDoctorId = assignedDoctorId;
                request.AssignedDoctor = assignedDoctor;
                request.SecretaryComments = comments;
                request.ConfirmedDate = appointmentDate.Date;
                request.ConfirmedStartTime = appointmentTime;
                request.ConfirmedEndTime = appointmentTime.Add(TimeSpan.FromMinutes(30));
                request.ProcessedDate = DateTime.Now;
                request.ProcessedBy = processedBy;
                request.RendezVousId = rendezVousId ?? request.RendezVousId;
                SaveChanges();
                return true;
            }
        }

        public static bool DeclineRequest(int requestId, string reason, string processedBy)
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                var request = Requests.FirstOrDefault(r => r.RequestId == requestId);
                if (request == null)
                {
                    return false;
                }

                request.Status = "Refusé";
                request.SecretaryComments = reason;
                request.AssignedDoctor = null;
                request.AssignedDoctorId = null;
                request.ConfirmedDate = null;
                request.ConfirmedStartTime = null;
                request.ConfirmedEndTime = null;
                request.ProcessedDate = DateTime.Now;
                request.ProcessedBy = processedBy;
                SaveChanges();
                return true;
            }
        }

        private static void EnsureLoaded()
        {
            if (_initialized)
            {
                return;
            }

            var serializer = new JavaScriptSerializer();
            var path = GetStoragePath();

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var items = serializer.Deserialize<List<AppointmentRequestViewModel>>(json);
                    if (items != null)
                    {
                        Requests.Clear();
                        Requests.AddRange(items);
                        _sequence = Requests.Count == 0 ? 1 : Requests.Max(r => r.RequestId) + 1;
                    }
                }
            }

            _initialized = true;
        }

        private static void SaveChanges()
        {
            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(Requests);
            var path = GetStoragePath();
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, json);
        }

        private static string GetStoragePath()
        {
            var path = HostingEnvironment.MapPath("~/App_Data/appointmentRequests.json");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Impossible de déterminer le chemin de stockage pour les demandes de rendez-vous.");
            }

            return path;
        }
    }
}
