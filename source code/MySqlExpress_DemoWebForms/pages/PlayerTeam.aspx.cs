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

                obTeam team = null;

                using (MySqlConnection conn = new MySqlConnection(config.ConnString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        team = m.GetObject<obTeam>($"select * from team where id={teamid} limit 1;");

                        conn.Close();
                    }
                }

                if (team.Id == 0)
                {
                    Response.Redirect("~/TeamList", true);
                }

                lbTeamName.Text = $"{year} - Team: {team.Name} ({team.Code})";

                LoadTeamPlayers();
            }
        }

        void LoadTeamPlayers()
        {
            List<obPlayer> lstPlayer = null;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    lstPlayer = m.GetObjectList<obPlayer>($"select a.* from player a, player_team b where a.id=b.player_id and b.team_id={teamid} and b.year={year} order by a.name;");

                    conn.Close();
                }
            }

            StringBuilder sb = new StringBuilder();

            sb.Append($@"

[ <a href='#' onclick=""selectAll('tb1', true); return false;"">Select All</a> |
<a href='#' onclick=""selectAll('tb1', false); return false;"">Clear Selection</a> ]

<table id='tb1' class=""table table-striped"">
<thead>
<tr>
<th>Remove</th>
<th>Name</th>
</tr>
</thead>
<tbody>
");
            foreach (var p in lstPlayer)
            {
                sb.Append($@"
<tr>
<td><input type='checkbox' name='cbRemove_{p.Id}' /></td>
<td><a href='/PlayerEdit?id={p.Id}' target='_blank'>{p.Name}</a></td>
</tr>
");
            }

            sb.Append("</tbody></table>");

            ph1.Controls.Add(new LiteralControl(sb.ToString()));
        }

        protected void btRemovePlayer_Click(object sender, EventArgs e)
        {
            List<int> lstId = new List<int>();

            foreach (var key in Request.Form.AllKeys)
            {
                if (key.StartsWith("cbRemove_"))
                {
                    string[] ka = key.Split('_');

                    int playerid = Convert.ToInt32(ka[1]);
                    lstId.Add(playerid);
                }
            }

            if (lstId.Count == 0)
            {
                LoadTeamPlayers();
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    m.StartTransaction();

                    foreach(var id in lstId)
                    {
                        m.Execute($"delete from player_team where `year`={year} and player_id={id} limit 1;");
                    }

                    m.Commit();

                    conn.Close();
                }
            }

            LoadTeamPlayers();

            ((master1)this.Master).WriteGoodMessage($"{lstId.Count} Selected Player(s) Removed Successfully");
        }

        protected void btSearch_Click(object sender, EventArgs e)
        {
            LoadTeamPlayers();

            MySqlExpress m = new MySqlExpress();

            StringBuilder sb = new StringBuilder();

            sb.Append($"select a.*,c.name 'teamname' from player a left join player_team b on a.id=b.player_id and b.`year`={year} and b.team_id!={teamid} left join team c on b.team_id=c.id where 1=1");

            if (txtSearch.Text.Trim().Length > 0)
            {
                string likestr = m.GetLikeString(txtSearch.Text);
                string codestr = m.Escape(txtSearch.Text);

                sb.Append($" and (a.name like {likestr} or a.code='{codestr}' or a.tel='{codestr}' or a.email='{codestr}')");
            }

            sb.Append($" order by a.name;");

            List<obPlayerTeam> lstPlayer = null;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    m.cmd = cmd;

                    lstPlayer = m.GetObjectList<obPlayerTeam>(sb.ToString());

                    conn.Close();
                }
            }

            sb.Clear();

            sb.Append($@"
[ <a href='#' onclick=""selectAll('tb2', true); return false;"">Select All</a> |
<a href='#' onclick=""selectAll('tb2', false); return false;"">Clear Selection</a> ]

<table id='tb2' class=""table table-striped"">
<thead>
<tr>
<th>Add</th>
<th>Name</th>
<th>Joined Team</th>
</tr>
</thead>
<tbody>
");
            foreach (var p in lstPlayer)
            {
                sb.Append($@"
<tr>
<td><input type='checkbox' name='cbAdd_{p.Id}' /></td>
<td><a href='/PlayerEdit?id={p.Id}' target='_blank'>{p.Name}</a></td>
<td>{p.Teamname}</td>
</tr>
");
            }

            sb.Append("</tbody></table>");

            ph2.Controls.Add(new LiteralControl(sb.ToString()));
        }

        protected void btAddPlayer_Click(object sender, EventArgs e)
        {
            List<int> lstId = new List<int>();

            foreach (var key in Request.Form.AllKeys)
            {
                if (key.StartsWith("cbAdd_"))
                {
                    string[] ka = key.Split('_');

                    int playerid = Convert.ToInt32(ka[1]);
                    lstId.Add(playerid);
                }
            }

            if (lstId.Count == 0)
            {
                LoadTeamPlayers();
                return;
            }

            List<string> lstUpdateCol = new List<string>();

            lstUpdateCol.Add("team_id");
            lstUpdateCol.Add("score");
            lstUpdateCol.Add("level");
            lstUpdateCol.Add("status");

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    m.StartTransaction();

                    foreach (var id in lstId)
                    {
                        Dictionary<string, object> dic = new Dictionary<string, object>();

                        dic["year"] = year;
                        dic["player_id"] = id;
                        dic["team_id"] = teamid;
                        dic["score"] = 0m;
                        dic["level"] = 0;
                        dic["status"] = 1;

                        m.InsertUpdate("player_team", dic, lstUpdateCol);
                    }

                    m.Commit();

                    conn.Close();
                }
            }

            LoadTeamPlayers();

            ((master1)this.Master).WriteGoodMessage($"{lstId.Count} Selected Player(s) Added Successfully");
        }
    }
}