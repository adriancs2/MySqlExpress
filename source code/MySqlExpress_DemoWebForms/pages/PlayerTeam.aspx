<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="PlayerTeam.aspx.cs" Inherits="System.pages.PlayerTeam" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
    <asp:PlaceHolder ID="phHead" runat="server"></asp:PlaceHolder>

    <script type="text/javascript">

        function selectAll(tablename, isselect) {
            let ipt = document.getElementById(tablename).getElementsByTagName("input");
            for (let i = 0; i < ipt.length; i++) {
                if (ipt[i].getAttribute("type") == "checkbox") {
                    ipt[i].checked = isselect;
                }
            }
        }

        function buildTeamPlayerTable(json) {

            let tbody = document.getElementById("tb1_tbody");
            tbody.replaceChildren();

            let pa = JSON.parse(json);

            if (pa.length == 0)
                return;

            for (let i = 0; i < pa.length; i++) {

                let p = pa[i];

                let tr = tbody.insertRow(-1);

                let td1 = tr.insertCell(-1);
                let td2 = tr.insertCell(-1);

                td1.innerHTML = `<input type='checkbox' playerid='${p.id}' />`;
                td2.innerHTML = `<a href='/PlayerEdit?id=${p.id}' target='_blank'>${p.name}</a>`;
            }
        }

        function loadTeamPlayers() {

            let xhttp = new XMLHttpRequest();
            xhttp.onload = function () {

                buildTeamPlayerTable(this.responseText);

            }

            xhttp.open("GET", `/apiPlayerTeam?action=1&year=${year}&teamid=${teamid}`, true);
            xhttp.send();
        }

        function searchPlayers() {

            let xhttp = new XMLHttpRequest();
            xhttp.onload = function () {

                let tbody = document.getElementById("tb2_tbody");
                tbody.replaceChildren();

                let pa = JSON.parse(this.responseText);

                if (pa.length == 0)
                    return;

                for (let i = 0; i < pa.length; i++) {

                    let p = pa[i];

                    let tr = tbody.insertRow(-1);

                    let td1 = tr.insertCell(-1);
                    let td2 = tr.insertCell(-1);
                    let td3 = tr.insertCell(-1);

                    td1.innerHTML = `<input type='checkbox' playerid='${p.id}' />`;
                    td2.innerHTML = `<a href='/PlayerEdit?id={p.id}' target='_blank'>${p.name}</a>`;
                    td3.innerHTML = p.teamname;
                }
            }

            let search = encodeURI(document.getElementById("txtSearch").value.trim());

            xhttp.open("GET", `/apiPlayerTeam?action=2&year=${year}&teamid=${teamid}&search=${search}`, true);
            xhttp.send();

        }

        function removePlayers() {
            let pid = "";
            let cbs = document.getElementById("tb1_tbody").getElementsByTagName("input");

            for (let i = 0; i < cbs.length; i++) {
                if (cbs[i].getAttribute("type") == "checkbox") {
                    if (cbs[i].checked) {
                        let thisPid = cbs[i].getAttribute("playerid");
                        if (pid.length > 0)
                            pid += ",";
                        pid += thisPid;
                    }
                }
            }

            if (pid.length == 0)
                return;

            let xhttp = new XMLHttpRequest();
            xhttp.onload = function () {

                document.getElementById("tb2_tbody").replaceChildren();
                buildTeamPlayerTable(this.responseText);
            }

            xhttp.open("GET", `/apiPlayerTeam?action=3&year=${year}&teamid=${teamid}&pid=${pid}`, true);
            xhttp.send();
        }

        function addPlayers() {
            let pid = "";
            let cbs = document.getElementById("tb2_tbody").getElementsByTagName("input");

            for (let i = 0; i < cbs.length; i++) {
                if (cbs[i].getAttribute("type") == "checkbox") {
                    if (cbs[i].checked) {
                        let thisPid = cbs[i].getAttribute("playerid");
                        if (pid.length > 0)
                            pid += ",";
                        pid += thisPid;
                    }
                }
            }

            if (pid.length == 0)
                return;

            let xhttp = new XMLHttpRequest();
            xhttp.onload = function () {

                buildTeamPlayerTable(this.responseText);
                searchPlayers();
            }

            xhttp.open("GET", `/apiPlayerTeam?action=4&year=${year}&teamid=${teamid}&pid=${pid}`, true);
            xhttp.send();
        }

        function keydownSearchPlayer(e) {
            if (e.keyCode == 13) {
                searchPlayers();
                return false;
            }
            return true;
        }

    </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Assign Player to Team
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <b>This page demonstrates the usage of javascript AJAX data loading and build HTML table dynamically.</b>

    <br /><br />

    <input type="submit" style="display: none;" aria-hidden="true" />

    <div class="heading1 margin_0">
        <h2>
            <asp:Label ID="lbTeamName" runat="server"></asp:Label>
        </h2>
        <hr />
    </div>

    <table style="width: 100%;">
        <tr>
            <td style="vertical-align: top; width: 40%;">

                <div class="heading1 margin_0">
                    <h2>Joined Team Player</h2>
                </div>

                <a href="#" onclick="removePlayers(); return false;" class="btn cur-p btn-primary">Remove Selected Player</a>

                <br />
                <br />

                [ <a href='#' onclick="selectAll('tb1', true); return false;">Select All</a> |
                <a href='#' onclick="selectAll('tb1', false); return false;">Clear Selection</a> ]

                <table id='tb1' class="table table-striped">
                    <thead>
                        <tr>
                            <th>Remove</th>
                            <th>Name</th>
                        </tr>
                    </thead>
                    <tbody id='tb1_tbody'>
                    </tbody>
                </table>

            </td>
            <td style="width: 2%; border-right: 1px solid #d3d3d3;"></td>
            <td style="width: 2%;"></td>
            <td style="vertical-align: top; width: 56%;">

                <div class="heading1 margin_0">
                    <h2>Search Players</h2>
                </div>

                <div class="tableform">
                    <a href="#" onclick="searchPlayers(); return false;" class="btn cur-p btn-primary">Search</a>
                    <input type="text" id="txtSearch" onkeydown="return keydownSearchPlayer(event);" placeholder="Player's Name, Code, Email, Tel" />
                    <a href="#" onclick="addPlayers(); return false;" class="btn cur-p btn-primary">Add Selected Players</a>
                </div>

                <br />

                [ <a href='#' onclick="selectAll('tb2', true); return false;">Select All</a> |
                <a href='#' onclick="selectAll('tb2', false); return false;">Clear Selection</a> ]

                <table id='tb2' class="table table-striped">
                    <thead>
                        <tr>
                            <th>Add</th>
                            <th>Name</th>
                            <th>Joined Team</th>
                        </tr>
                    </thead>
                    <tbody id="tb2_tbody">
                    </tbody>
                </table>

            </td>
        </tr>
    </table>

    <script type="text/javascript">

        loadTeamPlayers();

    </script>

</asp:Content>
