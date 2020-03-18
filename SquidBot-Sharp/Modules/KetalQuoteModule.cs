using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SquidBot_Sharp.Modules
{
    public static class KetalQuoteModule
    {
        public static List<KetalQuote> Quotes { get; set; }
        public static int SerializeQuotes()
        {
            try
            {
                using (Stream stream = File.Open("datafiles/data.ketalquotes", FileMode.Create))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, Quotes);
                }
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        public static int DeserializeQuotes()
        {
            try
            {
                using (Stream stream = File.Open("datafiles/data.ketalquotes", FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    var quotes = (List<KetalQuote>)bin.Deserialize(stream);
                    Quotes = quotes;
                    return 0;
                }
            }
            catch
            {
                Quotes = null;
                return 1;
            }
        }
    }


    [Serializable] public class KetalQuote
    {
        public int QuoteNumber { get; set; }
        public string Quote { get; set; }
        public string Footer { get; set; }
    }
}
