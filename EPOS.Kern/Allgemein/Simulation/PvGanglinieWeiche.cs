using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Weiche der Photovoltaik: Module (Profil) oder Ganglinie</b> (PVG, Schemaschritt 205).
    /// Ein Projekt rechnet seine Photovoltaik entweder über die Module seiner Anlagen (Klimadaten,
    /// Modulkennwerte, Ausrichtung — die Vorgabe) oder über die zugeordnete PV-Ganglinie.
    ///
    /// <para><b>Maßgeblich ist der Datenstand</b> wie bei der Solarthermie
    /// (<see cref="SolarganglinieWeiche"/>): Die Weiche steht auf Ganglinie genau dann, wenn dem
    /// Projekt über <c>Z_ProjektPvGanglinie</c> eine Ganglinie zugeordnet ist, deren Projektkopie
    /// vollständig ist — 8 760 Werte im Stundenraster oder 35 040 im Viertelstundenraster, jeder
    /// endlich und nicht negativ. Die Auswahlknöpfe „Profil"/„Ganglinie" der Kachel wählen nur den
    /// Dialog.</para>
    ///
    /// <para><b>Einheit:</b> Ein Wert ist die AC-Leistung der Anlage im Zeitschritt in kW — im
    /// Stundenraster zugleich die Energie der Stunde in kWh, im Viertelstundenraster ein Viertel
    /// davon je Wert.</para>
    ///
    /// <para><b>Unvollständige Ganglinie:</b> rechnet über die Module und meldet den Mangel
    /// (<see cref="Stand.Mangel"/>). <b>Mehrere Zuordnungen:</b> Es rechnet die mit der kleinsten
    /// <c>Z_ProjektPvGanglinie.ID</c>.</para>
    /// </summary>
    public static class PvGanglinieWeiche
    {
        /// <summary>Stunden eines Jahres (kein Schaltjahr).</summary>
        public const int STUNDEN = 8760;

        /// <summary>Viertelstunden eines Jahres.</summary>
        public const int VIERTELSTUNDEN = STUNDEN * 4;

        /// <summary>Das Stundenraster in Minuten.</summary>
        public const int RASTER_STUNDE = 60;

        /// <summary>Das Viertelstundenraster in Minuten.</summary>
        public const int RASTER_VIERTEL = 15;

        /// <summary>Was das Projekt an PV-Ganglinie führt und ob sie rechnen kann.</summary>
        public sealed class Stand
        {
            /// <summary>true, wenn dem Projekt mindestens eine Ganglinie zugeordnet ist.</summary>
            public bool Zugeordnet;

            /// <summary><c>Tab_PvGanglinie.ID</c> der rechnenden Zuordnung; 0 ohne Zuordnung.</summary>
            public int IdGanglinie;

            /// <summary>Bezeichner der Ganglinie; "" ohne Zuordnung.</summary>
            public string Bezeichner = "";

            /// <summary>Das Raster der Ganglinie in Minuten (60 oder 15).</summary>
            public int RasterMinuten = RASTER_STUNDE;

            /// <summary>Die gepflegte Nennleistung [kWp]; <c>null</c> = nicht gepflegt.</summary>
            public double? NennleistungKwp;

            /// <summary>Die Werte im Raster der Ganglinie [kW]; nur bei <see cref="Vollstaendig"/> gesetzt.</summary>
            public double[] Werte;

            /// <summary>Gelesene Zahl der Werte (auch bei Mangel).</summary>
            public int Anzahl;

            /// <summary>Benannter Mangel einer zugeordneten Ganglinie; "" ohne Mangel.</summary>
            public string Mangel = "";

            /// <summary>Zahl der Zuordnungen über die rechnende hinaus.</summary>
            public int WeitereZuordnungen;

            /// <summary>true, wenn die Ganglinie zugeordnet und vollständig ist.</summary>
            public bool Vollstaendig => Zugeordnet && Werte != null && string.IsNullOrEmpty(Mangel);

            /// <summary>Die Stellung der Weiche: true = Ganglinie rechnet, false = Module.</summary>
            public bool RechnetGanglinie => Vollstaendig;

            /// <summary>true, wenn die Ganglinie im Viertelstundenraster vorliegt.</summary>
            public bool Viertelstunden => RasterMinuten == RASTER_VIERTEL;

            /// <summary>Die Dauer eines Zeitschritts [h].</summary>
            public double SchrittStunden => RasterMinuten / 60.0;

            /// <summary>Jahressumme der Ganglinie [kWh]; 0 ohne vollständige Ganglinie.</summary>
            public double SummeKwh
            {
                get
                {
                    if (!Vollstaendig) return 0;
                    double s = 0;
                    for (int i = 0; i < Werte.Length; i++) s += Werte[i];
                    return s * SchrittStunden;
                }
            }

            /// <summary>Der Höchstwert im Raster der Ganglinie [kW]; 0 ohne vollständige Ganglinie.</summary>
            public double SpitzeKw
            {
                get
                {
                    if (!Vollstaendig) return 0;
                    double m = 0;
                    for (int i = 0; i < Werte.Length; i++) if (Werte[i] > m) m = Werte[i];
                    return m;
                }
            }

            /// <summary>
            /// Die Kennleistung für Bericht, Wirtschaftlichkeit und Einspeisegrenze [kWp]: die gepflegte
            /// <see cref="NennleistungKwp"/>, sonst die <see cref="SpitzeKw"/> der Ganglinie.
            /// </summary>
            public double KennleistungKwp =>
                NennleistungKwp.HasValue && NennleistungKwp.Value > 0 ? NennleistungKwp.Value : SpitzeKw;

            /// <summary>
            /// Die Stundenreihe [kWh je Stunde = mittlere kW]: im Stundenraster die Werte selbst, im
            /// Viertelstundenraster das Mittel der vier Viertel. <c>null</c> ohne vollständige Ganglinie.
            /// </summary>
            public double[] Stundenwerte()
            {
                if (!Vollstaendig) return null;
                if (!Viertelstunden) return (double[])Werte.Clone();
                var h = new double[STUNDEN];
                for (int i = 0; i < STUNDEN; i++)
                    h[i] = (Werte[4 * i] + Werte[4 * i + 1] + Werte[4 * i + 2] + Werte[4 * i + 3]) / 4.0;
                return h;
            }
        }

        /// <summary>Kein Projekt, keine Zuordnung — der Stand der Weiche „Module".</summary>
        public static Stand Keine() => new Stand();

        /// <summary>Die erwartete Wertzahl eines Rasters; 0 für ein unbekanntes Raster.</summary>
        public static int Erwartet(int rasterMinuten) =>
            rasterMinuten == RASTER_STUNDE ? STUNDEN : rasterMinuten == RASTER_VIERTEL ? VIERTELSTUNDEN : 0;

        /// <summary>
        /// Prüft eine gelesene Wertefolge ohne Datenbank. <c>null</c> in <paramref name="werte"/> steht
        /// für ein leeres Datenbankfeld.
        /// </summary>
        public static Stand Pruefen(int idGanglinie, string bezeichner, int rasterMinuten, double? nennleistungKwp,
                                    IList<double?> werte)
        {
            var s = new Stand
            {
                Zugeordnet = true,
                IdGanglinie = idGanglinie,
                Bezeichner = bezeichner ?? "",
                RasterMinuten = rasterMinuten,
                NennleistungKwp = nennleistungKwp,
                Anzahl = werte == null ? 0 : werte.Count
            };
            int erwartet = Erwartet(rasterMinuten);
            if (erwartet == 0)
            {
                s.Mangel = string.Format(CultureInfo.InvariantCulture, "das Raster {0} min ist unbekannt", rasterMinuten);
                return s;
            }
            if (s.Anzahl != erwartet)
            {
                s.Mangel = string.Format(CultureInfo.InvariantCulture,
                    "sie hat {0} statt {1} Werte", s.Anzahl, erwartet);
                return s;
            }
            var feld = new double[erwartet];
            for (int i = 0; i < erwartet; i++)
            {
                double? w = werte[i];
                if (!w.HasValue)
                {
                    s.Mangel = string.Format(CultureInfo.InvariantCulture, "Wert {0} ist leer", i + 1);
                    return s;
                }
                if (double.IsNaN(w.Value) || double.IsInfinity(w.Value))
                {
                    s.Mangel = string.Format(CultureInfo.InvariantCulture, "Wert {0} ist keine endliche Zahl", i + 1);
                    return s;
                }
                if (w.Value < 0)
                {
                    s.Mangel = string.Format(CultureInfo.InvariantCulture, "Wert {0} ist negativ ({1})", i + 1, w.Value);
                    return s;
                }
                feld[i] = w.Value;
            }
            s.Werte = feld;
            return s;
        }

        /// <summary>
        /// Liest die Weiche eines Projekts dialogfrei (<see cref="StilleDb"/>): die Zuordnung mit der
        /// kleinsten ID, ihre Projektkopie in <c>Tab_PvGanglinie</c> und deren Werte in
        /// Einfügereihenfolge. Ein Lesefehler — auch eine Datenbank ohne die Tabellen — ergibt „keine
        /// Zuordnung", die Vorgabe Module.
        /// </summary>
        public static Stand Lesen(int idProjekt)
        {
            if (idProjekt <= 0) return Keine();

            DataTable zuordnung = StilleDb.Tabelle(
                "SELECT z.ID, z.ID_Ganglinie, g.Bezeichner, g.Raster_Minuten, g.Nennleistung_kWp " +
                "FROM Z_ProjektPvGanglinie z LEFT JOIN Tab_PvGanglinie g ON g.ID = z.ID_Ganglinie " +
                "WHERE z.ID_Projekt = ? ORDER BY z.ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (zuordnung == null || zuordnung.Rows.Count == 0) return Keine();

            DataRow erste = zuordnung.Rows[0];
            int idGanglinie = StilleDb.Zahl(StilleDb.Feld(erste, "ID_Ganglinie"));
            string bezeichner = StilleDb.Text(StilleDb.Feld(erste, "Bezeichner"));
            int raster = StilleDb.Zahl(StilleDb.Feld(erste, "Raster_Minuten"), RASTER_STUNDE);
            object nenn = StilleDb.Feld(erste, "Nennleistung_kWp");
            double? nennKwp = nenn == null || nenn == DBNull.Value
                ? (double?)null
                : Convert.ToDouble(nenn, CultureInfo.InvariantCulture);

            var werte = new List<double?>();
            DataTable daten = StilleDb.Tabelle(
                "SELECT Wert FROM Tab_PvGanglinieDaten WHERE ID_Ganglinie = ? ORDER BY ID",
                StilleDb.Par("@g", DbParamTyp.Integer, idGanglinie));
            if (daten != null)
                foreach (DataRow r in daten.Rows)
                {
                    object o = StilleDb.Feld(r, "Wert");
                    werte.Add(o == null || o == DBNull.Value
                                  ? (double?)null
                                  : Convert.ToDouble(o, CultureInfo.InvariantCulture));
                }

            Stand s = Pruefen(idGanglinie, bezeichner, raster, nennKwp, werte);
            s.WeitereZuordnungen = zuordnung.Rows.Count - 1;
            return s;
        }
    }
}
