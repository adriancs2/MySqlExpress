using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySqlConnector;

namespace System.pages
{
    public partial class apiGenerateCustomSqlFields : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string sql = Server.UrlDecode(Request.QueryString["sql"] + "");
            int fieldtype = Convert.ToInt32(Request.QueryString["fieldtype"]);

            MySqlExpress.FieldsOutputType EnumFieldtype = (MySqlExpress.FieldsOutputType)fieldtype;

            string output = "";

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    output = m.GenerateCustomClassField(sql, EnumFieldtype);

                    conn.Close();
                }
            }

            Response.Write(output);
        }
    }
}