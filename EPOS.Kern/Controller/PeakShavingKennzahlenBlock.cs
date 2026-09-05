using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die 17 Kennzahlzeilen und die 13 Monatszeilen der Lastspitzenkappung — eine
    /// Wahrheit fuer Bildschirm, CSV und Razor-Seite (iU9-W12.0f).
    ///
    /// <para><b>Woher sie kommen.</b> <c>Form_PeakShaving.KennzahlenAnzeigen</c>
    /// (:583-608) baute sie unmittelbar als <c>ListViewItem</c>, samt der vier
    /// Hilfsmethoden <c>Kennzahl</c>, <c>Textzeile</c>, <c>AmortisationZeile</c> und
    /// <c>Trenner</c>. Welche Kennzahl in welcher Reihenfolge mit welcher Einheit
    /// steht, ist eine Fachaussage ueber den Lauf und keine Eigenschaft eines
    /// Steuerelements — Muster <see cref="SpeicherKennzahlenBlock"/> aus iU9-W11a.</para>
    ///
    /// <para><b>Die Farbe wird ein Kennzeichen.</b> Der Vorlaeufer faerbte negative
    /// Betraege rot (<c>Color.FromArgb(176, 0, 0)</c>, „Hausmuster ZahlFaerben").
    /// <c>System.Drawing</c> ist im Kern verboten; die Zeile traegt statt der Farbe
    /// <see cref="Zeile.Negativ"/>, und die Oberflaeche entscheidet, was sie daraus
    /// macht.</para>
    ///
    /// <para><b>Formate und Kultur bleiben woertlich</b> — <c>"0.#"</c>,
    /// <c>"N0"</c>, <c>"N2"</c>, <c>"0.0"</c> ueber
    /// <see cref="CultureInfo.CurrentCulture"/>, die Monatsnamen ueber
    /// <see cref="CultureInfo.CurrentUICulture"/>. Die Anzeige bleibt damit
    /// zeichengleich zum Vorlaeufer.</para>
    /// </summary>
    public static class PeakShavingKennzahlenBlock
    {
        /// <summary>
        /// Eine Zeile des Kennzahlenblocks. Eine Zeile ohne
        /// <paramref name="Bezeichnung"/> ist der TRENNER zwischen zwei Gruppen —
        /// im Vorlaeufer ein leeres <c>ListViewItem</c>.
        /// </summary>
        /// <param name="Bezeichnung">Anzeigetext aus dem Ressourcenkatalog; leer = Trenner.</param>
        /// <param name="Wert">Der fertig formatierte Wert.</param>
        /// <param name="Einheit">Die Einheit; leer, wo es keine gibt.</param>
        /// <param name="Negativ">Der Betrag ist kleiner als 0 — die Oberflaeche hebt ihn hervor.</param>
        public sealed record Zeile(string Bezeichnung, string Wert, string Einheit, bool Negativ)
        {
            /// <summary>Eine Trennzeile zwischen zwei Gruppen.</summary>
            public bool IstTrenner => string.IsNullOrEmpty(Bezeichnung);
        }

        /// <summary>Eine Zeile der Monatsspitzentabelle.</summary>
        /// <param name="Monat">Monatsname oder „Gesamtreihe".</param>
        /// <param name="Alt">Spitze ohne Speicher [kW], formatiert.</param>
        /// <param name="Neu">Spitze mit Speicher [kW], formatiert.</param>
        /// <param name="Kappung">Differenz [kW], formatiert.</param>
        public sealed record Monatszeile(string Monat, string Alt, string Neu, string Kappung);

        /// <summary>
        /// Baut den vollstaendigen Kennzahlenblock: 17 Zeilen in vier Gruppen, dazu
        /// drei Trenner. Ohne Ergebnis eine leere Liste.
        /// </summary>
        public static List<Zeile> Zeilen(PeakShavingErgebnis r)
        {
            List<Zeile> zeilen = new List<Zeile>();
            if (r == null) return zeilen;

            // --- 1) Die Kappung selbst -------------------------------------
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_SPITZE_ALT, r.PAltMaxKw, "0.#", "kW");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_SPITZE_NEU, r.PNeuMaxKw, "0.#", "kW");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_KAPPUNG, r.KappungKw, "0.#", "kW");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_SCHWELLE, r.ErreichteSchwelleKw, "0.#", "kW");
            Text(zeilen, MyResource.Resource.PEAK_KZ_GERISSEN,
                 r.SchwelleGerissen ? MyResource.Resource.PEAK_JA : MyResource.Resource.PEAK_NEIN, "");

            // --- 2) Energie -------------------------------------------------
            Trenner(zeilen);
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_LADEENERGIE, r.LadeenergieKwh, "N0", "kWh/a");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_ENTLADEENERGIE, r.EntladeenergieKwh, "N0", "kWh/a");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_VERLUSTE, r.SpeicherverlusteKwh, "N0", "kWh/a");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_VOLLZYKLEN,
                 r.Kennzahlen.AequivalenteVollzyklen, "0.0", "1/a");

            // --- 3) Ertrag --------------------------------------------------
            Trenner(zeilen);
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_ERSPARNIS, r.LeistungspreisersparnisEur, "N2", "EUR/a");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_VERLUSTKOSTEN, r.VerlustkostenEur, "N2", "EUR/a");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_ERTRAG, r.ErtragPsEur, "N2", "EUR/a");

            // --- 4) Wirtschaftlichkeit --------------------------------------
            Trenner(zeilen);
            // Voll ausgeschrieben: WindowsFormsApplication1 fuehrt einen eigenen
            // Typ dieses Namens (der Kapitalwertrechner nach DIN EN 17463), und
            // dieselbe Namensverdeckung hat schon SpeicherKennzahlenBlock getroffen.
            SpeicherEngine.WirtschaftlichkeitErgebnis w = r.Wirtschaftlichkeit;
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_INVEST, w.InvestitionEur, "N2", "EUR");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_ANNUITAET, w.AnnuitaetEur, "N2", "EUR/a");
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_UEBERSCHUSS, w.JahresueberschussEur, "N2", "EUR/a");
            Amortisation(zeilen, MyResource.Resource.PEAK_KZ_AMORT_STAT, w.StatischeAmortisation);
            Amortisation(zeilen, MyResource.Resource.PEAK_KZ_AMORT_DYN, w.DynamischeAmortisation);
            Zahl(zeilen, MyResource.Resource.PEAK_KZ_NPV, w.KapitalwertEur, "N2", "EUR");

            return zeilen;
        }

        /// <summary>
        /// Die Monatsspitzen. Der Monatsname kommt aus
        /// <see cref="CultureInfo.CurrentUICulture"/> — die Zeile „Gesamtreihe"
        /// traegt Monat 0 bzw. 13.
        /// </summary>
        public static List<Monatszeile> Monatszeilen(PeakShavingErgebnis r)
        {
            List<Monatszeile> zeilen = new List<Monatszeile>();
            if (r == null || r.Monatsspitzen == null) return zeilen;

            CultureInfo kultur = CultureInfo.CurrentUICulture;
            foreach (Monatsspitze m in r.Monatsspitzen)
            {
                string name = m.Monat >= 1 && m.Monat <= 12
                    ? kultur.DateTimeFormat.GetMonthName(m.Monat)
                    : MyResource.Resource.PEAK_MONAT_GESAMT;

                zeilen.Add(new Monatszeile(
                    name,
                    m.PAltMaxKw.ToString("0.#", CultureInfo.CurrentCulture),
                    m.PNeuMaxKw.ToString("0.#", CultureInfo.CurrentCulture),
                    m.KappungKw.ToString("0.#", CultureInfo.CurrentCulture)));
            }
            return zeilen;
        }

        // ------------------------------------------------------------ intern

        private static void Zahl(List<Zeile> zeilen, string name, double wert,
                                 string format, string einheit)
            => zeilen.Add(new Zeile(name, wert.ToString(format, CultureInfo.CurrentCulture),
                                    einheit, wert < 0.0));

        private static void Text(List<Zeile> zeilen, string name, string wert, string einheit)
            => zeilen.Add(new Zeile(name, wert, einheit, false));

        private static void Amortisation(List<Zeile> zeilen, string name,
                                         SpeicherEngine.Amortisation a)
        {
            string wert = a.IstAmortisierbar
                ? a.Jahre.ToString("0.0", CultureInfo.CurrentCulture)
                : (a.Status == SpeicherEngine.AmortisationStatus.UeberNutzungsdauer
                    ? MyResource.Resource.PEAK_AMORT_UEBER
                    : MyResource.Resource.PEAK_AMORT_NIE);
            Text(zeilen, name, wert, a.IstAmortisierbar ? "a" : "");
        }

        private static void Trenner(List<Zeile> zeilen)
            => zeilen.Add(new Zeile("", "", "", false));
    }
}
