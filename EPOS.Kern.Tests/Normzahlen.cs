using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Vorrichtung für die NICHT AUSGELIEFERTEN Normprüfdaten der Gebäudesimulation
    /// (Umsetzungskonzept 1.9, Entscheid E27 zu U8). Bauform wie <see cref="TestDatenbank"/>:
    /// Suche aufwärts vom Laufordner nach <c>Referenzlaeufe/Normzahlen/</c>, Kennzeichen
    /// <see cref="Vorhanden"/>, und jeder Normfall beginnt mit
    /// <c>if (!_n.Vorhanden) return;</c>.
    ///
    /// <para><b>Warum lokal und gitignoriert.</b> Das Ausliefern der Normzahlen in
    /// Testdateien wäre eine Vervielfältigung (Konzept N1.2). Der Ordner
    /// <c>Referenzlaeufe/Normzahlen/</c> ist deshalb in <c>.gitignore</c> bis auf sein
    /// <c>LIESMICH.md</c> ausgenommen; dort steht, welche Dateien der Anwender wohin legt.
    /// Datenquelle sind die Validierungsdaten der AixLib (RWTH Aachen, EBC, BSD-Lizenz mit
    /// Zusatzabsatz) im Unterordner <c>aixlib/</c>.</para>
    ///
    /// <para><b>Die Folge:</b> Der Nachweis der zwölf Normfälle ist ein LOKALER Nachweis.
    /// In der CI fehlt der Ordner <c>aixlib/</c>, und jeder Normfall schweigt. Was ohne die
    /// Zahlen prüfbar ist — Prüfband, Vorzeichen, dass diese Vorrichtung wirklich sucht —,
    /// halten die versionierten Gegenwächter in <see cref="GebaeudeModellNormfallTests"/>.</para>
    /// </summary>
    public sealed class Normzahlen
    {
        /// <summary>Der Ordner relativ zur Repowurzel.</summary>
        internal static readonly string[] Ordnerpfad = { "Referenzlaeufe", "Normzahlen" };

        /// <summary>Der Unterordner der AixLib-Daten.</summary>
        internal const string AixlibOrdnername = "aixlib";

        public Normzahlen()
            : this(AppContext.BaseDirectory)
        {
        }

        /// <summary>Sucht ab <paramref name="startordner"/> aufwärts — für die Gegenprobe mit eigenem Baum.</summary>
        internal Normzahlen(string startordner)
        {
            Ordner = OrdnerSuchen(startordner);
            if (Ordner == null) return;
            string aixlib = Path.Combine(Ordner, AixlibOrdnername);
            if (!Directory.Exists(aixlib)) return;
            AixlibOrdner = aixlib;
            Vorhanden = Directory.EnumerateFiles(aixlib, "TestCase*.mo", SearchOption.AllDirectories).Any();
        }

        /// <summary>Liegen die AixLib-Daten? Sonst schweigt jeder Normfall.</summary>
        public bool Vorhanden { get; }

        /// <summary>Der gefundene Ordner <c>Referenzlaeufe/Normzahlen</c>, oder <c>null</c>.</summary>
        internal string Ordner { get; }

        /// <summary>Der Ordner <c>Referenzlaeufe/Normzahlen/aixlib</c>, oder <c>null</c>.</summary>
        internal string AixlibOrdner { get; }

        /// <summary>Die Modelica-Datei des Testfalls <paramref name="nummer"/>, oder <c>null</c>.</summary>
        internal string Testfalldatei(int nummer)
        {
            if (AixlibOrdner == null) return null;
            string name = "TestCase" + nummer + ".mo";
            return Directory.EnumerateFiles(AixlibOrdner, name, SearchOption.AllDirectories).FirstOrDefault();
        }

        /// <summary>Liest den Normfall über die EINE Lesestelle <see cref="NormfallLeser"/>.</summary>
        internal Normfall Lesen(int nummer) => NormfallLeser.Lesen(this, nummer);

        /// <summary>Suche aufwärts, höchstens acht Ebenen — wie <c>TestDatenbank.Quelle()</c>.</summary>
        internal static string OrdnerSuchen(string startordner)
        {
            DirectoryInfo d = new DirectoryInfo(startordner);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(new[] { d.FullName }.Concat(Ordnerpfad).ToArray());
                if (Directory.Exists(kandidat)) return kandidat;
            }
            return null;
        }
    }

    /// <summary>Die drei Prüfgrößen der Ergebnistabellen.</summary>
    internal enum NormGroesse
    {
        /// <summary>Raumlufttemperatur [°C].</summary>
        Lufttemperatur,

        /// <summary>Operative Temperatur [°C].</summary>
        OperativeTemperatur,

        /// <summary>Heiz-/Kühllast [W], Heizen positiv.</summary>
        Last,
    }

    /// <summary>
    /// Eine Referenzreihe: eine Größe an einem Tag, 24 Blockstunden, zwei Programmspalten.
    /// Führt die Quelle nur eine Spalte (AixLib), sind beide Spalten gleich.
    /// </summary>
    internal sealed class Normreihe
    {
        internal Normreihe(NormGroesse groesse, int tag, double[] programm1, double[] programm2)
        {
            if (tag < 1) throw new ArgumentOutOfRangeException(nameof(tag));
            if (programm1 == null || programm1.Length != 24) throw new ArgumentException("24 Stundenwerte erwartet.", nameof(programm1));
            if (programm2 == null || programm2.Length != 24) throw new ArgumentException("24 Stundenwerte erwartet.", nameof(programm2));
            Groesse = groesse;
            Tag = tag;
            Programm1 = programm1;
            Programm2 = programm2;
        }

        internal NormGroesse Groesse { get; }

        /// <summary>Tag des Laufs, ab 1.</summary>
        internal int Tag { get; }

        /// <summary>Werte der ersten Programmspalte, Stunde 1…24 (Stunde n = Blockmittel (n−1):00–n:00).</summary>
        internal double[] Programm1 { get; }

        /// <summary>Werte der zweiten Programmspalte.</summary>
        internal double[] Programm2 { get; }
    }

    /// <summary>
    /// Ein gelesener Normfall: Parametersatz, Startzustand, die Randbedingungen jeder
    /// Blockstunde ab Stunde 1 und die Referenzreihen. Die Randbedingungen sind bereits auf
    /// die Knoten verteilt und tragen das Vorzeichen der Richtlinie (Last positiv = Heizen).
    /// </summary>
    internal sealed class Normfall
    {
        internal Normfall(int nummer, ErsatzparameterRC parameter, double thetaStart,
                          Stundenrand[] raender, IReadOnlyList<Normreihe> reihen)
        {
            Nummer = nummer;
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            ThetaStart = thetaStart;
            Raender = raender ?? throw new ArgumentNullException(nameof(raender));
            Reihen = reihen ?? throw new ArgumentNullException(nameof(reihen));
            foreach (Normreihe r in reihen)
                if (r.Tag * 24 > raender.Length)
                    throw new ArgumentException("Die Randbedingungen reichen nicht bis Tag " + r.Tag + ".", nameof(raender));
        }

        internal int Nummer { get; }
        internal ErsatzparameterRC Parameter { get; }
        internal double ThetaStart { get; }
        internal Stundenrand[] Raender { get; }
        internal IReadOnlyList<Normreihe> Reihen { get; }
    }

    /// <summary>Eine geprüfte Zelle: Größe, Tag, Stunde, Rechenwert und Überschreitung des Bands.</summary>
    internal readonly struct Normzelle
    {
        internal Normzelle(NormGroesse groesse, int tag, int stunde, double wert, double unten, double oben)
        {
            Groesse = groesse;
            Tag = tag;
            Stunde = stunde;
            Wert = wert;
            Unten = unten;
            Oben = oben;
        }

        internal NormGroesse Groesse { get; }
        internal int Tag { get; }
        internal int Stunde { get; }
        internal double Wert { get; }
        internal double Unten { get; }
        internal double Oben { get; }

        /// <summary>Wie weit der Rechenwert außerhalb des Bands liegt; 0 = im Band.</summary>
        internal double Ueberschreitung => Math.Max(0.0, Math.Max(Unten - Wert, Wert - Oben));

        /// <summary>Abstand zum näheren Bandrand; negativ = außerhalb.</summary>
        internal double Reserve => Math.Min(Wert - Unten, Oben - Wert);

        /// <summary>Kurzbeschreibung OHNE Absolutwerte — Größe, Tag, Stunde, Überschreitung.</summary>
        public override string ToString()
            => Groesse + " Tag " + Tag + " Stunde " + Stunde + ": "
               + Ueberschreitung.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " außerhalb";
    }

    /// <summary>
    /// Die Prüfregel nach Entscheid E10 (Konzept N1.15, Rechenschritte 10.2): Band zwischen
    /// den beiden Programmspalten, erweitert um die Toleranz der Richtlinie und die halbe
    /// Druckstelle — ± 0,15 K für Temperaturen, ± 1,5 W für Lasten; die Grenzen gehören
    /// zum Band. Einheitlich für alle Größen und alle Fälle.
    /// </summary>
    internal static class Normband
    {
        /// <summary>Zuschlag für Temperaturen [K] (Toleranz 0,1 K + halbe Druckstelle 0,05 K).</summary>
        internal const double ZUSCHLAG_TEMPERATUR_K = 0.15;

        /// <summary>Zuschlag für Lasten [W] (Toleranz 1 W + halbe Druckstelle 0,5 W).</summary>
        internal const double ZUSCHLAG_LAST_W = 1.5;

        /// <summary>Der Zuschlag einer Größe.</summary>
        internal static double Zuschlag(NormGroesse g) => g == NormGroesse.Last ? ZUSCHLAG_LAST_W : ZUSCHLAG_TEMPERATUR_K;

        /// <summary>Das Band [min(P1, P2) − Zuschlag, max(P1, P2) + Zuschlag].</summary>
        internal static (double Unten, double Oben) Band(double programm1, double programm2, NormGroesse g)
        {
            if (double.IsNaN(programm1) || double.IsNaN(programm2))
                throw new ArgumentException("Eine Programmspalte ist leer.");
            double z = Zuschlag(g);
            return (Math.Min(programm1, programm2) - z, Math.Max(programm1, programm2) + z);
        }

        /// <summary>Liegt <paramref name="wert"/> im Band (Grenzen eingeschlossen)?</summary>
        internal static bool ImBand(double wert, double programm1, double programm2, NormGroesse g)
        {
            (double u, double o) = Band(programm1, programm2, g);
            return wert >= u && wert <= o;
        }

        /// <summary>Die Überschreitung des Bands; 0 im Band.</summary>
        internal static double Ueberschreitung(double wert, double programm1, double programm2, NormGroesse g)
        {
            (double u, double o) = Band(programm1, programm2, g);
            return new Normzelle(g, 1, 1, wert, u, o).Ueberschreitung;
        }
    }

    /// <summary>
    /// Rechnet einen <see cref="Normfall"/> mit dem 2-K-Löser und hält jede Referenzzelle
    /// gegen das <see cref="Normband"/>. Stunde n des Tags d ist die Blockstunde mit dem
    /// Index (d − 1)·24 + n − 1 ab Laufbeginn. Die Last hat das Vorzeichen der Richtlinie:
    /// <see cref="Stundenergebnis.LastW"/> = Heizen − Kühlen.
    /// </summary>
    internal static class Normfallpruefung
    {
        /// <summary>Die Messkette der AixLib: Start aus <see cref="Normfall.ThetaStart"/>, kein Vorlauf.</summary>
        internal static List<Normzelle> Rechnen(Normfall fall)
        {
            var modell = new Zonenmodell2K(fall.Parameter, "Normfall " + fall.Nummer);
            modell.Zuruecksetzen(fall.ThetaStart);
            return Auswerten(fall, modell);
        }

        /// <summary>
        /// Wie <see cref="Rechnen"/>, aber aus dem eingeschwungenen Zustand: vorher läuft
        /// <see cref="Vorlauf2K"/> über die erste Woche der Randreihe, beginnend bei
        /// <see cref="Normfall.ThetaStart"/>. Nur für den berichtenden Vergleich — die
        /// Normprüfung selbst folgt der Messkette ohne Vorlauf.
        /// </summary>
        internal static List<Normzelle> RechnenMitVorlauf(Normfall fall, out Vorlaufergebnis vorlauf)
        {
            var modell = new Zonenmodell2K(fall.Parameter, "Normfall " + fall.Nummer);
            vorlauf = Vorlauf2K.Einschwingen(modell, fall.ThetaStart, Vorlaufwoche(fall));
            return Auswerten(fall, modell);
        }

        /// <summary>Die Randfolge des Vorlaufs: die erste Woche der Randreihe (oder alles, wenn kürzer).</summary>
        internal static ReadOnlySpan<Stundenrand> Vorlaufwoche(Normfall fall)
            => new ReadOnlySpan<Stundenrand>(fall.Raender, 0, Math.Min(Vorlauf2K.WOCHE_H, fall.Raender.Length));

        /// <summary>Rechnet den Fall vom gegenwärtigen Zustand des Modells aus und prüft jede Zelle.</summary>
        internal static List<Normzelle> Auswerten(Normfall fall, Zonenmodell2K modell)
        {
            int stunden = fall.Reihen.Count == 0 ? 0 : fall.Reihen.Max(r => r.Tag) * 24;
            var ergebnis = new Stundenergebnis[stunden];
            for (int h = 0; h < stunden; h++) ergebnis[h] = modell.Schritt(in fall.Raender[h]);

            var zellen = new List<Normzelle>();
            foreach (Normreihe reihe in fall.Reihen)
            {
                for (int stunde = 1; stunde <= 24; stunde++)
                {
                    Stundenergebnis e = ergebnis[(reihe.Tag - 1) * 24 + stunde - 1];
                    double wert = Wert(e, reihe.Groesse);
                    (double u, double o) = Normband.Band(reihe.Programm1[stunde - 1], reihe.Programm2[stunde - 1], reihe.Groesse);
                    zellen.Add(new Normzelle(reihe.Groesse, reihe.Tag, stunde, wert, u, o));
                }
            }
            return zellen;
        }

        /// <summary>Der Rechenwert einer Größe aus dem Blockmittel der Stunde.</summary>
        internal static double Wert(Stundenergebnis e, NormGroesse g)
        {
            switch (g)
            {
                case NormGroesse.Lufttemperatur: return e.ThetaAirMittel;
                case NormGroesse.OperativeTemperatur: return e.ThetaOpMittel;
                default: return e.LastW;
            }
        }
    }
}
