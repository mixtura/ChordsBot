using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChordsBot.Api.Interfaces;
using ChordsBot.Common;
using ChordsBot.Interfaces;
using ChordsBot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;

namespace ChordsBot.Api.Implementation
{
    public class BotUpdateProcessor : IBotUpdateProcessor
    {
        private const string SelectCommandName = "select_";
        private static readonly IDictionary<string, ChordsSearchResults> Cache
            = new ConcurrentDictionary<string, ChordsSearchResults>();

        private readonly ITelegramBotClient _botClient;
        private readonly IChordsService _chordsService;
        private readonly IChordsFormatter _chordsFormatter;

        public BotUpdateProcessor(
            ITelegramBotClient botClient, 
            IChordsService chordsService, 
            IChordsFormatter chordsFormatter)
        {
            _botClient = botClient;
            _chordsService = chordsService;
            _chordsFormatter = chordsFormatter;
        }

        public async Task Process(Update update)
        {
            switch (update.Type)
            {
                case UpdateType.InlineQueryUpdate:
                {
                    await ProcessInlineQueryUpdate(update);
                    break;
                }

                case UpdateType.MessageUpdate:
                {
                    await ProcessMessageUpdate(update);
                    break;
                }
            }
        }

        private async Task ProcessMessageUpdate(Update update)
        {
            var text = update.Message.Text;
            var chatId = update.Message.Chat.Id;

            if (text.StartsWith('/'))
            {
                await ProcessCommand(text.TrimStart('/'), chatId);
            }
            else
            {
                var link = await _chordsService.FindFirst(text);
                var chords = await link.Bind(x => _chordsService.Get(x));
                var chordsFile = ToChordsFileResult(link, chords);

                await SendFile(chordsFile, chatId);
            }
        }

        private async Task ProcessInlineQueryUpdate(Update update)
        {
            var text = update.InlineQuery.Query;
            var inlineQueryId = update.InlineQuery.Id;
            var cacheKey = string.Join('_', text.Split(' '));

            var result = !Cache.ContainsKey(cacheKey)
                ? Cache[cacheKey] = await _chordsService.FindChords(text)
                : Cache[cacheKey];

            var inlineQueryResults = result.Results.Take(50)
                .Select((x, index) => new InlineQueryResultContact
                {
                    Id = index.ToString(),
                    Title = x.SongName,
                    PhoneNumber = x.SongAuthor,
                    FirstName = x.SongName,
                    ThumbUrl = x.Thumbnail.ToString(),
                    InputMessageContent = new InputTextMessageContent
                    {
                        MessageText = $"/{SelectCommandName}{cacheKey}_{index}"
                    }
                }).ToArray();

            await _botClient.AnswerInlineQueryAsync(inlineQueryId, inlineQueryResults);
        }

        private async Task ProcessCommand(string command, long chatId)
        {
            switch (command)
            {
                case var x when x.StartsWith(SelectCommandName):
                {
                    await ProcessSelectCommand(command, chatId);
                    break;
                }
            }
        }

        private async Task ProcessSelectCommand(string command, long chatId)
        {
            var (index, cacheKey) = ParseSelectCommand(command);

            var link = Cache.GetAsResult(cacheKey).Bind(x => x.Results.GetByIndexAsResult(index));
            var chords = await link.Bind(x => _chordsService.Get(x));

            var chordsFile = ToChordsFileResult(link, chords);

            await SendFile(chordsFile, chatId);
        }

        private Result<FileToSend?> ToChordsFileResult(Result<ChordsLink> link, Result<string> chords)
        {
            var result = link.Bind(l =>
                chords.Bind(c =>
                {
                    var formattedChords = _chordsFormatter.Format(l, c);
                    var fileName = $"{l.SongAuthor} - {l.SongName}";

                    return ToTextFile(fileName, formattedChords).Return();
                })
            );

            return result;
        }

        private async Task SendFile(Result<FileToSend?> file, long chatId)
        {
            await file.Match(
                async f => await _botClient.SendDocumentAsync(chatId, f ?? default(FileToSend)),
                async err => await _botClient.SendTextMessageAsync(chatId, err)
            );
        }

        private static (int index, string cacheKey) ParseSelectCommand(string command)
        {
            var index = int.Parse(command.Substring(command.LastIndexOf('_') + 1));
            var cacheKey = command.Substring(SelectCommandName.Length, command.LastIndexOf('_') - SelectCommandName.Length);

            return (index, cacheKey);
        }

        private static FileToSend? ToTextFile(string name, string content)
        {
            var byteArray = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(byteArray);

            return new FileToSend($"{name}.txt", stream);
        }
    }
}
