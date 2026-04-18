using System.Collections.Generic;
using System.Web;

namespace System.handlers
{
    public static class TeamEditApi
    {
        static HttpRequest Req { get { return HttpContext.Current.Request; } }

        public static void Save()
        {
            if (!RequireConn()) return;

            string form_id   = (Req.Form["id"]     ?? "").Trim();
            string code      = (Req.Form["code"]   ?? "").Trim();
            string name      = (Req.Form["name"]   ?? "").Trim();
            string city      = (Req.Form["city"]   ?? "").Trim();
            string statusStr = (Req.Form["status"] ?? "1").Trim();

            if (name.Length == 0)
            {
                ApiHelper.WriteError("Name is required.");
                ApiHelper.EndResponse();
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
                Db.Run(m =>
                {
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
                });
                ApiHelper.WriteSuccess("Team deleted.");
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
