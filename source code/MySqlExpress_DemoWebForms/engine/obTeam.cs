using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.UI.WebControls;

namespace System
{
    public class obTeam
    {
        int id = 0;
        string code = "";
        string name = "";
        int logo_id = 0;
        int status = 0;

        public int Id { get { return id; } set { id = value; } }
        public string Code { get { return code; } set { code = value; } }
        public string Name { get { return name; } set { name = value; } }
        public int LogoId { get { return logo_id; } set { logo_id = value; } }
        public int Status { get { return status; } set { status = value; } }


        int total_players = 0;

        public int TotalPlayers { get { return total_players; } set { total_players = value; } }

        public string StatusStr
        {
            get
            {
                if (status == 1)
                {
                    return "<span style='color: darkgreen;'>Active</span>";
                }
                else
                {
                    return "<span style='color: red;'>Deleted</span>";
                }
            }
        }

        public string ImgLogo
        {
            get
            {
                return GetLogoImg("", "");
            }
        }

        public List<obPlayer> lstPlayer = null;

        public string GetLogoImg(string classname, string style)
        {
            string fileWebPath = GetLogoSrc();

            if (fileWebPath != "")
            {
                return $"<img src='{fileWebPath}' class='{classname}' style='{style}' />";
            }

            return "";
        }

        public string GetLogoSrc()
        {
            string filepath = HttpContext.Current.Server.MapPath($"~/teamlogo/{id}-{logo_id}.png");

            if (File.Exists(filepath))
            {
                return $"/teamlogo/{id}-{logo_id}.png";
            }
            else
            {
                return $"/teamlogo/no-logo.png";
            }
        }

        public string HtmlSelectStatus(int stat)
        {
            if (stat == status)
                return "selected";
            return "";
        }
    }
}