using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using MySqlConnector;

namespace Demo_ASPNET_Core.engine.handlers
{
    public static class TeamEditApi
    {
        public static void Save(HttpContext ctx)
        {
            if (!RequireConn(ctx)) return;

            IFormCollection form = ctx.Request.Form;

            string form_id   = (form["id"].ToString()     ?? "").Trim();
            string code      = (form["code"].ToString()   ?? "").Trim();
            string name      = (form["name"].ToString()   ?? "").Trim();
            string city      = (form["city"].ToString()   ?? "").Trim();
            string statusStr = (form["status"].ToString() ?? "1").Trim();

            if (name.Length == 0)
            {
                ApiHelper.WriteError(ctx, "Name is required.");
                return;
            }

            int id; int.TryParse(form_id, out id);
            int status; int.TryParse(statusStr, out status);

            var data = new Dictionary<string, object>
            {
                ["code"]   = code,
                ["name"]   = name,
                ["city"]   = city,
                ["status"] = status,
            };

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
                            if (id == 0)
                            {
                                m.Insert("team", data);
                                id = m.LastInsertId;
                            }
                            else
                            {
                                m.Update("team", data, "id", id);
                            }
                            m.Commit();
                        }
                        catch
                        {
                            m.Rollback();
                            throw;
                        }
                    }
                }

                ApiHelper.WriteJson(ctx, new { success = true, id = id, message = "Saved." });
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError(ctx, "Save failed: " + ex.Message);
            }
        }

        public static void Delete(HttpContext ctx)
        {
            if (!RequireConn(ctx)) return;

            IFormCollection form = ctx.Request.Form;

            int id;
            if (!int.TryParse((form["id"].ToString() ?? "").Trim(), out id) || id <= 0)
            {
                ApiHelper.WriteError(ctx, "Invalid id.");
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

                        try
                        {
                            m.StartTransaction();
                            m.Execute(
                                "delete from player_team where team_id = @vid;",
                                new Dictionary<string, object> { ["@vid"] = id });
                            m.Execute(
                                "delete from team where id = @vid;",
                                new Dictionary<string, object> { ["@vid"] = id });
                            m.Commit();
                        }
                        catch
                        {
                            m.Rollback();
                            throw;
                        }
                    }
                }

                ApiHelper.WriteSuccess(ctx, "Team deleted.");
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
