using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChordsBot.Api
{
    public class TelegramTokenOptions : AuthenticationSchemeOptions
    {
        public string Token { get; set; }
    }

    public class TelegramTokenHandler : AuthenticationHandler<TelegramTokenOptions>
    {
        private const string TelegramTokenName = "telegramToken";

        public TelegramTokenHandler(
            IOptionsMonitor<TelegramTokenOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var correctToken = Options.Token;
            var routeData = Context.GetRouteData();

            if (routeData.Values.ContainsKey(TelegramTokenName))
            {
                var token = (string)routeData.Values[TelegramTokenName];

                if (correctToken.Equals(token, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Task.FromResult(Success());
                }
            }

            if (Request.Query.ContainsKey(TelegramTokenName))
            {
                var token = (string) Request.Query[TelegramTokenName];

                if (correctToken.Equals(token, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Task.FromResult(Success());
                }
            }

            return Task.FromResult(AuthenticateResult.Fail("wrong token"));
        }

        private AuthenticateResult Success()
        {
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new GenericIdentity("user")), Scheme.Name));
        }
    }
}
