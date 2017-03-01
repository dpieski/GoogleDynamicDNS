Param(
  [string]$user,
  [string]$pass,
  [string]$hn
)

function logger($log)
{
  Out-File -filepath c:\Temp\DynamicDNS.txt -inputobject $log -encoding ASCII -width 160 -Append
  Write-Host $log
}

 $ip = (curl ipv4.icanhazip.com).Content

$pair = "${user}:${pass}"
$bytes = [System.Text.Encoding]::ASCII.GetBytes($pair)
$base64 = [System.Convert]::ToBase64String($bytes)
$headers = @{ Authorization = "Basic $($base64)"}

$ua = [Microsoft.PowerShell.Commands.PSUserAgent]::InternetExplorer


$body = "POST /nic/update?hostname=$hn&myip=$($ip) HTTP/1.1 "
$body = $body + "HOST: domains.google.com "

$res = Invoke-WebRequest -Uri "https://$($user):$($pass)@domains.google.com/nic/update?hostname=$($hn)&myip=$($ip)" -Body $body -Method POST -UserAgent $ua -Headers $headers

$result = -split $res

$response = switch ($result[0])
  {
    "good" {
            logger "The update was successful. The IP address $($result[1]) was set."
            }
    "nochg" {
            logger "The supplied IP address was already set."
            }
    "nohost" {
            logger "The hostname does not exist"
            }
    "badauth" {
            logger "The user/pass combo is not valid"
            }
    "notfqdn" {
            logger "The supplied hostname is not a valid fully-qualified domain name"
            }
    "badagent" {
            logger "You Dynamic DNS client is making bad requests."
            }
    "abuse" {
            logger "Dynamic DNS access for the hostname had been blocked due to failure to interpret previous responses."
            }
    "911" {
            logger "An error happened on Google's end. Please wait 5 min. "
            }
  }