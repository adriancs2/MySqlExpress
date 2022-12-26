using MySqlConnector;
using MySqlExpress_TestWebForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace System.pages
{
    public partial class TeamEdit : System.Web.UI.Page
    {
        int id { get { return Convert.ToInt32(ViewState["id"]); } set { ViewState["id"] = value; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["id"] != null)
                {
                    id = Convert.ToInt32(Request.QueryString["id"]);
                }

                LoadData();
            }
        }

        void LoadData()
        {
            if (id == 0)
            {
                lbId.Text = "[new]";
                lbStatus.Text = "[new]";
                btDelete.Visible = false;
                btRecover.Visible = false;
            }
            else
            {
                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["@id"] = id;

                obTeam p = null;

                using (MySqlConnection conn = new MySqlConnection(config.ConnString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        p = m.GetObject<obTeam>($"select * from team where id=@id limit 0,1;", dicParam);

                        conn.Close();
                    }
                }

                lbId.Text = p.Id.ToString();
                lbStatus.Text = p.StatusStr;
                txtCode.Text = p.Code;
                txtName.Text = p.Name;
                imgLogo.ImageUrl = p.GetLogoSrc();

                if (p.Status == 1)
                {
                    btDelete.Visible = true;
                    btRecover.Visible = false;
                }
                else if (p.Status == 0)
                {
                    btDelete.Visible = false;
                    btRecover.Visible = true;
                }
            }
        }

        void Save()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            dic["code"] = txtCode.Text;
            dic["name"] = txtName.Text;

            if (id == 0)
                dic["status"] = 1;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    if (id == 0)
                    {
                        m.Insert("team", dic);
                        id = m.LastInsertId;
                    }
                    else
                    {
                        m.Update("team", dic, "id", id);
                    }

                    if (fileLogo.HasFile)
                    {
                        engineTeam.SaveLogo(m, id, fileLogo.FileBytes);
                    }

                    conn.Close();
                }
            }
        }

        protected void btSave2_Click(object sender, EventArgs e)
        {
            obTeam t = new obTeam();

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    if (id == 0)
                    {
                        t.Status = 1;
                    }
                    else
                    {
                        Dictionary<string, object> dicParam = new Dictionary<string, object>();
                        dicParam["@id"] = id;

                        t = m.GetObject<obTeam>("select * from team where id=@id limit 0,1;", dicParam);
                    }

                    t.Code = txtCode.Text;
                    t.Name = txtName.Text;

                    m.Save("team", t);

                    if (id == 0)
                    {
                        id = m.LastInsertId;
                    }

                    if (fileLogo.HasFile)
                    {
                        engineTeam.SaveLogo(m, id, fileLogo.FileBytes);
                    }

                    conn.Close();
                }
            }

            LoadData();

            ((master1)this.Master).WriteGoodMessage("Data Saved");
        }

        protected void btSave_Click(object sender, EventArgs e)
        {
            Save();
            LoadData();
            ((master1)this.Master).WriteGoodMessage("Data Saved");
        }

        protected void btSaveNew_Click(object sender, EventArgs e)
        {
            Save();
            ((master1)this.Master).WriteSessionGoodMessage("Data Saved");
            Response.Redirect("~/TeamEdit", true);
        }

        protected void btDelete_Click(object sender, EventArgs e)
        {
            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["@id"] = id;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    m.Execute($"update team set status=0 where id=@id limit 1;", dicParam);

                    conn.Close();
                }
            }

            ((master1)this.Master).WriteGoodMessage("Team Deleted");

            LoadData();
        }

        protected void btRecover_Click(object sender, EventArgs e)
        {
            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["@id"] = id;

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    m.Execute($"update team set status=1 where id=@id limit 1;", dicParam);

                    conn.Close();
                }
            }

            ((master1)this.Master).WriteGoodMessage("Team Recovered");

            LoadData();
        }

    }
}