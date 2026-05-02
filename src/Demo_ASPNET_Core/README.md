# MySqlExpress Demo — Pageless ASP.NET Core (Flat edition)

This is the **ASP.NET Core 8** port of [`Demo_ASPNET_Pageless`](../Demo_ASPNET_Pageless). Same Pageless Architecture, same routes, same handlers, same database, same UI — same demo. Only the host changes.

If you've read the Web Forms sibling, this project will feel almost identical: a switch statement at the top of the app dispatches every URL to a static handler class, every handler builds its HTML with `StringBuilder` and `$@"..."` interpolation, every API endpoint writes its own JSON. No controllers, no Razor Pages, no MVC, no minimal-API attribute scanning.

The point of this port is to demonstrate that **Pageless Architecture is a pattern, not a framework feature**. It works on ASP.NET 4.8 + Web Forms. It works on ASP.NET Core 8 + Kestrel. The `Global.asax.cs` switch statement just becomes an `app.Run(...)` middleware. Everything else carries over.

---

## Quick Start

```
1. Open Demo_ASPNET_Core.csproj in Visual Studio 2022 (or run `dotnet run` from the folder).
2. Make sure you have a local MySQL server running.
3. Press F5 (or `dotnet run`).
4. The browser opens at http://localhost:5000.
5. In the setup page, enter your connection string and click "Save & Continue".
6. Click "Create Tables", then "Seed Sample Data".
7. Click around.
```

The connection string is persisted to `/App_Data/mysql_conn.txt` exactly as in the Web Forms version. If you've already run the Web Forms sibling against a database, the same MySQL data works here unchanged.

### Requirements

- .NET 8 SDK
- MySQL 5.7 or 8.0
- Visual Studio 2022 17.8+ (optional — works fine with `dotnet run`)
- Font Awesome 6 or 7 "Free for Web" — already in `/wwwroot/fonts/fontawesome/`

---

## What changed from the Web Forms version

The Pageless pattern carries over 1:1. The host changes. That's the whole story.

| Web Forms (4.8)                                  | ASP.NET Core 8                                              |
| ------------------------------------------------ | ----------------------------------------------------------- |
| `Global.asax` + `Application_BeginRequest`       | `app.Run(async ctx => { switch (path) { ... } })` in `Program.cs` |
| `HttpContext.Current.Request/Response`           | `HttpContext` passed into each handler                      |
| `HttpContext.Current.Request.Form["x"]`          | `(await ctx.Request.ReadFormAsync())["x"].ToString()`       |
| `HttpContext.Current.Request.QueryString["x"]`   | `ctx.Request.Query["x"].ToString()`                         |
| `Response.Write(html)`                           | `await ctx.Response.WriteAsync(html)`                       |
| `ApiHelper.EndResponse()`                        | Just `return` — middleware short-circuits naturally         |
| `void HandleRequest()`                           | `async Task HandleRequest(HttpContext ctx)`                 |
| `HostingEnvironment.MapPath("~/App_Data")`       | `IWebHostEnvironment.ContentRootPath` (cached on `Config`)  |
| `Newtonsoft.Json`                                | `System.Text.Json` (built into the BCL)                     |
| Static files served by IIS                       | `app.UseStaticFiles()` against `wwwroot/`                   |
| `Web.config`                                     | `appsettings.json` + `Program.cs`                           |
| Verbose `.csproj` listing every file             | SDK-style `.csproj` that auto-includes everything           |
| `namespace System` (legacy demo convention)      | `namespace Demo_ASPNET_Core.engine` (proper rooted namespace) |

What did **not** change:

- The routing table — same URLs, same order, same parameterized prefixes.
- The handlers — same logic, same SQL, same HTML, same inline JS.
- The models — `obPlayer`, `obTeam`, `obRosterRow` are byte-identical.
- The `MySqlExpress.cs` library file — verbatim copy, only the namespace changes.
- The CSS, JS, and fonts — copied unchanged into `wwwroot/`.
- `Schema.cs` — DDL constants are byte-identical.
- The two-error-channel pattern — `success: false` for business errors, `try/catch` on the client for transport errors.
- The "no service layer, no repository layer" stance.

