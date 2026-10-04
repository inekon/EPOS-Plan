using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Sperrfenster der Wärmepumpe</b> — eine Zeile von <c>Tab_Sperrfenster</c>
    /// (Schemaschritt <see cref="WaermepumpeSperrprofilSchema"/>).
    /// </summary>
    public sealed class Sperrfenster
    {
        /// <summary>Datenbank-ID der Zeile; 0 = neu.</summary>
        public int ID;

        /// <summary>Beginn [h des Tages, 0 … 24].</summary>
        public double VonH;

        /// <summary>Dauer [h, 0 … 24]; läuft über Mitternacht in den Folgetag.</summary>
        public double DauerH;

        /// <summary>Wochentage als Bitmaske, Mo = 1 … So = 64; 127 = jeden Tag.</summary>
        public int Wochentage = WaermepumpeSperrprofilSchema.ALLE_TAGE;

        /// <summary>Liefert auch der Heizstab der Wärmepumpe im Fenster nichts?</summary>
        public bool HeizstabGesperrt = true;

        /// <summary>Platz in der Liste.</summary>
        public int Reihenfolge;

        /// <summary>Gilt das Fenster am Wochentag <paramref name="wochentag"/> (Montag = 0 … Sonntag = 6)?</summary>
        public bool GiltAm(int wochentag) => (Wochentage & (1 << wochentag)) != 0;

        /// <summary>Eine flache Kopie.</summary>
        public Sperrfenster Kopie() => (Sperrfenster)MemberwiseClone();
    }

    /// <summary>
    /// <b>Das Sperrprofil einer Wärmepumpe</b> (Welle V14): die Stundenmaske über das Rechenjahr
    /// (8760 Stunden, kein Schaltjahr), in der der Verdichter — und je Fenster der Heizstab —
    /// nichts liefert. Zwei Quellen:
    /// <list type="number">
    /// <item>das <b>Altfenster</b> <c>Tab_Energieanlagen.Sperrung/Sperrzeit_von/Sperrzeit_bis</c>,
    /// genau wie bis dahin inline gerechnet: gesperrt ist jede Stunde des Tages mit
    /// <c>std &gt;= von &amp;&amp; std &lt; bis</c>, kein Übertrag über Mitternacht, jeden Tag; der
    /// Heizstab rechnet in diesem Fenster weiter (so stand es im Rechenweg);</item>
    /// <item>die Zeilen von <c>Tab_Sperrfenster</c>: eine Stunde ist gesperrt, wenn ihr Beginn im
    /// Fenster <c>[Tag·24 + Von_h, Tag·24 + Von_h + Dauer_h)</c> eines gültigen Wochentags liegt —
    /// dieselbe Regel wie beim Altfenster, nur mit Übertrag in den Folgetag; der Übertrag des
    /// 31. Dezember läuft in den 1. Januar (das Rechenjahr ist ein Kreis).</item>
    /// </list>
    /// Ohne Zeilen in <c>Tab_Sperrfenster</c> ist die Maske die des Altfensters — der Lauf rechnet
    /// wie zuvor.
    /// </summary>
    public sealed class Sperrprofil
    {
        /// <summary>Stunden des Rechenjahres.</summary>
        public const int STUNDEN = Kanalsatz.STUNDEN_JAHR;

        /// <summary>Tage des Rechenjahres.</summary>
        public const int TAGE = STUNDEN / 24;

        /// <summary>Lesen der Fenster einer Anlage, in Listenreihenfolge.</summary>
        public const string SQL_LESEN =
            "SELECT ID, Von_h, Dauer_h, Wochentage, Heizstab_gesperrt, Reihenfolge FROM " +
            WaermepumpeSperrprofilSchema.TAB + " WHERE ID_Energieanlage = ? ORDER BY Reihenfolge, ID";

        /// <summary>Verdichter gesperrt je Stunde.</summary>
        public readonly bool[] Verdichter = new bool[STUNDEN];

        /// <summary>Heizstab gesperrt je Stunde (nur aus Fenstern mit <see cref="Sperrfenster.HeizstabGesperrt"/>).</summary>
        public readonly bool[] Heizstab = new bool[STUNDEN];

        /// <summary>Zahl der Fenster aus <c>Tab_Sperrfenster</c> (das Altfenster zählt nicht).</summary>
        public int FensterAnzahl { get; private set; }

        /// <summary>Zahl der gesperrten Verdichterstunden im Jahr.</summary>
        public int GesperrteStunden { get; private set; }

        /// <summary>Ist der Verdichter in der Jahresstunde <paramref name="stunde"/> gesperrt?</summary>
        public bool Gesperrt(int stunde) => Verdichter[((stunde % STUNDEN) + STUNDEN) % STUNDEN];

        /// <summary>Ist der Heizstab in der Jahresstunde <paramref name="stunde"/> gesperrt?</summary>
        public bool HeizstabGesperrt(int stunde) => Heizstab[((stunde % STUNDEN) + STUNDEN) % STUNDEN];

        /// <summary>
        /// Baut die Maske aus Altfenster und Fensterliste.
        /// </summary>
        /// <param name="sperrung">Altfenster aktiv (<c>Tab_Energieanlagen.Sperrung</c>)?</param>
        /// <param name="von">Beginn des Altfensters [h des Tages].</param>
        /// <param name="bis">Ende des Altfensters [h des Tages], ausschließlich.</param>
        /// <param name="fenster">Die Zeilen von <c>Tab_Sperrfenster</c>; <c>null</c> = keine.</param>
        /// <param name="wochentagJan1">Wochentag des 1. Januar (Montag = 0 … Sonntag = 6).</param>
        public static Sperrprofil Bilden(bool sperrung, int von, int bis,
                                         IReadOnlyList<Sperrfenster> fenster, int wochentagJan1)
        {
            var p = new Sperrprofil();
            if (sperrung)
            {
                for (int h = 0; h < STUNDEN; h++)
                {
                    int std = h % 24;
                    if (std >= von && std < bis) p.Verdichter[h] = true;
                }
            }

            if (fenster != null)
            {
                int wt1 = ((wochentagJan1 % 7) + 7) % 7;
                foreach (Sperrfenster f in fenster)
                {
                    if (f == null || !(f.DauerH > 0)) continue;
                    p.FensterAnzahl++;
                    for (int tag = 0; tag < TAGE; tag++)
                    {
                        if (!f.GiltAm((wt1 + tag) % 7)) continue;
                        double beginn = tag * 24.0 + f.VonH;
                        double ende = beginn + Math.Min(f.DauerH, 24.0);
                        // Die erste Stunde, deren Beginn im Fenster liegt (Zahlenrand: 11,0 bleibt 11).
                        for (int h = (int)Math.Ceiling(beginn - 1e-9); h < ende - 1e-9; h++)
                        {
                            int j = h % STUNDEN;
                            p.Verdichter[j] = true;
                            if (f.HeizstabGesperrt) p.Heizstab[j] = true;
                        }
                    }
                }
            }

            int n = 0;
            for (int h = 0; h < STUNDEN; h++) if (p.Verdichter[h]) n++;
            p.GesperrteStunden = n;
            return p;
        }

        /// <summary>Die Fenster der Anlage <paramref name="idAnlage"/>; leer, wenn die Tabelle (noch) fehlt.</summary>
        public static List<Sperrfenster> Lesen(int idAnlage)
        {
            var l = new List<Sperrfenster>();
            if (idAnlage <= 0 || !DataRepository.TabelleVorhanden(WaermepumpeSperrprofilSchema.TAB)) return l;
            DataTable t = DataRepository.GetDataTable(SQL_LESEN, new DbParam("@a", idAnlage));
            if (t == null) return l;
            foreach (DataRow r in t.Rows)
            {
                l.Add(new Sperrfenster
                {
                    ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                    VonH = Convert.ToDouble(r["Von_h"], CultureInfo.InvariantCulture),
                    DauerH = Convert.ToDouble(r["Dauer_h"], CultureInfo.InvariantCulture),
                    Wochentage = Convert.ToInt32(r["Wochentage"], CultureInfo.InvariantCulture),
                    HeizstabGesperrt = Convert.ToInt32(r["Heizstab_gesperrt"], CultureInfo.InvariantCulture) != 0,
                    Reihenfolge = Convert.ToInt32(r["Reihenfolge"], CultureInfo.InvariantCulture),
                });
            }
            return l;
        }
    }
}
