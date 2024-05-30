namespace ApiServer
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading.Tasks;

    class SqlHandler
    {
        // MCS
        public static string MCSConnectionString = ConfigurationManager.ConnectionStrings["MCS"].ConnectionString;
        public static string MCS_SAVE = ConfigurationManager.AppSettings["MCS_SAVE"];
        public static string MCS_UPDATE_ALL = ConfigurationManager.AppSettings["MCS_UPDATE_ALL"];
        public static string MCS_MOVE = ConfigurationManager.AppSettings["MCS_MOVE"];
        public static string MCS_PickingList = ConfigurationManager.AppSettings["MCS_PickingList"];

        // RFID
        public static string ConnectionString = ConfigurationManager.ConnectionStrings["NIDEC"].ConnectionString;
        public static string SP_QR_TO_RFID = ConfigurationManager.AppSettings["SP_QR_TO_RFID"];
        public static string SP_QR_TO_RFID_DELETE = ConfigurationManager.AppSettings["SP_QR_TO_RFID_DELETE"];
        public static string SP_PUT_IN = ConfigurationManager.AppSettings["SP_PUT_IN"];
        public static string SP_PUT_IN_TEMP = ConfigurationManager.AppSettings["SP_PUT_IN_TEMP"];
        public static string SP_PUT_IN_CONFIRM = ConfigurationManager.AppSettings["SP_PUT_IN_CONFIRM"];
        public static string SP_PUT_IN_CONFIRM_TEMP = ConfigurationManager.AppSettings["SP_PUT_IN_CONFIRM_TEMP"];
        public static string SP_CHECK_RFID = ConfigurationManager.AppSettings["SP_CHECK_RFID"];
        public static string SP_CHECK_PALLET = ConfigurationManager.AppSettings["SP_CHECK_PALLET"];
        public static string SP_MOVE_PALLET_QR = ConfigurationManager.AppSettings["SP_MOVE_PALLET_QR"];
        public static string SP_TRACKING = ConfigurationManager.AppSettings["SP_TRACKING"];
        public static string SP_GET_DESC = ConfigurationManager.AppSettings["SP_GET_DESC"];
        public static string SP_CHECK_LOCATION = ConfigurationManager.AppSettings["SP_CHECK_LOCATION"];
        public static string USER = "HANDHELD";

        public static async Task<bool> MappingFunction(string EPC, List<QR_Decode> QR)
        {
            int insert_count = 0;
            try
            {
                foreach (var item in QR)
                {
                    using (SqlConnection connection = new SqlConnection(ConnectionString))
                    {
                        connection.Open();

                        using (SqlCommand cmd = new SqlCommand(SP_QR_TO_RFID, connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            //In parameters
                            cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;
                            cmd.Parameters.Add("@mCode", SqlDbType.NVarChar, 20).Value = item.MaterialCode;
                            cmd.Parameters.Add("@lotNumber", SqlDbType.NVarChar, 30).Value = item.LotNumber;
                            cmd.Parameters.Add("@quantity", SqlDbType.Int).Value = item.Quantity;
                            cmd.Parameters.Add("@box", SqlDbType.Int).Value = item.Box;
                            cmd.Parameters.Add("@user", SqlDbType.NVarChar, 50).Value = USER;

                            // Out parameters
                            cmd.Parameters.Add("@b_Success", SqlDbType.Int).Direction = ParameterDirection.Output;
                            cmd.Parameters.Add("@n_err", SqlDbType.Int).Direction = ParameterDirection.Output;
                            cmd.Parameters.Add("@c_errmsg", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                            await cmd.ExecuteNonQueryAsync();
                            int successFlag = int.Parse(cmd.Parameters["@b_Success"].Value.ToString());
                            int errorCode = int.Parse(cmd.Parameters["@n_err"].Value.ToString());
                            string errmsg = cmd.Parameters["@c_errmsg"].Value.ToString();

                            if (successFlag == 1)
                                insert_count++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            if (insert_count == QR.Count)
                return true;
            else
                return false;
        }

        public static string GetDescription(QR_Decode QR)
        {
            string temp = string.Empty;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(SP_GET_DESC, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@mCode", SqlDbType.NVarChar, 20).Value = QR.MaterialCode;
                        cmd.Parameters.Add("@lot", SqlDbType.NVarChar, 30).Value = QR.LotNumber;
                        cmd.Parameters.Add("@quantity", SqlDbType.Int).Value = QR.Quantity;
                        cmd.Parameters.Add("@box", SqlDbType.Int).Value = QR.Box;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                temp = reader["Description"].ToString();
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }
            return temp;
        }

        public static async Task<Tuple<bool, string>> HandheldPutIn(string LOC, string EPC)
        {
            var data = await GetRFIDInformationFunction(EPC);
            var result = await MCS_UpdateAll(data);
            if (result.Item1)
            {
                if (LOC.Length > 2 && LOC.Substring(0, 2) == "GR")
                {
                    result = await PutInConfirmConveyorFunction(LOC, EPC);
                }
                else
                {
                    result = await PutInConfirmFunction(LOC, EPC);
                }
                if (result.Item1)
                {
                    return Tuple.Create(true, string.Empty);
                }
            }
            return Tuple.Create(false, result.Item2);
        }

        public static async Task<Tuple<bool, string>> GatePutIn(string EPC)
        {
            var data = await GetRFIDInformationFunction(EPC);
            var result = await MCS_Save_Temp(data);
            if (result.Item1)
            {
                result = await AssignTempLocationFunction(EPC);
                if (result.Item1)
                {
                    return Tuple.Create(true, string.Empty);
                }
            }
            return Tuple.Create(false, result.Item2);
        }

        public static async Task<Tuple<bool, string>> HandheldMovePallet(string LOC, string EPC)
        {
            var data = await GetRFIDInformationFunction(EPC);
            var result = await MCS_Move(data);
            if (result.Item1)
            {
                if (LOC.Length > 2 && LOC.Substring(0, 2) == "GR")
                {
                    result = await PutInConfirmConveyorFunction(LOC, EPC);
                }
                else
                {
                    result = await PutInConfirmFunction(LOC, EPC);
                }
                if (result.Item1)
                {
                    return Tuple.Create(true, string.Empty);
                }
            }
            return Tuple.Create(false, result.Item2);
        }

        public static async Task<Tuple<bool, string>> AssignTempLocationFunction(string EPC)
        {
            int success = 0;
            string msg = string.Empty;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_PUT_IN_TEMP, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;
                        cmd.Parameters.Add("@user", SqlDbType.NVarChar, 50).Value = USER;

                        // Out parameters
                        cmd.Parameters.Add("@b_Success", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@n_err", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@c_errmsg", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                        await cmd.ExecuteNonQueryAsync();
                        success = int.Parse(cmd.Parameters["@b_Success"].Value.ToString());
                        msg = cmd.Parameters["@c_errmsg"].Value.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            if (success == 1)
                return Tuple.Create(true, string.Empty);
            return Tuple.Create(false, msg);
        }

        public static async Task<bool> AssignLocationFunction(string EPC)
        {
            int success = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_PUT_IN, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;
                        cmd.Parameters.Add("@user", SqlDbType.NVarChar, 50).Value = USER;

                        // Out parameters
                        cmd.Parameters.Add("@b_Success", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@n_err", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@c_errmsg", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                        await cmd.ExecuteNonQueryAsync();
                        success = int.Parse(cmd.Parameters["@b_Success"].Value.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            if (success == 1)
                return true;
            return false;
        }

        public static async Task<Tuple<bool, string>> PutInConfirmFunction(string LOC, string EPC)
        {
            int success = 0;
            string msg = string.Empty;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_PUT_IN_CONFIRM, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@location", SqlDbType.NVarChar, 50).Value = LOC;
                        cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;
                        cmd.Parameters.Add("@user", SqlDbType.NVarChar, 50).Value = USER;

                        // Out parameters
                        cmd.Parameters.Add("@b_Success", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@n_err", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@c_errmsg", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                        await cmd.ExecuteNonQueryAsync();
                        await cmd.ExecuteNonQueryAsync();
                        success = int.Parse(cmd.Parameters["@b_Success"].Value.ToString());
                        msg = cmd.Parameters["@c_errmsg"].Value.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            if (success == 1)
                return Tuple.Create(true,string.Empty);
            return Tuple.Create(false, msg);
        }

        public static async Task<Tuple<bool, string>> PutInConfirmConveyorFunction(string LOC, string EPC)
        {
            int success = 0;
            string msg = string.Empty;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_PUT_IN_CONFIRM_TEMP, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@location", SqlDbType.NVarChar, 50).Value = LOC;
                        cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;
                        cmd.Parameters.Add("@user", SqlDbType.NVarChar, 50).Value = USER;

                        // Out parameters
                        cmd.Parameters.Add("@b_Success", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@n_err", SqlDbType.Int).Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@c_errmsg", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                        await cmd.ExecuteNonQueryAsync();
                        success = int.Parse(cmd.Parameters["@b_Success"].Value.ToString());
                        msg = cmd.Parameters["@c_errmsg"].Value.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            if (success == 1)
                return Tuple.Create(true, string.Empty);
            return Tuple.Create(false, msg);
        }

        public static async Task<DataTable> GetRFIDInformationFunction(string EPC)
        {
            DataTable temp = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_CHECK_RFID, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;

                        // Execute
                        temp.Load(await cmd.ExecuteReaderAsync());
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            return temp;
        }

        public static async Task<string> CheckLocationStatus(string LOC)
        {
            string result = string.Empty;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_CHECK_LOCATION, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameter
                        cmd.Parameters.Add("@location", SqlDbType.NVarChar, 50).Value = LOC;

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                result = reader["Result"].ToString();
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
                result = e.Message;
            }
            return result;
        }

        public static async Task<DataTable> GetTrackingInformation(int mode, string input)
        {
            DataTable temp = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_TRACKING, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@mode", SqlDbType.Int).Value = mode;
                        cmd.Parameters.Add("@input", SqlDbType.NVarChar, 50).Value = input;

                        // Execute
                        temp.Load(await cmd.ExecuteReaderAsync());
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            return temp;
        }

        public static async Task<DataTable> GetPalletInformationFunction(string EPC)
        {
            DataTable temp = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(SP_CHECK_PALLET, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;

                        // Execute
                        temp.Load(await cmd.ExecuteReaderAsync());
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            return temp;
        }

        public static async Task<bool> MovingPalletQrFunction(string oriEPC, string newEPC, List<QR_Decode> QR)
        {
            int insert_count = 0;
            try
            {
                foreach (var item in QR)
                {
                    using (SqlConnection connection = new SqlConnection(ConnectionString))
                    {
                        connection.Open();
                        using (SqlCommand cmd = new SqlCommand(SP_MOVE_PALLET_QR, connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            //In parameters
                            cmd.Parameters.Add("@oriEpc", SqlDbType.NVarChar, 50).Value = oriEPC;
                            cmd.Parameters.Add("@newEpc", SqlDbType.NVarChar, 50).Value = newEPC;
                            cmd.Parameters.Add("@mCode", SqlDbType.NVarChar, 20).Value = item.MaterialCode;
                            cmd.Parameters.Add("@lotNumber", SqlDbType.NVarChar, 30).Value = item.LotNumber;
                            cmd.Parameters.Add("@quantity", SqlDbType.Int).Value = item.Quantity;
                            cmd.Parameters.Add("@box", SqlDbType.Int).Value = item.Box;
                            cmd.Parameters.Add("@user", SqlDbType.NVarChar, 50).Value = USER;

                            // Out parameters
                            cmd.Parameters.Add("@b_Success", SqlDbType.Int).Direction = ParameterDirection.Output;
                            cmd.Parameters.Add("@n_err", SqlDbType.Int).Direction = ParameterDirection.Output;
                            cmd.Parameters.Add("@c_errmsg", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                            await cmd.ExecuteNonQueryAsync();
                            int successFlag = int.Parse(cmd.Parameters["@b_Success"].Value.ToString());
                            int errorCode = int.Parse(cmd.Parameters["@n_err"].Value.ToString());
                            string errmsg = cmd.Parameters["@c_errmsg"].Value.ToString();

                            if (successFlag == 1)
                                insert_count++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }

            if (insert_count == QR.Count)
                return true;
            else
                return false;
        }

        public static async Task<Tuple<bool, string>> MCS_UpdateAll(DataTable data)
        {
            bool succeed = false;
            string msg = string.Empty;
            try
            {
                // IO_Save
                var save = await MCS_Save_Temp(data);
                succeed = save.Item1;
                msg = save.Item2;

                // IO_UpdateAll
                if (succeed)
                {
                    succeed = false;
                    using (SqlConnection connection = new SqlConnection(MCSConnectionString))
                    {
                        connection.Open();

                        using (SqlCommand cmd = new SqlCommand(MCS_UPDATE_ALL, connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                            {
                                while (reader.Read())
                                {
                                    int rs = int.Parse(reader["RS"].ToString());
                                    if (rs > 0)
                                        succeed = true;
                                    else
                                        msg = "MCS Update Failed";
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }
            return Tuple.Create(succeed, msg);
        }

        public static async Task<Tuple<bool, string>> MCS_Save_Temp(DataTable data)
        {
            bool succeed = false;
            string msg = string.Empty;
            try
            {
                foreach (DataRow row in data.Rows)
                {
                    using (SqlConnection connection = new SqlConnection(MCSConnectionString))
                    {
                        connection.Open();

                        // IO_Save
                        using (SqlCommand cmd = new SqlCommand(MCS_SAVE, connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            // In parameters
                            cmd.Parameters.Add("@SHELF_TO", SqlDbType.NVarChar, 10).Value = row[0].ToString();
                            cmd.Parameters.Add("@MATERIAL_CD", SqlDbType.NVarChar, 30).Value = row[1].ToString();
                            cmd.Parameters.Add("@LOT_NO", SqlDbType.NVarChar, 50).Value = row[2].ToString();
                            cmd.Parameters.Add("@BOX_NO", SqlDbType.NVarChar, 10).Value = row[4].ToString();
                            cmd.Parameters.AddWithValue("@TRANSFER_QTY", decimal.Parse(row[3].ToString()));

                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                            {
                                while (reader.Read())
                                {
                                    msg = reader["RS"].ToString();
                                    if (msg == "OK")
                                    {
                                        succeed = true;
                                    }
                                    else
                                    {
                                        succeed = false;
                                    }
                                }
                            }
                        }
                        if (!succeed)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
                msg = e.Message;
            }
            return Tuple.Create(succeed, msg);
        }

        public static async Task<Tuple<bool, string>> MCS_Move(DataTable data)
        {
            bool succeed = false;
            string msg = string.Empty;
            try
            {
                foreach (DataRow row in data.Rows)
                {
                    using (SqlConnection connection = new SqlConnection(MCSConnectionString))
                    {
                        connection.Open();

                        // IO_Save
                        using (SqlCommand cmd = new SqlCommand(MCS_MOVE, connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            // In parameters
                            cmd.Parameters.Add("@SHELF_TO", SqlDbType.NVarChar, 10).Value = row[0].ToString();
                            cmd.Parameters.Add("@MATERIAL_CD", SqlDbType.NVarChar, 30).Value = row[1].ToString();
                            cmd.Parameters.Add("@LOT_NO", SqlDbType.NVarChar, 50).Value = row[2].ToString();
                            cmd.Parameters.Add("@BOX_NO", SqlDbType.NVarChar, 10).Value = row[4].ToString();
                            cmd.Parameters.AddWithValue("@TRANSFER_QTY", decimal.Parse(row[3].ToString()));

                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                            {
                                while (reader.Read())
                                {
                                    msg = reader["RS"].ToString();
                                    if (msg == "SUCCEED")
                                    {
                                        succeed = true;
                                    }
                                    else
                                    {
                                        succeed = false;
                                    }
                                }
                            }
                        }
                    }
                    if (!succeed)
                        break;
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
                msg = e.Message;
            }
            return Tuple.Create(succeed, msg);
        }

        public static async Task<DataTable> GetPickingList(DateTime from, DateTime to)
        {
            DataTable temp = new DataTable();
            try
            {
                using (SqlConnection connection = new SqlConnection(MCSConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(MCS_PickingList, connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // In parameters
                        cmd.Parameters.AddWithValue("@STARTDATE", from);
                        cmd.Parameters.AddWithValue("@ENDDATE", to);

                        // Execute
                        temp.Load(await cmd.ExecuteReaderAsync());
                    }
                }

            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }
            return temp;
        }

        public static async Task<bool> DeleteMapping(string EPC)
        {
            bool result = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand(SP_QR_TO_RFID_DELETE, connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // In parameters
                    cmd.Parameters.Add("@epc", SqlDbType.NVarChar, 50).Value = EPC;
                    cmd.Parameters.Add("@user", SqlDbType.NVarChar, 50).Value = "HANDHELD";

                    // Out parameters
                    cmd.Parameters.Add("@b_Success", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@n_err", SqlDbType.Int).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@c_errmsg", SqlDbType.NVarChar, 250).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();
                    int successFlag = int.Parse(cmd.Parameters["@b_Success"].Value.ToString());
                    int errorCode = int.Parse(cmd.Parameters["@n_err"].Value.ToString());
                    string errmsg = cmd.Parameters["@c_errmsg"].Value.ToString();

                    if (successFlag == 1)
                        result = true;
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"SQL ERROR: {e}");
            }
            return await Task.FromResult(result);
        }
    }
}
