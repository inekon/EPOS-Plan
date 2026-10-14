using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Weiche der Solarthermie: Kollektorfeld oder Ganglinie</b> (Folgeauftrag 4,
    /// Entscheid ST8 Weg a). Ein Projekt rechnet seine Solarthermie entweder über das
    /// Kollektorfeld (Klimadaten, Kollektorkennwerte, Ausrichtung — die Vorgabe) oder über
    /// die zugeordnete Solarthermieganglinie (8 760 Stundenwerte).
    ///
    /// <para><b>Die Weiche hat keinen eigenen Persistenzort.</b> Die Auswahlknöpfe
    /// „Profil"/„Ganglinie" der Startseiten-Kachel wählen nur, welcher Dialog aufgeht; ihre
    /// Stellung wird nirgends gespeichert. Maßgeblich ist deshalb der Datenstand:
    /// <b>Die Weiche steht auf Ganglinie genau dann, wenn dem Projekt über
    /// <c>Z_ProjektSolarganglinie</c> eine Ganglinie zugeordnet ist, deren Projektkopie
    /// vollständig ist</b> — genau 8 760 Werte, jeder endlich und nicht negativ. Sonst steht
    /// sie auf Kollektorfeld. Ein neuer Schemaschritt ist dafür nicht nötig.</para>
    ///
    /// <para><b>Einheit:</b> Ein Wert ist die Wärmeleistung der Stunde in kW, also die
    /// Wärmemenge der Stunde in kWh — absolut, ohne Bezug auf eine Fläche.</para>
    ///
    /// <para><b>Unvollständige Ganglinie:</b> Ist eine Ganglinie zugeordnet, aber nicht
    /// vollständig, rechnet der Lauf mit dem Kollektorfeld und meldet den Mangel als
    /// Warnung (<see cref="Stand.Mangel"/>) — nie still.</para>
    ///
    /// <para><b>Mehrere Zuordnungen:</b> Es rechnet die Zuordnung mit der kleinsten
    /// <c>Z_ProjektSolarganglinie.ID</c>; weitere werden gezählt und gemeldet.</para>
    /// </summary>
    public static class SolarganglinieWeiche
    {
        /// <summary>Zahl der Stundenwerte einer vollständigen Ganglinie (festes Raster ohne Schaltjahr).</summary>
        public const int STUNDEN = 8760;

        /// <summary>Was das Projekt an Solarthermieganglinie führt und ob sie rechnen kann.</summary>
        public sealed class Stand
        {
            /// <summary>true, wenn dem Projekt mindestens eine Ganglinie zugeordnet ist.</summary>
            public bool Zugeordnet;

            /// <summary><c>Tab_Solarganglinie.ID</c> der rechnenden Zuordnung; 0 ohne Zuordnung.</summary>
            public int IdGanglinie;

            /// <summary>Bezeichner der Ganglinie (Anzeigename); "" ohne Zuordnung.</summary>
            public string Bezeichner = "";

            /// <summary>Die Stundenwerte [kW = kWh je Stunde]; nur bei <see cref="Vollstaendig"/> gesetzt.</summary>
            public double[] Werte;

            /// <summary>Gelesene Zahl der Werte (auch bei Mangel).</summary>
            public int Anzahl;

            /// <summary>Benannter Mangel einer zugeordneten Ganglinie; "" ohne Mangel.</summary>
            public string Mangel = "";

            /// <summary>Zahl der Zuordnungen über die rechnende hinaus.</summary>
            public int WeitereZuordnungen;

            /// <summary>true, wenn die Ganglinie zugeordnet und vollständig ist.</summary>
            public bool Vollstaendig => Zugeordnet && Werte != null && string.IsNullOrEmpty(Mangel);

            /// <summary>Die Stellung der Weiche: true = Ganglinie rechnet, false = Kollektorfeld.</summary>
            public bool RechnetGanglinie => Vollstaendig;

            /// <summary>Jahressumme der Ganglinie [kWh]; 0 ohne vollständige Ganglinie.</summary>
            public double SummeKwh
            {
                get
                {
                    if (!Vollstaendig) return 0;
                    double s = 0;
                    for (int i = 0; i < Werte.Length; i++) s += Werte[i];
                    return s;
                }
            }
        }

        /// <summary>Kein Projekt, keine Zuordnung — der Stand der Weiche „Kollektorfeld".</summary>
        public static Stand Keine() => new Stand();

        /// <summary>
        /// Prüft eine gelesene Wertefolge ohne Datenbank. <c>null</c> in
        /// <paramref name="werte"/> steht für ein leeres Datenbankfeld.
        /// </summary>
        public static Stand Pruefen(int idGanglinie, string bezeichner, IList<double?> werte)
        {
            Stand s = new Stand
            {
                Zugeordnet = true,
                IdGanglinie = idGanglinie,
                Bezeichner = bezeichner ?? "",
                Anzahl = werte == null ? 0 : werte.Count
            };

            if (s.Anzahl != STUNDEN)
            {
                s.Mangel = string.Format(CultureInfo.InvariantCulture,
                    "sie hat {0} statt {1} Stundenwerte", s.Anzahl, STUNDEN);
                return s;
            }

            double[] feld = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
            {
                double? w = werte[i];
                if (!w.HasValue)
                {
                    s.Mangel = string.Format(CultureInfo.InvariantCulture,
                        "Stunde {0} ist leer", i + 1);
                    return s;
                }
                if (double.IsNaN(w.Value) || double.IsInfinity(w.Value))
                {
                    s.Mangel = string.Format(CultureInfo.InvariantCulture,
                        "Stunde {0} ist keine endliche Zahl", i + 1);
                    return s;
                }
                if (w.Value < 0)
                {
                    s.Mangel = string.Format(CultureInfo.InvariantCulture,
                        "Stunde {0} ist negativ ({1})", i + 1, w.Value);
                    return s;
                }
                feld[i] = w.Value;
            }

            s.Werte = feld;
            return s;
        }

        /// <summary>
        /// Liest die Weiche eines Projekts dialogfrei (<see cref="StilleDb"/>): die
        /// Zuordnung mit der kleinsten ID, ihre Projektkopie in <c>Tab_Solarganglinie</c>
        /// und deren Werte in Einfügereihenfolge (<c>Tab_SolarganglinieDaten.ID</c>).
        /// Ein Lesefehler ergibt „keine Zuordnung" — die Vorgabe Kollektorfeld.
        /// </summary>
        public static Stand Lesen(int idProjekt)
        {
            if (idProjekt <= 0) return Keine();

            DataTable zuordnung = StilleDb.Tabelle(
                "SELECT z.ID, z.ID_Ganglinie, g.Bezeichner FROM Z_ProjektSolarganglinie z " +
                "LEFT JOIN Tab_Solarganglinie g ON g.ID = z.ID_Ganglinie " +
                "WHERE z.ID_Projekt = ? ORDER BY z.ID",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            if (zuordnung == null || zuordnung.Rows.Count == 0) return Keine();

            DataRow erste = zuordnung.Rows[0];
            int idGanglinie = StilleDb.Zahl(StilleDb.Feld(erste, "ID_Ganglinie"));
            string bezeichner = StilleDb.Text(StilleDb.Feld(erste, "Bezeichner"));

            List<double?> werte = new List<double?>();
            DataTable daten = StilleDb.Tabelle(
                "SELECT Wert FROM Tab_SolarganglinieDaten WHERE ID_Ganglinie = ? ORDER BY ID",
                StilleDb.Par("@g", DbParamTyp.Integer, idGanglinie));

            if (daten != null)
                foreach (DataRow r in daten.Rows)
                {
                    object o = StilleDb.Feld(r, "Wert");
                    werte.Add(o == null || o == DBNull.Value
                                  ? (double?)null
                                  : Convert.ToDouble(o, CultureInfo.InvariantCulture));
                }

            Stand s = Pruefen(idGanglinie, bezeichner, werte);
            s.WeitereZuordnungen = zuordnung.Rows.Count - 1;
            return s;
        }
    }
}
