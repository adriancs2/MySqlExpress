using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System
{
    public class obPlayer
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public DateTime date_register { get; set; }
        public string tel { get; set; }
        public string email { get; set; }
        public int status { get; set; }


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