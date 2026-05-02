using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using MySqlConnector;

namespace Demo_ASPNET_Core.engine.handlers
{
    /// <summary>
    /// JSON endpoints for player save/delete. The heavy lifting is one
    /// line of MySqlExpress each (Insert / Update / Execute).
    ///
    /// Sync edition: form reads via ctx.Request.Form (no await).
    /// </summary>
    public static class PlayerEditApi
    {
        public static void Save(HttpContext ctx)
        {
            if (!RequireConn(ctx)) return;

            IFormCollection form = ctx.Request.Form;

            string form_id    = (form["id"].ToString()           ?? "").Trim();
            string code       = (form["code"].ToString()         ?? "").Trim();
            string name       = (form["name"].ToString()         ?? "").Trim();
            string email      = (form["email"].ToString()        ?? "").Trim();
            string tel        = (form["tel"].ToString()          ?? "").Trim();
            string dateStr    = (form["dateRegister"].ToString() ?? "").Trim();
            string statusStr  = (form["status"].ToString()       ?? "1").Trim();

            if (name.Length == 0)
            {
                ApiHelper.WriteError(ctx, "Name is required.");
                return;
            }

            int id; int.TryParse(form_id, out id);
            int status; int.TryParse(statusStr, out status);

            DateTime dateRegister;
            if (!DateTime.TryParse(dateStr, out dateRegister))
                dateRegister = DateTime.Now;

            var data = new Dictionary<string, object>
            {
                ["code"]          = code,
                ["name"]          = name,
                ["email"]         = email,
                ["tel"]           = tel,
                ["date_register"] = dateRegister,
                ["status"]        = status,
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
                                // New row — Insert + LastInsertId.
                                m.Insert("player", data);
                                id = m.LastInsertId;
                            }
                            else
                            {
                                // Existing row — Update on "id" with LIMIT 1.
                                m.Update("player", data, "id", id);
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

                            // Roster references player_id; remove those rows first
                            // so the delete doesn't leave orphaned stats.
                            m.Execute(
                                "delete from player_team where player_id = @vid;",
                                new Dictionary<string, object> { ["@vid"] = id });

                            m.Execute(
                                "delete from player where id = @vid;",
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

                ApiHelper.WriteSuccess(ctx, "Player deleted.");
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
