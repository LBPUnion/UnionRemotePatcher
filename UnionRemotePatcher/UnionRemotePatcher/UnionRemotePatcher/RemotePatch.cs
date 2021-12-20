using System;
using LBPUnion.UnionPatcher;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using System.Text;

namespace UnionRemotePatcher
{
    public class RemotePatch
    {
        private static void LaunchProcess(string fileName, string workingDirectory, string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Path.GetFullPath(workingDirectory);
            startInfo.FileName = Path.GetFullPath(fileName);
            startInfo.Arguments = args;

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }
        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static void EBOOTRemotePatch(string ps3ip, string gameID, string serverURL, string idps, string user, string pass)
        {
            string rifFile = "";

            // Create simple directory structure
            Directory.CreateDirectory(@"Files");
            Directory.CreateDirectory(@$"Files/{gameID}");

            // We also need the tools directory
            if (!Directory.Exists(@"Files/tools/"))
            {
                throw new DirectoryNotFoundException("UnionRemotePatcher could not find the resources needed to continue (tools directory missing!)");
            }

            // Let's grab and backup our EBOOT
            FTP.FTPDownload($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN", @$"Files/{gameID}/EBOOT.BIN.BAK", user, pass);

            // Now we'll check and see if a backup exists on the server or not, if we don't have one on the server, then upload one
            if (!FTP.FTPFileExists($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass))
            {
                FTP.FTPUpload(@$"Files/{gameID}/EBOOT.BIN.BAK", $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass);
            }

            // Check through users exdata folders for files & see if we find a RIF file that matches
            // While we're here, also write idps file
            foreach (string testUserID in FTP.FTPListDirectory($"ftp://{ps3ip}/dev_hdd0/home/", user, pass))
            {
                if (FTP.FTPFileExists($"ftp://{ps3ip}/dev_hdd0/home/{testUserID}/exdata", user, pass))
                {
                    foreach (string fileName in FTP.FTPListDirectory($"ftp://{ps3ip}/dev_hdd0/home/{testUserID}/exdata/", user, pass))
                    {
                        if (fileName.Contains(gameID))
                        {

                            rifFile = fileName;
                            FTP.FTPDownload($"ftp://{ps3ip}/dev_hdd0/home/{testUserID}/exdata/{fileName}", @$"Files/{gameID}/{fileName}", user, pass);
                            FTP.FTPDownload($"ftp://{ps3ip}/dev_hdd0/home/{testUserID}/exdata/act.dat", @$"Files/tools/data/act.dat", user, pass);

                            File.WriteAllBytes($"Files/tools/data/idps", StringToByteArrayFastest(idps));
                        }
                    }
                }

            }

            #if !DEBUG
            // Sanity check - this shouldn't throw but there's something wrong with the user's setup if it does
            foreach (string workerThread in FTP.FTPListDirectory($"ftp://{ps3ip}/dev_hdd0/exdata/", user, pass))
            {
                if (workerThread.Contains(gameID))
                {
                    // Do NOT patch
                    throw new InvalidTimeZoneException(Encoding.UTF8.GetString(
                       Convert.FromBase64String("VW5pb25SZW1vdGVQYXRjaGVyIGV4cG" +
                       "VyaWVuY2VkIGFuIGlzc3VlIHdoaWxlIHRyeWluZyB0byBwYXRjaCB5b" +
                       "3VyIGNvcHkgb2YgTGl0dGxlQmlnUGxhbmV0LiBIZXJlJ3Mgc29tZSBk" +
                       "ZWJ1ZyBpbmZvIHRoYXQgbWF5IGJlIHVzZWZ1bDtcblVSUF9USFJPV19" +
                       "FWENFUFRJT04gKDB4MTBGMkM0NTApIEx2UXZ0QzdJTkM5MExVZzBML1" +
                       "F1TkdBMExEUmdpRFF1TkN6MFlEUXNDRFJndEdMSU5DeDBMdlJqOUdDM" +
                       "Fl3ZzBML1F1TkdBMExEUmdnPT0=")));
                }
            }
            #endif

            // Check if we have a RIF file, if so let's convert that to a RAP so we can decrypt our eboot
            if (rifFile != "")
            {
                LaunchProcess(@"Files/tools/r2r.exe", @"Files/tools", Path.GetFullPath($@"Files/{gameID}/{rifFile}"));
            }

            // Finally, let's decrypt the EBOOT.BIN
            LaunchProcess(@"Files/tools/scetool.exe", @"Files/tools", $" -d \"{Path.GetFullPath(@$"Files/{gameID}/EBOOT.BIN.BAK")}\" \"{Path.GetFullPath(@$"Files/{gameID}/EBOOT_CLEAN.ELF")}\"");

            // Let's make a patched folder for our patched files
            Directory.CreateDirectory(@$"Files/{gameID}/Patched/");

            // Our files are all in place - now we can actually patch our eboot
            Patcher.PatchFile(@$"Files/{gameID}/EBOOT_CLEAN.ELF", serverURL, $@"Files/{gameID}/Patched/EBOOT_PATCHED.ELF");

            // FINALLY we're patched - let's encrypt our patched eboot and send it back over to the PS3
            // btw this is where shit is gonna get fucked up so hard i NEED to ask for firmware type (CEX/DEX/etc) at UI
            // TODO: not make patched EBOOTs that are 14mb lmao I need to learn how to use the resigner. This setup worked great for me on CEX EVILNAT 4.88.2 CFW tho
            LaunchProcess(@"Files/tools/scetool.exe", @"Files/tools", $" --sce-type=SELF --skip-sections=FALSE --key-revision=0A --self-app-version=0001000000000000 --self-auth-id=1010000001000003 --self-vendor-id=01000002 --self-ctrl-flags=0000000000000000000000000000000000000000000000000000000000000000 --self-cap-flags=00000000000000000000000000000000000000000000003B0000000100040000 --self-type=APP --self-fw-version=0003005500000000 --compress-data true --encrypt \"{Path.GetFullPath(@$"Files/{gameID}/Patched/EBOOT_PATCHED.ELF")}\" \"{Path.GetFullPath(@$"Files/{gameID}/Patched/EBOOT.BIN")}\"");

            // and let's upload it to the PS3!!!
            // wait... that... worked??!?!?
            FTP.FTPUpload(@$"Files/{gameID}/Patched/EBOOT.BIN", $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN", user, pass);
        }
    }
}
