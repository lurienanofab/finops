using FinOps.Models;
using LNF;
using LNF.Cache;
using LNF.CommonTools;
using LNF.Data;
using LNF.Models.Data;
using LNF.Repository;
using LNF.Repository.Data;
using OnlineServices.Api.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace FinOps.Controllers
{
    public class ConfigurationController : Controller
    {
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

            var dc = new DataClient();
            dc.InsertClientRemote(new ClientRemoteItem
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
            var acctSubsidy = ServiceProvider.Current.Billing.AccountSubsidy.DisableAccountSubsidy(model.AccountSubsidyID);
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
            model.CurrentUser = CacheManager.Current.CurrentUser;
            model.CurrentUserClientAccounts = CacheManager.Current.GetCurrentUserClientAccounts().ToList();
            model.StartDate = GetHolidayStartDate();
            model.EndDate = GetHolidayEndDate();
            model.Holidays = DA.Current.Query<Holiday>().Where(x => x.HolidayDate >= model.StartDate).OrderBy(x => x.HolidayDate).ToList();
            model.CalendarFeeds = DA.Current.Query<GoogleCalendarFeed>().Where(x => x.Active).OrderByDescending(x => x.LastUsed).ToList();

            if (model.CalendarFeeds.Count() == 0)
            {
                var defaultFeed = new GoogleCalendarFeed()
                {
                    GoogleCalendarID = "en.usa#holiday@group.v.calendar.google.com",
                    Client = DA.Current.Single<Client>(CacheManager.Current.CurrentUser.ClientID),
                    Created = DateTime.Now,
                    LastUsed = DateTime.Now,
                    Active = true
                };

                DA.Current.SaveOrUpdate(defaultFeed);

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
                DA.Current.SaveOrUpdate(model);
            }

            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/delete/{holidayId}")]
        public ActionResult DeleteHoliday(int holidayId)
        {
            var holiday = DA.Current.Single<Holiday>(holidayId);

            if (holiday != null)
                DA.Current.Delete(holiday);

            return RedirectToAction("Holidays");
        }

        [HttpPost, Route("configuration/holidays/feed/add")]
        public ActionResult AddCalendarFeed(GoogleCalendarFeed model)
        {
            if (!string.IsNullOrEmpty(model.GoogleCalendarID))
            {
                model.Client = DA.Current.Single<Client>(CacheManager.Current.CurrentUser.ClientID);
                model.Created = DateTime.Now;
                model.LastUsed = DateTime.Now;
                model.Active = true;
                DA.Current.SaveOrUpdate(model);
            }

            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/feed/{googleCalendarId}/delete")]
        public ActionResult DeleteCalendarFeed(string googleCalendarId)
        {
            var feed = DA.Current.Query<GoogleCalendarFeed>().First(x => x.GoogleCalendarID == googleCalendarId);
            DA.Current.Delete(feed);
            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/feed/{googleCalendarId}/add/all")]
        public ActionResult AddCalendarFeedAllEvents(string googleCalendarId)
        {
            var feed = DA.Current.Query<GoogleCalendarFeed>().First(x => x.GoogleCalendarID == googleCalendarId);
            var cal = GoogleCalendar.Create(feed);

            foreach (var e in cal.GetEvents(GetHolidayStartDate(), GetHolidayEndDate()))
                cal.AddHoliday(e);

            return RedirectToAction("Holidays");
        }

        [Route("configuration/holidays/feed/{googleCalendarId}/add/{uid}/{index}")]
        public ActionResult AddCalendarFeedEvent(string googleCalendarId, string uid, int index)
        {
            var feed = DA.Current.Query<GoogleCalendarFeed>().First(x => x.GoogleCalendarID == googleCalendarId);
            var cal = GoogleCalendar.Create(feed);

            var e = cal.GetEvents(GetHolidayStartDate(), GetHolidayEndDate()).First(x => x.Uid == uid && x.OccurrenceIndex == index);

            cal.AddHoliday(e);

            return RedirectToAction("Holidays");
        }
    }
}