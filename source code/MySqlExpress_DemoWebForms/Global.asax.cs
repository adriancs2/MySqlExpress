using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace System
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            RouteFolder("~/pages");
        }

        public static void RouteFolder(string folder)
        {
            string rootFolder = HttpContext.Current.Server.MapPath("~/");

            if (folder.StartsWith("~/"))
            { }
            else if (folder.StartsWith("/"))
            {
                folder = "~" + folder;
            }
            else
            {
                folder = "~/" + folder;
            }

            folder = HttpContext.Current.Server.MapPath(folder);

            MapPageRoute(folder, rootFolder);
        }

        static void MapPageRoute(string folder, string rootFolder)
        {
            // obtain sub-folders
            string[] folders = Directory.GetDirectories(folder);

            foreach (var subFolder in folders)
            {
                MapPageRoute(subFolder, rootFolder);
            }

            string[] files = Directory.GetFiles(folder);

            foreach (var file in files)
            {
                // not a page, skip action
                if (!file.EndsWith(".aspx"))
                    continue;

                string webPath = file.Replace(rootFolder, "~/").Replace("\\", "/");

                var filename = Path.GetFileNameWithoutExtension(file);

                if (filename.ToLower() == "default")
                {
                    continue;
                }

                RouteTable.Routes.MapPageRoute(filename, filename, webPath);
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Server.Transfer("~/pages/Error.aspx", false);
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}