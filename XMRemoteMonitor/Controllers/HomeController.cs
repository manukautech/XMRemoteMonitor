﻿/*
      Project XMRemoteMonitor - HomeController
      HomeController is Server-side code for page loading 
      and AJAX request handling.
      Project XMRemoteMonitor is an Open Source project. 
      See Project home repository on Github for details:
      https://github.com/manukautech/XMRemoteMonitor
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using Newtonsoft.Json;
using XMRemoteMonitor.Models;

namespace XMRemoteMonitor.Controllers
{
	public class HomeController : Controller
	{

		private IHostingEnvironment _env;

		//20160813 JPC HOWTO read connectionString in a controller
        //20180818 JPC With move to WebSockets+SignalR we have dropped the database
        // We may bring it back later so comment-out _connectionString for now
		//private string _connectionString;
		private readonly string _appAccessKey;
        private readonly bool _debug = false;
        private readonly bool _nightfolders = true;

        public HomeController(IConfiguration configuration, IHostingEnvironment env)
		{
			//_connectionString = configuration.GetConnectionString("DefaultConnection");
			_appAccessKey = configuration.GetSection("AppSettings")["appAccessKey"];
            //string debug = configuration.GetSection("AppSettings")["debug"].ToLower();
            ////"readonly" value as above can be changed in the constructor
            //if (debug == "true") _debug = true;
            _debug = Convert.ToBoolean(configuration.GetSection("AppSettings")["debug"]);
            _nightfolders = Convert.ToBoolean(configuration.GetSection("AppSettings")["nightfolders"]);
            _env = env;
		}

		public IActionResult Index()
		{
            return Redirect("~/Index.html");
        }

       
        //AJAX follow SignalR established convention of categoryid integer values 
        //starting at 20 = GetSettings
        //2019-02-01 Add parameters for variety of startup scenarios. Change from [HttpGet] to [HttpPost]
        [HttpPost]
        public string GetSettings(bool IsImageDownload = true, bool IsAuth = false, string AccessKey = "")
        {

            ResponseAJAX response = new ResponseAJAX()
            {
                categoryid = 20,
                method = "GetSettings",
                issuccess = true,
                debug = _debug,
                message = "Server Connection OK"
            };

            //AccessKey check
            if (IsAuth && AccessKey == _appAccessKey)
            {
                response.issuccess = true;
                response.message = "Access Key confirmed.";
            }
            else if(IsAuth)
            {
                //Authentication fail
                response.issuccess = false;
                response.message = "Needs Access Key.";
            }

            if (IsImageDownload)
            {
                //2019-01-27 JPC sending placeholder image data is now only for client apps that may save it locally
                //Method ImagePost below now handles saving placeholders on the server in response to an options flag
                List<string> imgBase64 = new List<string>();
                imgBase64.Add(System.IO.File.ReadAllText(_env.WebRootPath + "\\res\\PlaceholderBase64.txt"));
                response.xdata = imgBase64;
            }
            //2019-01-16 apply our new convention of "signal" as the keyword for a package of metadata and data in json string format
            string signal = JsonConvert.SerializeObject(response);
            return signal;
        }

        //2019-02-02 JPC ver 0.9 For backwards compatibility with ver 0.8 e.g. to support test cameras 
        //out in the field running local apps that will take time get to for updates
        [HttpGet]
        public string GetSettings()
        {
            return GetSettings(true, false, "");
        }


        [HttpPost]
        public string ImagePost(string CameraNumber, string AccessKey, string ImageBase64, string options = "base64")
        {
            ResponseAJAX response = new ResponseAJAX();
            response.categoryid = 21;
            response.method = "ImagePost";

            //AccessKey check
            if (AccessKey != _appAccessKey)
            {
                response.issuccess = false;
                response.message = "Needs Access Key.";
                string jsonNoLogin = JsonConvert.SerializeObject(response);
                return jsonNoLogin;
            }

            string webRoot = _env.WebRootPath;
            //2019-04-19 JPC group images in night folders 12midday 12:00 
            double hoursShift = 0.0;
            if (_nightfolders) hoursShift = -12.0;
            DateTime dayShift = DateTime.Now.AddHours(hoursShift);
            string folderName = dayShift.ToString("yyyy-MM-dd") + "\\" + CameraNumber;
            System.IO.Directory.CreateDirectory(webRoot + "\\modet\\" + folderName);
            string fileWithLocation = "modet\\" + folderName + "\\" + XDayTime() + ".jpg";
            string filePath = Path.Combine(webRoot, fileWithLocation);

            if(options == "placeholder")
            {
                //2019-01-27 JPC handle placeholder by sending an options message rather than the full data
                try
                {
                    //copy file "Placeholder.jpg" from its "res" folder to the recorded sequence of images
                    string placeholderPath = Path.Combine(webRoot, "res\\Placeholder.jpg");
                    System.IO.File.Copy(placeholderPath, filePath);
                    response.message = fileWithLocation.Replace("\\", "/");
                    response.issuccess = true;
                }
                catch (Exception e)
                {
                    response.issuccess = false;
                    response.message = "ERROR: On saving 'Placeholder' image " + e.Message + ".";
                }
            }
            else
            {
                //main process, recording image from client camera
                string tracking = "Begin Base64 conversion to binary.";
                //var textFilePath = Path.Combine(webRoot, "images\\MotionDetected\\" + folderName + "\\log.txt");
                try
                {
                    var bytes = Convert.FromBase64String(ImageBase64);
                    if (bytes.Length < 1000)
                    {
                        response.issuccess = false;
                        response.message = "INCOMPLETE data.";
                        string jsonIncomplete = JsonConvert.SerializeObject(response);
                        return jsonIncomplete;
                    }
                    tracking = "Image.Load(bytes) ";
                    var img = Image.Load(bytes);

                    tracking = "img.SaveAsJpeg ";
                    using (var imageFile = new FileStream(filePath, FileMode.Create))
                    {
                        img.SaveAsJpeg(imageFile);
                    }
                    response.message = fileWithLocation.Replace("\\", "/");
                    response.issuccess = true;
                }
                catch (Exception e)
                {
                    response.issuccess = false;
                    response.message = "ERROR: " + tracking + " " + e.Message + ".";
                }
            }
            string signal = JsonConvert.SerializeObject(response);
            return signal;
        }

        //2018-12-26
        [HttpPost]
        public string ImageList(string AccessKey, string dateFolder, string cameraFolder)
        {
            ResponseAJAX response = new ResponseAJAX();
            response.categoryid = 22;
            response.method = "ImageList";

            //AccessKey check
            if (AccessKey != _appAccessKey)
            {
                response.issuccess = false;
                response.message = "Needs Access Key.";
                string jsonNoLogin = JsonConvert.SerializeObject(response);
                return jsonNoLogin;
            }

            string xFolder = _env.WebRootPath + "\\modet";
            if (!(dateFolder=="none"))
            {
                xFolder += "\\" + dateFolder;
            }
            if(!(cameraFolder == "none"))
            {
                xFolder += "\\" + cameraFolder;
            }
            try
            {
                //List<string> itemList = Directory.EnumerateFileSystemEntries(xFolder).ToList();
                List<string> itemList = Directory.GetFileSystemEntries(xFolder).ToList();
                for(int i = 0; i < itemList.Count; i++)
                {
                    string[] itemArray = itemList[i].Split("\\");
                    itemList[i] = itemArray[itemArray.Length - 1];
                }
                itemList.Sort();
                if(dateFolder == "none" || !(cameraFolder == "none")) itemList.Reverse();
                response.xdata = itemList;
                response.issuccess = true;
                string message = "modet";
                if (!(dateFolder == "none")) message += "/" + dateFolder;
                if (!(cameraFolder == "none")) message += "/" + cameraFolder;
                response.message = message;
            }
            catch (Exception e)
            {
                response.issuccess = false;
                response.message = "ERROR: " + e.Message + ".";
            }

            string signal = JsonConvert.SerializeObject(response);
            return signal;
        }

		public IActionResult Error()
		{
			return View();
		}

        //Utility time in milliseconds since start of day
        private string XDayMilliseconds()
        {
            DateTime d = DateTime.Now;
            Double m = d.Hour * 3600000 + d.Minute * 60000 + d.Second * 1000 + d.Millisecond;
            string s = "00000000" + m.ToString();
            s = s.Substring(s.Length - 8);
            return s;
        }

        //2019-04-19 JPC group images in night folders 12:00:00
        //to the following 11:59:59.99
        private string XDayTime()
        {
            //ref: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings?view=netframework-4.7.2
            string s = DateTime.Now.ToString("HH-mm-ss.ff");
            if(_nightfolders)
            {
                int hours = Convert.ToInt32(s.Substring(0, 2));
                if (hours >= 12)
                {
                    s = "A_" + s;
                }
                else
                {
                    s = "B_" + s;
                }
            }
            return s;
        }
	}
}