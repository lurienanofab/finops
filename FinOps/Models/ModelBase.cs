using LNF.Data;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace FinOps.Models
{
    public class ModelBase
    {
        public IClient CurrentUser { get; set; }
        public IEnumerable<IClientAccount> CurrentUserClientAccounts { get; set; }
        public AlertMessage Message { get; set; }
    }

    public class AlertMessage
    {
        public AlertType AlertType { get; set; }
        public bool Dismissible { get; set; }
        public string Text { get; set; }
    }

    public enum AlertType
    {
        Success = 1,
        Danger = 2,
        Info = 3
    }

    public static class FinOpsHtmlExtensions
    {
        public static IHtmlString GetMesage<T>(this HtmlHelper<T> helper) where T : ModelBase
        {
            var model = helper.ViewData.Model;

            if (model.Message != null)
            {
                TagBuilder alert = new TagBuilder("div");
                alert.AddCssClass("alert");
                alert.AddCssClass("alert-" + Enum.GetName(typeof(AlertType), model.Message.AlertType).ToLower());
                alert.MergeAttribute("role", "alert", true);

                if (model.Message.Dismissible)
                {
                    TagBuilder button = new TagBuilder("button");
                    button.AddCssClass("close");
                    button.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(new { @type = "button", data_dismiss = "alert", aria_label = "Close" }));

                    TagBuilder span = new TagBuilder("span");
                    span.MergeAttribute("aria-hidden", "true");
                    span.InnerHtml = "&times;";

                    button.InnerHtml = span.ToString();

                    alert.InnerHtml += button.ToString();
                }

                alert.InnerHtml += model.Message.Text;

                return new HtmlString(alert.ToString());
            }

            return new HtmlString(string.Empty);
        }
    }
}