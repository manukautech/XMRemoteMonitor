# XMRemoteMonitor Version 0.8.1 Beta
A web app that enables Android smart phones and some other devices to act as remote monitoring cameras capturing images on motion detection.
Scenarios include:
- Security Camera
- Watching bird sanctuaries to study rat and other pest behaviours

Easiest use is on devices which can run an HTML5-capable browser which continues to run when the screen is locked. For Android smart phones this usually means installing and using FireFox. We find that this web app client option needs recent design well resourced phones to run reliably - ref Issue 1.2:
https://github.com/manukautech/XMRemoteMonitor/issues/3  
We find that we cannot install FireFox or we experience crashes on some low cost phones. We have had success with coding a mobile app for them based on the web app HTML and JavaScript but repackaged with "Droidscript". The basic Droidscript is free from Google Play. 

This XMRemoteMonitor download has a placeholder "Access Key" of "111111",
Edit "appSettings.json" to change this. 
We recommend a 6 digit number for easy data entry from a smart phone.

Run XMRemoteMonitor on a Windows machine with Visual Studio 2017 installed. We use the "Community Edition" which is free of charge for education, individuals and small businesses. Also download and install ".NET Core 2.2 SDK" from:
[https://www.microsoft.com/net/download/core](https://www.microsoft.com/net/download/core)  
If you are running the full IIS Webserver on your test machine, you will also need to download and install ".NET Core Runtime".  

Quickest first test is to run in Visual Studio on a computer with a webcam. Run in 2 windows: select "WebPageCam" in one and "Monitor" in the other.

XMRemoteMonitor hosting has so far been tested on an Amazon Lightsail Windows 2016 virtual machine server.  
We expect XMRemoteMonitor to also host well on Microsoft Azure shared hosting web apps - todo test this.
XMRemoteMonitor writes captured photos to a folder named "modet" (motion detection). You need to make sure that this folder has write permissions for the IIS_IUSRS (Internet Users) system account.

Current issues include:  
- Power supply for environments without mains power. We are working on combinations of additional batteries and solar panels.
- Near-real-time Monitor has stopped working on some field tests.  We recommend trying "Monitor" only for short run lab testing.  "Review" gives us reliable remote viewing so stick to "Review" until further notice.  
- Motion Detection in daylight field tests gets a lot of false positives from trees and plants moving in the wind as well as light level changes as clouds move across the sun.  
- Can smart phone cameras deliver night vision with infra red lighting? Early testing suggests yes but with a question mark over sensitivity.
- Is infra red lighting the best human intruder management security strategy? Visible light flash may be better.
- Care with privacy issues when using as a security camera. For our scenarios a starting point is to only use in private spaces at times where there should be no person there. Avoid any visibility of public and semi-public spaces. Check and review the privacy laws and regulations for your nation and/or territory.  
- Care with setting up devices outdoors so they do not overheat in sunlight. Best simple quick enclosure from our testing results is a transparent plastic food container lined with white paper.  
- Keep phones vertical. We have tried turning them horizontal for a wider view image. The results range from distortion to crashes and vary wildly with different phones or even the same phone with changes in settings. Needs more investigation.  

Why not simply go to a shop and buy security cameras? How does this app compete?
- A system that is all our code has big advantages for customisation and adapting to special needs. It is especially good to have control of our own server setup and programming.
- Smart phones and other HTML-JS-capable devices e.g. Raspberry Pi Zero have advantages for environments without mains electricity. So far all of our field tests have been in such environments.
- There is a good supply on auction sites of smart phones with cracked screens at low prices.  

"XMRemoteMonitor" is a spinoff from "XMRemoteRobot":  
[https://github.com/manukautech/XMRemoteRobot](https://github.com/manukautech/XMRemoteRobot)

