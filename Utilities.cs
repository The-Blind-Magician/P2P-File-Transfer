using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Utilities
{
    public class Utilities
    {
        public IPAddress IPAddress;

        public TcpListener serverListener;
        public Socket serverSocket;

        public TcpClient client;
        public Stream clientStream;

        public int BUFFER_SIZE = 1024;

        public long fileSize = 0;
        public string fileName = "";
        public Utilities() { }
        public Utilities(IPAddress ip)
        {
            IPAddress = ip;
        }
        public async Task SetupServer()
        {
            //serverListener = new TcpListener(IPAddress.Parse(new WebClient().DownloadString("http://icanhazip.com")), 5001);
            //serverListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5001);
            //serverListener.Start();
            Console.Write("Establishing connection... ");
            serverListener = new TcpListener(IPAddress.Loopback, 80);
            serverListener.Start();

            serverSocket = await serverListener.AcceptSocketAsync();
            //serverSocket = await serverListener.AcceptSocketAsync();
            Console.Write("Connected to destination client\n");
        }
        public async Task InitServer(string file)
        {
            fileSize = new FileInfo(file).Length;
            await SendData(Encoding.ASCII.GetBytes(fileSize.ToString()));
            fileName = new FileInfo(file).Name;
            await SendData(Encoding.Default.GetBytes(fileName));
        }

        public async Task SetupClient()
        {
            client = new TcpClient();
            Console.Write("Establishing connection... ");
            await client.ConnectAsync(IPAddress, 80);
            Console.Write("Connected to source server\n\n");
        }
        public async Task InitClient()
        {
            byte[] tempArr = await RecieveInitData();
            fileSize = Convert.ToInt64(Encoding.ASCII.GetString(tempArr));
            tempArr = await RecieveInitData();
            fileName = Encoding.ASCII.GetString(tempArr).Trim();
        }
        public async IAsyncEnumerable<byte[]> RecieveFileData()
        {
            clientStream = client.GetStream();
            byte[] byteArr = new byte[BUFFER_SIZE];
            byte[] result;
            long bytesReceived = 0;
            do
            {
                //Console.WriteLine("Awaiting stream read");
                int length = await clientStream.ReadAsync(byteArr, 0, byteArr.Length);

                result = byteArr[0..length];

                await clientStream.WriteAsync(Encoding.Default.GetBytes($"Success"));
                //Console.WriteLine("Awaiting write)");
                yield return result;
            } while (bytesReceived < fileSize);
            Console.WriteLine($"packet only {result.Length}");
        }

        public async Task<byte[]> RecieveInitData()
        {
            clientStream = client.GetStream();
            byte[] byteArr = new byte[BUFFER_SIZE];
            for (int i = 0; i < BUFFER_SIZE; i++) { byteArr[i] = BitConverter.GetBytes(' ').First(); }
            await clientStream.ReadAsync(byteArr, 0, byteArr.Length);
            await clientStream.WriteAsync(Encoding.Default.GetBytes($"Success"));
            return byteArr;
        }

        public async IAsyncEnumerable<long> SendData(string path)
        {
            long bytesSent = 0;
            byte[] tempArr = new byte[BUFFER_SIZE];
            await foreach (var result in ChunkData(path))
            {
                //Console.WriteLine("\nAwaiting send data");
                //Console.WriteLine(Encoding.Default.GetString(result));
                await serverSocket.SendAsync(result, SocketFlags.None);// TODO: do not send and recieve empty packets
                                                                       //Console.WriteLine("Awaiting recieve");
                var res = await serverSocket.ReceiveAsync(tempArr, SocketFlags.None);
                yield return (bytesSent += result.Length);
            }
        }
        public async Task SendData(byte[] byteArr)
        {
            byte[] tempArr = new byte[20];
            //Console.WriteLine("\nAwaiting send data 1");
            await serverSocket.SendAsync(byteArr, SocketFlags.None);
            Console.WriteLine($"\n\"{Encoding.Default.GetString(byteArr).Trim()}\"");
            //Console.WriteLine("Awaiting recieve 1");
            await serverSocket.ReceiveAsync(tempArr, SocketFlags.None);
            //tempArr = (byte[])tempArr.Where(x => BitConverter.ToString(new byte[] { x }) != "32"); //read byte space
            Console.WriteLine(Encoding.ASCII.GetString(tempArr).Trim() + "\n");
        }

        public async IAsyncEnumerable<byte[]> ChunkData(string path)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using (var file = new BinaryReader(stream))
            {
                do
                {
                    var tempData = file.ReadBytes(BUFFER_SIZE);
                    if (tempData.Length < BUFFER_SIZE) { yield return tempData; break; }
                    await stream.FlushAsync();
                    yield return tempData;
                } while (true);
            }
        }
        public async IAsyncEnumerable<byte[]> ChunkString(string str)
        {
            //while (true)
            //{
            byte[] tempData = new byte[BUFFER_SIZE]; //Stores one chunk of data
            byte[] dataBytes = Encoding.Default.GetBytes(str); //Stores all data TODO: convert to filestream
            int extraBytes = 0; //Track any extra bytes after we have exhausted full chunks
            int dataChunks = dataBytes.Length / BUFFER_SIZE; //Determine how many chunks are in the 'file'
            for (int i = 0; i <= dataChunks; i++) //Loop for all dataChunks and one more for extraBytes
            {
                if (i == dataChunks) //Last iteration
                {
                    extraBytes = dataBytes.Length - dataChunks * BUFFER_SIZE;
                    if (extraBytes == 0) break; //Break if no extra bytes
                }
                int endPoint = dataChunks > 0 ? (BUFFER_SIZE * (i != dataChunks ? (i + 1) : i)) : 0;
                int frontPoint = i == 0 ? 0 : (BUFFER_SIZE * i);

                tempData = dataBytes[frontPoint..(endPoint + extraBytes)]; //Assign tempData to range

                yield return tempData;
            }
            //}
        }
    }
}
