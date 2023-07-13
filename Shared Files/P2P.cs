using P2P_Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2P
{
    class P2P
    {
        public static Utilities utils;        
        //public static string testPath = @"..\..\..\..\Shared Files\firmware-bltouch-for-z-homing.bin";
        //public static string testPath = @"..\..\..\..\Shared Files\P2P.txt";
        public static string testPath = @"..\..\..\..\Shared Files\Test.txt";
        //public static string testPath = @"..\..\..\..\Shared Files\Test Small.txt";
        [STAThread]
        static async Task Main(string[] args)
        {
            bool MODE = args[0] == "send";
            if (MODE)
            {
                utils = new Utilities();
                await Server();
            }
            else
            {
                //utils = new Utilities(IPAddress.Parse(args[3]));
                utils = new Utilities();
                await Client();
            }
        }

        static async Task Client()
        {
            await utils.SetupClient();
            await utils.InitClient();
            long bytesReceived = 0;
            utils.fileName = utils.fileName.Trim();
            string path = Path.Combine(Environment.CurrentDirectory, utils.fileName);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (!File.Exists(path)) { File.Create(path).Close(); }
            using (FileStream fileStream = new FileStream(path, FileMode.Truncate, FileAccess.Write))
            {
                await foreach (var result in utils.RecieveFileData())
                {
                    await fileStream.FlushAsync();
                    //var temp = result.Where(x => x != BitConverter.GetBytes(' ').First()).ToArray();
                    var temp = result.ToArray();
                    if (Encoding.Default.GetString(temp) == "stop1234") { Console.WriteLine($"\n\"{Encoding.Default.GetString(temp)}\" received"); break; }
                    await fileStream.WriteAsync(temp);

                    Console.CursorLeft = 0;
                    Console.Write($"{bytesReceived += temp.Length}/{utils.fileSize} bytes received");
                }
            }
            stopwatch.Stop();
            Console.Write($"\n\nFile sucessfully downloaded in {stopwatch.Elapsed}s"); //TODO: file contains one duplicate packet at the end
            Console.ReadKey();
        }

        static async Task Server()
        {
            await utils.SetupServer();
            await utils.InitServer(testPath);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await foreach (var result in utils.SendData(testPath))
            {
                Console.CursorLeft = 0;
                Console.Write($"{result}/{utils.fileSize} bytes sent");
            }
            Thread.Sleep(500);
            await utils.SendData(Encoding.Default.GetBytes("stop1234"));
            stopwatch.Stop();
            Console.WriteLine($"\n\nFile sent in {stopwatch.Elapsed}s");
            Console.ReadKey();
        }
    }
}
