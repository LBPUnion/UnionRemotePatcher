using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UnionRemotePatcher
{
    public class FTP
    {
        public static bool FTPFileExists(string url, string user, string pass)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest feRequest = (FtpWebRequest)WebRequest.Create(url);
            feRequest.Credentials = new NetworkCredential(user, pass);
            feRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;

            // Let's see if a backup already exists
            try
            {
                FtpWebResponse chResponse = (FtpWebResponse)feRequest.GetResponse();
            }
            catch (WebException ex)
            {
                FtpWebResponse feResponse = (FtpWebResponse)ex.Response;
                if (feResponse.StatusCode ==
                    FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return false;
                }
            }

            return true;
        }

        public static string[] FTPListDirectory(string url, string user, string pass)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest ldRequest = (FtpWebRequest)WebRequest.Create(url);
            ldRequest.Credentials = new NetworkCredential(user, pass);
            ldRequest.Method = WebRequestMethods.Ftp.ListDirectory;

            FtpWebResponse ldResponse = (FtpWebResponse)ldRequest.GetResponse();

            string files = new StreamReader(ldResponse.GetResponseStream()).ReadToEnd();

            return files.Split("\r\n");
        }

        public static void FTPUpload(string source, string destination, string user, string pass)
        {
            FtpWebRequest ulRequest = (FtpWebRequest)WebRequest.Create(destination);
            ulRequest.Credentials = new NetworkCredential(user, pass);
            ulRequest.Method = WebRequestMethods.Ftp.UploadFile;

            byte[] fileContents = File.ReadAllBytes(source);

            ulRequest.ContentLength = fileContents.Length;

            using (Stream requestStream = ulRequest.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }
        }

        public static void FTPDownload(string source, string destination, string user, string pass)
        {
            FtpWebRequest dlRequest = (FtpWebRequest)WebRequest.Create(source);
            dlRequest.Credentials = new NetworkCredential(user, pass);
            dlRequest.Method = WebRequestMethods.Ftp.DownloadFile;

            FtpWebResponse dlResponse = (FtpWebResponse)dlRequest.GetResponse();

            Stream responseStream = dlResponse.GetResponseStream();

            using (Stream s = File.Create(destination))
            {
                responseStream.CopyTo(s);
            }
        }
    }
}
