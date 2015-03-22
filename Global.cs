using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TerraFirma
{
    internal static class Global
    {
        internal static void WriteToLog(string msg)
        {
            FileStream fs = null;
            try
            {
                //write to log
                string fileLocation = AppDomain.CurrentDomain.BaseDirectory;
                string fileLog = System.IO.Path.GetDirectoryName(fileLocation) + "\\log.txt";

                fs = new FileStream(fileLog, FileMode.Append);
                msg = DateTime.Now.ToString() + ": " + msg + "\r\n";

                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(msg);
                fs.Write(messageBytes, 0, messageBytes.Length);

                fs.Flush();
                fs.Close();
            }
            catch (Exception exp)
            {
                try
                {
                    if (fs != null)
                        fs.Dispose();

                    string fileLocation = AppDomain.CurrentDomain.BaseDirectory;
                    string fileLog = System.IO.Path.GetDirectoryName(fileLocation) + "\\log2.txt";

                    fs = new FileStream(fileLog, FileMode.Append);
                    string message = "first log file locked. msg:" + msg + exp.Message + "\r\n";

                    byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
                    fs.Write(messageBytes, 0, messageBytes.Length);

                    fs.Flush();
                    fs.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        internal static void WriteToScript(string line)
        {
            StreamWriter sw = new StreamWriter("script.txt", true);
            sw.WriteLine(line);
            sw.Close();
        }


        //used to be a loop but I found a quicker solution.
        const ulong m1 = 0x5555555555555555UL;
        const ulong m2 = 0x3333333333333333UL;
        const ulong m4 = 0x0f0f0f0f0f0f0f0fUL;
        const ulong h01 = 0x0101010101010101UL;
        internal static int BitCount(ulong l)
        {
            if (l == 0)
                return 0;

            l -= (l >> 1) & m1;
            l = (l & m2) + ((l >> 2) & m2);
            l = (l + (l >> 4)) & m4;
            return (int)((l * h01) >> 56);
        }

        internal static void PrintBitBoard(this ulong bitBoard)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 7; i >= 0; i--)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (((bitBoard >> (i * 8 + j)) & 1L) == 1L)
                        sb.Append("1 ");
                    else
                        sb.Append(". ");
                }

                sb.Append('\n');
            }

            sb.Append('\n');
            Console.Write(sb);
        }

    }
}
