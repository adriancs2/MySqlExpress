using System.Net;
using System.Text;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// Raw SQL scratchpad. Type any SQL, press Run, see results.
    /// The API at /api/rawsql/run decides whether to call
    /// m.Select (returns rows) or m.Execute (non-query) based on the
    /// leading keyword.
    /// </summary>
    public static class RawSql
    {
        public static void HandleRequest()
        {
            if (!Config.HasConnString) { Render.NotConfigured(); return; }

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header("Raw SQL", "rawsql"));

            sb.Append(@"
<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-terminal'></i> Raw SQL Scratchpad</h2>
    </div>

    <div class='section-note'>
        <i class='fas fa-code'></i>
        <code>SELECT</code>/<code>SHOW</code>/<code>DESCRIBE</code>/<code>EXPLAIN</code>
        statements go through <code>m.Select(sql)</code> and render as a table.
        Everything else runs as <code>m.Execute(sql)</code> &mdash; DDL,
        <code>INSERT</code>/<code>UPDATE</code>/<code>DELETE</code>, <code>SET</code>, pragmas, whatever.
    </div>

    <div class='form-field'>
        <label>SQL</label>
        <textarea id='sqlBox' rows='6' placeholder='select * from player limit 10;'>select * from player order by id desc limit 10;</textarea>
        <span class='hint'>Tip: parameterized SQL isn't supported here &mdash; this is a plain tinkering surface. Don't paste user input.</span>
    </div>

    <div class='form-actions' style='border-top:none; padding-top:0; margin-top:0;'>
        <button class='btn btn-primary' onclick='runSql()'><i class='fas fa-play'></i> Run</button>
        <button class='btn btn-secondary' onclick='clearResult()'>Clear</button>
    </div>
</div>

<div id='resultArea'></div>

<div class='card'>
    <h3><i class='fas fa-lightbulb'></i> Examples to try</h3>
    <ul style='padding-left:20px; line-height:2'>
        <li class='mono small'>select count(*) from player;</li>
        <li class='mono small'>show tables;</li>
        <li class='mono small'>describe player;</li>
        <li class='mono small'>select a.name, b.year, b.score from player a inner join player_team b on a.id=b.player_id;</li>
        <li class='mono small'>update player set status=1 where status is null;</li>
    </ul>
</div>

<script>
function runSql() {
    var sql = document.getElementById('sqlBox').value;
    if (!sql || !sql.trim()) return;
    document.getElementById('resultArea').innerHTML =
        ""<div class='card'><p class='muted'>Running...</p></div>"";
    window.apiPostForm('/api/rawsql/run', { sql: sql }, function (d) {
        renderResult(d);
    });
}
function clearResult() {
    document.getElementById('resultArea').innerHTML = '';
}
function renderResult(d) {
    var area = document.getElementById('resultArea');
    if (!d.success) {
        area.innerHTML =
            ""<div class='card'><div class='flash flash-error'><i class='fas fa-exclamation-triangle'></i> "" +
            window.escapeHtml(d.message || 'Error') + ""</div></div>"";
        return;
    }
    if (d.kind === 'execute') {
        area.innerHTML =
            ""<div class='card'><div class='flash flash-success'><i class='fas fa-check-circle'></i> "" +
            window.escapeHtml(d.message || 'OK') + ""</div></div>"";
        return;
    }
    // kind === 'select'
    var cols = d.columns || [];
    var rows = d.rows || [];
    var html = ""<div class='card'>"";
    html += ""<div class='card-header'><h3>Results <span class='muted small' style='font-weight:400'>("" +
        rows.length + "" rows)</span></h3></div>"";
    if (rows.length === 0) {
        html += ""<p class='muted'>(empty result set)</p></div>"";
        area.innerHTML = html;
        return;
    }
    html += ""<div style='overflow-x:auto'><table class='data-table'><thead><tr>"";
    for (var i = 0; i < cols.length; i++) html += ""<th>"" + window.escapeHtml(cols[i]) + ""</th>"";
    html += ""</tr></thead><tbody>"";
    for (var r = 0; r < rows.length; r++) {
        html += ""<tr>"";
        for (var c = 0; c < cols.length; c++) {
            var v = rows[r][cols[c]];
            html += ""<td>"" + (v === null || v === undefined ? ""<span class='muted'>null</span>"" : window.escapeHtml(String(v))) + ""</td>"";
        }
        html += ""</tr>"";
    }
    html += ""</tbody></table></div></div>"";
    area.innerHTML = html;
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
