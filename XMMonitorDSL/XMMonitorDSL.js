//XMMonitorDSL
//Remote monitoring camera Android client for use with the XMRemoteMonitor server app.
//Part of the open source project "XMRemoteMonitor" - ref:
//https://github.com/manukautech/XMRemoteMonitor

//Attribution:
//"XMMonitorDSL" is based on the sample app "Camera Motion" by droidscript.org
//"Camera Motion" is part of the documentation of the "DroidScript" package available in Google Play:
//https://play.google.com/store/apps/details?id=com.smartphoneremote.androidscriptfree
//DroidScript is a package including IDE and APIs for programming Android devices with JavaScript.

//Initialise variables with variables most needed for configuration up first
//Server app root URL if you have one. Do NOT end with "/". The code will add that.
//Otherwise "placeholder" or "" will give local-only recording of images.

var requestAppUrl = "placeholder";

var sensitivity = 70; //percent.
var minPeriod = 200;  //millisecs.
var mosaic = "5x5";
var captureFolder = "/sdcard/modet/";
var cameraNumber = 1;

//State management
//2018-12-26 JPC trigger override capture without motion detection on long timeout
var isModet = false;  //code sets isModet to true on motion detection events
var overrideTimeout = 1800000; //usually 1800000 = 1800 seconds ie 30 min
var timeInterval = 30000; //status check every 30 seconds
var overrideCounter = 0;

//For local recording, this app keeps the most recent 2000 images 
//with naming starting at 1000.jpg to keep all names 4 characters long.
var counter = 1000;
var placeholderBase64DSL = ""; //image to record when scheduler specifies off-time

//Setup AJAX  refs
//https://www.w3schools.com/xml/ajax_xmlhttprequest_send.asp
//https://www.w3schools.com/js/js_ajax_http.asp
var xmlhttp = new XMLHttpRequest();
var cam;
var img;
var hasReadSettings = false;

var isActive = false;
var isTesting = false;
var xtimer;
var log;

function scheduledActive() {
    //comment-out next statement "return true;" to switch on scheduling
    //return true;

    //2019-04-19 JPC make testing possible while scheduled on
    if (isTesting) return false;

    //Scheduled active from 6:30pm to 6:30am on week-nights
    //'return true' effect is to capture images on motion detection
    //'return false' effect is to NOT capture images on motion detection
    var d = new Date();

    //Remain active through the weekend
    //if (d.getDay() == 6 || d.getDay() == 0) {
    //    return true;
    //}

    var dayMinutes = d.getHours() * 60 + d.getMinutes();
    if (dayMinutes >= 18 * 60 + 30) {
        return true;
    } else if (dayMinutes <= 6 * 60 + 30) {
        return true;
    }
    return false;
}

