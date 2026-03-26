using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace XovisPaxForecastFeedWinSvc
{
    public static class XovisAPIHelper
    {
        private static readonly string xovisBaseApiUrl = "http://10.1.6.32:8010/XsWebService";
        private static readonly string xovisTemplateApiUrl = "http://10.1.6.32:8010/XsWebService/getPlaceHistorical?place=_place_&item=_item_&name=_name_&value=_value_&from=_from_&to=_to_&interval=_interval_";
        //Sample: http://10.1.6.32:8010/XsWebService/getPlaceHistorical?place=CheckInZoneA&item=Queue&name=CheckIn_ZoneA_Queue&value=WaitF&from=1772614205&to=1772700605&interval=900
        private static readonly string xovisApiUsername = "API";
        private static readonly string xovisApiPassword = "newuser1212!!";

        public static async Task<string> FetchDataFromApiLive(int areaId)
        {
            try
            {
                //string updateInterval = "10115000";

                using (HttpClient client = new HttpClient())
                {
                    // Set up the Basic Authentication header
                    var authToken = Encoding.ASCII.GetBytes($"{xovisApiUsername}:{xovisApiPassword}");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
                    var xovisApiUrl = xovisBaseApiUrl + $"/getLiveData?type=APP&id={areaId}";
                    HttpResponseMessage response = await client.GetAsync(xovisApiUrl).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException e)
            {
                // Handle exception
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                // Handle any other exceptions
                Console.WriteLine($"General error: {e.Message}");
                return null;
            }
        }

        public static async Task<List<AreaWaitTimeItem>> FetchDataFromApiHistorical(DataTable dtXovisAreas, int? areaId, long fromUnixTS, long toUnixTS)
        {
            try
            {
                if (dtXovisAreas == null || dtXovisAreas.Rows.Count == 0)
                {
                    //Console.WriteLine("No area data found.");
                    return null;
                }

                var rawResults = new List<AreaWaitTimeRawItem>();

                using (HttpClient client = new HttpClient())
                {
                    var authToken = Encoding.ASCII.GetBytes(xovisApiUsername + ":" + xovisApiPassword);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

                    foreach (DataRow row in dtXovisAreas.Rows)
                    {
                        int currentAreaId = Convert.ToInt32(row["AREAMASTERID"]);
                        string place = Convert.ToString(row["PLACE"]);
                        string item = Convert.ToString(row["ITEM"]);
                        string name = Convert.ToString(row["NAME"]);
                        string value = Convert.ToString(row["ELEMENT"]);
                        string intervalText = Convert.ToString(row["INTERVAL"]);

                        string xovisApiUrl = xovisTemplateApiUrl
                            .Replace("_place_", Uri.EscapeDataString(place ?? string.Empty))
                            .Replace("_item_", Uri.EscapeDataString(item ?? string.Empty))
                            .Replace("_name_", Uri.EscapeDataString(name ?? string.Empty))
                            .Replace("_value_", Uri.EscapeDataString(value ?? string.Empty))
                            .Replace("_from_", fromUnixTS.ToString(CultureInfo.InvariantCulture))
                            .Replace("_to_", toUnixTS.ToString(CultureInfo.InvariantCulture))
                            .Replace("_interval_", Uri.EscapeDataString(intervalText ?? string.Empty));

                        HttpResponseMessage response = await client.GetAsync(xovisApiUrl).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        XovisHistoricalResponse apiData = JsonConvert.DeserializeObject<XovisHistoricalResponse>(responseContent);

                        if (apiData == null || apiData.values == null)
                            continue;

                        foreach (List<long> itemPair in apiData.values)
                        {
                            if (itemPair == null || itemPair.Count < 2)
                                continue;

                            long unixTimestamp = itemPair[0];
                            int waitTime = Convert.ToInt32(itemPair[1]);

                            DateTime slotDateTime = GenericHelper.UnixTimeToDateTime(unixTimestamp);

                            // Sunday=1 ... Saturday=7
                            int dayCode = ((int)slotDateTime.DayOfWeek) + 1;

                            // minute of day: 00:00 = 0, 00:15 = 15, ..., 23:45 = 1425
                            int timeSlot = (slotDateTime.Hour * 60) + slotDateTime.Minute;

                            rawResults.Add(new AreaWaitTimeRawItem
                            {
                                AREAID = currentAreaId,
                                DAYCODE = dayCode,
                                TIMESLOT = timeSlot,
                                WAITTIME = waitTime
                            });
                        }
                    }
                }

                //List<AreaWaitTimeItem> finalResults = rawResults
                //    .GroupBy(x => new { x.AREAID, x.DAYCODE, x.TIMESLOT })
                //    .Select(g => new AreaWaitTimeItem
                //    {
                //        AREAID = g.Key.AREAID,
                //        DAYCODE = g.Key.DAYCODE,
                //        TIMESLOT = g.Key.TIMESLOT,
                //        MINWAITTIME = g.Min(x => x.WAITTIME),
                //        AVGWAITTIME = Convert.ToInt32(Math.Round(g.Average(x => x.WAITTIME), MidpointRounding.AwayFromZero)),
                //        MAXWAITTIME = g.Max(x => x.WAITTIME)
                //    })
                //    .OrderBy(x => x.AREAID)
                //    .ThenBy(x => x.DAYCODE)
                //    .ThenBy(x => x.TIMESLOT)
                //    .ToList();

                var finalResults = rawResults
                    .GroupBy(x => new { x.AREAID, x.DAYCODE, x.TIMESLOT })
                    .Select(g =>
                    {
                        int minWaitSeconds = g.Min(x => x.WAITTIME);
                        int maxWaitSeconds = g.Max(x => x.WAITTIME);

                        int minWaitMinutes = GenericHelper.ConvertSecondsToRoundedMinutes(minWaitSeconds);
                        int maxWaitMinutes = GenericHelper.ConvertSecondsToRoundedMinutes(maxWaitSeconds);

                        // Rule 4
                        if (minWaitMinutes > 15)
                            minWaitMinutes = 10;

                        // Rule 5
                        if (maxWaitMinutes > 15)
                            maxWaitMinutes = 15;

                        // Rule 6
                        if (maxWaitMinutes < minWaitMinutes)
                            maxWaitMinutes = minWaitMinutes;

                        return new AreaWaitTimeItem
                        {
                            AREAID = g.Key.AREAID,
                            DAYCODE = g.Key.DAYCODE,
                            TIMESLOT = g.Key.TIMESLOT,
                            MINWAITTIME = minWaitMinutes,
                            MAXWAITTIME = maxWaitMinutes
                        };
                    })
                    .ToList();

                finalResults = finalResults
                    .OrderBy(x => x.AREAID)
                    .ThenBy(x => x.TIMESLOT)
                    .ToList();

                return finalResults;
            }
            catch (HttpRequestException e)
            {
                //Console.WriteLine("Request error: " + e.Message);
                return null;
            }
            catch (Exception e)
            {
                //Console.WriteLine("General error: " + e.Message);
                return null;
            }
        }
    }
}
