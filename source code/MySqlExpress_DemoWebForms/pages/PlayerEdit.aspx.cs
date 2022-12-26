using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySqlConnector;
using MySqlExpress_TestWebForms;

namespace System.pages
{
    public partial class PlayerEdit : System.Web.UI.Page
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
                obPlayer p = null;

                Dictionary<string, object> dicParam = new Dictionary<string, object>();
                dicParam["@id"] = id;

                using (MySqlConnection conn = new MySqlConnection(config.ConnString))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        p = m.GetObject<obPlayer>($"select * from player where id=@id limit 0,1;", dicParam);

                        conn.Close();
                    }
                }

                lbId.Text = p.Id.ToString();
                lbStatus.Text = p.StatusStr;
                txtCode.Text = p.Code;
                txtName.Text = p.Name;
                txtEmail.Text = p.Email;
                txtTel.Text = p.Tel;
                txtDateRegister.Text = p.DateRegisterInput;

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
            DateTime dateRegister = config.GetDateInput(txtDateRegister.Text);

            Dictionary<string, object> dic = new Dictionary<string, object>();

            dic["code"] = txtCode.Text;
            dic["name"] = txtName.Text;
            dic["date_register"] = dateRegister;
            dic["tel"] = txtTel.Text;
            dic["email"] = txtEmail.Text;

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
                        m.Insert("player", dic);
                        id = m.LastInsertId;
                    }
                    else
                    {
                        m.Update("player", dic, "id", id);
                    }

                    conn.Close();
                }
            }
        }

        protected void btSave2_Click(object sender, EventArgs e)
        {
            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["@id"] = id;

            obPlayer p = new obPlayer();

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    if (id > 0)
                    {
                        p = m.GetObject<obPlayer>("select * from player where id=@id limit 0,1;", dicParam);
                    }

                    p.Code = txtCode.Text;
                    p.Name = txtName.Text;
                    p.DateRegister = config.GetDateInput(txtDateRegister.Text);
                    p.Tel = txtTel.Text;
                    p.Email = txtEmail.Text;

                    if (id == 0)
                    {
                        p.Status = 1;
                    }

                    m.Save("player", p);

                    if (id == 0)
                    {
                        id = m.LastInsertId;
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
            Response.Redirect("~/PlayerEdit", true);
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

                    m.Execute($"update player set status=0 where id=@id limit 1;", dicParam);

                    conn.Close();
                }
            }

            ((master1)this.Master).WriteGoodMessage("Player Deleted");

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

                    m.Execute($"update player set status=1 where id=@id limit 1;", dicParam);

                    conn.Close();
                }
            }

            ((master1)this.Master).WriteGoodMessage("Player Recovered");

            LoadData();
        }

    }
}