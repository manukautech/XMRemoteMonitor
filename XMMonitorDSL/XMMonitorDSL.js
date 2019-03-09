//XMMonitorDSL
//Remote monitoring camera Android client for use with the XMRemoteMonitor server app.
//Part of the open source project "XMRemoteMonitor" - ref:
//https://github.com/manukautech/XMRemoteMonitor

//Attribution:
//"XMMonitorDSL" is based on the sample app "Camera Motion" by droidscript.org
//"Camera Motion" is part of the Sample App documentation of the "DroidScript" package which is available in Google Play:
//https://play.google.com/store/apps/details?id=com.smartphoneremote.androidscriptfree
//(DroidScript is a package including IDE and APIs for programming Android devices with JavaScript)

//Initialise variables with variables most needed for configuration up first
//Server app root URL if you have one. Do NOT end with "/". The code will add that.
//Otherwise "placeholder" or "example.com" (all in lower case) 
//will give local-only recording of images, keeping the most recent 2000
var requestAppUrl = "placeholder";

var sensitivity = 40; //percent.
var minPeriod = 200;  //millisecs.
var mosaic = "5x5";
var captureFolder = "/sdcard/modet/";
var cameraNumber = 1;

//State management
//2018-12-26 JPC trigger override capture without motion detection on long timeout
var isModet = false;  //code sets isModet to true on motion detection events
var overrideTimeout = 900000; //usually 900000 = 900 seconds ie 15 min
var counter = 1000;
var placeholderBase64DSL = ""; //image to record when scheduler specifies off-time

//Setup AJAX  refs
//https://www.w3schools.com/xml/ajax_xmlhttprequest_send.asp
//https://www.w3schools.com/js/js_ajax_http.asp
var xmlhttp = new XMLHttpRequest();
var cam;
var hasReadSettings = false;

function scheduledActive() {
    //comment-out next statement "return true;" to switch on scheduling
    //return true;

    //Scheduled active from 7pm to 6:30am on week-nights plus all weekend
    //'return true' effect is to capture images on motion detection
    //'return false' effect is to NOT capture images on motion detection
    var d = new Date();
    //Remain active through the weekend
    if (d.getDay() == 6 || d.getDay() == 0) {
        return true;
    }
    var dayMinutes = d.getHours() * 60 + d.getMinutes();
    if (dayMinutes >= 19 * 60) {
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

    //Create camera view control. Work with image resolution 
    //"CIF" = 352 x 240, "VGA" = 640 x 480 , higher options eg "XGA" are giving 640 x 480
    cam = app.CreateCameraView(0.4, 0.8, "CIF,UseBitmap");
    cam.SetSound(false);
    layLeft.AddChild(cam);

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
    btn.SetOnTouch(StartDetection);
    btn.SetMargins(0.02, 0, 0, 0);
    btn.SetPadding(0, 0, 0, 0.01);
    btn.SetTextColor("#ffffffff");
    btn.SetBackColor("#ff222222");
    layLogin.AddChild(btn);

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

    //Add main layout to app.
    app.AddLayout(lay);

    //Initialise AJAX
    xmlhttp.onreadystatechange = function () {
        ajaxCallback();
    };

    //"Start capture after 1 second" replaced with Start button
    //setTimeout( "StartDetection()", 1000 ); 
}

//Start motion detection.
function StartDetection() {
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
    w = cam.GetImageWidth();
    h = cam.GetImageHeight();
    img = app.CreateImage(null, 0.4, 0.8, "fix", w, h);
    img.SetAlpha(0.5);
    layLeft.AddChild(img);

    //Enable 10x10 matrix motion detection and mark
    //motion detections in the image control.
    //for "mosaic" value settings like "10x10", split on "x" to get the numeric values
    var mx = parseInt(mosaic.split("x")[0]);
    var my = parseInt(mosaic.split("x")[1]);
    cam.MotionMosaic(mx, my, (100 - sensitivity) / 5, minPeriod, img);
    cam.SetOnMotion(OnMotion);

    //Create folder for saved pictures 
    app.MakeFolder(captureFolder);
    //cam.SetPictureSize(1024, 768);

    //Create array to hold log messages.
    log = new Array();
    Log(captureFolder);

    //Start preview.
    cam.StartPreview();

    //Record a placeholder image if no motion detection for the specified time
    setInterval(idleOverride, overrideTimeout);
}


//Handle menu selections.
function OnMenu(name) {
    if (name == "Capture")
        idleOverride();
    else if (name == "Exit")
        app.Exit();
}

//Called when motion is detected.
//(data contains an array of detection strength 
//values corresponding to each mosaic tile)
function OnMotion(data) {
    if (isLocalOnly()) {
        sendImage(scheduledActive());
    } else if (scheduledActive()) {
        isModet = true;
        sendImage(true);
    } else {
        isModet = false;
        Log("Scheduled off, ignore " + xgetTime());
    }
}

//Send new image or placeholder to the server
function sendImage(isScheduled) {
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
        request = cam.GetPixelData("jpgbase64", 0, 0, w, h);
        if (request == null) {
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
        app.ShowPopup(jsonObject.message);
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
    if (!isModet) {
        if (scheduledActive()) {
            //No motion detection in the last 15 min so send a capture
            OnMotion([0]);
        } else {
            //send a placeholder image.
            sendImage(false);
        }
    }
    isModet = false;
}

function isLocalOnly() {
    if (requestAppUrl.indexOf("example.com") > -1 || requestAppUrl == "placeholder") {
        return true;
    } else {
        return false;
    }
}

//-------------------------------------------
//Utilities

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
function spin_OnTouch(item) {
    mosaic = item;
    ChangeSettings();
}

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