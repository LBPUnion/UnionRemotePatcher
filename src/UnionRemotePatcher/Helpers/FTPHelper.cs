using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UnionRemotePatcher.Helpers
{
    public class FTPHelper
    {
        public static bool FTP_CheckFileExists(string url, string user, string pass)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Credentials = new NetworkCredential(user, pass);
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

            try
            {
                Console.WriteLine($"FTP: Checking if file {url} exists");

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode ==
                    FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return false;
                }
            }

            return true;
        }

        public static string[] FTP_ListDirectory(string url, string user, string pass)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Credentials = new NetworkCredential(user, pass);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            try
            {
                Console.WriteLine($"FTP: Listing directory {url}");

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                string names = new StreamReader(response.GetResponseStream()).ReadToEnd();

                List<string> dirs = new List<string>();

                foreach(string dir in names.Split("\r\n"))
                {
                    if (dir != ".")
                    {
                        if (dir != "..")
                        {
                            if (!string.IsNullOrEmpty(dir))
                            {
                                dirs.Add(dir);
                            }
                        }
                    }
                }

                foreach(string dir in dirs.ToArray())
                {
                    Console.WriteLine($"/{dir}");
                }

                Console.WriteLine("");

                return dirs.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public static void FTP_Upload(string source, string destination, string user, string pass)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(destination);
            request.Credentials = new NetworkCredential(user, pass);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            byte[] fileContents = File.ReadAllBytes(source);

            request.ContentLength = fileContents.Length;

            try
            {
                Console.WriteLine($"FTP: Uploading file {source} to {destination}");

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }
            }
            catch
            {
                throw new WebException("Could not upload file");
            }

        }

        public static void FTP_Download(string source, string destination, string user, string pass)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(source);
            request.Credentials = new NetworkCredential(user, pass);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            try
            {
                Console.WriteLine($"FTP: Downloading file {source} to {destination}");

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();

                using (Stream s = File.Create(destination))
                {
                    responseStream.CopyTo(s);
                }
            }
            catch
            {
                throw new WebException("Could not download file");
            }
        }

        public static string FTP_ReadFile(string url, string user, string pass)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Credentials = new NetworkCredential(user, pass);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            try
            {
                Console.Write($"\nFTP: Reading file {url} ");

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();

                using (StreamReader reader = new StreamReader(responseStream))
                {
                    Console.WriteLine($" - File reads: {reader.ReadToEnd()}");
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return "<Error Downloading File>";
            }
        }
    }
}
