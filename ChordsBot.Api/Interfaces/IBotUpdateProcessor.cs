using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ChordsBot.Api.Interfaces
{
    public interface IBotUpdateProcessor
    {
        Task Process(Update update);
    }
}
