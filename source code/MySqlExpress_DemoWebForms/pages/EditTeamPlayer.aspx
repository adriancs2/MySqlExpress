<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="EditTeamPlayer.aspx.cs" Inherits="System.pages.EditTeamPlayer" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Edit Team Player
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <div class="tableform">
        <table>
            <tr>
                <td>Year</td>
                <td>
                    <asp:Label ID="lbYear" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>Team</td>
                <td>
                    <asp:Label ID="lbTeam" runat="server"></asp:Label>
                </td>
            </tr>
        </table>
    </div>

    

</asp:Content>
