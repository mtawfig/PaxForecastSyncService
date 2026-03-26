using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XovisPaxForecastFeedWinSvc
{
    public static class OracleDBHelper
    {
        public static DataTable GetAreasMaster()
        {
            DataTable dt = new DataTable();

            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (OracleConnection conn = new OracleConnection(connStr))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "PKG_AREADATA.GET_AREAS_MASTER";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Output cursor
                    OracleParameter pCursor = new OracleParameter();
                    pCursor.ParameterName = "c_result";
                    pCursor.OracleType = OracleType.Cursor;
                    pCursor.Direction = ParameterDirection.Output;

                    cmd.Parameters.Add(pCursor);

                    conn.Open();

                    using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }

        public static DataTable GetAreasMapping(int? areaId)
        {
            DataTable dt = new DataTable();

            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (OracleConnection conn = new OracleConnection(connStr))
            {
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "PKG_AREADATA.GET_AREAS_MAPPING";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Input parameter
                    OracleParameter pAreaId = new OracleParameter();
                    pAreaId.ParameterName = "p_areaid";
                    pAreaId.OracleType = OracleType.Number;
                    pAreaId.Direction = ParameterDirection.Input;
                    // Add this using directive at the top of your file
                    if (areaId.HasValue && areaId > 0)
                        pAreaId.Value = areaId.Value;
                    else
                        pAreaId.Value = DBNull.Value;

                    cmd.Parameters.Add(pAreaId);

                    // Output cursor
                    OracleParameter pCursor = new OracleParameter();
                    pCursor.ParameterName = "c_result";
                    pCursor.OracleType = OracleType.Cursor;
                    pCursor.Direction = ParameterDirection.Output;

                    cmd.Parameters.Add(pCursor);

                    conn.Open();

                    using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }

        public static InsertWaitTimeResult InsertWaitTimeData(AreaWaitTimeItem item)
        {
            InsertWaitTimeResult result = null;

            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (OracleConnection conn = new OracleConnection(connStr))
            {
                conn.Open();

                result = new InsertWaitTimeResult();
                using (OracleCommand cmd = new OracleCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "PKG_AREADATA.INSERT_WAITTIME_DATA";
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Input parameter
                    OracleParameter pAreaId = new OracleParameter();
                    pAreaId.ParameterName = "p_areaid";
                    pAreaId.OracleType = OracleType.Number;
                    pAreaId.Direction = ParameterDirection.Input;
                    pAreaId.Value = item.AREAID;
                    cmd.Parameters.Add(pAreaId);

                    OracleParameter pDayCode = new OracleParameter();
                    pDayCode.ParameterName = "p_daycode";
                    pDayCode.OracleType = OracleType.Number;
                    pDayCode.Direction = ParameterDirection.Input;
                    pDayCode.Value = item.DAYCODE;
                    cmd.Parameters.Add(pDayCode);

                    OracleParameter pTimeSlot = new OracleParameter();
                    pTimeSlot.ParameterName = "p_timeslot";
                    pTimeSlot.OracleType = OracleType.Number;
                    pTimeSlot.Direction = ParameterDirection.Input;
                    pTimeSlot.Value = item.TIMESLOT;
                    cmd.Parameters.Add(pTimeSlot);

                    OracleParameter pMinTime = new OracleParameter();
                    pMinTime.ParameterName = "p_mintime";
                    pMinTime.OracleType = OracleType.Number;
                    pMinTime.Direction = ParameterDirection.Input;
                    pMinTime.Value = item.MINWAITTIME;
                    cmd.Parameters.Add(pMinTime);

                    OracleParameter pMaxTime = new OracleParameter();
                    pMaxTime.ParameterName = "p_maxtime";
                    pMaxTime.OracleType = OracleType.Number;
                    pMaxTime.Direction = ParameterDirection.Input;
                    pMaxTime.Value = item.MAXWAITTIME;
                    cmd.Parameters.Add(pMaxTime);

                    // Output parameters o_rowsaffected, o_message, o_exception
                    //OracleParameter oRowsAffected = new OracleParameter();
                    //oRowsAffected.ParameterName = "o_rowsaffected";
                    //oRowsAffected.OracleType = OracleType.Number;
                    //oRowsAffected.Direction = ParameterDirection.Output;
                    //cmd.Parameters.Add(oRowsAffected);

                    //OracleParameter oMessage = new OracleParameter();
                    //oMessage.ParameterName = "o_message";
                    //oMessage.OracleType = OracleType.VarChar;
                    //oMessage.Size = 4000;
                    //oMessage.Direction = ParameterDirection.Output;
                    //cmd.Parameters.Add(oMessage);

                    //OracleParameter oException = new OracleParameter();
                    //oException.ParameterName = "o_exception";
                    //oException.OracleType = OracleType.VarChar;
                    //oException.Size = 4000;
                    //oException.Direction = ParameterDirection.Output;
                    //cmd.Parameters.Add(oException);

                    // Output parameters
                    OracleParameter pRowsAffected = new OracleParameter("o_rowsaffected", OracleType.Number);
                    pRowsAffected.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(pRowsAffected);

                    OracleParameter pMessage = new OracleParameter("o_message", OracleType.VarChar, 4000);
                    pMessage.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(pMessage);

                    OracleParameter pException = new OracleParameter("o_exception", OracleType.VarChar, 4000);
                    pException.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(pException);

                    cmd.ExecuteNonQuery();
                    result.AFFECTEDROWS = Convert.ToInt32(pRowsAffected.Value);
                    result.MESSAGE = pMessage?.Value?.ToString();
                    result.EXCEPTION = pException?.Value?.ToString();

                    conn.Close();
                }
            }

            return result;
        }
    }
}
