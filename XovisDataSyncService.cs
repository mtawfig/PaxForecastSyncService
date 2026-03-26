using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace XovisPaxForecastFeedWinSvc
{
    public partial class XovisDataSyncService : ServiceBase
    {
        private Timer timer;

        public XovisDataSyncService()
        {
            InitializeComponent();

            this.ServiceName = "SvcXovisPaxForecastFeeder";
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            // Implement logging to file using Serilog library
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Information()
            //    .WriteTo.File(
            //        @"C:\Temp\Logs\PaxForecastFeedSvc-.log",
            //        rollingInterval: RollingInterval.Day,
            //        retainedFileCountLimit: 30,
            //        shared: true)
            //    .CreateLogger();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            try
            {
                Log.Information("XovisDataSyncService is starting.");

                timer = new Timer();

                // Default interval is 10 minutes
                int intervalMinutes = 10;
                string configIntervalValue = ConfigurationManager.AppSettings["IntervalMinutes"];
                if (!string.IsNullOrWhiteSpace(configIntervalValue))
                {
                    int parsedIntervalValue;
                    if (int.TryParse(configIntervalValue, out parsedIntervalValue) && parsedIntervalValue >= 1)
                    {
                        intervalMinutes = parsedIntervalValue;
                    }
                }
                
                // convert minutes to milliseconds
                timer.Interval = intervalMinutes * 60 * 1000;

                timer.Elapsed += Timer_Elapsed;

                timer.AutoReset = true;

                timer.Enabled = true;

                Log.Information($"XovisDataSyncService started successfully. Timer innterval: {intervalMinutes.ToString()} minutes.");

                Timer_Elapsed(null, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while starting the service.");
            }
        }

        protected override void OnStop()
        {
            try
            {
                Log.Information("XovisDataSyncService is stopping.");

                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while stopping the service.");
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Log.Information("Timer elapsed. Scheduled task started.");

                SyncXovisData();

                Log.Information("Scheduled task completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception occurred while executing scheduled task.");
            }
        }

        private void SyncXovisData()
        {
            var dtAreasMaster = OracleDBHelper.GetAreasMaster();
            if (dtAreasMaster == null || dtAreasMaster.Rows.Count == 0)
            {
                Log.Warning("No areas mapping data returned from database", EventLogEntryType.Warning);
                return;
            }

            List<AreaWaitTimeItem> allAreaWaitTimeItems = new List<AreaWaitTimeItem>();

            string configNumDaysValue = ConfigurationManager.AppSettings["NumberOfDays"];
            int numberOfDays = 1; // Default to fetch 1 day of historical data
            if (!string.IsNullOrWhiteSpace(configNumDaysValue))
            {
                int parsedNumDaysValue;
                if (int.TryParse(configNumDaysValue, out parsedNumDaysValue) && parsedNumDaysValue >= 1)
                {
                    numberOfDays = parsedNumDaysValue;
                }
            }


            // Yesterday start/end based on server local time
            //DateTime yesterday = DateTime.Today.AddDays(-1);
            DateTime fromDate = DateTime.Today.Date.AddDays(-numberOfDays); // 00:00:00
            DateTime toDate = DateTime.Today.Date.AddSeconds(-1); // 23:59:59
            long fromUnixTS = GenericHelper.ToUnixTimestamp(fromDate);
            long toUnixTS = GenericHelper.ToUnixTimestamp(toDate);

            foreach (DataRow row in dtAreasMaster.Rows)
            {
                int areaId = Convert.ToInt32(row["AREAID"]);
                DataTable dtXovisAreas = OracleDBHelper.GetAreasMapping(areaId);
                if (dtXovisAreas == null || dtXovisAreas.Rows.Count == 0)
                {
                    Log.Warning($"No mapping data returned from database for AreaID {areaId}", EventLogEntryType.Warning);
                    continue; // Skip if no mapping found for this area
                }
                var historicalDataTask = XovisAPIHelper.FetchDataFromApiHistorical(dtXovisAreas, areaId, fromUnixTS, toUnixTS);
                if (historicalDataTask == null || historicalDataTask.Result == null)
                {
                    Log.Warning($"No wait time data returned from XOVIS API for AreaID {areaId}", EventLogEntryType.Warning);
                    continue; // Skip if API call failed or returned no data
                }

                allAreaWaitTimeItems.AddRange(historicalDataTask.Result);
            }

            if (allAreaWaitTimeItems.Count == 0)
            {
                Log.Warning("No wait time data returned from XOVIS API for any area", EventLogEntryType.Warning);
                return;
            }

            //var finalResults = JsonConvert.SerializeObject(allAreaWaitTimeItems);
            //var result = OracleDBHelper.InsertWaitTimeData(allAreaWaitTimeItems);
            foreach (var item in allAreaWaitTimeItems)
            {
                var result = OracleDBHelper.InsertWaitTimeData(item);
                if (result == null || result.AFFECTEDROWS == 0)
                {
                    Log.Warning($"Failed to insert wait time data for AreaID: {item.AREAID}, TimeSlot: {item.TIMESLOT}", EventLogEntryType.Error);
                }
            }
        }
    }
}