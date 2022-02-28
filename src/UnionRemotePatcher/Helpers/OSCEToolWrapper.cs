using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UnionRemotePatcher.Helpers
{
    public static class OSCEToolWrapper
    {
        [DllImport("oscetool")]
        private static extern void main(int argc, string[] argv);

        public static void OSCETool(string args)
        {
            args = $"oscetool {args}";
            main(args.Split(' ').Length, args.Split(' '));
        }
    }
}