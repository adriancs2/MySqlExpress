using MySqlConnector;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Web;

namespace System.handlers
{
    /// <summary>
    /// Home = Setup + Dashboard.
    ///
    /// When there is no connection string yet, the page is a setup form.
    /// When the connection string is set, the page becomes a dashboard
    /// with counts (ExecuteScalar demos) and table actions.
    /// </summary>
    public static class Home
    {
        public static void HandleRequest()
        {
            HttpResponse res = HttpContext.Current.Response;
            res.ContentType = "text/html";
            res.Charset = "utf-8";

            StringBuilder sb = new StringBuilder();
            sb.Append(SiteTemplate.Header("Setup / Dashboard", "home"));

            if (!Config.HasConnString)
            {
                AppendSetupForm(sb, initialMode: true);
            }
            else
            {
                AppendDashboard(sb);
            }

            sb.Append(SiteTemplate.Footer());
            res.Write(sb.ToString());
            ApiHelper.EndResponse();
        }

        // -----------------------------------------------------------
        // Setup form (no connection string yet)
        // -----------------------------------------------------------

        static void AppendSetupForm(StringBuilder sb, bool initialMode)
        {
            string defaultConn = "server=localhost;user=root;pwd=1234;database=mysqlexpress_demo;convertzerodatetime=true;treattinyasboolean=true;";

            sb.Append($@"
<div id='flashArea'></div>

<div class='card'>
    <h2><i class='fas fa-plug'></i> Step 1 &mdash; Connection String</h2>
    <p class='muted mb-2'>
        Enter your MySQL connection string. It is persisted to
        <code>/App_Data/mysql_conn.txt</code>. This is a demo; in production,
        you would use <code>Web.config</code>, environment variables, or a secrets manager.
    </p>
    <div class='form-row'>
        <div class='form-field'>
            <label>MySQL Connection String</label>
            <input type='text' id='connStr' value='{WebUtility.HtmlEncode(defaultConn)}' />
            <span class='hint'>
                Format: <code>server=...;user=...;pwd=...;database=...;</code>
            </span>
        </div>
    </div>
    <div class='form-actions'>
        <button type='button' class='btn btn-secondary' onclick='testConn()'>
            <i class='fas fa-vial'></i> Test Connection
        </button>
        <button type='button' class='btn btn-primary' onclick='saveConn()'>
            <i class='fas fa-save'></i> Save &amp; Continue
        </button>
    </div>
</div>

<div class='card'>
    <h2><i class='fas fa-info-circle'></i> What this page does</h2>
    <p class='muted'>
        Once a connection is saved, this page switches to a dashboard where
        you can create the demo tables, seed sample data, and navigate to
        the feature showcases. Each table is created with a single
        <code>m.Execute(createSql)</code> call &mdash; MySqlExpress at work on its
        own setup.
    </p>
</div>
");
            sb.Append(@"
<script>
async function testConn() {
    window.clearFlash();

    const formData = new FormData();
    formData.append('connStr', document.getElementById('connStr').value);

    try {
        const response = await fetch('/api/setup/test-conn', {
            method: 'POST',
            body: formData
        });
        const d = await response.json();
        if (d.success) window.flash('success', d.message || 'Connection OK');
        else           window.flash('error',   d.message || 'Connection failed');
    } catch (err) {
        console.error('POST failed', err);
        window.flash('error', 'Network error.');
    }
}
async function saveConn() {
    window.clearFlash();

    const formData = new FormData();
    formData.append('connStr', document.getElementById('connStr').value);

    try {
        const response = await fetch('/api/setup/save-conn', {
            method: 'POST',
            body: formData
        });
        const d = await response.json();
        if (d.success) window.location.reload();
        else           window.flash('error', d.message || 'Save failed');
    } catch (err) {
        console.error('POST failed', err);
        window.flash('error', 'Network error.');
    }
}
</script>
");
        }

        // -----------------------------------------------------------
        // Dashboard (connection string configured)
        // -----------------------------------------------------------

        static void AppendDashboard(StringBuilder sb)
        {
            // Try to collect table counts via ExecuteScalar<int>.
            // If tables don't exist yet, we fall back to "—".
            int playerCount = -1, teamCount = -1, rosterCount = -1;
            string dbError = "";
            bool tablesReady = false;

            bool dbNotExistsDetected = false;
            string successCreatedDB = "";

            // Test Database and connection existed
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                }
            }
            catch (Exception exFirstLaunch)
            {
                if (exFirstLaunch.Message.Contains("Unknown database"))
                {
                    dbNotExistsDetected = true;
                }
            }

            // Attemp to automatic create database
            if (dbNotExistsDetected)
            {
                try
                {
                    MySqlConnectionStringBuilder connSB = new MySqlConnectionStringBuilder(Config.ConnString);
                    string targetDB = connSB.Database;
                    connSB.Database = "";
                    using (MySqlConnection conn = new MySqlConnection(connSB.ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{targetDB.Replace("`", "``")}`";
                            cmd.ExecuteNonQuery();
                        }
                    }
                    successCreatedDB = SiteTemplate.SuccessBanner($"Database created: {targetDB}");
                }
                catch { }
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        MySqlExpress m = new MySqlExpress(cmd);

                        // Check which tables exist by peeking at GetTableList().
                        List<string> tables = m.GetTableList();
                        bool hasPlayer = tables.Contains("player");
                        bool hasTeam = tables.Contains("team");
                        bool hasPT = tables.Contains("player_team");

                        tablesReady = hasPlayer && hasTeam && hasPT;

                        if (hasPlayer)
                            playerCount = m.ExecuteScalar<int>("select count(*) from player;");

                        if (hasTeam)
                            teamCount = m.ExecuteScalar<int>("select count(*) from team;");

                        if (hasPT)
                            rosterCount = m.ExecuteScalar<int>("select count(*) from player_team;");
                    }
                }
            }
            catch (Exception ex)
            {
                dbError = ex.Message;
            }

            string playersDisplay = playerCount >= 0 ? playerCount.ToString() : "&mdash;";
            string teamsDisplay = teamCount >= 0 ? teamCount.ToString() : "&mdash;";
            string rosterDisplay = rosterCount >= 0 ? rosterCount.ToString() : "&mdash;";

            string connMasked = MaskConnString(Config.ConnString);

            // Top: stat grid
            sb.Append($@"
<div id='flashArea'></div>

<div class='stat-grid'>
    <div class='stat'>
        <div class='stat-icon green'><i class='fas fa-users'></i></div>
        <div>
            <div class='stat-num'>{playersDisplay}</div>
            <div class='stat-label'>Players</div>
        </div>
    </div>
    <div class='stat'>
        <div class='stat-icon blue'><i class='fas fa-shield-alt'></i></div>
        <div>
            <div class='stat-num'>{teamsDisplay}</div>
            <div class='stat-label'>Teams</div>
        </div>
    </div>
    <div class='stat'>
        <div class='stat-icon orange'><i class='fas fa-list-ol'></i></div>
        <div>
            <div class='stat-num'>{rosterDisplay}</div>
            <div class='stat-label'>Roster Entries</div>
        </div>
    </div>
    <div class='stat'>
        <div class='stat-icon purple'><i class='fas fa-database'></i></div>
        <div>
            <div class='stat-num'>{(tablesReady ? "Ready" : "Pending")}</div>
            <div class='stat-label'>Schema Status</div>
        </div>
    </div>
</div>
");
            if (!string.IsNullOrEmpty(successCreatedDB))
            {
                sb.Append(successCreatedDB);
            }

            if (!string.IsNullOrEmpty(dbError))
            {
                sb.Append(SiteTemplate.ErrorBanner("Database error: " + dbError));
            }

            // Setup actions
            sb.Append($@"
<div class='card'>
    <div class='card-header'>
        <h2><i class='fas fa-tools'></i> Database Setup</h2>
        <span class='badge badge-blue'>{WebUtility.HtmlEncode(connMasked)}</span>
    </div>

    <p class='muted mb-2'>
        Each action below is a direct MySqlExpress call. Check the source of
        <code>engine/handlers/SetupApi.cs</code> to see the SQL being executed.
    </p>

    <div class='form-actions' style='border-top: none; padding-top: 0; margin-top: 0;'>
        <button type='button' class='btn btn-success' onclick='createTables()'>
            <i class='fas fa-table'></i> Create Tables
        </button>
        <button type='button' class='btn btn-primary' onclick='seedData()'>
            <i class='fas fa-seedling'></i> Seed Sample Data
        </button>
        <button type='button' class='btn btn-danger' onclick='dropTables()'>
            <i class='fas fa-trash-alt'></i> Drop All Tables
        </button>
        <button type='button' class='btn btn-secondary' onclick='clearConn()'>
            <i class='fas fa-unlink'></i> Change Connection String
        </button>
    </div>
</div>

<div class='card'>
    <h3><i class='fas fa-code'></i> What each action does</h3>
    <div class='row'>
        <div class='col'>
            <p><strong>Create Tables</strong></p>
            <p class='muted small'>Calls <code>m.Execute(sql)</code> three times inside a transaction.
            The SQL lives in <code>engine/Schema.cs</code>. Idempotent &mdash; uses
            <code>create table if not exists</code>.</p>
        </div>
        <div class='col'>
            <p><strong>Seed Sample Data</strong></p>
            <p class='muted small'>Uses <code>m.Insert(table, dic)</code> for players and teams, and
            <code>m.InsertUpdate(&quot;player_team&quot;, dic, cols)</code> for roster rows &mdash;
            which is what the composite-PK upsert was built for.</p>
        </div>
    </div>
    <div class='row'>
        <div class='col'>
            <p><strong>Drop All Tables</strong></p>
            <p class='muted small'>Single <code>m.Execute(multiSql)</code>. Resets the demo.</p>
        </div>
        <div class='col'>
            <p><strong>Change Connection String</strong></p>
            <p class='muted small'>Clears <code>/App_Data/mysql_conn.txt</code> and returns to the
            setup form. Data in MySQL is untouched.</p>
        </div>
    </div>
</div>");

            sb.Append(@"

<script>
async function createTables() {
    window.clearFlash();
    try {
        const response = await fetch('/api/setup/create-tables', {
            method: 'POST',
            body: new FormData()
        });
        const d = await response.json();
        window.flash(d.success ? 'success' : 'error', d.message);
        if (d.success) setTimeout(function() { window.location.reload(); }, 700);
    } catch (err) {
        console.error('POST failed', err);
        window.flash('error', 'Network error.');
    }
}
async function seedData() {
    window.clearFlash();
    try {
        const response = await fetch('/api/setup/seed', {
            method: 'POST',
            body: new FormData()
        });
        const d = await response.json();
        window.flash(d.success ? 'success' : 'error', d.message);
        if (d.success) setTimeout(function() { window.location.reload(); }, 700);
    } catch (err) {
        console.error('POST failed', err);
        window.flash('error', 'Network error.');
    }
}
async function dropTables() {
    if (!confirm('Drop ALL demo tables? This cannot be undone.')) return;
    window.clearFlash();
    try {
        const response = await fetch('/api/setup/drop-tables', {
            method: 'POST',
            body: new FormData()
        });
        const d = await response.json();
        window.flash(d.success ? 'success' : 'error', d.message);
        if (d.success) setTimeout(function() { window.location.reload(); }, 700);
    } catch (err) {
        console.error('POST failed', err);
        window.flash('error', 'Network error.');
    }
}
async function clearConn() {
    if (!confirm('Clear saved connection string? Your MySQL data is not affected.')) return;
    try {
        const response = await fetch('/api/setup/clear-conn', {
            method: 'POST',
            body: new FormData()
        });
        const d = await response.json();
        if (d.success) window.location.reload();
    } catch (err) {
        console.error('POST failed', err);
    }
}
</script>
");
        }

        static string MaskConnString(string connStr)
        {
            if (string.IsNullOrEmpty(connStr)) return "(none)";
            // Hide password value but keep the rest visible.
            var parts = connStr.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                int eq = p.IndexOf('=');
                if (eq > 0)
                {
                    string key = p.Substring(0, eq).Trim().ToLowerInvariant();
                    if (key == "pwd" || key == "password")
                    {
                        parts[i] = p.Substring(0, eq + 1) + "***";
                    }
                }
            }
            return string.Join(";", parts);
        }
    }
}
