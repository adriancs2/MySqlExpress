<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="TeamEdit.aspx.cs" Inherits="System.pages.TeamEdit" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder_title" runat="server">
    Edit Team
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder_body" runat="server">

    <asp:Button ID="btSave" runat="server" Text="Save" CssClass="btn cur-p btn-primary" OnClick="btSave_Click" />
    <asp:Button ID="btSave2" runat="server" Text="Save v2 (m.Save)" CssClass="btn cur-p btn-primary" OnClick="btSave2_Click" />
    <asp:Button ID="btSaveNew" runat="server" Text="Save & New" CssClass="btn cur-p btn-primary" OnClick="btSaveNew_Click" />
    <a href="/TeamEdit" class="btn cur-p btn-primary">New</a>
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
                <td>Team Code</td>
                <td>
                    <asp:TextBox ID="txtCode" runat="server" required></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Team Name</td>
                <td>
                    <asp:TextBox ID="txtName" runat="server" required></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Team Logo</td>
                <td>
                    <asp:Image ID="imgLogo" runat="server" /><br />
                    Upload New Logo:<br />
                    <asp:FileUpload ID="fileLogo" runat="server" accept="image/png, image/jpeg, image/jpg" onchange="return checkFileExtension(this);" />
                </td>
            </tr>
        </table>
    </div>

    <script type="text/javascript">
        var validFilesTypes = ["png", "jpg", "jpeg"];

        function checkFileExtension(file) {
            /*global document: false */
            var filePath = file.value;
            var ext = filePath.substring(filePath.lastIndexOf('.') + 1).toLowerCase();
            var isValidFile = false;

            for (var i = 0; i < validFilesTypes.length; i++) {
                if (ext == validFilesTypes[i]) {
                    isValidFile = true;
                    break;
                }
            }

            if (!isValidFile) {
                file.value = null;
                alert("Invalid File. Valid extensions are:\n\n" + validFilesTypes.join(", "));
            }

            return isValidFile;
        }
    </script>

</asp:Content>
