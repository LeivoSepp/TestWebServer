using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WebServerTest
{
    internal class WebServer
    {
        private const uint BufferSize = 8192;

        public async void Start()
        {
            var listener = new StreamSocketListener();
            await listener.BindServiceNameAsync("80"); //the 8080 port is already in use

            listener.ConnectionReceived += async (sender, args) =>
            {
                var request = new StringBuilder();
                using (var input = args.Socket.InputStream)
                {
                    var data = new byte[BufferSize];
                    IBuffer buffer = data.AsBuffer();
                    var dataRead = BufferSize;

                    while (dataRead == BufferSize)
                    {
                        await input.ReadAsync(
                             buffer, BufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(
                                                      data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                //string query = GetQuery(request);
                string json = "[{\"name\":\"pressure\",\"data\":" + PT.pressure + "},{\"name\":\"temperature\",\"data\":" + PT.tempreature + "}]";

                using (var output = args.Socket.OutputStream)
                {
                    using (var response = output.AsStreamForWrite())
                    {
                        String responseString = json;

                        var html = Encoding.UTF8.GetBytes(responseString);
                        using (var bodyStream = new MemoryStream(html))
                        {
                            var header = $"HTTP/1.1 200 OK\r\nContent-Length: {bodyStream.Length}\r\nConnection: close\r\n\r\n";
                            var headerArray = Encoding.UTF8.GetBytes(header);
                            await response.WriteAsync(headerArray,
                                                      0, headerArray.Length);
                            await bodyStream.CopyToAsync(response);
                            await response.FlushAsync();
                        }
                    }
                }
            };
        }
        private async Task<String> openExistingFile(string filename)
        {
            byte[] fileBytes;
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(filename);

            using (var stream = await file.OpenStreamForReadAsync())
            {
                fileBytes = new byte[stream.Length];
                stream.Read(fileBytes, 0, fileBytes.Length);
            }
            return System.Text.Encoding.UTF8.GetString(fileBytes);
        }
        private async Task<bool> isFilePresent(string fileName)
        {
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            return item != null;
        }
        private static string GetQuery(StringBuilder request)
        {
            var requestLines = request.ToString().Split(' ');

            var url = requestLines.Length > 1
                              ? requestLines[1] : string.Empty;

            var uri = new Uri("http://localhost" + url);
            var query = uri.Query;
            return query;
        }
    }
}