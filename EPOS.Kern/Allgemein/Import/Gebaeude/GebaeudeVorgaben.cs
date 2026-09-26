using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorgaben EINER Baualtersklasse (U12) oder EINES Energiestandards (E47); <c>null</c> je Wert =
    /// die Zeile hat keinen.
    /// </summary>
    internal sealed class Baualtersvorgabe
    {
        internal Baualtersvorgabe(char klasse, int katalogsaetze, double? uAussenwand, double? uFenster, double? uDach,
                                  double? uGrund, double? uSonstige, double? gWert, double? psiFensterWand,
                                  double? psiWandDach, double? psiAussenwandKeller, string energiestandard = null)
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

        /// <summary>Zahl der Katalogsätze dieser Klasse bzw. dieses Standards, aus denen die Mediane stammen.</summary>
        public int Katalogsaetze { get; }

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
    /// Entscheid E47, Konzept Baualtersklassen Abschnitt 4): U-Werte, g-Wert und die drei ψ-Werte, mit
    /// denen der Gebäudeimport füllt, was die Datei nicht trägt — bei gbXML ψ immer (3.7), U-Werte ohne
    /// Konstruktionen (3.6, Punkt 3).
    ///
    /// <para><b>Eigene Katalogwerte, keine Normzahlen.</b> Jeder Wert ist der MEDIAN der Sätze dieser
    /// Klasse (bzw. dieses Energiestandards) in <c>Tab_Gebaeude_STAMM</c> der Testdatenbank
    /// (<c>Referenzlaeufe/Kenndaten_Test.sqlite</c>, nach der Umschlüsselung des Schritts
    /// <see cref="BaualtersklassenSchema.SCHRITT"/>) über die Werte größer null — U- und g-Werte auf
    /// zwei, ψ-Werte auf drei Nachkommastellen gerundet. Die Klassenzeile umfasst alle Sätze der
    /// Klasse, gleich welchen Standard sie tragen. TABULA/IWU ist bewusst nicht die Quelle — von der
    /// Typologie stammen nur die Jahresgrenzen der Klassen (Umsetzungskonzept U12, E47).</para>
    ///
    /// <para><b>Vorrang</b> (<see cref="Fuer(char?, string)"/>): Ist ein Energiestandard gesetzt und hat
    /// er Katalogsätze, kommt die Vorgabe aus dem Standard, sonst aus der Klasse. <b>Ohne Katalogsatz
    /// keine Vorgabe</b> — die Zeile bleibt leer (Herkunft <see cref="Importherkunft.Leer"/>), die
    /// Meldung nennt es; ein Wert der Nachbarklasse wird nie geliehen. Der Import kennt keinen Standard
    /// und fragt allein die Klasse (<see cref="Fuer(char?)"/>). Der Test <c>GebaeudeVorgabenTests</c>
    /// rechnet die Mediane aus der Testdatenbank nach und gibt bei einer Abweichung die neuen Tabellen
    /// aus — wer den Katalog ändert, zieht sie hier nach.</para>
    /// </summary>
    internal static class GebaeudeVorgaben
    {
        /// <summary>Der „Buchstabe" einer Zeile, die keine Klasse ist (die Zeilen der Energiestandards).</summary>
        public const char KEINE_KLASSE = ' ';

        private static readonly Baualtersvorgabe[] _tabelle =
        {
            //  Klasse, Sätze, U_AW,  U_Fe,  U_Da,  U_Gr,  U_So,  g,     ψ_FW,  ψ_WD,  ψ_AK
            Z('A', 0, null, null, null, null, null, null, null, null, null),
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
            Z('M', 0, null, null, null, null, null, null, null, null, null),
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
            S(WindowsFormsApplication1.Energiestandard.EH55, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.EH40, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.DENKMAL, 0, null, null, null, null, null, null, null, null, null),
            S(WindowsFormsApplication1.Energiestandard.PASSIVHAUS, 3, 0.12, 0.80, 0.16, 0.35, 1.10, 0.62, 0.050, 0.160, 0.400),
            S(WindowsFormsApplication1.Energiestandard.NULLEMISSION, 0, null, null, null, null, null, null, null, null, null),
        };

        /// <summary>Die 13 Klassen A…M in ihrer festen Reihenfolge.</summary>
        public static IReadOnlyList<Baualtersvorgabe> Alle => _tabelle;

        /// <summary>Die elf Energiestandards in der Reihenfolge von <see cref="WindowsFormsApplication1.Energiestandard.CODES"/>.</summary>
        public static IReadOnlyList<Baualtersvorgabe> Standards => _standards;

        /// <summary>
        /// Die Zielfelder, die eine Baualtersklasse füllt (<see cref="Wert"/>): fünf U-Werte, der g-Wert
        /// und die drei ψ. Der Zuordnungsdialog nennt daran, wie viel die Klasse zu einer Datei beiträgt
        /// (<see cref="GebaeudeZuordnungsModell.KlassenHinweis"/>).
        /// </summary>
        public static readonly IReadOnlyList<string> Klassenfelder = new[]
        {
            GebaeudeZielfelder.U_AUSSENWAND, GebaeudeZielfelder.U_FENSTER, GebaeudeZielfelder.U_DACH,
            GebaeudeZielfelder.U_GRUND, GebaeudeZielfelder.U_SONSTIGE, GebaeudeZielfelder.G_WERT,
            GebaeudeZielfelder.PSI_FENSTER_WAND, GebaeudeZielfelder.PSI_WAND_DACH, GebaeudeZielfelder.PSI_AUSSENWAND_KELLER,
        };

        /// <summary>
        /// Die Vorgaben einer Klasse; <c>null</c> ohne Klasse oder bei einem Buchstaben außerhalb
        /// A…M. Kleinbuchstaben gelten wie Großbuchstaben.
        /// </summary>
        public static Baualtersvorgabe Fuer(char? klasse)
        {
            if (!klasse.HasValue) return null;
            int index = char.ToUpperInvariant(klasse.Value) - 'A';
            return index >= 0 && index < _tabelle.Length ? _tabelle[index] : null;
        }

        /// <summary>Die Vorgaben eines Energiestandards; <c>null</c> ohne Code oder bei einem unbekannten.</summary>
        public static Baualtersvorgabe FuerStandard(string energiestandard)
        {
            if (string.IsNullOrEmpty(energiestandard)) return null;
            foreach (Baualtersvorgabe v in _standards)
                if (string.Equals(v.Energiestandard, energiestandard, StringComparison.Ordinal)) return v;
            return null;
        }

        /// <summary>
        /// DER VORRANG (E47, Konzept Baualtersklassen 4): der Standard, wenn er gesetzt ist UND
        /// Katalogsätze hat, sonst die Klasse. Hat auch die Klasse keinen Satz, liefert ihre (leere)
        /// Zeile keinen Wert — geliehen wird nicht.
        /// </summary>
        public static Baualtersvorgabe Fuer(char? klasse, string energiestandard)
        {
            Baualtersvorgabe s = FuerStandard(energiestandard);
            return s != null && s.Katalogsaetze > 0 ? s : Fuer(klasse);
        }

        /// <summary>
        /// Der Vorgabewert einer Klasse für ein Zielfeld (<see cref="GebaeudeZielfelder"/>);
        /// <c>null</c>, wenn das Feld keine Vorgabe kennt oder die Klasse keinen Wert hat.
        /// </summary>
        public static double? Wert(char? klasse, string zielfeld) => Feldwert(Fuer(klasse), zielfeld);

        /// <summary>
        /// Der Vorgabewert mit Vorrang des Energiestandards (<see cref="Fuer(char?, string)"/>) für ein
        /// Zielfeld; <c>null</c>, wenn das Feld keine Vorgabe kennt oder die gewählte Zeile keinen Wert hat.
        /// </summary>
        public static double? Wert(char? klasse, string energiestandard, string zielfeld)
            => Feldwert(Fuer(klasse, energiestandard), zielfeld);

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
    }
}
