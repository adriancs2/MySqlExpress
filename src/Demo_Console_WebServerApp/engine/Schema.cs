using System.Collections.Generic;

namespace System
{
    /// <summary>
    /// All DDL for the demo database, held as C# constants.
    /// The setup page runs each statement through `m.Execute(sql)`,
    /// which is exactly the "run any SQL" surface described in the
    /// MySqlExpress README.
    /// </summary>
    public static class Schema
    {
        public const string CreatePlayer = @"
create table if not exists player (
    id              int unsigned not null auto_increment,
    code            varchar(20),
    name            varchar(200),
    date_register   datetime,
    tel             varchar(50),
    email           varchar(150),
    status          int unsigned default 1,
    primary key (id),
    key idx_player_code (code),
    key idx_player_name (name)
);";

        public const string CreateTeam = @"
create table if not exists team (
    id      int unsigned not null auto_increment,
    code    varchar(20),
    name    varchar(200),
    city    varchar(100),
    status  int unsigned default 1,
    primary key (id),
    key idx_team_code (code)
);";

        /// <summary>
        /// Composite-PK join table. The (year, player_id) pair is the key,
        /// which is exactly the shape MySqlExpress.InsertUpdate was built
        /// for: insert a new row if the pair is new, or update this year's
        /// stats if the pair already exists.
        /// </summary>
        public const string CreatePlayerTeam = @"
create table if not exists player_team (
    year        int unsigned not null,
    player_id   int unsigned not null,
    team_id     int unsigned not null,
    score       decimal(10,2) default 0,
    level       int unsigned default 1,
    status      int unsigned default 1,
    primary key (year, player_id),
    key idx_pt_team (team_id),
    key idx_pt_player (player_id)
);";

        public const string DropAll = @"
drop table if exists player_team;
drop table if exists player;
drop table if exists team;";

        public class SeedRow
        {
            public string Table;
            public Dictionary<string, object> Data;
            public SeedRow(string table, Dictionary<string, object> data)
            {
                Table = table;
                Data = data;
            }
        }

        /// <summary>
        /// A small amount of sample data so freshly-set-up instances
        /// aren't staring at empty lists. Wrapped in a transaction by
        /// the caller.
        /// </summary>
        public static SeedRow[] SeedTeams()
        {
            return new[]
            {
                new SeedRow("team", new Dictionary<string, object> {
                    ["code"] = "T01", ["name"] = "Thunder Foxes",  ["city"] = "Northfield", ["status"] = 1 }),
                new SeedRow("team", new Dictionary<string, object> {
                    ["code"] = "T02", ["name"] = "Ocean Marlins",  ["city"] = "Baysville",  ["status"] = 1 }),
                new SeedRow("team", new Dictionary<string, object> {
                    ["code"] = "T03", ["name"] = "Highland Hawks", ["city"] = "Hillbrook",  ["status"] = 1 }),
            };
        }

        public static SeedRow[] SeedPlayers()
        {
            return new[]
            {
                new SeedRow("player", new Dictionary<string, object> {
                    ["code"] = "P001", ["name"] = "Alice Johnson", ["date_register"] = DateTime.Now.AddDays(-120),
                    ["tel"] = "5550001001", ["email"] = "alice@example.com", ["status"] = 1 }),
                new SeedRow("player", new Dictionary<string, object> {
                    ["code"] = "P002", ["name"] = "Bob Williams",  ["date_register"] = DateTime.Now.AddDays(-100),
                    ["tel"] = "5550001002", ["email"] = "bob@example.com",   ["status"] = 1 }),
                new SeedRow("player", new Dictionary<string, object> {
                    ["code"] = "P003", ["name"] = "Carol Davis",   ["date_register"] = DateTime.Now.AddDays(-80),
                    ["tel"] = "5550001003", ["email"] = "carol@example.com", ["status"] = 1 }),
                new SeedRow("player", new Dictionary<string, object> {
                    ["code"] = "P004", ["name"] = "David Miller",  ["date_register"] = DateTime.Now.AddDays(-60),
                    ["tel"] = "5550001004", ["email"] = "david@example.com", ["status"] = 1 }),
                new SeedRow("player", new Dictionary<string, object> {
                    ["code"] = "P005", ["name"] = "Emma Brown",    ["date_register"] = DateTime.Now.AddDays(-40),
                    ["tel"] = "5550001005", ["email"] = "emma@example.com",  ["status"] = 1 }),
            };
        }

        public static SeedRow[] SeedRoster()
        {
            return new[]
            {
                new SeedRow("player_team", new Dictionary<string, object> {
                    ["year"]=2024, ["player_id"]=1, ["team_id"]=1, ["score"]=92.5m, ["level"]=4, ["status"]=1 }),
                new SeedRow("player_team", new Dictionary<string, object> {
                    ["year"]=2024, ["player_id"]=2, ["team_id"]=1, ["score"]=88.0m, ["level"]=3, ["status"]=1 }),
                new SeedRow("player_team", new Dictionary<string, object> {
                    ["year"]=2024, ["player_id"]=3, ["team_id"]=2, ["score"]=95.0m, ["level"]=5, ["status"]=1 }),
                new SeedRow("player_team", new Dictionary<string, object> {
                    ["year"]=2024, ["player_id"]=4, ["team_id"]=3, ["score"]=79.0m, ["level"]=3, ["status"]=1 }),
                new SeedRow("player_team", new Dictionary<string, object> {
                    ["year"]=2024, ["player_id"]=5, ["team_id"]=3, ["score"]=83.5m, ["level"]=3, ["status"]=1 }),
            };
        }
    }
}
