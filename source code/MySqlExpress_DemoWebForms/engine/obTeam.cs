using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System
{
    public class obTeam
    {
        int id = 0;
        string code = "";
        string name = "";
        int status = 0;

        public int Id { get { return id; } set { id = value; } }
        public string Code { get { return code; } set { code = value; } }
        public string Name { get { return name; } set { name = value; } }
        public int Status { get { return status; } set { status = value; } }


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


        public List<obPlayer> lstPlayer = null;
    }
}