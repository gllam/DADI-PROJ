using System;
using System.Collections.Generic;
using System.Text;

namespace PuppetMaster
{
    class Utils
    {
        public static String ReadFromFile(String filePath)
        {
            return System.IO.File.ReadAllText(@filePath);
        }

    }
}
