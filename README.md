# WindowsFirewallLogsProcessor
Processes Windows Firewall logs and detects which programs prohibited or allowed to connect to network

## How it works?

First you must enable Windows Firewall Logging by executing following command:`auditpol /set /subcategory:"{0CCE9226-69AE-11D9-BED3-505054503030}" /success:enable /failure:enable` from (https://serverfault.com/questions/316428/does-windows-firewall-have-the-ability-to-log-which-exe-is-blocked)

Then by executing WindowsFirewallLogsProcessor, you can find which programs accessing network:

[Screenshot1](dox/Capture.PNG)