//Called when application is started.
function OnStart() {
    //Fix orientation to landscape since
    //most phone cameras work this way.   
    app.SetOrientation("Landscape");

    //Create horizontal layout that fills the screen.
    lay = app.CreateLayout("Linear", "Horizontal,FillXY");

    //Create frame layout on left for camera view.
    layLeft = app.CreateLayout("Frame");
    layLeft.SetMargins(0, 0.1, 0.02, 0);
    lay.AddChild(layLeft);

    //Move cam create from here

    //Create vertical layout on right for other stuff.
    layRight = app.CreateLayout("Linear", "Vertical,FillXY");
    lay.AddChild(layRight);

    layLogin = app.CreateLayout("Linear", "Horizontal,FillX");
    layLogin.SetMargins(0, 0, 0, 0);
    layLogin.SetPadding(0, 0.01, 0, 0.01);
    layRight.AddChild(layLogin);

    //2019-01-25 JPC Create a text control for server password
    txtAccess = app.CreateTextEdit("", 0.18, 0.1, "password");
    txtAccess.SetMargins(0, 0, 0.02, 0);
    //2019-02-03 JPC sometimes I need to "state the obvious" to get controls to line up
    txtAccess.SetPadding(0, 0, 0, 0);
    txtAccess.SetTextColor("#ffffffff");
    txtAccess.SetBackColor("#ff222222");
    layLogin.AddChild(txtAccess);

    //2019-02-03 JPC add cameraNumber options
    var camOptions = "1,2,3,4,5";
    spinCam = app.CreateSpinner(camOptions, 0.1, -1);
    spinCam.SetTextColor("#ffffffff");
    spinCam.SetBackColor("#ff222222");
    spinCam.SetOnTouch(spinCam_OnTouch);
    spinCam.SetText(cameraNumber.toString());
    layLogin.AddChild(spinCam);

    btn = app.CreateButton("Start", 0.15, 0.1);
    btn.SetOnTouch(prepDetection);
    btn.SetMargins(0.02, 0, 0, 0);
    btn.SetPadding(0, 0, 0, 0.01);
    btn.SetTextColor("#ffffffff");
    btn.SetBackColor("#ff222222");
    layLogin.AddChild(btn);

    layOps = app.CreateLayout("Linear", "Horizontal,FillX");
    layOps.SetMargins(0, 0, 0, 0);
    layOps.SetPadding(0, 0.01, 0, 0.01);
    layRight.AddChild(layOps);

    btnTest = app.CreateButton("Test On", 0.16, 0.1);
    btnTest.SetOnTouch(testRun);
    btnTest.SetMargins(0.02, 0, 0, 0);
    btnTest.SetPadding(0, 0, 0, 0.01);
    btnTest.SetTextColor("#ffffffff");
    btnTest.SetBackColor("#ff333333");
    if (scheduledActive()) btnTest.SetEnabled(false);
    layOps.AddChild(btnTest);

    btnExit = app.CreateButton("Exit", 0.16, 0.1);
    btnExit.SetOnTouch(thisAppExit);
    btnExit.SetMargins(0.02, 0, 0, 0);
    btnExit.SetPadding(0, 0, 0, 0.01);
    btnExit.SetTextColor("#ffffffff");
    btnExit.SetBackColor("#ff333333");
    layOps.AddChild(btnExit);

    //Create a text control to show logs.
    txt = app.CreateText("", 0.5, 0.3, "Multiline,Left");
    txt.SetMargins(0, 0.1, 0, 0);
    txt.SetPadding(0.01, 0.01, 0.01, 0.01);
    txt.SetTextColor("#ffffffff");
    txt.SetBackColor("#ff222222");
    layRight.AddChild(txt);

    //Create sensitivity seek bar label.
    txtSens = app.CreateText("Sensitivity");
    txtSens.SetMargins(0, 0.05, 0, 0);
    layRight.AddChild(txtSens);

    //Create sensitivity seek bar.
    skb = app.CreateSeekBar(0.3, -1);
    skb.SetOnTouch(skb_OnTouch);
    skb.SetValue(sensitivity);
    layRight.AddChild(skb);

    /*
    //Create mosaic spinner label.
    txtTiles = app.CreateText("Mosaic");
    txtTiles.SetMargins(0, 0.05, 0, 0);
    layRight.AddChild(txtTiles);

    //Create mosaic spinner.
    var layouts = "3x2,3x3,5x5,10x10";
    spin = app.CreateSpinner(layouts, 0.2, -1);
    spin.SetOnTouch(spin_OnTouch);
    spin.SetText(mosaic);
    layRight.AddChild(spin);

    //Set menus.
    app.SetMenu("Capture,Exit");
    */

    //Add main layout to app.
    app.AddLayout(lay);

    //Create array to hold log messages.
    log = new Array();
    Log(captureFolder);

    //Initialise AJAX
    xmlhttp.onreadystatechange = function () {
        ajaxCallback();
    };

}

//Start motion detection
//4 x functions - first time starts and continuing awakeDetection
function prepDetection() {
    //2019-01-30 JPC startup check of server if we are using a server ie not-local-only
    if (!isLocalOnly()) {
        var formData = new FormData();
        formData.append("IsImageDownload", false);
        formData.append("IsAuth", true);
        formData.append("AccessKey", txtAccess.GetText());
        xmlhttp.open("POST", requestAppUrl + "/Home/GetSettings", true);
        //xmlhttp.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
        xmlhttp.send(formData);
    }
    //Create an image control over the top of the 
    //camera view with transparency (alpha) and with a
    //fixed internal bitmap the same size as camera view.
    //w = cam.GetImageWidth();
    //h = cam.GetImageHeight();
    img = app.CreateImage(null, 0.4, 0.8, "fix", 640, 480);
    img.SetAlpha(0.5);
    //layLeft.AddChild(img);

    //Create folder for saved pictures 
    app.MakeFolder(captureFolder);
    //cam.SetPictureSize(1024, 768);

    setInterval(idleOverride, timeInterval);
    if (scheduledActive() || isTesting) {
        awakeDetection();
        isActive = true;
    } else {
        app.ShowPopup("Standby");
        Log("On Standby, app scheduled off");
        btnTest.SetEnabled(false);
    }
    btn.SetEnabled(false);
}

