﻿using System;
using Distributed.Node;
using Distributed.Proxy;
using System.Diagnostics;

using Distributed;

namespace DebugApplicationUsingPDLib
{
    public class TestAppJob : Job
    {
        
        public override void Main(string[] args)
        {
            for (int i = 0; i < 4; i++)
            {
                AddTask(new TestAppJobTask());
            }
            
        }

    }

    public class TestAppJobTask : JobTask
    {
        public override void Main(string[] args)
        {
            Console.WriteLine("Completing Job: " + args[0]);
        }
    }
    class Program2
    {

        /// <summary>
        /// Just a simple test app, run program with some number of arguments
        /// first to start up the "server" and then run the secondary proxy
        /// after to communicate. 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            Console.WriteLine("Hello World");
            bool val = false;
            if (val)
            {
                Process myProcess = new Process();

                try
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    // You can start any process, HelloWorld is a do-nothing example.
                    myProcess.StartInfo.FileName = "";
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.Start();
                    // This code assumes the process you are starting will terminate itself. 
                    // Given that is is started without a window so you cannot terminate it 
                    // on the desktop, it must terminate itself or you can do it programmatically
                    // from this application using the Kill method.
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            Console.WriteLine(Environment.GetCommandLineArgs()[0]);
            if (args.Length > 0)
            {
                NodeManager.Main();     
            }
            else
            {
                //Proxy p = new Proxy(new NodeReceiver(), new NodeSender(), , 12345);
                //Node.Main(new string[] { "129.21.89.207", "12345" });

            }
            
        }
        public delegate void MainMethod();
    }
}
