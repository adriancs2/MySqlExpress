using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySqlConnector;
using System.Text;
using System.IO;
using System.engine;

namespace System.pages
{
    public partial class TeamEditBatch : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadData();
            }
        }

        void LoadData()
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

            StringBuilder sb = new StringBuilder();

            sb.Append($@"
<div class=""heading1 margin_0"">
<h2>Search Result</h2>
</div>

Total Found: {lstTeam.Count}<br />
<br />

<table class=""table table-striped"">
<thead>
<tr>
<th>ID</th>
<th>Status</th>
<th>Team Code</th>
<th>Team Name</th>
<th>Logo</th>
<th>Change Logo</th>
</tr>
</thead>
<tbody>
");

            foreach (var t in lstTeam)
            {
                sb.Append($@"
<tr>
<td>{t.id}</td>
<td>
<select name='input_{t.id}_status'>
<option value='1' {t.HtmlSelectStatus(1)}>Active</option>
<option value='0' {t.HtmlSelectStatus(0)}>Deleted</option>
</select>
</td>
<td><input type='text' name='input_{t.id}_code' style='width: 150px;' value='{Server.HtmlEncode(t.code)}' /></td>
<td><input type='text' name='input_{t.id}_name' style='width: 200px;' value='{Server.HtmlEncode(t.name)}' /></td>
<td>{t.ImgLogo}</td>
<td><input type='file' name='file_{t.id}' />
</tr>
");
            }

            sb.Append("</tbody></table>");

            ph1.Controls.Add(new LiteralControl(sb.ToString()));
        }

        protected void btLoad_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        protected void btSave_Click(object sender, EventArgs e)
        {
            Dictionary<int, obTeam> dicTeam = new Dictionary<int, obTeam>();

            foreach (var key in Request.Form.AllKeys)
            {
                if (key.StartsWith("input_"))
                {
                    string[] ka = key.Split('_');
                    int teamid = Convert.ToInt32(ka[1]);

                    if (!dicTeam.ContainsKey(teamid))
                    {
                        dicTeam[teamid] = new obTeam();
                    }

                    switch (ka[2])
                    {
                        case "status":
                            dicTeam[teamid].status = Convert.ToInt32(Request.Form[key]);
                            break;
                        case "code":
                            dicTeam[teamid].code = Request.Form[key];
                            break;
                        case "name":
                            dicTeam[teamid].name = Request.Form[key];
                            break;
                    }
                }
            }

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    m.StartTransaction();

                    foreach (var kv in dicTeam)
                    {
                        int id = kv.Key;
                        var t = kv.Value;

                        Dictionary<string, object> dic = new Dictionary<string, object>();
                        dic["status"] = t.status;
                        dic["code"] = t.code;
                        dic["name"] = t.name;

                        m.Update("team", dic, "id", id);
                    }

                    m.Commit();

                    m.StartTransaction();

                    foreach (var key in Request.Files.AllKeys)
                    {
                        HttpPostedFile file = Request.Files[key];

                        if (file.InputStream.Length == 0)
                        {
                            continue;
                        }

                        string[] ka = key.Split('_');
                        int id = Convert.ToInt32(ka[1]);

                        byte[] ba = new byte[file.InputStream.Length];

                        for (int i = 0; i < ba.Length; i++)
                        {
                            file.InputStream.Read(ba, 0, ba.Length);
                        }

                        engineTeam.SaveLogo(m, id, ba);
                    }

                    m.Commit();

                    conn.Close();
                }
            }

            LoadData();

            ((master1)this.Master).WriteGoodMessage("Data Saved");
        }

        protected void btAdd_Click(object sender, EventArgs e)
        {
            int addno = 0;

            int.TryParse(txtAddNo.Text, out addno);

            if (addno == 0)
            {
                LoadData();
                return;
            }

            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic["status"] = 1;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    for (int i = 0; i < addno; i++)
                    {
                        m.Insert("team", dic);
                    }

                    conn.Close();
                }
            }

            LoadData();
        }
    }
}