using LNF.Billing;
using System;
using System.Collections.Generic;
using System.Linq;
using LNF.Repository;
using LNF.Repository.Scheduler;
using LNF.CommonTools;

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

        public void Load(ReservationDurations rd)
        {
            Durations = rd.Where(x => ReservationFilter(x) && ActivityFilter(x) && ClientFilter(x) && ResourceFilter(x) && ProcessTechFilter(x) && AccountFilter(x));
        }

        public IEnumerable<ReservationDurationItem> GetTransferReservations(ReservationDurationItem item)
        {

            ReservationDateRange range = new ReservationDateRange(item.Reservation.ReservationID, item.Reservation.ChargeBeginDateTime, item.Reservation.ChargeEndDateTime);
            ReservationDurations durations = range.CreateReservationDurations();
            return durations.Where(x => x.Reservation != item.Reservation && x.UtilizedDuration.TotalSeconds > 0);
        }

        private ReservationDateRange GetRange()
        {
            ReservationDateRange result;

            int resourceId;

            if (int.TryParse(Resource, out resourceId))
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
            int activityId;
            if (!string.IsNullOrEmpty(Activity))
            {
                if (int.TryParse(Activity, out activityId))
                    return item.Reservation.ActivityID == activityId;
                else
                    return item.Reservation.ActivityName.ToLower().Contains(Activity.ToLower());
            }
            else
                return true;
        }

        private bool ClientFilter(ReservationDurationItem item)
        {
            int clientId;
            if (!string.IsNullOrEmpty(Client))
            {
                if (int.TryParse(Client, out clientId))
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
            int resourceId;
            if (!string.IsNullOrEmpty(Resource))
            {
                if (int.TryParse(Resource, out resourceId))
                    return item.Reservation.ResourceID == resourceId;
                else
                    return item.Reservation.ResourceName.ToLower().Contains(Resource.ToLower());
            }
            else
                return true;
        }

        private bool ProcessTechFilter(ReservationDurationItem item)
        {
            int procTechId;
            if (!string.IsNullOrEmpty(ProcessTech))
            {
                if (int.TryParse(ProcessTech, out procTechId))
                    return item.Reservation.ProcessTechID == procTechId;
                else
                    return item.Reservation.ProcessTechName.ToLower().Contains(ProcessTech.ToLower());
            }
            else
                return true;
        }

        private bool AccountFilter(ReservationDurationItem item)
        {
            int accountId;
            if (!string.IsNullOrEmpty(Account))
            {
                if (int.TryParse(Account, out accountId))
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
                result = Convert.ToDecimal(item.StandardDuration.TotalHours) * item.Reservation.Cost.HourlyRate();
            }

            return Math.Round(result, 3, MidpointRounding.AwayFromZero);
        }

        public decimal GetOverTimeCharge(ReservationDurationItem item)
        {
            return Math.Round(Convert.ToDecimal(item.OverTimeDuration.TotalHours) * item.Reservation.Cost.OverTimeRate(), 3, MidpointRounding.AwayFromZero);
        }

        public decimal GetBookingFeeCharge(ReservationDurationItem item)
        {
            decimal result = 0M;

            if (item.Reservation.IsCancelledBeforeCutoff)
                result = Convert.ToDecimal(item.StandardDuration.TotalHours) * item.Reservation.Cost.BookingFeeRate();

            return Math.Round(result, 3, MidpointRounding.AwayFromZero);
        }

        public decimal GetPerUseCharge(ReservationDurationItem item)
        {
            decimal result = 0M;

            if (!item.Reservation.IsCancelledBeforeCutoff)
                result = item.Reservation.Cost.PerUseRate();

            return Math.Round(result, 3, MidpointRounding.AwayFromZero);
        }

        public decimal GetPerUseBookingFeeCharge(ReservationDurationItem item)
        {
            decimal result = 0M;

            if (item.Reservation.IsCancelledBeforeCutoff)
                result = item.Reservation.Cost.PerUseBookingFeeRate();

            return Math.Round(result, 3, MidpointRounding.AwayFromZero);
        }

        public decimal GetTotalCharge(ReservationDurationItem item)
        {
            return GetStandardCharge(item) + GetBookingFeeCharge(item) + GetOverTimeCharge(item) + GetPerUseCharge(item) + GetPerUseBookingFeeCharge(item);
        }
    }
}