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
        private enum Format
        {
            Message,
            Txt
        }

        private static readonly IDictionary<long, Format> FormatMapping;

        private const string SelectCommandName = "select";
        private const string SetFormatCommandName = "setFormat";
        private const int MaxResultsForInlineSearch = 50;
        private const int MessageMaxLength = 4096;

        private readonly ITelegramBotClient _botClient;
        private readonly IChordsFormatter _chordsFormatter;
        private readonly IChordsService _chordsService;

        static BotUpdateProcessor()
        {
            FormatMapping = new ConcurrentDictionary<long, Format>();
        }

        public BotUpdateProcessor(
            ITelegramBotClient botClient,              
            IChordsFormatter chordsFormatter,
            Func<IChordsService> chordsServiceFactory)
        {
            _botClient = botClient;
            _chordsFormatter = chordsFormatter;
            _chordsService = chordsServiceFactory();
        }

        public async Task Process(Update update)
        {
            var answer = await GetAnswer(update);

            await answer();
        }

        public async Task<Func<Task>> GetAnswer(Update update)
        {
            switch (update.Type)
            {
                case UpdateType.InlineQueryUpdate:
                    return await GetInlineQueryAnswer(update.InlineQuery);

                case UpdateType.MessageUpdate:
                    return await GetMessageAnswer(update.Message);

                default:
                    return () => Task.FromResult(0); // ignore
            }
        }

        private async Task<Func<Task>> GetInlineQueryAnswer(InlineQuery inlineQuery)
        {
            var query = inlineQuery.Query;
            var inlineQueryId = inlineQuery.Id;

            var result = await _chordsService.FindChords(query);

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
                        MessageText = $"/{SelectCommandName} '{query}' {index}"
                    }
                });

            return () => _botClient.AnswerInlineQueryAsync(
                inlineQueryId, inlineQueryResults.ToArray<InlineQueryResult>());
        }

        private async Task<Func<Task>> GetMessageAnswer(Message message)
        {
            var text = message.Text;
            var chatId = message.Chat.Id;

            var answer = text.StartsWith('/')
                ? await GetCommandAnswer(text.TrimStart('/'), chatId)
                : await GetFirstChordsAnswer(text, chatId);

            return () => answer.Match(x => x(), x => _botClient.SendTextMessageAsync(chatId, x));
        }

        private async Task<IResult<Func<Task>>> GetFirstChordsAnswer(string query, long chatId)
        {
            var link = await _chordsService.FindFirst(query);
            var chords = await link.Bind(x => _chordsService.Get(x));

            return chords.Bind(x => SendChordsFn(x, chatId).Return());
        }

        private async Task<IResult<Func<Task>>> GetCommandAnswer(string command, long chatId)
        {
            switch (command)
            {
                case var commandTest when commandTest.StartsWith(SelectCommandName):
                    return await GetSelectCommandAnswer(command, chatId);

                case var commandTest when commandTest.StartsWith(SetFormatCommandName):
                    return GetSetFormatCommandAnswer(command, chatId);

                default:
                    return Result<Func<Task>>.Error("wrong command");
            }
        }

        private async Task<IResult<Func<Task>>> GetSelectCommandAnswer(string command, long chatId)
        {
            var args = ParseSelectCommandArgs(command.Replace(SelectCommandName, string.Empty));

            var link = await args.Bind(async x => {
                var result = await _chordsService.FindChords(x.query);
                return result.Results.GetByIndexAsResult(x.index);
            });

            var chords = await link.Bind(x => _chordsService.Get(x));

            return chords.Bind(x => SendChordsFn(x, chatId).Return());
        }

        private IResult<Func<Task>> GetSetFormatCommandAnswer(string command, long chatId)
        {
            var args = command.Replace(SetFormatCommandName, string.Empty);

            return ParseSetFormatCommandArgs(args).Bind(x =>
            {
                FormatMapping[chatId] = x;
                return SendMessageFn("format was updated", chatId).Return();
            });
        }

        private Func<Task> SendChordsFn(Chords chords, long chatId)
        {
            var format = FormatMapping.ContainsKey(chatId)
                ? FormatMapping[chatId]
                : Format.Message;

            switch (format)
            {
                case Format.Message:
                    return SendMessageFn(chords.RawChords, chatId);

                case Format.Txt:
                    return SendChordsAsFileFn(chords, chatId);

                default:
                    return SendMessageFn(chords.RawChords, chatId);
            }
        }

        private Func<Task> SendChordsAsFileFn(Chords chords, long chatId)
        {
            var formattedChords = _chordsFormatter.Format(chords);
            var fileName = $"{chords.SourceLink.SongAuthor} - {chords.SourceLink.SongName}";

            var txtFile = ToTextFile(fileName, formattedChords);

            return SendFileFn(txtFile, chatId);
        }

        private Func<Task> SendMessageFn(string message, long chatId) =>
            () =>
            {
                var tasks = message.ChunksUpTo(MessageMaxLength) 
                    .Select(x => _botClient.SendTextMessageAsync(chatId, x, ParseMode.Default, true));

                return Task.WhenAll(tasks);
            };

        private Func<Task> SendFileFn(FileToSend file, long chatId) =>
            () => _botClient.SendDocumentAsync(chatId, file);

        private static IResult<(int index, string query)> ParseSelectCommandArgs(string args)
        {
            const string pattern = @" '(.+)' (\d+)";
            var error = GetCommandArgsError(SelectCommandName);
            var match = Regex.Match(args, pattern);

            if (!match.Success)
            {
                return Result<(int index, string query)>.Error(error);
            }

            var query = match.Groups[1].Value;
            var index = int.Parse(match.Groups[2].Value);

            return (index, query).Return();
        }

        private static IResult<Format> ParseSetFormatCommandArgs(string args)
        {
            const string pattern = @" (\w+)";
            var error = GetCommandArgsError(SetFormatCommandName);
            var match = Regex.Match(args, pattern);

            if (!match.Success)
            {
                return Result<Format>.Error(error);
            }

            var format = match.Groups[1].Value;

            return Enum.TryParse(format, true, out Format parsedFormat) 
                ? parsedFormat.Return() 
                : Result<Format>.Error(error);
        }

        private static string GetCommandArgsError(string commandName) 
            => $"{commandName} command has wrong arguments";

        private static FileToSend ToTextFile(string name, string content)
        {
            var byteArray = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(byteArray);

            return new FileToSend($"{name}.txt", stream);
        }
    }
}
