<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="PlayerTeam.aspx.cs" Inherits="System.pages.PlayerTeam" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Assign Player to Team
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

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

                <asp:Button ID="btRemovePlayer" runat="server" Text="Remove Selected Player" CssClass="btn cur-p btn-primary" OnClick="btRemovePlayer_Click" />

                <br />
                <br />

                <asp:PlaceHolder ID="ph1" runat="server"></asp:PlaceHolder>

            </td>
            <td style="width: 2%; border-right: 1px solid #d3d3d3;"></td>
            <td style="width: 2%;"></td>
            <td style="vertical-align: top; width: 56%;">

                <div class="heading1 margin_0">
                    <h2>Search Players</h2>
                </div>

                <div class="tableform">
                    <asp:Button ID="btSearch" runat="server" Text="Search" CssClass="btn cur-p btn-primary" OnClick="btSearch_Click" ClientIDMode="Static" />
                    <asp:TextBox ID="txtSearch" runat="server" onkeydown="return submitForm(event)" placeholder="Name, Code, Email, Tel"></asp:TextBox>
                    <asp:Button ID="btAddPlayer" runat="server" Text="Add Selected Player" CssClass="btn cur-p btn-primary" OnClick="btAddPlayer_Click" />
                </div>

                <br />

                <asp:PlaceHolder ID="ph2" runat="server"></asp:PlaceHolder>

            </td>
        </tr>
    </table>

    <script type="text/javascript">
        function submitForm(e) {
            if (e.keyCode == 13) {
                document.getElementById("btSearch").click(); //javascript
                return false;
            }
            return true;
        }

        function selectAll(tablename, isselect) {
            let ipt = document.getElementById(tablename).getElementsByTagName("input");
            for (let i = 0; i < ipt.length; i++) {
                if (ipt[i].getAttribute("type") == "checkbox") {
                    ipt[i].checked = isselect;
                }
            }
        }
    </script>

</asp:Content>
