/*
      Project XMRemoteMonitor - Welcome, info and test page designs are served from here.
      v2.0 - change to SignalR - communications hub is now "RobotHub.cs"
      Copyright © 2017 John Calder as project leader
      Licensed under the "Open Source" Apache License, Version 2.0. 
      See file "LICENSE" in the project root for license information.
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

		public HomeController(IConfiguration configuration, IHostingEnvironment env)
		{
			//_connectionString = configuration.GetConnectionString("DefaultConnection");
			_appAccessKey = configuration.GetSection("AppSettings")["appAccessKey"];
            string debug = configuration.GetSection("AppSettings")["debug"].ToLower();
            //"readonly" value as above can be changed in the constructor
            if (debug == "true") _debug = true;
            _env = env;
		}

		public IActionResult Index()
		{
            return Redirect("~/Index.html");
        }

       
        //AJAX follow SignalR established convention of categoryid integer values 
        //for the category of action, starting at 20 = getsettings
        [HttpGet]
        public string GetSettings()
        {
            ResponseAJAX response = new ResponseAJAX()
            {
                categoryid = 20, method = "GetSettings", issuccess = true, debug = _debug
            };
            string json = JsonConvert.SerializeObject(response);
            return json;
            //Previous version had low level code which looked like this example
            //string json = "{\"categoryid\":20, \"method\":\"GetSettings\", \"issuccess\":true, \"debug\":" + _debug + "}";
        }

        [HttpPost]
        public string ImagePost(string CameraNumber, string AccessKey, string ImageBase64)
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
            string folderName = DateTime.Now.ToString("yyyy-MM-dd") + "\\" + CameraNumber;
            System.IO.Directory.CreateDirectory(webRoot + "\\modet\\" + folderName);
            string fileWithLocation = "modet\\" + folderName + "\\" + XDayTime() + ".jpg";
            var filePath = Path.Combine(webRoot, fileWithLocation);

            string tracking = "Begin Base64 conversion to binary.";
            
            //var textFilePath = Path.Combine(webRoot, "images\\MotionDetected\\" + folderName + "\\log.txt");
            try {
                //20181220 JPC experiment of saving the base64 version of the image
                //Works with web interfaces but not for local apps like Image Viewer or Explorer icons.
                //System.IO.File.AppendText(filePath).Write(ImageBase64);

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
            string jsonResponse = JsonConvert.SerializeObject(response);
            return jsonResponse;
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

            string json = JsonConvert.SerializeObject(response);
            return json;
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

        private string XDayTime()
        {
            //ref: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings?view=netframework-4.7.2
            string s = DateTime.Now.ToString("HH-mm-ss.ff");
            return s;
        }
	}
}