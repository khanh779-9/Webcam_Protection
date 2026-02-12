using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Webcam_Protection
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            string webcamVID_PID = "\\\\?\\USB\\VID_0C45&PID_671E&MI_00\\6&1296259f&0&0000"; // VID/PID webcam của bạn

            while(true)
            {
                Console.WriteLine($"🔍 Kiểm tra ai đang truy cập webcam {webcamVID_PID}...");

                List<int> processes = WebcamDetector.GetProcessesUsingDevice(webcamVID_PID);
                if (processes.Count > 0)
                {
                    foreach (int pid in processes)
                    {
                        string processName = Process.GetProcessById(pid).ProcessName;
                        Console.WriteLine($"📸 Phát hiện: {processName} (PID: {pid}) đang truy cập webcam!");
                    }
                }
                else
                {
                    Console.WriteLine("✅ Không có process nào đang truy cập webcam.");
                }
                Thread.Sleep(2000); // Kiểm tra lại sau 5 giây
            }    

        }
    }
}
