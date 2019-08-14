using FinOps.Models;
using LNF;
using LNF.Billing;
using LNF.CommonTools;
using LNF.Models.Billing.Reports;
using LNF.Models.Data;
using LNF.Models.Scheduler;
using LNF.Repository;
using LNF.Repository.Billing;
using LNF.Repository.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace FinOps.Controllers
{
    public class ReportController : Controller
    {
        [Route("report/misc-charges")]
        public ActionResult MiscCharges(MiscChargesModel model)
        {
            if (Session["MiscCharges:Period"] == null)
                model.Period = DateTime.Now.FirstOfMonth().AddMonths(-1);
            else
                model.Period = (DateTime)Session["MiscCharges:Period"];

            model.ApplyPeriod = model.Period;
            model.Quantity = 1;

            model.Clients = FilterByPeriod(model.Period);
            model.MiscBillingCharges = DA.Current.Query<MiscBillingCharge>().Where(x => x.Period == model.Period).ToList();

            return View(model);
        }

        private IEnumerable<IClient> FilterByPeriod(DateTime period)
        {
            IQueryable<ActiveLogClient> alogs = DA.Current.Query<ActiveLogClient>().Where(x => x.EnableDate < period && (x.DisableDate == null || x.DisableDate.Value > period));
            return DA.Current.Query<ClientInfo>().Join(alogs, o => o.ClientID, i => i.ClientID, (outer, inner) => outer).CreateModels<IClient>();
        }

        [Route("report/misc-charges/run")]
        public ActionResult MiscChargesRun(MiscChargesModel model)
        {
            Session["MiscCharges:Period"] = model.Period;
            return RedirectToAction("MiscCharges");
        }

        [Route("report/misc-charges/add")]
        public ActionResult MiscChargesAdd(MiscChargesModel model)
        {
            Session["MiscCharges:Period"] = model.Period;
            return RedirectToAction("MiscCharges");
        }

        [HttpGet, Route("report/tool-billing")]
        public ActionResult ToolBilling(ToolBillingModel model)
        {
            ReservationDateRange range;
            IReservation rsv = null;
            int resourceId = 0;

            if (model.ReservationID > 0)
            {
                rsv = ServiceProvider.Current.Scheduler.Reservation.GetReservation(model.ReservationID.Value);
                resourceId = rsv.ResourceID;
            }

            if (!model.StartDate.HasValue || !model.EndDate.HasValue)
            {
                ModelState.Remove("StartDate");
                ModelState.Remove("EndDate");

                if (rsv != null)
                {
                    model.StartDate = rsv.ChargeBeginDateTime.FirstOfMonth();
                    model.EndDate = model.StartDate.Value.AddMonths(1);
                }
            }

            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (rsv == null)
                {
                    int.TryParse(model.Resource, out resourceId);
                }

                range = new ReservationDateRange(resourceId, model.StartDate.Value, model.EndDate.Value);
                ReservationDurations rd = range.CreateReservationDurations();
                model.LoadReservationDurations(rd);
            }

            return View("ToolBilling", model);
        }

        [HttpGet, Route("report/financial-manager")]
        public ActionResult FinancialManager(DateTime? period = null, string message = null, string option = null)
        {
            // Possible option values:
            //  <null> (default): send email to manager and cc address
            //  nomgr: do not send email to manager, only cc addresss (for debugging)

            Session["FinancialManagerMessage"] = message;
            Session["FinancialManagerOption"] = option;

            var p = period.GetValueOrDefault(DateTime.Now.FirstOfMonth().AddMonths(-1));

            ViewBag.Period = p;
            ViewBag.Message = message;
            ViewBag.Option = option;

            var emails = ServiceProvider.Current.Billing.Report.GetFinancialManagerReportEmails(new FinancialManagerReportOptions
            {
                Period = p,
                ClientID = 0,
                ManagerOrgID = 0,
                Message = message,
                IncludeManager = true
            });

            if (ViewBag.Option == "nomgr")
            {
                foreach (var e in emails)
                    e.ToAddress = null;
            }

            ViewBag.Emails = emails;

            return View();
        }

        [HttpGet, Route("report/financial-manager/send")]
        public ActionResult SendFinancialManagerEmail(DateTime period, int managerOrgId)
        {
            string message = Session["FinancialManagerMessage"] == null ? string.Empty : Convert.ToString(Session["FinancialManagerMessage"]);
            string option = Session["FinancialManagerOption"] == null ? string.Empty : Convert.ToString(Session["FinancialManagerOption"]);

            ServiceProvider.Current.Billing.Report.SendFinancialManagerReport(new FinancialManagerReportOptions
            {
                IncludeManager = option != "nomgr",
                Message = message,
                Period = period,
                ClientID = 0,
                ManagerOrgID = managerOrgId
            });

            return RedirectToAction("FinancialManager", new { period = period.ToString("yyyy-MM-dd"), message });
        }

        [HttpGet, Route("report/external-invoice")]
        public ActionResult ExternalInvoice(ExternalInvoiceModel model)
        {
            //var context = await LoadAsync(model);

            //model.Period = context.GetSessionItem("Period", DateTime.Now.FirstOfMonth().AddMonths(-1));
            //model.IncludeRemoteProcessing = context.GetSessionItem("IncludeRemoteProcessing", false);

            //await ExternalInvoiceLoadAsync(model);

            return View("ExternalInvoice", model);
        }

        [HttpPost, Route("report/external-invoice")]
        public ActionResult ExternalInvoicePost(ExternalInvoiceModel model)
        {
            //var context = await LoadAsync(model);

            //context.SetSessionItem("Period", model.Period);
            //context.SetSessionItem("IncludeRemoteProcessing", model.IncludeRemoteProcessing);

            //await ExternalInvoiceLoadAsync(model);

            return View("ExternalInvoice", model);
        }

        [Route("report/external-invoice/download/zip")]
        public ActionResult ExternalInvoiceDownloadZip(ExternalInvoiceModel model)
        {
            //var context = await LoadAsync(model);

            //model.Period = context.GetSessionItem("Period", DateTime.Now.FirstOfMonth().AddMonths(-1));
            //model.IncludeRemoteProcessing = context.GetSessionItem("IncludeRemoteProcessing", false);

            return RedirectToAction("ExternalInvoice");
        }

        private void ExternalInvoiceLoadAsync(ExternalInvoiceModel model)
        {
            //using (var bc = ApiProvider.NewBillingClient())
            //{
            //    model.Invoices = await bc.GetInvoices(model.Period);
            //}
        }

        [HttpGet, Route("report/subsidy")]
        public ActionResult Subsidy(DateTime? period = null, int clientId = 0)
        {
            var model = new SubsidyModel
            {
                Period = period.GetValueOrDefault(DateTime.Now.FirstOfMonth().AddMonths(-1)),
                ClientID = clientId
            };

            return View(model);
        }

        [HttpPost, Route("report/subsidy")]
        public ActionResult Subsidy(SubsidyModel model)
        {
            return RedirectToAction("Subsidy", new { period = model.Period, clientId = model.ClientID });
        }
    }
}