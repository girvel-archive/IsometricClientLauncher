using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IsometricLauncher
{
    internal class Program
    {
        private static void Main(string[] consoleArgs)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    TypeNameHandling = TypeNameHandling.Objects,
                });

            Version version;
            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Connect(IPAddress.Parse("192.168.0.100"), 7999);
                    socket.Send(
                        Encoding.ASCII.GetBytes(
                            new JObject
                            {
                                ["type"] = "get version",
                                ["args"] = JObject.FromObject(new Dictionary<string, dynamic>(), serializer),
                            }.ToString()));

                    var currentStringBuilder = new StringBuilder();
                    var receivedData = new byte[4096];

                    do
                    {
                        var bytes = socket.Receive(receivedData);

                        currentStringBuilder.Append(Encoding.ASCII.GetString(receivedData, 0, bytes));
                    } while (socket.Available > 0);

                    version = JObject.Parse(currentStringBuilder.ToString())["version"].ToObject<Version>();
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine("\n\nCan not connect to server. Press any key to close this window...");
                    Console.ReadKey(true);
                    return;
                }
            }

            var path = "version.txt";
            var needsUpdate = !File.Exists(path);

            if (!needsUpdate)
            {
                using (var stream = File.OpenRead(path))
                using (var reader = new StreamReader(stream))
                {
                    var currentVersion = Version.Parse(reader.ReadToEnd());
                    needsUpdate = currentVersion < version;
                    Console.WriteLine($"Your version is {currentVersion}");
                }
            }

            if (needsUpdate)
            {
                Console.WriteLine("You need to download update");
                using (var client = new WebClient())
                {
                    Console.WriteLine("Begin downloading file");
                    var task = client.DownloadFileTaskAsync(
                        new Uri(
                            @"https://github.com/girvel/IsometricClientNew/raw/master/Isometric%20Client/Imperia.zip"),
                        "Imperia.zip");

                    client.DownloadProgressChanged += (obj, args) =>
                    {
                        Console.WriteLine(args.ProgressPercentage + "% of file is downloaded");
                    };

                    task.Wait();
                }

                Console.WriteLine("Begin zip extracting");

                if (Directory.Exists("Imperia"))
                {
                    Directory.Delete("Imperia", true);
                }

                ZipFile.ExtractToDirectory("Imperia.zip", Directory.GetCurrentDirectory());

                Console.WriteLine("Zip is extracted");

                using (var stream = File.OpenWrite(path))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(version);
                    Console.WriteLine($"Your version is {version} now");
                }
            }

            Console.WriteLine("Starting client...");
            Process.Start(Path.Combine("Imperia", "Client.exe"));
        }
    }
}
