using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

using Microsoft.SqlServer.Server;
using System.Diagnostics;
using System.Collections.Generic;
using SandboxDatabaseManagerService.Configuration;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Storage;

namespace SandboxDatabaseManagerService
{
    public class PerformanceCounters
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        List<Tuple<string, string, PerformanceCounter, int?, string, int?, int?>> performanceCounters = new List<Tuple<string, string, PerformanceCounter, int?, string, int?, int?>>();
        private PerformanceCounterResults _result = new PerformanceCounterResults();
        private List<MonitoredFSRMQuota> MonitoredFSRMQuotaList = new List<MonitoredFSRMQuota>();
        List<Tuple<string, string, System.IO.DriveInfo, int?, string, int?, int?>> driveInfoCounters = new List<Tuple<string, string, System.IO.DriveInfo, int?, string, int?, int?>>();
        private IFsrmQuotaManager FsrmQuotaManager = null;
        private object _sync = new object();
        private Task _worker;
        public PerformanceCounterResults CurrentResults
        {
            get
            {
                lock(_sync)
                {
                    return _result;
                }

            }
        }
        static PerformanceCounters _instance;
        static PerformanceCounters Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PerformanceCounters();

                return _instance;
            }
        }
        private PerformanceCounters()
        {
            

            foreach (var item in MonitoredCountersSection.Instance.ItemsList)
            {
                try
                {
                    // Special case for reporting LogicalDisk\Free MegaBytes counter values because the counter does only update once per 5 minutes, need to make this more frequent by using System.IO.DriveInfo from .Net
                    if (String.Compare("LogicalDisk", item.CategoryName, true) == 0 && String.Compare("Free MegaBytes", item.CounterName, true) == 0 && item.InstanceName.Length > 0)
                    {
                        bool found = false;
                        foreach(var driveInfo in System.IO.DriveInfo.GetDrives())
                        {
                            if(String.Compare(driveInfo.Name.Substring(0,1), item.InstanceName.Substring(0, 1), true) == 0)
                            {
                                found = true;

                                driveInfoCounters.Add(new Tuple<string, string, System.IO.DriveInfo, int?, string, int?, int?>(item.FriendlyName,
                                                        item.DotNetFormatString,
                                                        driveInfo,
                                                        String.IsNullOrWhiteSpace(item.DivideRawCounterValueBy) ? null : (int?)Int32.Parse(item.DivideRawCounterValueBy),
                                                        item.ChartYAxisSufix,
                                                        String.IsNullOrWhiteSpace(item.LowWarningValue) ? null : (int?)Int32.Parse(item.LowWarningValue),
                                                        String.IsNullOrWhiteSpace(item.HighWarningValue) ? null : (int?)Int32.Parse(item.HighWarningValue)
                                                        ));

                            }
                        }

                        if(!found)
                        {
                            Log.Error(String.Format("Failed initialize counter (System.IO.DriveInfo), Category: {0}, Counter:{1}, Instance:{2}. This counter will not be reported.", item.CategoryName, item.CounterName, item.InstanceName));
                        }

                        continue;
                    }


                    PerformanceCounter counter = new PerformanceCounter
                    {
                        CategoryName = item.CategoryName,
                        CounterName = item.CounterName,
                        InstanceName = item.InstanceName,
                    };
                    counter.NextValue();

                    performanceCounters.Add(new Tuple<string, string, PerformanceCounter, int?, string, int?, int?>(item.FriendlyName, 
                        item.DotNetFormatString, 
                        counter, 
                        String.IsNullOrWhiteSpace(item.DivideRawCounterValueBy) ? null : (int?)Int32.Parse(item.DivideRawCounterValueBy), 
                        item.ChartYAxisSufix, 
                        String.IsNullOrWhiteSpace(item.LowWarningValue) ? null : (int?)Int32.Parse(item.LowWarningValue), 
                        String.IsNullOrWhiteSpace(item.HighWarningValue) ? null : (int?)Int32.Parse(item.HighWarningValue)
                        ));
                }
                catch(Exception ex)
                {
                    Log.Error(String.Format("Failed initialize counter, Category: {0}, Counter:{1}, Instance:{2}. This counter will not be reported.", item.CategoryName, item.CounterName, item.InstanceName), ex);
                }
            }

            try
            {
                if (MonitoredFSRMQuotasSection.Instance.ItemsList.Count > 0)
                {
                    FsrmQuotaManager = new FsrmQuotaManager();


                    foreach (var quotaItem in MonitoredFSRMQuotasSection.Instance.ItemsList)
                    {
                        try
                        {
                            if (!String.IsNullOrWhiteSpace(quotaItem.LowWarningValue))
                            {
                                try
                                {
                                    Int32.Parse(quotaItem.LowWarningValue);

                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("LowWarningValue not in correct format");
                                }
                            }

                            if (!String.IsNullOrWhiteSpace(quotaItem.HighWarningValue))
                            {
                                try
                                {
                                    Int32.Parse(quotaItem.HighWarningValue);

                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("HighWarningValue not in correct format");
                                }
                            }

                            if (!String.IsNullOrWhiteSpace(quotaItem.DivideRawValueBy))
                            {
                                try
                                {
                                    Int32.Parse(quotaItem.DivideRawValueBy);

                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("DivideRawValueBy not in correct format");
                                }
                            }

                            var quotaEntry = FsrmQuotaManager.GetQuota(quotaItem.QuotaFolder);

                            MonitoredFSRMQuotaList.Add(quotaItem);

                        }
                        catch (Exception ex)
                        {
                            Log.Error(String.Format("Failed to initialize MonitoredFSRMQuota, FriendlyName {0}. This quota entry will not be reported.", quotaItem.FriendlyName), ex);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to load FSRM Quota Manager, no FSRM quotas will be reported, Exception: {0}", ex.Message);
            }




            

            _worker = new Task(GetPerformanceCounterValues, TaskCreationOptions.LongRunning);
            _worker.Start();

        }
        void GetPerformanceCounterValues()
        {
            PerformanceCounterResults resultNew;
            Dictionary<string, CounterSample> prevSample = new Dictionary<string, CounterSample>();

            while (true)
            {
                try
                {

                    resultNew = new PerformanceCounterResults();


                    foreach (var value in PerformanceCounters.Instance.performanceCounters)
                    {
                        try
                        {


                            var counterSample = value.Item3.NextSample();

                            if(prevSample.ContainsKey(value.Item1))
                            {
                                bool warning = false;

                                float counterValueRaw = CounterSample.Calculate(prevSample[value.Item1], counterSample);
                                
                                if (value.Item4.HasValue)
                                    counterValueRaw = counterValueRaw / value.Item4.Value;

                                if (value.Item6.HasValue && counterValueRaw < value.Item6.Value)
                                    warning = true;

                                if (value.Item7.HasValue && counterValueRaw > value.Item7.Value)
                                    warning = true;


                                resultNew.ResultList.Add(new PerformanceCounterResults.PerformanceCounterResult() { CounterFriendlyName = value.Item1, CounteValue = counterValueRaw, DotNetFormatString = value.Item2, ChartYAxisSufix = value.Item5, Warning = warning });
                            }

                            prevSample[value.Item1] = counterSample;

                        }
                        catch (Exception ex)
                        {
                            Log.Error(String.Format("Failed to retrieve result from performance counter, counter FriendlyNamme: {0}", value.Item1), ex);
                        }
                    }

                    foreach (var value in PerformanceCounters.Instance.driveInfoCounters)
                    {
                        try
                        {


                            float counterValueRaw = value.Item3.TotalFreeSpace / 1048576;
                            
                            bool warning = false;
                            
                            if (value.Item4.HasValue)
                                counterValueRaw = counterValueRaw / value.Item4.Value;

                            if (value.Item6.HasValue && counterValueRaw < value.Item6.Value)
                                warning = true;

                            if (value.Item7.HasValue && counterValueRaw > value.Item7.Value)
                                warning = true;


                            resultNew.ResultList.Add(new PerformanceCounterResults.PerformanceCounterResult() { CounterFriendlyName = value.Item1, CounteValue = counterValueRaw, DotNetFormatString = value.Item2, ChartYAxisSufix = value.Item5, Warning = warning });
                      

                        }
                        catch (Exception ex)
                        {
                            Log.Error(String.Format("Failed to retrieve result from performance counter (by driveInfo), counter FriendlyNamme: {0}", value.Item1), ex);
                        }
                    }


                    if (FsrmQuotaManager != null)
                    {
                        foreach(var quotaItem in MonitoredFSRMQuotaList)
                        {
                            try
                            {
                                var quotaEntry = FsrmQuotaManager.GetQuota(quotaItem.QuotaFolder);
                                var quoteFreeSpace = (((float)quotaEntry.QuotaLimit) - ((float)quotaEntry.QuotaUsed));

                                bool warning = false;

                                if (!String.IsNullOrWhiteSpace(quotaItem.DivideRawValueBy))
                                {
                                    try
                                    {
                                        quoteFreeSpace = quoteFreeSpace / Int32.Parse(quotaItem.DivideRawValueBy);

                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("DivideRawValueBy not in correct format");
                                    }
                                }


                                if (!String.IsNullOrWhiteSpace(quotaItem.LowWarningValue))
                                {
                                    try
                                    {
                                        if(quoteFreeSpace < Int32.Parse(quotaItem.LowWarningValue))
                                        {
                                            warning = true;
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("LowWarningValue not in correct format");
                                    }
                                }

                                if (!String.IsNullOrWhiteSpace(quotaItem.HighWarningValue))
                                {
                                    try
                                    {
                                        if (quoteFreeSpace > Int32.Parse(quotaItem.LowWarningValue))
                                        {
                                            warning = true;
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("HighWarningValue not in correct format");
                                    }
                                }


                                resultNew.ResultList.Add(new PerformanceCounterResults.PerformanceCounterResult() { CounterFriendlyName = quotaItem.FriendlyName, CounteValue = quoteFreeSpace, DotNetFormatString = quotaItem.DotNetFormatString, ChartYAxisSufix = quotaItem.ChartYAxisSufix, Warning = warning });
                            }
                            catch (Exception ex)
                            {
                                Log.Error(String.Format("Failed to retrieve result from MonitoredFSRMQuota, FriendlyName: {0}", quotaItem.FriendlyName), ex);
                            }
                        }
                    }

                    lock (_sync)
                    {
                        _result = resultNew;

                    }

                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    Log.Error("Exception in UserList.", ex);
                }
            }

            


        }

        internal static PerformanceCounterResults GetPerformanceCounters()
        {
            return PerformanceCounters.Instance.CurrentResults;
        }

        internal static void Initiallize()
        {
            var dummy =  PerformanceCounters.Instance;
        }

    }

}
