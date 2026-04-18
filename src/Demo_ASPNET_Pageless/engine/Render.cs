using System.Text;
using System.Web;

namespace System
{
    /// <summary>
    /// Shared short helpers for the common render paths every handler needs.
    /// </summary>
    public static class Render
    {
        /// <summary>
        /// Redirects (via 302) to the setup page when a handler is hit
        /// before the connection string has been configured.
        /// </summary>
        public static void NotConfigured()
        {
            HttpContext.Current.Response.Redirect("/", false);
            ApiHelper.EndResponse();
        }

        /// <summary>
        /// Renders a full page with an error banner and nothing else.
        /// Used when a handler can't load its data.
        /// </summary>
        public static void Error(string pageTitle, string activeNav, string message)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header(pageTitle, activeNav));
            sb.Append(SiteTemplate.ErrorBanner(message));
            sb.Append("<div class='card'><p class='muted'>Fix the issue and try again, or <a href='/'>return to Setup</a>.</p></div>");
            sb.Append(SiteTemplate.Footer());

            HttpResponse res = HttpContext.Current.Response;
            res.ContentType = "text/html";
            res.Charset = "utf-8";
            res.Write(sb.ToString());
            ApiHelper.EndResponse();
        }
    }
}
