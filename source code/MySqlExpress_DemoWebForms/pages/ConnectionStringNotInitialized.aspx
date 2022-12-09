<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="ConnectionStringNotInitialized.aspx.cs" Inherits="System.pages.ConnectionStringNotInitialized" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Connection String Is Not Initialized
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    Connection string is not initialized, please go to the following page to create and set the connection string:
    <br />
    <br />

    <a href="/">Setup / Dashboard</a>

</asp:Content>
