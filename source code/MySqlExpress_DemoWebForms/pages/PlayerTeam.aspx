<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="PlayerTeam.aspx.cs" Inherits="System.pages.PlayerTeam" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
    <asp:PlaceHolder ID="phHead" runat="server"></asp:PlaceHolder>

    <style type="text/css">
        .teaminfo_maincontainer {
            height: 200px;
        }

        .teaminfo {
            width: 200px;
            padding: 15px 25px;
            display: block;
            border-radius: 25px;
            text-align: center;
        }

            .teaminfo:hover {
                box-shadow: 1px 1px 12px #a9a9a9;
                position: relative;
                top: 2px;
                bottom: 2px;
                background: #e0f1ff;
            }

        .highlight {
            color: #9c0000;
            font-weight: bold;
        }
    </style>

    <script type="text/javascript">

        var year = 0;
        var teamid = 0;

        // A function to download Team Info
        function loadTeamInfo() {

            // obtain the query string
            const queryParams = new URLSearchParams(window.location.search)

            // get the year and teamid from query string
            year = queryParams.get("year");
            teamid = queryParams.get("teamid");

            // if the year or teamid is empty or blank or not defined
            // redirect the user to the "TeamList" page
            if (year == null || teamid == null || year == 0 || teamid == 0) {
                window.location.href = "/TeamList";
            }

            // declare a team object container
            let team = null;

            // initiate an AJAX to call an API
            let xhttp = new XMLHttpRequest();

            // After the AJAX finished downloading the data
            xhttp.onload = function () {

                // data returned in JSON string format from API
                let result = this.responseText;

                // convert JSON string to javascript object
                team = JSON.parse(result);

                // if the team object's ID is zero,
                // which means the requested team info is not existed
                // then redirect the user to "TeamList" page
                if (team.Id == 0) {
                    window.location.href = "/TeamList";
                }

                // display the Team Logo and Info to the UI
                document.getElementById("h2teamname").innerHTML = `
                    <a href='/TeamEdit?id=${team.Id}' class='teaminfo'>
                        ${team.ImgLogo}<br />
                        ${team.Name}
                    </a> ${year}
                    (Team Code: ${team.Code})`;
            }

            // executes the calling of AJAX to the API (url) to download data
            xhttp.open("GET", `/apiPlayerTeam?action=5&year=${year}&teamid=${teamid}`, true);
            xhttp.send();
        }

        // A function to select all the checkboxs
        function selectAll(tablename, isselect) {

            // get the table element
            let tableObject = document.getElementById(tablename);

            // get all the checkboxs within the table
            let ipt = tableObject.getElementsByTagName("input");

            // loop through all the checkboxs one by one
            for (let i = 0; i < ipt.length; i++) {

                // check or uncheck the checkbox
                if (ipt[i].getAttribute("type") == "checkbox") {
                    ipt[i].checked = isselect;
                }
            }
        }

        // Build the HTML table rows for Joined Players
        function buildTeamPlayerTable(json) {

            // Get the TBODY element of the table
            let tbody = document.getElementById("tb1_tbody");

            // Remove all rows (reset)
            tbody.replaceChildren();

            // Convert the JSON string into javascript list of objects
            let pa = JSON.parse(json);

            // If the list has no record, terminate the process
            if (pa.length == 0)
                return;

            // Loop through all the objects in the list
            for (let i = 0; i < pa.length; i++) {

                // Get the object
                let p = pa[i];

                // Add new row
                let tr = tbody.insertRow(-1);

                // Add new cells
                let td1 = tr.insertCell(-1);
                let td2 = tr.insertCell(-1);
                let td3 = tr.insertCell(-1);

                // Add write the content into the cells
                td1.innerHTML = `<input type='checkbox' playerid='${p.Id}' />`;
                td2.innerHTML = `<a href='/PlayerEdit?id=${p.Id}' target='_blank'>${p.Name}</a>`;
                td3.innerText = p.Code;
            }
        }

        // Load table of Joined Players
        function loadTeamPlayers() {

            // initiate an AJAX to call an API
            let xhttp = new XMLHttpRequest();

            // After the AJAX finished downloading the data
            xhttp.onload = function () {

                // Pass the downloaded JSON string to another function to build the table
                buildTeamPlayerTable(this.responseText);
            }

            // Executes the AJAX to download data from API
            xhttp.open("GET", `/apiPlayerTeam?action=1&year=${year}&teamid=${teamid}`, true);
            xhttp.send();
        }

        // Perform searching other players
        function searchPlayers() {

            // initiate an AJAX to call an API
            let xhttp = new XMLHttpRequest();

            // After the AJAX finished downloading the data
            xhttp.onload = function () {

                // Get the TBODY element of the table
                let tbody = document.getElementById("tb2_tbody");

                // Remove all rows
                tbody.replaceChildren();

                // Convert the downloaded JSON string into javascript list of objects
                let pa = JSON.parse(this.responseText);

                // If the list is empty, terminate process
                if (pa.length == 0)
                    return;

                // Loop through all the objects in the list
                for (let i = 0; i < pa.length; i++) {

                    // Get the player object
                    let p = pa[i];

                    // Add new row
                    let tr = tbody.insertRow(-1);

                    // Add new cells
                    let td1 = tr.insertCell(-1);
                    let td2 = tr.insertCell(-1);
                    let td3 = tr.insertCell(-1);
                    let td4 = tr.insertCell(-1);

                    // Insert content into the cells
                    td1.innerHTML = `<input type='checkbox' playerid='${p.Id}' />`;
                    td2.innerHTML = `<a href='/PlayerEdit?id={p.id}' target='_blank'>${p.Name}</a>`;
                    td3.innerText = p.Code;
                    td4.innerText = p.Teamname;
                }
            }

            // Get the element of search textbox
            let txtSearch = document.getElementById("txtSearch");

            // Get the user input for searching
            let search = encodeURI(txtSearch.value.trim());

            // Executes the AJAX for API call to download data
            xhttp.open("GET", `/apiPlayerTeam?action=2&year=${year}&teamid=${teamid}&search=${search}`, true);
            xhttp.send();

        }

        // Remove players from joined list
        function removePlayers() {

            // declare an object for collection of player id that are going to be removed
            let pid = "";

            // Get the element of TBODY from table 1
            let tb1_tbody = document.getElementById("tb1_tbody");

            // Get all the checkbox within the element of TBODY
            let cbs = tb1_tbody.getElementsByTagName("input");

            // Loop through all the checkboxs
            for (let i = 0; i < cbs.length; i++) {
                if (cbs[i].getAttribute("type") == "checkbox") {

                    // If the checkbox is checked
                    if (cbs[i].checked) {

                        // Get the player's id
                        let thisPid = cbs[i].getAttribute("playerid");

                        // Append the player's id into the collection
                        if (pid.length > 0)
                            pid += ",";
                        pid += thisPid;
                    }
                }
            }

            // No player is marked for removal, terminate process
            if (pid.length == 0)
                return;

            // Initiate an AJAX for API call to download data
            let xhttp = new XMLHttpRequest();

            // Upon the AJAX finished download data
            xhttp.onload = function () {

                // Get the TBODY object from table 2
                let tb2_tbody = document.getElementById("tb2_tbody");

                // Removes all rows
                tb2_tbody.replaceChildren();

                // Pass the downloaded JSON string to another another to build the HTML table
                buildTeamPlayerTable(this.responseText);
            }

            // Execute the AJAX for API call to get data
            xhttp.open("GET", `/apiPlayerTeam?action=3&year=${year}&teamid=${teamid}&pid=${pid}`, true);
            xhttp.send();
        }

        // Add selected players at the search section into the team
        function addPlayers() {

            // declare an string object for collection of player id that are going to be added
            let pid = "";

            // get all the checkboxs from table2
            let cbs = document.getElementById("tb2_tbody").getElementsByTagName("input");

            // loop through all the checkboxs
            for (let i = 0; i < cbs.length; i++) {
                if (cbs[i].getAttribute("type") == "checkbox") {

                    // if the checkbox is checked, get the player id and add it into the list
                    if (cbs[i].checked) {

                        // get the player id
                        let thisPid = cbs[i].getAttribute("playerid");

                        // append the player id into the string collection list
                        if (pid.length > 0)
                            pid += ",";
                        pid += thisPid;
                    }
                }
            }

            // if there is no player id collected, terminate process
            if (pid.length == 0)
                return;

            // Initiate an AJAX for API call
            let xhttp = new XMLHttpRequest();

            // upon the completion of data download
            xhttp.onload = function () {

                // pass the downloaded JSON string to another function to build the table
                buildTeamPlayerTable(this.responseText);

                // refresh the search list
                searchPlayers();
            }

            // Executes the AJAX for API call to download data
            xhttp.open("GET", `/apiPlayerTeam?action=4&year=${year}&teamid=${teamid}&pid=${pid}`, true);
            xhttp.send();
        }

        // detect the [Enter] key press, execute search player
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

    <b>This page demonstrates the usage of <span class="highlight">Javascript AJAX</span> by calling api for data loading at client side web browser and build HTML table dynamically.</b>

    <br />
    <br />

    <input type="submit" style="display: none;" aria-hidden="true" />

    <div class="heading1 margin_0 teaminfo_maincontainer">
        <h2 id="h2teamname"></h2>
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
                            <th>Code</th>
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
                    <input type="text" id="txtSearch" onkeydown="return keydownSearchPlayer(event);" style="width: 200px;" placeholder="Player's Name, Code, Email, Tel" />
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
                            <th>Code</th>
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

        // upon first loading of the page

        // load the team info
        loadTeamInfo();

        // load the joined player list
        loadTeamPlayers();

    </script>

</asp:Content>
