<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="AltApi.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <asp:Label ID="lblstatut" runat="server" />
        <a href="Login.aspx">Login avec Code (Pour test)</a>
<%--         Fichier à uploader  <asp:TextBox ID="txtUploadFile" runat="server" Text="C:\Users\User\Documents\GitHub\Altis\AltApi\Sample\test.pdf"></asp:TextBox>
       <asp:Button ID="btnUpload" runat="server" Text="Upload" OnClick="btnUpload_Click" />
        <asp:TextBox ID="txtResult" runat="server"></asp:TextBox>--%>
    </form>
</body>
</html>
