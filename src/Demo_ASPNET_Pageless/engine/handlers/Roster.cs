using MySqlConnector;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using System.models;

namespace System.handlers
{
    /// <summary>
    /// Roster page — the "JOIN into a custom POCO" + "composite-PK upsert"
    /// showcase. This is the same JOIN example shown in the MySqlExpress
    /// README, rendered live with demo data.
    /// </summary>
    public static class Roster
    {
        public static void HandleRequest()
        {
            if (!Config.HasConnString) { Render.NotConfigured(); return; }

            int year = ParseYear();

            List<obRosterRow> rows = new List<obRosterRow>();
            List<obPlayer>    players = new List<obPlayer>();
            List<obTeam>      teams = new List<obTeam>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        // Put a note for readers right at the source: this is the
                        // exact JOIN from the README, landing directly in obRosterRow.
                        rows = m.GetObjectList<obRosterRow>(@"
                            select a.id, a.code, a.name, b.year, b.score, b.level,
                                   c.name as teamname, c.code as teamcode, c.id as teamid
                            from player a
                            inner join player_team b on a.id = b.player_id
                            inner join team c on b.team_id = c.id
                            where b.year = @year
                            order by b.score desc;",
                            new Dictionary<string, object> { ["@year"] = year });

                        players = m.GetObjectList<obPlayer>(
                            "select * from player order by name;");
                        teams = m.GetObjectList<obTeam>(
                            "select * from team order by name;");
                    }
                }
            }
            catch (Exception ex)
            {
                Render.Error("Roster", "roster", ex.Message);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header("Roster (Join + Upsert)", "roster"));

            sb.Append($@"
<div id='flashArea'></div>

<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-list-ol'></i> Roster for {year}</h2>
        <form method='get' action='/roster' style='display:flex; gap:8px; align-items:center; margin:0;'>
            <label class='small muted' style='margin:0'>Year:</label>
            <input type='number' name='year' value='{year}' min='2000' max='2100'
                   style='width:100px; padding:6px 10px; border:1px solid #d0d9e3; border-radius:5px;' />
            <button type='submit' class='btn btn-sm btn-secondary'>View</button>
        </form>
    </div>

    <div class='section-note'>
        <i class='fas fa-code'></i> This table is the result of a 3-way JOIN
        (<code>player &rarr; player_team &rarr; team</code>), projected directly
        into <code>List&lt;obRosterRow&gt;</code> by
        <code>m.GetObjectList&lt;T&gt;</code>. The POCO's field names match the
        column aliases in the <code>SELECT</code>. See
        <code>engine/handlers/Roster.cs</code>.
    </div>

    <table class='data-table'>
        <thead>
            <tr>
                <th style='width:90px'>Player Code</th>
                <th>Player</th>
                <th>Team</th>
                <th style='width:90px'>Score</th>
                <th style='width:90px'>Level</th>
                <th class='actions' style='width:120px'>Actions</th>
            </tr>
        </thead>
        <tbody>");

            if (rows.Count == 0)
            {
                sb.Append($"<tr class='empty-row'><td colspan='6'>No roster entries for {year}.</td></tr>");
            }
            else
            {
                foreach (var r in rows)
                {
                    sb.Append($@"
            <tr>
                <td class='mono'>{WebUtility.HtmlEncode(r.Code)}</td>
                <td>{WebUtility.HtmlEncode(r.Name)}</td>
                <td>
                    <span class='badge badge-blue'>{WebUtility.HtmlEncode(r.Teamcode)}</span>
                    {WebUtility.HtmlEncode(r.Teamname)}
                </td>
                <td class='mono'>{r.Score:0.00}</td>
                <td class='mono'>{r.Level}</td>
                <td class='actions'>
                    <button class='btn btn-sm btn-danger'
                            onclick='removeRoster({year}, {r.Id})'>
                        <i class='fas fa-trash'></i> Remove
                    </button>
                </td>
            </tr>");
                }
            }

            sb.Append($@"
        </tbody>
    </table>
</div>

<div class='card'>
    <h2><i class='fas fa-user-plus'></i> Assign Player to Team ({year})</h2>

    <div class='section-note'>
        <i class='fas fa-lightbulb'></i> This form calls
        <code>m.InsertUpdate(&quot;player_team&quot;, dic, cols)</code>.
        The primary key is <code>(year, player_id)</code>; if that pair
        already exists, the listed columns update on the fly. If not, a new
        row is inserted. One call, either outcome.
    </div>

    <div class='form-row'>
        <div class='form-field'>
            <label>Player</label>
            <select id='rosterPlayer'>{BuildPlayerOptions(players)}</select>
        </div>
        <div class='form-field'>
            <label>Team</label>
            <select id='rosterTeam'>{BuildTeamOptions(teams)}</select>
        </div>
        <div class='form-field' style='max-width:120px'>
            <label>Score</label>
            <input type='number' id='rosterScore' value='80.0' step='0.1' />
        </div>
        <div class='form-field' style='max-width:100px'>
            <label>Level</label>
            <input type='number' id='rosterLevel' value='3' min='1' max='10' />
        </div>
    </div>

    <div class='form-actions'>
        <button class='btn btn-success' onclick='assignRoster({year})'>
            <i class='fas fa-save'></i> Assign / Update
        </button>
    </div>
</div>
");
            sb.Append(@"
<script>
async function assignRoster(year) {
    window.clearFlash();

    const formData = new FormData();
    formData.append('year',     year);
    formData.append('playerId', document.getElementById('rosterPlayer').value);
    formData.append('teamId',   document.getElementById('rosterTeam').value);
    formData.append('score',    document.getElementById('rosterScore').value);
    formData.append('level',    document.getElementById('rosterLevel').value);

    try {
        const response = await fetch('/api/roster/assign', {
            method: 'POST',
            body: formData
        });
        const d = await response.json();
        if (d.success) window.location.reload();
        else           window.flash('error', d.message || 'Assign failed');
    } catch (err) {
        console.error('POST failed', err);
        window.flash('error', 'Network error.');
    }
}
async function removeRoster(year, playerId) {
    if (!confirm('Remove this roster entry?')) return;

    const formData = new FormData();
    formData.append('year',     year);
    formData.append('playerId', playerId);

    try {
        const response = await fetch('/api/roster/delete', {
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

        static int ParseYear()
        {
            string raw = HttpContext.Current.Request.QueryString["year"] ?? "";
            int y;
            if (int.TryParse(raw, out y) && y >= 2000 && y <= 2100) return y;
            return 2024;
        }

        static string BuildPlayerOptions(List<obPlayer> players)
        {
            if (players.Count == 0)
                return "<option value=''>(no players)</option>";

            StringBuilder sb = new StringBuilder();
            foreach (var p in players)
            {
                sb.Append($"<option value='{p.Id}'>{WebUtility.HtmlEncode(p.Code)} &mdash; {WebUtility.HtmlEncode(p.Name)}</option>");
            }
            return sb.ToString();
        }

        static string BuildTeamOptions(List<obTeam> teams)
        {
            if (teams.Count == 0)
                return "<option value=''>(no teams)</option>";

            StringBuilder sb = new StringBuilder();
            foreach (var t in teams)
            {
                sb.Append($"<option value='{t.Id}'>{WebUtility.HtmlEncode(t.Code)} &mdash; {WebUtility.HtmlEncode(t.Name)}</option>");
            }
            return sb.ToString();
        }
    }
}
