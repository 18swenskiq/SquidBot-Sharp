using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SquidBot_Sharp.Modules
{
    public class MarkovModule
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
    }
}
