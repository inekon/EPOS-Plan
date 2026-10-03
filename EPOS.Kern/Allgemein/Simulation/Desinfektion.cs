using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Thermische Desinfektion</b> (Welle M7, BW5; Konzept Simulationsablauf 21) — die Rechenregeln
    /// ohne Datenbank: Ereignisstunden und Zusatzbedarf.
    ///
    /// <code>
    /// Q_D = V · 1,163 kWh/(m³·K) · (ϑ_Ziel − ϑ_Soll) / 1000      [kWh je Ereignis, V in Litern]
    /// Ereignis am Tag d (0 … 364) in der Stunde s, wenn (d + 1) mod Intervall = 0
    /// </code>
    /// Bei sieben Tagen sind das 52 Ereignisse im Jahr (Tage 6, 13, …, 363).
    /// </summary>
    public static class Desinfektion
    {
        /// <summary>Volumenbezogene Wärmekapazität des Wassers [kWh/(m³·K)] = 1,163 Wh/(l·K).</summary>
        public const double WAERMEKAPAZITAET_KWH_M3K = 1.163;

        /// <summary>Speichersolltemperatur, wenn das Projekt keine führt [°C].</summary>
        public const double SOLL_VORGABE_C = 60.0;

        /// <summary>Zusatzbedarf EINES Ereignisses [kWh]; nicht negativ.</summary>
        public static double ZusatzbedarfKwh(double volumenL, double zielC, double sollC)
        {
            if (!(volumenL > 0)) return 0;
            double q = volumenL * WAERMEKAPAZITAET_KWH_M3K * (zielC - sollC) / 1000.0;
            return q > 0 ? q : 0;
        }

        /// <summary>Ist <paramref name="stunde"/> (0 … 8759) eine Desinfektionsstunde?</summary>
        public static bool IstEreignisstunde(int stunde, int intervallTage, int uhrStunde)
        {
            if (stunde < 0 || stunde >= 8760 || intervallTage < 1) return false;
            int tag = stunde / 24;
            return stunde % 24 == uhrStunde && (tag + 1) % intervallTage == 0;
        }

        /// <summary>Zahl der Ereignisse im Jahr (365 Tage) — <c>⌊365 / Intervall⌋</c>.</summary>
        public static int EreignisseImJahr(int intervallTage) => intervallTage >= 1 ? 365 / intervallTage : 0;

        /// <summary>
        /// Die Jahresmenge eines Projekts [kWh] mit aufgelöstem Volumen und Speichersolltemperatur — dieselben
        /// Regeln wie im Lauf (<c>SimulationWaermebedarf.BrauchwasserDesinfektion</c>), für Auskünfte außerhalb
        /// des Laufs (Bericht). 0 ohne Desinfektion, ohne Volumen oder ohne Übertemperatur.
        /// </summary>
        public static double JahresmengeKwh(int idProjekt, Desinfektionsvorgabe v, out double volumenL, out double sollC)
        {
            volumenL = 0;
            sollC = TwwTemperaturen.SpeicherSollC(idProjekt) ?? SOLL_VORGABE_C;
            if (v == null || !v.Aktiv) return 0;
            volumenL = v.VolumenL ?? TwwTemperaturen.BrauchwasserspeicherVolumenL(idProjekt) ?? 0;
            return EreignisseImJahr(v.IntervallWirksam) * ZusatzbedarfKwh(volumenL, v.ZielWirksamC, sollC);
        }

        /// <summary>Die Stundenreihe des Zusatzbedarfs [kWh] — 8 760 Werte, je Ereignis <paramref name="jeEreignisKwh"/>.</summary>
        public static double[] Reihe(int intervallTage, int uhrStunde, double jeEreignisKwh)
        {
            var reihe = new double[8760];
            if (!(jeEreignisKwh > 0)) return reihe;
            for (int h = 0; h < 8760; h++)
                if (IstEreignisstunde(h, intervallTage, uhrStunde)) reihe[h] = jeEreignisKwh;
            return reihe;
        }
    }

    /// <summary>
    /// <b>Temperaturen und Volumen des Brauchwassers eines Projekts</b> — gelesen, nicht gerechnet: die
    /// Zapftemperatur und die Speichersolltemperatur des Zapfprofilgenerators (<c>Tab_TwwZone</c>,
    /// <c>Tab_TwwNutzungsart_STAMM</c>, <c>Tab_TwwProjekt</c>) und das Volumen der Brauchwasserspeicher
    /// (<c>Tab_Pufferspeicher</c>). Dialogfrei und spaltentolerant; <c>null</c> heißt „führt das Projekt
    /// nicht", und der Aufrufer nennt seine Vorgabe im Protokoll.
    /// </summary>
    public static class TwwTemperaturen
    {
        /// <summary>
        /// Höchste Zapftemperatur der Zonen [°C] — nur auf dem Generatorweg; eine Zone ohne eigene Angabe
        /// trägt den Bezugswert ihrer Nutzungsart. <c>null</c> auf dem Bestandsweg oder ohne Angabe.
        /// </summary>
        public static double? ZapftemperaturC(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                if (ZapfprofilCtrl.Weg(idProjekt) != BrauchwasserWeg.Generator) return null;
                object o = StilleDb.Scalar(
                    "SELECT MAX(COALESCE(z.Zapftemperatur, n.Bezug_Zapftemperatur)) FROM Tab_TwwZone z " +
                    "LEFT JOIN Tab_TwwNutzungsart_STAMM n ON n.ID = z.ID_Nutzungsart WHERE z.ID_Projekt = ?",
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
                double w = StilleDb.Kommazahl(o, double.NaN);
                return double.IsNaN(w) || w <= 0 ? (double?)null : w;
            }
            catch (Exception) { return null; }
        }

        /// <summary>Speichersolltemperatur des Generators [°C] (<c>Tab_TwwProjekt.Speicher_C</c>); sonst <c>null</c>.</summary>
        public static double? SpeicherSollC(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                if (ZapfprofilCtrl.Weg(idProjekt) != BrauchwasserWeg.Generator) return null;
                object o = StilleDb.Scalar("SELECT Speicher_C FROM Tab_TwwProjekt WHERE ID_Projekt = ?",
                                           StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
                double w = StilleDb.Kommazahl(o, double.NaN);
                return double.IsNaN(w) || w <= 0 ? (double?)null : w;
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Volumen der Speicher des Projekts, die Brauchwasser führen [l] — Klassen-Set
        /// <c>Nutzung_Brauchwasser</c> oder Verwendung Brauchwasser/Kombi. <c>null</c> ohne einen solchen.
        /// </summary>
        public static double? BrauchwasserspeicherVolumenL(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            DataTable dt = StilleDb.Tabelle("SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?",
                                            StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            if (dt == null) return null;
            double summe = 0;
            bool gefunden = false;
            foreach (DataRow r in dt.Rows)
            {
                bool bw = StilleDb.Zahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_NUTZUNG_BRAUCHWASSER), 0) != 0;
                string verwendung = StilleDb.Text(StilleDb.Feld(r, "Verwendung"));
                if (!bw && verwendung != DbWerte.PSP_VERWENDUNG_BRAUCHWASSER && verwendung != DbWerte.PSP_VERWENDUNG_KOMBI)
                    continue;
                double v = StilleDb.Kommazahl(StilleDb.Feld(r, "Gesamtvolumen"), 0);
                if (v > 0) { summe += v; gefunden = true; }
            }
            return gefunden ? summe : (double?)null;
        }
    }

    /// <summary>
    /// <b>Deckung des Desinfektionsbedarfs im Lauf</b> (BW5; Konzept Simulationsablauf 21). Der
    /// Zusatzbedarf steht im Brauchwasserkanal, wird aber vor jeder Stufe ZURÜCKGEHALTEN, die die
    /// Zieltemperatur nicht erreicht, und vor jeder Stufe FREIGEGEBEN, die sie erreicht. Was eine Stufe
    /// danach im Kanal deckt, deckt zuerst den Zusatzbedarf. Was nach allen Stufen offen bleibt, deckt
    /// ein benannter Zusatzstrom (elektrisch, Wirkungsgrad 1).
    ///
    /// <para>Ein Pufferspeicher deckt den Zusatzbedarf nie — er ist es, der aufgeheizt wird.</para>
    /// </summary>
    public sealed class Desinfektionsdeckung
    {
        /// <summary>Zusatzbedarf je Stunde, wie er im Bedarf steht [kWh].</summary>
        public readonly double[] Bedarf;

        /// <summary>Noch offener Zusatzbedarf je Stunde [kWh] — er steht NICHT im Kanalrest.</summary>
        public readonly double[] Offen;

        /// <summary>Gedeckt je Stufenname [kWh].</summary>
        public readonly Dictionary<string, double> GedecktJeStufe = new Dictionary<string, double>();

        /// <summary>Zieltemperatur [°C].</summary>
        public readonly double ZielC;

        /// <summary>Kann die Erzeugerart (<c>ProjektPuffer.TYP_*</c>) innerhalb der Speicherstufe die Zieltemperatur erreichen?</summary>
        public readonly Dictionary<int, bool> FaehigJeArt = new Dictionary<int, bool>();

        /// <summary>Kann der Heizstab der Wärmepumpe den Zusatzbedarf decken (Modul mit Heizstab)?</summary>
        public bool HeizstabFaehig;

        /// <summary>
        /// Speicher (Puffer-ID), die den Zusatzbedarf abgeben dürfen: Sie führen Brauchwasser, ihr
        /// gepflegter Vorlauf erreicht die Zieltemperatur, und eine Anlage, die sie lädt, erreicht sie auch.
        /// Die Wärme kommt dann aus dieser Ladung; der Speicher gibt sie in der Nachentladung ab.
        /// </summary>
        public readonly HashSet<int> FaehigeSpeicher = new HashSet<int>();

        public Desinfektionsdeckung(double[] bedarf, double zielC)
        {
            Bedarf = bedarf ?? new double[8760];
            Offen = (double[])Bedarf.Clone();
            ZielC = zielC;
        }

        /// <summary>Summe des Zusatzbedarfs [kWh].</summary>
        public double BedarfKwh { get { double s = 0; foreach (double v in Bedarf) s += v; return s; } }

        /// <summary>Summe des offenen Rests [kWh] — nach dem Lauf der Zusatzstrom.</summary>
        public double OffenKwh { get { double s = 0; foreach (double v in Offen) s += v; return s; } }

        /// <summary>Nimmt den offenen Zusatzbedarf aus dem Kanal (vor dem Lauf, je Stunde).</summary>
        public void AusKanalNehmen(double[] kanalBrauchwasser)
        {
            for (int h = 0; h < Offen.Length && h < kanalBrauchwasser.Length; h++)
            {
                if (Offen[h] <= 0) continue;
                kanalBrauchwasser[h] -= Offen[h];
                if (kanalBrauchwasser[h] < 0) kanalBrauchwasser[h] = 0;
            }
        }

        /// <summary>Gibt den offenen Zusatzbedarf der Stunde frei; liefert den Kanalstand danach.</summary>
        public double Freigeben(int stunde, double[] rest)
        {
            if (Offen[stunde] > 0) rest[Kanal.BRAUCHWASSER] += Offen[stunde];
            return rest[Kanal.BRAUCHWASSER];
        }

        /// <summary>
        /// Hält nach einer fähigen Stufe zurück, was vom Zusatzbedarf noch offen ist: Die Deckung der Stufe
        /// trifft zuerst den Zusatzbedarf.
        /// </summary>
        public void Zurueckhalten(int stunde, double[] rest, double vorher, string stufe)
        {
            double gedeckt = vorher - rest[Kanal.BRAUCHWASSER];
            if (gedeckt < 0) gedeckt = 0;
            double offen = Offen[stunde];
            if (offen <= 0) return;
            double teil = gedeckt < offen ? gedeckt : offen;
            Offen[stunde] = offen - teil;
            if (teil > 0)
                GedecktJeStufe[stufe] = (GedecktJeStufe.TryGetValue(stufe, out double s) ? s : 0) + teil;
            rest[Kanal.BRAUCHWASSER] -= Offen[stunde];
            if (rest[Kanal.BRAUCHWASSER] < 0) rest[Kanal.BRAUCHWASSER] = 0;
        }

        /// <summary>Freigeben für eine ganze Vektorstufe (alle Stunden) — liefert den Kanalstand davor.</summary>
        public double[] FreigebenJahr(double[] kanalBrauchwasser)
        {
            var vorher = new double[kanalBrauchwasser.Length];
            for (int h = 0; h < kanalBrauchwasser.Length && h < Offen.Length; h++)
            {
                if (Offen[h] > 0) kanalBrauchwasser[h] += Offen[h];
                vorher[h] = kanalBrauchwasser[h];
            }
            return vorher;
        }

        /// <summary>Zurückhalten nach einer Vektorstufe (alle Stunden).</summary>
        public void ZurueckhaltenJahr(double[] kanalBrauchwasser, double[] vorher, string stufe)
        {
            var rest = new double[Kanal.ANZAHL];
            for (int h = 0; h < kanalBrauchwasser.Length && h < Offen.Length; h++)
            {
                if (Offen[h] <= 0) continue;
                rest[Kanal.BRAUCHWASSER] = kanalBrauchwasser[h];
                Zurueckhalten(h, rest, vorher[h], stufe);
                kanalBrauchwasser[h] = rest[Kanal.BRAUCHWASSER];
            }
        }
    }
}
