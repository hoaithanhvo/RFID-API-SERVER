namespace ApiServer
{
    using System.Data;
    using System.Collections.Generic;
    using System.Configuration;

    class MappingValues
    {
        public string EPC { get; set; }
        public List<QR_String> QR { get; set; }
    }

    class MovingPalletQrValues
    {
        public string OriEPC { get; set; }
        public string NewEPC { get; set; }
        public List<QR_String> QR { get; set; }
    }

    class QR_String
    {
        public string VALUE { get; set; }
    }

    class QR_Decode
    {
        public static int Material_Length = int.Parse(ConfigurationManager.AppSettings["Material_Length"]);
        public static int Lot_Length = int.Parse(ConfigurationManager.AppSettings["Lot_Length"]);
        public static int Quantity_Length = int.Parse(ConfigurationManager.AppSettings["Quantity_Length"]);
        public static int Box_Length = int.Parse(ConfigurationManager.AppSettings["Box_Length"]);

        public string MaterialCode { get; set; }
        public string Description { get; set; }
        public string LotNumber { get; set; }
        public int Quantity { get; set; }
        public int Box { get; set; }

        public static List<QR_Decode> AnalyzeQRList(List<QR_String> input)
        {
            List<QR_Decode> full = new List<QR_Decode>();
            foreach (var item in input)
            {
                QR_Decode temp = new QR_Decode
                {
                    MaterialCode = item.VALUE.Substring(0, Material_Length).Trim(),
                    Description = string.Empty,
                    LotNumber = item.VALUE.Substring(Material_Length, Lot_Length).Trim(),
                    Quantity = int.Parse(item.VALUE.Substring(Material_Length + Lot_Length, Quantity_Length).Trim()),
                    Box = int.Parse(item.VALUE.Substring(Material_Length + Lot_Length + Quantity_Length, Box_Length).Trim())
                };
                full.Add(temp);
            }
            return full;
        }

        public static QR_Decode AnalyzeQRString(string input)
        {
            QR_Decode temp = new QR_Decode
            {
                MaterialCode = input.Substring(0, Material_Length).Trim(),
                LotNumber = input.Substring(Material_Length, Lot_Length).Trim(),
                Quantity = int.Parse(input.Substring(Material_Length + Lot_Length, Quantity_Length).Trim()),
                Box = int.Parse(input.Substring(Material_Length + Lot_Length + Quantity_Length, Box_Length).Trim())
            };
            temp.Description = SqlHandler.GetDescription(temp);
            return temp;
        }

        public static bool SameMaterialCode(List<QR_Decode> input)
        {
            int check = 0;
            var first = input[0];
            foreach (var item in input)
            {
                if (item.MaterialCode.Equals(first.MaterialCode))
                    check++;
            }
            if (check == input.Count)
                return true;
            return false;
        }
    }

    class RfidFunction
    {
        public static DataTable RfidNotFound()
        {
            DataTable temp = new DataTable();
            temp.Columns.Add("Location", typeof(string));
            temp.Columns.Add("MaterialCode", typeof(string));
            temp.Columns.Add("LotNumber", typeof(string));
            temp.Columns.Add("Quantity", typeof(string));
            temp.Columns.Add("Box", typeof(string));

            var row = temp.NewRow();
            row["Location"] = "NOT FOUND";
            row["MaterialCode"] = "NOT FOUND";
            row["LotNumber"] = "NOT FOUND";
            row["Quantity"] = "NOT FOUND";
            row["Box"] = "NOT FOUND";

            temp.Rows.Add(row);
            return temp;
        }
    }

    class PalletFunction
    {
        public static DataTable PalletNotFound()
        {
            DataTable temp = new DataTable();
            temp.Columns.Add("MaterialCode", typeof(string));
            temp.Columns.Add("LotNumber", typeof(string));
            temp.Columns.Add("Quantity", typeof(string));
            temp.Columns.Add("Box", typeof(string));

            var row = temp.NewRow();
            row["MaterialCode"] = "NOT FOUND";
            row["LotNumber"] = "NOT FOUND";
            row["Quantity"] = "NOT FOUND";
            row["Box"] = "NOT FOUND";

            temp.Rows.Add(row);
            return temp;
        }
    }

    class TrackingFunction
    {
        public static DataTable TrackingNotFound()
        {
            DataTable temp = new DataTable();
            temp.Columns.Add("Location", typeof(string));
            temp.Columns.Add("EPC", typeof(string));
            temp.Columns.Add("MaterialCode", typeof(string));
            temp.Columns.Add("LotNumber", typeof(string));
            temp.Columns.Add("Quantity", typeof(string));
            temp.Columns.Add("Box", typeof(string));

            var row = temp.NewRow();
            row["Location"] = "NOT FOUND";
            row["EPC"] = "NOT FOUND";
            row["MaterialCode"] = "NOT FOUND";
            row["LotNumber"] = "NOT FOUND";
            row["Quantity"] = "NOT FOUND";
            row["Box"] = "NOT FOUND";

            temp.Rows.Add(row);
            return temp;
        }
    }

    class PickingFunction
    {
        public static DataTable PickingListNotFound()
        {
            DataTable temp = new DataTable();
            temp.Columns.Add("No", typeof(string));
            temp.Columns.Add("PickingNo", typeof(string));
            temp.Columns.Add("LocTo", typeof(string));
            temp.Columns.Add("Time", typeof(string));
            temp.Columns.Add("Status", typeof(string));

            var row = temp.NewRow();
            row["No"] = "NOT FOUND";
            row["PickingNo"] = "NOT FOUND";
            row["LocTo"] = "NOT FOUND";
            row["Time"] = "NOT FOUND";
            row["Status"] = "NOT FOUND";

            temp.Rows.Add(row);
            return temp;
        }
    }
}
