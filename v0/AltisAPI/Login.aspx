<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="AltApi.Login" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <input id="txtPin" type="number" value="123"  runat="server"/>
            <asp:Button ID="btnSubmit" runat="server" Text="Login" OnClick="btnSubmit_Click" />
        </div>
    </form>
</body>
</html>
