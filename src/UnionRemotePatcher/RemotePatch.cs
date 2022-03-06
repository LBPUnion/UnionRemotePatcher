﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using LBPUnion.UnionPatcher;
using PS3MAPI_NCAPI;
using UnionRemotePatcher.Helpers;
using Mono.Posix;
using Mono.Unix;

namespace UnionRemotePatcher;

public class RemotePatch
{
    private readonly PS3MAPI _ps3Mapi = new();

    private static Dictionary<string, string> GetUsers(string ps3Ip, string user, string pass)
    {
        Console.WriteLine("Getting users...");

        Dictionary<string, string> users = new();

        string[] userFolders = FTPHelper.FTP_ListDirectory($"ftp://{ps3Ip}/dev_hdd0/home/", user, pass);

        string username = "";

        for (int i = 0; i < userFolders.Length; i++)
        {
            username = FTPHelper.FTP_ReadFile($"ftp://{ps3Ip}/dev_hdd0/home/{userFolders[i]}/localusername", user,
                pass);
            users.Add(userFolders[i], username);

            Console.WriteLine("User found: " + username + $" <{userFolders[i]}>");
        }

        return users;
    }

    public static void LaunchSCETool(string args)
    {
        string platformExecutable = "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            platformExecutable = "scetool/win64/scetool.exe";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            platformExecutable = "scetool/linux64/scetool";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) platformExecutable = "";

        if (platformExecutable != "")
        {
            ProcessStartInfo startInfo = new();
            startInfo.UseShellExecute = false;
            startInfo.FileName = Path.GetFullPath(platformExecutable);
            startInfo.WorkingDirectory = Path.GetFullPath(".");
            startInfo.Arguments = args;
            startInfo.RedirectStandardOutput = true;

            Console.WriteLine("\n\n===== START SCETOOL =====\n");
            using (Process proc = Process.Start(startInfo))
            {
                while (!proc.StandardOutput.EndOfStream) Console.WriteLine(proc.StandardOutput.ReadLine());
                proc.WaitForExit();
            }

            Console.WriteLine("\n===== END SCETOOL =====\n\n");
        }
        else
        {
            throw new Exception("Error starting SCETool. Your platform may not be supported yet.");
        }
    }
    
