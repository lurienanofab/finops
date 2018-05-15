using FinOps.Models;
using LNF;
using LNF.Billing;
using LNF.CommonTools;
using LNF.Models.Data;
using LNF.Repository;
using LNF.Repository.Data;
using LNF.Repository.Scheduler;
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

        private IList<ClientItem> FilterByPeriod(DateTime period)
        {
            IQueryable<ActiveLogClient> alogs = DA.Current.Query<ActiveLogClient>().Where(x => x.EnableDate < period && (x.DisableDate == null || x.DisableDate.Value > period));
            return DA.Current.Query<ClientInfo>().Join(alogs, o => o.ClientID, i => i.ClientID, (outer, inner) => outer).Model<ClientItem>();
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

            if (!model.StartDate.HasValue || !model.EndDate.HasValue)
            {
                if (model.ReservationID > 0)
                {
                    ModelState.Remove("StartDate");
                    ModelState.Remove("EndDate");
                    Reservation rsv = DA.Current.Single<Reservation>(model.ReservationID);
                    model.StartDate = rsv.ChargeBeginDateTime().FirstOfMonth();
                    model.EndDate = model.StartDate.Value.AddMonths(1);
                    range = new ReservationDateRange(rsv.Resource.ResourceID, model.StartDate.Value, model.EndDate.Value);
                }
            }

            if (model.StartDate.HasValue && model.EndDate.HasValue)
            {
                int resourceId;
                if (int.TryParse(model.Resource, out resourceId))
                    range = new ReservationDateRange(resourceId, model.StartDate.Value, model.EndDate.Value);
                else
                    range = new ReservationDateRange(model.StartDate.Value, model.EndDate.Value);

                ReservationDurations rd = range.CreateReservationDurations();
                model.Load(rd);
            }

            return View("ToolBilling", model);
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
    }
}