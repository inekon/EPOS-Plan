using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorgaben EINER Baualtersklasse (U12) oder EINES Energiestandards (E47) — aus dem Katalog
    /// (Median der Katalogsätze) oder, ohne Katalogsatz, der FREIE Wert (E51); <c>null</c> je Wert =
    /// die Zeile hat keinen.
    /// </summary>
    internal sealed class Baualtersvorgabe
    {
        internal Baualtersvorgabe(char klasse, int katalogsaetze, double? uAussenwand, double? uFenster, double? uDach,
                                  double? uGrund, double? uSonstige, double? gWert, double? psiFensterWand,
                                  double? psiWandDach, double? psiAussenwandKeller, string energiestandard = null,
                                  string quellklasse = null)
        {
            Klasse = klasse;
            Energiestandard = energiestandard;
            Katalogsaetze = katalogsaetze;
            UAussenwand = uAussenwand;
            UFenster = uFenster;
            UDach = uDach;
            UGrund = uGrund;
            USonstige = uSonstige;
            GWert = gWert;
            PsiFensterWand = psiFensterWand;
            PsiWandDach = psiWandDach;
            PsiAussenwandKeller = psiAussenwandKeller;
            Quellklasse = quellklasse;
        }

        /// <summary>
        /// Der Klassenbuchstabe A…M (<see cref="GebaeudeStammCtrl.BAUALTERSKLASSEN_DE"/>);
        /// <see cref="GebaeudeVorgaben.KEINE_KLASSE"/>
        /// bei einer Zeile eines Energiestandards.
        /// </summary>
        public char Klasse { get; }

        /// <summary>Der Code des Energiestandards (<see cref="WindowsFormsApplication1.Energiestandard.CODES"/>); <c>null</c> bei einer Klassenzeile.</summary>
        public string Energiestandard { get; }

        /// <summary>Die Kennung der Zeile für Beleg und Meldung: der Buchstabe der Klasse oder der Code des Standards.</summary>
        public string Kennung => Energiestandard ?? Klasse.ToString();

        /// <summary>Zahl der Katalogsätze dieser Klasse bzw. dieses Standards, aus denen die Mediane stammen; 0 beim freien Wert.</summary>
        public int Katalogsaetze { get; }

        /// <summary>
        /// Die Baualtersklasse der freien Quelle (Stein/Loga 2025, Tab. 28 — „bis 1918", „1919–1948" …),
        /// aus der der freie Wert stammt; <c>null</c> bei einer Zeile aus dem Katalog.
        /// </summary>
        public string Quellklasse { get; }

        /// <summary>
        /// <c>true</c> = der FREIE Wert nach Stein/Loga (2025) (E51), nicht der Median eigener
        /// Katalogsätze; Herkunft <see cref="Importherkunft.VorgabeFrei"/>, Beleg
        /// <see cref="GebaeudeVorgaben.BELEG_FREI"/>.
        /// </summary>
        public bool Frei => Quellklasse != null;

        /// <summary>U-Wert Außenwand [W/(m²K)].</summary>
        public double? UAussenwand { get; }
        /// <summary>U-Wert Fenster [W/(m²K)].</summary>
        public double? UFenster { get; }
        /// <summary>U-Wert Dach [W/(m²K)].</summary>
        public double? UDach { get; }
        /// <summary>U-Wert Grundfläche [W/(m²K)].</summary>
        public double? UGrund { get; }
        /// <summary>U-Wert sonstige Flächen [W/(m²K)].</summary>
        public double? USonstige { get; }
        /// <summary>Gesamtenergiedurchlassgrad g [–].</summary>
        public double? GWert { get; }
        /// <summary>ψ Anschluss Fenster–Wand [W/(mK)].</summary>
        public double? PsiFensterWand { get; }
        /// <summary>ψ Anschluss Wand–Dach [W/(mK)].</summary>
        public double? PsiWandDach { get; }
        /// <summary>ψ Anschluss Außenwand–Kellerdecke [W/(mK)].</summary>
        public double? PsiAussenwandKeller { get; }
    }

    /// <summary>
    /// <b>Die Vorgaben je Baualtersklasse und je Energiestandard</b> (Frage U12, entschieden mit E27;
    /// Entscheide E47 und E51, Konzept Baualtersklassen Abschnitt 4): U-Werte, g-Wert und die drei
    /// ψ-Werte, mit denen der Gebäudeimport füllt, was die Datei nicht trägt — bei gbXML ψ immer (3.7),
    /// U-Werte ohne Konstruktionen (3.6, Punkt 3).
    ///
    /// <para><b>Eigene Katalogwerte zuerst.</b> Jeder Wert der Tabellen <c>_tabelle</c> und
    /// <c>_standards</c> ist der MEDIAN der Sätze dieser Klasse (bzw. dieses Energiestandards) in
    /// <c>Tab_Gebaeude_STAMM</c> der Testdatenbank (<c>Referenzlaeufe/Kenndaten_Test.sqlite</c>, nach der
    /// Umschlüsselung des Schritts <see cref="BaualtersklassenSchema.SCHRITT"/> und mit der Saat des
    /// Schritts <see cref="GebaeudeSaatSchema.SCHRITT"/>) über die Werte größer null — U- und g-Werte auf
    /// zwei, ψ-Werte auf drei Nachkommastellen gerundet. Die Klassenzeile umfasst alle Sätze der Klasse,
    /// gleich welchen Standard sie tragen. Von der IWU-Typologie 2015 stammen nur die Jahresgrenzen der
    /// Klassen (Umsetzungskonzept U12, E47).</para>
    ///
    /// <para><b>Der freie Wert als Rückfall</b> (E51): Hat weder der Standard noch die Klasse einen
    /// Katalogsatz, gilt <c>_frei</c> — U-Werte und g-Wert des Typgebäudes EZFH nach Stein, B.; Loga, T.
    /// (2025), Anhang A, Tab. 28 (S. 68), CC BY 4.0, für Nichtwohngebäude dieselben Werte eines
    /// Wohngebäudemodells; ψ bleibt dort leer. Sichtbar mit eigener Herkunft
    /// (<see cref="Importherkunft.VorgabeFrei"/>), Beleg und Meldung, nie still. Weil alle Klassen A…M
    /// Katalogsätze haben, RUHT der Rückfall; die Tests prüfen ihn über die Lesenaht
    /// <see cref="KatalogOhne"/>. Ein Wert der Nachbarklasse wird nie geliehen.</para>
    ///
    /// <para><b>Vorrang</b> (<see cref="Fuer(char?, string)"/>): Standard mit Katalogsätzen → Klasse mit
    /// Katalogsätzen → freier Wert. Der Import kennt keinen Standard und fragt allein die Klasse
    /// (<see cref="Wert(char?, string)"/>). Der Test <c>GebaeudeVorgabenTests</c> rechnet die Mediane aus
    /// der Testdatenbank nach und gibt bei einer Abweichung die neuen Tabellen aus — wer den Katalog
    /// ändert, zieht sie hier nach.</para>
    /// </summary>
    internal static class GebaeudeVorgaben
    {
        /// <summary>Der „Buchstabe" einer Zeile, die keine Klasse ist (die Zeilen der Energiestandards).</summary>
        public const char KEINE_KLASSE = ' ';

        /// <summary>Beleg einer Vorgabe aus dem Katalog: {0} Klasse, {1} Zahl der Sätze.</summary>
        public const string BELEG_KLASSE = "GIMP_BELEG_VORGABE_KLASSE";

        /// <summary>Beleg eines freien Werts (E51): {0} Klasse, {1} Klasse der Quelle.</summary>
        public const string BELEG_FREI = "GIMP_BELEG_VORGABE_FREI";

        private static readonly Baualtersvorgabe[] _tabelle =
        {
            //  Klasse, Sätze, U_AW,  U_Fe,  U_Da,  U_Gr,  U_So,  g,     ψ_FW,  ψ_WD,  ψ_AK
            Z('A', 3, 1.37, 3.49, 1.22, 1.06, 3.49, 0.59, 0.021, 0.067, 0.101),
            Z('B', 11, 1.84, 2.50, 0.80, 0.80, 3.50, 0.75, 0.150, 0.400, 0.700),
            Z('C', 13, 1.47, 2.80, 0.80, 1.40, 3.50, 0.75, 0.150, 0.400, 0.700),
            Z('D', 26, 1.39, 2.50, 0.97, 0.97, 3.50, 0.62, 0.110, 0.200, 0.700),
            Z('E', 34, 1.12, 2.50, 0.75, 0.88, 3.50, 0.75, 0.110, 0.345, 0.630),
            Z('F', 21, 1.08, 2.50, 0.75, 0.75, 3.50, 0.75, 0.090, 0.300, 0.600),
            Z('G', 30, 0.83, 2.50, 0.45, 0.75, 3.50, 0.70, 0.060, 0.160, 0.500),
            Z('H', 38, 0.74, 1.95, 0.30, 0.55, 3.50, 0.70, 0.050, 0.200, 0.400),
            Z('I', 14, 0.40, 1.40, 0.26, 0.35, 2.50, 0.62, 0.105, 0.170, 0.600),
            Z('J', 42, 0.18, 1.10, 0.14, 0.25, 1.50, 0.62, 0.040, 0.100, 0.050),
            Z('K', 8, 0.18, 0.90, 0.15, 0.25, 1.10, 0.62, 0.040, 0.160, 0.040),
            Z('L', 30, 0.20, 0.95, 0.15, 0.26, 0.50, 0.60, 0.070, 0.180, 0.300),
            Z('M', 3, 0.20, 0.95, 0.14, 0.25, 1.26, 0.60, 0.041, 0.132, 0.198),
        };

        private static readonly Baualtersvorgabe[] _standards =
        {
            //  Energiestandard, Sätze, U_AW,  U_Fe,  U_Da,  U_Gr,  U_So,  g,     ψ_FW,  ψ_WD,  ψ_AK
            S(WindowsFormsApplication1.Energiestandard.TEILSANIERT, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.SANIERT, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.NIEDRIGENERGIE, 34, 0.18, 1.10, 0.14, 0.25, 1.50, 0.62, 0.040, 0.100, 0.050),
            S(WindowsFormsApplication1.Energiestandard.EH115_100, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.EH85, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.EH70, 3, 0.15, 0.90, 0.15, 0.22, 1.10, 0.62, 0.040, 0.160, 0.040),
            S(WindowsFormsApplication1.Energiestandard.EH55, 1, 0.20, 0.91, 0.14, 0.25, 1.26, 0.60, 0.041, 0.132, 0.198),
            S(WindowsFormsApplication1.Energiestandard.EH40, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.DENKMAL, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.PASSIVHAUS, 3, 0.12, 0.80, 0.16, 0.35, 1.10, 0.62, 0.050, 0.160, 0.400),
            S(WindowsFormsApplication1.Energiestandard.NULLEMISSION, 0, null, null, null, null, null, null, null, null, null),
        };

        /// <summary>
        /// <b>Die freien Werte</b> (E51) — U-Werte und g-Wert der nicht modernisierten Bauteile des
        /// Typgebäudes EZFH freistehend nach Stein, B.; Loga, T. (2025): Das Typgebäude-Modell zur
        /// energetischen Bewertung des Wohngebäudebestands, IWU im Auftrag des BBSR, Zenodo,
        /// doi:10.5281/zenodo.15488271, Anhang A, Tab. 28 (S. 68), differenziertes Modell, Referenzjahr
        /// 2025, CC BY 4.0; auf zwei Stellen gerundet. Dach = Spalte „Dach", Grund = Spalte „Kellerdecke"
        /// (gleich „Boden gegen Erdreich"), Sonstige = Außentüren. ψ führt die Quelle nur als
        /// Wärmebrückenzuschlag — die Zeilen lassen ψ leer. Die Quelle beginnt mit „bis 1918": A und B
        /// beruhen auf derselben Quellklasse, ebenso D, E, F (1949–1978) und G, H (1979–1994).
        /// </summary>
        private static readonly Baualtersvorgabe[] _frei =
        {
            //  Klasse, U_AW,  U_Fe,  U_Da,  U_Gr,  U_So,  g,    Klasse der Quelle
            F('A', 1.37, 3.49, 1.32, 1.02, 3.49, 0.59, "bis 1918"),
            F('B', 1.37, 3.49, 1.32, 1.02, 3.49, 0.59, "bis 1918"),
            F('C', 1.37, 3.46, 1.32, 1.02, 3.46, 0.59, "1919–1948"),
            F('D', 1.14, 3.38, 1.07, 1.01, 3.38, 0.59, "1949–1978"),
            F('E', 1.14, 3.38, 1.07, 1.01, 3.38, 0.59, "1949–1978"),
            F('F', 1.14, 3.38, 1.07, 1.01, 3.38, 0.59, "1949–1978"),
            F('G', 0.74, 2.85, 0.47, 0.67, 2.85, 0.71, "1979–1994"),
            F('H', 0.74, 2.85, 0.47, 0.67, 2.85, 0.71, "1979–1994"),
            F('I', 0.50, 1.80, 0.25, 0.40, 1.80, 0.56, "1995–2001"),
            F('J', 0.34, 1.48, 0.22, 0.37, 1.48, 0.55, "2002–2009"),
            F('K', 0.20, 1.10, 0.15, 0.25, 1.10, 0.55, "2010–2015"),
            F('L', 0.18, 0.98, 0.15, 0.18, 0.98, 0.55, "2016–2020"),
            F('M', 0.16, 0.95, 0.13, 0.16, 0.95, 0.55, "2021–2025"),
        };

        /// <summary>Die Lesenaht (<see cref="KatalogOhne"/>): ersetzt Klassen- und Standardtabelle im laufenden Ablauf.</summary>
        private static readonly AsyncLocal<(Baualtersvorgabe[] Klassen, Baualtersvorgabe[] Standards)?> _naht =
            new AsyncLocal<(Baualtersvorgabe[] Klassen, Baualtersvorgabe[] Standards)?>();

        private static Baualtersvorgabe[] Klassentabelle => _naht.Value?.Klassen ?? _tabelle;

        private static Baualtersvorgabe[] Standardtabelle => _naht.Value?.Standards ?? _standards;

        /// <summary>Die 13 Klassen A…M in ihrer festen Reihenfolge — die Zeilen aus dem Katalog.</summary>
        public static IReadOnlyList<Baualtersvorgabe> Alle => Klassentabelle;

        /// <summary>Die elf Energiestandards in der Reihenfolge von <see cref="WindowsFormsApplication1.Energiestandard.CODES"/>.</summary>
        public static IReadOnlyList<Baualtersvorgabe> Standards => Standardtabelle;

        /// <summary>Die 13 freien Werte A…M (E51) in der Reihenfolge der Klassen.</summary>
        public static IReadOnlyList<Baualtersvorgabe> FreieWerte => _frei;

        /// <summary>
        /// Die Zielfelder, die eine Baualtersklasse füllt (<see cref="Wert(char?, string)"/>): fünf
        /// U-Werte, der g-Wert und die drei ψ. Der Zuordnungsdialog nennt daran, wie viel die Klasse zu
        /// einer Datei beiträgt (<see cref="GebaeudeZuordnungsModell.KlassenHinweis"/>).
        /// </summary>
        public static readonly IReadOnlyList<string> Klassenfelder = new[]
        {
            GebaeudeZielfelder.U_AUSSENWAND, GebaeudeZielfelder.U_FENSTER, GebaeudeZielfelder.U_DACH,
            GebaeudeZielfelder.U_GRUND, GebaeudeZielfelder.U_SONSTIGE, GebaeudeZielfelder.G_WERT,
            GebaeudeZielfelder.PSI_FENSTER_WAND, GebaeudeZielfelder.PSI_WAND_DACH, GebaeudeZielfelder.PSI_AUSSENWAND_KELLER,
        };

        /// <summary>
        /// Die Katalogzeile einer Klasse (ohne Rückfall); <c>null</c> ohne Klasse oder bei einem
        /// Buchstaben außerhalb A…M. Kleinbuchstaben gelten wie Großbuchstaben.
        /// </summary>
        public static Baualtersvorgabe Fuer(char? klasse)
        {
            int index = Index(klasse);
            return index < 0 ? null : Klassentabelle[index];
        }

        /// <summary>Der freie Wert einer Klasse (E51); <c>null</c> ohne Klasse oder außerhalb A…M.</summary>
        public static Baualtersvorgabe Frei(char? klasse)
        {
            int index = Index(klasse);
            return index < 0 ? null : _frei[index];
        }

        /// <summary>Die Vorgaben eines Energiestandards; <c>null</c> ohne Code oder bei einem unbekannten.</summary>
        public static Baualtersvorgabe FuerStandard(string energiestandard)
        {
            if (string.IsNullOrEmpty(energiestandard)) return null;
            foreach (Baualtersvorgabe v in Standardtabelle)
                if (string.Equals(v.Energiestandard, energiestandard, StringComparison.Ordinal)) return v;
            return null;
        }

        /// <summary>
        /// DER VORRANG (E47, E51; Konzept Baualtersklassen 4): der Standard, wenn er gesetzt ist UND
        /// Katalogsätze hat; sonst die Klasse, wenn sie Katalogsätze hat; sonst der freie Wert der Klasse
        /// (<see cref="Baualtersvorgabe.Frei"/>). Ohne gültige Klasse und ohne Standard mit Sätzen
        /// <c>null</c>. Geliehen wird nie — der freie Wert ist der eigene der Klasse.
        /// </summary>
        public static Baualtersvorgabe Fuer(char? klasse, string energiestandard)
        {
            Baualtersvorgabe s = FuerStandard(energiestandard);
            if (s != null && s.Katalogsaetze > 0) return s;
            Baualtersvorgabe k = Fuer(klasse);
            if (k == null) return null;
            return k.Katalogsaetze > 0 ? k : Frei(klasse);
        }

        /// <summary>
        /// Der Vorgabewert einer Klasse für ein Zielfeld (<see cref="GebaeudeZielfelder"/>) mit dem Vorrang
        /// von <see cref="Fuer(char?, string)"/> ohne Standard — Katalog, sonst freier Wert; <c>null</c>,
        /// wenn das Feld keine Vorgabe kennt oder die gewählte Zeile keinen Wert hat (ψ beim freien Wert).
        /// </summary>
        public static double? Wert(char? klasse, string zielfeld) => Feldwert(Fuer(klasse, null), zielfeld);

        /// <summary>
        /// Der Vorgabewert mit Vorrang des Energiestandards (<see cref="Fuer(char?, string)"/>) für ein
        /// Zielfeld; <c>null</c>, wenn das Feld keine Vorgabe kennt oder die gewählte Zeile keinen Wert hat.
        /// </summary>
        public static double? Wert(char? klasse, string energiestandard, string zielfeld)
            => Feldwert(Fuer(klasse, energiestandard), zielfeld);

        /// <summary>
        /// Der Beleg einer Vorgabezeile: aus dem Katalog <see cref="BELEG_KLASSE"/> (Kennung, Zahl der
        /// Sätze), als freier Wert <see cref="BELEG_FREI"/> (Klasse, Klasse der Quelle); <c>null</c> ohne Zeile.
        /// </summary>
        public static GebaeudeBeleg Beleg(Baualtersvorgabe v)
        {
            if (v == null) return null;
            return v.Frei
                ? new GebaeudeBeleg(BELEG_FREI, v.Kennung, v.Quellklasse)
                : new GebaeudeBeleg(BELEG_KLASSE, v.Kennung, v.Katalogsaetze.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Die Herkunft einer übernommenen Vorgabe: <see cref="Importherkunft.VorgabeFrei"/> beim freien Wert, sonst <see cref="Importherkunft.Vorgabe"/>.</summary>
        public static Importherkunft Herkunft(Baualtersvorgabe v)
            => v != null && v.Frei ? Importherkunft.VorgabeFrei : Importherkunft.Vorgabe;

        /// <summary>
        /// <b>Die Lesenaht</b> für Tests: Im laufenden Ablauf (bis zum <c>Dispose</c>) haben die genannten
        /// Klassen und Standards KEINEN Katalogsatz — ihre Zeilen sind leer, der Rückfall greift. So lässt
        /// sich der freie Wert prüfen, obwohl heute jede Klasse Katalogsätze hat. Wirkt nur im eigenen
        /// Ablauf (<see cref="AsyncLocal{T}"/>), nie in einem parallel laufenden.
        /// </summary>
        internal static IDisposable KatalogOhne(IEnumerable<char> klassen, IEnumerable<string> standards = null)
        {
            var ohneKlassen = new HashSet<char>((klassen ?? Enumerable.Empty<char>()).Select(char.ToUpperInvariant));
            var ohneStandards = new HashSet<string>(standards ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            Baualtersvorgabe[] k = _tabelle
                .Select(v => ohneKlassen.Contains(v.Klasse) ? Z(v.Klasse, 0, null, null, null, null, null, null, null, null, null) : v)
                .ToArray();
            Baualtersvorgabe[] s = _standards
                .Select(v => ohneStandards.Contains(v.Energiestandard) ? S(v.Energiestandard, 0, null, null, null, null, null, null, null, null, null) : v)
                .ToArray();
            (Baualtersvorgabe[] Klassen, Baualtersvorgabe[] Standards)? vorher = _naht.Value;
            _naht.Value = (k, s);
            return new Rueckgabe(() => _naht.Value = vorher);
        }

        private sealed class Rueckgabe : IDisposable
        {
            private Action _zurueck;
            internal Rueckgabe(Action zurueck) { _zurueck = zurueck; }
            public void Dispose() { _zurueck?.Invoke(); _zurueck = null; }
        }

        private static int Index(char? klasse)
        {
            if (!klasse.HasValue) return -1;
            int index = char.ToUpperInvariant(klasse.Value) - 'A';
            return index >= 0 && index < _tabelle.Length ? index : -1;
        }

        private static double? Feldwert(Baualtersvorgabe v, string zielfeld)
        {
            if (v == null) return null;
            switch (zielfeld)
            {
                case GebaeudeZielfelder.U_AUSSENWAND: return v.UAussenwand;
                case GebaeudeZielfelder.U_FENSTER: return v.UFenster;
                case GebaeudeZielfelder.U_DACH: return v.UDach;
                case GebaeudeZielfelder.U_GRUND: return v.UGrund;
                case GebaeudeZielfelder.U_SONSTIGE: return v.USonstige;
                case GebaeudeZielfelder.G_WERT: return v.GWert;
                case GebaeudeZielfelder.PSI_FENSTER_WAND: return v.PsiFensterWand;
                case GebaeudeZielfelder.PSI_WAND_DACH: return v.PsiWandDach;
                case GebaeudeZielfelder.PSI_AUSSENWAND_KELLER: return v.PsiAussenwandKeller;
                default: return null;
            }
        }

        private static Baualtersvorgabe Z(char klasse, int saetze, double? uAw, double? uFe, double? uDa, double? uGr,
                                          double? uSo, double? g, double? psiFw, double? psiWd, double? psiAk)
            => new Baualtersvorgabe(klasse, saetze, uAw, uFe, uDa, uGr, uSo, g, psiFw, psiWd, psiAk);

        private static Baualtersvorgabe S(string standard, int saetze, double? uAw, double? uFe, double? uDa, double? uGr,
                                          double? uSo, double? g, double? psiFw, double? psiWd, double? psiAk)
            => new Baualtersvorgabe(KEINE_KLASSE, saetze, uAw, uFe, uDa, uGr, uSo, g, psiFw, psiWd, psiAk, standard);

        private static Baualtersvorgabe F(char klasse, double uAw, double uFe, double uDa, double uGr, double uSo, double g,
                                          string quellklasse)
            => new Baualtersvorgabe(klasse, 0, uAw, uFe, uDa, uGr, uSo, g, null, null, null, null, quellklasse);
    }
}
