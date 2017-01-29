using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;
using Glovebox.Graphics.Drivers;
using Glovebox.Graphics.Components;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using Newtonsoft.Json;

namespace WebServerTest
{
    class PT
    {
        public static double pressure;
        public static double tempreature;
    }
    public sealed class StartupTask : IBackgroundTask
    {
        // MPL115A2 Sensor
        private MPL115A2 MPL115A2Sensor;
        private static BackgroundTaskDeferral _Deferral = null;
        private async Task saveStringToLocalFile(string filename, string content)
        {
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(content.ToCharArray());
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
            }
        }
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _Deferral = taskInstance.GetDeferral();
            MPL115A2Sensor = new MPL115A2();
            var webserver = new WebServer();
            await ThreadPool.RunAsync(workItem =>
            {
                webserver.Start();
            });
            while (true)
            {
                PT.pressure = Math.Round(MPL115A2Sensor.getPressure(), 1);
                PT.tempreature= Math.Round(MPL115A2Sensor.getTemperature(), 1);
                Task.Delay(1000).Wait();
            }
        }
    }
}
