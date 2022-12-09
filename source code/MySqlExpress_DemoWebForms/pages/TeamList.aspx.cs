using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace System.pages
{
    public partial class TeamList : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtYear.Text = DateTime.Now.Year.ToString();
            }
        }

        protected void btSearch_Click(object sender, EventArgs e)
        {
            Dictionary<string, object> dicParam = new Dictionary<string, object>();

            MySqlExpress m = new MySqlExpress();

            StringBuilder sb = new StringBuilder();

            sb.Append("select * from team where 1=1");

            if (txtSearch.Text.Trim().Length > 0)
            {
                dicParam["@namelike"] = m.GetLikeString(txtSearch.Text);
                dicParam["@code"] = txtSearch.Text;

                sb.Append($" and (name like @namelike or code=@code)");
            }

            if (dropStatus.SelectedIndex > 0)
            {
                dicParam["@stat"] = Convert.ToInt32(dropStatus.SelectedValue);

                sb.Append($" and status=@stat");
            }

            sb.Append(" order by name;");

            List<obTeam> lst = null;

            int year = 0;

            int.TryParse(txtYear.Text, out year);

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    m.cmd = cmd;

                    lst = m.GetObjectList<obTeam>(sb.ToString(), dicParam);

                    if (year > 0)
                    {
                        dicParam.Clear();
                        dicParam["@year"] = year;

                        foreach (var t in lst)
                        {
                            dicParam["@teamid"] = t.id;

                            t.lstPlayer = m.GetObjectList<obPlayer>($"select a.* from player a,player_team b where a.id=b.player_id and b.year=@year and b.team_id=@teamid order by a.name;", dicParam);
                        }
                    }

                    conn.Close();
                }
            }

            sb.Clear();

            sb.Append(@"
<div class=""heading1 margin_0"">
<h2>Search Result</h2>
</div>

<table class=""table table-striped"">
<thead>
<tr>
<th>ID</th>
<th>Status</th>
<th>Team Code</th>
<th>Team Name</th>
<th>Total Players</th>
<th></th>
<th>Team Players</th>
</tr>
</thead>
<tbody>
");
            foreach (var t in lst)
            {
                string teamcode = Server.HtmlEncode(t.code);
                string teamname = Server.HtmlEncode(t.name);

                sb.Append($@"
<tr>
<td>{t.id}</td>
<td>{t.StatusStr}</td>
<td>{teamcode}</td>
<td><a href='/TeamEdit?id={t.id}'>{teamname}</a></td>
<td>{t.lstPlayer.Count}</td>
<td>[<a href='/PlayerTeam?year={year}&teamid={t.id}'>Edit Team Player</a>]</td>
<td>");
                foreach (var p in t.lstPlayer)
                {
                    if (p.status == 1)
                        sb.Append($"<a href='/PlayerEdit?id={p.id}'>{p.name}</a><br />");
                    else
                        sb.Append($"<span style='text-decoration: line-through; color: red;'>{p.name}</span><br />");
                }

                sb.Append("</td></tr>");
            }

            sb.Append("</tbody></table>");

            ph1.Controls.Add(new LiteralControl(sb.ToString()));
        }
    }
}