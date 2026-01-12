using System.Collections.Generic;

namespace CABMED.Models
{
    /// <summary>
    /// Centralized status constants for the application
    /// </summary>
    public static class AppointmentStatus
    {
        // RendezVous Statut values (database)
        public const string Confirmed = "Confirmé";
        public const string Pending = "En attente";
        public const string Cancelled = "Annulé";
        public const string Completed = "Terminé";
        public const string Refused = "Refusé";

        // AppointmentRequest Status values (JSON repository)
        public const string Approved = "Approuvé";
        public const string RequestPending = "En attente";  // Same as database
        public const string Declined = "Refusé";            // Same as database

        /// <summary>
        /// Get all valid database statuses
        /// </summary>
        public static IEnumerable<string> GetDatabaseStatuses()
        {
            return new[] { Confirmed, Pending, Cancelled, Completed, Refused };
        }

        /// <summary>
        /// Get all valid request statuses
        /// </summary>
        public static IEnumerable<string> GetRequestStatuses()
        {
            return new[] { Approved, RequestPending, Declined };
        }

        /// <summary>
        /// Map request status to database status
        /// </summary>
        public static string MapRequestToDatabase(string requestStatus)
        {
            switch (requestStatus)
            {
                case Approved:
                    return Confirmed;
                case RequestPending:
                    return Pending;
                case Declined:
                    return Refused;
                default:
                    return Pending;
            }
        }
    }
}
