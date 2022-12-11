using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System
{
    public class obPlayerTeam
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public DateTime date_register { get; set; }
        public string tel { get; set; }
    public string email { get; set; }
        public int status { get; set; }
        public int year { get; set; }
        public string teamname { get; set; }
        public string teamcode { get; set; }
        public int team_id { get; set; }


        public string DateRegisterStr { get { if (date_register != DateTime.MinValue) return date_register.ToString("dd MMM yyyy"); return ""; } }

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
    }
}