using MySqlConnector;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// Showcases the Generate* methods built into MySqlExpress itself.
    /// No desktop Helper app required &mdash; the library can write its own
    /// boilerplate from a live database connection.
    /// </summary>
    public static class CodeGen
    {
        public static void HandleRequest()
        {
            if (!Config.HasConnString) { Render.NotConfigured(); return; }

            List<string> tables;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        tables = m.GetTableList();
                    }
                }
            }
            catch (Exception ex)
            {
                Render.Error("Code Generation", "codegen", ex.Message);
                return;
            }

            string tableOptions = tables.Count == 0
                ? "<option value=''>(no tables &mdash; run Setup first)</option>"
                : string.Join("", tables.ConvertAll(t =>
                    $"<option value='{WebUtility.HtmlEncode(t)}'>{WebUtility.HtmlEncode(t)}</option>"));

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header("Code Generation", "codegen"));

            sb.Append($@"
<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-code'></i> Code Generation</h2>
    </div>

    <div class='section-note'>
        <i class='fas fa-lightbulb'></i> Every button below calls a method
        that already lives inside <code>MySqlExpress.cs</code>. You point it
        at a table, it reads <code>SHOW COLUMNS</code>, it writes C# for you.
    </div>

    <div class='form-row'>
        <div class='form-field' style='max-width:240px'>
            <label>Table</label>
            <select id='cgTable'>{tableOptions}</select>
        </div>
        <div class='form-field' style='max-width:300px'>
            <label>Class-Fields Style</label>
            <select id='cgStyle'>
                <option value='PrivateFielsPublicProperties' selected>Private fields + public properties</option>
                <option value='PublicProperties'>Public properties only</option>
                <option value='PublicFields'>Public fields only</option>
            </select>
        </div>
    </div>

    <div class='form-actions' style='border-top:none; padding-top:0; margin-top:0;'>
        <button class='btn btn-primary' onclick='runGen(""class-fields"")'>
            <i class='fas fa-cube'></i> Class Fields
        </button>
        <button class='btn btn-primary' onclick='runGen(""dict"")'>
            <i class='fas fa-list'></i> Dictionary (Insert/Update)
        </button>
        <button class='btn btn-primary' onclick='runGen(""param-dict"")'>
            <i class='fas fa-at'></i> Parameter Dictionary
        </button>
        <button class='btn btn-primary' onclick='runGen(""update-cols"")'>
            <i class='fas fa-pen'></i> Update Column List
        </button>
        <button class='btn btn-primary' onclick='runGen(""create-sql"")'>
            <i class='fas fa-database'></i> CREATE TABLE SQL
        </button>
    </div>
</div>

<div class='card'>
    <h3><i class='fas fa-project-diagram'></i> Custom projection (joins, aliases)</h3>
    <p class='muted'>
        Paste a <code>SELECT</code> with joins or aliases. MySqlExpress runs it
        with <code>where 1=0</code> (so nothing returns) and reads the column
        metadata to generate a matching POCO. Same method you'd use to build
        a <code>obPlayerTeam</code> class for a 3-table join.
    </p>

    <div class='form-field'>
        <label>SELECT</label>
        <textarea id='cgSql' rows='4' placeholder='select a.*, b.year from player a inner join player_team b on a.id = b.player_id;'>select a.id, a.name, b.year, b.score, c.name as teamname from player a inner join player_team b on a.id = b.player_id inner join team c on b.team_id = c.id;</textarea>
    </div>

    <div class='form-actions' style='border-top:none; padding-top:0; margin-top:0;'>
        <button class='btn btn-primary' onclick='runGen(""custom"")'>
            <i class='fas fa-magic'></i> Generate from SELECT
        </button>
    </div>
</div>

<div id='cgResult'></div>
");
            sb.Append(@"
<script>
function runGen(kind) {
    var payload = {
        kind:  kind,
        table: document.getElementById('cgTable').value,
        style: document.getElementById('cgStyle').value,
        sql:   document.getElementById('cgSql').value
    };
    document.getElementById('cgResult').innerHTML =
        ""<div class='card'><p class='muted'>Generating...</p></div>"";
    window.apiPostForm('/api/codegen/run', payload, function (d) {
        var el = document.getElementById('cgResult');
        if (!d.success) {
            el.innerHTML =
                ""<div class='card'><div class='flash flash-error'><i class='fas fa-exclamation-triangle'></i> "" +
                window.escapeHtml(d.message || 'Error') + ""</div></div>"";
            return;
        }
        el.innerHTML =
            ""<div class='card'>"" +
                ""<div class='card-header'><h3>"" + window.escapeHtml(d.label || 'Generated') + ""</h3>"" +
                ""<button class='btn btn-sm btn-secondary' onclick='copyCode()'><i class=\""fas fa-copy\""></i> Copy</button>"" +
                ""</div>"" +
                ""<div class='code-label'>"" + window.escapeHtml(d.method || '') + ""</div>"" +
                ""<pre class='code-block' id='cgCode'>"" + window.escapeHtml(d.code || '') + ""</pre>"" +
            ""</div>"";
    });
}

function copyCode() {
    var pre = document.getElementById('cgCode');
    if (!pre) return;
    var text = pre.textContent || pre.innerText || '';
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text);
    } else {
        var ta = document.createElement('textarea');
        ta.value = text;
        document.body.appendChild(ta);
        ta.select();
        document.execCommand('copy');
        document.body.removeChild(ta);
    }
}
</script>
");
            sb.Append(SiteTemplate.Footer());

            var res = HttpContext.Current.Response;
            res.ContentType = "text/html"; res.Charset = "utf-8";
            res.Write(sb.ToString());
            ApiHelper.EndResponse();
        }
    }
}