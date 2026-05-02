using MySqlConnector;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.models;

namespace System.handlers
{
    public static class TeamEdit
    {
        public static void HandleRequest()
        {
            if (!Config.HasConnString) { Render.NotConfigured(); return; }

            string path = (HttpContext.Current.Request.Path ?? "").ToLowerInvariant().Trim();
            int id = ParseIdFromPath(path);

            obTeam t = new obTeam();
            if (id > 0)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.Connection = conn;
                            MySqlExpress m = new MySqlExpress(cmd);

                            t = m.GetObject<obTeam>(
                                "select * from team where id = @vid;",
                                new Dictionary<string, object> { ["@vid"] = id });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Render.Error("Edit Team", "teams", ex.Message);
                    return;
                }

                if (t == null || t.Id == 0)
                {
                    Render.Error("Edit Team", "teams", "Team not found.");
                    return;
                }
            }

            bool isNew = t.Id == 0;
            string title = isNew ? "Add Team" : "Edit Team: " + t.Name;
            string activeNav = isNew ? "team-new" : "teams";

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header(title, activeNav));

            sb.Append($@"
<div id='flashArea'></div>

<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-{(isNew ? "plus-circle" : "edit")}'></i> {WebUtility.HtmlEncode(title)}</h2>
        <a class='btn btn-secondary btn-sm' href='/teams'><i class='fas fa-arrow-left'></i> Back to list</a>
    </div>

    <input type='hidden' id='teamId' value='{t.Id}' />

    <div class='form-row'>
        <div class='form-field' style='max-width:180px'>
            <label>Code</label>
            <input type='text' id='code' value='{WebUtility.HtmlEncode(t.Code)}' maxlength='20' />
        </div>
        <div class='form-field'>
            <label>Name</label>
            <input type='text' id='name' value='{WebUtility.HtmlEncode(t.Name)}' maxlength='200' />
        </div>
    </div>

    <div class='form-row'>
        <div class='form-field'>
            <label>City</label>
            <input type='text' id='city' value='{WebUtility.HtmlEncode(t.City)}' maxlength='100' />
        </div>
        <div class='form-field' style='max-width:180px'>
            <label>Status</label>
            <select id='status'>
                <option value='1' {(t.Status == 1 ? "selected" : "")}>Active</option>
                <option value='0' {(t.Status == 0 ? "selected" : "")}>Inactive</option>
            </select>
        </div>
    </div>

    <div class='form-actions'>
        <button class='btn btn-success' onclick='saveTeam()'>
            <i class='fas fa-save'></i> {(isNew ? "Create" : "Save Changes")}
        </button>
        <a class='btn btn-secondary' href='/teams'>Cancel</a>
    </div>
</div>
");
            sb.Append(@"
<script>
async function saveTeam() {
    window.clearFlash();

    const formData = new FormData();
    formData.append('id',     document.getElementById('teamId').value);
    formData.append('code',   document.getElementById('code').value);
    formData.append('name',   document.getElementById('name').value);
    formData.append('city',   document.getElementById('city').value);
    formData.append('status', document.getElementById('status').value);

    try {
        const response = await fetch('/api/teams/save', {
            method: 'POST',
            body: formData
        });
        const d = await response.json();
        if (d.success) window.location.href = '/teams';
        else           window.flash('error', d.message || 'Save failed');
    } catch (err) {
        console.error('POST failed', err);
        window.flash('error', 'Network error.');
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

        static int ParseIdFromPath(string path)
        {
            int slash = path.LastIndexOf('/');
            if (slash < 0 || slash == path.Length - 1) return 0;
            string tail = path.Substring(slash + 1);
            int id;
            return int.TryParse(tail, out id) ? id : 0;
        }
    }
}
