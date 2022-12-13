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
    public partial class PlayerList : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                List<obTeam> lstTeam = null;

                using (MySqlConnection conn = new MySqlConnection(config.ConnString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        lstTeam = m.GetObjectList<obTeam>($"select * from team where status=1 order by name;");

                        conn.Close();
                    }
                }

                dropTeam.Items.Add(new ListItem("---", "0"));

                foreach (var t in lstTeam)
                {
                    dropTeam.Items.Add(new ListItem(t.Name, t.Id.ToString()));
                }

                txtYear.Text = DateTime.Now.Year.ToString();
            }
        }

        protected void btSearch_Click(object sender, EventArgs e)
        {
            int year = 0;

            int.TryParse(txtYear.Text, out year);

            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["@year"] = year;

            MySqlExpress m = new MySqlExpress();

            StringBuilder sb = new StringBuilder();

            sb.Append("select a.*");

            if (year > 0)
            {
                sb.Append(",b.year `year`, c.id 'teamid', c.name 'teamname',c.code 'teamcode'");
            }

            sb.Append(" from player a");

            if (year > 0)
            {
                sb.Append($" left join player_team b on a.id=b.player_id and b.year=@year left join team c on b.team_id=c.id");
            }

            sb.Append(" where 1=1");

            if (txtSearch.Text.Trim().Length > 0)
            {
                dicParam["@nameLike"] = m.GetLikeString(txtSearch.Text);
                dicParam["@code"] = txtSearch.Text;

                sb.Append($" and (a.name like @nameLike or a.code=@code or a.email=@code or a.tel=@code)");
            }

            if (year > 0 && dropTeam.SelectedIndex > 0)
            {
                dicParam["@teamid"] = Convert.ToInt32(dropTeam.SelectedValue);
                sb.Append($" and b.team_id=@teamid");
            }

            DateTime datefrom = config.GetDateInput(txtDateRegisterFrom.Text);
            DateTime dateto = config.GetDateInput(txtDateRegisterTo.Text);

            if (datefrom != DateTime.MinValue)
            {
                dicParam["@datefrom"] = datefrom;
                sb.Append($" and a.date_register>=@datefrom");
            }

            if (dateto != DateTime.MinValue)
            {
                dicParam["@dateto"] = dateto;
                sb.Append($" and a.date_register<=@dateto");
            }

            if (dropStatus.SelectedIndex > 0)
            {
                dicParam["@stat"] = Convert.ToInt32(dropStatus.SelectedValue);
                sb.Append($" and a.status=@stat");
            }

            sb.Append(" order by a.name;");

            List<obPlayerTeam> lst = null;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    m.cmd = cmd;

                    lst = m.GetObjectList<obPlayerTeam>(sb.ToString(), dicParam);

                    conn.Close();
                }
            }

            sb.Clear();

            string yearstr = "";

            if (year > 0)
            {
                yearstr = $": Year {year}";
            }

            sb.Append($@"
<div class=""heading1 margin_0"">
<h2>Search Result{yearstr}</h2>
</div>

<table class=""table table-striped"">
<thead>
<tr>
<th>ID</th>
<th>Status</th>
<th>Code</th>
<th>Name</th>
<th>Date Register</th>
<th>Email</th>
<th>Tel</th>
<th>Team</th>
</tr>
</thead>
<tbody>
");
            foreach (var p in lst)
            {
                string team_year = "";

                if (year > 0)
                {
                    team_year = $"<a href='/PlayerTeam?year={year}&teamid={p.Teamid}'>{p.Teamname}</a> ({year})";
                }

                sb.Append($@"
<tr>
<td>{p.Id}</td>
<td>{p.StatusStr}</td>
<td>{Server.HtmlEncode(p.Code)}</td>
<td><a href='/PlayerEdit?id={p.Id}' target='_blank'>{Server.HtmlEncode(p.Name)}</a></td>
<td>{p.DateRegisterStr}</td>
<td>{Server.HtmlEncode(p.Email)}</td>
<td>{Server.HtmlEncode(p.Tel)}</td>
<td>{team_year}</td>
</tr>
");
            }

            sb.Append("</tbody></table>");

            ph1.Controls.Add(new LiteralControl(sb.ToString()));
        }
    }
}