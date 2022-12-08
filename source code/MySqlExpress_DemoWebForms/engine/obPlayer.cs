using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System
{
    public class obPlayer
    {
        int id = 0;
        string code = "";
        string name = "";
        DateTime date_register = DateTime.MinValue;
        string tel = "";
        string email = "";
        int status = 0;

        public int Id { get { return id; } set { id = value; } }
        public string Code { get { return code; } set { code = value; } }
        public string Name { get { return name; } set { name = value; } }
        public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
        public string Tel { get { return tel; } set { tel = value; } }
        public string Email { get { return email; } set { email = value; } }
        public int Status { get { return status; } set { status = value; } }


        public string DateRegisterStr { get { if (date_register != DateTime.MinValue) return date_register.ToString("dd-MM-yyyy"); return ""; } }


        public string DateRegisterInput { get { if (date_register != DateTime.MinValue) return date_register.ToString("yyyy-MM-dd"); return ""; } }

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