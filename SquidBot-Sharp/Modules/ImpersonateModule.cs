using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Modules
{
    public class ImpersonateModule
    {
        private const string BASEPATH = @"C:\Users\Quinton\source\repos\SquidBot-Sharp\SquidBot-Sharp\bin\Debug\netcoreapp3.0\datafiles\Markov\users\";
        public async Task WriteEntry(MessageCreateEventArgs e)
        {
            if (File.Exists(Path.Combine(BASEPATH, $"{e.Author.Id}.txt")))
            {
                await File.AppendAllTextAsync(Path.Combine(BASEPATH, $"{e.Author.Id}.txt"), e.Message.Content + "\n", Encoding.UTF8);
            }
            else
            {
                var fs = File.Create(Path.Combine(BASEPATH, $"{e.Author.Id}.txt"));
                fs.Close();

                await File.AppendAllTextAsync(Path.Combine(BASEPATH, $"{e.Author.Id}.txt"), e.Message.Content + "\n", Encoding.UTF8);
            }
        }

        public async Task<string[]> LoadFile(string userID)
        {
            var returnarray = new string[] { };
            var searchpath = Path.Combine(BASEPATH, $"{userID}.txt");
            if(File.Exists(searchpath))
            {
                returnarray = await File.ReadAllLinesAsync(searchpath);
                // Get rid of all empty entries
                returnarray = returnarray.Where(x => !string.IsNullOrEmpty(x.Trim())).ToArray();

                // TODO: Remove all entries that start with `>`
                returnarray = returnarray.Where(x => !x.StartsWith(">")).ToArray();
            }
            return returnarray;
        }
    }
}
