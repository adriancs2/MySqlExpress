using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.models;

namespace System.handlers
{
    /// <summary>
    /// Player edit form. Single handler for both "new" (/players/new)
    /// and "edit" (/players/edit/{id}) — the URL drives the mode.
    ///
    /// Shows GetObject&lt;T&gt; on load.
    /// The save API (PlayerEditApi.Save) shows Insert / Update.
    /// </summary>
    public static class PlayerEdit
    {
        public static void HandleRequest()
        {
            if (!Config.HasConnString)
            {
                Render.NotConfigured(); return;
            }

            string path = (HttpContext.Current.Request.Path ?? "").ToLowerInvariant().Trim();
            int id = ParseIdFromPath(path);

            obPlayer p = new obPlayer();
            if (id > 0)
            {
                try
                {
                    p = Db.Get(m => m.GetObject<obPlayer>(
                        "select * from player where id = @vid;",
                        new Dictionary<string, object> { ["@vid"] = id }));
                }
                catch (Exception ex)
                {
                    Render.Error("Edit Player", "players", ex.Message);
                    return;
                }

                if (p == null || p.Id == 0)
                {
                    Render.Error("Edit Player", "players", "Player not found.");
                    return;
                }
            }

            bool isNew = p.Id == 0;
            string title = isNew ? "Add Player" : "Edit Player: " + p.Name;
            string activeNav = isNew ? "player-new" : "players";

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header(title, activeNav));

            string dateValue = p.DateRegister == DateTime.MinValue
                ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm")
                : p.DateRegister.ToString("yyyy-MM-ddTHH:mm");

            sb.Append($@"
<div id='flashArea'></div>

<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-{(isNew ? "user-plus" : "user-edit")}'></i> {WebUtility.HtmlEncode(title)}</h2>
        <a class='btn btn-secondary btn-sm' href='/players'>
            <i class='fas fa-arrow-left'></i> Back to list
        </a>
    </div>

    <input type='hidden' id='playerId' value='{p.Id}' />

    <div class='form-row'>
        <div class='form-field' style='max-width:180px'>
            <label>Code</label>
            <input type='text' id='code' value='{WebUtility.HtmlEncode(p.Code)}' maxlength='20' />
        </div>
        <div class='form-field'>
            <label>Name</label>
            <input type='text' id='name' value='{WebUtility.HtmlEncode(p.Name)}' maxlength='200' />
        </div>
    </div>

    <div class='form-row'>
        <div class='form-field'>
            <label>Email</label>
            <input type='email' id='email' value='{WebUtility.HtmlEncode(p.Email)}' maxlength='150' />
        </div>
        <div class='form-field' style='max-width:180px'>
            <label>Tel</label>
            <input type='text' id='tel' value='{WebUtility.HtmlEncode(p.Tel)}' maxlength='50' />
        </div>
    </div>

    <div class='form-row'>
        <div class='form-field' style='max-width:240px'>
            <label>Registered</label>
            <input type='datetime-local' id='dateRegister' value='{dateValue}' />
        </div>
        <div class='form-field' style='max-width:180px'>
            <label>Status</label>
            <select id='status'>
                <option value='1' {(p.Status == 1 ? "selected" : "")}>Active</option>
                <option value='0' {(p.Status == 0 ? "selected" : "")}>Inactive</option>
            </select>
        </div>
    </div>

    <div class='form-actions'>
        <button class='btn btn-success' onclick='savePlayer()'>
            <i class='fas fa-save'></i> {(isNew ? "Create" : "Save Changes")}
        </button>
        <a class='btn btn-secondary' href='/players'>Cancel</a>
    </div>
</div>

<div class='card'>
    <h3><i class='fas fa-code'></i> What this page shows</h3>
    <p class='muted'>
        {(isNew
            ? "On submit, the handler calls <code>m.Insert(\"player\", dic)</code>. The <code>LastInsertId</code> property retrieves the new row's ID afterwards."
            : "The form was pre-populated with <code>m.GetObject&lt;obPlayer&gt;(sql, params)</code>. On save, the handler uses <code>m.Update(\"player\", data, \"id\", id)</code> &mdash; the single-condition overload that appends <code>LIMIT 1</code> for safety.")}
        See <code>engine/handlers/PlayerEditApi.cs</code>.
    </p>
</div>
");

            sb.Append(@"
<script>
function savePlayer() {
    window.clearFlash();
    var payload = {
        id:           document.getElementById('playerId').value,
        code:         document.getElementById('code').value,
        name:         document.getElementById('name').value,
        email:        document.getElementById('email').value,
        tel:          document.getElementById('tel').value,
        dateRegister: document.getElementById('dateRegister').value,
        status:       document.getElementById('status').value
    };
    window.apiPostForm('/api/players/save', payload, function (d) {
        if (d.success) {
            window.location.href = '/players';
        } else {
            window.flash('error', d.message || 'Save failed');
        }
    });
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

        /// <summary>
        /// /players/edit/{id} — pick the number off the tail.
        /// Returns 0 if the path has no numeric suffix ("new" or empty).
        /// </summary>
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
