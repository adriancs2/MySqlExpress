using MySqlConnector;
using System.Collections.Generic;
using System.Data;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// Runs arbitrary SQL. Routes to m.Select or m.Execute based on
    /// the first keyword. Returns a JSON shape the RawSql page's JS
    /// can render as a table or a success banner.
    /// </summary>
    public static class RawSqlApi
    {
        public static void Run()
        {
            if (!Config.HasConnString)
            {
                ApiHelper.WriteError("Not configured.");
                ApiHelper.EndResponse();
                return;
            }

            string sql = (HttpContext.Current.Request.Form["sql"] ?? "").Trim();
            if (sql.Length == 0)
            {
                ApiHelper.WriteError("SQL is empty.");
                ApiHelper.EndResponse();
                return;
            }

            bool isQuery = IsReadStatement(sql);

            try
            {
                if (isQuery)
                {
                    DataTable dt;
                    using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.Connection = conn;
                            MySqlExpress m = new MySqlExpress(cmd);

                            dt = m.Select(sql);
                        }
                    }

                    var columns = new List<string>();
                    foreach (DataColumn dc in dt.Columns) columns.Add(dc.ColumnName);

                    var rowsOut = new List<Dictionary<string, object>>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        var row = new Dictionary<string, object>();
                        foreach (DataColumn dc in dt.Columns)
                        {
                            object v = dr[dc];
                            if (v == DBNull.Value) v = null;
                            // Format DateTime as ISO-like so the browser shows something sane.
                            if (v is DateTime) v = ((DateTime)v).ToString("yyyy-MM-dd HH:mm:ss");
                            row[dc.ColumnName] = v;
                        }
                        rowsOut.Add(row);
                    }

                    ApiHelper.WriteJson(new
                    {
                        success = true,
                        kind = "select",
                        columns = columns,
                        rows = rowsOut
                    });
                }
                else
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
                                m.Execute(sql);
                                m.Commit();
                            }
                            catch
                            {
                                m.Rollback();
                                throw;
                            }
                        }
                    }

                    ApiHelper.WriteJson(new
                    {
                        success = true,
                        kind = "execute",
                        message = "Statement executed."
                    });
                }
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("SQL error: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }

        static bool IsReadStatement(string sql)
        {
            // Strip leading whitespace + common comment forms.
            int i = 0;
            while (i < sql.Length && (char.IsWhiteSpace(sql[i]))) i++;
            // Strip /* ... */ at start.
            while (i < sql.Length - 1 && sql[i] == '/' && sql[i + 1] == '*')
            {
                int end = sql.IndexOf("*/", i + 2);
                if (end < 0) break;
                i = end + 2;
                while (i < sql.Length && char.IsWhiteSpace(sql[i])) i++;
            }
            // Strip -- line comment at start.
            while (i < sql.Length - 1 && sql[i] == '-' && sql[i + 1] == '-')
            {
                int end = sql.IndexOf('\n', i);
                if (end < 0) { i = sql.Length; break; }
                i = end + 1;
                while (i < sql.Length && char.IsWhiteSpace(sql[i])) i++;
            }

            if (i >= sql.Length) return false;

            string rest = sql.Substring(i);

            string[] readPrefixes = {
                "select", "show", "describe", "desc ", "explain", "with "
            };
            foreach (var p in readPrefixes)
            {
                if (rest.Length >= p.Length &&
                    rest.Substring(0, p.Length).Equals(p, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
