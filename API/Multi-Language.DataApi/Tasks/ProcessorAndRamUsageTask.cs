﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Multi_language.Services;
using Newtonsoft.Json;

namespace Multi_Language.DataApi.Tasks
{
    public  class ProcessorAndRamUsageTask : IProcessorAndRamUsageTask
    {
        static PerformanceCounter _cpuCounter, _memUsageCounter;
        private readonly ISystemStabilityLoggsService systemStabilityLoggsService;

        public ProcessorAndRamUsageTask(ISystemStabilityLoggsService systemStabilityLoggsService)
        {
            this.systemStabilityLoggsService = systemStabilityLoggsService;
        }
        public  void CallWebApi()
        {
            try
            {
                _cpuCounter = new PerformanceCounter();
                _cpuCounter.CategoryName = "Processor";
                _cpuCounter.CounterName = "% Processor Time";
                _cpuCounter.InstanceName = "_Total";

                _memUsageCounter = new PerformanceCounter("Memory", "Available KBytes");

                RunPollingThread();

                SaveInDb();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void SaveInDb()
        {
            // throw new NotImplementedException();
        }

        static void RunPollingThread()
        {
            double cpuTime;
            ulong memUsage, totalMemory;

            // Get the stuff we need to send
            GetMetrics(out cpuTime, out memUsage, out totalMemory);

            // Send the data
            var postData = new
            {
                MachineName = System.Environment.MachineName,
                Processor = cpuTime,
                MemUsage = memUsage,
                TotalMemory = totalMemory
            };

            var json = JsonConvert.SerializeObject(postData);
            // Post the data to the server
            var serverUrl = new Uri(ConfigurationManager.AppSettings["ServerUrl"]);

            var client = new WebClient();
            client.Headers.Add("Content-Type", "application/json");
            client.UploadString(serverUrl, json);

        }


        static void GetMetrics(out double processorTime, out ulong memUsage, out ulong totalMemory)
        {
            processorTime = (double)_cpuCounter.NextValue();
            memUsage = (ulong)_memUsageCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            processorTime = (double)_cpuCounter.NextValue();
            memUsage = (ulong)_memUsageCounter.NextValue();

            totalMemory = 0;

            // Get total memory from WMI
            ObjectQuery memQuery = new ObjectQuery("SELECT * FROM CIM_OperatingSystem");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(memQuery);

            foreach (ManagementObject item in searcher.Get())
            {
                totalMemory = (ulong)item["TotalVisibleMemorySize"];
            }
        }

    }
}