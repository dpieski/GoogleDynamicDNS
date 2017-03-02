Imports System.Data.SQLite
Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
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

        Dim sUrl As String = "http://ipv4.icanhazip.com"
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
        MakeDatabase()
        MsgBox(InsertIntoHost(host))
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

    Private Function Encrypt(clearText As String) As String
        Dim EncryptionKey As String = "MAKV2SPBNI99212"
        Dim clearBytes As Byte() = Encoding.Unicode.GetBytes(clearText)
        Using encryptor As Aes = Aes.Create()
            Dim pdb As New Rfc2898DeriveBytes(EncryptionKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D,
             &H65, &H64, &H76, &H65, &H64, &H65,
             &H76})
            encryptor.Key = pdb.GetBytes(32)
            encryptor.IV = pdb.GetBytes(16)
            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)
                    cs.Write(clearBytes, 0, clearBytes.Length)
                    cs.Close()
                End Using
                clearText = Convert.ToBase64String(ms.ToArray())
            End Using
        End Using
        Return clearText
    End Function

    Private Function Decrypt(cipherText As String) As String
        Dim EncryptionKey As String = "MAKV2SPBNI99212"
        Dim cipherBytes As Byte() = Convert.FromBase64String(cipherText)
        Using encryptor As Aes = Aes.Create()
            Dim pdb As New Rfc2898DeriveBytes(EncryptionKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D,
             &H65, &H64, &H76, &H65, &H64, &H65,
             &H76})
            encryptor.Key = pdb.GetBytes(32)
            encryptor.IV = pdb.GetBytes(16)
            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write)
                    cs.Write(cipherBytes, 0, cipherBytes.Length)
                    cs.Close()
                End Using
                cipherText = Encoding.Unicode.GetString(ms.ToArray())
            End Using
        End Using
        Return cipherText
    End Function

    Private Sub MakeDatabase()
        Dim appPath As String = My.Application.Info.DirectoryPath
        Dim dbPath = appPath & "\gDDNS.sql3"
        If Not File.Exists(dbPath) Then
            SQLiteConnection.CreateFile(dbPath)
            Dim Connection As New SQLiteConnection
            Using Query As New SQLiteCommand()
                Connection.ConnectionString = "Data Source=" & dbPath & ";Version=3;New=False;Compress=True;"
                Connection.Open()
                With Query
                    .Connection = Connection
                    .CommandText = "CREATE TABLE Hosts(ID INTEGER PRIMARY KEY ASC, UserName VARCHAR(25)," &
                                   "Pass NVARCHAR(200), HostName VARCHAR(50), ipAddress VARCHAR(15), updateDTS DATETIME)"
                End With
                Query.ExecuteNonQuery()
                Connection.Close()
            End Using
        End If
    End Sub

    Private Function InsertIntoHost(ByRef host As Models.Host) As String
        Dim appPath As String = My.Application.Info.DirectoryPath
        Dim dbPath = appPath & "\gDDNS.sql3"
        Dim res As String = ""

        Dim dbConnection As New SQLiteConnection("Data Source=" + dbPath + ";Version=3;")
        Dim strSQL = "insert into Hosts(UserName, Pass, HostName, ipAddress, updateDTS) " &
                     "values(""" & host.username & """, """ & host.pass & """, """ & host.HostName &
                     """, """ & host.ipAddress.Substring(0, host.ipAddress.Count - 1) & """, """ & Date.Now.ToString("yyyy-mm-dd hh:mm:ss") & """)"
        Dim command As New SQLiteCommand(strSQL, dbConnection) 'Create a SQLite command which accepts the query And database connection.
        dbConnection.Open() 'Open the connection With database
        res = command.ExecuteNonQuery() 'Executes the SQL query
        dbConnection.Close() 'Close the connection With database

        Return res
    End Function
End Class
