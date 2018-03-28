using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parser.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ChordsBot.Api.Controllers
{
    [Route("api/chordsBot")]
    [Authorize(AuthenticationSchemes = "TelegramAuthScheme")]
    public class ChordsBotController : Controller
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ICollection<IChordsFinder> _chordsFinders;
        
        public ChordsBotController(
            ITelegramBotClient botClient, 
            ICollection<IChordsFinder> chordsFinders)
        {
            _botClient = botClient;
            _chordsFinders = chordsFinders;
        }

        [HttpPost]
        [Route("init")]
        public async Task<string> Init(string telegramToken)
        {
            var webHookUrl = Url.RouteUrl("webHook", new { telegramToken }, Request.Scheme, Request.Host.ToString());
            
            await InitWebHook(webHookUrl);

            return webHookUrl;
        }

        [HttpGet]
        [Route("test")]
        public async Task<string> Test(string query)
        {
            var result = await await Task.WhenAny(_chordsFinders.Select(x => x.FindChords(query)));
            var finalResult = string.Empty;

            result.Match(
                x => finalResult = x,
                x => finalResult = x
            );

            return finalResult;
        }

        [HttpPost]
        [Route("webhook/{telegramToken}", Name = "webHook")]
        public async Task WebHook(string telegramToken, Update update)
        {
            if (update.Type == UpdateType.MessageUpdate)
            {
                var text = update.Message.Text;

                var result = await await Task.WhenAny(_chordsFinders.Select(x => x.FindChords(text)));
                var chatId = update.Message.Chat.Id;

                await result.Match(
                    async x => await _botClient.SendTextMessageAsync(chatId, x), 
                    async x => await _botClient.SendTextMessageAsync(chatId, "Can't find song.")
                );
            }
        }
        
        private async Task InitWebHook(string webHookUrl)
        {
            var info = await _botClient.GetWebhookInfoAsync();

            if (string.IsNullOrEmpty(info.Url))
            {
                await _botClient.SetWebhookAsync(webHookUrl);
            }
        }
    }
}
