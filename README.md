# WindowsFirewallLogsProcessor
Processes Windows Firewall logs and detects which programs prohibited or allowed to connect to network

## How it works?

First you must enable Windows Firewall Logging by executing following command:`auditpol /set /subcategory:"{0CCE9226-69AE-11D9-BED3-505054503030}" /success:enable /failure:enable` from (https://serverfault.com/questions/316428/does-windows-firewall-have-the-ability-to-log-which-exe-is-blocked)

Then by executing WindowsFirewallLogsProcessor, you can find which programs accessing network:

[Screenshot1](dox/Capture.PNG)

## Why I developed it?
By default Windows Firewall blocks all incoming connections and allows all outgoing connections. I blocked all outgoing connections for more security, but some apps needed to connect to network. for example windows update, windows time and ... 
Reading logs was so slow and time consuming, so I developed this app to find all apps that want to connect rather than they can connect or their request will be rejected. 

## Features
- Detects allowed and prohibited applications
- Specify how much records to check
- There are some checkboxes that will not work! (they work for previus view and will be deleted)

## What's Next?
I want to add these features:
- Cleaning events from dashboard
- Allowing or preventing an app to and from firewall from dashboard
- Better view
