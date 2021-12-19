using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using XMRemoteMonitor.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Data.SqlClient;
using System.Security.Principal;
using System.IO;
using SixLabors.ImageSharp;

namespace XMRemoteMonitor
{
    public class RobotHub : Hub
    {
        private readonly IHostingEnvironment _env;

        //20160813 JPC HOWTO read connectionString by dependency injection
        //20180818 JPC With move to WebSockets+SignalR we have dropped the database
        // We may bring it back later so comment-out _connectionString for now
        //private string _connectionString;
        private readonly string _appAccessKey;

        public RobotHub(IConfiguration configuration, IHostingEnvironment env)
        {
            //_connectionString = configuration.GetConnectionString("DefaultConnection");
            _appAccessKey = configuration.GetSection("AppSettings")["appAccessKey"];
            _env = env;
        }


        //20180717 JPC start with simplest working system
        //Extend later into control of trials online with access for limited time

        /*
         * 
        Example registration DEMO mode - commander device gets started with "categoryid":4 which triggers automatic channel allocation
        {"categoryid":4,"commanderid":0,"robotid":0,"accesskey":"demo-access-key"}
 
        Example registration - commander device gets started with "categoryid":5 
        {"categoryid":5,"commanderid":1,"robotid":101,"accesskey":"demo-access-key"}

        Example registration - robot device gets started with "categoryid":6
        {"categoryid":6,"commanderid":13,"robotid":113,"accesskey":"demo-access-key"}  
            
        Example Command
        {"categoryid":1,"commanderid":13,"robotid":113,"accesskey":"demo-access-key"
        ,"islog:0;"xdata":{ "sx1x1":92, "sx1x2":83, "sx2x1":0, "sx2x2":0}}
        */

        public async Task XSignal(string request)
        {
            string response = "";
            //unpack JSON
            //20180722 JPC change from JObject to NewtonSoft.Json.JsonConvert
            dynamic requestObject = JsonConvert.DeserializeObject(request);
            string accessKey = (string)requestObject.SelectToken("accesskey");
            //simple access check
            if (accessKey != _appAccessKey)
            {
                response = "{\"issuccess\": false, \"message\": \"Access Denied - contact app owner to check your registration and user status\"}";
                await Clients.Caller.SendAsync("XSignal", response);
                return;
            }

            int categoryId = (int)requestObject.SelectToken("categoryid");
            int commanderId = (int)requestObject.SelectToken("commanderid");
            int robotId = (int)requestObject.SelectToken("robotid");
            string message = (string)requestObject.SelectToken("message");

            //map recipient id to generated signalr Client Id
            int recipientId = 0;
            switch (categoryId)
            {
                case 1:
                    //Commander sending command to Robot
                    recipientId = robotId;
                    break;
                case 2:
                    //Robot sending data to Commander
                    recipientId = commanderId;
                    break;
                case 3:
                    //Telemetry for storage in the database on this server
                    break;
                case 4: //register a commander with auto channel number allocation
                case 5: //register a commander
                case 6: //register a robot
                    if (categoryId == 4) {
                        //Demo Commander needs a temporary id between 10 and 99
                        //Demo Robot will be this id plus 100
                        for (int i = 10; i <= 100; i++)
                        {
                            if (!CommsManager.ClientMap.ContainsKey(i))
                            {
                                if (i < 100)
                                {
                                    commanderId = i;
                                    robotId = i + 100;
                                    break;
                                }
                                else
                                {
                                    response = "{\"categoryid\":4, \"commanderid\":0, \"issuccess\":false, \"message\":\"System is too popular! This system is currently running its maximum number of allowed demos. Try again later and it may help to close and restart your browser.\"}";
                                    await Clients.Caller.SendAsync("XSignal", response);
                                    return;
                                }
                            }
                        }
                    }
                    //Get a SignalR userId and map that to the senderId
                    //ref: https://docs.microsoft.com/en-us/aspnet/signalr/overview/guide-to-the-api/mapping-users-to-connections
                    string userId = Context.ConnectionId;
                    int senderId = commanderId;
                    if (categoryId == 6) senderId = robotId;
                    if (CommsManager.ClientMap.ContainsKey(senderId))
                    { 
                        //2019-01-19 JPC use message as flag for taking channel number ownership
                        //Note that channel 0 is used for initial connection testing by all clients
                        if (message == "takeover" || commanderId == 0)
                        {
                            CommsManager.ClientMap.Remove(senderId);
                        }
                        else if(CommsManager.ClientMap[senderId] == userId)
                        {
                            response = "{\"categoryid\":" + categoryId + ",\"commanderid\":" + commanderId + ",\"robotid\":" + robotId 
                            + ", \"issuccess\":false, \"message\":\"You are already registered to use this channel. You do not need to request it again\"}";
                            await Clients.Caller.SendAsync("XSignal", response);
                            return;
                        }
                        else
                        {
                            response = "{\"categoryid\":" + categoryId + ",\"commanderid\":0, \"issuccess\":false, \"message\":\"This channel is already registered. You can select another channel or request a takeover of this one.\"}";
                            await Clients.Caller.SendAsync("XSignal", response);
                            return;
                        }
                    }
                    CommsManager.ClientMap.Add(senderId, userId);
                    response = "{\"categoryid\":" + categoryId
                        + ",\"commanderid\":" + commanderId + ",\"robotid\":" + robotId
                        + ", \"issuccess\":true, \"message\":\"SignalR connection is ready.\"}";
                    await Clients.Caller.SendAsync("XSignal", response);
                    return;
                default:
                    response = "{\"issuccess\":false, \"message\":\"Error: this message has an unrecognised categoryid = "
                        + categoryId
                        + ". We currently support values of 1 for command, 2 for robot reporting to commander, 3 for telemetry, 4 for demo commander registration, 5 for commander registration, 6 for robot registration \" }";
                    await Clients.Caller.SendAsync("XSignal", response);
                    return;
            }

            //Is there a need to pass the message on? 
            if(recipientId > 0)
            {
                //Lookup the in-memory dictionary for the SignalR ClientId
                string userId = "";
                if(CommsManager.ClientMap.TryGetValue(recipientId, out userId))
                {
                    //20180718 JPC not working with Clients.User - fix was change to Clients.Client
                    //information source: the aspnet/SignalR project on Github
                    //https://github.com/aspnet/SignalR/blob/release/2.2/samples/SignalRSamples/Hubs/Chat.cs
                    //line 34 as retrieved July 18, 2018
                    string[] signalArray = request.Split(",");
                    for(int i = 0; i < signalArray.Length; i++)
                    {
                        if(signalArray[i].IndexOf("accesskey") > -1)
                        {
                            signalArray[i] = "\"issuccess\":true";
                            break;
                        }
                    }
                    response = String.Join(",", signalArray);
                    await Clients.Client(userId).SendAsync("XSignal", response);
                }
                else
                {
                    response = "{\"categoryid\":" + categoryId + "\"issuccess\":false, \"message\": \"Error: recipient id " + recipientId + " not found \" }";
                    await Clients.Caller.SendAsync("XSignal", response);
                }
            }
        }
    }
}
