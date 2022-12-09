using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace System
{
    public class obTeam
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public int status { get; set; }


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