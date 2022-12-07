using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace System.pages
{
    public partial class Error : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();

            if (ex != null)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }

                phErrTitle.Controls.Add(new LiteralControl(ex.Message));
                string msg = Server.HtmlEncode(ex.ToString()).Replace("\r\n", "<br />");
                ph1.Controls.Add(new LiteralControl(msg));
            }
            else
            {
                Response.Redirect("~/", true);
            }
        }
    }
}