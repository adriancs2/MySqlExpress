using System.Text;

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
        public static void NotConfigured(Ctx ctx)
        {
            ctx.Redirect("/");
        }

        /// <summary>
        /// Renders a full page with an error banner and nothing else.
        /// Used when a handler can't load its data.
        /// </summary>
        public static void Error(Ctx ctx, string pageTitle, string activeNav, string message)
        {
            StringBuilder sb = ctx.Out;
            sb.Clear();
            sb.Append(SiteTemplate.Header(pageTitle, activeNav));
            sb.Append(SiteTemplate.ErrorBanner(message));
            sb.Append("<div class='card'><p class='muted'>Fix the issue and try again, or <a href='/'>return to Setup</a>.</p></div>");
            sb.Append(SiteTemplate.Footer());

            ctx.ContentType = "text/html; charset=utf-8";
        }
    }
}
