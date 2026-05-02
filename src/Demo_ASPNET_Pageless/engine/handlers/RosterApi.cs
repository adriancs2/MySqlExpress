using MySqlConnector;
using System.Collections.Generic;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// Roster JSON endpoints. The Assign action is the star of the show:
    /// one <c>InsertUpdate</c> call handles both the "first assignment"
    /// case and the "this year's player already has stats, update them"
    /// case, because the primary key is <c>(year, player_id)</c>.
    /// </summary>
    public static class RosterApi
    {
        static HttpRequest Req { get { return HttpContext.Current.Request; } }

        public static void List()
        {
            // Kept for future JS-rendered callers. Not used by the server-
            // rendered Roster.cs page today, but the endpoint is reserved.
            if (!RequireConn()) return;

            int year;
            if (!int.TryParse((Req.QueryString["year"] ?? "").Trim(), out year)) year = 2024;

            try
            {
                List<models.obRosterRow> list;
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        list = m.GetObjectList<models.obRosterRow>(@"
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

                ApiHelper.WriteJson(new { success = true, rows = list });
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("List failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        public static void Assign()
        {
            if (!RequireConn()) return;

            int year;     int.TryParse((Req.Form["year"]     ?? "").Trim(), out year);
            int playerId; int.TryParse((Req.Form["playerId"] ?? "").Trim(), out playerId);
            int teamId;   int.TryParse((Req.Form["teamId"]   ?? "").Trim(), out teamId);
            int level;    int.TryParse((Req.Form["level"]    ?? "1").Trim(), out level);

            decimal score;
            if (!decimal.TryParse((Req.Form["score"] ?? "0").Trim(), out score))
                score = 0m;

            if (year == 0 || playerId == 0 || teamId == 0)
            {
                ApiHelper.WriteError("Year, player, and team are required.");
                ApiHelper.EndResponse();
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

                ApiHelper.WriteSuccess("Roster entry saved.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("Assign failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        public static void Delete()
        {
            if (!RequireConn()) return;

            int year;     int.TryParse((Req.Form["year"]     ?? "").Trim(), out year);
            int playerId; int.TryParse((Req.Form["playerId"] ?? "").Trim(), out playerId);

            if (year == 0 || playerId == 0)
            {
                ApiHelper.WriteError("Invalid parameters.");
                ApiHelper.EndResponse();
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

                ApiHelper.WriteSuccess("Roster entry removed.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("Delete failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        static bool RequireConn()
        {
            if (!Config.HasConnString)
            {
                ApiHelper.WriteError("Not configured.");
                ApiHelper.EndResponse();
                return false;
            }
            return true;
        }
    }
}
