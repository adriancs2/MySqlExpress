<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MySqlExpress_TestWebForms.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
    <script type="text/javascript">
        function displayLoading(message) {
            let divConnString = document.getElementById("divConnString");
            let divLoading = document.getElementById("divLoading");
            let div_global_message = document.getElementById("div_global_message");

            document.getElementById("h2_loading").innerText = message;

            div_global_message.innerHTML = "";
            divConnString.style.display = "none";
            divLoading.style.display = "block";
            
        }
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Setup / Dashboard
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <div id="divConnString" class="divcode">
        MySQL Connection String:<br />
        <br />
        <asp:TextBox ID="txtConnStr" runat="server" Width="98%" spellcheck="false"></asp:TextBox>
        <br />
        <br />
        <asp:Button ID="btSaveConnStr" runat="server" Text="Save Connection String" CssClass="btn cur-p btn-primary" OnClick="btSaveConnStr_Click" OnClientClick="displayLoading('Testing Connection... Hold On....');" />
        <asp:Button ID="btGenerateSampleData" runat="server" Text="Create/Reset Tables & Sample Data" CssClass="btn cur-p btn-primary" OnClick="btGenerateSampleData_Click" OnClientClick="displayLoading('Generating/Rebuilding Database...');" />
    </div>

    <div id="divLoading" style="display: none;">
        <table>
            <tr>
                <td>
                    <img src="/images/loading.gif" />
                </td>
                <td>
                    <h2 id="h2_loading"></h2>
                </td>
            </tr>
        </table>
    </div>

    <br />

    <div class="heading1 margin_0">
        <h2>About This Web Project</h2>
        <hr />
    </div>

    This small web project serves as a demo of using <b>MySqlExpress</b> and show case the implementation of MySQL database in C#, ASP.NET WebForms.<br />
    <br />

    MySqlExpress aims to encourage rapid application development, save time and save efforts.<br />
    <br />

    This website applies "<b>Auto-Route</b>", read more: <a href="https://github.com/adriancs2/Auto-Route">https://github.com/adriancs2/Auto-Route</a><br />
    <br />

    During development, <b>MySqlExpress</b> works together with an software called <b>"MySqlExpress Helper"</b> which can be downloaded at:<br />
    <br />

    <a href="https://github.com/adriancs2/MySqlExpress/releases">https://github.com/adriancs2/MySqlExpress/releases</a>

    <br />
    <br />

    <b>"MySqlExpress Helper"</b> used to generate C# object class and dictionaries.

    <br />
    <br />

    Below is the list of demos of MySqlExpress methods that implemented in every page.<br />

    <br />

    <b><a href="/">/Home Page</a></b>

    <div class="divcode">
        m.Execute(sql);<br />
        m.StartTransaction();<br />
        m.Commit();<br />
        m.GetObjectList&lt;class&gt;(sql)<br />
        m.Insert(tablename, dic);<br />
        m.InsertUpdate(tablename, dic, lstUpdateCol);<br />
    </div>

    <br />

    <b><a href="/PlayerList">/PlayerList</a></b><br />
    <b><a href="/TeamList">/TeamList</a></b>

    <div class="divcode">
        MySQL - Custom Class Object (inner join SQL statement)<br />
        m.GetObjectList&lt;class&gt;(sql)<br />
    </div>

    <br />

    <b><a href="/PlayerEdit">/PlayerEdit</a></b><br />
    <b><a href="/TeamEdit">/TeamEdit</a></b>

    <div class="divcode">
        m.GetObject&lt;class&gt;(sql)<br />
        m.Insert(tablename, dic);<br />
        m.Update(tablename, dic, "id", id);<br />
        m.Execute(sql);<br />
    </div>

    <br />

    <b><a href="/PlayerTeam">/PlayerTeam</a></b>

    <div class="divcode">
        MySQL - Custom Class Object (inner join SQL statement)
        m.GetObject&lt;class&gt;(sql)<br />
        m.GetObjectList&lt;class&gt;(sql)<br />
        m.InsertUpdate(tablename, dic, lstUpdateCol);<br />
        m.Execute(sql);<br />
    </div>

</asp:Content>
