using System.Threading.Tasks;
using ChordsBot.Api.Interfaces;
using ChordsBot.Common;
using ChordsBot.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ChordsBot.Api.Controllers
{
    [Route("api/chords/[action]")]
    [Authorize(AuthenticationSchemes = "TelegramAuthScheme")]
    public class ChordsController : Controller
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IChordsService _chordsService;
        private readonly IChordsFormatter _chordsFormatter;
        private readonly IBotUpdateProcessor _botUpdateProcessor;
        
        public ChordsController(
            ITelegramBotClient botClient, 
            IChordsService chordsService,
            IChordsFormatter chordsFormatter,
            IBotUpdateProcessor botUpdateProcessor)
        {
            _botClient = botClient;
            _chordsService = chordsService;
            _chordsFormatter = chordsFormatter;
            _botUpdateProcessor = botUpdateProcessor;
        }

        [HttpPost]
        public async Task<string> Init(string telegramToken)
        {
            var webHookUrl = Url.RouteUrl("webHook", new { telegramToken }, Request.Scheme, Request.Host.ToString());
            
            await InitWebHook(webHookUrl);

            return webHookUrl;
        }

        [HttpGet]
        public async Task<string> Test(string query)
        {
            var link = await _chordsService.FindFirst(query);
            var chords = await link.Bind(x => _chordsService.Get(x));
            var result = link.Bind(x => chords.Bind(y => _chordsFormatter.Format(x, y).Return()));

            return result.ToString();
        }

        [HttpPost]
        [Route("{telegramToken}", Name = "webHook")]
        public async Task WebHook(string telegramToken, [FromBody]Update update)
        {
            await _botUpdateProcessor.Process(update);
        }
        
        private async Task InitWebHook(string webHookUrl)
        {
            var webHookInfo = await _botClient.GetWebhookInfoAsync();

            if (!string.IsNullOrEmpty(webHookInfo.Url))
            {
                await _botClient.DeleteWebhookAsync();
            }

            await _botClient.SetWebhookAsync(webHookUrl);
        }
    }
}
