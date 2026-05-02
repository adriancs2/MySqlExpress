using MySqlConnector;
using System.Collections.Generic;

namespace System.handlers
{
    public static class TeamEditApi
    {
        public static void Save(Ctx ctx)
        {
            if (!RequireConn(ctx)) return;

            var f = ctx.Request.Form;
            string form_id   = (f["id"]     ?? "").Trim();
            string code      = (f["code"]   ?? "").Trim();
            string name      = (f["name"]   ?? "").Trim();
            string city      = (f["city"]   ?? "").Trim();
            string statusStr = (f["status"] ?? "1").Trim();

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

        public static void Delete(Ctx ctx)
        {
            if (!RequireConn(ctx)) return;

            int id;
            if (!int.TryParse((ctx.Request.Form["id"] ?? "").Trim(), out id) || id <= 0)
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

        static bool RequireConn(Ctx ctx)
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
