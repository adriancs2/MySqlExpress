<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="TeamEditBatch.aspx.cs" Inherits="System.pages.TeamEditBatch" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Batch Edit Teams
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <input type="submit" style="display: none;" aria-hidden="true" />

    <asp:Button CssClass="btn cur-p btn-primary" ID="btLoad" runat="server" Text="Refresh" OnClick="btLoad_Click" />
    <asp:Button CssClass="btn cur-p btn-primary" ID="btSave" runat="server" Text="Save" OnClick="btSave_Click" />
    <asp:Button CssClass="btn cur-p btn-primary" ID="btAdd" runat="server" Text="Add" OnClick="btAdd_Click" />
    <asp:TextBox ID="txtAddNo" runat="server" Width="50px" Text="1"></asp:TextBox>
    New Team(s)


    <br />
    <br />

    <asp:PlaceHolder ID="ph1" runat="server"></asp:PlaceHolder>

</asp:Content>
