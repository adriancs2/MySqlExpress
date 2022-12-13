using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System
{
    public class obPlayerTeam
    {
        int id = 0;
        string code = "";
        string name = "";
        DateTime date_register = DateTime.MinValue;
        string tel = "";
        string email = "";
        int status = 0;
        int year = 0;
        string teamname = "";
        string teamcode = "";
        int teamid = 0;

        public int Id { get { return id; } set { id = value; } }
        public string Code { get { return code; } set { code = value; } }
        public string Name { get { return name; } set { name = value; } }
        public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
        public string Tel { get { return tel; } set { tel = value; } }
        public string Email { get { return email; } set { email = value; } }
        public int Status { get { return status; } set { status = value; } }
        public int Year { get { return year; } set { year = value; } }
        public string Teamname { get { return teamname; } set { teamname = value; } }
        public string Teamcode { get { return teamcode; } set { teamcode = value; } }
        public int Teamid { get { return teamid; } set { teamid = value; } }


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