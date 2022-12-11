using MySqlExpress_TestWebForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySqlConnector;
using System.Text;

namespace System.pages
{
    public partial class PlayerTeam : System.Web.UI.Page
    {
        int year { get { return Convert.ToInt32(ViewState["year"]); } set { ViewState["year"] = value; } }
        int teamid { get { return Convert.ToInt32(ViewState["teamid"]); } set { ViewState["teamid"] = value; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                year = Convert.ToInt32(Request.QueryString["year"]);
                teamid = Convert.ToInt32(Request.QueryString["teamid"]);

                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["@teamid"] = teamid;

                obTeam team = null;

                using (MySqlConnection conn = new MySqlConnection(config.ConnString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        team = m.GetObject<obTeam>($"select * from team where id=@teamid limit 1;", dicParam);

                        conn.Close();
                    }
                }

                if (team.id == 0)
                {
                    Response.Redirect("~/TeamList", true);
                }

                string logo = team.ImgLogo;

                if (logo.Length > 0)
                    logo += "<br />";

                lbTeamName.Text = $"{logo}{year} - Team: {team.name} ({team.code})";

                string script = $@"
<script type='text/javascript'>
    var year = {year};
    var teamid = {teamid};
</script>
";

                phHead.Controls.Add(new LiteralControl(script));
            }
        }
    }
}