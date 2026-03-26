using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XovisPaxForecastFeedWinSvc
{
    internal class DataModels
    {
    }

    public class XovisHistoricalResponse
    {
        public List<List<long>> values { get; set; }
    }

    // Class to represent the raw area wait time data
    public class AreaWaitTimeRawItem
    {
        public int AREAID { get; set; }
        public int DAYCODE { get; set; }
        public int TIMESLOT { get; set; }
        public int WAITTIME { get; set; }
    }

    // Class to represent the grouped area wait time data
    public class AreaWaitTimeItem
    {
        public int AREAID { get; set; }
        public int DAYCODE { get; set; }
        public int TIMESLOT { get; set; }

        public int MINWAITTIME { get; set; }
        public int MAXWAITTIME { get; set; }
    }

    public class InsertWaitTimeResult
    {
        public int AFFECTEDROWS { get; set; }
        public string MESSAGE { get; set; }
        public string EXCEPTION { get; set; }
    }
}
