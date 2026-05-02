using System;
using System.Threading.Tasks;
using Demo_ASPNET_Core.engine;
using Demo_ASPNET_Core.engine.handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Demo_ASPNET_Core
{
    /// <summary>
    /// Pageless Architecture entry point — ASP.NET Core edition (sync).
    ///
    /// The same switch-statement-as-routing-table that lived inside
    /// Global.asax.cs in the Web Forms sibling now lives inside a single
    /// terminal middleware. Same routes, same handlers, same pattern.
    ///
    /// No MapGet, no controllers, no MapPageRoute, no attribute scanning.
    /// Add a route by adding a case and a handler class. That's it.
    ///
    /// This edition uses ZERO async/await. Every handler is a plain
    /// `void HandleRequest(HttpContext)`. Form reads, body writes, and
    /// dispatch are all synchronous. AllowSynchronousIO is enabled on
    /// Kestrel and IIS to permit blocking I/O on the request body.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Allow synchronous reads/writes on the request and response
            // bodies. Required because every handler in this app uses
            // ctx.Request.Form (sync) and ctx.Response.Body.Write (sync).
            builder.Services.Configure<KestrelServerOptions>(o =>
            {
                o.AllowSynchronousIO = true;
            });
            builder.Services.Configure<IISServerOptions>(o =>
            {
                o.AllowSynchronousIO = true;
            });

            // Make ContentRootPath available to Config so it can persist
            // /App_Data/mysql_conn.txt without depending on HttpContext.
            Config.ContentRoot = builder.Environment.ContentRootPath;

            // Load persisted MySQL connection string (if setup ran previously).
            Config.Load();

            WebApplication app = builder.Build();

            // -----------------------------------------------------------
            // Static assets (css/js/fonts/images/favicon)
            // -----------------------------------------------------------
            // wwwroot is the ASP.NET Core convention for static files.
            // Folder names (css/, js/, fonts/) are kept identical to the
            // Web Forms version so every URL in the app is unchanged.
            app.UseStaticFiles();

            // -----------------------------------------------------------
            // The dispatch middleware. This is the single point of entry
            // for every URL the app handles. Static files have already
            // had their chance above.
            //
            // Every entry maps a URL to a static handler's HandleRequest().
            // Parameterized routes come first, then the switch.
            //
            // The lambda returns Task.CompletedTask — no async, no await.
            // Handlers are plain `void` methods that write directly to
            // ctx.Response.Body.
            // -----------------------------------------------------------
            app.Run(ctx =>
            {
                string path = (ctx.Request.Path.Value ?? "").ToLowerInvariant().Trim().TrimEnd('/');
                if (path.Length == 0) path = "/";

                // --- Parameterized: /players/edit/{id} and /teams/edit/{id} ---
                if (path.StartsWith("/players/edit/"))
                {
                    PlayerEdit.HandleRequest(ctx);
                    return Task.CompletedTask;
                }

                if (path.StartsWith("/teams/edit/"))
                {
                    TeamEdit.HandleRequest(ctx);
                    return Task.CompletedTask;
                }

                switch (path)
                {
                    // ---- Setup / Dashboard ----
                    case "/":
                    case "/home":
                        Home.HandleRequest(ctx);
                        break;

                    case "/api/setup/save-conn":
                        SetupApi.SaveConnString(ctx);
                        break;

                    case "/api/setup/test-conn":
                        SetupApi.TestConnString(ctx);
                        break;

                    case "/api/setup/create-tables":
                        SetupApi.CreateTables(ctx);
                        break;

                    case "/api/setup/seed":
                        SetupApi.SeedSampleData(ctx);
                        break;

                    case "/api/setup/drop-tables":
                        SetupApi.DropTables(ctx);
                        break;

                    case "/api/setup/clear-conn":
                        SetupApi.ClearConnString(ctx);
                        break;

                    // ---- Players ----
                    case "/players":
                        PlayerList.HandleRequest(ctx);
                        break;

                    case "/players/new":
                        PlayerEdit.HandleRequest(ctx);
                        break;

                    case "/api/players/save":
                        PlayerEditApi.Save(ctx);
                        break;

                    case "/api/players/delete":
                        PlayerEditApi.Delete(ctx);
                        break;

                    // ---- Teams ----
                    case "/teams":
                        TeamList.HandleRequest(ctx);
                        break;

                    case "/teams/new":
                        TeamEdit.HandleRequest(ctx);
                        break;

                    case "/api/teams/save":
                        TeamEditApi.Save(ctx);
                        break;

                    case "/api/teams/delete":
                        TeamEditApi.Delete(ctx);
                        break;

                    // ---- Roster (JOIN + Upsert) ----
                    case "/roster":
                        Roster.HandleRequest(ctx);
                        break;

                    case "/api/roster/list":
                        RosterApi.List(ctx);
                        break;

                    case "/api/roster/assign":
                        RosterApi.Assign(ctx);
                        break;

                    case "/api/roster/delete":
                        RosterApi.Delete(ctx);
                        break;

                    // ---- Tools ----
                    case "/rawsql":
                        RawSql.HandleRequest(ctx);
                        break;

                    case "/api/rawsql/run":
                        RawSqlApi.Run(ctx);
                        break;

                    case "/codegen":
                        CodeGen.HandleRequest(ctx);
                        break;

                    case "/api/codegen/run":
                        CodeGenApi.Run(ctx);
                        break;

                    // Unmatched path — 404.
                    default:
                        ctx.Response.StatusCode = 404;
                        ApiHelper.WriteText(ctx, "Not Found");
                        break;
                }

                return Task.CompletedTask;
            });

            app.Run();
        }
    }
}