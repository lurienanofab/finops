using LNF;
using LNF.Impl.Repository.Data;
using LNF.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinOps.Models
{
    public class HolidaysModel : ModelBase
    {
        public IProvider Provider { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IEnumerable<Holiday> Holidays { get; set; }
        public IEnumerable<GoogleCalendarFeed> CalendarFeeds { get; set; }
        public string SelectedGoogleCalendarID { get; set; }

        public GoogleCalendar GetCalendar()
        {
            var selectedFeed = Provider.DataAccess.Session.Query<GoogleCalendarFeed>().First(x => x.GoogleCalendarID == SelectedGoogleCalendarID); // yes, thow exceptions when null
            var result = GoogleCalendar.Create(selectedFeed);
            selectedFeed.LastUsed = DateTime.Now;
            return result;
        }

        public bool IsSelected(GoogleCalendarFeed feed)
        {
            return feed.GoogleCalendarID == SelectedGoogleCalendarID;
        }

        public bool HolidayExists(string description, DateTime date)
        {
            return Holidays.Any(x => x.Description == description && x.HolidayDate == date);
        }
    }
}