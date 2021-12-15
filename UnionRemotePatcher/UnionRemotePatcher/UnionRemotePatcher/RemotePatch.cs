using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBPUnion.UnionPatcher;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;

namespace UnionRemotePatcher
{
    public class RemotePatch
    {
        private static void DownloadFile(string url, string path)
        {
            WebClient cln = new WebClient();
            cln.DownloadFile(url, path);
        }
        private static void LaunchSCETool(string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Path.GetFullPath(@"Files/TrueAncestor_tmp/tool");
            startInfo.FileName = Path.GetFullPath(@"Files/TrueAncestor_tmp/tool/scetool.exe");
            startInfo.Arguments = args;

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        private static bool FTPFileExists(string url, string user, string pass)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest chRequest = (FtpWebRequest)WebRequest.Create(url);
            chRequest.Credentials = new NetworkCredential(user, pass);
            chRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;

            // Let's see if a backup already exists
            try
            {
                FtpWebResponse chResponse = (FtpWebResponse)chRequest.GetResponse();
            }
            catch (WebException ex)
            {
                FtpWebResponse chResponse = (FtpWebResponse)ex.Response;
                if (chResponse.StatusCode ==
                    FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    return false;
                }
            }

            return true;
        }

        private static void FTPUpload(string source, string destination, string user, string pass)
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

        private static void FTPDownload(string source, string destination, string user, string pass)
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

        public static void EBOOTRemotePatch(string ps3ip, string gameID, string serverURL)
        {
            // Create simple directory structure
            Directory.CreateDirectory(@"Files");
            Directory.CreateDirectory(@$"Files/{gameID}");

            // If we don't have a copy of TrueAncestor downloaded then grab it
            if (!File.Exists(@"Files/TrueAncestor.tmp"))
            {
                DownloadFile("https://www.psx-place.com/resources/trueancestor-self-resigner-by-jjkkyu.33/download?version=44", @"Files/TrueAncestor.tmp");
            }
            
            // Extract TrueAncestor if it's not already extracted
            if (!Directory.Exists(@"Files/TrueAncestor_tmp/") && !File.Exists(@"Files/TrueAncestor_tmp/tool/scetool.exe"))
            {
                ZipFile.ExtractToDirectory(@"Files/TrueAncestor.tmp", @"Files/TrueAncestor_tmp/");
            }

            // Copy data (keys) to tool directory make things easier on us later
            if (!Directory.Exists(@"Files/TrueAncestor_tmp/tool/data/"))
            {
                Directory.CreateDirectory(@"Files/TrueAncestor_tmp/tool/data/");
                if (!File.Exists("Files/TrueAncestor_tmp/tool/data/keys") || !File.Exists("Files/TrueAncestor_tmp/tool/data/ldr_curves") || !File.Exists("Files/TrueAncestor_tmp/tool/data/vsh_curves"))
                {
                    File.Copy(@"Files/TrueAncestor_tmp/data/keys", @"Files/TrueAncestor_tmp/tool/data/keys");
                    File.Copy(@"Files/TrueAncestor_tmp/data/ldr_curves", @"Files/TrueAncestor_tmp/tool/data/ldr_curves");
                    File.Copy(@"Files/TrueAncestor_tmp/data/vsh_curves", @"Files/TrueAncestor_tmp/tool/data/vsh_curves");
                }
            }

            // Now that we know TrueAncestor has been downloaded, let's grab and backup our EBOOT
            FTPDownload($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN", @$"Files/{gameID}/EBOOT.BIN.BAK", "anonymous", "");

            // Now we'll check and see if a backup exists on the server or not, if we don't have one on the server, then upload one
            if (!FTPFileExists($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", "anonymous", ""))
            {
                FTPUpload(@$"Files/{gameID}/EBOOT.BIN.BAK", $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", "anonymous", "");
            }

            // Finally, let's decrypt the EBOOT.BIN
            LaunchSCETool($" -d \"{Path.GetFullPath(@$"Files/{gameID}/EBOOT.BIN.BAK")}\" \"{Path.GetFullPath(@$"Files/{gameID}/EBOOT_CLEAN.ELF")}\"");

            // Let's make a patched folder for our patched files
            Directory.CreateDirectory(@$"Files/{gameID}/Patched/");

            // Our files are all in place - now we can actually patch our eboot
            Patcher.PatchFile(@$"Files/{gameID}/EBOOT_CLEAN.ELF", serverURL, $@"Files/{gameID}/Patched/EBOOT_PATCHED.ELF");

            // FINALLY we're patched - let's encrypt our patched eboot and send it back over to the PS3
            // btw this is where shit is gonna get fucked up so hard i NEED to ask for firmware type (CEX/DEX/etc) at UI
            // TODO: not make patched EBOOTs that are 14mb lmao I need to learn how to use the resigner. This setup worked great for me on CEX EVILNAT 4.88.2 CFW tho
            LaunchSCETool($" --sce-type=SELF --skip-sections=FALSE --key-revision=0A --self-app-version=0001000000000000 --self-auth-id=1010000001000003 --self-vendor-id=01000002 --self-ctrl-flags=0000000000000000000000000000000000000000000000000000000000000000 --self-cap-flags=00000000000000000000000000000000000000000000003B0000000100040000 --self-type=APP --self-fw-version=0003005500000000 --compress-data true --encrypt \"{Path.GetFullPath(@$"Files/{gameID}/Patched/EBOOT_PATCHED.ELF")}\" \"{Path.GetFullPath(@$"Files/{gameID}/Patched/EBOOT.BIN")}\"");

            // and let's upload it to the PS3!!!
            // wait... that... worked??!?!?
            FTPUpload(@$"Files/{gameID}/Patched/EBOOT.BIN", $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN", "anonymous", "");
        }
    }
}