function awakeDetection() {
    //Create camera view control. Work with image resolution 
    //"CIF" = 352 x 240, "VGA" = 640 x 480 , higher options eg "XGA" are giving 640 x 480
    cam = app.CreateCameraView(0.4, 0.8, "VGA,UseBitmap");
    cam.SetSound(false);
    //Get img appearing in front of cam on each startup
    layLeft.AddChild(cam);
    layLeft.AddChild(img);

    btn.SetEnabled(false);
    if (isTesting) {
        app.ShowPopup("Start Cam test");
    }
    setTimeout(awakeStep2, 1000);
}

function awakeStep2() {
    //Enable 10x10 matrix motion detection and mark
    //motion detections in the image control.
    //for "(" value settings like "10x10", split on "x" to get the numeric values
    var mx = parseInt(mosaic.split("x")[0]);
    var my = parseInt(mosaic.split("x")[1]);
    cam.MotionMosaic(mx, my, (100 - sensitivity) / 5, minPeriod, img);
    cam.SetOnMotion(OnMotion);
    //Start preview.
    cam.StartPreview();
}

function sleepDetection() {
    cam.StopPreview();
    //Get img appearing in front of cam on each startup
    layLeft.RemoveChild(cam);
    layLeft.RemoveChild(img);
    cam.Destroy();
    if (isTesting) {
        app.ShowPopup("Stop Cam test");
    }
}


//Called when motion is detected.
//(data contains an array of detection strength 
//values corresponding to each mosaic tile)
function OnMotion(data) {
    if (isLocalOnly()) {
        sendImage(scheduledActive() || isTesting);
    } else if (scheduledActive() || isTesting) {
        isModet = true;
        sendImage(true);
    } else {
        isModet = false;
        Log("Scheduled off, ignore trigger " + xgetTime());
    }
}

//Send new image or placeholder to the server
function sendImage(isScheduled) {
    overrideCounter = 0;
    //Running locally without a server
    if (isLocalOnly()) {
        if (isScheduled) {
            localRecordImage();
            Log("Local recording only " + xgetTime());
        }
        return;
    }

    var request;
    var options;
    if (isScheduled) {
        request = cam.GetPixelData("jpgbase64", 0, 0, 640, 480);
        if (request == null || request.length < 10) {
            Log("Image reading has no data - skip");
            return;
        }
        options = "base64";
        Log("Image data size: " + request.length);
    } else {
        request = "";
        options = "placeholder";
        Log("Save Placeholder image " + xgetTime());
    }

    var formData = new FormData();
    formData.append("CameraNumber", cameraNumber);
    formData.append("AccessKey", txtAccess.GetText());
    //2019-01-25 JPC this "jgpbase64" does not have any prefix so no need to slice
    formData.append("ImageBase64", request);
    //2019-01-27 JPC send options value to get server to save a placeholder image
    formData.append("Options", options);

    //throw new Error("Test error");

    xmlhttp.open("POST", requestAppUrl + "/Home/ImagePost", true);
    //xmlhttp.setRequestHeader("Content-type", "application/x-www-form-urlencoded");
    xmlhttp.send(formData);
    localRecordImage();
}

function localRecordImage() {
    var file = captureFolder + counter + ".jpg";
    cam.TakePicture(file);
    counter++;
    if (counter >= 3000) counter = 1000;
}

function ajaxCallback() {
    if (xmlhttp.readyState === 4 && xmlhttp.status === 200) {
        var response = xmlhttp.responseText;
        //our AJAX methods return json strings so can use same json handling
        //function xreceive as SignalR
        xreceive(response);
    }
}

//SignalR and AJAX responses in json format both handled by this one function
function xreceive(response) {
    var jsonObject = JSON.parse(response);
    if (response.indexOf("issuccess") > -1 && !jsonObject.issuccess) {
        Log(jsonObject.message);
    } else if (jsonObject.categoryid && jsonObject.categoryid == 20) {
        //2019-01-27 JPC we do not need placeholder image data for this version
        //but we may need it later to save in local storage
        //placeholderBase64DSL = jsonObject.xdata[0];
        //Code variation for this DroidScript Layout-based app
        //headerLength = placeholderBase64DSL.indexOf(";base64,") + 8;
        //placeholderBase64DSL = placeholderBase64DSL.slice(headerLength);
        Log(jsonObject.message);
        hasReadSettings = true;
    } else if (jsonObject.categoryid && jsonObject.categoryid == 21) {
        Log("Image saved OK " + xgetTime());
    } else {
        app.ShowPopup(response + "ERROR: Data from server is badly formatted or otherwise not making sense.")
    }
    //if (debug) DebugDisplay(response);
}