---

## The dispatch middleware

This is the heart of the port. The Web Forms sibling's switch statement lived inside `Application_BeginRequest`. Here it lives inside a single terminal middleware in `Program.cs`:

```csharp
app.UseStaticFiles();

app.Run(async ctx =>
{
    string path = (ctx.Request.Path.Value ?? "").ToLowerInvariant().Trim().TrimEnd('/');
    if (path.Length == 0) path = "/";

    // --- Parameterized: /players/edit/{id} and /teams/edit/{id} ---
    if (path.StartsWith("/players/edit/")) { await PlayerEdit.HandleRequest(ctx); return; }
    if (path.StartsWith("/teams/edit/"))   { await TeamEdit.HandleRequest(ctx);   return; }

    switch (path)
    {
        case "/":
        case "/home":           await Home.HandleRequest(ctx); return;
        case "/players":        await PlayerList.HandleRequest(ctx); return;
        case "/api/players/save": await PlayerEditApi.Save(ctx); return;
        // ... every other route ...
    }

    ctx.Response.StatusCode = 404;
    await ctx.Response.WriteAsync("Not Found");
});
```

**The switch statement IS the routing table.** Same as the Web Forms version. Add a route by adding a `case` and pointing it at a static handler method. No attribute scanning, no convention discovery, no `MapGet` proliferation, no controllers.

---

## Project layout

```
Demo_ASPNET_Core/
├── App_Data/                     (connection string is saved here at runtime)
├── Demo_ASPNET_Core.csproj       (SDK-style — references MySqlConnector only)
├── Program.cs                    (the routing table)
├── appsettings.json
├── Properties/launchSettings.json
│
├── wwwroot/                      (ASP.NET Core static-files convention)
│   ├── css/
│   │   └── style.css             (self-contained stylesheet, no framework)
│   ├── js/
│   │   └── site.js               (escapeHtml, flash, sidebar toggle)
│   └── fonts/
│       └── fontawesome/
│
└── engine/
    ├── ApiHelper.cs              (JSON helpers — System.Text.Json)
    ├── Config.cs                 (connection string persistence)
    ├── Render.cs                 (shared "not configured" / error page)
    ├── Schema.cs                 (CREATE TABLE SQL + seed data, as C# constants)
    ├── SiteTemplate.cs           (Header / Footer / nav links)
    ├── MySqlExpress.cs           (the library, verbatim from upstream)
    │
    ├── models/
    │   ├── obPlayer.cs
    │   ├── obTeam.cs
    │   └── obRosterRow.cs
    │
    └── handlers/
        ├── Home.cs               + SetupApi.cs
        ├── PlayerList.cs
        ├── PlayerEdit.cs         + PlayerEditApi.cs
        ├── TeamList.cs
        ├── TeamEdit.cs           + TeamEditApi.cs
        ├── Roster.cs             + RosterApi.cs
        ├── RawSql.cs             + RawSqlApi.cs
        └── CodeGen.cs            + CodeGenApi.cs
```

Compare to the Web Forms layout in [`../Demo_ASPNET_Pageless/README.md`](../Demo_ASPNET_Pageless/README.md) — they are essentially identical. The only structural change is `wwwroot/` for the static assets, which is the ASP.NET Core convention.

---

## License

Public Domain. No attribution required.

---

## Related

- **Web Forms sibling:** [`../Demo_ASPNET_Pageless`](../Demo_ASPNET_Pageless) — the same app on .NET Framework 4.8 + Web Forms.
- **Main library:** [github.com/adriancs2/MySqlExpress](https://github.com/adriancs2/MySqlExpress)
- **SQLite sibling:** [github.com/adriancs2/SQLiteExpress](https://github.com/adriancs2/SQLiteExpress)
