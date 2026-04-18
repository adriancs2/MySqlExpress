using MySqlConnector;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// Runs MySqlExpress's built-in code generators and returns the C#
    /// output as a string for display. Every branch here maps to a
    /// single library call &mdash; the library is doing the work, we are
    /// just dispatching.
    /// </summary>
    public static class CodeGenApi
    {
        static HttpRequest Req { get { return HttpContext.Current.Request; } }

        public static void Run()
        {
            if (!Config.HasConnString)
            {
                ApiHelper.WriteError("Not configured.");
                ApiHelper.EndResponse();
                return;
            }

            string kind  = (Req.Form["kind"]  ?? "").Trim();
            string table = (Req.Form["table"] ?? "").Trim();
            string style = (Req.Form["style"] ?? "PrivateFielsPublicProperties").Trim();
            string sql   = (Req.Form["sql"]   ?? "").Trim();

            if (kind != "custom" && string.IsNullOrEmpty(table))
            {
                ApiHelper.WriteError("Pick a table first.");
                ApiHelper.EndResponse();
                return;
            }

            MySqlExpress.FieldsOutputType styleEnum;
            if (!Enum.TryParse(style, true, out styleEnum))
                styleEnum = MySqlExpress.FieldsOutputType.PrivateFielsPublicProperties;

            try
            {
                string code = "";
                string label = "";
                string method = "";

                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        switch (kind)
                        {
                            case "class-fields":
                                code = m.GenerateTableClassFields(table, styleEnum);
                                label = $"Class fields for `{table}`";
                                method = "m.GenerateTableClassFields(\"" + table + "\", FieldsOutputType." + styleEnum + ")";
                                break;

                            case "dict":
                                code = m.GenerateTableDictionaryEntries(table);
                                label = $"Dictionary template for `{table}`";
                                method = "m.GenerateTableDictionaryEntries(\"" + table + "\")";
                                break;

                            case "param-dict":
                                code = m.GenerateParameterDictionaryTable(table);
                                label = $"Parameter dictionary template for `{table}`";
                                method = "m.GenerateParameterDictionaryTable(\"" + table + "\")";
                                break;

                            case "update-cols":
                                code = m.GenerateUpdateColumnList(table);
                                label = $"Update column list for `{table}` (non-PK columns)";
                                method = "m.GenerateUpdateColumnList(\"" + table + "\")";
                                break;

                            case "create-sql":
                                code = m.GetCreateTableSql(table);
                                label = $"CREATE TABLE SQL for `{table}`";
                                method = "m.GetCreateTableSql(\"" + table + "\")";
                                break;

                            case "custom":
                                if (string.IsNullOrWhiteSpace(sql))
                                    throw new Exception("Provide a SELECT statement.");
                                code = m.GenerateCustomClassField(sql, styleEnum);
                                label = "Class fields from custom SELECT";
                                method = "m.GenerateCustomClassField(sql, FieldsOutputType." + styleEnum + ")";
                                break;

                            default:
                                throw new Exception("Unknown kind: " + kind);
                        }
                    }
                }

                ApiHelper.WriteJson(new
                {
                    success = true,
                    label = label,
                    method = method,
                    code = code,
                });
            }
            catch (Exception ex)
            {
                ApiHelper.WriteError("Generate failed: " + ex.Message);
            }
            ApiHelper.EndResponse();
        }
    }
}
