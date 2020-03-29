using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SquidBot_Sharp.Commands
{
    class TranslateCMD : BaseCommandModule
    {
        [Command("translate"), Description("Translate to a given language (language is auto detected)")]
        public async Task Translate(CommandContext ctx, string TranslateTo, [RemainingText] string TranslateQuery)
        {
            await TranslateCore(ctx, "ad", TranslateTo, TranslateQuery);
            return;
        }

        [Command("translatem"), Description("Translate to a given language (language translating from must be specified")]
        public async Task TranslateManual(CommandContext ctx, string TranslateFrom, string TranslateTo, [RemainingText] string TranslateQuery)
        {
            await TranslateCore(ctx, TranslateFrom, TranslateTo, TranslateQuery);
            return;
        }

        private async Task TranslateCore(CommandContext ctx, string TranslateFrom, string TranslateTo, string TranslateQuery)
        {
            TranslateQuery = TranslateQuery.Replace("\n", " ");
            TranslateQuery = TranslateQuery.Replace("  ", " ");
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={TranslateFrom}&tl={TranslateTo}&dt=t&q={HttpUtility.UrlEncode(TranslateQuery)}";
            url = url.Replace("++", "+");
            var webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            var result = webClient.DownloadString(url);
            try
            {
                //string translatepayload = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                //translatepayload = translatepayload.Replace("\\n", " ");
                result = ParseResponse(result).TranslatedString;
                result = result.Replace("  ", " ");
                result = result.Replace("TOKEN_TRANSLATEAPI_QUOTATION_MARK", "\"");
                await ctx.RespondAsync($"Translation:\n```json\n{result}\n```");
                return;
            }
            catch (Exception e)
            {
                await ctx.RespondAsync($"Translation failed because of {e.Message}");
                return;
            }
        }


        private TranslateAPIResponse ParseResponse(string APIResponse)
        {
            // Fuck this format

            string translatedstring = "";
            string thisline = null;

            var parsedresponse = new TranslateAPIResponse();

            StringReader strReader = new StringReader(APIResponse);
            // Okay, so first 3 characters are going to be left brackets and the next should be a double quote
            if(APIResponse[0] != '[' || APIResponse[1] != '[' || APIResponse[2] != '[' || APIResponse[3] != '"')
            {
                // If we can't detect this then its fucked
                return null;
            }
            while (true)
            {
                thisline = strReader.ReadLine();
                if (thisline == null) break;
                if (thisline == "]") continue;

                thisline = thisline.Replace(@"\" + '"', "TOKEN_TRANSLATEAPI_QUOTATION_MARK");
                thisline = thisline.Replace('[', ' ');
                thisline = thisline.Replace(']', ' ');

                var thislinearr = thisline.Split('"');
                thislinearr = thislinearr.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                if (thislinearr[0] == ", ")
                {
                    if (thislinearr[1] == "null") continue;
                    thislinearr = thislinearr.Skip(1).ToArray();
                }
                if (thislinearr[0] == ",null,") break;
                translatedstring += thislinearr[0].TrimStart();
                translatedstring += " ";

                Console.WriteLine("test");

            }
            parsedresponse.TranslatedString = translatedstring;
            return parsedresponse;
        }
    }

    public class TranslateAPIResponse
    {
        public string TranslatedString { get; set; }
    }
}
