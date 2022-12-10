using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySqlConnector;

namespace System.pages
{
    public partial class Helper : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                List<string> lstTables = null;

                using (MySqlConnection conn = new MySqlConnection(config.ConnString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        lstTables = m.GetTableList();

                        conn.Close();
                    }
                }

                StringBuilder sb = new StringBuilder();

                foreach (var t in lstTables)
                {
                    sb.AppendLine($@"<a id='table_{t}' href='#' onclick=""selectTable('{t}'); return false;"" class=''>{t}</a>");
                }

                phTables.Controls.Add(new LiteralControl(sb.ToString()));
            }
        }
    }
}