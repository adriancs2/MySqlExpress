using System.IO;

namespace System
{
    /// <summary>
    /// Holds the MySQL connection string for the demo.
    ///
    /// In the ASP.NET version this lived under /App_Data with
    /// <c>HostingEnvironment.MapPath</c>. In the console version there is
    /// no hosting environment — App_Data is just a folder next to the EXE.
    /// </summary>
    public static class Config
    {
        public static string ConnString = "";

        public static bool HasConnString
        {
            get { return !string.IsNullOrWhiteSpace(ConnString); }
        }

        static string FilePath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string appData = Path.Combine(baseDir, "App_Data");
                if (!Directory.Exists(appData))
                    Directory.CreateDirectory(appData);
                return Path.Combine(appData, "mysql_conn.txt");
            }
        }

        public static void Load()
        {
            try
            {
                string p = FilePath;
                if (File.Exists(p))
                    ConnString = File.ReadAllText(p).Trim();
            }
            catch { /* first-run — fine */ }
        }

        public static void Save(string connString)
        {
            ConnString = connString ?? "";
            File.WriteAllText(FilePath, ConnString);
        }

        public static void Clear()
        {
            ConnString = "";
            try
            {
                string p = FilePath;
                if (File.Exists(p))
                    File.Delete(p);
            }
            catch { }
        }
    }
}
