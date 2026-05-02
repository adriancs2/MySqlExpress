using System.IO;
using System.Web;
using System.Web.Hosting;

namespace System
{
    /// <summary>
    /// Holds the MySQL connection string for the demo.
    ///
    /// The value is persisted to a plain text file under /App_Data so the
    /// setup page can write to it without database access of its own.
    /// This is a demo convention; in production you would use Web.config,
    /// environment variables, or a secrets manager instead.
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
                string appData = HostingEnvironment.MapPath("~/App_Data");
                if (string.IsNullOrEmpty(appData))
                {
                    // Fallback if called outside an HTTP request.
                    appData = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "App_Data");
                }
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
