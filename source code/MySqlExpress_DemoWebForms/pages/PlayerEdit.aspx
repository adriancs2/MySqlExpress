<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="PlayerEdit.aspx.cs" Inherits="System.pages.PlayerEdit" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Edit Player
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <asp:Button ID="btSave" runat="server" Text="Save" CssClass="btn cur-p btn-primary" OnClick="btSave_Click" />
    <asp:Button ID="btSaveNew" runat="server" Text="Save & New" CssClass="btn cur-p btn-primary" OnClick="btSaveNew_Click" />
    <a href="/PlayerEdit" class="btn cur-p btn-primary">New</a>
    <asp:Button ID="btDelete" runat="server" Text="Delete" CssClass="btn cur-p btn-primary" OnClick="btDelete_Click" OnClientClick="return confirm('Are you sure to delete?');" />
    <asp:Button ID="btRecover" runat="server" Text="Recover" CssClass="btn cur-p btn-primary" OnClick="btRecover_Click" OnClientClick="return confirm('Are you sure to recover');" />

    <div class="tableform">
        <table>
            <tr>
                <td>ID</td>
                <td>
                    <asp:Label ID="lbId" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>Status</td>
                <td>
                    <asp:Label ID="lbStatus" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>Player Code</td>
                <td>
                    <asp:TextBox ID="txtCode" runat="server" required></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Player Name</td>
                <td>
                    <asp:TextBox ID="txtName" runat="server" required></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Date Register</td>
                <td>
                    <asp:TextBox ID="txtDateRegister" runat="server" TextMode="Date" required></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Tel</td>
                <td>
                    <asp:TextBox ID="txtTel" runat="server" required></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Email</td>
                <td>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" required></asp:TextBox>
                </td>
            </tr>
        </table>
    </div>

</asp:Content>
