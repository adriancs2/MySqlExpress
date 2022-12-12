using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services.Description;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace System
{
    public partial class master1 : System.Web.UI.MasterPage
    {
        protected override void OnInit(EventArgs e)
        {
            if (config.ConnString == "")
            {
                string url = Request.Url.ToString();
                if (url.EndsWith("/default.aspx") || url.EndsWith("ConnectionStringNotInitialized"))
                {
                    
                }
                else
                {
                    Response.Redirect("~/ConnectionStringNotInitialized", true);
                }
            }

            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["good_message"] != null)
            {
                WriteGoodMessage(Session["good_message"] + "");
                Session.Remove("good_message");
            }

            if (Session["bad_message"] != null)
            {
                WriteBadMessage(Session["bad_message"] + "");
                Session.Remove("bad_message");
            }

            version.Controls.Add(new LiteralControl(MySqlExpress.Version));
        }

        public void WriteBadMessage(string message)
        {
            string msg = $@"<div class=""alert alert-danger"" role=""alert"">{message}</div>";
            phMsg.Controls.Add(new LiteralControl(msg));
        }

        public void WriteGoodMessage(string message)
        {
            string msg = $@"<div class=""alert alert-success"" role=""alert"">{message}</div>";
            phMsg.Controls.Add(new LiteralControl(msg));
        }

        public void WriteSessionGoodMessage(string msg)
        {
            Session["good_message"] = msg;
        }

        public void WriteSessionBadMessage(string msg)
        {
            Session["bad_message"] = msg;
        }
    }
}