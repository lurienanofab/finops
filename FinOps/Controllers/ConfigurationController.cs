using FinOps.Models;
using LNF;
using LNF.Billing;
using LNF.CommonTools;
using LNF.Data;
using LNF.Impl.Repository.Data;
using LNF.Repository;
using LNF.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace FinOps.Controllers
{
    public class ConfigurationController : FinOpsController
    {
        public ConfigurationController(IProvider provider) : base(provider) { }

        [Route("configuration/remote-processing")]
        public ActionResult RemoteProcessing(RemoteProcessingModel model)
        {
            model.Period = DateTime.Now.FirstOfMonth().AddMonths(-1);

            if (Session["ErrorMessage"] != null)
            {
                model.Message = new AlertMessage() { AlertType = AlertType.Danger, Dismissible = true, Text = Session["ErrorMessage"].ToString() };
                Session["ErrorMessage"] = null;
            }

            return View(model);
        }

        [HttpPost, Route("configuration/remote-processing/save")]
        public ActionResult RemoteProcessingSave(RemoteProcessingModel model)
        {
            SaveRemoteProcessingAsync(model);
            return RedirectToAction("RemoteProcessing");
        }

        private void SaveRemoteProcessingAsync(RemoteProcessingModel model)
        {
            if (model.ClientID == 0)
            {
                Session["ErrorMessage"] = "Please select a staff member.";
                return;
            }

            if (model.RemoteClientID == 0)
            {
                Session["ErrorMessage"] = "Please select a remote user.";
                return;
            }

            if (model.RemoteAccountID == 0)
            {
                Session["ErrorMessage"] = "Please select a remote account.";
                return;
            }

            ServiceProvider.Current.Data.Client.InsertClientRemote(new ClientRemoteItem
            {
                ClientID = model.ClientID,
                RemoteClientID = model.RemoteClientID,
                AccountID = model.RemoteAccountID
            }, model.Period);
        }


        [Route("configuration/account-subsidy")]
        public ActionResult AccountSubsidy(AccountSubsidyModel model, string command = null)
        {
            //var context = await LoadAsync(model);

            //if (model.EnableDate != default(DateTime))
            //    context.SetSessionItem("EnableDate", model.EnableDate);
            //else
            //    model.EnableDate = context.GetSessionItem("EnableDate", DateTime.Now.Date);

            //using (var dc = ApiProvider.NewDataClient())
            //{
            //    var activeAccounts = await dc.GetActiveAccounts(model.EnableDate, model.EnableDate.AddDays(1));

            //    //ChargeTypeID used for internal accounts (the type of acccount to which subsidy is applied)
            //    int internalChargeTypeId = 5;

            //    model.Accounts = activeAccounts.Where(x => x.ChargeTypeID == internalChargeTypeId);
            //}

            //using (var bc = ApiProvider.NewBillingClient())
            //{
            //    if (command == "add-account-subsidy")
            //    {
            //        if (model.UserPaymentPercentage <= 0)
            //        {
            //            model.Message = new AlertMessage() { AlertType = AlertType.Danger, Dismissible = true, Text = "Percentage must be greater than zero." };
            //        }

            //        var acctSubsidy = await bc.AddAccountSubsidy(new AccountSubsidyModel() { AccountID = model.AccountID, EnableDate = model.EnableDate, UserPaymentPercentage = model.UserPaymentPercentage / 100 });

            //        context.SetSessionItem("EnableDate", acctSubsidy.EnableDate);

            //        return RedirectToAction("AccountSubsidy");
            //    }

            //    var accountSubsidies = await bc.GetAccountSubsidies(model.EnableDate, model.EnableDate.AddDays(1));

            //    model.AccountSubsidies = accountSubsidies;
            //}

            return View(model);
        }

        [Route("configuration/account-subsidy/disable/{AccountSubsidyID}")]
        public ActionResult DisableAccountSubsidy(AccountSubsidyModel model)
        {
            var disableResult = ServiceProvider.Current.Billing.AccountSubsidy.DisableAccountSubsidy(model.AccountSubsidyID);
            IAccountSubsidy acctSubsidy = ServiceProvider.Current.Billing.AccountSubsidy.GetSingleAccountSubsidy(model.AccountSubsidyID);

            Session["EnableDate"] = acctSubsidy.EnableDate;
            return RedirectToAction("AccountSubsidy");
        }

        private DateTime GetHolidayStartDate()
        {
            return DateTime.Now.Date.AddDays(-3);
        }

        private DateTime GetHolidayEndDate()
        {
            return DateTime.Now.Date.AddYears(2);
        }

        [Route("configuration/holidays/{SelectedGoogleCalendarID?}")]
        public ActionResult Holidays(HolidaysModel model)
        {
            model.Provider = Provider;
            model.CurrentUser = CurrentUser;
            model.CurrentUserClientAccounts = HttpContext.GetCurrentUserClientAccounts();
            model.StartDate = GetHolidayStartDate();
            model.EndDate = GetHolidayEndDate();
            model.Holidays = DataSession.Query<Holiday>().Where(x => x.HolidayDate >= model.StartDate).OrderBy(x => x.HolidayDate).ToList();
            model.CalendarFeeds = DataSession.Query<GoogleCalendarFeed>().Where(x => x.Active).OrderByDescending(x => x.LastUsed).ToList();

            if (model.CalendarFeeds.Count() == 0)
            {
                var defaultFeed = new GoogleCalendarFeed()
                {
                    GoogleCalendarID = "en.usa#holiday@group.v.calendar.google.com",
                    Client = DataSession.Single<Client>(CurrentUser.ClientID),
                    Created = DateTime.Now,
                    LastUsed = DateTime.Now,
                    Active = true
                };

                DataSession.SaveOrUpdate(defaultFeed);

                model.CalendarFeeds = new List<GoogleCalendarFeed>() { defaultFeed };
            }

            if (string.IsNullOrEmpty(model.SelectedGoogleCalendarID))
                model.SelectedGoogleCalendarID = model.CalendarFeeds.First().GoogleCalendarID; //should be most recenlty used because of sorting

            return View(model);
        }

        [HttpPost, Route("configuration/holidays/add")]
        public ActionResult AddHoliday(Holiday model)
        {
            if (!string.IsNullOrEmpty(model.Description) && model.HolidayDate != default(DateTime))
            {
                DataSession.SaveOrUpdate(model);
            }

            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/delete/{holidayId}")]
        public ActionResult DeleteHoliday(int holidayId)
        {
            var holiday = DataSession.Single<Holiday>(holidayId);

            if (holiday != null)
                DataSession.Delete(holiday);

            return RedirectToAction("Holidays");
        }

        [HttpPost, Route("configuration/holidays/feed/add")]
        public ActionResult AddCalendarFeed(GoogleCalendarFeed model)
        {
            if (!string.IsNullOrEmpty(model.GoogleCalendarID))
            {
                model.Client = DataSession.Single<Client>(CurrentUser.ClientID);
                model.Created = DateTime.Now;
                model.LastUsed = DateTime.Now;
                model.Active = true;
                DataSession.SaveOrUpdate(model);
            }

            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/feed/{googleCalendarId}/delete")]
        public ActionResult DeleteCalendarFeed(string googleCalendarId)
        {
            var feed = DataSession.Query<GoogleCalendarFeed>().First(x => x.GoogleCalendarID == googleCalendarId);
            DataSession.Delete(feed);
            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/feed/{googleCalendarId}/add/all")]
        public ActionResult AddCalendarFeedAllEvents(string googleCalendarId)
        {
            var feed = DataSession.Query<GoogleCalendarFeed>().First(x => x.GoogleCalendarID == googleCalendarId);
            var cal = GoogleCalendar.Create(feed);

            IEnumerable<IHoliday> holidays = DataSession.Query<Holiday>().ToList();

            foreach (var e in cal.GetEvents(GetHolidayStartDate(), GetHolidayEndDate()))
                AddHoliday(e, holidays);

            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/feed/{googleCalendarId}/add/{uid}/{index}")]
        public ActionResult AddCalendarFeedEvent(string googleCalendarId, string uid, int index)
        {
            var feed = DataSession.Query<GoogleCalendarFeed>().First(x => x.GoogleCalendarID == googleCalendarId);
            var cal = GoogleCalendar.Create(feed);

            var e = cal.GetEvents(GetHolidayStartDate(), GetHolidayEndDate()).First(x => x.Uid == uid && x.OccurrenceIndex == index);

            IEnumerable<IHoliday> holidays = DataSession.Query<Holiday>().ToList();

            AddHoliday(e, holidays);

            return RedirectToAction("Holidays");
        }

        private void AddHoliday(GoogleCalendar.CalendarEvent e, IEnumerable<IHoliday> holidays)
        {
            var start = e.Start;
            var summary = e.Summary;

            // check for existing

            var existing = holidays.FirstOrDefault(x => x.Description == summary && x.HolidayDate == start);

            if (existing == null)
            {
                DataSession.SaveOrUpdate(new Holiday()
                {
                    Description = summary,
                    HolidayDate = start
                });
            }
        }
    }
}