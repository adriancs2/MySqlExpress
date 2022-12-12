using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MySqlConnector;

namespace MySqlExpress_TestWebForms
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtConnStr.Text = config.ConnString;
                if (txtConnStr.Text.Trim().Length == 0)
                {
                    txtConnStr.Text = "server=127.0.0.1;user=root;pwd=1234;database=test;convertzerodatetime=true;treattinyasboolean=true;";
                }
            }
        }

        protected void btSaveConnStr_Click(object sender, EventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        conn.Close();
                    }
                }

                config.ConnString = txtConnStr.Text;
                ((master1)this.Master).WriteGoodMessage("Connection String Saved");
            }
            catch(Exception ex)
            {
                config.ConnString = null;
                throw ex;
            }
        }

        protected void btGenerateSampleData_Click(object sender, EventArgs e)
        {
            if (config.ConnString == "")
            {
                Response.Redirect("~/ConnectionStringNotInitialized", true);
            }

            List<string> lstSql = new List<string>();

            lstSql.Add("drop table if exists `player`;");
            lstSql.Add("drop table if exists `player_team`;");
            lstSql.Add("drop table if exists `team`;");
            lstSql.Add(@"CREATE TABLE IF NOT EXISTS `player` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `code` varchar(10),
  `name` varchar(300),
  `date_register` datetime,
  `tel` varchar(100),
  `email` varchar(100),
  `status` int unsigned,
  PRIMARY KEY (`id`));");
            lstSql.Add(@"CREATE TABLE IF NOT EXISTS `player_team` (
  `year` int unsigned NOT NULL,
  `player_id` int unsigned NOT NULL,
  `team_id` int unsigned,
  `score` decimal(10,2),
  `level` int unsigned,
  `status` int unsigned,
  PRIMARY KEY (`year`,`player_id`))");
            lstSql.Add(@"CREATE TABLE IF NOT EXISTS `team` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `code` varchar(45),
  `name` varchar(300),
  `logo_id` int,
  `status` int unsigned,
  PRIMARY KEY (`id`))");

            Random rd = new Random();
            DateTime dateRegister = DateTime.Now.AddDays(-50);
            Dictionary<string, int> dicCode = new Dictionary<string, int>();

            using (MySqlConnection conn = new MySqlConnection(config.ConnString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    MySqlExpress m = new MySqlExpress(cmd);

                    foreach (var sql in lstSql)
                    {
                        m.Execute(sql);
                    }

                    string names = "Smith,Johnson,Williams,Jones,Brown,Davis,Miller,Wilson,Moore,Taylor,Anderson,Thomas,Jackson,White,Harris,Martin,Thompson,Garcia,Martinez,Robinson,Clark,Rodriguez,Lewis,Lee,Walker,Hall,Allen,Young,Hernandez,King,Wright,Lopez,Hill,Scott,Green,Adams,Baker,Gonzalez,Nelson,Carter,Mitchell,Perez,Roberts,Turner,Phillips,Campbell,Parker,Evans,Edwards,Collins,Stewart,Sanchez,Morris,Rogers,Reed,Cook,Morgan,Bell,Murphy,Bailey,Rivera,Cooper,Richardson,Cox,Howard,Ward,Torres,Peterson,Gray,Ramirez,James,Watson,Brooks,Kelly,Sanders,Price,Bennett,Wood,Barnes,Ross,Henderson,Coleman,Jenkins,Perry,Powell,Long,Patterson,Hughes,Flores,Washington,Butler,Simmons,Foster,Gonzales,Bryant,Alexander,Russell,Griffin,Diaz,Hayes,Aiden,Jackson,Mason,Liam,Jacob,Jayden,Ethan,Noah,Lucas,Logan,Caleb,Caden,Jack,Ryan,Connor,Michael,Elijah,Brayden,Benjamin,Nicholas,Alexander,William,Matthew,James,Landon,Nathan,Dylan,Evan,Luke,Andrew,Gabriel,Gavin,Joshua,Owen,Daniel,Carter,Tyler,Cameron,Christian,Wyatt,Henry,Eli,Joseph,Max,Isaac,Samuel,Anthony,Grayson,Zachary,David,Christopher,John,Isaiah,Levi,Jonathan,Oliver,Chase,Cooper,Tristan,Colton,Austin,Colin,Charlie,Dominic,Parker,Hunter,Thomas,Alex,Ian,Jordan,Cole,Julian,Aaron,Carson,Miles,Blake,Brody,Adam,Sebastian,Adrian,Nolan,Sean,Riley,Bentley,Xavier,Hayden,Jeremiah,Jason,Jake,Asher,Micah,Jace,Brandon,Josiah,Hudson,Nathaniel,Bryson,Ryder,Justin,Bryce,Sophia,Emma,Isabella,Olivia,Ava,Lily,Chloe,Madison,Emily,Abigail,Addison,Mia,Madelyn,Ella,Hailey,Kaylee,Avery,Kaitlyn,Riley,Aubrey,Brooklyn,Peyton,Layla,Hannah,Charlotte,Bella,Natalie,Sarah,Grace,Amelia,Kylie,Arianna,Anna,Elizabeth,Sophie,Claire,Lila,Aaliyah,Gabriella,Elise,Lillian,Samantha,Makayla,Audrey,Alyssa,Ellie,Alexis,Isabelle,Savannah,Evelyn,Leah,Keira,Allison,Maya,Lucy,Sydney,Taylor,Molly,Lauren,Harper,Scarlett,Brianna,Victoria,Liliana,Aria,Kayla,Annabelle,Gianna,Kennedy,Stella,Reagan,Julia,Bailey,Alexandra,Jordyn,Nora,Carolin,Mackenzie,Jasmine,Jocelyn,Kendall,Morgan,Nevaeh,Maria,Eva,Juliana,Abby,Alexa,Summer,Brooke,Penelope,Violet,Kate,Hadley,Ashlyn,Sadie,Paige,Katherine,Sienna,Piper";

                    string[] na = names.Split(',');

                    m.StartTransaction();

                    for (int i = 0; i < 100; i++)
                    {
                        string firstname = na[rd.Next(0, na.Length)];
                        string lastname = na[rd.Next(0, na.Length)];
                        string name = $"{firstname} {lastname}";
                        string tel = $"{rd.Next(10000, 99999)}{rd.Next(10000, 99999)}";

                        string email = "";

                        int emailformat = rd.Next(1, 4);
                        int emailExtension = rd.Next(1, 8);

                        switch (emailformat)
                        {
                            case 1: email = name.ToLower().Replace(" ", "_"); break;
                            case 2: email = name.ToLower().Replace(" ", "."); break;
                            case 3: email = firstname + rd.Next(1, 999); break;
                            case 4: email = lastname + rd.Next(1, 999); break;
                        }

                        switch (emailExtension)
                        {
                            case 1: email = email + "@gmail.com"; break;
                            case 2: email = email + "@yahoo.com"; break;
                            case 3: email = email + "@outlook.com"; break;
                            case 4: email = email + "@mail.com"; break;
                            case 5: email = email + "@zoho.com"; break;
                            case 6: email = email + "@protonmail.com"; break;
                            case 7: email = email + "@aol.com"; break;
                        }

                        email = email.ToLower();

                        dateRegister = dateRegister.AddDays(rd.Next(0, 2));

                        string code = name.Substring(0, 1).ToUpper();

                        if (!dicCode.ContainsKey(code))
                        {
                            dicCode[code] = 0;
                        }

                        dicCode[code] = dicCode[code] + 1;

                        code = code + dicCode[code].ToString().PadLeft(4, '0');

                        Dictionary<string, object> dic = new Dictionary<string, object>();

                        dic["code"] = code;
                        dic["name"] = name;
                        dic["date_register"] = dateRegister;
                        dic["tel"] = tel;
                        dic["email"] = email;
                        dic["status"] = 1;

                        m.Insert("player", dic);
                    }

                    m.Commit();
                    m.StartTransaction();

                    List<string> lstTeamName = new List<string>();

                    for (int i = 0; i < 15; i++)
                    {
                        string newteamname = na[rd.Next(0, na.Length)];

                        while (lstTeamName.Contains(newteamname))
                        {
                            newteamname = na[rd.Next(0, na.Length)];
                        }

                        lstTeamName.Add(newteamname);
                    }

                    foreach (var t in lstTeamName)
                    {
                        Dictionary<string, object> dic = new Dictionary<string, object>();

                        dic["code"] = "T00-101-" + rd.Next(100, 999);
                        dic["name"] = t;
                        dic["status"] = 1;
                        dic["logo_id"] = 1;

                        m.Insert("team", dic);
                    }

                    List<obTeam> lstTeam = m.GetObjectList<obTeam>($"select * from team;");
                    List<obPlayer> lstPlayer = m.GetObjectList<obPlayer>($"select * from player;");

                    int year = dateRegister.Year;

                    List<string> lstUpdateCol = new List<string>();

                    lstUpdateCol.Add("team_id");
                    lstUpdateCol.Add("score");
                    lstUpdateCol.Add("level");
                    lstUpdateCol.Add("status");

                    foreach (var p in lstPlayer)
                    {
                        Dictionary<string, object> dic = new Dictionary<string, object>();

                        decimal score = (decimal)rd.Next(1000, 9999);
                        int level = Convert.ToInt32(score / 1000m);

                        dic["year"] = year;
                        dic["player_id"] = p.id;
                        dic["team_id"] = lstTeam[rd.Next(0, lstTeam.Count)].id;
                        dic["score"] = score;
                        dic["level"] = level;
                        dic["status"] = 1;

                        m.InsertUpdate("player_team", dic, lstUpdateCol);
                    }

                    m.Commit();

                    conn.Close();
                }
            }

            ((master1)this.Master).WriteGoodMessage("Sample Data Created/Regerated!");
        }
    }
}