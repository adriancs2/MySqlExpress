using System.Collections.Generic;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// JSON endpoints for player save/delete. The heavy lifting is one
    /// line of MySqlExpress each (Insert / Update / Execute).
    /// </summary>
    public static class PlayerEditApi
    {
        static HttpRequest Req { get { return HttpContext.Current.Request; } }

        public static void Save()
        {
            if (!RequireConn()) return;

            string form_id    = (Req.Form["id"]           ?? "").Trim();
            string code       = (Req.Form["code"]         ?? "").Trim();
            string name       = (Req.Form["name"]         ?? "").Trim();
            string email      = (Req.Form["email"]        ?? "").Trim();
            string tel        = (Req.Form["tel"]          ?? "").Trim();
            string dateStr    = (Req.Form["dateRegister"] ?? "").Trim();
            string statusStr  = (Req.Form["status"]       ?? "1").Trim();

            if (name.Length == 0)
            {
                ApiHelper.WriteError("Name is required.");
                ApiHelper.EndResponse();
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
                Db.Run(m =>
                {
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
                });

                ApiHelper.WriteJson(new { success = true, id = id, message = "Saved." });
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("Save failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        public static void Delete()
        {
            if (!RequireConn()) return;

            int id;
            if (!int.TryParse((Req.Form["id"] ?? "").Trim(), out id) || id <= 0)
            {
                ApiHelper.WriteError("Invalid id.");
                ApiHelper.EndResponse();
                return;
            }

            try
            {
                Db.Run(m =>
                {
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
                });

                ApiHelper.WriteSuccess("Player deleted.");
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
