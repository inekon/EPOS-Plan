using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Die Vorgaben EINER Baualtersklasse (U12); <c>null</c> je Wert = die Klasse hat keinen.</summary>
    public sealed class Baualtersvorgabe
    {
        internal Baualtersvorgabe(char klasse, int katalogsaetze, double? uAussenwand, double? uFenster, double? uDach,
                                  double? uGrund, double? uSonstige, double? gWert, double? psiFensterWand,
                                  double? psiWandDach, double? psiAussenwandKeller)
        {
            Klasse = klasse;
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

        /// <summary>Der Klassenbuchstabe A…U (<see cref="GebaeudeStammCtrl.BAUALTERSKLASSEN_DE"/>).</summary>
        public char Klasse { get; }

        /// <summary>Zahl der Katalogsätze dieser Klasse, aus denen die Mediane stammen.</summary>
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
    /// <b>Die Vorgaben je Baualtersklasse</b> (Frage U12, entschieden mit E27): U-Werte, g-Wert und
    /// die drei ψ-Werte, mit denen der Gebäudeimport füllt, was die Datei nicht trägt — bei gbXML
    /// ψ immer (3.7), U-Werte ohne Konstruktionen (3.6, Punkt 3).
    ///
    /// <para><b>Eigene Katalogwerte, keine Normzahlen.</b> Jeder Wert ist der MEDIAN der Sätze
    /// dieser Klasse in <c>Tab_Gebaeude_STAMM</c> der Testdatenbank
    /// (<c>Referenzlaeufe/Kenndaten_Test.sqlite</c>) über die Werte größer null — U- und g-Werte auf
    /// zwei, ψ-Werte auf drei Nachkommastellen gerundet. TABULA/IWU ist bewusst nicht die Quelle
    /// (weder DOI noch Datensatzlizenz, Umsetzungskonzept U12).</para>
    ///
    /// <para><b>Klassen ohne Katalogsatz</b> (heute L, O, P, R, T, U) haben keine Vorgabe; ihre
    /// Zeilen bleiben leer (Herkunft <see cref="Importherkunft.Leer"/>). Der Test
    /// <c>GebaeudeVorgabenTests</c> rechnet die Mediane aus der Testdatenbank nach und gibt bei einer
    /// Abweichung die neue Tabelle aus — wer den Katalog ändert, zieht sie hier nach.</para>
    /// </summary>
    public static class GebaeudeVorgaben
    {
        private static readonly Baualtersvorgabe[] _tabelle =
        {
            //  Klasse, Sätze, U_AW,  U_Fe,  U_Da,  U_Gr,  U_So,  g,     ψ_FW,  ψ_WD,  ψ_AK
            Z('A', 11, 1.84, 2.50, 0.80, 0.80, 3.50, 0.75, 0.150, 0.400, 0.700),
            Z('B', 13, 1.47, 2.80, 0.80, 1.40, 3.50, 0.75, 0.150, 0.400, 0.700),
            Z('C', 26, 1.39, 2.50, 0.97, 0.97, 3.50, 0.62, 0.110, 0.200, 0.700),
            Z('D', 34, 1.12, 2.50, 0.75, 0.88, 3.50, 0.75, 0.110, 0.345, 0.630),
            Z('E', 21, 1.08, 2.50, 0.75, 0.75, 3.50, 0.75, 0.090, 0.300, 0.600),
            Z('F', 30, 0.83, 2.50, 0.45, 0.75, 3.50, 0.70, 0.060, 0.160, 0.500),
            Z('G', 38, 0.74, 1.95, 0.30, 0.55, 3.50, 0.70, 0.050, 0.200, 0.400),
            Z('H', 14, 0.40, 1.40, 0.26, 0.35, 2.50, 0.62, 0.105, 0.170, 0.600),
            Z('I', 34, 0.18, 1.10, 0.14, 0.25, 1.50, 0.62, 0.040, 0.100, 0.050),
            Z('J', 3, 0.12, 0.80, 0.16, 0.35, 1.10, 0.62, 0.050, 0.160, 0.400),
            Z('K', 5, 0.22, 1.10, 0.22, 0.35, 1.50, 0.62, 0.040, 0.160, 0.040),
            Z('L', 0, null, null, null, null, null, null, null, null, null),
            Z('M', 5, 0.18, 0.90, 0.15, 0.25, 1.10, 0.62, 0.040, 0.160, 0.040),
            Z('N', 3, 0.15, 0.90, 0.15, 0.22, 1.10, 0.62, 0.040, 0.160, 0.040),
            Z('O', 0, null, null, null, null, null, null, null, null, null),
            Z('P', 0, null, null, null, null, null, null, null, null, null),
            Z('Q', 26, 0.21, 0.95, 0.15, 0.26, 0.50, 0.60, 0.070, 0.180, 0.300),
            Z('R', 0, null, null, null, null, null, null, null, null, null),
            Z('S', 4, 0.16, 1.05, 0.16, 0.27, 0.18, 0.50, 0.020, 0.050, 0.040),
            Z('T', 0, null, null, null, null, null, null, null, null, null),
            Z('U', 0, null, null, null, null, null, null, null, null, null),
        };

        /// <summary>Die 21 Klassen A…U in ihrer festen Reihenfolge.</summary>
        public static IReadOnlyList<Baualtersvorgabe> Alle => _tabelle;

        /// <summary>
        /// Die Vorgaben einer Klasse; <c>null</c> ohne Klasse oder bei einem Buchstaben außerhalb
        /// A…U. Kleinbuchstaben gelten wie Großbuchstaben.
        /// </summary>
        public static Baualtersvorgabe Fuer(char? klasse)
        {
            if (!klasse.HasValue) return null;
            int index = char.ToUpperInvariant(klasse.Value) - 'A';
            return index >= 0 && index < _tabelle.Length ? _tabelle[index] : null;
        }

        /// <summary>
        /// Der Vorgabewert einer Klasse für ein Zielfeld (<see cref="GebaeudeZielfelder"/>);
        /// <c>null</c>, wenn das Feld keine Vorgabe kennt oder die Klasse keinen Wert hat.
        /// </summary>
        public static double? Wert(char? klasse, string zielfeld)
        {
            Baualtersvorgabe v = Fuer(klasse);
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
    }
}
