using P2P_Utilities;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2P
{
    class P2P
    {
        public static Utilities utils;
        public static string testPath = @"C:\Users\chris\Documents\GitHub\P2P-File-Transfer\Test.txt";
        [STAThread]
        static async Task Main(string[] args)
        {
            bool MODE = args[0] == "send" ? true : false;
            if (MODE)
            {
                utils = new Utilities();
                await Server();
            }
            else
            {
                //utils = new Utilities(IPAddress.Parse(args[3]));
                utils = new Utilities(IPAddress.Loopback);
                await Client();
            }
        }              

        static async Task Client()
        {
            await utils.SetupClient();
            await utils.InitClient();
            long bytesReceived = 0;
            utils.fileName = utils.fileName.Trim();
            string path = Path.Combine(Environment.CurrentDirectory, utils.fileName.Trim());

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine(path);
            if (!File.Exists(path))
                File.Create(path);

            using (FileStream fileStream = new FileStream(Path.Combine(Environment.CurrentDirectory, utils.fileName.Trim()), FileMode.Append, FileAccess.Write))
            {
                await foreach (var result in utils.RecieveFileData())
                {
                    await fileStream.WriteAsync(result);
                    Console.Write($"{bytesReceived += result.Length}/{utils.fileSize} bytes received");
                    Console.CursorLeft = 0;
                    await fileStream.FlushAsync();
                }
            }
            stopwatch.Stop();
            Console.Write($"\nFile sucessfully downloaded in {stopwatch.Elapsed}s");
        }

        static async Task Server()
        {
            await utils.SetupServer();
            await utils.InitServer(testPath);
            
            long bytesSent = 0; 
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await foreach(var result in utils.SendData(testPath))
            {
                bytesSent += result;
                Console.Write($"{bytesSent}/{utils.fileSize} bytes sent");
                Console.CursorLeft = 0;
            }
            stopwatch.Stop();
            Console.WriteLine($"\nFile sent in {stopwatch.Elapsed}s");
        }
    }
}
