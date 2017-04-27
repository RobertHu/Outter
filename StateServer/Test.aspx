<%@ Page language="c#" Inherits="iExchange.StateServer.Test" Codebehind="Test.aspx.cs" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<HTML>
	<HEAD>
		<title>Test</title>
		<meta name="GENERATOR" Content="Microsoft Visual Studio 7.0">
		<meta name="CODE_LANGUAGE" Content="C#">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
	</HEAD>
	<body>
		<form method="post" runat="server">
			<FONT face="system">
				<asp:TextBox id="LoginID" style="Z-INDEX: 101; LEFT: 214px; POSITION: absolute; TOP: 63px" runat="server"
					Width="163px" Height="35px" TextMode="Password"></asp:TextBox>
				<asp:TextBox id="ExpireTime" style="Z-INDEX: 109; LEFT: 213px; POSITION: absolute; TOP: 173px"
					runat="server" Height="35px" Width="163px"></asp:TextBox>
				<asp:Label id="Label1" style="Z-INDEX: 102; LEFT: 148px; POSITION: absolute; TOP: 74px" runat="server">LoginID</asp:Label>
				<asp:Label id="Label2" style="Z-INDEX: 103; LEFT: 148px; POSITION: absolute; TOP: 125px" runat="server">Password</asp:Label>
				<asp:TextBox id="Password" style="Z-INDEX: 104; LEFT: 216px; POSITION: absolute; TOP: 120px"
					runat="server" Width="160px" Height="32px" TextMode="Password"></asp:TextBox>
				<asp:Button id="Submit" style="Z-INDEX: 105; LEFT: 213px; POSITION: absolute; TOP: 240px" runat="server"
					Text="Submit" onclick="Submit_Click"></asp:Button>
				<asp:TextBox id="Status" style="Z-INDEX: 106; LEFT: 210px; POSITION: absolute; TOP: 295px" runat="server"
					Width="160px" Height="33px" Enabled="False"></asp:TextBox>
				<asp:Label id="Label3" style="Z-INDEX: 107; LEFT: 142px; POSITION: absolute; TOP: 302px" runat="server"
					Width="25px" Height="31px">Status</asp:Label>
				<asp:Button id="Verify" style="Z-INDEX: 108; LEFT: 296px; POSITION: absolute; TOP: 242px" runat="server"
					Width="64px" Height="23px" Text="Test" onclick="Verify_Click"></asp:Button>
				<asp:Label id="Label4" style="Z-INDEX: 110; LEFT: 125px; POSITION: absolute; TOP: 175px" runat="server"
					Height="29px" Width="34px">ExpireTime</asp:Label></FONT>
		</form>
	</body>
</HTML>
