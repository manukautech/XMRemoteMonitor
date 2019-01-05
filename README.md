# XMRemoteMonitor Version 0.8 Beta
A web app that enables Android smart phones and some other devices to act as remote monitoring cameras capturing images on motion detection.
Scenarios include:
- Security Camera
- Watching bird sanctuaries to study rat and other pest behaviours

Easiest use is on devices which can run an HTML5-capable browser which continues to run when the screen is locked. For Android smart phones this usually means installing and using FireFox. We find that we cannot install FireFox on some low cost phones but we have had success with coding an app for them based on the same HTML and JavaScript packaged with "Droidscript". The basic Droidscript is free from Google Play.

This XMRemoteMonitor download has a placeholder "Access Key" of "111111",
Edit "appSettings.json" to change this. 
We recommend a 6 digit number for easy data entry from a smart phone.

Run XMRemoteMonitor on a Windows machine with Visual Studio 2017 installed. We use the "Community Edition" which is free of charge for education, individuals and small businesses. Also download and install ".NET Core 2.2 SDK" from:
[https://www.microsoft.com/net/download/core](https://www.microsoft.com/net/download/core)  
If you are running the full IIS Webserver on your test machine, you will also need to download and install ".NET Core Runtime".  

Quickest first test is to run in Visual Studio on a computer with a webcam. Run in 2 windows: select "WebPageCam" in one and "Monitor" in the other.

XMRemoteMonitor hosting has so far been tested on an Amazon Lightsail Windows 2016 virtual machine server.  
We expect XMRemoteMonitor to also host well on Microsoft Azure shared hosting web apps - todo test this.
XMRemoteMonitor writes captured photos to a folder named "modet" (motion detection). You need to make sure that this folder has write permissions for the IUSR (Internet Users) system account.

Current issues include:  
- Power supply for environments without mains power. We are working on combinations of additional batteries and solar panels.
- Near-real-time Monitor has stopped working on some field tests
- Motion Detection in daylight field tests gets a lot of false positives from trees and plants moving in the wind as well as light level changes as clouds move across the sun
- Can smart phone cameras work with infra red lighting? Early testing suggests yes but with a question mark over sensitivity.
- Is infra red lighting the best human intruder management security strategy? Visible light flash may be better.
- Care with privacy issues when using as a security camera. For our scenarios a good starting point is to only use in private spaces - avoiding any visibility of public and semi-public spaces.

Why not simply go to a shop and buy security cameras? How does this app compete?
- Big advantages for customisation and adapting to special needs in having a system that is all our code, especially control of our own server.
- Smart phones and other HTML-JS-capable devices e.g. Raspberry Pi Zero have advantages for environments without mains electricity. So far all of our field tests have been in such environments.

"XMRemoteMonitor" is a spinoff from "XMRemoteRobot":  
[https://github.com/manukautech/XMRemoteRobot](https://github.com/manukautech/XMRemoteRobot)








 
