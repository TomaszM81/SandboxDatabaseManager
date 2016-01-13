using SandboxDatabaseManager.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SandboxDatabaseManager.Worker
{
    public class GarbageFileCollector
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Lazy<GarbageFileCollector> _instance = new Lazy<GarbageFileCollector>(() => new GarbageFileCollector());
        private List<string> _whileList = new List<string>();
        private Task backgroundTask;
        private object _sync = new object();
        private GarbageFileCollector()
        {
            backgroundTask = new Task(RemoveOldFiles, TaskCreationOptions.LongRunning);
            backgroundTask.Start();
        }

        public static void Initialize()
        {
            var dummmy = Instance;
        }

        async void RemoveOldFiles()
        {
            while (true)
            {
                try
                {

                   

                        foreach(var databaseServer in DatabaseServers.Instance.ItemsList)
                        {
                            if(Directory.Exists(databaseServer.CopyDatabaseNetworkSharePath))
                            {
                                foreach(var filePath in Directory.GetFiles(databaseServer.CopyDatabaseNetworkSharePath))
                                {

                                    var partial = Path.GetFileNameWithoutExtension(filePath);
                                    var begin = partial.LastIndexOf('_');


                                    if (begin >= 0 && partial.Length >= begin + 37)
                                    {
                                        var guidName = partial.Substring(begin + 1, 36).ToUpper();
                                        bool fileInUse = false;

                                        lock (_sync)
                                        {
                                            if (_whileList.Contains(guidName))
                                            {
                                                fileInUse = true;
                                            }
                                        }

                                        if (!fileInUse)
                                        {
                                            try
                                            {
                                                File.Delete(filePath);
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Error(String.Format("Error while deleting garbage file: {0}.", filePath), ex);
                                            }
                                        }

                                    }else
                                    { 
                                        try
                                        {
                                            File.Delete(filePath);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(String.Format("Error while deleting garbage file: {0}.", filePath), ex);
                                        }

                                    }



                                }
                            }

                        }


                    await Task.Delay(new TimeSpan(0, 5, 0));
                }
                catch (Exception ex)
                {
                    Log.Error("Exception in RemoveOldFiles.", ex);
                }
            }

        }


        public static GarbageFileCollector Instance
        {
            get
            {
                return _instance.Value;
            }
        }

     

        public void AddGUIDToWhiteList(string fileGuid)
        {
            lock (_sync)
            {
                if (!_whileList.Contains(fileGuid))
                    _whileList.Add(fileGuid);
            }
        }

        public void RemoveGUIDFromWhiteList(string fileGuid)
        {
            lock (_sync)
            {
                if (_whileList.Contains(fileGuid))
                    _whileList.Remove(fileGuid);
            }
        }


       
    } 
}