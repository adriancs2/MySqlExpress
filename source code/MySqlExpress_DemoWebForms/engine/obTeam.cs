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
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public int logo_id { get; set; }
        public int status { get; set; }
        public int total_players { get; set; }


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

            return "";
        }
    }
}