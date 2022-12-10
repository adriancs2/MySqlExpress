using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System
{
    public class obPlayerTeam
    {
        public int id = 0;
        public string code = "";
        public string name = "";
        public DateTime date_register = DateTime.MinValue;
        public string tel = "";
        public string email = "";
        public int status = 0;
        public int year = 0;
        public string teamname = "";
        public string teamcode = "";
        public int team_id = 0;


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