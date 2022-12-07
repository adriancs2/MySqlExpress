<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="TeamList.aspx.cs" Inherits="System.pages.TeamList" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Team List
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <div class="heading1 margin_0">

        <h2>Search Filter</h2>
        <hr />

        <div class="tableform">
            <table>
                <tr>
                    <td>Search Team
                    </td>
                    <td>
                        <asp:TextBox ID="txtSearch" runat="server" placeholder="Name, Code"></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td>Year</td>
                    <td>
                        <asp:TextBox ID="txtYear" runat="server" Width="80px"></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td>Status</td>
                    <td>
                        <asp:DropDownList ID="dropStatus" runat="server">
                            <asp:ListItem Value="2" Text="---"></asp:ListItem>
                            <asp:ListItem Value="1" Text="Active"></asp:ListItem>
                            <asp:ListItem Value="0" Text="Deleted"></asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
            </table>
        </div>
    </div>

    <asp:Button ID="btSearch" runat="server" Text="Search Teams" CssClass="btn cur-p btn-primary" OnClick="btSearch_Click" />

    <br />
    <br />

    <asp:PlaceHolder ID="ph1" runat="server"></asp:PlaceHolder>

</asp:Content>
