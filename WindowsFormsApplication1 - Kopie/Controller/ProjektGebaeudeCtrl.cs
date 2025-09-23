using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    class ProjektGebaeudeCtrl : ProjektGebaeudeModel
    {
        OdbcCommand DBCommand;
        ProjektGebaeudeModel model;
        public int rows;

        public ProjektGebaeudeCtrl()
        {
            rows = 0;
            DBCommand = Program.DBConnection.CreateCommand();
            model = new ProjektGebaeudeModel();
        }

        ~ProjektGebaeudeCtrl()
        {
            rows = 0;
            DBCommand.Dispose();
        }

        public void ReadAll(int ID_Projekt)
        {
            DBCommand.CommandText = "select * from Abfrage_Projektgebaeude where ID_Projekt=" + ID_Projekt;
            OdbcDataReader DBReader = DBCommand.ExecuteReader();

            items = new ProjektGebaeudeModel[1000];
            rows = 0;

            while (DBReader.Read())
            {
                ProjektGebaeudeModel item = new ProjektGebaeudeModel();

                if (!DBReader.IsDBNull(0)) item.ID_Projekt = (int)DBReader.GetValue(0);
                if (!DBReader.IsDBNull(1)) item.AuswahlWohnflaeche = (double)DBReader.GetValue(1);
                if (!DBReader.IsDBNull(2)) item.Einheit = (string)DBReader.GetString(2);
                if (!DBReader.IsDBNull(3)) item.Jahresnutzungsgrad = (double)DBReader.GetValue(3);
                if (!DBReader.IsDBNull(4)) item.DezentralWarmwasser = (bool)DBReader.GetValue(4);
                if (!DBReader.IsDBNull(5)) item.Gebaeudename = DBReader.GetValue(5).ToString();
                if (!DBReader.IsDBNull(6)) item.Typ = DBReader.GetValue(6).ToString();
                if (!DBReader.IsDBNull(7)) item.Beschreibung = DBReader.GetValue(7).ToString();
                if (!DBReader.IsDBNull(8)) item.Wohnflaeche_gesamt = (double)DBReader.GetValue(8);
                if (!DBReader.IsDBNull(9)) item.Bewohner = (double)DBReader.GetValue(9);
                if (!DBReader.IsDBNull(10)) item.Flaeche_Nutzer = (double)DBReader.GetValue(10);
                if (!DBReader.IsDBNull(11)) item.Interne_Waermegewinne = (double)DBReader.GetValue(11);
                if (!DBReader.IsDBNull(12)) item.Bauweise = (double)DBReader.GetValue(12);
                if (!DBReader.IsDBNull(13)) item.Fensterflaeche_Sued = (double)DBReader.GetValue(13);
                if (!DBReader.IsDBNull(14)) item.Fensterflaeche_Ost = (double)DBReader.GetValue(14);
                if (!DBReader.IsDBNull(15)) item.Fensterflaeche_Nord = (double)DBReader.GetValue(15);
                if (!DBReader.IsDBNull(16)) item.Fensterdurchlassgrad = (double)DBReader.GetValue(16);
                if (!DBReader.IsDBNull(17)) item.Raumsolltemperatur_Nachtabsenkung = (double)DBReader.GetValue(17);
                if (!DBReader.IsDBNull(18)) item.Raumsolltemperatur_Tag = (double)DBReader.GetValue(18);
                if (!DBReader.IsDBNull(19)) item.Raumsolltemperatur_Wochenende = (double)DBReader.GetValue(19);
                if (!DBReader.IsDBNull(20)) item.Raumsolltemperatur_Ferien = (double)DBReader.GetValue(20);
                if (!DBReader.IsDBNull(21)) item.Maximaleraumtemperatur = (double)DBReader.GetValue(21);
                if (!DBReader.IsDBNull(22)) item.k_Wert_Außenwand = (double)DBReader.GetValue(22);
                if (!DBReader.IsDBNull(23)) item.k_Wert_Fenster = (double)DBReader.GetValue(23);
                if (!DBReader.IsDBNull(24)) item.k_Wert_Dachflaeche = (double)DBReader.GetValue(24);
                if (!DBReader.IsDBNull(25)) item.k_Wert_Grundflaeche = (double)DBReader.GetValue(25);
                if (!DBReader.IsDBNull(26)) item.k_Wert_Sonstiges = (double)DBReader.GetValue(26);
                if (!DBReader.IsDBNull(27)) item.Flaeche_Außenwand = (double)DBReader.GetValue(27);
                if (!DBReader.IsDBNull(28)) item.gesamte_Fensterflaeche = (double)DBReader.GetValue(28);
                if (!DBReader.IsDBNull(29)) item.Dachflaeche = (double)DBReader.GetValue(29);
                if (!DBReader.IsDBNull(30)) item.Grundflaeche = (double)DBReader.GetValue(30);
                if (!DBReader.IsDBNull(31)) item.Sonstige_Flaechen = (double)DBReader.GetValue(31);
                if (!DBReader.IsDBNull(32)) item.Wohnflaeche = (double)DBReader.GetValue(32);
                if (!DBReader.IsDBNull(33)) item.Raumhoehe = (double)DBReader.GetValue(33);
                if (!DBReader.IsDBNull(34)) item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = (double)DBReader.GetValue(34);
                if (!DBReader.IsDBNull(35)) item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = (double)DBReader.GetValue(35);
                if (!DBReader.IsDBNull(36)) item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = (double)DBReader.GetValue(36);
                if (!DBReader.IsDBNull(37)) item.Abmessung_Anschluß_Fenster_Wand = (double)DBReader.GetValue(37);
                if (!DBReader.IsDBNull(38)) item.Abmessung_Anschluß_Wand_Dach = (double)DBReader.GetValue(38);
                if (!DBReader.IsDBNull(39)) item.Abmessung_Anschluß_Außenwand_Kellerdecke = (double)DBReader.GetValue(39);
                if (!DBReader.IsDBNull(40)) item.Luftwechselrate = (double)DBReader.GetValue(40);
                if (!DBReader.IsDBNull(41)) item.Wochenende = (double)DBReader.GetValue(41);
                if (!DBReader.IsDBNull(42)) item.Ferien = (double)DBReader.GetValue(42);
                if (!DBReader.IsDBNull(43)) item.Ferienbeginn_1 = (double)DBReader.GetValue(43);
                if (!DBReader.IsDBNull(44)) item.Ferienende_1 = (double)DBReader.GetValue(44);
                if (!DBReader.IsDBNull(45)) item.Ferienbeginn_2 = (double)DBReader.GetValue(45);
                if (!DBReader.IsDBNull(46)) item.Ferienende_2 = (double)DBReader.GetValue(46);
                if (!DBReader.IsDBNull(47)) item.Ferienbeginn_3 = (double)DBReader.GetValue(47);
                if (!DBReader.IsDBNull(48)) item.Ferienende_3 = (double)DBReader.GetValue(48);
                if (!DBReader.IsDBNull(49)) item.Ferienbeginn_4 = (double)DBReader.GetValue(49);
                if (!DBReader.IsDBNull(50)) item.Ferienende_4 = (double)DBReader.GetValue(50);
                if (!DBReader.IsDBNull(51)) item.WW_Bedarf = (double)DBReader.GetValue(51);
                if (!DBReader.IsDBNull(52)) item.spez_Waermeverbrauch = (double)DBReader.GetValue(52);
                if (!DBReader.IsDBNull(53)) item.Waermebedarf = (double)DBReader.GetValue(53);
                if (!DBReader.IsDBNull(54)) item.Baualtersklasse = DBReader.GetValue(54).ToString();
                if (!DBReader.IsDBNull(55)) item.Gebaeudeart = DBReader.GetValue(55).ToString();
                if (!DBReader.IsDBNull(56)) item.Wohngebaeude_Nicht_Wohngebaeude = DBReader.GetValue(56).ToString();

                items[rows] = item;
                rows += 1;
                item = null;
            }
            DBReader.Dispose();
            DBReader.Close();
        }

    }
}
