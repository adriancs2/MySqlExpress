<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="System.pages.Error" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Error
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <div class="heading1 margin_0">
        <h2><asp:PlaceHolder ID="phErrTitle" runat="server"></asp:PlaceHolder></h2>
        <hr />
    </div>
    
    <br />

    Error message:<br />
    <br />

    <pre class="divcode"><asp:PlaceHolder ID="ph1" runat="server"></asp:PlaceHolder></pre>
    

</asp:Content>
