using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
using SquidBot_Sharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    public class CurrencyCMD
    {
        [Command("currency"), Description("Convert a currency value to another currency")]
        public async Task Currency(CommandContext ctx, string argument1, string argument2 = null, string argument3 = null)
        {
            double convertAmount = 0;
            string convertFrom = null;
            string convertTo = null;

            if (argument3 != null)
            {
                convertTo = argument3.ToUpper();
                convertFrom = argument2.ToUpper();
                try
                {
                    convertAmount = Convert.ToDouble(argument1);
                }
                catch
                {
                    await ctx.RespondAsync("Number given to convert is not a valid number");
                    return;
                }
            }
            else
            {
                if (argument2 != null)
                {

                    if(argument1.Any(char.IsLetter))
                    {
                        string tempstringfornumber = "";
                        foreach (var character in argument1)
                        {
                            if (Char.IsDigit(character) || character == '-' || character == '.')
                            {
                                tempstringfornumber += character;
                            }
                            else
                            {
                                convertFrom += character;
                            }
                        }
                        try
                        {
                            convertAmount = Convert.ToDouble(tempstringfornumber);
                        }
                        catch
                        {
                            await ctx.RespondAsync("No numbers detected to convert");
                            return;
                        }
                        convertTo = argument2.ToUpper();
                    }
                    else
                    {
                        convertFrom = argument2.ToUpper();
                        try
                        {
                            convertAmount = Convert.ToDouble(argument1);
                        }
                        catch
                        {
                            await ctx.RespondAsync("Number given to convert is not a valid number");
                            return;
                        }
                    }

                }
                else
                {
                    string tempstringfornumber = "";
                    foreach (var character in argument1)
                    {
                        if (Char.IsDigit(character) || character == '-' || character == '.')
                        {
                            tempstringfornumber += character;
                        }
                        else
                        {
                            convertFrom += character;
                        }
                    }
                    try
                    {
                        convertAmount = Convert.ToDouble(tempstringfornumber);
                    }
                    catch
                    {
                        await ctx.RespondAsync("No numbers detected to convert");
                        return;
                    }
                }
            }

            convertFrom = convertFrom.ToUpper();

            var validTypes = new List<string> { "CAD", "HKD", "ISK", "PHP", "DKK", "HUF", "CZK", "AUD", "RON", "SEK", "IDR", "INR", "BRL", "RUB", "HRK", "JPY", "THB", "CHF", "SGD", "PLN", "BGN", "TRY", "CNY", "NOK", "NZD", "ZAR", "USD", "MXN", "ILS", "GBP", "KRW", "MYR" };
            if (!validTypes.Contains(convertFrom))
            {
                await ctx.RespondAsync("Currency code to convert from was not recognized");
                return;
            }
            if(convertTo != null && !validTypes.Contains(convertTo))
            {
                await ctx.RespondAsync("Currency code to convert to was not recognized");
                return;
            }


            ExchangeRates rates = null;
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"https://api.exchangeratesapi.io/latest?base={convertFrom}");
                string resultstring = await result.Content.ReadAsStringAsync();
                rates = JsonConvert.DeserializeObject<ExchangeRates>(resultstring);
            }

            if (convertTo != null)
            {
                convertTo = convertTo.ToUpper();
                var conversionRate = rates.Rates.GetType().GetProperty(convertTo).GetValue(rates.Rates, null);
                string responseString = $"`{convertAmount}` `{convertFrom}` == `{(Convert.ToDouble(conversionRate) * convertAmount).ToString("0.##")}` `{convertTo}`";
                await ctx.RespondAsync(responseString);
                return;
            }
            else
            {
                string responsestring = $"`{convertAmount}` `{convertFrom}` is:\n" +
                    $"```py\n" +
                    $"{(rates.Rates.AUD * convertAmount).ToString("0.##")} Australian Dollars\n" +
                    $"{(rates.Rates.BRL * convertAmount).ToString("0.##")} Brazilian Real\n" +
                    $"{(rates.Rates.BGN * convertAmount).ToString("0.##")} Bulgarian Lev\n" +
                    $"{(rates.Rates.CAD * convertAmount).ToString("0.##")} Canadian Dollars\n";

                if(convertFrom == "EUR")
                {
                    responsestring += $"{convertAmount} Euros\n";
                }
                else
                {
                    responsestring += $"{(rates.Rates.EUR * convertAmount).ToString("0.##")} Euros\n";
                }

                responsestring += $"{(rates.Rates.GBP * convertAmount).ToString("0.##")} Great Britain Pound\n" +
                    $"{(rates.Rates.NZD * convertAmount).ToString("0.##")} New Zealand Dollar\n" +
                    $"{(rates.Rates.PLN * convertAmount).ToString("0.##")} Polish złoty\n" +
                    $"{(rates.Rates.USD * convertAmount).ToString("0.##")} United States Dollar\n" +
                    $"{(rates.Rates.ZAR * convertAmount).ToString("0.##")} South African Rand\n" +
                    "```";
                await ctx.RespondAsync(responsestring);
                return;
            }

        }
    }
}