    public void PSNEBOOTRemotePatch(string ps3ip, string gameID, string serverURL, string user, string pass)
    {
        string idps = "";
        Dictionary<string, string> users;

        _ps3Mapi.ConnectTarget(ps3ip);
        _ps3Mapi.PS3.RingBuzzer(PS3MAPI.PS3_CMD.BuzzerMode.Double);
        _ps3Mapi.PS3.Notify("UnionRemotePatcher Connected! Patching...");

        // Create simple directory structure
        Directory.CreateDirectory(@"data/rifs");
        Directory.CreateDirectory(@"eboot");
        Directory.CreateDirectory($@"eboot/{gameID}");
        Directory.CreateDirectory($@"eboot/{gameID}/original");
        Directory.CreateDirectory($@"eboot/{gameID}/patched");

        // Let's grab and backup our EBOOT
        FTPHelper.FTP_Download($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN",
            @$"eboot/{gameID}/original/EBOOT.BIN", user, pass);

        // Now we'll check and see if a backup exists on the server or not, if we don't have one on the server, then upload one
        if (!FTPHelper.FTP_CheckFileExists($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass))
            FTPHelper.FTP_Upload(@$"eboot/{gameID}/original/EBOOT.BIN",
                $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass);

        // Start getting idps and act.dat - these will help us decrypt a PSN eboot
        idps = PS3MAPI.PS3MAPI_Client_Server.PS3_GetIDPS();

        File.WriteAllBytes(@"data/idps", IDPSHelper.StringToByteArray(idps));

        users = GetUsers(ps3ip, user, pass);

        //GetUsers(ps3ip, "", "");

        //UserSelection select = new UserSelection(users.Values.ToArray(), gameID);

        //select.Show();

        // Not asking for user right now because I'm too tired - just use the first user (me)

        string currentUser = users.Keys.ToArray()[0];

        if (FTPHelper.FTP_CheckFileExists($"ftp://{ps3ip}/dev_hdd0/home/{currentUser}/exdata", user, pass))
        {
            FTPHelper.FTP_Download($"ftp://{ps3ip}/dev_hdd0/home/{currentUser}/exdata/act.dat", @"data/act.dat", user,
                pass);

            foreach (string fileName in FTPHelper.FTP_ListDirectory(
                         $"ftp://{ps3ip}/dev_hdd0/home/{currentUser}/exdata/", user, pass))
                if (fileName.Contains(gameID))
                    FTPHelper.FTP_Download($"ftp://{ps3ip}/dev_hdd0/home/{currentUser}/exdata/{fileName}",
                        @$"data/rifs/{fileName}", user, pass);
        }

        // Check for keys. If we don't have keys, we can't decrypt a PSN copy

        // Finally, let's decrypt the EBOOT.BIN
        LaunchSCETool($" -v -d \"{Path.GetFullPath(@$"eboot/{gameID}/original/EBOOT.BIN")}\" \"{Path.GetFullPath(@$"eboot/{gameID}/patched/EBOOT.ELF")}\"");

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

    // Cut-down version that only patches disc copies
    public void DiscEBOOTRemotePatch(string ps3ip, string gameID, string serverURL, string user, string pass)
    {
        // Create a simple directory structure
        Directory.CreateDirectory(@"data");
        Directory.CreateDirectory(@"eboot");
        Directory.CreateDirectory($@"eboot/{gameID}");
        Directory.CreateDirectory($@"eboot/{gameID}/original");
        Directory.CreateDirectory($@"eboot/{gameID}/patched");

        // Let's grab and backup our EBOOT
        FTPHelper.FTP_Download($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN",
            @$"eboot/{gameID}/original/EBOOT.BIN", user, pass);

        // Now we'll check and see if a backup exists on the server or not, if we don't have one on the server, then upload one
        if (!FTPHelper.FTP_CheckFileExists($"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass))
            FTPHelper.FTP_Upload(@$"eboot/{gameID}/original/EBOOT.BIN",
                $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN.BAK", user, pass);

        // Check for keys in the data directory
        if (!File.Exists("data/keys"))
            throw new FileNotFoundException(
                "UnionRemotePatcher cannot find the keys, ldr_curves, or vsh_curves files required to continue. Please make sure you have copies of these files placed in the data directory where you found the executable to run UnionRemotePatcher. Without them, we can't patch your game.");

        // Decrypt the EBOOT
        LaunchSCETool($"-d eboot/{gameID}/original/EBOOT.BIN eboot/{gameID}/original/EBOOT.ELF");

        // Now, patch the EBOOT;
        Patcher.PatchFile($"eboot/{gameID}/original/EBOOT.ELF", serverURL, $"eboot/{gameID}/patched/EBOOT.ELF");

        // Encrypt the EBOOT
        LaunchSCETool(
            $" --sce-type=SELF --skip-sections=FALSE --key-revision=0A --self-app-version=0001000000000000 --self-auth-id=1010000001000003 --self-vendor-id=01000002 --self-ctrl-flags=0000000000000000000000000000000000000000000000000000000000000000 --self-cap-flags=00000000000000000000000000000000000000000000003B0000000100040000 --self-type=APP --self-fw-version=0003005500000000 --compress-data true --encrypt \"{Path.GetFullPath(@$"eboot/{gameID}/patched/EBOOT.ELF")}\" \"{Path.GetFullPath(@$"eboot/{gameID}/patched/EBOOT.BIN")}\"");

        // And upload the encrypted, patched EBOOT to the system.
        FTPHelper.FTP_Upload(@$"eboot/{gameID}/patched/EBOOT.BIN",
            $"ftp://{ps3ip}/dev_hdd0/game/{gameID}/USRDIR/EBOOT.BIN", user, pass);
    }
}