function idleOverride() {
    //Check for testing time ends
    bCheck = scheduledActive();
    if (bCheck) {
        btnTest.SetEnabled(false);
        isTesting = false;
    }

    if (isTesting) {
        if (!isActive) {
            awakeDetection();
            isActive = true;
        } else {
            sleepDetection();
            isActive = false;
        }
    } else {
        if (bCheck && !isActive) {
            //Timer is switching camera on
            //Start/connect to our service.
            Log("Camera started");
            awakeDetection();
            isActive = true;
        } else if (bCheck == false && isActive == true) {
            //Scheduler is switching camera off
            //Tell service we are quitting.
            Log("Status: camera stopped");
            sleepDetection();
            isActive = false;
        } else if (bCheck && isActive) {
            if (overrideCounter < overrideTimeout) {
                overrideCounter += timeInterval;
                return;
            }
            //code to only run at long intervals
            if (!isModet) {
                if (bCheck) {
                    //No motion detection in the last long interval eg 30 min so send a capture
                    //this is managed by setting overrideCounter to 0 whenever motion detection happens
                    OnMotion([0]);
                } else {
                    //send a placeholder image
                    sendImage(false);
                }
            }
            isModet = false;
        }
    }
}

function isLocalOnly() {
    if (requestAppUrl.indexOf("example.com") > -1 || requestAppUrl == "placeholder"
        || requestAppUrl == "" || txtAccess.GetText() == "") {
        return true;
    } else {
        return false;
    }
}

//Called when user touches sensitivity seek bar.
//( value ranges from 0 to 100 )
function skb_OnTouch(value) {
    sensitivity = value;
    ChangeSettings();
}

function spinCam_OnTouch(item) {
    cameraNumber = parseInt(item);
}


//Called when user touches mosaic spinner.
//function spin_OnTouch(item) {
//    mosaic = item;
//    ChangeSettings();
//}

//Change the motion detection settings.
function ChangeSettings() {
    var x = 3, y = 2;
    if (mosaic == "3x3") { x = 3; y = 3; }
    else if (mosaic == "5x5") { x = 5; y = 5; }
    else if (mosaic == "10x10") { x = 10; y = 10; }

    cam.MotionMosaic(x, y, (100 - sensitivity) / 5, minPeriod, img);
}

//Add messages to log.
function Log(msg) {
    var maxLines = txt.GetMaxLines() - 1;
    if (log.length >= maxLines) log.shift();
    log.push(msg + "\n");
    txt.SetText(log.join(""));
}

function testRun() {
    if (!isTesting) {
        isTesting = true;
        if (btn.IsEnabled) {
            Log("TEST MODE: Press [Start] to begin");
        } else {
            Log("TEST MODE: cam on/off every " + timeInterval + " sec.");
        }
        btnTest.SetText("Stop Test");
    } else {
        isTesting = false;
        Log("NORMAL MODE: Scheduler will operate");
        btnTest.SetText("Start Test");
        sleepDetection();
        btn.SetEnabled(true);
    }

}

function thisAppExit() {
    app.Exit();
}

//-------------------------------------------
//Utilities

//This is returning Greenwich Mean Time (Universal Time) so deprecated 
//and replacing beginning with function xgetTime()
function xgetISONow() {
    //Finding issues with non-alphanumeric characters in file names so substitute
    //Date and time ends up looking like this. Split on "T" to separate date and time.
    //2019-01-25T213344D23

    var d = new Date();
    var s = d.toISOString();
    s = s.replace(/\:/g, "");
    s = s.replace(".", "D");
    s = s.slice(0, 20);
    return s;
}

//Returns local time in format HH:mm:ss.sss example 21:03:16.81
function xgetTime() {
    var d = new Date();
    var hours = ("00" + d.getHours()).slice(-2);
    var minutes = ("00" + d.getMinutes()).slice(-2);
    var seconds = ("00" + d.getSeconds()).slice(-2);
    var decimalSeconds = ("000" + d.getMilliseconds()).slice(-3).slice(0, 3);
    nowTime = hours + ":" + minutes + ":" + seconds + "." + decimalSeconds;
    return nowTime;
}
