using System;
using System.IO;

namespace Demo_ASPNET_Core.engine
{
    /// <summary>
    /// Holds the MySQL connection string for the demo.
    ///
    /// The value is persisted to a plain text file under /App_Data so the
    /// setup page can write to it without database access of its own.
    /// This is a demo convention; in production you would use
    /// appsettings.json, environment variables, or a secrets manager.
    ///
    /// ContentRoot is set once during Program.Main bootstrap; the rest of
    /// the file system layout follows the Web Forms version 1:1.
    /// </summary>
    public static class Config
    {
        public static string ConnString = "";

        /// <summary>
        /// Set by Program.Main from IWebHostEnvironment.ContentRootPath.
        /// Replaces HostingEnvironment.MapPath("~/...") from the WebForms version.
        /// </summary>
        public static string ContentRoot = "";

        public static bool HasConnString
        {
            get { return !string.IsNullOrWhiteSpace(ConnString); }
        }

        static string FilePath
        {
            get
            {
                string root = !string.IsNullOrEmpty(ContentRoot)
                    ? ContentRoot
                    : AppDomain.CurrentDomain.BaseDirectory;

                string appData = Path.Combine(root, "App_Data");
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
