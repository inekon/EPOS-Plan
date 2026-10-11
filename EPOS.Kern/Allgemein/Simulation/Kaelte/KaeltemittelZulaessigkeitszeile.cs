using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Lesezeilen des Kältemittels in den Dialogen</b> (Konzept Kältemittel als Stammdatum 5.2, 7.2 bis 7.5;
    /// Welle KM4-E3-b): die Zeile „Zulässigkeit“ einer Anlage, die Stammwerte eines Kältemittels unter der Klappliste
    /// der Wärmepumpe und die Zählung der Hinweise für die Kachel „Kühlung und Kälteanlagen“ — fertige Texte für die
    /// Hüllen, die Oberfläche kennt weder Prüfklasse noch Kältemitteltabelle.
    ///
    /// <para>Die Zeile „Zulässigkeit“ zeigt genau eines von fünf Ergebnissen: ohne Inbetriebnahmejahr nicht geprüft;
    /// zulässig; Warnung mit Regel (Fundstelle), Stichtag und Kriterium; nicht prüfbar mit der fehlenden Größe; nicht
    /// geprüft (Wärmepumpe ohne Kältemittel, Absorption, Bestandsanlage). Sie rechnet über
    /// <see cref="KaeltemittelProjektpruefung"/> — denselben Weg wie Warnkriterium, Laufhinweis und Bericht.</para>
    /// </summary>
    public static class KaeltemittelZulaessigkeitszeile
    {
        /// <summary>
        /// Die Zeile „Zulässigkeit“ einer Anlage des Projekts. Mit <paramref name="ersetzen"/> prüft eine
        /// Wärmepumpe den Code <paramref name="kaeltemittelWp"/> (Wahl im offenen Dialog) statt des gespeicherten.
        /// Dialogfrei; ein Lesefehler ergibt die Zeile „nicht geprüft“, nie einen Wurf.
        /// </summary>
        public static string Anlage(int idProjekt, int idAnlage, bool ersetzen = false, string kaeltemittelWp = null)
        {
            try
            {
                int? jahr = KaeltemittelProjektpruefung.Inbetriebnahmejahr(idProjekt);
                if (!jahr.HasValue) return MyResource.Resource.KM_ZUL_OHNE_JAHR;
                return Text(jahr, KaeltemittelProjektpruefung.PruefenAnlage(idProjekt, idAnlage, ersetzen, kaeltemittelWp));
            }
            catch (Exception)
            {
                return MyResource.Resource.KM_ZUL_NICHT_GEPRUEFT;
            }
        }

        /// <summary>
        /// Der Text zu einem Prüfergebnis. <paramref name="jahr"/> ist das Inbetriebnahmejahr des Projekts
        /// (<c>null</c> = keine Prüfung); <paramref name="p"/> <c>null</c> = die Anlage wird nicht geprüft.
        /// </summary>
        public static string Text(int? jahr, KaeltemittelErzeugerpruefung p)
        {
            if (!jahr.HasValue) return MyResource.Resource.KM_ZUL_OHNE_JAHR;
            if (p == null) return MyResource.Resource.KM_ZUL_OHNE_KAELTEMITTEL;
            string code = string.IsNullOrWhiteSpace(p.Code) ? "—" : p.Code.Trim();
            string j = jahr.Value.ToString(CultureInfo.InvariantCulture);
            KaeltemittelErgebnis e = p.Ergebnis;
            if (e == null)
                return string.Equals(p.Geraeteklasse, Kaeltemittelzulaessigkeit.GERAETEKLASSE_ABSORPTION, StringComparison.Ordinal)
                    ? MyResource.Resource.KM_ZUL_ABSORPTION
                    : MyResource.Resource.KM_ZUL_NICHT_GEPRUEFT;
            switch (e.Klasse)
            {
                case KaeltemittelErgebnisklasse.Zulaessig:
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_ZUL_ZULAESSIG, code, j);
                case KaeltemittelErgebnisklasse.AbStichtag:
                    {
                        string fundstelle = !string.IsNullOrWhiteSpace(p.Regel?.Quelle) ? p.Regel.Quelle.Trim() : e.Regelschluessel;
                        return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_ZUL_WARNUNG, code, fundstelle,
                            e.Stichtag?.ToString(CultureInfo.InvariantCulture) ?? "—",
                            Warnkriterien.KaeltemittelKriteriumText(p.Regel?.Kriterium, e.Grenze), j);
                    }
                case KaeltemittelErgebnisklasse.NichtPruefbar:
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_ZUL_NICHT_PRUEFBAR, j,
                        Warnkriterien.KaeltemittelFehltText(e.FehlendeGroesse));
                case KaeltemittelErgebnisklasse.Bestand:
                    return MyResource.Resource.KM_ZUL_BESTAND;
                default:
                    return MyResource.Resource.KM_ZUL_NICHT_GEPRUEFT;
            }
        }

        /// <summary>
        /// Die Zählung für die Hinweiszeile der Kachel „Kühlung und Kälteanlagen“ (Konzept 5.2, 7.5): Anlagen mit
        /// Warnung (Klasse 2) und „nicht prüfbar“ (Klasse 4). Ohne Jahr beides 0 — dann gibt es keine Zeile.
        /// </summary>
        public static (int Warnungen, int NichtPruefbar) Zaehlen(int idProjekt)
        {
            int w = 0, n = 0;
            try
            {
                foreach (KaeltemittelErzeugerpruefung p in KaeltemittelProjektpruefung.Pruefen(idProjekt))
                {
                    if (p?.Ergebnis == null) continue;
                    if (p.Ergebnis.Klasse == KaeltemittelErgebnisklasse.AbStichtag) w++;
                    else if (p.Ergebnis.Klasse == KaeltemittelErgebnisklasse.NichtPruefbar) n++;
                }
            }
            catch (Exception)
            {
                return (0, 0);
            }
            return (w, n);
        }

        /// <summary>
        /// Die Stammwerte eines Kältemittels als eine Lesezeile (Konzept 7.3): GWP, F-Gas, natürlich,
        /// Sicherheitsgruppe, PFAS und Belegstand; ein unbekanntes Feld fällt weg. <c>""</c> ohne Satz.
        /// </summary>
        public static string Lesewerte(KaeltemittelCtrl.Kaeltemittelsatz s) => Lesewerte(s, true);

        /// <summary>
        /// Die Stammwerte ohne das GWP — für den Katalogdialog der Kältemaschine, der das GWP in einer eigenen Zeile mit
        /// Herkunft zeigt (Konzept 7.1, KM4-Q17). <c>""</c> ohne Satz.
        /// </summary>
        public static string LesewerteOhneGwp(KaeltemittelCtrl.Kaeltemittelsatz s) => Lesewerte(s, false);

        /// <summary>
        /// Ab welchem Anteil das Geräte-GWP (<c>Kaeltemittel_GWP</c>) als abweichend vom Tabellenwert gilt (KM4-Q17 a:
        /// „Abweichung über 1 % als Hinweis in der Lesezeile“).
        /// </summary>
        public const double GWP_ABWEICHUNG_ANTEIL = 0.01;

        /// <summary>
        /// Weicht das Geräte-GWP vom Tabellenwert ab (KM4-Q17 a)? Nur, wenn beide gesetzt sind und der Unterschied mehr als
        /// <see cref="GWP_ABWEICHUNG_ANTEIL"/> des Tabellenwerts beträgt; bei Tabellenwert 0 zählt jedes Geräte-GWP ungleich 0.
        /// </summary>
        public static bool GwpWeichtAb(double? tabelle, double? geraet)
        {
            if (!tabelle.HasValue || !geraet.HasValue) return false;
            double t = tabelle.Value, g = geraet.Value;
            if (t == 0) return g != 0;
            return Math.Abs(g - t) > GWP_ABWEICHUNG_ANTEIL * Math.Abs(t);
        }

        private static string Lesewerte(KaeltemittelCtrl.Kaeltemittelsatz s, bool mitGwp)
        {
            if (s == null) return "";
            var teile = new List<string>();
            if (mitGwp && s.GWP.HasValue)
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_LESE_GWP,
                    s.GWP.Value.ToString("#,0.###", CultureInfo.CurrentCulture)));
            if (s.F_Gas.HasValue)
                teile.Add(s.F_Gas.Value ? MyResource.Resource.KM_LESE_FGAS : MyResource.Resource.KM_LESE_KEIN_FGAS);
            if (s.Natuerlich == true) teile.Add(MyResource.Resource.KM_LESE_NATUERLICH);
            if (!string.IsNullOrWhiteSpace(s.Sicherheitsgruppe))
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_LESE_SICHERHEITSGRUPPE, s.Sicherheitsgruppe.Trim()));
            string pfas = PfasText(s.PFAS_Status);
            if (pfas.Length > 0) teile.Add(pfas);
            string status = StatusText(s.Status);
            if (status.Length > 0)
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KM_LESE_STATUS, status));
            return teile.Count == 0 ? "" : string.Join(" · ", teile);
        }

        /// <summary>Die Lesezeile zum Code (Abgleich wie <see cref="KaeltemittelCtrl.LadenZumCode"/>); <c>""</c> ohne Treffer.</summary>
        public static string LesewerteZumCode(string code)
        {
            try { return Lesewerte(KaeltemittelCtrl.LadenZumCode(code)); }
            catch (Exception) { return ""; }
        }

        /// <summary>Der PFAS-Status (<c>DbWerte.KAELTEMITTEL_PFAS*</c>) als Anzeigetext; leer ohne Wert.</summary>
        public static string PfasText(string wert)
        {
            switch ((wert ?? "").Trim())
            {
                case DbWerte.KAELTEMITTEL_PFAS: return MyResource.Resource.KM_LESE_PFAS;
                case DbWerte.KAELTEMITTEL_PFAS_BESTANDTEIL: return MyResource.Resource.KM_LESE_PFAS_BESTANDTEIL;
                case DbWerte.KAELTEMITTEL_PFAS_KEIN: return MyResource.Resource.KM_LESE_PFAS_KEIN;
                default: return (wert ?? "").Trim();
            }
        }

        /// <summary>Der Belegstand (<c>DbWerte.GESETZ_STATUS_*</c>) als Anzeigetext; leer ohne Wert.</summary>
        public static string StatusText(string wert)
        {
            switch ((wert ?? "").Trim())
            {
                case DbWerte.GESETZ_STATUS_GESICHERT: return MyResource.Resource.KM_STATUS_GESICHERT;
                case DbWerte.GESETZ_STATUS_VORLAEUFIG: return MyResource.Resource.KM_STATUS_VORLAEUFIG;
                case DbWerte.GESETZ_STATUS_PROGNOSE: return MyResource.Resource.KM_STATUS_PROGNOSE;
                case DbWerte.GESETZ_STATUS_ABGEKUENDIGT: return MyResource.Resource.KM_STATUS_ABGEKUENDIGT;
                default: return (wert ?? "").Trim();
            }
        }
    }
}
