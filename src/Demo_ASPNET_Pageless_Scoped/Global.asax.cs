using System.Web;
using System.handlers;

namespace System
{
    /// <summary>
    /// Pageless Architecture entry point.
    ///
    /// No <c>MapPageRoute</c> calls. No RouteTable. No .aspx file behind
    /// each URL. Every request is dispatched from a switch statement here.
    ///
    /// Add a new route by adding a case and a handler class. That's it.
    /// The switch statement IS the routing table.
    /// </summary>
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Load persisted MySQL connection string (if setup ran previously).
            Config.Load();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            string path = (Request.Path ?? "").ToLowerInvariant().Trim().TrimEnd('/');
            if (path.Length == 0) path = "/";

            // Let static assets fall through to IIS.
            if (IsStaticAsset(path)) return;

            // Block direct .aspx access from non-local clients — all UI
            // must go through the friendly URLs defined below.
            if (path.EndsWith(".aspx"))
            {
                Response.StatusCode = 404;
                ApiHelper.EndResponse();
                return;
            }

            // ------------------------------------------------------------
            // The routing table.
            // Every entry maps a URL to a static handler's HandleRequest().
            // Parameterized routes (if any) come first, then the switch.
            // ------------------------------------------------------------

            // --- Parameterized: /players/edit/{id} and /teams/edit/{id} ---
            if (path.StartsWith("/players/edit/"))
            {
                PlayerEdit.HandleRequest();
                return;
            }
            if (path.StartsWith("/teams/edit/"))
            {
                TeamEdit.HandleRequest();
                return;
            }

            switch (path)
            {
                // ---- Setup / Dashboard ----
                case "/":
                case "/home":
                    Home.HandleRequest();
                    return;

                case "/api/setup/save-conn":
                    SetupApi.SaveConnString();
                    return;

                case "/api/setup/test-conn":
                    SetupApi.TestConnString();
                    return;

                case "/api/setup/create-tables":
                    SetupApi.CreateTables();
                    return;

                case "/api/setup/seed":
                    SetupApi.SeedSampleData();
                    return;

                case "/api/setup/drop-tables":
                    SetupApi.DropTables();
                    return;

                case "/api/setup/clear-conn":
                    SetupApi.ClearConnString();
                    return;

                // ---- Players ----
                case "/players":
                    PlayerList.HandleRequest();
                    return;


                case "/players/new":
                    PlayerEdit.HandleRequest();
                    return;

                case "/api/players/save":
                    PlayerEditApi.Save();
                    return;

                case "/api/players/delete":
                    PlayerEditApi.Delete();
                    return;

                // ---- Teams ----
                case "/teams":
                    TeamList.HandleRequest();
                    return;


                case "/teams/new":
                    TeamEdit.HandleRequest();
                    return;

                case "/api/teams/save":
                    TeamEditApi.Save();
                    return;

                case "/api/teams/delete":
                    TeamEditApi.Delete();
                    return;

                // ---- Roster (JOIN + Upsert) ----
                case "/roster":
                    Roster.HandleRequest();
                    return;

                case "/api/roster/list":
                    RosterApi.List();
                    return;

                case "/api/roster/assign":
                    RosterApi.Assign();
                    return;

                case "/api/roster/delete":
                    RosterApi.Delete();
                    return;

                // ---- Tools ----
                case "/rawsql":
                    RawSql.HandleRequest();
                    return;

                case "/api/rawsql/run":
                    RawSqlApi.Run();
                    return;

                case "/codegen":
                    CodeGen.HandleRequest();
                    return;

                case "/api/codegen/run":
                    CodeGenApi.Run();
                    return;
            }

            // Unmatched path — let IIS handle it (404 or static file).
        }

        /// <summary>
        /// True for paths we don't want the dispatcher to touch at all.
        /// IIS serves these directly.
        /// </summary>
        static bool IsStaticAsset(string path)
        {
            return path.StartsWith("/css/")
                || path.StartsWith("/js/")
                || path.StartsWith("/fonts/")
                || path.StartsWith("/images/")
                || path == "/favicon.ico";
        }

        protected void Application_Error(object sender, EventArgs e) { }
        protected void Application_End(object sender, EventArgs e) { }
    }
}