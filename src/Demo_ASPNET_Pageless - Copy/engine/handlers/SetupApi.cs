using MySqlConnector;
using System.Collections.Generic;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// JSON endpoints for the Home/Setup page.
    ///
    /// Each method writes a JSON response via ApiHelper and ends the
    /// response so the ASP.NET pipeline doesn't render anything else.
    /// </summary>
    public static class SetupApi
    {
        static HttpRequest Req { get { return HttpContext.Current.Request; } }

        // ---------- connection string: save / test / clear ----------

        public static void SaveConnString()
        {
            string connStr = (Req.Form["connStr"] ?? "").Trim();
            if (string.IsNullOrEmpty(connStr))
            {
                ApiHelper.WriteError("Connection string is empty.");
                ApiHelper.EndResponse();
                return;
            }

            // Sanity-check by actually opening the connection.
            string err = TryConnect(connStr);
            if (!string.IsNullOrEmpty(err))
            {
                ApiHelper.WriteError("Could not connect: " + err);
                ApiHelper.EndResponse();
                return;
            }

            Config.Save(connStr);
            ApiHelper.WriteSuccess("Connection saved.");
            ApiHelper.EndResponse();
        }

        public static void TestConnString()
        {
            string connStr = (Req.Form["connStr"] ?? "").Trim();
            if (string.IsNullOrEmpty(connStr))
            {
                ApiHelper.WriteError("Connection string is empty.");
                ApiHelper.EndResponse();
                return;
            }

            string err = TryConnect(connStr);
            if (!string.IsNullOrEmpty(err))
            {
                ApiHelper.WriteError("Could not connect: " + err);
                ApiHelper.EndResponse();
                return;
            }

            ApiHelper.WriteSuccess("Connection OK.");
            ApiHelper.EndResponse();
        }

        public static void ClearConnString()
        {
            Config.Clear();
            ApiHelper.WriteSuccess("Connection string cleared.");
            ApiHelper.EndResponse();
        }

        // ---------- schema: create / drop ----------

        public static void CreateTables()
        {
            if (!RequireConnOrEnd()) return;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        // A transaction here is overkill for DDL (MySQL auto-commits
                        // DDL anyway), but we use it consistently throughout the demo.
                        try
                        {
                            m.StartTransaction();
                            m.Execute(Schema.CreateTeam);
                            m.Execute(Schema.CreatePlayer);
                            m.Execute(Schema.CreatePlayerTeam);
                            m.Commit();
                        }
                        catch
                        {
                            m.Rollback();
                            throw;
                        }
                    }
                }

                ApiHelper.WriteSuccess("Tables created.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("Create failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        public static void DropTables()
        {
            if (!RequireConnOrEnd()) return;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        // Execute handles multi-statement scripts separated by ';'
                        // when AllowUserVariables / compound statements are enabled
                        // on MySqlConnector. To be safe, we split and run each.
                        string[] statements = Schema.DropAll.Split(';');
                        foreach (string raw in statements)
                        {
                            string sql = raw.Trim();
                            if (sql.Length > 0) m.Execute(sql + ";");
                        }
                    }
                }

                ApiHelper.WriteSuccess("Tables dropped.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("Drop failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        // ---------- seed data ----------

        public static void SeedSampleData()
        {
            if (!RequireConnOrEnd()) return;

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

                            // Teams first so FK-ish ordering reads naturally.
                            foreach (var sr in Schema.SeedTeams())
                                m.Insert(sr.Table, sr.Data);

                            foreach (var sr in Schema.SeedPlayers())
                                m.Insert(sr.Table, sr.Data);

                            // Roster uses InsertUpdate (upsert) because the
                            // (year, player_id) pair is a composite primary key.
                            var updateCols = new List<string> { "team_id", "score", "level", "status" };
                            foreach (var sr in Schema.SeedRoster())
                                m.InsertUpdate(sr.Table, sr.Data, updateCols);

                            m.Commit();
                        }
                        catch
                        {
                            m.Rollback();
                            throw;
                        }
                    }
                }

                ApiHelper.WriteSuccess("Sample data seeded.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("Seed failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        // ---------- helpers ----------

        static string TryConnect(string connStr)
        {
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("select 1;", conn))
                    {
                        cmd.ExecuteScalar();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        static bool RequireConnOrEnd()
        {
            if (!Config.HasConnString)
            {
                ApiHelper.WriteError("No connection string configured. Go to Setup first.");
                ApiHelper.EndResponse();
                return false;
            }
            return true;
        }
    }
}
