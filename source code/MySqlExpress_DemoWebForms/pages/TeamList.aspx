<%@ Page Title="" Language="C#" MasterPageFile="~/master1.Master" AutoEventWireup="true" CodeBehind="TeamList.aspx.cs" Inherits="System.pages.TeamList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder_head" runat="server">
    <style type="text/css">
        .divTeamBlock {
            float: left;
            width: 200px;
            text-align: center;
            padding: 10px;
        }

        .divTeamBlock_a {
            display: block;
        }

            .divTeamBlock:hover {
                box-shadow: 1px 1px 12px #a9a9a9;
                position: relative;
                top: 2px;
                bottom: 2px;
                background: #e0f1ff;
            }

            .divTeamBlock_Info {
                color: #5e5e5e !important;
                text-decoration: none !important;
                font-style: normal !important;
                font-weight: normal !important;
            }

            .divTeamBlock:hover .divTeamBlock_Info {
                text-decoration: none !important;
                font-style: normal !important;
                font-weight: normal !important;
                color: red !important;
            }
    </style>
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
                        <asp:TextBox ID="txtSearch" runat="server" placeholder="Team Name, Team Code"></asp:TextBox>
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
                <tr>
                    <td>Result Display Mode</td>
                    <td>
                        <asp:DropDownList ID="dropResultMode" runat="server">
                            <asp:ListItem Value="2" Text="Team Gallery"></asp:ListItem>
                            <asp:ListItem Value="1" Text="Table List With Players' Name"></asp:ListItem>
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
