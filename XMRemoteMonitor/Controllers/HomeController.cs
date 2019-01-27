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
            //2019-01-27 JPC the sending of placeholder data is now only for backward compatibility
            //method ImagePost below now handles this task all on the server in response to an options flag
            List<string> imgBase64 = new List<string>();
            imgBase64.Add(System.IO.File.ReadAllText(_env.WebRootPath + "\\res\\PlaceholderBase64.txt"));

            ResponseAJAX response = new ResponseAJAX()
            {
                categoryid = 20,
                method = "GetSettings",
                issuccess = true,
                debug = _debug,
                xdata = imgBase64
            };
            //2019-01-16 apply our new convention of "signal" as the keyword for a package of metadata and data in json string format
            string signal = JsonConvert.SerializeObject(response);
            return signal;
            //Previous version had low level code which looked like this example
            //string json = "{\"categoryid\":20, \"method\":\"GetSettings\", \"issuccess\":true, \"debug\":" + _debug + "}";
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
            string folderName = DateTime.Now.ToString("yyyy-MM-dd") + "\\" + CameraNumber;
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

        private string XDayTime()
        {
            //ref: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings?view=netframework-4.7.2
            string s = DateTime.Now.ToString("HH-mm-ss.ff");
            return s;
        }
	}
}