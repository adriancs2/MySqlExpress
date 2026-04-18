# MySqlExpress Demo — Pageless ASP.NET Web Forms

A small web app that exercises every feature of [MySqlExpress](https://github.com/adriancs2/MySqlExpress) against real MySQL tables, built in the **Pageless** ASP.NET Web Forms style — no `.aspx` per page, no master pages, no session state, no reflection-based routing. A switch statement in `Global.asax.cs` dispatches every URL to a static handler class. Each handler composes its HTML with `StringBuilder` and `$@"..."` interpolation.

The point of this demo is two-fold:

1. **Show MySqlExpress working.** Every method from the library's README shows up here in realistic UI code — `Insert`, `GetObject<T>`, `GetObjectList<T>`, `InsertUpdate`, `Update`, `Save`, `Execute`, `Select`, `ExecuteScalar<T>`, `GenerateContainsString`, the full `Generate*` family.
2. **Show the Pageless pattern.** If you've never worked without .aspx files per page, the codebase is small enough (~20 files) that you can read all of it in an afternoon and see how the pieces fit.

---

## Quick Start

```
1. Clone the repo.
2. Open Demo_ASPNET_Pageless.csproj in Visual Studio (2019 or newer).
3. Make sure you have a local MySQL server running.
4. Press F5.
5. In the setup page, enter your connection string and click "Save & Continue".
6. Click "Create Tables", then "Seed Sample Data".
7. Click around.
```

Nothing is persisted except the connection string itself, which goes to `/App_Data/mysql_conn.txt` the first time you save it.

### Requirements

- .NET Framework 4.8
- MySQL 5.7 or 8.0 (earlier may work; not tested)
- IIS Express (bundled with Visual Studio) or IIS
- Font Awesome 6 or 7 "Free for Web", dropped into `/fonts/fontawesome/` — see that folder's own README for the one-minute install

---

## What each page demonstrates

Every feature from the main MySqlExpress README is wired into a working UI somewhere in this demo. This table maps feature to handler, so you can jump straight to the source.

| MySqlExpress feature | Where to see it | Source file |
| --- | --- | --- |
| Connection bootstrap (`new MySqlExpress(cmd)`) | all handlers (via `Db.Run` / `Db.Get`) | `engine/Db.cs` |
| `m.Execute(createSql)` | Setup → "Create Tables" | `engine/handlers/SetupApi.cs` |
| `m.Insert(table, dic)` + `m.LastInsertId` | Setup → "Seed" • Add Player • Add Team | `SetupApi.cs` • `PlayerEditApi.cs` • `TeamEditApi.cs` |
| `m.InsertUpdate(table, dic, cols)` (upsert) | Roster → "Assign / Update" | `RosterApi.cs` |
| `m.Update(table, data, "id", id)` (+ `LIMIT 1` default) | Edit Player save • Edit Team save | `PlayerEditApi.cs` • `TeamEditApi.cs` |
| `m.GetObject<T>(sql, params)` | Edit Player / Edit Team page load | `PlayerEdit.cs` • `TeamEdit.cs` |
| `m.GetObjectList<T>(sql)` | Player List • Team List | `PlayerList.cs` • `TeamList.cs` |
| `m.GetObjectList<T>(sql)` with JOIN & alias projection | Roster view (3-way join) | `Roster.cs` |
| `m.ExecuteScalar<int>(sql)` | Dashboard count badges | `Home.cs` |
| `m.Execute(sql, params)` (DELETE) | Delete Player • Delete Team • Remove Roster entry | `PlayerEditApi.cs` • `TeamEditApi.cs` • `RosterApi.cs` |
| `m.Select(sql)` (raw SQL → DataTable) | Raw SQL tool (for SELECT/SHOW/DESCRIBE) | `RawSqlApi.cs` |
| `m.GenerateContainsString(...)` | Player List search box (multi-word) | `PlayerList.cs` |
| `m.GetTableList()` | Setup dashboard • Code Generation dropdown | `Home.cs` • `CodeGen.cs` |
| `m.GenerateTableClassFields(...)` | Code Generation → "Class Fields" | `CodeGenApi.cs` |
| `m.GenerateCustomClassField(...)` | Code Generation → "Generate from SELECT" | `CodeGenApi.cs` |
| `m.GenerateTableDictionaryEntries(...)` | Code Generation → "Dictionary" | `CodeGenApi.cs` |
| `m.GenerateParameterDictionaryTable(...)` | Code Generation → "Parameter Dictionary" | `CodeGenApi.cs` |
| `m.GenerateUpdateColumnList(...)` | Code Generation → "Update Column List" | `CodeGenApi.cs` |
| `m.GetCreateTableSql(...)` | Code Generation → "CREATE TABLE SQL" | `CodeGenApi.cs` |
| `StartTransaction` / `Commit` / `Rollback` | every multi-write action in the app | passim |

There is no separate "transaction demo" page, on purpose. Transactions appear wherever multiple writes happen, just as you'd write production code. Read `SetupApi.SeedSampleData` or `PlayerEditApi.Delete` for a representative example.

---

## The Pageless pattern

The short version is: there are no `.aspx` pages driving the URLs. `Global.asax.cs` intercepts `Application_BeginRequest` and dispatches directly.

```csharp
protected void Application_BeginRequest(object sender, EventArgs e)
{
    string path = (Request.Path ?? "").ToLowerInvariant().Trim().TrimEnd('/');
    if (path.Length == 0) path = "/";

    if (IsStaticAsset(path)) return;   // let IIS handle CSS/JS/fonts

    // Parameterized routes first
    if (path.StartsWith("/players/edit/")) { PlayerEdit.HandleRequest(); return; }
    if (path.StartsWith("/teams/edit/"))   { TeamEdit.HandleRequest();   return; }

    switch (path)
    {
        case "/":
        case "/home":            Home.HandleRequest();            return;
        case "/players":         PlayerList.HandleRequest();      return;
        case "/api/players/save":PlayerEditApi.Save();            return;
        // ...
    }
}
```

**The switch statement IS the routing table.** Add a route by adding a `case` and pointing it at a static handler method. That's the entire extension mechanism. No attribute scanning, no assembly loading, no convention discovery.

### Handler shape

Every UI handler has the same shape:

```csharp
public static class PlayerList
{
    public static void HandleRequest()
    {
        if (!Config.HasConnString) { Render.NotConfigured(); return; }

        List<obPlayer> rows = Db.Get(m => m.GetObjectList<obPlayer>(
            "select * from player order by id desc;"));

        var sb = new StringBuilder();
        sb.Append(SiteTemplate.Header("Player List", "players"));
        sb.Append($@"<div class='card'>... {WebUtility.HtmlEncode(row.Name)} ...</div>");
        sb.Append(SiteTemplate.Footer());

        var res = HttpContext.Current.Response;
        res.ContentType = "text/html";
        res.Write(sb.ToString());
        ApiHelper.EndResponse();
    }
}
```

One static class per page. `SiteTemplate.Header()` / `Footer()` is the layout. `$@"..."` is the view engine. If you've been doing Web Forms with code-behind files for years, it feels spartan at first — and then strangely pleasant. Every page is one file, top to bottom, no markup/code split, no designer file drifting out of sync.

### JSON endpoints follow the same shape

API endpoints are just handlers that write JSON instead of HTML. They live in the same `engine/handlers/` folder, suffixed `...Api`:

```csharp
public static void Save()
{
    var data = new Dictionary<string, object>
    {
        ["name"] = Req.Form["name"],
        // ...
    };
    Db.Run(m =>
    {
        m.StartTransaction();
        m.Insert("player", data);
        m.Commit();
    });
    ApiHelper.WriteSuccess("Saved.");
    ApiHelper.EndResponse();
}
```

The browser-side JS (`js/site.js`) has a `window.apiPostForm(url, payload, onSuccess)` helper that posts form-encoded bodies and expects JSON back. No framework, no build step.

### Why no session state?

ASP.NET's built-in session state doesn't interact well with Pageless routing — requests that skip the page lifecycle often bypass session acquisition, leading to intermittent null-ish behaviour. For this demo there's no state to track anyway. When a real app needs cross-request state, the Pageless convention is to use a static `Dictionary` or a proper cache (e.g. a JWT cookie for identity, a `MemoryCache` for per-user server state, or just a table in MySQL).

---

## Project layout

```
Demo_ASPNET_Pageless/
├── App_Data/                     (connection string is saved here at runtime)
├── Default.aspx                  (empty placeholder so IIS finds "/")
├── Global.asax + Global.asax.cs  (the routing table)
├── MySqlExpress.cs               (the library itself, single file)
├── Web.config
│
├── css/
│   └── style.css                 (self-contained stylesheet, no framework)
├── js/
│   └── site.js                   (apiPostForm, escapeHtml, sidebar toggle)
├── fonts/
│   └── fontawesome/              (drop Font Awesome here — see that README)
│
└── engine/
    ├── ApiHelper.cs              (JSON + EndResponse helpers)
    ├── Config.cs                 (connection string persistence)
    ├── Db.cs                     (Db.Run / Db.Get using-block wrapper)
    ├── Render.cs                 (shared "not configured" / error page)
    ├── Schema.cs                 (CREATE TABLE SQL + seed data, as C# constants)
    ├── SiteTemplate.cs           (Header / Footer / nav links)
    │
    ├── models/
    │   ├── obPlayer.cs           (POCOs for table rows)
    │   ├── obTeam.cs
    │   └── obRosterRow.cs        (custom JOIN projection)
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

Three layers, three responsibilities:

- **`engine/` (not `handlers/`)** — shared infrastructure. No page-specific code.
- **`engine/models/`** — POCOs. Field and property names match MySQL column names.
- **`engine/handlers/`** — one static class per route. Each handler holds its SQL, its HTML, and its JS inline.

There is deliberately no "service layer" or "repository layer". For a demo of this size, the abstraction cost exceeds the benefit. In a larger project you'd extract reusable queries into their own classes; the pattern generalises cleanly.

---

## The database

Three tables. Schema lives in `engine/Schema.cs` as C# string constants — the setup page runs `m.Execute(sql)` on each one.

```
player           (id PK auto_increment, code, name, date_register, tel, email, status)
team             (id PK auto_increment, code, name, city, status)
player_team      (year + player_id composite PK, team_id, score, level, status)
```

`player_team` is the interesting one. Its composite primary key `(year, player_id)` is exactly the shape `m.InsertUpdate(...)` is designed for — "for year 2024, assign this player to this team, or update their stats if they're already on the roster". One call, either outcome.

---

## Tinkering

**Raw SQL tab** lets you type any SQL and run it. Read-ish statements (`SELECT`, `SHOW`, `DESCRIBE`, `EXPLAIN`, `WITH`) route to `m.Select(sql)` and render as a table; everything else routes to `m.Execute(sql)` wrapped in a transaction. Useful for poking at the seed data, trying joins, or verifying that an `UPDATE` did what you expected.

**Code Generation tab** is the live demo of `GenerateTableClassFields`, `GenerateTableDictionaryEntries`, `GenerateParameterDictionaryTable`, `GenerateUpdateColumnList`, `GetCreateTableSql`, and `GenerateCustomClassField`. Pick a table (or paste a custom SELECT), pick an output style, get paste-ready C#. This is the same codepath the standalone MySqlExpress Helper desktop app uses — except here it's served from the library itself, inside your own web process, against your own live database.

---

## Extending the demo

Add a new page in three steps:

1. Create `engine/handlers/MyPage.cs` with a `public static void HandleRequest()`.
2. Add the file under `<Compile Include="..." />` in the `.csproj`.
3. Add a `case "/mypage": MyPage.HandleRequest(); return;` to `Global.asax.cs`.

If the page needs a JSON endpoint, create `MyPageApi.cs` next to it and route `/api/mypage/something` to the appropriate method. That's the whole extension model.

---

## License

Public Domain. No attribution required. Use it, fork it, rebrand it, ship it.

---

## Related

- **Main library:** [github.com/adriancs2/MySqlExpress](https://github.com/adriancs2/MySqlExpress)
- **SQLite sibling:** [github.com/adriancs2/SQLiteExpress](https://github.com/adriancs2/SQLiteExpress) — same API, swap the connection type.
