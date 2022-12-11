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

                lbId.Text = p.id.ToString();
                lbStatus.Text = p.StatusStr;
                txtCode.Text = p.code;
                txtName.Text = p.name;
                imgLogo.ImageUrl = p.GetLogoSrc();

                if (p.status == 1)
                {
                    btDelete.Visible = true;
                    btRecover.Visible = false;
                }
                else if (p.status == 0)
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
                        Dictionary<string, object> dicParam = new Dictionary<string, object>();
                        dicParam["id"] = id;

                        int logoid = m.ExecuteScalar<int>("select logo_id from team where id=@id;", dicParam);

                        logoid++;

                        MemoryStream ms = new MemoryStream(fileLogo.FileBytes);
                        System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
                        img = ImageFunc.ResizeCropFitToSize(img, 100, 100);

                        string filePath = Server.MapPath($"~/teamlogo/{id}-{logoid}.png");
                        img.Save(filePath);

                        dicParam.Clear();
                        dicParam["@logoid"] = logoid;
                        dicParam["@id"] = id;

                        m.Execute("update team set logo_id=@logoid where id=@id;", dicParam);
                    }

                    conn.Close();
                }
            }
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