using System;
using System.Collections.Generic;
using Demo_ASPNET_Core.engine.models;
using Microsoft.AspNetCore.Http;
using MySqlConnector;

namespace Demo_ASPNET_Core.engine.handlers
{
    /// <summary>
    /// Roster JSON endpoints. The Assign action is the star of the show:
    /// one <c>InsertUpdate</c> call handles both the "first assignment"
    /// case and the "this year's player already has stats, update them"
    /// case, because the primary key is <c>(year, player_id)</c>.
    /// </summary>
    public static class RosterApi
    {
        public static void List(HttpContext ctx)
        {
            // Kept for future JS-rendered callers. Not used by the server-
            // rendered Roster.cs page today, but the endpoint is reserved.
            if (!RequireConn(ctx)) return;

            int year;
            if (!int.TryParse((ctx.Request.Query["year"].ToString() ?? "").Trim(), out year)) year = 2024;

            try
            {
                List<obRosterRow> list;
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        list = m.GetObjectList<obRosterRow>(@"
                            select a.id, a.code, a.name, b.year, b.score, b.level,
                                   c.name as teamname, c.code as teamcode, c.id as teamid
                            from player a
                            inner join player_team b on a.id = b.player_id
                            inner join team c on b.team_id = c.id
                            where b.year = @year
                            order by b.score desc;",
                            new Dictionary<string, object> { ["@year"] = year });
                    }
                }

                ApiHelper.WriteJson(ctx, new { success = true, rows = list });
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError(ctx, "List failed: " + ex.Message);
            }
        }

        public static void Assign(HttpContext ctx)
        {
            if (!RequireConn(ctx)) return;

            IFormCollection form = ctx.Request.Form;

            int year;     int.TryParse((form["year"].ToString()     ?? "").Trim(), out year);
            int playerId; int.TryParse((form["playerId"].ToString() ?? "").Trim(), out playerId);
            int teamId;   int.TryParse((form["teamId"].ToString()   ?? "").Trim(), out teamId);
            int level;    int.TryParse((form["level"].ToString()    ?? "1").Trim(), out level);

            decimal score;
            if (!decimal.TryParse((form["score"].ToString() ?? "0").Trim(), out score))
                score = 0m;

            if (year == 0 || playerId == 0 || teamId == 0)
            {
                ApiHelper.WriteError(ctx, "Year, player, and team are required.");
                return;
            }

            var data = new Dictionary<string, object>
            {
                ["year"]      = year,
                ["player_id"] = playerId,
                ["team_id"]   = teamId,
                ["score"]     = score,
                ["level"]     = level,
                ["status"]    = 1,
            };

            // On conflict with the (year, player_id) primary key, update
            // these specific columns. Everything else (the PK itself) stays.
            var updateCols = new List<string> { "team_id", "score", "level", "status" };

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        try
                        {
                            m.StartTransaction();
                            m.InsertUpdate("player_team", data, updateCols);
                            m.Commit();
                        }
                        catch
                        {
                            m.Rollback();
                            throw;
                        }
                    }
                }

                ApiHelper.WriteSuccess(ctx, "Roster entry saved.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError(ctx, "Assign failed: " + ex.Message);
            }
        }

        public static void Delete(HttpContext ctx)
        {
            if (!RequireConn(ctx)) return;

            IFormCollection form = ctx.Request.Form;

            int year;     int.TryParse((form["year"].ToString()     ?? "").Trim(), out year);
            int playerId; int.TryParse((form["playerId"].ToString() ?? "").Trim(), out playerId);

            if (year == 0 || playerId == 0)
            {
                ApiHelper.WriteError(ctx, "Invalid parameters.");
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        m.Execute(
                            "delete from player_team where year = @year and player_id = @pid;",
                            new Dictionary<string, object>
                            {
                                ["@year"] = year,
                                ["@pid"]  = playerId,
                            });
                    }
                }

                ApiHelper.WriteSuccess(ctx, "Roster entry removed.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError(ctx, "Delete failed: " + ex.Message);
            }
        }

        static bool RequireConn(HttpContext ctx)
        {
            if (!Config.HasConnString)
            {
                ApiHelper.WriteError(ctx, "Not configured.");
                return false;
            }
            return true;
        }
    }
}
