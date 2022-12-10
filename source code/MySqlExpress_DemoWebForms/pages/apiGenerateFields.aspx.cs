using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySqlConnector;

namespace System.pages
{
    public partial class apiGenerateFields : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string tablename = Request.QueryString["tablename"] + "";
            int outputtype = Convert.ToInt32(Request.QueryString["outputtype"]);
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

                    switch (outputtype)
                    {
                        case 0:
                            output = m.GenerateTableClassFields(tablename, EnumFieldtype);
                            break;
                        case 1:
                            output = m.GenerateTableDictionaryEntries(tablename);
                            break;
                        case 2:
                            output = m.GetCreateTableSql(tablename);
                            break;
                        case 3:
                            output = m.GenerateUpdateColumnList(tablename);
                            break;
                    }

                    conn.Close();
                }
            }

            Response.Write(output);
        }
    }
}