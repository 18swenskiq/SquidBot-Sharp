﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Commands
{
    public class ConvertCMD
    {
        [Command("convert"), Description("Convert a measurement unit to its corresponding metric or imperial unit")]
        public async Task ConvertUnits(CommandContext ctx, string argument1, string argument2 = null)
        {
            double convertAmount = 0;
            string convertFrom = null;
            string convertResults = null;
            string convertTo = null;

            if (argument2 != null)
            {
                convertFrom = argument2;
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
                }
                else
                {
                    await ctx.RespondAsync("No units detected to convert");
                    return;
                }
            }

            ConvertResultInfo thisconversion = null;

            switch(convertFrom.ToLower())
            {
                case "in":
                    thisconversion = ConvertToCentimeters(convertAmount);
                    break;
                case "cm":
                    thisconversion = ConvertToInches(convertAmount);
                    break;

                case "mi":
                    thisconversion = ConvertToKilometers(convertAmount);
                    break;
                case "km":
                    thisconversion = ConvertToMiles(convertAmount);
                    break;

                case "ft":
                    thisconversion = ConvertToMeters(convertAmount);
                    break;
                case "m":
                    thisconversion = ConvertToFeet(convertAmount);
                    break;

                case "f":
                    thisconversion = ConvertToCelcius(convertAmount);
                    break;
                case "c":
                    thisconversion = ConvertToFahrenheit(convertAmount);
                    break;

                case "lbs":
                    thisconversion = ConvertToKilograms(convertAmount);
                    break;
                case "kg":
                    thisconversion = ConvertToPounds(convertAmount);
                    break;

                default:
                    await ctx.RespondAsync("Type to convert not recognized");
                    return;
            }
            convertResults = (thisconversion.ConvertTotal).ToString("0.##");
            convertTo = thisconversion.ConvertType;


            await ctx.RespondAsync($"`{convertAmount}` `{convertFrom}` == `{convertResults}` `{convertTo}`");
            return;
        }

        public ConvertResultInfo ConvertToCentimeters(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * 2.54, ConvertType = "cm" };
        }
        public ConvertResultInfo ConvertToInches(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * .393701, ConvertType = "in" };
        }
        public ConvertResultInfo ConvertToKilometers(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * 1.60934, ConvertType = "km" };
        }
        public ConvertResultInfo ConvertToMiles(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * .621371, ConvertType = "mi" };
        }
        public ConvertResultInfo ConvertToMeters(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * .3048, ConvertType = "m" };
        }
        public ConvertResultInfo ConvertToFeet(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * 3.28084, ConvertType = "ft" };
        }
        public ConvertResultInfo ConvertToCelcius(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = ((arg1 - (double)32) * .5555555), ConvertType = "c" };
        }
        public ConvertResultInfo ConvertToFahrenheit(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = ((arg1 * 1.8) + (double)32), ConvertType = "f" };
        }
        public ConvertResultInfo ConvertToPounds(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * 2.20462, ConvertType = "lbs" };
        }
        public ConvertResultInfo ConvertToKilograms(double arg1)
        {
            return new ConvertResultInfo { ConvertTotal = arg1 * .453592, ConvertType = "kg" };
        }
    }

    public class ConvertResultInfo
    {
        public string ConvertType { get; set; }
        public double ConvertTotal { get; set; }
    }
}
