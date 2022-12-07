<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MySqlExpress_TestWebForms.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Setup / Dashboard
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <div class="divcode">
        MySQL Connection String:<br />
        <br />
        <asp:TextBox ID="txtConnStr" runat="server" Width="98%" spellcheck="false"></asp:TextBox>
        <br />
        <br />
        <asp:Button ID="btSaveConnStr" runat="server" Text="Save Connection String" CssClass="btn cur-p btn-primary" OnClick="btSaveConnStr_Click" />
        <asp:Button ID="btGenerateSampleData" runat="server" Text="Regenerate Sample Data" CssClass="btn cur-p btn-primary" OnClick="btGenerateSampleData_Click" />
    </div>

    <br />

    <div class="heading1 margin_0">
        <h2>About This Web Project</h2>
        <hr />
    </div>

    This website applies "<b>Auto-Route</b>", read more: <a href="https://github.com/adriancs2/Auto-Route">https://github.com/adriancs2/Auto-Route</a><br />
    <br />
    This small web project serves as a demo of using <b>MySqlExpress</b>. It also serves as an example of using MySQL in C# (ASP.NET WebForms).

    <br />
    <br />

    Below is the list of demos that implemented in every page.<br />
    <br />

</asp:Content>
