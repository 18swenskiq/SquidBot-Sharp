using Renci.SshNet;
using System;
using System.IO;
using System.Net;

namespace SquidBot_Sharp.Modules
{
    public class DatabaseModule
    {
        private static SftpClient _ftp { get; set; }

        public DatabaseModule(string host, string username, string password)
        {
            _ftp = new SftpClient(host, 22, username, password);
        }


        public static void UploadFile(string sourceFile)
        {
            _ftp.Connect();
            using(FileStream fs = new FileStream(sourceFile, FileMode.Open))
            {
                _ftp.BufferSize = 4 * 1024;
                _ftp.UploadFile(fs, "datafiles/" + Path.GetFileName(sourceFile));
            }
            _ftp.Disconnect();
        }

        public static void RetrieveFile(string pathOfFileToGet)
        {
            _ftp.Connect();
            using (Stream fileStream = File.Create(@$"{Directory.GetCurrentDirectory()}\{pathOfFileToGet}"))
            {
                _ftp.DownloadFile(Path.Combine(@"datafiles/", Path.GetFileName(pathOfFileToGet)), fileStream);
            }
            _ftp.Disconnect();
        }


    }
}
