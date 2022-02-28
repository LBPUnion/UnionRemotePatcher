using System;
using LBPUnion.UnionPatcher;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using System.Text;
using UnionRemotePatcher.Helpers;
using System.Collections.Generic;
using Eto.Forms;
using System.Linq;
using PS3MAPI_NCAPI;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnionRemotePatcher
{
    public class RemotePatch
    {
        public PS3MAPI ps3mapi = new PS3MAPI();

        private static Dictionary<string, string> GetUsers(string ps3_ip, string user, string pass)
        {
            Console.WriteLine("Getting users...");

            Dictionary<string, string> users = new Dictionary<string, string>();

            string[] userFolders = FTPHelper.FTP_ListDirectory($"ftp://{ps3_ip}/dev_hdd0/home/", user, pass);

            string username = "";

            for (int i = 0; i < userFolders.Length; i++)
            {
                username = FTPHelper.FTP_ReadFile($"ftp://{ps3_ip}/dev_hdd0/home/{userFolders[i]}/localusername", user, pass);
                users.Add(username, userFolders[i]);

                Console.WriteLine("User found: " + username + $" <{userFolders[i]}>");
            }

            return users;
        }

        public static void LaunchSCETool(string args)
        {
            string platformExecutable = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformExecutable = "scetool/win64/scetool.exe";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platformExecutable = "scetool/linux64/scetool";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platformExecutable = "";
            }

            if (platformExecutable != "")
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = false;
                startInfo.FileName = Path.GetFullPath(platformExecutable);
                startInfo.WorkingDirectory = @"./";
                startInfo.Arguments = args;
                startInfo.RedirectStandardOutput = true;

                Console.WriteLine("\n\n===== START SCETOOL =====\n");
                using (Process proc = Process.Start(startInfo))
                {
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        Console.WriteLine(proc.StandardOutput.ReadLine());
                    }
                    proc.WaitForExit();
                }
                Console.WriteLine("\n===== END SCETOOL =====\n\n");
            }
            else
            {
                throw new Exception("Error starting SCETool. Your platform may not be supported yet.");
            }
        }

        public void EBOOTRemotePatch(string ps3ip, string gameID, string serverURL, string user, string pass)
        {
            string idps = "";
            Dictionary<string, string> users;

            ps3mapi.ConnectTarget(ps3ip);
            ps3mapi.PS3.RingBuzzer(PS3MAPI.PS3_CMD.BuzzerMode.Double);
            ps3mapi.PS3.Notify("UnionRemotePatcher Connected! Patching...");

            // Create simple directory structure
            Directory.CreateDirectory(@"EBOOT");
            Directory.CreateDirectory($@"EBOOT/{gameID}");
            Directory.CreateDirectory($@"EBOOT/{gameID}/Original");
            Directory.CreateDirectory($@"EBOOT/{gameID}/Patched");

            // Let's grab and backup our EBOOT
            FTPHelper.FTP_Download($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN", @$"EBOOT/{gameID}/Original/EBOOT.BIN", user, pass);

            // Now we'll check and see if a backup exists on the server or not, if we don't have one on the server, then upload one
            if (!FTPHelper.FTP_CheckFileExists($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass))
            {
                FTPHelper.FTP_Upload(@$"EBOOT/{gameID}/Original/EBOOT.BIN", $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass);
            }

            idps = PS3MAPI.PS3MAPI_Client_Server.PS3_GetIDPS();

            File.WriteAllBytes(@"data/idps", IDPSHelper.StringToByteArray(idps));

            users = GetUsers(ps3ip, user, pass);

            //GetUsers(ps3ip, "", "");

            //UserSelection select = new UserSelection(users.Keys.ToArray(), gameID);

            //select.Show();

            // Check if we have a RIF file, if so let's convert that to a RAP so we can decrypt our eboot
            //if (rifFile != "")
            //{
            //    LaunchProcess(@"Files/tools/r2r.exe", @"Files/tools", Path.GetFullPath($@"Files/{gameID}/{rifFile}"));
            //}

            // Finally, let's decrypt the EBOOT.BIN
            //LaunchProcess(@"Files/tools/scetool.exe", @"Files/tools", $" -d \"{Path.GetFullPath(@$"Files/{gameID}/EBOOT.BIN.BAK")}\" \"{Path.GetFullPath(@$"Files/{gameID}/EBOOT_CLEAN.ELF")}\"");

            // Let's make a patched folder for our patched files
            //Directory.CreateDirectory(@$"Files/{gameID}/Patched/");

            // Our files are all in place - now we can actually patch our eboot
            //Patcher.PatchFile(@$"Files/{gameID}/EBOOT_CLEAN.ELF", serverURL, $@"Files/{gameID}/Patched/EBOOT_PATCHED.ELF");

            // FINALLY we're patched - let's encrypt our patched eboot and send it back over to the PS3
            // btw this is where shit is gonna get fucked up so hard i NEED to ask for firmware type (CEX/DEX/etc) at UI
            // TODO: not make patched EBOOTs that are 14mb lmao I need to learn how to use the resigner. This setup worked great for me on CEX EVILNAT 4.88.2 CFW tho
            //LaunchProcess(@"Files/tools/scetool.exe", @"Files/tools", $" --sce-type=SELF --skip-sections=FALSE --key-revision=0A --self-app-version=0001000000000000 --self-auth-id=1010000001000003 --self-vendor-id=01000002 --self-ctrl-flags=0000000000000000000000000000000000000000000000000000000000000000 --self-cap-flags=00000000000000000000000000000000000000000000003B0000000100040000 --self-type=APP --self-fw-version=0003005500000000 --compress-data true --encrypt \"{Path.GetFullPath(@$"Files/{gameID}/Patched/EBOOT_PATCHED.ELF")}\" \"{Path.GetFullPath(@$"Files/{gameID}/Patched/EBOOT.BIN")}\"");

            // and let's upload it to the PS3!!!
            // wait... that... worked??!?!?
            //FTPHelper.FTP_Upload(@$"Files/{gameID}/Patched/EBOOT.BIN", $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN", user, pass);
        }
    }
}