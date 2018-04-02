using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ChordsBot.Api.Controllers
{
    [Route("api/chords/[action]")]
    [Authorize(AuthenticationSchemes = "TelegramAuthScheme")]
    public class ChordsController : Controller
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IChordsService _chordsService;
        private readonly IChordsFormatter _chordsFormatter;
        
        public ChordsController(
            ITelegramBotClient botClient, 
            IChordsService chordsService,
            IChordsFormatter chordsFormatter)
        {
            _botClient = botClient;
            _chordsService = chordsService;
            _chordsFormatter = chordsFormatter;
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
            await ProcessUpdate(update);
        }

        private async Task ProcessUpdate(Update update)
        {
            if (update.Type == UpdateType.MessageUpdate)
            {
                var text = update.Message.Text;
                var chatId = update.Message.Chat.Id;

                var link = await _chordsService.FindFirst(text);
                var chords = await link.Bind(x => _chordsService.Get(x));
                var result = link.Bind(x => 
                    chords.Bind(y => 
                        ToTextFile($"{x.SongAuthor} - {x.SongName}", y).Return()
                    ));

                await result.Match(
                    async file => await _botClient.SendDocumentAsync(chatId, file),
                    async error => await _botClient.SendTextMessageAsync(chatId, error)
                );
            }
        }

        private static FileToSend ToTextFile(string name, string content)
        {
            var byteArray = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(byteArray);

            return new FileToSend($"{name}.txt", stream);
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
