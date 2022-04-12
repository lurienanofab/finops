using LNF.Billing;
using LNF.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinOps.Models
{
    public class ToolBillingModel : ModelBase
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? ReservationID { get; set; }
        public string Activity { get; set; }
        public string Resource { get; set; }
        public string ProcessTech { get; set; }
        public string Client { get; set; }
        public string Account { get; set; }
        public IEnumerable<ReservationDurationItem> Durations { get; set; }

        public void LoadReservationDurations(ReservationDurations rd)
        {
            // OrderBy is
            Durations = rd.Where(x => ReservationFilter(x) && ActivityFilter(x) && ClientFilter(x) && ResourceFilter(x) && ProcessTechFilter(x) && AccountFilter(x))
                .OrderBy(x => x.Reservation.ActDate)
                .ThenBy(x => x.Reservation.ReservationID).ToList();
        }

        public IEnumerable<ReservationDurationItem> GetTransferReservations(ReservationDurationItem item)
        {
            ReservationDateRange range = new ReservationDateRange(item.Reservation.ResourceID, item.Reservation.ChargeBeginDateTime, item.Reservation.ChargeEndDateTime);
            ReservationDurations durations = new ReservationDurations(range);
            return durations.Where(x => x.Reservation.ReservationID != item.Reservation.ReservationID && x.UtilizedDuration.TotalSeconds > 0);
        }

        private ReservationDateRange GetRange()
        {
            ReservationDateRange result;

            if (int.TryParse(Resource, out int resourceId))
                result = new ReservationDateRange(resourceId, StartDate.Value, EndDate.Value);
            else
                result = new ReservationDateRange(StartDate.Value, EndDate.Value);

            return result;
        }

        private bool ReservationFilter(ReservationDurationItem item)
        {
            if (ReservationID.GetValueOrDefault(0) == 0)
                return true;
            else
                return item.Reservation.ReservationID == ReservationID;
        }

        private bool ActivityFilter(ReservationDurationItem item)
        {
            if (!string.IsNullOrEmpty(Activity))
            {
                if (int.TryParse(Activity, out int activityId))
                    return item.Reservation.ActivityID == activityId;
                else
                    return item.Reservation.ActivityName.ToLower().Contains(Activity.ToLower());
            }
            else
                return true;
        }

        private bool ClientFilter(ReservationDurationItem item)
        {
            if (!string.IsNullOrEmpty(Client))
            {
                if (int.TryParse(Client, out int clientId))
                    return item.Reservation.ClientID == clientId;
                else
                    return item.Reservation.DisplayName.ToLower().Contains(Client.ToLower())
                        || item.Reservation.UserName.ToLower().Contains(Client.ToLower());
            }
            else
                return true;
        }

        private bool ResourceFilter(ReservationDurationItem item)
        {
            if (!string.IsNullOrEmpty(Resource))
            {
                if (int.TryParse(Resource, out int resourceId))
                    return item.Reservation.ResourceID == resourceId;
                else
                    return item.Reservation.ResourceName.ToLower().Contains(Resource.ToLower());
            }
            else
                return true;
        }

        private bool ProcessTechFilter(ReservationDurationItem item)
        {
            if (!string.IsNullOrEmpty(ProcessTech))
            {
                if (int.TryParse(ProcessTech, out int procTechId))
                    return item.Reservation.ProcessTechID == procTechId;
                else
                    return item.Reservation.ProcessTechName.ToLower().Contains(ProcessTech.ToLower());
            }
            else
                return true;
        }

        private bool AccountFilter(ReservationDurationItem item)
        {
            if (!string.IsNullOrEmpty(Account))
            {
                if (int.TryParse(Account, out int accountId))
                    return item.Reservation.AccountID == accountId;
                else
                    return item.Reservation.AccountName.ToLower().Contains(Account.ToLower())
                        || item.Reservation.ShortCode.ToLower().Contains(Account.ToLower());
            }
            else
                return true;
        }

        public decimal GetStandardCharge(ReservationDurationItem item)
        {
            decimal result = 0M;

            if (!item.Reservation.IsCancelledBeforeCutoff)
            {
                var standardHours = GetRoundedHours(item.StandardDuration, 4);
                result = standardHours * item.Reservation.Cost.HourlyRate();
            }

            return GetRoundedMoney(result);
        }

        public decimal GetOverTimeCharge(ReservationDurationItem item)
        {
            var overtimeHours = GetRoundedHours(item.OverTimeDuration, 4);
            var result = overtimeHours * item.Reservation.Cost.OverTimeRate();
            return GetRoundedMoney(result);
        }

        public decimal GetBookingFeeCharge(ReservationDurationItem item)
        {
            decimal result = 0M;

            if (item.Reservation.IsCancelledBeforeCutoff)
            {
                var standardHours = GetRoundedHours(item.StandardDuration, 4);
                result = standardHours * item.Reservation.Cost.BookingFeeRate();
            }

            return GetRoundedMoney(result);
        }

        public decimal GetPerUseCharge(ReservationDurationItem item)
        {
            decimal result = 0M;

            if (!item.Reservation.IsCancelledBeforeCutoff)
                result = item.Reservation.Cost.PerUseRate();

            return GetRoundedMoney(result);
        }

        public decimal GetSubtotal(ReservationDurationItem item)
        {
            return GetStandardCharge(item) + GetBookingFeeCharge(item) + GetOverTimeCharge(item) + GetPerUseCharge(item);
        }

        public decimal GetTotalCharge(ReservationDurationItem item)
        {
            return GetSubtotal(item) * Convert.ToDecimal(item.Reservation.ChargeMultiplier);     
        }

        private decimal GetRoundedHours(TimeSpan ts, int decimals)
        {
            var minutes = ts.TotalMinutes;
            var hours = minutes / 60D;
            var rounded = Math.Round(hours, decimals, MidpointRounding.AwayFromZero);
            return Convert.ToDecimal(rounded);
        }

        private decimal GetRoundedMoney(decimal val)
        {
            return Math.Round(val, 2, MidpointRounding.AwayFromZero);
        }
    }
}