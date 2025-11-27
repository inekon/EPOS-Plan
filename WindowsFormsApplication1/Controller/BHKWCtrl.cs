using MathNet.Numerics.RootFinding;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class BHKWCtrl : BHKWModel
    {
        public OdbcCommand DBCommand;
        public BHKWModel model;
        public static string[] BrennstoffartText = { "Öl", "Gas", "Biogas", "Rapsöl", "Holz/Pellet", "Sonstiges", "", "", "Flüssiggas", "", "", "Bioerdgas", "", "", "", "Strom" };
        public static string[] LeistungText = { "kleiner 20 kW", "20 bis 40 kW", "40 bis 80 kW", "80 bis 200 kW", "200 bis 500 kW", "500 bis 800 kW", "800 bis 1200 kW", "größer 1200 kW" };
        public static string[] LeistungFilterText = { "Ptherm LIKE '%'", "Ptherm<20", "Ptherm>=20 and Ptherm<40", "Ptherm>=40 and Ptherm<80", "Ptherm>=80 and Ptherm<200",
                                                      "Ptherm>=200 and Ptherm<500", "Ptherm>=500 and Ptherm<800", "Ptherm>=800 and Ptherm<1200", "Ptherm>=1200" };

        public BHKWCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
        }

        ~BHKWCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(string szFilter = "")
        {
            string sql;

            if (szFilter == "")
                sql = "select * from Tab_BHKW order by Bezeichner";
            else
                sql = "select * from Tab_BHKW where " + szFilter + " order by Bezeichner";

            DBCommand.CommandText = sql;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();
            items = new BHKWModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                BHKWModel item = new BHKWModel();

                if (!DBReader.IsDBNull(0)) item.m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) item.m_szFirma = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.m_szBeschreibung = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) item.m_Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.m_Pel = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) item.m_Brennstoff = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) item.m_Wirkungsgrad = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) item.m_Investition_KWel = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.m_Raumbedarf = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.m_Wartungskosten_kWhel = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) item.m_Nutzungsdauer = (int)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) item.m_NOx = (int)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) item.m_SO2 = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) item.m_CO = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) item.m_CO2 = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) item.m_Staub = (int)DBReader.GetValue(16);
                if (!DBReader.IsDBNull(17)) item.m_szMotortyp = (string)DBReader.GetValue(17);
                if (!DBReader.IsDBNull(18)) item.m_Grenzleistung = (double)DBReader.GetValue(18);
                if (!DBReader.IsDBNull(19)) item.m_Kosten_Modul = (double)DBReader.GetValue(19);
                if (!DBReader.IsDBNull(20)) item.m_Kosten_Montage = (double)DBReader.GetValue(20);
                if (!DBReader.IsDBNull(21)) item.m_Kosten_Lieferung = (double)DBReader.GetValue(21);
                if (!DBReader.IsDBNull(22)) item.m_Kosten_Schallschutzhaube = (double)DBReader.GetValue(22);
                if (!DBReader.IsDBNull(23)) item.m_Kosten_Abgasreinigung = (double)DBReader.GetValue(23);

                items[rows] = item;
                item = null;
                rows += 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(int ID)
        {
            DBCommand.CommandText = "select * from Tab_BHKW where ID=" + ID;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szFirma = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) m_Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Pel = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_Brennstoff = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_Wirkungsgrad = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_Investition_KWel = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_Raumbedarf = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) m_Wartungskosten_kWhel = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) m_Nutzungsdauer = (int)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) m_NOx = (int)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) m_SO2 = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) m_CO = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) m_CO2 = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) m_Staub = (int)DBReader.GetValue(16);
                if (!DBReader.IsDBNull(17)) m_szMotortyp = (string)DBReader.GetValue(17);
                if (!DBReader.IsDBNull(18)) m_Grenzleistung = (double)DBReader.GetValue(18);
                if (!DBReader.IsDBNull(19)) m_Kosten_Modul = (double)DBReader.GetValue(19);
                if (!DBReader.IsDBNull(20)) m_Kosten_Montage = (double)DBReader.GetValue(20);
                if (!DBReader.IsDBNull(21)) m_Kosten_Lieferung = (double)DBReader.GetValue(21);
                if (!DBReader.IsDBNull(22)) m_Kosten_Schallschutzhaube = (double)DBReader.GetValue(22);
                if (!DBReader.IsDBNull(23)) m_Kosten_Abgasreinigung = (double)DBReader.GetValue(23);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void ReadSingle(string szBezeichner)
        {
            DBCommand.CommandText = "select * from Tab_BHKW where Bezeichner='" + szBezeichner + "'";
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            rows = 0;

            DBReader.Read();

            if (DBReader.HasRows)
            {
                if (!DBReader.IsDBNull(0)) m_ID = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) m_szBezeichner = (string)DBReader.GetString(1);
                if (!DBReader.IsDBNull(2)) m_szFirma = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) m_szBeschreibung = (string)DBReader.GetString(3);
                if (!DBReader.IsDBNull(4)) m_Ptherm = (double)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) m_Pel = (double)DBReader.GetValue(5);
                if (!DBReader.IsDBNull(6)) m_Brennstoff = (int)DBReader.GetValue(6);
                if (!DBReader.IsDBNull(7)) m_Wirkungsgrad = (double)DBReader.GetValue(7);
                if (!DBReader.IsDBNull(8)) m_Investition_KWel = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) m_Raumbedarf = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) m_Wartungskosten_kWhel = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) m_Nutzungsdauer = (int)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) m_NOx = (int)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) m_SO2 = (int)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) m_CO = (int)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) m_CO2 = (int)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) m_Staub = (int)DBReader.GetValue(16);
                if (!DBReader.IsDBNull(17)) m_szMotortyp = (string)DBReader.GetValue(17);
                if (!DBReader.IsDBNull(18)) m_Grenzleistung = (double)DBReader.GetValue(18);
                if (!DBReader.IsDBNull(19)) m_Kosten_Modul = (double)DBReader.GetValue(19);
                if (!DBReader.IsDBNull(20)) m_Kosten_Montage = (double)DBReader.GetValue(20);
                if (!DBReader.IsDBNull(21)) m_Kosten_Lieferung = (double)DBReader.GetValue(21);
                if (!DBReader.IsDBNull(22)) m_Kosten_Schallschutzhaube = (double)DBReader.GetValue(22);
                if (!DBReader.IsDBNull(23)) m_Kosten_Abgasreinigung = (double)DBReader.GetValue(23);

                rows = 1;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szBezeichner);
            }
        }

        public bool Update()
        {
            try
            {
                string sql = "UPDATE Tab_BHKW SET Beschreibung='" + model.m_szBeschreibung + "'" +
                    ", Firma = '" + model.m_szFirma + "'" +
                    ", Motortyp = '" + model.m_szMotortyp + "'" +
                    ", Ptherm=" + model.m_Ptherm.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Brennstoff=" + model.m_Brennstoff +
                    ", Wirkungsgrad=" + model.m_Wirkungsgrad.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Investition_kwel= " + model.m_Investition_KWel.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Raumbedarf= " + model.m_Raumbedarf.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Wartungskosten_kwhel= " + model.m_Wartungskosten_kWhel.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Nutzungsdauer= " + model.m_Nutzungsdauer.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", CO2= " + model.m_CO2.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", SO2= " + model.m_SO2.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", NOx= " + model.m_NOx.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", CO= " + model.m_CO.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Staub= " + model.m_Staub.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Grenzleistung= " + model.m_Grenzleistung.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Kosten_Modul= " + model.m_Kosten_Modul.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Kosten_Montage= " + model.m_Kosten_Montage.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Kosten_Lieferung= " + model.m_Kosten_Lieferung.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Kosten_Schallschutzhaube= " + model.m_Kosten_Schallschutzhaube.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    ", Kosten_Abgasreinigung= " + model.m_Kosten_Abgasreinigung.ToString(CultureInfo.CreateSpecificCulture("en-US")) +
                    " WHERE Bezeichner='" + model.m_szBezeichner + "'";

                DBCommand.CommandText = sql;
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
            return true;

        }
    }
}
