using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private static class Format
        {
            public const string Message = "message";
            public const string Txt = "txt";
        }

        private const string SelectCommandName = "select";
        private const string SetFormatCommandName = "setFormat";
        private const int MaxResultsForInlineSearch = 50;
        private const int MessageMaxLength = 4096;

        private static readonly IDictionary<long, string> FormatMapping;
        private static readonly IDictionary<string, ChordsSearchResults> Cache;

        private readonly ITelegramBotClient _botClient;
        private readonly IChordsService _chordsService;
        private readonly IChordsFormatter _chordsFormatter;

        static BotUpdateProcessor()
        {
            FormatMapping = new ConcurrentDictionary<long, string>();
            Cache = new ConcurrentDictionary<string, ChordsSearchResults>(StringComparer.CurrentCultureIgnoreCase);
        }

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
        
        private async Task ProcessInlineQueryUpdate(Update update)
        {
            var text = update.InlineQuery.Query;
            var inlineQueryId = update.InlineQuery.Id;
            var cacheKey = string.Join('_', text.Split(' '));

            var result = !Cache.ContainsKey(cacheKey)
                ? Cache[cacheKey] = await _chordsService.FindChords(text)
                : Cache[cacheKey];

            var inlineQueryResults = result.Results.Take(MaxResultsForInlineSearch)
                .Select((x, index) => new InlineQueryResultContact
                {
                    Id = index.ToString(),
                    Title = x.SongName,
                    PhoneNumber = x.SongAuthor,
                    FirstName = x.SongName,
                    ThumbUrl = x.Thumbnail.ToString(),
                    InputMessageContent = new InputTextMessageContent
                    {
                        MessageText = $"/{SelectCommandName} {cacheKey} {index}"
                    }
                });

            await _botClient.AnswerInlineQueryAsync(inlineQueryId, inlineQueryResults.ToArray());
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

                await SendChords(link, chords, chatId);
            }
        }

        private async Task ProcessCommand(string command, long chatId)
        {
            switch (command)
            {
                case var commandTest when commandTest.StartsWith(SelectCommandName):
                {
                    await ProcessSelectCommand(command, chatId);
                    break;
                }

                case var commandTest when commandTest.StartsWith(SetFormatCommandName):
                {
                    ProcessSetFormatCommand(command, chatId);
                    break;
                }

                // more commands here
            }
        }

        private async Task ProcessSelectCommand(string command, long chatId)
        {
            var parsedCommand = ParseSelectCommand(command);

            var link = parsedCommand.Bind(c => 
                Cache.GetAsResult(c.cacheKey).Bind(x => 
                    x.Results.GetByIndexAsResult(c.index))
            );

            var chords = await link.Bind(x => _chordsService.Get(x));

            await SendChords(link, chords, chatId);
        }

        private static void ProcessSetFormatCommand(string command, long chatId)
        {
            var format = ParseSetFormatCommand(command);

            FormatMapping[chatId] = format;
        }

        private async Task SendChords(Result<ChordsLink> chordsLink, Result<string> chordsText, long chatId)
        {
            var format = FormatMapping.ContainsKey(chatId) ? FormatMapping[chatId] : Format.Message;

            switch (format)
            {
                case Format.Message:
                {
                    await SendChordsAsMessage(chordsLink, chordsText, chatId);
                    break;
                }

                case Format.Txt:
                {
                    await SendChordsAsFile(chordsLink, chordsText, chatId);
                    break;
                }
            }
        }

        private async Task SendChordsAsMessage(Result<ChordsLink> chordsLink, Result<string> chordsText, long chatId)
        {
            var result = chordsLink.Bind(l => 
                chordsText.Bind(c => _chordsFormatter.Format(l, c).Return())
            );

            await result.Match(
                text => _botClient.SendTextMessageAsync(chatId, text, ParseMode.Default, true),
                err => _botClient.SendTextMessageAsync(chatId, err)
            );
        }

        private async Task SendChordsAsFile(Result<ChordsLink> link, Result<string> chordsText, long chatId)
        {
            var result = link.Bind(l =>
                chordsText.Bind(c =>
                {
                    var formattedChords = _chordsFormatter.Format(l, c);
                    var fileName = $"{l.SongAuthor} - {l.SongName}";

                    return ToTextFile(fileName, formattedChords).Return();
                })
            );

            await SendFile(result, chatId);
        }

        private async Task SendFile(Result<FileToSend?> file, long chatId)
        {
            await file.Match(
                f => _botClient.SendDocumentAsync(chatId, f ?? default(FileToSend)),
                err => _botClient.SendTextMessageAsync(chatId, err)
            );
        }

        private static Result<(int index, string cacheKey)> ParseSelectCommand(string command)
        {
            const string commandError = "can't understand command";
            var pattern = $@"{SelectCommandName} ([a-zA-Zа-яА-Я0-9_-]+) (\d+)";
            var match = Regex.Match(command, pattern);

            if (!match.Success)
            {
                return Result<(int index, string cacheKey)>.Error(commandError);
            }

            var cacheKey = match.Groups[1].Value;
            var index = int.Parse(match.Groups[2].Value);

            return (index, cacheKey).Return();
        }

        private static string ParseSetFormatCommand(string command)
        {
            return command.Split(' ').LastOrDefault() ?? Format.Message;
        }

        private static FileToSend? ToTextFile(string name, string content)
        {
            var byteArray = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(byteArray);

            return new FileToSend($"{name}.txt", stream);
        }
    }
}
