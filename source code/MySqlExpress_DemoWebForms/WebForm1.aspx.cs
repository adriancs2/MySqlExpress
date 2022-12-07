using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Data;
using MySqlConnector;

namespace System
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Dictionary<string, int> dic = new Dictionary<string, int>();

            DateTime dateRegister = DateTime.Now.AddMonths(-2);

            Random rd = new Random();

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    List<obPlayer> lst = m.GetObjectList<obPlayer>($"select * from player;");

                    m.StartTransaction();

                    foreach (obPlayer p in lst)
                    {
                        string n = p.Name.Substring(0, 1);
                        n = n.ToUpper();

                        if (!dic.ContainsKey(n))
                        {
                            dic[n] = 0;
                        }

                        dic[n] = dic[n] + 1;

                        string code = n + dic[n].ToString().PadLeft(4, '0');

                        int addday = rd.Next(0, 2);

                        dateRegister = dateRegister.AddDays(addday);

                        m.Execute($"update player set code='{code}',date_register='{dateRegister.ToString("yyyy-MM-dd")} 00:00:00' where id={p.Id} limit 1;");
                    }

                    m.Commit();

                    conn.Close();
                }
            }
        }
    }
}