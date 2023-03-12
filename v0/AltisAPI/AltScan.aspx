<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="AltScan.aspx.cs" Inherits="AltApi.AltScan" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div> Veuillez saisir le chemin du fichier ou uploader un fichier
        </div>
      Fichier sur le serveur  <asp:TextBox ID="txtUploadFile" runat="server" Text="C:\Users\User\Documents\GitHub\Altis\AltApi\Sample\test.pdf"></asp:TextBox>
        <br />
        ou <br />
        <asp:FileUpload ID="filUpload" runat="server"  /> <br />
        <asp:Button ID="btnUpload" runat="server" Text="Upload" OnClick="btnUpload_Click" /><br />
        <asp:Label ID="lblResult" runat="server" Text=""></asp:Label>
    </form>
</body>
</html>
