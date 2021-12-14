using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBPUnion.UnionPatcher;
using System.IO;
using System.Net;

namespace UnionRemotePatcher
{
    public class RemotePatch
    {
        private static void DownloadFile(string url, string path)
        {
            WebClient cln = new WebClient();
            cln.DownloadFile(url, path);
        }

        public static void EBOOTRemotePatch(string ps3ip, string gameID, string serverURL)
        {
            Directory.CreateDirectory(@"Files");
            DownloadFile("https://www.psx-place.com/resources/trueancestor-self-resigner-by-jjkkyu.33/download?version=44", @"Files/TrueAncestor.tmp");

        }
    }
}
