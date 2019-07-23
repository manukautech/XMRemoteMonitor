# XMRemoteMonitor Version 0.9.1 Beta
A web app that enables Android smart phones and some other devices to act as remote monitoring cameras capturing images on motion detection.
Scenarios include:
- Watching bird sanctuaries to study rat and other pest behaviours
- Security Camera
  
Easiest quick test is on devices e.g. laptops which can run an HTML5-capable browser. The web app can provide a web page which manages the camera, adds motion detection and sends images to the server.
Our preferred client is to run the app "XMMonitorDSL" on an Android smartphone. "XMMonitorDSL" can save files both locally and on the server."XMMonitorDSL" can also run on a schedule, e.g. capture images only at night and save power during the day by shutting down camera operations. We run "XMMonitorDSL" in Android "pinned" mode with the screen brightness turned down to its lowest setting.

XMRemoteMonitor was saving 1 folder of photos on the server for each day of operation. We now have an option, switched on by default, to save 1 folder of photos for each night of operation. That way we can look in one folder to see the story of a night. 
Edit "appSettings.json" to configure this "nightfolders" option as "true" or "false".

Also in "appSetting.json" is a placeholder "Access Key" of "111111". 
Edit "appSettings.json" to change this. 
We recommend a 6 digit number for easy data entry from a smart phone.  

The server website is for private use by the owner and or administrator of a set of cameras. Therefore it should have an "unfriendly" URL address including a subdomain, and possibly a sub-application, that look and act like PIN numbers.  
e.g. ["https://johnsecurity.examplesharedhosting.net"](https://example.com) is a bad URL. The use of a less friendly random prefix like ["https://k37402.examplesharedhosting.net"](https://example.com) is much better.  
e.g. If you own a domain like "example.com" then setup your server to be like ["https://k37402.example.com"](https://example.com) with a separate and different server and ip address from whatever the parent domain like "example.com" points to. That is because public registry information shows your domain name but not your sub-domains.  
e.g. If you control your own server operating system, usually as a "virtual machine", consider using sub-applications. Then URLs look like this: ["https://k37402.example.com/bqigy"](https://example.com) with another small barrier to hackers but adding on extra barriers is good. 
Always operate the web app with security SSL certificates installed so your URLs work starting with "https". That sentence can also start "Always operate ANY web app ...". If you use Azure shared hosting this is already provided for you. If you run your own virtual machine you can run with "Let's Encrypt" certificates which are free but you need to renew them every 90 days. The timesaving of a paid service may be worth it for larger scale operations. Ref your hosting service provider.

Test-run XMRemoteMonitor on a Windows machine with a recent version of Visual Studio installed. We use the "Community Edition" which is free of charge for education, individuals and small businesses. Also download and install the most recent ".NET Core SDK" from:
[https://www.microsoft.com/net/download/core](https://www.microsoft.com/net/download/core)  
If you are running the full IIS Webserver on your test machine, you will also need to download and install ".NET Core Runtime". 
At the time of writing this in July 2019, the latest Visual Studio version is "2019" and the latest .NET Core version is "2.2".

Quickest first test is to run in Visual Studio on a computer with a webcam. Run in 2 windows: select "WebPageCam" in one and "Monitor" in the other.

XMRemoteMonitor hosting has so far been tested on an Amazon Lightsail Windows 2016 virtual machine server and on a Microsoft Windows Azure Hosted App Service.
XMRemoteMonitor writes captured photos to a folder named "modet" (motion detection).  
- For Virtual Machines, e.g. Amazon Lightsail, you need to make sure that this folder has write permissions for the IIS_IUSRS (Internet Users) system account.  
- For Windows Azure App Services - our testing finds that we can write into this folder without the need for any special configuration.

Note for Windows Azure App Services. The time zone is set to UTC (aka "Greenwich Mean Time", "Co-ordinated Universal Time"). To set for your time zone you can use the Azure Portal web app service control panel under "Configuration" to set an "Application Setting" of "WEBSITE_TIME_ZONE" with a value like "UTC+12", "UTC+10", "UTC-5". You would then need to manually correct this when your region applies Daylight Saving Time. For me in New Zealand I need "UTC+12" in winter and "UTC+13" in summer.

Current issues include:  
- Power supply for environments without mains power. We are working on combinations of additional batteries and solar panels.
- Near-real-time Monitor has stopped working on some field tests.  We recommend trying "Monitor" only for short run lab testing.  "Review" gives us reliable remote viewing so stick to "Review" until further notice.  
- Motion Detection in daylight field tests gets a lot of false positives from trees and plants moving in the wind as well as light level changes as clouds move across the sun.  
- Can smart phone cameras deliver night vision with infra red lighting? Yes, with an invasive modification involving removing an "infra red cut filter" from behind the lens. We do this to old secondhand phones. More info at:  
["Smartphone VFD-300 as an Infra-Red Camera"](https://hitechfromlotech.blogspot.com/2019/03/smartphone-vfd-300-as-infra-red-camera.html).  
- Is infra red lighting the best human intruder management security strategy? Visible light flash may be better.
- Care with privacy issues when using as a security camera. For our scenarios a starting point is to only use in private spaces at times where there should be no person there. Avoid any visibility of public and semi-public spaces. Check and review the privacy laws and regulations for your nation and/or territory.  
- Care with setting up devices outdoors so they do not overheat in sunlight. Best simple quick enclosure from our testing results is a transparent plastic food container lined with light-coloured gaffer tape aka duct tape.  
- Keep phones vertical for the web app. Keep phones horizontal for the Android app. We have tried rotating them. The results range from distortion to crashes and vary wildly with different phones or even the same phone with changes in settings. Needs more investigation.  

Why not simply go to a shop and buy security cameras? How does this app compete?
- A system that is all our code has big advantages for customisation and adapting to special needs. It is especially good to have control of our own server setup and programming.
- Smart phones and other HTML-JS-capable devices e.g. Raspberry Pi Zero have advantages for environments without mains electricity. So far all of our field tests have been in such environments.

A related project is "XMRemoteRobot":  
[https://github.com/manukautech/XMRemoteRobot](https://github.com/manukautech/XMRemoteRobot)  
Remote control of robotic devices including remote vision.
