using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace System.pages
{
    public partial class apiPlayerTeam : System.Web.UI.Page
    {
        int action = 0;
        int year = 0;
        int teamid = 0;
        string search;
        string pid = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                action = Convert.ToInt32(Request.QueryString["action"]);
                year = Convert.ToInt32(Request.QueryString["year"]);
                teamid = Convert.ToInt32(Request.QueryString["teamid"]);
                search = Server.UrlDecode(Request.QueryString["search"] + "").Trim();
                pid = Server.UrlDecode(Request.QueryString["pid"] + "").Trim();
            }
            catch
            {
                return;
            }

            switch (action)
            {
                case 1:
                    LoadTeamPlayers();
                    break;
                case 2:
                    SearchPlayers();
                    break;
                case 3:
                    RemovePlayers();
                    break;
                case 4:
                    AddPlayers();
                    break;
                case 5:
                    GetTeamInfo();
                    break;
            }

        }

        void GetTeamInfo()
        {
            int teamid = 0;

            if (!int.TryParse(Request.QueryString["teamid"] + "", out teamid))
            {
                return;
            }

            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["@teamid"] = teamid;

            obTeam t = null;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    t = m.GetObject<obTeam>($"select * from team where id=@teamid limit 0,1;", dicParam);

                    conn.Close();
                }
            }

            string jsonstr = JsonSerializer.Serialize(t);
            Response.Write(jsonstr);
        }

        void LoadTeamPlayers()
        {
            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["@year"] = year;
            dicParam["@teamid"] = teamid;

            List<obPlayer> lstPlayer = null;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    lstPlayer = m.GetObjectList<obPlayer>($"select a.* from player a, player_team b where a.id=b.player_id and b.team_id=@teamid and b.year=@year order by a.name;", dicParam);

                    conn.Close();
                }
            }

            string json = JsonSerializer.Serialize(lstPlayer);
            Response.Write(json);
        }

        void SearchPlayers()
        {
            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["@year"] = year;

            MySqlExpress m = new MySqlExpress();

            StringBuilder sb = new StringBuilder();

            sb.Append($"select a.*, c.id 'teamid', c.name 'teamname' from player a left join player_team b on a.id=b.player_id and b.`year`=@year left join team c on b.team_id=c.id where 1=1");

            if (search.Trim().Length > 0)
            {
                dicParam["@namelike"] = m.GetLikeString(search);
                dicParam["@playercode"] = search;

                sb.Append($" and (a.name like @namelike or a.code=@playercode or a.tel=@playercode or a.email=@playercode)");
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

                    lstPlayer = m.GetObjectList<obPlayerTeam>(sb.ToString(), dicParam);

                    conn.Close();
                }
            }

            for (int i = lstPlayer.Count - 1; i >= 0; i--)
            {
                if (lstPlayer[i].Teamid == teamid)
                {
                    lstPlayer.RemoveAt(i);
                }
            }

            string json = JsonSerializer.Serialize(lstPlayer);
            Response.Write(json);
        }

        List<int> GetListId()
        {
            List<int> lst = new List<int>();
            string[] sa = pid.Split(',');
            foreach (var s in sa)
            {
                int id = 0;
                int.TryParse(s, out id);
                if (id > 0)
                {
                    lst.Add(id);
                }
            }

            return lst;
        }

        void RemovePlayers()
        {
            List<int> lstId = GetListId();

            if (lstId.Count > 0)
            {
                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["@year"] = year;

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
                            dicParam["@playerid"] = id;
                            m.Execute($"delete from player_team where `year`=@year and player_id=@playerid limit 1;", dicParam);
                        }

                        m.Commit();

                        conn.Close();
                    }
                }
            }

            LoadTeamPlayers();
        }

        void AddPlayers()
        {
            List<int> lstId = GetListId();

            if (lstId.Count > 0)
            {
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
            }

            LoadTeamPlayers();
        }
    }
}