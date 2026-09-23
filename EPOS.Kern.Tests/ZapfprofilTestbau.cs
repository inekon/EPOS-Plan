using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Bausteine der Zapfprofil-Tests</b> (Stufe Z1): ein fiktiver Katalog und ein fiktiver
    /// Parametersatz mit ERFUNDENEN, runden Werten — keine Normzahl, kein Normformvektor
    /// (Umsetzungskonzept Zapfprofilgenerator Kapitel 6 (a)). Die Schlüssel sind die des
    /// Rechenwegs (<see cref="ZapfParameter"/>), die Werte stehen nur hier.
    /// </summary>
    internal static class ZapfprofilTestbau
    {
        internal const string QUELLE = "Testkatalog (fiktiv)";
        internal static readonly Provenienz Fiktiv = new Provenienz(QUELLE, null, "TEST-Z1", Herkunftsart.Fiktiv);

        /// <summary>Bezugstemperaturen des fiktiven Katalogs [°C] — erfunden.</summary>
        internal static readonly Temperaturbezug Bezug = new Temperaturbezug(50.0, 12.0);

        /// <summary>Die erfundenen Parameter; einzelne Schlüssel lassen sich weglassen oder ersetzen.</summary>
        internal static Parametersatz Parameter(IDictionary<string, double> ersetzen = null, params string[] weglassen)
        {
            var werte = new Dictionary<string, double>
            {
                [ZapfParameter.KALTWASSER_MITTEL] = 11.0,
                [ZapfParameter.KALTWASSER_AMPLITUDE] = 2.0,
                [ZapfParameter.KALTWASSER_MONAT_MAXIMUM] = 8.0,
                [ZapfParameter.WOHNEN_FORMEL_A] = 20.0,
                [ZapfParameter.WOHNEN_FORMEL_B] = 0.1,
                [ZapfParameter.WOHNEN_FORMEL_C] = 6.0,
                [ZapfParameter.WOHNEN_FLAECHE_JE_WE] = 80.0,
                [ZapfParameter.ZIRKULATION_ANTEIL] = 0.25,
                [ZapfParameter.ZIRKULATION_LAUFZEIT] = 18.0,
                [ZapfParameter.ZIRKULATION_LAGE] = 1.0,
                [ZapfParameter.ZIRKULATION_KENNWERT_LAGE1] = 5.0,
                [ZapfParameter.ZIRKULATION_KENNWERT_LAGE2] = 20.0,
                [ZapfParameter.ZIRKULATION_VERLUST_JE_METER] = 10.0,
                [ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE] = 0.5,
                [ZapfParameter.FORMVEKTOR_WARNSCHWELLE] = 0.01,
            };
            if (ersetzen != null) foreach (var kv in ersetzen) werte[kv.Key] = kv.Value;
            foreach (string w in weglassen) werte.Remove(w);
            return Parametersatz.Aus("TEST-Z1",
                werte.Select(kv => new ZapfParameterwert(kv.Key, kv.Value, "", Fiktiv)).ToArray());
        }

        /// <summary>24 Anteile: die genannten Stunden (0 … 23) mit ihrem Wert, sonst 0.</summary>
        internal static double[] Gang(params (int Stunde, double Anteil)[] werte)
        {
            var a = new double[24];
            foreach (var (s, w) in werte) a[s] = w;
            return a;
        }

        /// <summary>Ein Tagesgangsatz aus vier Tagesgängen (Werktag, Samstag, Sonn-/Feiertag, Ruhetag).</summary>
        internal static Tagesgangsatz Satz(int id, double[] werktag, double[] samstag, double[] sonntag, double[] ruhetag)
        {
            var anteile = new double[4, 24];
            double[][] g = { werktag, samstag, sonntag, ruhetag };
            for (int t = 0; t < 4; t++)
                for (int h = 0; h < 24; h++) anteile[t, h] = g[t][h];
            return new Tagesgangsatz(id, anteile, new[] { Fiktiv, Fiktiv, Fiktiv, Fiktiv })
            {
                Bezeichner = "Testsatz " + id + " (fiktiv)", Katalogversion = "TEST-Z1", Status = ZapfKatalogstatus.Eigen
            };
        }

        /// <summary>Der Standardsatz: vier erfundene Formen, je Σ 1.</summary>
        internal static Tagesgangsatz Standardsatz(int id = 1) => Satz(id,
            Gang((6, 0.25), (7, 0.25), (18, 0.25), (19, 0.25)),
            Gang((8, 0.5), (9, 0.5)),
            Gang((9, 0.25), (10, 0.25), (11, 0.25), (12, 0.25)),
            Gang((10, 0.5), (11, 0.5)));

        /// <summary>Eine erfundene Nutzungsart; ohne Angabe: Personen, 1/2/3 kWh, Grenze 1, Wohnen, flach.</summary>
        internal static Nutzungsart Art(int id = 1,
                                        ZapfBezugsart bezug = ZapfBezugsart.Personen,
                                        double[] bedarf = null,
                                        ZapfBilanzgrenze grenze = ZapfBilanzgrenze.Zapfstelle,
                                        ZapfKalenderart kalender = ZapfKalenderart.Wohnen,
                                        double? ferienfaktor = null,
                                        double[] monate = null,
                                        double[] woche = null,
                                        Tagesgangsatz satz = null,
                                        Bedarfsbandbreite bandbreite = null)
        {
            return new Nutzungsart(id, "Testnutzung " + id + " (fiktiv)", bezug,
                bedarf ?? new[] { 1.0, 2.0, 3.0 }, Bezug, grenze, kalender, ferienfaktor,
                monate ?? Enumerable.Repeat(1.0, 12).ToArray(),
                woche ?? new[] { 0.15, 0.15, 0.15, 0.15, 0.15, 0.125, 0.125 },
                satz ?? Standardsatz(),
                new Katalogherkunft(Fiktiv, bandbreite ?? new Bedarfsbandbreite(new double?[3], new double?[3]),
                                    Fiktiv, Fiktiv, new[] { Fiktiv, Fiktiv, Fiktiv, Fiktiv }))
            {
                Katalogversion = "TEST-Z1", Status = ZapfKatalogstatus.Eigen
            };
        }

        /// <summary>Eine Zone mit erfundenen Werten.</summary>
        internal static ZonenStand Zone(string name = "Zone A", int idArt = 1, double menge = 10.0, int id = 1)
            => new ZonenStand { Id = id, Name = name, IdNutzungsart = idArt, Bezugsmenge = menge, Reihenfolge = id };

        /// <summary>Projektgrößen wie die DDL-Vorgaben (automatisch, Methode Flächenkennwert).</summary>
        internal static ProjektStand Projekt() => new ProjektStand
        {
            Id = 1, Weg = BrauchwasserWeg.Generator, ZirkAuto = true,
            ZirkMethode = ZapfZirkulationsmethode.Flaechenkennwert, Speicherart = ZapfSpeicherart.Ladespeicher,
            Realisierungen = 10, Perzentil = 99, Seed = 1, LadeAuto = true
        };

        /// <summary>Die Wochenend-Kennzeichen eines Jahres, das am gegebenen Wochentag beginnt, samt Feiertagen.</summary>
        internal static bool[] We(int wochentagJan1, params int[] feiertage)
        {
            var we = new bool[365];
            for (int d = 1; d <= 365; d++)
            {
                int wt = (wochentagJan1 + d - 1) % 7;
                we[d - 1] = wt >= 5;
            }
            foreach (int f in feiertage) we[f - 1] = true;
            return we;
        }

        /// <summary>Ein Eingang mit Standardkalender (Jahr beginnt am Montag).</summary>
        internal static Zapfprofileingang Eingang(ProjektStand projekt, Parametersatz ps, params ZonenStand[] zonen)
            => new Zapfprofileingang
            {
                Zonen = zonen, Projekt = projekt, WochentagJan1 = 0, We = We(0), Parameter = ps
            };

        /// <summary>Relative Abweichung.</summary>
        internal static double Relativ(double ist, double soll)
            => soll == 0.0 ? System.Math.Abs(ist) : System.Math.Abs(ist - soll) / System.Math.Abs(soll);
    }
}
