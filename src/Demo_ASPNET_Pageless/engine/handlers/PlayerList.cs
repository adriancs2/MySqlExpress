using MySqlConnector;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.models;

namespace System.handlers
{
    /// <summary>
    /// Player list page — shows GetObjectList&lt;T&gt; and the multi-word
    /// search helper GenerateContainsString working together.
    ///
    /// The table is rendered server-side (simpler, and the point here is
    /// to showcase the SQL layer, not JS frameworks).
    /// </summary>
    public static class PlayerList
    {
        public static void HandleRequest()
        {
            if (!Config.HasConnString)
            {
                Render.NotConfigured(); return;
            }

            string q = (HttpContext.Current.Request.QueryString["q"] ?? "").Trim();

            List<obPlayer> rows;
            try
            {
                rows = LoadPlayers(q);
            }
            catch (Exception ex)
            {
                Render.Error("Players", "players", ex.Message);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header("Player List", "players"));

            sb.Append($@"
<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-users'></i> Players <span class='muted small' style='font-weight:400'>({rows.Count})</span></h2>
        <a class='btn btn-primary' href='/players/new'><i class='fas fa-plus'></i> Add Player</a>
    </div>

    <form method='get' action='/players' class='search-bar'>
        <input type='text' name='q' value='{WebUtility.HtmlEncode(q)}'
               placeholder='Search by name (multi-word: e.g. &quot;john smith&quot;)' />
        <button type='submit' class='btn btn-secondary'>
            <i class='fas fa-search'></i> Search
        </button>
        { (string.IsNullOrEmpty(q) ? "" : "<a class='btn btn-secondary' href='/players'>Clear</a>") }
    </form>

    { (string.IsNullOrEmpty(q) ? "" : $@"
    <div class='section-note'>
        <i class='fas fa-code'></i> This search uses
        <code>m.GenerateContainsString(&quot;name&quot;, q, sb, dicParam)</code>,
        which splits the query on whitespace and builds a parameterized
        multi-word LIKE condition. Every token must match.
    </div>") }

    <table class='data-table'>
        <thead>
            <tr>
                <th style='width:90px'>Code</th>
                <th>Name</th>
                <th style='width:150px'>Email</th>
                <th style='width:120px'>Registered</th>
                <th style='width:90px'>Status</th>
                <th class='actions' style='width:170px'>Actions</th>
            </tr>
        </thead>
        <tbody>
");

            if (rows.Count == 0)
            {
                string msg = string.IsNullOrEmpty(q)
                    ? "No players yet. Add one to get started."
                    : "No players match that search.";
                sb.Append($"<tr class='empty-row'><td colspan='6'>{msg}</td></tr>");
            }
            else
            {
                foreach (var p in rows)
                {
                    string statusBadge = p.Status == 1
                        ? "<span class='badge badge-green'>Active</span>"
                        : "<span class='badge badge-gray'>Inactive</span>";
                    string dateDisplay = p.DateRegister == DateTime.MinValue
                        ? "<span class='muted'>&mdash;</span>"
                        : p.DateRegister.ToString("yyyy-MM-dd");

                    sb.Append($@"
            <tr>
                <td class='mono'>{WebUtility.HtmlEncode(p.Code)}</td>
                <td>{WebUtility.HtmlEncode(p.Name)}</td>
                <td class='mono'>{WebUtility.HtmlEncode(p.Email)}</td>
                <td>{dateDisplay}</td>
                <td>{statusBadge}</td>
                <td class='actions'>
                    <a class='btn btn-sm btn-secondary' href='/players/edit/{p.Id}'>
                        <i class='fas fa-edit'></i> Edit
                    </a>
                    <button class='btn btn-sm btn-danger'
                            onclick='confirmDeletePlayer({p.Id})'>
                        <i class='fas fa-trash'></i>
                    </button>
                </td>
            </tr>");
                }
            }

            sb.Append(@"
        </tbody>
    </table>
</div>

<div class='card'>
    <h3><i class='fas fa-code'></i> What this page shows</h3>
    <p class='muted'>
        The rows above are loaded with a single <code>m.GetObjectList&lt;obPlayer&gt;(sql, dic)</code>
        call &mdash; no <code>DataTable</code>, no manual row-by-row mapping.
        When a search term is present, the SQL is built with
        <code>GenerateContainsString</code>, which parameterizes each word as a
        separate <code>LIKE</code> clause. See <code>engine/handlers/PlayerList.cs</code>.
    </p>
</div>

<script>
async function confirmDeletePlayer(id) {
    if (!confirm('Delete this player?')) return;

    const formData = new FormData();
    formData.append('id', id);

    try {
        const response = await fetch('/api/players/delete', {
            method: 'POST',
            body: formData
        });
        const d = await response.json();
        if (d.success) window.location.reload();
        else           alert((d && d.message) || 'Delete failed');
    } catch (err) {
        console.error('POST failed', err);
        alert('Network error.');
    }
}
</script>
");

            sb.Append(SiteTemplate.Footer());

            HttpResponse res = HttpContext.Current.Response;
            res.ContentType = "text/html";
            res.Charset = "utf-8";
            res.Write(sb.ToString());
            ApiHelper.EndResponse();
        }

        static List<obPlayer> LoadPlayers(string q)
        {
            using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    MySqlExpress m = new MySqlExpress(cmd);

                    if (string.IsNullOrEmpty(q))
                    {
                        return m.GetObjectList<obPlayer>(
                            "select * from player order by id desc;");
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append("select * from player where 1=1");

                    var dic = new Dictionary<string, object>();
                    m.GenerateContainsString("name", q, sb, dic);

                    sb.Append(" order by id desc;");

                    return m.GetObjectList<obPlayer>(sb.ToString(), dic);
                }
            }
        }
    }
}
