using System.Net;

namespace System
{
    /// <summary>
    /// The site layout. Each handler builds its HTML as:
    ///
    ///   sb.Append(SiteTemplate.Header("My Page", "my-page-key"));
    ///   sb.Append(body);
    ///   sb.Append(SiteTemplate.Footer());
    ///
    /// No master pages, no ASPX files, no placeholders.
    /// Just string interpolation.
    /// </summary>
    public static class SiteTemplate
    {
        public static string Header(string pageTitle, string activeNav = "")
        {
            string encodedTitle = WebUtility.HtmlEncode(pageTitle ?? "");

            string navHome     = NavLink("/",          "fa-cog",       "Setup / Dashboard", "home",   activeNav);
            string navPlayers  = NavLink("/players",   "fa-users",     "Player List",       "players",activeNav);
            string navPlayerNew= NavLink("/players/new","fa-user-plus","Add Player",        "player-new", activeNav);
            string navTeams    = NavLink("/teams",     "fa-shield-alt","Team List",         "teams",  activeNav);
            string navTeamNew  = NavLink("/teams/new", "fa-plus-circle","Add Team",         "team-new", activeNav);
            string navRoster   = NavLink("/roster",    "fa-list-ol",   "Roster (Join)",     "roster", activeNav);
            string navRawSql   = NavLink("/rawsql",    "fa-terminal",  "Raw SQL",           "rawsql", activeNav);
            string navCodeGen  = NavLink("/codegen",   "fa-code",      "Code Generation",   "codegen",activeNav);

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>{encodedTitle} - MySqlExpress Demo</title>
    <link rel='stylesheet' href='/fonts/fontawesome/css/all.min.css' />
    <link rel='stylesheet' href='/css/style.css' />
</head>
<body>
    <div class='sidebar-overlay' onclick='toggleSidebar()'></div>
    <div class='app'>
        <aside class='sidebar'>
            <div class='sidebar-brand'>
                <i class='fas fa-database'></i>
                <div class='brand-text'>
                    <strong>MySqlExpress</strong>
                    <span>Pageless ASP.NET Demo</span>
                </div>
            </div>
            <nav class='sidebar-nav'>
                {navHome}
                <div class='nav-divider'>Data</div>
                {navPlayers}
                {navPlayerNew}
                {navTeams}
                {navTeamNew}
                {navRoster}
                <div class='nav-divider'>Tools</div>
                {navRawSql}
                {navCodeGen}
            </nav>
            <div class='sidebar-footer'>
                <a href='https://github.com/adriancs2/MySqlExpress' target='_blank'>
                    <i class='fab fa-github'></i> View on GitHub
                </a>
            </div>
        </aside>
        <div class='main'>
            <div class='topbar'>
                <button class='menu-btn' onclick='toggleSidebar()'><i class='fas fa-bars'></i></button>
                <h1 class='page-title'>{encodedTitle}</h1>
            </div>
            <div class='content'>
";
        }

        public static string Footer()
        {
            int year = DateTime.Now.Year;

            return $@"            </div>
            <div class='site-footer'>
                MySqlExpress Demo &copy; {year} &mdash; Public Domain
            </div>
        </div>
    </div>
    <script src='/js/site.js'></script>
</body>
</html>";
        }

        static string NavLink(string href, string icon, string label, string key, string activeKey)
        {
            string activeAttr = (!string.IsNullOrEmpty(activeKey)
                && string.Equals(key, activeKey, StringComparison.OrdinalIgnoreCase))
                ? " class='active'" : "";

            return $"<a href='{href}'{activeAttr}><i class='fas {icon}'></i><span>{label}</span></a>";
        }

        /// <summary>
        /// Renders a flash-style success banner. Call inline inside the body.
        /// </summary>
        public static string SuccessBanner(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            return $"<div class='flash flash-success'><i class='fas fa-check-circle'></i> {WebUtility.HtmlEncode(message)}</div>";
        }

        public static string ErrorBanner(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            return $"<div class='flash flash-error'><i class='fas fa-exclamation-triangle'></i> {WebUtility.HtmlEncode(message)}</div>";
        }

        public static string InfoBanner(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            return $"<div class='flash flash-info'><i class='fas fa-info-circle'></i> {WebUtility.HtmlEncode(message)}</div>";
        }

        /// <summary>
        /// Standalone page for the "no connection string yet" state.
        /// Used when a handler tries to run before Setup has been completed.
        /// </summary>
        public static string NotConfiguredPage()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Setup Required - MySqlExpress Demo</title>
    <link rel='stylesheet' href='/css/style.css' />
</head>
<body class='centered'>
    <div class='card narrow'>
        <h1>Setup Required</h1>
        <p>The connection string has not been configured yet.</p>
        <p><a class='btn btn-primary' href='/'>Go to Setup</a></p>
    </div>
</body>
</html>";
        }
    }
}
