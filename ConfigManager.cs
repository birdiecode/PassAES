using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace aesPass
{
    internal class ConfigManager
    {
        private const int SIZE = 1024;
        public string path;

        public ConfigManager(string filename)
        {
            path = (string)System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), filename);
        }
        
        public string GetPrivateString(string aSection, string aKey)
        {
            StringBuilder buffer = new StringBuilder(SIZE);
            GetPrivateString(aSection, aKey, null, buffer, SIZE, path);
            return buffer.ToString();
        }
        
        public void WritePrivateString(string aSection, string aKey, string aValue)
        {
            WritePrivateString(aSection, aKey, aValue, path);
        }

        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
        private static extern int GetPrivateString(string section, string key, string def, StringBuilder buffer, int size, string path);

        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
        private static extern int WritePrivateString(string section, string key, string str, string path);
    }
}
