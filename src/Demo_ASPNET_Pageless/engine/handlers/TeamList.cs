using MySqlConnector;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.models;

namespace System.handlers
{
    /// <summary>
    /// Team list — the simpler sibling of PlayerList.
    /// Plain GetObjectList&lt;obTeam&gt;, no search.
    /// </summary>
    public static class TeamList
    {
        public static void HandleRequest()
        {
            if (!Config.HasConnString) { Render.NotConfigured(); return; }

            List<obTeam> rows;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        rows = m.GetObjectList<obTeam>(
                            "select * from team order by id desc;");
                    }
                }
            }
            catch (Exception ex)
            {
                Render.Error("Teams", "teams", ex.Message);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header("Team List", "teams"));

            sb.Append($@"
<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-shield-alt'></i> Teams <span class='muted small' style='font-weight:400'>({rows.Count})</span></h2>
        <a class='btn btn-primary' href='/teams/new'><i class='fas fa-plus'></i> Add Team</a>
    </div>

    <table class='data-table'>
        <thead>
            <tr>
                <th style='width:90px'>Code</th>
                <th>Name</th>
                <th>City</th>
                <th style='width:90px'>Status</th>
                <th class='actions' style='width:170px'>Actions</th>
            </tr>
        </thead>
        <tbody>");

            if (rows.Count == 0)
            {
                sb.Append("<tr class='empty-row'><td colspan='5'>No teams yet.</td></tr>");
            }
            else
            {
                foreach (var t in rows)
                {
                    string statusBadge = t.Status == 1
                        ? "<span class='badge badge-green'>Active</span>"
                        : "<span class='badge badge-gray'>Inactive</span>";
                    sb.Append($@"
            <tr>
                <td class='mono'>{WebUtility.HtmlEncode(t.Code)}</td>
                <td>{WebUtility.HtmlEncode(t.Name)}</td>
                <td>{WebUtility.HtmlEncode(t.City)}</td>
                <td>{statusBadge}</td>
                <td class='actions'>
                    <a class='btn btn-sm btn-secondary' href='/teams/edit/{t.Id}'><i class='fas fa-edit'></i> Edit</a>
                    <button class='btn btn-sm btn-danger' onclick='confirmDeleteTeam({t.Id})'><i class='fas fa-trash'></i></button>
                </td>
            </tr>");
                }
            }

            sb.Append(@"
        </tbody>
    </table>
</div>

<script>
async function confirmDeleteTeam(id) {
    if (!confirm('Delete this team? Roster entries referencing it will also be removed.')) return;

    const formData = new FormData();
    formData.append('id', id);

    try {
        const response = await fetch('/api/teams/delete', {
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

            var res = HttpContext.Current.Response;
            res.ContentType = "text/html"; res.Charset = "utf-8";
            res.Write(sb.ToString());
            ApiHelper.EndResponse();
        }
    }
}
