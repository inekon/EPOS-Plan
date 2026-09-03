using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Vorschlag für den KWK-Zuschlagssatz einer <b>einzelnen</b> Anlage samt seiner
    /// Herleitung (Etappe E6). Beide Sätze stehen in ct/kWh; ein Satz von 0 mit
    /// gefüllter Herleitung heißt „begründet kein Zuschlag", nicht „nicht gerechnet".
    /// </summary>
    public sealed class KwkgSatzVorschlag
    {
        /// <summary>Vorgeschlagener Satz auf <b>eingespeisten</b> KWK-Strom [ct/kWh].</summary>
        public double SatzEinspeisungCt;

        /// <summary>Vorgeschlagener Satz auf <b>selbst genutzten</b> KWK-Strom [ct/kWh].</summary>
        public double SatzEigenCt;

        /// <summary>Herleitung des Einspeisesatzes im Klartext (Tranchen, Norm, Stichtagsjahr).</summary>
        public string HerleitungEinspeisung = "";

        /// <summary>Herleitung des Eigenstromsatzes im Klartext.</summary>
        public string HerleitungEigen = "";

        /// <summary>true, wenn mindestens ein gebrauchter Katalogsatz fehlt — dann ist der
        /// betroffene Satz 0 und die Herleitung nennt den fehlenden Schlüssel.</summary>
        public bool Unvollstaendig;
    }

    /// <summary>
    /// Bildet den Zuschlagssatz einer KWK-Anlage nach § 7 KWKG 2025 aus dem Katalog
    /// gesetzlicher Parameter (Etappe E6, Nutzerentscheidung vom 18.08.2026:
    /// „Vorschlag aus dem Katalog, überschreibbar, Herleitung wird angezeigt").
    ///
    /// <para><b>Reine Funktion ohne Datenbankzugriff</b> (Leitentscheidung L9): Der
    /// Katalog wird als Delegat hereingereicht, damit dieselbe Rechnung im Dialog, in
    /// der Wirtschaftlichkeit und in einer Probe verwendbar ist.</para>
    ///
    /// <para><b>„Leistungsanteil" heißt Staffel, nicht Klasse — der Kern dieser Klasse.</b>
    /// § 7 Abs. 1 und 2 KWKG überschreiben ihre Wertetabelle mit <i>Leistungsanteil</i>
    /// und meinen damit <b>marginale Tranchen</b>, nicht eine Klasse, in die die Anlage
    /// als Ganzes fällt. Eine 300-kW-Anlage bekommt deshalb nicht durchgehend 4,4 ct/kWh,
    /// sondern die ersten 50 kW zu 8, die nächsten 50 kW zu 6, die nächsten 150 kW zu 5
    /// und die restlichen 50 kW zu 4,4 ct/kWh — leistungsgewichtet 5,5667 ct/kWh. Wer hier
    /// „Klasse suchen, Satz anwenden" implementiert, rechnet oberhalb von 50 kW
    /// systematisch zu niedrig; bei 300 kW wären es 4,40 statt 5,5667 ct/kWh und damit
    /// <b>21 % zu wenig</b>.</para>
    ///
    /// <para><b>Einspeisung und Eigennutzung sind nicht symmetrisch.</b> Auf
    /// eingespeisten Strom besteht der Zuschlag ohne weitere Voraussetzung (Abs. 1).
    /// Auf selbst genutzten Strom besteht er <b>nicht generell</b>, sondern nur in den
    /// drei Tatbeständen des § 6 Abs. 3 (Abs. 2) — mit drei verschiedenen Satzreihen.
    /// Ist keiner davon erfasst, ist 0 ct/kWh die richtige Antwort und nicht eine Lücke.
    /// Über allem steht die Sonderregel des § 7 Abs. 3a für <b>neue</b> Anlagen bis
    /// 50 kW (16 bzw. 8 ct/kWh), die Abs. 1 <i>und</i> 2 vorgeht.</para>
    ///
    /// <para>Faktenbasis: <c>Grundlagen_KWKG_Energiesteuer_Stromsteuer.md</c>,
    /// Abschnitt 1.3.</para>
    /// </summary>
    public static class KwkgSatzRechner
    {
        /// <summary>Eine Tranche der Leistungsstaffel: kumulierte Obergrenze und der
        /// Katalogschlüssel des Satzes, der für sie gilt.</summary>
        private sealed class Tranche
        {
            public readonly string GrenzeSchluessel;   // null = unbegrenzte letzte Tranche
            public readonly double GrenzeErsatzKW;     // Rückfall, wenn der Schlüssel fehlt
            public readonly string SatzSchluessel;

            public Tranche(string grenzeSchluessel, double grenzeErsatzKW, string satzSchluessel)
            {
                GrenzeSchluessel = grenzeSchluessel;
                GrenzeErsatzKW = grenzeErsatzKW;
                SatzSchluessel = satzSchluessel;
            }
        }

        /// <summary>
        /// Der Vorschlag für eine Anlage.
        /// </summary>
        /// <param name="pelKW">elektrische Nennleistung der Anlage [kW]</param>
        /// <param name="jahr">Stichtagsjahr — das Inbetriebnahmejahr <b>dieser</b> Anlage</param>
        /// <param name="anlagenart">Steuerwert <c>DbWerte.KWKG_ANLAGENART_*</c>; leer = neue Anlage</param>
        /// <param name="eigenfall">Steuerwert <c>DbWerte.KWKG_EIGENFALL_*</c>; leer = keiner</param>
        /// <param name="katalog">Lesefassade auf <c>Tab_Gesetzesparameter</c> (Schlüssel, Jahr)</param>
        /// <param name="kultur">Zahlenformat der Herleitung</param>
        public static KwkgSatzVorschlag Vorschlag(double pelKW, int jahr, string anlagenart,
                                                  string eigenfall,
                                                  Func<string, int, GesetzParameter> katalog,
                                                  CultureInfo kultur)
        {
            var v = new KwkgSatzVorschlag();
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            if (katalog == null || pelKW <= 0)
            {
                v.Unvollstaendig = true;
                v.HerleitungEinspeisung = MyResource.Resource.WIRT_KWKG_HERLEITUNG_OHNE_LEISTUNG;
                v.HerleitungEigen = v.HerleitungEinspeisung;
                return v;
            }

            string art = string.IsNullOrEmpty(anlagenart) ? DbWerte.KWKG_ANLAGENART_NEU : anlagenart;
            string fall = string.IsNullOrEmpty(eigenfall) ? DbWerte.KWKG_EIGENFALL_KEINER : eigenfall;

            // --- § 7 Abs. 3a: geht Abs. 1 UND 2 vor -------------------------------
            double neuGrenze = Grenze(katalog, jahr, DbWerte.GESETZ_KWKG_NEUANLAGE_GRENZE, 50.0);
            if (string.Equals(art, DbWerte.KWKG_ANLAGENART_NEU, StringComparison.Ordinal) &&
                pelKW <= neuGrenze)
            {
                Pauschal(v, katalog, jahr, pelKW, neuGrenze, kultur);
                return v;
            }

            // --- § 7 Abs. 1: eingespeister Strom, marginale Tranchen ---------------
            string herleitung;
            v.SatzEinspeisungCt = Mischsatz(pelKW, EinspeiseStaffel(art), jahr, katalog, kultur,
                                            "§ 7 Abs. 1 KWKG 2025", out herleitung);
            v.HerleitungEinspeisung = herleitung;
            if (v.SatzEinspeisungCt <= 0) v.Unvollstaendig = true;

            // --- § 7 Abs. 2: selbst genutzter Strom, nur in den drei Fällen --------
            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_KEINER, StringComparison.Ordinal))
            {
                v.SatzEigenCt = 0;
                v.HerleitungEigen = MyResource.Resource.WIRT_KWKG_HERLEITUNG_KEIN_EIGENFALL;
                return v;
            }

            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_NR1, StringComparison.Ordinal))
            {
                double n1Grenze = Grenze(katalog, jahr, DbWerte.GESETZ_KWKG_EIGEN_N1_GRENZE, 100.0);
                if (pelKW > n1Grenze)
                {
                    v.SatzEigenCt = 0;
                    v.HerleitungEigen = string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_N1_ZU_GROSS,
                        n1Grenze.ToString("N0", kultur), pelKW.ToString("N1", kultur));
                    return v;
                }
            }

            v.SatzEigenCt = Mischsatz(pelKW, EigenStaffel(fall), jahr, katalog, kultur,
                                      NormEigen(fall), out herleitung);
            v.HerleitungEigen = herleitung;
            if (v.SatzEigenCt <= 0) v.Unvollstaendig = true;
            return v;
        }

        // =====================================================================
        // Die Staffeln
        // =====================================================================

        /// <summary>§ 7 Abs. 1 — eingespeister Strom. Oberhalb von 2 MW hängt der Satz
        /// an der Anlagenart (nachgerüstet 3,1 statt 3,4 ct/kWh).</summary>
        private static Tranche[] EinspeiseStaffel(string anlagenart)
        {
            string ueber2MW =
                string.Equals(anlagenart, DbWerte.KWKG_ANLAGENART_NACHGERUESTET, StringComparison.Ordinal)
                    ? DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW_NACHGER
                    : DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW;
            return new[]
            {
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1,   50.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW),
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_2,  100.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW),
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3,  250.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW),
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4, 2000.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW),
                new Tranche(null,                                     0.0, ueber2MW)
            };
        }

        /// <summary>
        /// § 7 Abs. 2 — selbst genutzter Strom, je Tatbestand des § 6 Abs. 3 eine eigene
        /// Staffel. Nr. 1 endet bei 100 kW, weil der Tatbestand selbst dort endet (die
        /// Anlagengrenze wird vorher geprüft); Nr. 3 führt 50 bis 250 kW als EINE
        /// Tranche zu 4,00 ct/kWh — so steht es im Gesetz und so ist der Katalog gesät.
        /// </summary>
        private static Tranche[] EigenStaffel(string fall)
        {
            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_NR1, StringComparison.Ordinal))
                return new[]
                {
                    new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1,  50.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW),
                    new Tranche(null,                                   0.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS100KW)
                };

            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_NR3, StringComparison.Ordinal))
                return new[]
                {
                    new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1,   50.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS50KW),
                    new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3,  250.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS250KW),
                    new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4, 2000.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_BIS2MW),
                    new Tranche(null,                                    0.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N3_UEBER2MW)
                };

            return new[]   // Nr. 2 — Kundenanlage / geschlossenes Verteilernetz
            {
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1,   50.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW),
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_2,  100.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS100KW),
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3,  250.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS250KW),
                new Tranche(DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4, 2000.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS2MW),
                new Tranche(null,                                    0.0, DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_UEBER2MW)
            };
        }

        /// <summary>Normbezeichnung des Eigenstromfalls für die Herleitung — sprachneutral,
        /// weil sie nur aus Paragrafenzeichen und Zahlen besteht.</summary>
        private static string NormEigen(string fall)
        {
            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_NR1, StringComparison.Ordinal))
                return "§ 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 1 KWKG 2025";
            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_NR3, StringComparison.Ordinal))
                return "§ 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 3 KWKG 2025";
            return "§ 7 Abs. 2 i.V.m. § 6 Abs. 3 Nr. 2 KWKG 2025";
        }

        // =====================================================================
        // Die Rechnung
        // =====================================================================

        /// <summary>
        /// Leistungsgewichteter Mischsatz über die marginalen Tranchen [ct/kWh] samt
        /// Herleitung. Fehlt ein gebrauchter Satz im Katalog, wird 0 geliefert und die
        /// Herleitung nennt den fehlenden Schlüssel — nie ein geratener Ersatzwert
        /// (dieselbe Regel wie <see cref="GesetzKatalog.Wert(string,int)"/>).
        /// </summary>
        private static double Mischsatz(double pelKW, Tranche[] staffel, int jahr,
                                        Func<string, int, GesetzParameter> katalog,
                                        CultureInfo kultur, string norm, out string herleitung)
        {
            double unten = 0, summe = 0;
            var teile = new List<string>();

            foreach (Tranche t in staffel)
            {
                double oben = t.GrenzeSchluessel == null
                    ? double.MaxValue
                    : Grenze(katalog, jahr, t.GrenzeSchluessel, t.GrenzeErsatzKW);
                double breite = Math.Min(oben, pelKW) - unten;
                if (breite <= 0) break;

                GesetzParameter p = katalog(t.SatzSchluessel, jahr);
                if (p == null || !p.Wert.HasValue)
                {
                    herleitung = string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_SATZ_FEHLT,
                                               t.SatzSchluessel);
                    return 0;
                }

                summe += breite * p.Wert.Value;
                teile.Add(breite.ToString("N1", kultur) + " kW × " +
                          p.Wert.Value.ToString("N2", kultur));
                unten = oben;
                if (oben >= pelKW) break;
            }

            double satz = summe / pelKW;
            herleitung = string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_TRANCHEN,
                                       pelKW.ToString("N1", kultur),
                                       string.Join(" + ", teile.ToArray()),
                                       satz.ToString("N2", kultur),
                                       norm,
                                       jahr.ToString(CultureInfo.InvariantCulture));
            return satz;
        }

        /// <summary>§ 7 Abs. 3a — ein Satz für die ganze Anlage, keine Tranchen.</summary>
        private static void Pauschal(KwkgSatzVorschlag v, Func<string, int, GesetzParameter> katalog,
                                     int jahr, double pelKW, double grenzeKW, CultureInfo kultur)
        {
            const string norm = "§ 7 Abs. 3a KWKG 2025";
            GesetzParameter einsp = katalog(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP, jahr);
            GesetzParameter eigen = katalog(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN, jahr);

            v.SatzEinspeisungCt = einsp != null && einsp.Wert.HasValue ? einsp.Wert.Value : 0;
            v.SatzEigenCt = eigen != null && eigen.Wert.HasValue ? eigen.Wert.Value : 0;
            v.Unvollstaendig = v.SatzEinspeisungCt <= 0 || v.SatzEigenCt <= 0;

            v.HerleitungEinspeisung = einsp != null && einsp.Wert.HasValue
                ? string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_PAUSCHAL,
                                pelKW.ToString("N1", kultur), grenzeKW.ToString("N0", kultur),
                                v.SatzEinspeisungCt.ToString("N2", kultur), norm,
                                jahr.ToString(CultureInfo.InvariantCulture))
                : string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_SATZ_FEHLT,
                                DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP);

            v.HerleitungEigen = eigen != null && eigen.Wert.HasValue
                ? string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_PAUSCHAL,
                                pelKW.ToString("N1", kultur), grenzeKW.ToString("N0", kultur),
                                v.SatzEigenCt.ToString("N2", kultur), norm,
                                jahr.ToString(CultureInfo.InvariantCulture))
                : string.Format(MyResource.Resource.WIRT_KWKG_HERLEITUNG_SATZ_FEHLT,
                                DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN);
        }

        /// <summary>Eine Leistungsgrenze aus dem Katalog; fehlt sie, gilt der mitgegebene
        /// Ersatzwert. Er steht hier und nicht als Literal in der Rechnung, damit eine
        /// Datenbank ohne die Nachsaat (Generation 3) trotzdem einen Vorschlag liefert.</summary>
        private static double Grenze(Func<string, int, GesetzParameter> katalog, int jahr,
                                     string schluessel, double ersatzKW)
        {
            try
            {
                GesetzParameter p = katalog(schluessel, jahr);
                if (p != null && p.Wert.HasValue && p.Wert.Value > 0) return p.Wert.Value;
            }
            catch { }
            return ersatzKW;
        }
    }
}
