namespace ApiServer
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Grapevine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [RestResource]
    public class ApiResource
    {
        [RestRoute("Get", @"/api/mapping?.+$")]
        [RestRoute("Post", @"/api/mapping?.+$")]
        public async Task HandleMappingRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    var json = sr.ReadToEnd();
                    var MappingObject = JsonConvert.DeserializeObject<MappingValues>(json);
                    var QR = QR_Decode.AnalyzeQRList(MappingObject.QR);
                    var succeed = await SqlHandler.MappingFunction(MappingObject.EPC, QR);
                    if (succeed)
                    {
                        Logging.WriteLog("Mapping - OK");
                        await context.Response.SendResponseAsync("Mapping - OK");
                    }
                    else
                    {
                        Logging.WriteLog("Mapping - FAILED");
                        await context.Response.SendResponseAsync("Mapping - FAILED");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        // Gate Put in -> Update to MCS temp only
        [RestRoute("Get", @"/api/gate_put?.+$")]
        [RestRoute("Post", @"/api/gate_put?.+$")]
        public async Task HandleGatePutRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                string EPC = context.Request.QueryString["EPC"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(EPC))
                {
                    var result = await SqlHandler.GatePutIn(EPC);
                    if (result.Item1)
                    {
                        Logging.WriteLog("Put - OK");
                        await context.Response.SendResponseAsync("Put - OK");
                    }
                    else
                    {
                        Logging.WriteLog($"Put - FAILED - {result.Item2}");
                        await context.Response.SendResponseAsync($"Put - FAILED - {result.Item2}");
                    }
                }
                else
                {
                    Logging.WriteLog("URL parameter 'EPC' not found");
                    await context.Response.SendResponseAsync("URL parameter 'EPC' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        // Handheld Put in -> Update to MCS
        [RestRoute("Get", @"/api/put?.+$")]
        [RestRoute("Post", @"/api/put?.+$")]
        public async Task HandlePutRequest(IHttpContext context)
        {
            string LOC = string.Empty;
            string EPC = string.Empty;
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                foreach (string k in context.Request.QueryString)
                {
                    Logging.WriteLog($"{k}: {context.Request.QueryString[k]}");
                    switch (k)
                    {
                        case "LOC":
                            LOC = context.Request.QueryString[k];
                            break;
                        case "EPC":
                            EPC = context.Request.QueryString[k];
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(LOC) && !string.IsNullOrEmpty(EPC))
                {
                    var result = await SqlHandler.HandheldPutIn(LOC, EPC);
                    if (result.Item1)
                    {
                        Logging.WriteLog("Put - OK");
                        await context.Response.SendResponseAsync("Put - OK");
                    }
                    else
                    {
                        Logging.WriteLog($"Put - FAILED - {result.Item2}");
                        await context.Response.SendResponseAsync($"Put - FAILED - {result.Item2}");
                    }
                }
                else
                {
                    Logging.WriteLog("URL parameter 'LOC' and 'EPC' not found");
                    await context.Response.SendResponseAsync("URL parameter 'LOC' and 'EPC' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/move?.+$")]
        [RestRoute("Post", @"/api/move?.+$")]
        public async Task HandleMoveRequest(IHttpContext context)
        {
            string LOC = string.Empty;
            string EPC = string.Empty;
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                foreach (string k in context.Request.QueryString)
                {
                    Logging.WriteLog($"{k}: {context.Request.QueryString[k]}");
                    switch (k)
                    {
                        case "LOC":
                            LOC = context.Request.QueryString[k];
                            break;
                        case "EPC":
                            EPC = context.Request.QueryString[k];
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(LOC) && !string.IsNullOrEmpty(EPC))
                {
                    var result = await SqlHandler.HandheldMovePallet(LOC, EPC);
                    if (result.Item1)
                    {
                        Logging.WriteLog("Move - OK");
                        await context.Response.SendResponseAsync("Move - OK");
                    }
                    else
                    {
                        Logging.WriteLog($"Move - FAILED - {result.Item2}");
                        await context.Response.SendResponseAsync($"Move - FAILED - {result.Item2}");
                    }
                }
                else
                {
                    Logging.WriteLog("URL parameter 'LOC' and 'EPC' not found");
                    await context.Response.SendResponseAsync("URL parameter 'LOC' and 'EPC' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/getQR?.+$")]
        [RestRoute("Post", @"/api/getQR?.+$")]
        public async Task HandleGetQRRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                string QR = context.Request.QueryString["QR"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(QR))
                {
                    var qr_decode = QR_Decode.AnalyzeQRString(QR);
                    var jsonBody = JObject.FromObject(new
                    {
                        MaterialCode = $"{qr_decode.MaterialCode}",
                        Description = $"{qr_decode.Description}",
                        LotNumber = $"{qr_decode.LotNumber}",
                        Quantity = $"{qr_decode.Quantity}",
                        Box = $"{qr_decode.Box}"
                    });
                    Logging.WriteLog("Get QR - OK");
                    await context.Response.SendResponseAsync(jsonBody.ToString());
                }
                else
                {
                    Logging.WriteLog("URL parameter 'QR' not found");
                    await context.Response.SendResponseAsync("URL parameter 'QR' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/getRFID?.+$")]
        [RestRoute("Post", @"/api/getRFID?.+$")]
        public async Task HandleGetRFIDRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                string EPC = context.Request.QueryString["EPC"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(EPC))
                {
                    var result = await SqlHandler.GetRFIDInformationFunction(EPC);
                    if (result.Rows.Count > 0)
                    {
                        Logging.WriteLog("Get RFID - OK");
                    }
                    else
                    {
                        result = RfidFunction.RfidNotFound();
                        Logging.WriteLog("Get RFID - NOT FOUND");
                    }
                    var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
                    await context.Response.SendResponseAsync(jsonResult);
                }
                else
                {
                    Logging.WriteLog("URL parameter 'EPC' not found");
                    await context.Response.SendResponseAsync("URL parameter 'EPC' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/getPallet?.+$")]
        [RestRoute("Post", @"/api/getPallet?.+$")]
        public async Task HandleGetPalletRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                string EPC = context.Request.QueryString["EPC"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(EPC))
                {
                    var result = await SqlHandler.GetPalletInformationFunction(EPC);
                    if (result.Rows.Count > 0)
                    {
                        Logging.WriteLog("Get Pallet - OK");
                    }
                    else
                    {
                        result = PalletFunction.PalletNotFound();
                        Logging.WriteLog("Get Pallet - NOT FOUND");
                    }
                    var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
                    await context.Response.SendResponseAsync(jsonResult);
                }
                else
                {
                    Logging.WriteLog("URL parameter 'EPC' not found");
                    await context.Response.SendResponseAsync("URL parameter 'EPC' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/PalletQrMove?.+$")]
        [RestRoute("Post", @"/api/PalletQrMove?.+$")]
        public async Task HandlePalletQrMoveRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    var json = sr.ReadToEnd();
                    var MovingObject = JsonConvert.DeserializeObject<MovingPalletQrValues>(json);
                    var QR = QR_Decode.AnalyzeQRList(MovingObject.QR);
                    var succeed = await SqlHandler.MovingPalletQrFunction(MovingObject.OriEPC, MovingObject.NewEPC, QR);
                    if (succeed)
                    {
                        Logging.WriteLog("Moving Pallet QR - OK");
                        await context.Response.SendResponseAsync("Moving Pallet QR - OK");
                    }
                    else
                    {
                        Logging.WriteLog("Moving Pallet QR - FAILED");
                        await context.Response.SendResponseAsync("Moving Pallet QR - FAILED");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/tracking?.+$")]
        [RestRoute("Post", @"/api/tracking?.+$")]
        public async Task HandleTrackingRequest(IHttpContext context)
        {
            int mode = -1;
            string input = string.Empty;
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                foreach (string k in context.Request.QueryString)
                {
                    Logging.WriteLog($"{k}: {context.Request.QueryString[k]}");
                    switch (k)
                    {
                        case "input":
                            input = context.Request.QueryString[k];
                            break;
                        case "mode":
                            mode = int.Parse(context.Request.QueryString[k]);
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(input) && mode != -1)
                {
                    var result = await SqlHandler.GetTrackingInformation(mode, input);
                    if (result.Rows.Count > 0)
                    {
                        Logging.WriteLog("Tracking - OK");
                    }
                    else
                    {
                        result = TrackingFunction.TrackingNotFound();
                        Logging.WriteLog("Tracking - NOT FOUND");
                    }
                    var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
                    await context.Response.SendResponseAsync(jsonResult);
                }
                else
                {
                    Logging.WriteLog("URL parameter 'input' not found or wrong mode value");
                    await context.Response.SendResponseAsync("URL parameter 'input' not found or wrong mode value");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/checkLocation?.+$")]
        [RestRoute("Post", @"/api/checkLocation?.+$")]
        public async Task HandleCheckLocationRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                string Location = context.Request.QueryString["LOC"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(Location))
                {
                    var result = await SqlHandler.CheckLocationStatus(Location);
                    Logging.WriteLog($"Check Location result - {result}");
                    await context.Response.SendResponseAsync(result);
                }
                else
                {
                    Logging.WriteLog("URL parameter 'LOC' not found");
                    await context.Response.SendResponseAsync("URL parameter 'LOC' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/getPickingList?.+$")]
        [RestRoute("Post", @"/api/getPickingList?.+$")]
        public async Task HandleGetPickingList(IHttpContext context)
        {
            string stringFrom = string.Empty;
            string stringTo = string.Empty;
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");

            try
            {
                foreach (string k in context.Request.QueryString)
                {
                    Logging.WriteLog($"{k}: {context.Request.QueryString[k]}");
                    switch (k)
                    {
                        case "from":
                            stringFrom = context.Request.QueryString[k];
                            break;
                        case "to":
                            stringTo = context.Request.QueryString[k];
                            break;
                    }
                }

                if (!string.IsNullOrEmpty(stringTo) && !string.IsNullOrEmpty(stringFrom))
                {
                    var dateFrom = DateTime.ParseExact(stringFrom, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    var dateTo = DateTime.ParseExact(stringTo, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                    var result = await SqlHandler.GetPickingList(dateFrom, dateTo);
                    if (result.Rows.Count > 0)
                    {
                        Logging.WriteLog("Get Picking List - OK");
                    }
                    else
                    {
                        result = PickingFunction.PickingListNotFound();
                        Logging.WriteLog("Get Picking List - Not found");
                    }

                    var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
                    await context.Response.SendResponseAsync(jsonResult);
                }
                else
                {
                    Logging.WriteLog("URL parameters 'DateFrom'/'DateTo' not found");
                    await context.Response.SendResponseAsync("URL parameters 'DateFrom'/'DateTo' not found");
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", @"/api/deleteMapping?.+$")]
        [RestRoute("Post", @"/api/deleteMapping?.+$")]
        public async Task HandleDeleteMapping(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            Logging.WriteLog($"Method: {context.Request.HttpMethod}");
            try
            {
                string EPC = context.Request.QueryString["EPC"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(EPC))
                {
                    var remove = await SqlHandler.DeleteMapping(EPC);
                    if (remove)
                    {
                        Logging.WriteLog($"Delete mapping: {EPC} - OK");
                        await context.Response.SendResponseAsync("Delete - OK");
                    }
                    else
                    {
                        Logging.WriteLog($"Delete mapping: {EPC} - Failed: {EPC} NOT FOUND");
                        await context.Response.SendResponseAsync($"Delete - NOT FOUND");
                    }
                }
            }
            catch (Exception e)
            {
                Logging.WriteLog($"ERROR: {e.Message}\r\n{e.StackTrace}");
                await context.Response.SendResponseAsync(e.Message + "\n" + e.StackTrace);
            }
        }

        [RestRoute("Get", "/api/handshake")]
        public async Task HandleGetGreetRequest(IHttpContext context)
        {
            Logging.WriteLog($"URL: {context.Request.RawUrl}");
            await context.Response.SendResponseAsync("Welcome! RestAPI Server is running!");
        }

        [RestRoute]
        public async Task HandleAllGetRequests(IHttpContext context)
        {
            Logging.WriteLog("ROOT NODE");
            await context.Response.SendResponseAsync("ROOT NODE");
        }
    }
}
