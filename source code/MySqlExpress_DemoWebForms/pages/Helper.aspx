<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="Helper.aspx.cs" Inherits="System.pages.Helper" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
    <style type="text/css">
        .divTableList {
            height: 100%;
            width: 100%;
            border: 1px solid #808080;
            margin: 0;
            padding: 0;
            overflow: scroll;
        }

            .divTableList a {
                padding: 5px;
                background: white;
                display: block;
                font-weight: normal !important;
                color: #535353 !important;
            }

                .divTableList a:hover {
                    text-decoration: none !important;
                    font-style: normal !important;
                    background: #b9b9b9 !important;
                    color: black !important;
                }

                .divTableList a.active {
                    background: #535353 !important;
                    color: white !important;
                    text-decoration: none !important;
                    font-style: normal !important;
                }
    </style>
    <script type="text/javascript">
        function selectTable(tablename) {
            let ta = document.getElementById("divTableList").getElementsByTagName("a");
            for (let i = 0; i < ta.length; i++) {
                ta[i].className = "";
            }
            let taCurrent = document.getElementById(`table_${tablename}`);
            taCurrent.className = "active";
            loadTable(tablename);
        }

        function loadTable(tablename) {

            let cbOutputType = document.getElementById("cbOutputType").value;
            let cbFieldType = document.getElementById("cbFieldType").value;

            const xhttp = new XMLHttpRequest();
            xhttp.onload = function () {
                let txtOutput = document.getElementById("txtOutput");
                txtOutput.value = this.responseText;
                txtOutput.select();
            }
            xhttp.open("GET", `/apiGenerateFields?tablename=${tablename}&outputtype=${cbOutputType}&fieldtype=${cbFieldType}`, true);
            xhttp.send();
        }

        function loadCustomObject() {
            let sql = encodeURI(document.getElementById("txtSql").value);
            if (sql.length == 0) {
                return;
            }
            let cbFieldType = document.getElementById("cbFieldType").value;

            const xhttp = new XMLHttpRequest();
            xhttp.onload = function () {
                let txtOutput = document.getElementById("txtOutput");
                txtOutput.value = this.responseText;
                txtOutput.select();
            }
            xhttp.open("GET", `/apiGenerateCustomSqlFields?fieldtype=${cbFieldType}&sql=${sql}`, true);
            xhttp.send();
        }

    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    MySqlExpress Helper
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <div class="tableform">
        <table>
            <tr>
                <td>Output Type</td>
                <td>
                    <asp:DropDownList ID="cbOutputType" runat="server" ClientIDMode="Static">
                        <asp:ListItem Value="0" Text="Generate Class Object"></asp:ListItem>
                        <asp:ListItem Value="1" Text="Generate Dictionary Entries"></asp:ListItem>
                        <asp:ListItem Value="2" Text="Generate Create Table SQL"></asp:ListItem>
                        <asp:ListItem Value="3" Text="Create Update Column List"></asp:ListItem>
                    </asp:DropDownList>
                </td>
                <td>Field Type</td>
                <td>
                    <asp:DropDownList ID="cbFieldType" runat="server" ClientIDMode="Static">
                        <asp:ListItem Value="0" Text="public properties"></asp:ListItem>
                        <asp:ListItem Value="1" Text="public fields"></asp:ListItem>
                        <asp:ListItem Value="2" Text="private fields + public properties"></asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>
        </table>
        <a href="#" class="btn cur-p btn-primary" onclick="loadCustomObject(); return false;">Generate Customized Class Object</a>
        &nbsp;
        <input id="txtSql" type="text" style="width: 600px;" placeholder="Enter Custom SQL (for example INNER JOIN statement)" value="select a.*,c.id 'team_id',c.name 'team_name',c.code 'team_code' from player a, player_team b, team c where a.id=b.player_id and b.team_id=c.id and 1=2;" spellcheck="false" class="divcode" />

    </div>

    <br />

    <table style="height: calc(100vh - 350px); width: 100%;">
        <tr>
            <td style="vertical-align: top; width: 250px;">
                Click Table Name To Generate:<br />
                <div id="divTableList" class="divTableList divcode">
                    <asp:PlaceHolder ID="phTables" runat="server"></asp:PlaceHolder>
                </div>
            </td>
            <td style="width: 20px;"></td>
            <td style="vertical-align: top;">
                Output:<br />
                <textarea id="txtOutput" style="height: 100%; width: 100%; border: 1px solid #808080;" spellcheck="false" class="divcode"></textarea>
            </td>
        </tr>
    </table>

</asp:Content>
