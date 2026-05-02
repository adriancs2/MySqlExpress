using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using MySqlConnector;

namespace Demo_ASPNET_Core.engine.handlers
{
    /// <summary>
    /// JSON endpoints for the Home/Setup page.
    ///
    /// Each method writes a JSON response via ApiHelper. Returning from
    /// the handler short-circuits the pipeline — there's no equivalent
    /// to ApiHelper.EndResponse() in ASP.NET Core because we never
    /// invoke `next` after writing a response.
    ///
    /// Sync edition: form reads use ctx.Request.Form (synchronous);
    /// writes use ApiHelper which writes bytes synchronously.
    /// </summary>
    public static class SetupApi
    {
        // ---------- connection string: save / test / clear ----------

        public static void SaveConnString(HttpContext ctx)
        {
            IFormCollection form = ctx.Request.Form;
            string connStr = (form["connStr"].ToString() ?? "").Trim();
            if (string.IsNullOrEmpty(connStr))
            {
                ApiHelper.WriteError(ctx, "Connection string is empty.");
                return;
            }

            // Sanity-check by actually opening the connection.
            string err = TryConnect(connStr);
            if (!string.IsNullOrEmpty(err))
            {
                ApiHelper.WriteError(ctx, "Could not connect: " + err);
                return;
            }

            Config.Save(connStr);
            ApiHelper.WriteSuccess(ctx, "Connection saved.");
        }

        public static void TestConnString(HttpContext ctx)
        {
            IFormCollection form = ctx.Request.Form;
            string connStr = (form["connStr"].ToString() ?? "").Trim();
            if (string.IsNullOrEmpty(connStr))
            {
                ApiHelper.WriteError(ctx, "Connection string is empty.");
                return;
            }

            string err = TryConnect(connStr);
            if (!string.IsNullOrEmpty(err))
            {
                ApiHelper.WriteError(ctx, "Could not connect: " + err);
                return;
            }

            ApiHelper.WriteSuccess(ctx, "Connection OK.");
        }

        public static void ClearConnString(HttpContext ctx)
        {
            Config.Clear();
            ApiHelper.WriteSuccess(ctx, "Connection string cleared.");
        }

        // ---------- schema: create / drop ----------

        public static void CreateTables(HttpContext ctx)
        {
            if (!RequireConnOrEnd(ctx)) return;

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

                ApiHelper.WriteSuccess(ctx, "Tables created.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError(ctx, "Create failed: " + ex.Message);
            }
        }

        public static void DropTables(HttpContext ctx)
        {
            if (!RequireConnOrEnd(ctx)) return;

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

                ApiHelper.WriteSuccess(ctx, "Tables dropped.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError(ctx, "Drop failed: " + ex.Message);
            }
        }

        // ---------- seed data ----------

        public static void SeedSampleData(HttpContext ctx)
        {
            if (!RequireConnOrEnd(ctx)) return;

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

                ApiHelper.WriteSuccess(ctx, "Sample data seeded.");
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError(ctx, "Seed failed: " + ex.Message);
            }
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

        static bool RequireConnOrEnd(HttpContext ctx)
        {
            if (!Config.HasConnString)
            {
                ApiHelper.WriteError(ctx, "No connection string configured. Go to Setup first.");
                return false;
            }
            return true;
        }
    }
}
