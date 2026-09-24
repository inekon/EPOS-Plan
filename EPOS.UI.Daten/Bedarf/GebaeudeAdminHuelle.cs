using System;
using System.Collections.Generic;
using System.Globalization;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der GEBÄUDEVERWALTUNG (<c>GebaeudeAdminDialog</c>; Konzept
    /// Administrationsdialoge, Stufe 5, V16, Bestand A9) — Menü Administration › Gebäude ›
    /// Bearbeiten.
    ///
    /// <para><b>Ausgegliedert aus <see cref="GebaeudeHuelle"/></b>: Bis Stufe 5 war die
    /// Verwaltung die Betriebsart „Admin" des Projektdialogs, gebaut aus demselben
    /// Parametersatz. Jetzt steht sie im Gerüst der übrigen Verwaltungen — Katalogliste,
    /// Auswahlleiste, Stammblatt — und bekommt ihren eigenen Satz; der Projektdialog behält
    /// seinen unverändert.</para>
    ///
    /// <para><b>Alles über den Kern</b>: Zeilen, Verwendung, Duplizieren, Schloss und Löschen
    /// stehen in <see cref="GebaeudeStammCtrl"/>. <b>Gespeichert wird über den Weg des
    /// Katalogeditors</b> (<see cref="GebaeudeKatalogHuelle.Schreiben"/>, Welle #465): Das
    /// Stammblatt führt jedes Feld des Editors auf demselben Arbeitsstand, und derselbe
    /// Schreibweg samt Ableitungen und Auslieferungssperre schreibt es. Der Katalogeditor selbst
    /// erscheint nur noch für „Neu…", die Gebäudetypen (Naht <see cref="Gebaeudewege"/>) als
    /// Überlagerung im selben Fenster.</para>
    /// </summary>
    internal static class GebaeudeAdminHuelle
    {
        /// <summary>Der Fenstertitel — derselbe Text wie die Dialogüberschrift.</summary>
        internal static string Titel() => Text_("GEBA_TITEL", "Verwaltung Gebäude");

        /// <summary>Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            var werte = new Dictionary<string, object>
            {
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    () => GebaeudeStammCtrl.Katalogfilterzeilen()),
                ["Katalogprofil"] = Katalogfilterprofil.FuerGebaeude(Uebersetzen),
                ["Satz"] = new Func<string, GebaeudeStammblattDaten>(Satz),
                ["Gebaeudetypen"] = new Func<IReadOnlyList<string>>(() => TagVCtrl.Typen()),
                ["Gebaeudearten"] = new Func<IReadOnlyList<string>>(() => GebaeudeStammCtrl.Gebaeudearten(null)),
                ["Baualtersklassen"] = GebaeudeStammCtrl.Baualtersklassen(),
                ["Verwendungswerte"] = (IReadOnlyList<string>)new[]
                {
                    GebaeudeStammCtrl.FILTERWERT_WOHN, GebaeudeStammCtrl.FILTERWERT_SONSTIGE
                },
                ["Verwendungen"] = (IReadOnlyList<string>)new[]
                {
                    GebaeudeStammCtrl.Verwendungstext(GebaeudeStammCtrl.FILTERWERT_WOHN),
                    GebaeudeStammCtrl.Verwendungstext(GebaeudeStammCtrl.FILTERWERT_SONSTIGE)
                },
                ["Verwendung"] = new Func<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
                    () => GebaeudeStammCtrl.Projektverwendung()),
                // #465: der Schreibweg des Katalogeditors - dieselbe Pruefung (im Dialog), dieselbe
                // Ableitung und dieselbe Auslieferungssperre (in der Huelle).
                ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(
                    GebaeudeKatalogHuelle.Schreiben),
                ["HuellTexte"] = GebaeudeKatalogHuelle.Texte(),
                ["Prueftexte"] = GebaeudeKatalogHuelle.Prueftexte(),
                // Stufe AK1 (Anlagenkopplung 8.4, 9.1): die hergeleiteten Vorgaben der
                // Waermeuebergabe - mit der Klimareihe des geoeffneten Projekts; ohne Projekt
                // steht die Regel ohne Zahl - und das Vorschaubild des Zeitprogramms.
                ["UebergabeHerleitung"] = GebaeudeKatalogHuelle.Herleitungsweg(Dienste.Projekt.Id),
                ["WochenVorschau"] = GebaeudeKatalogHuelle.Wochenvorschau(),
                ["Loeschen"] = new Func<string, bool>(GebaeudeStammCtrl.Loeschen),
                ["Duplizieren"] = new Func<int, string, KatalogSpeicherErgebnis>(Duplizieren),
                // AD-Q15: das Schloss laesst sich nach Rueckfrage umschalten.
                ["Schloss"] = Schlosswege.Aus(GebaeudeStammCtrl.SchlossSetzen),
                ["Exists"] = new Func<string, bool>(n => new GebaeudeStammCtrl().Lies(n) != null),
                // Der Katalogeditor nur noch fuer "Neu..." (AD-Q6, #465) - bearbeitet wird im Stammblatt.
                ["KatalogGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => GebaeudeKatalogHuelle.Gaben("", GebaeudeKatalogModus.Neu)),
                ["TitelText"] = Titel(),
                ["HilfeSchluessel"] = "Form_Gebaeude.btn_Help"
            };

            // Die Gebaeudetypen-Verwaltung liegt in der Windows-Schale - ein Haken der Naht
            // (Gebaeudewege); ohne ihn kein Knopf "Gebaeudetypen...".
            if (Gebaeudewege.GebaeudetypGaben != null)
                werte["GebaeudetypGaben"] = Gebaeudewege.GebaeudetypGaben;

            return werte;
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Ein Satz fürs Stammblatt: Kenndaten, Kennzahlen, die Hülle der vier Bauteile und
        /// „Alle Daten". <c>null</c>, wenn es den Satz nicht (mehr) gibt.
        /// </summary>
        internal static GebaeudeStammblattDaten Satz(string name)
        {
            GebaeudeModel m = new GebaeudeStammCtrl().Lies(name);
            if (m == null) return null;

            return new GebaeudeStammblattDaten
            {
                Id = m.ID,
                Name = m.Gebaeudename ?? "",
                Typ = m.Typ ?? "",
                Gebaeudeart = m.Gebaeudeart ?? "",
                Verwendung = m.Wohngebaeude_Nicht_Wohngebaeude ?? "",
                Baualtersklasse = string.IsNullOrEmpty(m.Baualtersklasse)
                    ? (int?)null : GebaeudeStammCtrl.KlassenIndex(m.Baualtersklasse),
                Beschreibung = m.Beschreibung ?? "",
                Wohnflaeche = m.Wohnflaeche_gesamt,
                HgesWK = Gebaeudehuellbilanz.GesamtWK(m),
                Rechenweg = GebaeudeHuelle.Rechenwegtext(m.Gebaeude_Modell),
                Auslieferung = new GebaeudeStammCtrl().IsReadOnly(name),
                Huelle = Huelle(m),
                AlleDaten = AlleDaten(m),
                // #465: der Feldsatz des Katalogeditors - der Arbeitsstand des Stammblatts.
                Feldsatz = GebaeudeKatalogHuelle.AusModell(m)
            };
        }

        /// <summary>Die Gruppe „Hülle": Fläche und U-Wert je Bauteil (Außenwand, Fenster, Dach, Grundfläche).</summary>
        internal static IReadOnlyList<Stammblattwert> Huelle(GebaeudeModel m)
        {
            return new[]
            {
                Bauteil("GEBK_LBL_U_AUSSENWAND", "Außenwand", m.Flaeche_Außenwand, m.k_Wert_Außenwand),
                Bauteil("GEBK_LBL_U_FENSTER", "Fenster", m.gesamte_Fensterflaeche, m.k_Wert_Fenster),
                Bauteil("GEBK_LBL_U_DACHFLAECHE", "Dachfläche", m.Dachflaeche, m.k_Wert_Dachflaeche),
                Bauteil("GEBK_LBL_U_GRUNDFLAECHE", "Grundfläche", m.Grundflaeche, m.k_Wert_Grundflaeche)
            };
        }

        private static Stammblattwert Bauteil(string schluessel, string rueckfall, double flaeche, double u)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            string wert = Text_("GEBA_HUELLE_WERT", "{0} m² · U {1} W/(m²K)")
                .Replace("{0}", flaeche.ToString("N1", k))
                .Replace("{1}", u.ToString("N2", k));
            return new Stammblattwert(Text_(schluessel, rueckfall), wert);
        }

        /// <summary>
        /// „Alle Daten": die übrigen Felder des Katalogeditors als Text, in seinen Abschnitten —
        /// Kenngrößen, Fenster nach Orientierung, Raumtemperaturen, Wärmebrücken und
        /// Anschlussmaße, Modellparameter.
        /// </summary>
        internal static IReadOnlyList<Stammblattwert> AlleDaten(GebaeudeModel m)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            string Z(double wert, int stellen = 2) => wert.ToString("N" + stellen, k);
            string N(double? wert, int stellen = 2) => wert.HasValue ? wert.Value.ToString("N" + stellen, k) : "";

            return new List<Stammblattwert>
            {
                Stammblattwert.Abschnitt(Text_("GEBK_GRP_KENNGROESSEN", "Kenngrößen")),
                new(Text_("GEBK_LBL_WOHNFLAECHE", "Nutzfläche"), Z(m.Wohnflaeche_gesamt, 1), "m²"),
                new(Text_("GEBK_LBL_FLAECHE_NUTZER", "Fläche / Nutzer"), Z(m.Flaeche_Nutzer, 1), "m²"),
                new(Text_("GEBK_LBL_WAERMEGEWINNE", "Interne Wärmegewinne"), Z(m.Interne_Waermegewinne, 0), "W"),
                new(Text_("GEBK_LBL_FENSTERDURCHLASS", "Fensterdurchlaßgrad"), Z(m.Fensterdurchlassgrad)),
                new(Text_("GEBK_LBL_RAUMHOEHE", "Raumhöhe"), Z(m.Raumhoehe), "m"),
                new(Text_("GEBK_LBL_LUFTWECHSEL", "Luftwechselrate"), Z(m.Luftwechselrate), "1/h"),
                new(Text_("GEBK_LBL_SONST_FLAECHEN", "sonstige Flächen"), Z(m.Sonstige_Flaechen, 1), "m²"),
                new(Text_("GEBK_LBL_U_SONSTIGES", "Sonstiges") + " (U)", Z(m.k_Wert_Sonstiges), "W/(m²K)"),

                Stammblattwert.Abschnitt(Text_("GEBK_GRP_FENSTER_ORIENTIERUNG", "Fenster nach Orientierung")),
                new(Text_("GEBK_LBL_FF_NORD", "Fensterfläche Nord"), Z(m.Fensterflaeche_Nord, 1), "m²"),
                new(Text_("GEBK_LBL_FF_SUED", "Fensterfläche Süd"), Z(m.Fensterflaeche_Sued, 1), "m²"),
                new(Text_("GEBK_LBL_FF_OST", "Fensterfläche Ost"), N(m.Fensterflaeche_Ost, 1), "m²"),
                new(Text_("GEBK_LBL_FF_WEST", "Fensterfläche West"), N(m.Fensterflaeche_West, 1), "m²"),
                new(Text_("GEBK_LBL_FF_OSTWEST", "Fensterfläche Ost + West"), Z(m.Fensterflaeche_OstWest, 1), "m²"),

                Stammblattwert.Abschnitt(Text_("GEBK_GRP_RAUMTEMPERATUREN", "Raumtemperaturen")),
                new(Text_("GEBK_LBL_SOLL_TAG", "Soll am Tag"), Z(m.Raumsolltemperatur_Tag, 1), "°C"),
                new(Text_("GEBK_LBL_NACHTABSENKUNG", "Nachtabsenkung auf"), Z(m.Raumsolltemperatur_Nachtabsenkung, 1), "°C"),
                new(Text_("GEBK_LBL_WE_ABSENKUNG", "Wochenendabsenkung"), Z(m.Raumsolltemperatur_Wochenende, 1), "°C"),
                new(Text_("GEBK_LBL_SOLL_FERIEN", "Soll in Ferien"), Z(m.Raumsolltemperatur_Ferien, 1), "°C"),
                new(Text_("GEBK_LBL_MAXTEMPERATUR", "Maximalraumtemperatur"), Z(m.Maximaleraumtemperatur, 1), "°C"),

                Stammblattwert.Abschnitt(Text_("GEBK_GRP_SONSTIGES", "Sonstiges")),
                new(Text_("GEBK_LBL_FENSTER_WAND", "Fenster-Wand"),
                    Z(m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand, 3) + " W/(mK) · "
                    + Z(m.Abmessung_Anschluß_Fenster_Wand, 1) + " m"),
                new(Text_("GEBK_LBL_WAND_DACH", "Wand-Dach"),
                    Z(m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, 3) + " W/(mK) · "
                    + Z(m.Abmessung_Anschluß_Wand_Dach, 1) + " m"),
                new(Text_("GEBK_LBL_AUSSENWAND_KELLER", "Außenwand-Keller"),
                    Z(m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, 3) + " W/(mK) · "
                    + Z(m.Abmessung_Anschluß_Außenwand_Kellerdecke, 1) + " m"),

                Stammblattwert.Abschnitt(Text_("GEBK_GRP_MODELLPARAMETER", "Modellparameter (VDI 6007)")),
                new(Text_("GEBK_LBL_RAHMENANTEIL", "Rahmenanteil"), N(m.Rahmenanteil)),
                new(Text_("GEBK_LBL_VERSCHATTUNG", "Verschattungsfaktor"), N(m.Verschattungsfaktor)),
                new(Text_("GEBK_LBL_MASSEANTEIL", "Masseanteil außen"), N(m.Masseanteil_Aussen)),
                new(Text_("GEBK_LBL_INNENFLAECHENFAKTOR", "Innenflächenfaktor"), N(m.Innenflaechenfaktor)),
                new(Text_("GEBK_LBL_HEIZUNG_STRAHLUNG", "Strahlungsanteil Heizung"), N(m.Heizung_Strahlungsanteil)),
                new(Text_("GEBK_LBL_HEIZLEISTUNG_MAX", "Heizleistungsgrenze"), N(m.Heizleistung_Max, 1), "kW"),
                new(Text_("GEBK_LBL_INFILTRATION", "Infiltration"), N(m.Luftwechsel_Infiltration), "1/h"),
                new(Text_("GEBK_LBL_NUTZERLUEFTUNG", "Nutzerlüftung"), N(m.Luftwechsel_Nutzer), "1/h"),
                new(Text_("GEBK_LBL_KELLERTEMPERATUR", "Kellertemperatur"), N(m.Kellertemperatur, 1), "°C")
            };
        }

        private static KatalogSpeicherErgebnis Duplizieren(int id, string name)
        {
            Katalogkopie.Ergebnis e = GebaeudeStammCtrl.Duplizieren(id, name);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        private static string Uebersetzen(string schluessel) => Text_(schluessel, schluessel);

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
