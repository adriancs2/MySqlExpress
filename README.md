# MySqlExpress

[![C#](https://img.shields.io/badge/C%23-7.3%2B-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6%2B-512BD4?logo=.net&logoColor=white)](https://dotnet.microsoft.com/download/dotnet-framework)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0%2B-512BD4?logo=.net&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/standard/net-standard)
[![.NET](https://img.shields.io/badge/.NET-6%20%7C%208%20%7C%209%2B-512BD4?logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![MySQL](https://img.shields.io/badge/MySQL-5.7%2B%20%7C%208.0%2B-4479A1?logo=mysql&logoColor=white)](https://www.mysql.com/)
[![NuGet](https://img.shields.io/nuget/v/MySqlExpress?logo=nuget)](https://www.nuget.org/packages/MySqlExpress)
[![Single-File](https://img.shields.io/badge/deploy-single--file-brightgreen)](#install)
[![License](https://img.shields.io/badge/license-Public%20Domain-lightgrey)](#license)

A lightweight C# class library for MySQL — designed for developers who want to stay close to SQL while removing the tedium around it.

MySqlExpress has a sibling library for SQLite, [SQLiteExpress](https://github.com/adriancs2/SQLiteExpress), which mirrors this API — giving you a consistent database interface across both MySQL and SQLite projects.

---

## Contents

**Part 1 — The Library**

Getting Started

- [The Design](#the-design)
- [Install](#install)
- [Quick Start](#quick-start)
- [A note on Dictionary syntax](#a-note-on-dictionary-syntax)

MySqlExpress Highlights

- [Insert](#insert)
- [GetObject / GetObjectList](#getobject--getobjectlist)
- [InsertUpdate (Upsert)](#insertupdate-upsert)
- [Update](#update)
- [Save / SaveList](#save--savelist)

More API

- [Transactions (Start / Commit / Rollback)](#transactions-start--commit--rollback)
- [Select](#select)
- [ExecuteScalar](#executescalar)
- [Execute (any SQL)](#execute-any-sql)
- [Select (any SQL)](#select-any-sql)

Reference

- [Class Field Binding Modes](#class-field-binding-modes)
- [String Helpers](#string-helpers)
- [DB Info](#db-info)
- [Supported Data Types](#supported-data-types)

**Part 2 — MySqlExpress Helper App**

- [What the Helper does](#what-the-helper-does)
- [Download](#download-the-helper)
- [Generating class fields](#generating-class-fields)
- [Generating dictionary entries](#generating-dictionary-entries)
- [Generating the update column list](#generating-the-update-column-list)

**Part 3 — Extras**

- [Visual Studio toolbox tip](#visual-studio-toolbox-tip)

**About**

- [Relationship with SQLiteExpress](#relationship-with-sqliteexpress)
- [License](#license)

---

# Part 1 — The Library

## The Design

No ORM. No migrations. No DbContext. No LINQ-to-SQL translation layer.

You write SQL. MySqlExpress handles the plumbing: parameterization, object binding, type conversion, CRUD generation. So you don't have to repeat yourself.

The idea is simple: wrap a raw `MySqlCommand` and give it superpowers.

```csharp
using MySqlConnector;

using (MySqlConnection conn = new MySqlConnection(connString))
{
    conn.Open();
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;

        MySqlExpress m = new MySqlExpress(cmd);

        // You're ready. That's it.
    }
}
```

> Throughout this README, `m` is the `MySqlExpress` instance.

---

## Install

MySqlExpress ships as a single `.cs` file, distributed through NuGet. There are two packages — pick the MySQL connector you prefer:

| Connector                   | NuGet Package                                                                                    | License |
| --------------------------- | ------------------------------------------------------------------------------------------------ | ------- |
| **MySqlConnector** (default) | [MySqlExpress](https://www.nuget.org/packages/MySqlExpress)                                      | MIT     |
| **MySql.Data** (Oracle)      | [MySqlExpress.MySql.Data](https://www.nuget.org/packages/MySqlExpress.MySql.Data)                | GPL / Oracle |

```
PM> NuGet\Install-Package MySqlExpress
```

or for the Oracle connector:

```
PM> NuGet\Install-Package MySqlExpress.MySql.Data
```

Prefer to drop the source in directly? Grab `MySqlExpress.cs` from the repo — it lives in the `System` namespace, so any file that already has `using System;` picks it up automatically.

---

## Quick Start

```csharp
using System;
using System.Collections.Generic;
using MySqlConnector;

string connStr = "server=localhost;user=root;pwd=1234;database=test;";

using (MySqlConnection conn = new MySqlConnection(connStr))
{
    conn.Open();
    using (MySqlCommand cmd = new MySqlCommand())
    {
        cmd.Connection = conn;

        MySqlExpress m = new MySqlExpress(cmd);

        // Insert a row.
        m.Insert("player", new Dictionary<string, object>
        {
            ["name"]  = "John Smith",
            ["score"] = 99.5m,
        });

        int newId = m.LastInsertId;

        // Read it back.
        int count = m.ExecuteScalar<int>("select count(*) from player;");
        Console.WriteLine($"Rows: {count}, last id: {newId}");
    }
}
```

---

## A note on Dictionary syntax

Throughout this README, most examples build dictionaries with the indexer-initializer form (`["key"] = value`). C# gives you a few equivalent ways to write the same thing — use whichever your fingers prefer:

```csharp
// Style 1 — indexer initializer (used in this README)
var dic = new Dictionary<string, object>
{
    ["id"]    = 1,
    ["name"]  = "John",
    ["score"] = 100,
};

// Style 2 — collection initializer
var dic = new Dictionary<string, object>
{
    { "id", 1 },
    { "name", "John" },
    { "score", 100 },
};

// Style 3 — plain assignment
var dic = new Dictionary<string, object>();
dic["id"]    = 1;
dic["name"]  = "John";
dic["score"] = 100;
```

All three produce the same `Dictionary<string, object>`. MySqlExpress doesn't care which you pick.

---

## Insert

Dictionary-based. Pass a table name and a `Dictionary<string, object>` of column → value. MySqlExpress builds the parameterized `INSERT`, handles type conversion, and you're done.

```csharp
Dictionary<string, object> dic = new Dictionary<string, object>
{
    ["code"]          = "P001",
    ["name"]          = "John Smith",
    ["date_register"] = DateTime.Now,
    ["tel"]           = "0123456789",
    ["email"]         = "john@mail.com",
    ["status"]        = 1,
};

m.Insert("player", dic);

int newId  = m.LastInsertId;      // int
long newIdLong = m.LastInsertIdLong; // long, for BIGINT primary keys
```

---

## GetObject / GetObjectList

Bind a single row to an object, or a result set straight into a `List<T>`. Column names are matched against both fields and properties — no attributes, no ceremony.

```csharp
// Single row
obPlayer p = m.GetObject<obPlayer>("select * from player where id = 1;");

// With parameters
obPlayer p2 = m.GetObject<obPlayer>(
    "select * from player where id = @vid;",
    new Dictionary<string, object> { ["@vid"] = 1 });

// Into an existing instance
obPlayer p3 = new obPlayer();
m.GetObject("select * from player where id = 1;", p3);

// List
List<obPlayer> lst = m.GetObjectList<obPlayer>("select * from player;");

// List with LIKE filter
List<obPlayer> matches = m.GetObjectList<obPlayer>(
    "select * from player where name like @vname;",
    new Dictionary<string, object> { ["@vname"] = "%adam%" });
```

It also works cleanly with multi-table joins — project any SELECT shape into a custom POCO:

```csharp
public class obPlayerTeam
{
    public int id { get; set; }
    public string name { get; set; }
    public int year { get; set; }
    public string teamname { get; set; }
    public string teamcode { get; set; }
    public int teamid { get; set; }
}

List<obPlayerTeam> lst = m.GetObjectList<obPlayerTeam>(@"
    select a.id, a.name, b.year,
           c.name as teamname, c.code as teamcode, c.id as teamid
    from player a
    inner join player_team b on a.id = b.player_id
    inner join team c on b.team_id = c.id
    where a.name like @vname;",
    new Dictionary<string, object> { ["@vname"] = "%adam%" });
```

See [Class Field Binding Modes](#class-field-binding-modes) for the supported POCO styles.

---

## InsertUpdate (Upsert)

Insert a row — and if the primary key already exists, update **only specific columns**. Uses MySQL's `INSERT ... ON DUPLICATE KEY UPDATE`.

```csharp
List<string> lstUpdateCol = new List<string> { "score", "level", "status" };

Dictionary<string, object> dic = new Dictionary<string, object>
{
    ["year"]      = 2024,
    ["player_id"] = 1,
    ["score"]     = 99.5m,
    ["level"]     = 5,
    ["status"]    = 1,
};

m.InsertUpdate("player_team", dic, lstUpdateCol);
```

Include / exclude mode:

```csharp
List<string> lstCols = new List<string> { "score", "level" };

// include = true: update ONLY score and level on conflict
m.InsertUpdate("player_team", dic, lstCols, include: true);

// include = false: update everything EXCEPT score and level
m.InsertUpdate("player_team", dic, lstCols, include: false);
```

Especially useful for tables with composite primary keys and no auto-increment — the typical "upsert this year's row for this player" pattern.

---

## Update

The default `Update` overloads append `LIMIT 1` for safety — the most common bug in hand-written SQL is an `UPDATE` without a proper `WHERE`, and this catches it. Pass `updateSingleRow: false` when you genuinely want to update multiple rows.

```csharp
// 1) Single-column condition (updates one matching row)
Dictionary<string, object> data = new Dictionary<string, object>
{
    ["name"] = "John Smith Updated",
    ["tel"]  = "0999888777",
};
m.Update("player", data, "id", 1);

// 2) Same, but update every matching row
m.Update("player", data, "status", 1, updateSingleRow: false);

// 3) Multi-column condition
Dictionary<string, object> cond = new Dictionary<string, object>
{
    ["status"] = 1,
    ["tel"]    = "0123456789",
};
m.Update("player", data, cond);

// 4) Multi-column condition, no LIMIT 1
m.Update("player", data, cond, updateSingleRow: false);
```

---

## Save / SaveList

Reflection-based wrappers that map a class object to a `Dictionary<string, object>` and call `InsertUpdate` with all non-primary-key columns. Field and property names must match the column names.

```csharp
obPlayer player = new obPlayer();
player.code = "P001";
player.name = "John Smith";

m.Save("player", player);

// Bulk
List<obPlayer> lst = new List<obPlayer> { player1, player2, player3 };
m.SaveList("player", lst);
```

> `Save` / `SaveList` use `INSERT ... ON DUPLICATE KEY UPDATE` semantics — insert if the PK is new, update all non-PK columns if it already exists. For fine-grained control over which columns update, use [`InsertUpdate`](#insertupdate-upsert) directly.

---

## Transactions (Start / Commit / Rollback)

If you're doing more than one write, wrap it in a transaction. Two big reasons:

**1. Data safety.** Either everything succeeds, or nothing does. If the second insert throws, the first one won't be left stranded in the database. Your tables stay in a consistent state.

**2. Speed.** MySQL commits to disk at the end of every statement by default. On a 7200 rpm HDD, that caps you at roughly 100–120 write operations per second. Inside a transaction, MySQL batches the commits into a single disk flush at the end. Bulk inserts commonly run **10×–100× faster** inside a transaction.

```csharp
try
{
    m.StartTransaction();

    m.Insert("player", dic1);
    m.Insert("player", dic2);
    m.Update("player", data, "id", 1);

    m.Commit();
}
catch
{
    m.Rollback();
    throw;
}
```

**Without a transaction:** each `Insert` / `Update` / `Execute` is its own auto-committed unit. A crash halfway through leaves partial data. Bulk operations are slow.

**With a transaction:** the whole block behaves as one atomic operation. `Commit` makes all changes permanent; `Rollback` discards them. Always pair `StartTransaction` with a `try / catch` that calls `Rollback` on failure.

> Rule of thumb: any time you write more than one row — or any time a write depends on a previous write — use a transaction.

---

## Select

`Select` returns a `DataTable`.

```csharp
// All rows
DataTable dt = m.Select("select * from player;");

// With a dictionary of parameters
DataTable dt2 = m.Select(
    "select * from player where id = @vid;",
    new Dictionary<string, object> { ["@vid"] = 1 });

// With an explicit list of MySqlParameter
List<MySqlParameter> plist = new List<MySqlParameter>
{
    new MySqlParameter("@name", "John"),
};
DataTable dt3 = m.Select("select * from player where name = @name;", plist);
```

---

## ExecuteScalar

Returns a single value. The generic form converts for you.

```csharp
int count       = m.ExecuteScalar<int>("select count(*) from player;");
string name     = m.ExecuteScalar<string>("select name from player where id = 1;");
decimal total   = m.ExecuteScalar<decimal>("select sum(score) from player;");
DateTime when   = m.ExecuteScalar<DateTime>("select date_register from player where id = 1;");

// Non-generic returns object
object raw = m.ExecuteScalar("select count(*) from player;");

// With parameters
long age = m.ExecuteScalar<long>(
    "select age from player where name = @n;",
    new Dictionary<string, object> { ["@n"] = "alice" });
```

---

## Execute (any SQL)

`Execute` runs any non-query statement — DDL, hand-written `INSERT/UPDATE/DELETE`, set statements, anything that doesn't return rows.

```csharp
m.Execute("create index idx_player_name on player(name);");

m.Execute(
    "delete from player where id = @vid;",
    new Dictionary<string, object> { ["@vid"] = 5 });

m.Execute(
    "delete from player where name = @vname or code = @vcode;",
    new Dictionary<string, object>
    {
        ["@vname"] = "James O'Brien",
        ["@vcode"] = "P001",
    });
```

---

## Select (any SQL)

`Select` is the escape hatch for anything — joins, CTEs, subqueries, window functions, anything that returns rows. You write the SQL, MySqlExpress parameterizes it and hands you a `DataTable`.

```csharp
DataTable dt = m.Select(@"
    select a.id, a.name, b.year, b.score
    from player a
    inner join player_team b on a.id = b.player_id
    where b.year = @year
    order by b.score desc;",
    new Dictionary<string, object> { ["@year"] = 2024 });
```

Pair it with [`GetObjectList<T>`](#getobject--getobjectlist) when you'd rather have strongly-typed objects than a `DataTable`.

---

## Class Field Binding Modes

MySqlExpress supports three mapping styles for POCOs. Pick whichever suits your codebase.

**Mode 1 — Private fields + public properties** *(recommended)*

Private field names match column names exactly (MySQL's `snake_case`). Public properties follow C# conventions (`PascalCase`). MySqlExpress binds to the private fields; your application code uses the clean property names.

```csharp
public class obPlayer
{
    int id = 0;
    string code = "";
    string name = "";
    DateTime date_register = DateTime.MinValue;
    string tel = "";
    string email = "";
    int status = 0;

    public int Id { get { return id; } set { id = value; } }
    public string Code { get { return code; } set { code = value; } }
    public string Name { get { return name; } set { name = value; } }
    public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
    public string Tel { get { return tel; } set { tel = value; } }
    public string Email { get { return email; } set { email = value; } }
    public int Status { get { return status; } set { status = value; } }
}
```

This gives you the best of both worlds: the private fields bridge MySQL's `snake_case` naming to .NET, while the public API stays idiomatic C#.

**Mode 2 — Public properties only**

Property names must match column names exactly.

```csharp
public class obPlayer
{
    public int id { get; set; }
    public string code { get; set; }
    public string name { get; set; }
    public DateTime date_register { get; set; }
    public string tel { get; set; }
    public string email { get; set; }
    public int status { get; set; }
}
```

**Mode 3 — Public fields only**

Field names must match column names exactly.

```csharp
public class obPlayer
{
    public int id = 0;
    public string code = "";
    public string name = "";
    public DateTime date_register = DateTime.MinValue;
    public string tel = "";
    public string email = "";
    public int status = 0;
}
```

> A note on the `ob` prefix: naming classes `obPlayer`, `obPlayerTeam`, etc. is a personal convention — `ob` for "object of". Use whatever prefix (or no prefix) you prefer; MySqlExpress doesn't care about class names, only field/property names.

---

## String Helpers

### Escape

Escapes single quotes and backslashes so a string is safe to inline into SQL.

```csharp
string safe = m.Escape("Jane O'Brien"); // "Jane O''Brien"
```

### GetLikeString

Wraps each whitespace-separated token with `%`.

```csharp
string like    = m.GetLikeString("John Smith");           // "%John%Smith%"
string likeEsc = m.GetLikeString("Jane O'Brien", true);   // "%Jane%O''Brien%"

// Typical use
List<obPlayer> lst = m.GetObjectList<obPlayer>(
    "select * from player where name like @vname;",
    new Dictionary<string, object> { ["@vname"] = m.GetLikeString("James O'Brien") });
```

### GenerateContainsString

Builds a parameterized multi-word `LIKE` condition and appends it to a `StringBuilder`.

```csharp
StringBuilder sb = new StringBuilder();
sb.Append("select * from player where 1=1");

Dictionary<string, object> dicParam = new Dictionary<string, object>();
m.GenerateContainsString("name", "john smith", sb, dicParam);

// sb:
//   select * from player where 1=1 and (`name` like @csname0 and `name` like @csname1)
// dicParam:
//   { "@csname0": "%john%", "@csname1": "%smith%" }

List<obPlayer> results = m.GetObjectList<obPlayer>(sb.ToString(), dicParam);
```

---

## DB Info

```csharp
List<string> tables = m.GetTableList();                 // show tables;
string createSql    = m.GetCreateTableSql("player");    // show create table `player`;
```

---

## Supported Data Types

MySqlExpress handles automatic conversion for:

`string`, `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `char`, `DateTime`, `byte[]`, `Guid`, `TimeSpan`

`null` and `DBNull` values convert to the type's default (`""`, `0`, `false`, `DateTime.MinValue`, etc.), so you never get a `NullReferenceException` reading a nullable column into a non-nullable field.

---

# Part 2 — MySqlExpress Helper App

## What the Helper does

MySqlExpress Helper is a small Windows desktop app that connects to your MySQL database and generates C# boilerplate you'd otherwise type by hand: class field definitions, dictionary entries for `Insert` and `Update`, and update column lists for `InsertUpdate`.

It's **entirely optional** — everything the Helper generates, you can write yourself. The library doesn't depend on it. But for bootstrapping a new table into your codebase, copy-pasting from the Helper is faster than typing out 20 properties.

Demo:

![MySqlExpress Demo](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/screenshot05.png)

## Download the Helper

Get the latest release from the GitHub releases page:

**[https://github.com/adriancs2/MySqlExpress/releases](https://github.com/adriancs2/MySqlExpress/releases)**

## Generating class fields

Three output modes, matching the three [Class Field Binding Modes](#class-field-binding-modes).

**Mode 1 — Private fields + public properties** *(recommended)*

![Private fields + public properties](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/f03.png)

Paste the generated text into your class:

```csharp
public class obPlayer
{
    int id = 0;
    string code = "";
    string name = "";
    DateTime date_register = DateTime.MinValue;
    string tel = "";
    string email = "";
    int status = 0;

    public int Id { get { return id; } set { id = value; } }
    public string Code { get { return code; } set { code = value; } }
    public string Name { get { return name; } set { name = value; } }
    public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
    public string Tel { get { return tel; } set { tel = value; } }
    public string Email { get { return email; } set { email = value; } }
    public int Status { get { return status; } set { status = value; } }
}
```

**Mode 2 — Public properties**

![Public properties](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/g01.png)

**Mode 3 — Public fields**

![Public fields](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/f02.png)

**Custom SELECT (joins, aliases, projections)**

Paste any custom SELECT — including joins — and the Helper generates a POCO matching the result shape.

![Custom SELECT class generation](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/g03.png)

```csharp
public class obPlayerTeam
{
    int id = 0;
    string code = "";
    string name = "";
    DateTime date_register = DateTime.MinValue;
    string tel = "";
    string email = "";
    int status = 0;
    int year = 0;
    string teamname = "";
    string teamcode = "";
    int teamid = 0;

    public int Id { get { return id; } set { id = value; } }
    public string Code { get { return code; } set { code = value; } }
    public string Name { get { return name; } set { name = value; } }
    public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
    public string Tel { get { return tel; } set { tel = value; } }
    public string Email { get { return email; } set { email = value; } }
    public int Status { get { return status; } set { status = value; } }
    public int Year { get { return year; } set { year = value; } }
    public string Teamname { get { return teamname; } set { teamname = value; } }
    public string Teamcode { get { return teamcode; } set { teamcode = value; } }
    public int Teamid { get { return teamid; } set { teamid = value; } }
}
```

## Generating dictionary entries

For `Insert` and `Update`, the Helper generates an empty dictionary populated with all column keys, ready for you to fill in the values.

![Dictionary entry generator](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/g04.png)

```csharp
Dictionary<string, object> dic = new Dictionary<string, object>();

dic["id"]            =
dic["code"]          =
dic["name"]          =
dic["date_register"] =
dic["tel"]           =
dic["email"]         =
dic["status"]        =
```

Delete the auto-increment primary key line (`dic["id"] =`), fill in the rest, and you have a working `Insert`:

```csharp
Dictionary<string, object> dic = new Dictionary<string, object>();

dic["code"]          = "P001";
dic["name"]          = "John Smith";
dic["date_register"] = DateTime.Now;
dic["tel"]           = "0123456789";
dic["email"]         = "john@mail.com";
dic["status"]        = 1;

m.Insert("player", dic);
```

There's also a sibling generator for parameter dictionaries (`@paramName` keys), used for `Select` and `ExecuteScalar` calls.

## Generating the update column list

For `InsertUpdate`, the Helper generates a `List<string>` of all non-primary-key columns — the columns that should get updated on conflict.

![Update column list generator](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/g05.png)

![Update column list generator](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/g06.png)

```csharp
List<string> lstUpdateCol = new List<string>();

lstUpdateCol.Add("team_id");
lstUpdateCol.Add("score");
lstUpdateCol.Add("level");
lstUpdateCol.Add("status");

Dictionary<string, object> dic = new Dictionary<string, object>();
dic["year"]      = 2024;
dic["player_id"] = 1;
dic["team_id"]   = 1;
dic["score"]     = 10m;
dic["level"]     = 1;
dic["status"]    = 1;

m.InsertUpdate("player_team", dic, lstUpdateCol);
```

> The Helper is a convenience. Everything it generates can also be produced programmatically at runtime through MySqlExpress itself: `GenerateTableClassFields`, `GenerateCustomClassField`, `GenerateTableDictionaryEntries`, `GenerateParameterDictionaryTable`, and `GenerateUpdateColumnList`. Use whichever workflow fits your project.

---

# Part 3 — Extras

## Visual Studio toolbox tip

The standard MySQL connection block is boilerplate you'll type hundreds of times. Visual Studio's **Toolbox** can hold it as a drag-and-drop snippet.

Select the code block, drag it onto the Toolbox:

![Drag code into toolbox](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/02.png)

Once saved, it lives in the Toolbox:

![Saved in toolbox](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/03.png)

Next time, drag it from the Toolbox into the editor:

![Drag back into editor](https://raw.githubusercontent.com/adriancs2/MySqlExpress/master/wiki/05.png)

Small thing, but it adds up over a career.

---

## Relationship with SQLiteExpress

[SQLiteExpress](https://github.com/adriancs2/SQLiteExpress) mirrors this library's API for SQLite. If you work across both databases, you can keep the same mental model — `Insert`, `GetObject<T>`, `InsertUpdate`, `Update`, and the rest behave identically, with only the connection/command types swapping out.

| Feature                                      | MySqlExpress | SQLiteExpress |
| -------------------------------------------- | :----------: | :-----------: |
| Select / Execute / ExecuteScalar             | ✓            | ✓             |
| GetObject\<T\> / GetObjectList\<T\>          | ✓            | ✓             |
| GetObject into existing instance             | ✓            | ✓             |
| Insert (Dictionary)                          | ✓            | ✓             |
| Update (single / multi condition, LIMIT 1)   | ✓            | ✓             |
| InsertUpdate (Upsert)                        | ✓ (`ON DUPLICATE KEY UPDATE`) | ✓ (`ON CONFLICT DO UPDATE`) |
| InsertOrReplace                              | —            | ✓             |
| Save / SaveList (object)                     | ✓            | ✓             |
| String helpers (Escape, GetLikeString, …)    | ✓            | ✓             |
| Code generation                              | ✓            | ✓             |
| Table DDL (CreateTable, etc.)                | —            | ✓             |
| Attach / Detach database                     | —            | ✓             |

The differences reflect the underlying databases: MySQL has robust `ALTER TABLE`, so SQLiteExpress's DDL helpers aren't needed here; SQLite doesn't have `INSERT ... ON DUPLICATE KEY UPDATE`, so the upsert syntax differs under the hood.

---

## License

Public Domain. No attribution required. Use it, fork it, rebrand it, ship it.
