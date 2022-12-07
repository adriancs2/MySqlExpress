using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace System
{
    public class config
    {
        static string _constr = null;

        public static string ConnString
        {
            get
            {
                if (_constr == null)
                {
                    ReadConnectionString();
                }

                return _constr;
            }
            set
            {
                _constr = value;
                SaveConnectionString();
            }
        }

        public static void ReadConnectionString()
        {
            string file = HttpContext.Current.Server.MapPath($"~/App_Data/constr.txt");

            if (File.Exists(file))
            {
                _constr = File.ReadAllText(file);
            }
        }

        static void SaveConnectionString()
        {
            string appdata_folder = HttpContext.Current.Server.MapPath($"~/App_Data");

            if (!Directory.Exists(appdata_folder))
            {
                Directory.CreateDirectory(appdata_folder);
            }

            string file = HttpContext.Current.Server.MapPath($"~/App_Data/constr.txt");

            File.WriteAllText(file, _constr);
        }

        public static DateTime GetDateInput(string input)
        {
            try
            {
                string[] ia = input.Split('-');

                int year = Convert.ToInt32(ia[0]);
                int month = Convert.ToInt32(ia[1]);
                int day = Convert.ToInt32(ia[2]);

                return new DateTime(year, month, day);
            }
            catch
            { }

            return DateTime.MinValue;
        }
    }
}