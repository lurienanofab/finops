﻿using Ical.Net;
using LNF.Repository;
using LNF.Repository.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace FinOps.Models
{
    public class GoogleCalendar
    {
        private const string URL_FORMAT = "https://calendar.google.com/calendar/ical/{0}/public/basic.ics";

        public GoogleCalendarFeed Feed { get; private set; }
        public Calendar Source { get; private set; }

        private GoogleCalendar(GoogleCalendarFeed feed)
        {
            Feed = feed;
        }

        public void Refresh()
        {
            using (var wc = new WebClient())
            {
                Uri uri = new Uri(string.Format(URL_FORMAT, WebUtility.UrlEncode(Feed.GoogleCalendarID)));
                var content = wc.DownloadString(uri);
                var collection = Calendar.LoadFromStream(new StringReader(content)) as CalendarCollection;
                Source = collection.First() as Calendar;
            }
        }

        public static GoogleCalendar Create(GoogleCalendarFeed feed)
        {
            var result = new GoogleCalendar(feed);
            result.Refresh();
            return result;
        }

        public void AddHoliday(CalendarEvent e)
        {
            var start = e.Start;
            var summary = e.Summary;

            // check for existing

            var existing = DA.Current.Query<Holiday>().FirstOrDefault(x => x.Description == summary && x.HolidayDate == start);

            if (existing == null)
            {
                DA.Current.SaveOrUpdate(new Holiday()
                {
                    Description = summary,
                    HolidayDate = start
                });
            }
        }

        public IEnumerable<CalendarEvent> GetEvents(DateTime sd, DateTime ed)
        {
            List<CalendarEvent> result = new List<CalendarEvent>();

            var occurrences = Source.GetOccurrences(sd, ed);

            if (occurrences.Count > 0)
            {
                int index = 0;
                foreach (var o in occurrences)
                {
                    var e = o.Source as Event;
                    result.Add(new CalendarEvent() { Uid = e.Uid, Summary = e.Summary, Start = o.Period.StartTime.AsSystemLocal, OccurrenceIndex = index });
                    index++;
                }
            }
            else
            {
                foreach (var e in Source.Events.Where(x => x.Start.AsSystemLocal >= sd && x.Start.AsSystemLocal <= ed))
                {
                    result.Add(new CalendarEvent() { Uid = e.Uid, Summary = e.Summary, OccurrenceIndex = 0 });
                }
            }

            return result;
        }

        public class CalendarEvent
        {
            public string Uid { get; set; }
            public string Summary { get; set; }
            public DateTime Start { get; set; }
            public int OccurrenceIndex { get; set; }
        }
    }
}