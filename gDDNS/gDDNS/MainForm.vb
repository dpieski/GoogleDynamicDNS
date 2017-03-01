Imports System.IO
Imports System.Net
Imports System.Text

Public Class MainForm
    Dim WithEvents NotifyIcon1 As New NotifyIcon
    Dim appDataPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        NotifyIcon1.Icon = Icon
        Me.WindowState = FormWindowState.Minimized
        TurnOnIcon()
        log(GetCurrentIPAddress())
    End Sub

    Private Sub MainForm_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If WindowState = FormWindowState.Minimized Then
            TurnOnIcon()
        End If
    End Sub

    Private Sub NotifyIcon1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles NotifyIcon1.DoubleClick
        Me.Show()
        ShowInTaskbar = True
        Me.WindowState = FormWindowState.Normal
        NotifyIcon1.Visible = False
    End Sub

    Private Sub TurnOnIcon()
        NotifyIcon1.Visible = True
        Me.Hide()
        NotifyIcon1.BalloonTipText = "Google Dynamic DNS Started!"
        NotifyIcon1.ShowBalloonTip(500)
    End Sub

    Private Function GetCurrentIPAddress() As String
        Dim wHeader As WebHeaderCollection = New WebHeaderCollection()
        wHeader.Clear()

        Dim sUrl As String = "http://www.icanhazip.com"
        Dim wRequest As HttpWebRequest = DirectCast(System.Net.HttpWebRequest.Create(sUrl), HttpWebRequest)
        wRequest.Headers = wHeader
        wRequest.Method = "GET"

        Dim wResponse As HttpWebResponse = DirectCast(wRequest.GetResponse(), HttpWebResponse)
        Dim sResponse As String
        Using srRead As New StreamReader(wResponse.GetResponseStream())
            sResponse = srRead.ReadToEnd()
        End Using
        Return sResponse
    End Function

    Private Sub log(ByRef value As String)
        Dim Path = appDataPath & "\log.txt"
        If File.Exists(Path) = False Then
            Using sw As StreamWriter = File.CreateText(Path)
                sw.WriteLine(value)
            End Using
        Else
            Using sr As StreamWriter = File.AppendText(Path)
                sr.WriteLine(value)
            End Using
        End If
        Console.WriteLine(value)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim host As New Models.Host
        host.HostName = txtHostname.Text
        host.pass = txtPassword.Text
        host.username = txtUserName.Text
        host.ipAddress = GetCurrentIPAddress()
        UpdateGDNS(host)
    End Sub

    Private Function GetByte64(ByRef value As Models.Host) As String
        Dim pair = value.username & ":" & value.pass
        Dim bytes = System.Text.Encoding.ASCII.GetBytes(pair)
        Return System.Convert.ToBase64String(bytes)
    End Function

    Private Sub UpdateGDNS(host As Models.Host)

        Dim wHeader As WebHeaderCollection = New WebHeaderCollection()

        wHeader.Clear()
        wHeader.Add("Authorization: Basic " & GetByte64(host))

        Dim sUrl As String = "https://" & host.username & ":" &
            host.pass & "@domains.google.com/nic/update?hostname=" &
            host.HostName & "&myip=" & host.ipAddress


        Dim wRequest As HttpWebRequest = DirectCast(System.Net.HttpWebRequest.Create(sUrl), HttpWebRequest)
        wRequest.UserAgent = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)"
        wRequest.Headers = wHeader
        wRequest.Method = "POST"
        wRequest.ContentType = "HTTP/1.1"

        Dim body = "/nic/update?hostname=" &
            host.HostName & "&myip=" & host.ipAddress &
            "HTTP/1.1" &
            "HOST: domains.google.com"

        Dim encoder As New ASCIIEncoding()
        Dim data = encoder.GetBytes(body)
        wRequest.ContentLength = data.Length
        wRequest.Expect = "HTTP/1.1"

        wRequest.GetRequestStream().Write(data, 0, data.Length)
        Dim wResponse As HttpWebResponse = DirectCast(wRequest.GetResponse(), HttpWebResponse)

        Dim sResponse As String = ""

        Using srRead As New StreamReader(wResponse.GetResponseStream())
            sResponse = srRead.ReadToEnd()
        End Using

        Dim res = sResponse.Split(" ")
        Console.WriteLine(res(0))

        Select Case res(0)
            Case "good"
                log("The update was successful. The IP address " & host.ipAddress & " was set.")
            Case "nochg"
                log("The supplied IP address was already set.")
            Case "nohost"
                log("The hostname does Not exist")
            Case "badauth"
                log("The user/pass combo Is Not valid")
            Case "notfqdn"
                log("The supplied hostname Is Not a valid fully-qualified domain name")
            Case "badagent"
                log("You Dynamic DNS client Is making bad requests.")
            Case "abuse"
                log("Dynamic DNS access for the hostname had been blocked due to failure to interpret previous responses.")
            Case "911"
                log("An error happened on Google's end. Please wait 5 min. ")
        End Select
    End Sub
End Class
