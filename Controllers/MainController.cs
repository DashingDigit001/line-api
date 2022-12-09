using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace LineAPI.Controllers
{

    public class RequestContent
    {
        public string destination { get; set; }
        public Event[] events { get; set; }


    }
    

    public class Event
    {
        public string type { get; set; }

        public Message message { get; set; }

        public string webhookEventId { get; set; }
        public DeliveryContext deliveryContext { get; set; }
        public long timestamp { get; set; }  
        public Source source { get; set; }
        public string replyToken{ get; set; }
        public string mode{ get; set; }




    }

    public class Message
    {
        public string type { get; set; }
        public string id { get; set; }
        public string text { get; set; }
    }

    public class DeliveryContext
    {
        public Boolean isRedelivery { get; set; }

    }
    public class Source
    {
        public string type { get; set; }

        public string userId { get; set; }

    }



    public class MainController : Controller
    {
        private readonly ILogger<MainController> _logger;

        public MainController(ILogger<MainController> logger)
        {
            _logger = logger;
            _logger.LogDebug(1, "NLog injected into HomeController");
        }

        string channelAccseeToken = "1jhKN4KaMZ03Sdsi2RW5b7+MEIZCWcp4HfndnVjfqX3Tdx/1/7lc7HBnbfIML4VF8XpjJbJDmFKp/GrLeBZYWFRyb5i3AZWe2ypp2ZhuPEv0BpGIGYAsHjm7i1CQACDEw7FPLd0jPlj4Rh3M37W42QdB04t89/1O/w1cDnyilFU=";
        string getBotProfileUrl = "https://api.line.me/v2/bot/info";
        string getUserProfileUrl = "https://api.line.me/v2/bot/profile/";
        string replyMessageUrl = "https://api.line.me/v2/bot/message/reply";


        [HttpPost]
        [Route("callbyline")]
        public async Task<object> CallByLine([FromBody] object body)
        {
            try
            {
                
                _logger.LogInformation("this is body" + body.ToString());

                JObject bodyObject = JsonConvert.DeserializeObject<JObject>(body.ToString());
                string type = bodyObject["events"][0]["type"].ToString();

                _logger.LogInformation("this is type" + type);

                //依照聊天室不同的 evnet 執行不同的函式
                switch (type)
                {
                    //user 傳送訊息到聊天室
                    case "message":
                        onMessageEvent(bodyObject);
                        break;
                    //user 加入聊天室                    
                    case "follow":
                        onFollowEvent(bodyObject);
                        break;

                    case "unfollow":
                        onUnfollowEvent(bodyObject);
                        break;
                }
                string userId = bodyObject["events"][0]["source"]["userId"].ToString();
                _logger.LogInformation("this is userId" + userId );
            }
            catch (Exception ex)
            {
                _logger.LogError("this is error" + ex.Message);
                _logger.LogError("this is error StackTrace" + ex.StackTrace);
            }

            return "success";
        }

        public void onMessageEvent(JObject body) {
            _logger.LogInformation("function onMessageEvent");

                executeMessageEvent(body);
        }
        public async void  executeMessageEvent(JObject body) {
            _logger.LogInformation("function executeMessageEvent");

            Dictionary<string, string> headers = new()
            {
                { "Content-Type", "application/json" },
                { "Authorization", "Bearer " + channelAccseeToken }
            };

            List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>() ;
            messages.Add(new Dictionary<string, string>()
            {
                { "type", "text" },
                { "text", "Hello world" }
            });

            var replyToken = body["events"][0]["replyToken"].ToString();
            _logger.LogInformation("this is replyToken" + replyToken);

            Dictionary<string, object> replyMessagebody = new()
            {
                { "replyToken", replyToken},
                { "messages",messages }
            };


            try
            {
                var replyMessageRequest = await replyMessageUrl.WithHeaders(headers).PostJsonAsync(replyMessagebody);
                _logger.LogInformation("this is replyMessageRequest" + replyMessageRequest);

                string replyMessageRaw = await replyMessageRequest.ResponseMessage.Content.ReadAsStringAsync();
                _logger.LogInformation("this is replyMessageRaw" + replyMessageRaw);

                var replyMessageResult = JsonConvert.DeserializeObject<JObject>(replyMessageRaw);
                _logger.LogInformation("this is replyMessageResult" + replyMessageResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("this is messageEvent error" + ex.Message);
                _logger.LogError("this is messageEvent error StackTrace" + ex.StackTrace);
            }

          

        }

        public void onFollowEvent(JObject body)
        {
            _logger.LogInformation("function onFollowEvent");
            executeFollowEvent(body);
        }
        public async void executeFollowEvent(JObject body)
        {
            _logger.LogInformation("function executeFollowEvent");
            //取得 line bot profile
            var botProfileRequest = await getBotProfileUrl.WithHeader("Authorization", "Bearer " + channelAccseeToken).GetAsync();
            string botProfileRaw = await botProfileRequest.ResponseMessage.Content.ReadAsStringAsync();
            var botProfile = JsonConvert.DeserializeObject<JObject>(botProfileRaw);

            //bot Id
            string basicId = botProfile["basicId"].ToString();
            _logger.LogInformation("this is basicId" + basicId);
            _logger.LogInformation("this is botProfile" + JsonConvert.SerializeObject(botProfile));

            //取得 line userId
            string userId = body["events"][0]["source"]["userId"].ToString();

            //取得跟 line bot 聊天的 user profile
            string getBotProfileAndUserIdUrl = getUserProfileUrl + userId;
            var userProfileRequest = await getBotProfileAndUserIdUrl.WithHeader("Authorization", "Bearer " + channelAccseeToken).GetAsync();
            string userProfileRaw = await userProfileRequest.ResponseMessage.Content.ReadAsStringAsync();
            var userProfile = JsonConvert.DeserializeObject<JObject>(userProfileRaw);

            string userDisplayName = userProfile["displayName"].ToString();

            _logger.LogInformation("this is userDisplayName" + userDisplayName);
            _logger.LogInformation("this is userProfile " + JsonConvert.SerializeObject(userProfile));


        }
        public void onUnfollowEvent(JObject body)
        {
            executeUnfollowEvent(body);
        }
        public void executeUnfollowEvent(JObject body)
        {

        }
    }

    
}
