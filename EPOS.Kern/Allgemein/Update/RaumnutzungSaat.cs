using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SAAT DES NUTZUNGSPROFIL-KATALOGS (Schritt 189, RaumnutzungSchema; Konzept Nutzungsprofile
    // Kapitel 5, Entscheide E90, E91 und E96; die Kategorie DIN nach Schritt 190, RaumnutzungDinTsSchema).
    //
    // EINE QUELLE IM CODE. Aus diesen Konstanten saet der Schemaschritt, gegen sie pruefen die Wache und
    // der Generator (NP1b). Saatschluessel sind Namen, keine Ids: die Kategorie ueber ihren Bezeichner,
    // das Profil ueber (Kategorie, Bezeichner), die Zuordnung ueber (Art, Schluessel).
    //
    //   EPOS-Muster      Wohnen, Buero, Schule tragen ihre Kennwerte (Konzept 2.3) UND als Zeilenbild
    //                    (NP-F7) genau die Vorgabezeilen der ausgelieferten Konditionierungsvorlagen
    //                    gleicher Nutzung (KonditionierungsvorlagenSaattabelle, 46 Zeilen) - das
    //                    Zeilenbild geht dem Generator vor und macht die Bitgleichheit unabhaengig von
    //                    seiner Herleitung. Sonstige traegt keinen Kennwert. Die fuenf neuen Muster
    //                    (Sport, Gastronomie, Lager, Verkehr, Technik) tragen die runden Vorschlagswerte
    //                    aus Konzept 5.1 (Q43); "Kuehlen aus" durchgehend steht als Zeilenbild TAG aus.
    //                    Kein Muster traegt einen Nennwert (NP-F18).
    //   DIN/TS 18599-10  Nummer und Name der 43 Nutzungen der DIN/TS 18599-10:2025-10, Tabelle 6 (E96;
    //                    Konzept 5.2) - Tatsachen ohne Kennwert, ohne Zeilenbild, ohne Stundenprofil (E90).
    //                    Die Namen nach Tabelle 6 mit grossem Anfangsbuchstaben; 23 und 24 tragen den Titel
    //                    aus Anhang A (der Name der Tabelle 6 ist laenger als 80 Zeichen). Die Wohngebaeude
    //                    (Tabelle 5, ohne Nummer) fuehrt der Katalog nicht als Profil, sondern ueber das
    //                    Muster Wohnen und die Werte 70 und 71 der Projektdatei.
    //   SIA 2024,
    //   VDI 2078         leer, nur Beschreibung und Quellenhinweis (Konzept 5.3).
    //   Zuordnung        die heutigen festen Paare (Din18599Nutzung, Zonenplan.NutzungAusKlasse) und
    //                    die Paare der neuen Muster (Konzept 5.4, Q42). Der Schluessel DIN_NUMMER ist die
    //                    Nummer der HottCAD-Projektdatei (PdProfileUsage.ProfileUsageType), nicht die Nummer
    //                    des Katalogs: Er zaehlt wie bisher nach DIN V 18599-10:2018-09 (28, 29, 31, 35, 41),
    //                    weil keine Projektdatei im Repositorium belegt, dass HottCAD wie die DIN/TS 2025
    //                    zaehlt (Konzept 5.4, B10). DIN 19 (Verkehrsflaechen) und 20 (Lager, Technik, Archiv)
    //                    zaehlen in beiden Ausgaben gleich und fuehren auf die Muster Verkehr und Lager (E96);
    //                    eine Datenbank, die Schritt 189 ohne sie durchlief, bekommt sie aus Schritt 190.
    //   Z_Nutzungsprofil die Musternamen Buero und Schule unter Quelle KONDITIONIERUNG auf BUERO_SCHULE,
    //                    wie die Kennungen BUERO und SCHULE (NP-F15); Wohnen und Sonstige bleiben
    //                    Vorgabe wie WOHNEN und SONSTIGE.
    // ====================================================================================

    /// <summary>Eine ausgelieferte Kategorie des Nutzungsprofil-Katalogs.</summary>
    public sealed class RaumnutzungSaatkategorie
    {
        internal RaumnutzungSaatkategorie(string art, string bezeichner, string beschreibung, string quellenhinweis, int reihenfolge)
        {
            Art = art;
            Bezeichner = bezeichner;
            Beschreibung = beschreibung;
            Quellenhinweis = quellenhinweis;
            Reihenfolge = reihenfolge;
        }

        /// <summary>Die Art (<see cref="RaumnutzungSchema.ARTEN_KATALOG"/>).</summary>
        public string Art { get; }

        /// <summary>Der Name der Kategorie, eindeutig ohne Unterschied der Schreibung.</summary>
        public string Bezeichner { get; }

        /// <summary>Die Beschreibung (≤ 400 Zeichen).</summary>
        public string Beschreibung { get; }

        /// <summary>Norm, Ausgabe, Lizenzhinweis (≤ 400 Zeichen); <c>null</c> = ohne.</summary>
        public string Quellenhinweis { get; }

        /// <summary>Die Anzeigereihenfolge.</summary>
        public int Reihenfolge { get; }
    }

    /// <summary>Eine Zeile des Zeilenbilds eines ausgelieferten Profils (NP-F7) — Form von <c>Tab_Konditionierungsvorgabe</c>.</summary>
    public sealed class RaumnutzungSaatzeile
    {
        internal RaumnutzungSaatzeile(string groesse, string zeile, double? wert, bool aus, int? von, int? bis)
        {
            Groesse = groesse;
            Zeile = zeile;
            Wert = wert;
            Aus = aus;
            Von = von;
            Bis = bis;
        }

        /// <summary>Das Kennwort der Größe (<see cref="DbWerte.KOND_GROESSEN"/>).</summary>
        public string Groesse { get; }

        /// <summary>Die Zeile der Matrix (TAG, NACHT, WOCHENENDE, FERIEN).</summary>
        public string Zeile { get; }

        /// <summary>Der Wert in der Einheit der Größe; <c>null</c> bei „aus".</summary>
        public double? Wert { get; }

        /// <summary>„aus".</summary>
        public bool Aus { get; }

        /// <summary>Beginn des Nachtfensters [Uhr], nur in der Zeile NACHT.</summary>
        public int? Von { get; }

        /// <summary>Ende des Nachtfensters [Uhr], nur in der Zeile NACHT.</summary>
        public int? Bis { get; }

        /// <summary>Kurzform für Meldungen.</summary>
        public override string ToString() => Groesse + "/" + Zeile;
    }

    /// <summary>
    /// Ein ausgeliefertes Profil: Kategorie, Nummer, Name und die Kennwerte in der Reihenfolge von
    /// <see cref="RaumnutzungSchema.SPALTEN_KENNWERTE"/>; jeder Kennwert darf leer sein (NP-F6).
    /// </summary>
    public sealed class RaumnutzungSaatprofil
    {
        /// <summary>Der Bezeichner der Kategorie, zu der das Profil gehört.</summary>
        public string Kategorie { get; init; }

        /// <summary>Die Nummer in der Quelle (Text, NP-F5); <c>null</c> = ohne.</summary>
        public string Nummer { get; init; }

        /// <summary>Der Name, eindeutig je Kategorie.</summary>
        public string Bezeichner { get; init; }

        /// <summary>Die Beschreibung; <c>null</c> = ohne.</summary>
        public string Beschreibung { get; init; }

        /// <summary>Nutzungszeit [Uhr 0 … 24].</summary>
        public int? Nutzung_Von { get; init; }
        /// <summary>Nutzungszeit [Uhr 0 … 24].</summary>
        public int? Nutzung_Bis { get; init; }
        /// <summary>Betriebszeit [Uhr 0 … 24]; leer = wie Nutzung.</summary>
        public int? Betrieb_Von { get; init; }
        /// <summary>Betriebszeit [Uhr 0 … 24]; leer = wie Nutzung.</summary>
        public int? Betrieb_Bis { get; init; }
        /// <summary>Sieben Ziffern 0/1, Montag bis Sonntag.</summary>
        public string Nutzungstage_Woche { get; init; }
        /// <summary>Nutzungstage je Jahr (Vergleichswert, NP-F8).</summary>
        public int? Nutzungstage_Jahr { get; init; }
        /// <summary>Die neun bundeseinheitlichen Feiertage „wie Sonntag".</summary>
        public bool? Feiertage_Wie_Sonntag { get; init; }
        /// <summary>Heizsollwert im Betriebsfenster [°C].</summary>
        public double? Heiz_Soll { get; init; }
        /// <summary>Heizsollwert außerhalb [°C].</summary>
        public double? Heiz_Soll_Ausserhalb { get; init; }
        /// <summary>Heizen außerhalb aus.</summary>
        public bool? Heiz_Aus_Ausserhalb { get; init; }
        /// <summary>Kühlsollwert im Betriebsfenster [°C].</summary>
        public double? Kuehl_Soll { get; init; }
        /// <summary>Kühlsollwert außerhalb [°C].</summary>
        public double? Kuehl_Soll_Ausserhalb { get; init; }
        /// <summary>Kühlen außerhalb aus.</summary>
        public bool? Kuehl_Aus_Ausserhalb { get; init; }
        /// <summary>Außenluft im Betriebsfenster in <see cref="Aussenluft_Einheit"/>.</summary>
        public double? Aussenluft { get; init; }
        /// <summary><c>1/h</c> oder <c>m3/hm2</c> (NP-F10).</summary>
        public string Aussenluft_Einheit { get; init; }
        /// <summary>Außenluft außerhalb in <see cref="Aussenluft_Einheit"/>.</summary>
        public double? Aussenluft_Ausserhalb { get; init; }
        /// <summary>Fläche je Person [m²].</summary>
        public double? Personen_Flaeche { get; init; }
        /// <summary>Wärmeabgabe je Person [W].</summary>
        public double? Personen_Waerme { get; init; }
        /// <summary>Anteil der Personen im Nutzungsfenster [0 … 1].</summary>
        public double? Personen_Anteil { get; init; }
        /// <summary>Anteil der Personen außerhalb [0 … 1].</summary>
        public double? Personen_Anteil_Ausserhalb { get; init; }
        /// <summary>Gerätelast [W/m²].</summary>
        public double? Geraete_Leistung { get; init; }
        /// <summary>Anteil der Geräte im Nutzungsfenster [0 … 1].</summary>
        public double? Geraete_Anteil { get; init; }
        /// <summary>Anteil der Geräte außerhalb [0 … 1].</summary>
        public double? Geraete_Anteil_Ausserhalb { get; init; }
        /// <summary>Beleuchtung [W/m²] (Q38: Anteil der Gerätelast, getrennt geführt).</summary>
        public double? Beleuchtung_Leistung { get; init; }
        /// <summary>Anteil der Beleuchtung [0 … 1].</summary>
        public double? Beleuchtung_Anteil { get; init; }

        /// <summary>Das Zeilenbild (NP-F7); leer = ohne.</summary>
        public IReadOnlyList<RaumnutzungSaatzeile> Zeilen { get; init; } = Array.Empty<RaumnutzungSaatzeile>();

        /// <summary>Die Kennwerte in der Reihenfolge von <see cref="RaumnutzungSchema.SPALTEN_KENNWERTE"/>; leer = <c>null</c>.</summary>
        public IReadOnlyList<object> Kennwerte() => new object[]
        {
            Nutzung_Von, Nutzung_Bis, Betrieb_Von, Betrieb_Bis, Nutzungstage_Woche, Nutzungstage_Jahr,
            Bit(Feiertage_Wie_Sonntag),
            Heiz_Soll, Heiz_Soll_Ausserhalb, Bit(Heiz_Aus_Ausserhalb),
            Kuehl_Soll, Kuehl_Soll_Ausserhalb, Bit(Kuehl_Aus_Ausserhalb),
            Aussenluft, Aussenluft_Einheit, Aussenluft_Ausserhalb,
            Personen_Flaeche, Personen_Waerme, Personen_Anteil, Personen_Anteil_Ausserhalb,
            Geraete_Leistung, Geraete_Anteil, Geraete_Anteil_Ausserhalb,
            Beleuchtung_Leistung, Beleuchtung_Anteil,
        };

        /// <summary>Trägt das Profil keinen einzigen Kennwert und kein Zeilenbild?</summary>
        public bool IstLeer => Kennwerte().All(w => w == null) && Zeilen.Count == 0;

        private static object Bit(bool? b) => b.HasValue ? (object)(b.Value ? 1 : 0) : null;

        /// <summary>Kurzform für Meldungen.</summary>
        public override string ToString() => Kategorie + " / " + Bezeichner;
    }

    /// <summary>Eine ausgelieferte Zeile der Zuordnung (NP-F12).</summary>
    public sealed class RaumnutzungSaatzuordnung
    {
        internal RaumnutzungSaatzuordnung(string art, string schluessel, string kategorie, string profil)
        {
            Art = art;
            Schluessel = schluessel;
            Kategorie = kategorie;
            Profil = profil;
        }

        /// <summary>Die Art (<see cref="RaumnutzungSchema.ARTEN_ZUORDNUNG"/>).</summary>
        public string Art { get; }

        /// <summary>Der Schlüssel (DIN-Nummer als Text, IFC-Nutzungsklasse).</summary>
        public string Schluessel { get; }

        /// <summary>Die Kategorie des Zielprofils.</summary>
        public string Kategorie { get; }

        /// <summary>Der Name des Zielprofils.</summary>
        public string Profil { get; }

        /// <summary>Kurzform für Meldungen.</summary>
        public override string ToString() => Art + ":" + Schluessel + " -> " + Profil;
    }

    /// <summary>
    /// <b>Die Saat des Nutzungsprofil-Katalogs</b> (Schritt <see cref="RaumnutzungSchema.SCHRITT"/>): Kategorien,
    /// Profile samt Zeilenbild, Zuordnung und die Pufferzuordnung der Musternamen — die EINE Quelle für
    /// Schemaschritt, Wache und Generator. Herleitung im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungSaat
    {
        /// <summary>Der Name der Kategorie der EPOS-Muster.</summary>
        public const string KATEGORIE_EPOS = "EPOS-Muster";

        /// <summary>Der Name der Kategorie DIN/TS 18599-10 (Ausgabe 2025-10, E96).</summary>
        public const string KATEGORIE_DIN = "DIN/TS 18599-10";

        /// <summary>Der Name der Kategorie SIA 2024.</summary>
        public const string KATEGORIE_SIA = "SIA 2024";

        /// <summary>Der Name der Kategorie VDI 2078.</summary>
        public const string KATEGORIE_VDI = "VDI 2078";

        /// <summary>Muster Wohnen — Name gleich der Konditionierungsvorlage.</summary>
        public const string WOHNEN = KonditionierungsvorlagenSaattabelle.WOHNEN;

        /// <summary>Muster Büro — Name gleich der Konditionierungsvorlage.</summary>
        public const string BUERO = KonditionierungsvorlagenSaattabelle.BUERO;

        /// <summary>Muster Schule — Name gleich der Konditionierungsvorlage.</summary>
        public const string SCHULE = KonditionierungsvorlagenSaattabelle.SCHULE;

        /// <summary>Muster Sonstige — ohne Kennwert.</summary>
        public const string SONSTIGE = "Sonstige";

        /// <summary>Neues Muster Sport.</summary>
        public const string SPORT = "Sport";

        /// <summary>Neues Muster Gastronomie.</summary>
        public const string GASTRONOMIE = "Gastronomie";

        /// <summary>Neues Muster Lager.</summary>
        public const string LAGER = "Lager";

        /// <summary>Neues Muster Verkehr.</summary>
        public const string VERKEHR = "Verkehr";

        /// <summary>Neues Muster Technik.</summary>
        public const string TECHNIK = "Technik";

        /// <summary>Die Beschreibung der Muster mit Werten — dieselbe wie an den Konditionierungsvorlagen.</summary>
        public const string BESCHREIBUNG_MUSTER = KonditionierungsvorlagenSaattabelle.BESCHREIBUNG;

        /// <summary>Die Beschreibung des Musters Sonstige.</summary>
        public const string BESCHREIBUNG_SONSTIGE = "Ohne Kennwerte: nur der Name an der Zone";

        /// <summary>Der Quellenhinweis der Kategorie DIN/TS 18599-10 (Konzept 5.2, E96).</summary>
        public const string QUELLE_DIN =
            "DIN/TS 18599-10:2025-10, Energetische Bewertung von Gebäuden – Teil 10: Nutzungsrandbedingungen, " +
            "Klimadaten; ersetzt DIN V 18599-10:2018-09. Werte trägt der Anwender aus seiner lizenzierten Ausgabe ein " +
            "oder importiert sie.";

        /// <summary>Die Beschreibung der Kategorie DIN/TS 18599-10.</summary>
        public const string BESCHREIBUNG_DIN = "Nutzungsprofile nach DIN/TS 18599-10 mit Nummer und Name, ohne Werte";

        /// <summary>Die vier ausgelieferten Kategorien in Anzeigereihenfolge.</summary>
        public static IReadOnlyList<RaumnutzungSaatkategorie> Kategorien { get; } = new[]
        {
            new RaumnutzungSaatkategorie(RaumnutzungSchema.ART_EPOS_MUSTER, KATEGORIE_EPOS,
                "Muster von EPOS-Plan mit runden Werten, weder Norm- noch Messwerte",
                "EPOS-Plan", 1),
            new RaumnutzungSaatkategorie(RaumnutzungSchema.ART_DIN_V_18599_10, KATEGORIE_DIN, BESCHREIBUNG_DIN, QUELLE_DIN, 2),
            new RaumnutzungSaatkategorie(RaumnutzungSchema.ART_SIA_2024, KATEGORIE_SIA,
                "Raumnutzungen nach SIA 2024, nummeriert mit Punkten, mit Tagesverläufen je Stunde " +
                "(Stundenprofile); ausgeliefert ohne Profile",
                "Merkblatt SIA 2024, Raumnutzungsdaten für Energie- und Gebäudetechnik; lizenzpflichtig. Profile " +
                "legt der Anwender aus seiner Ausgabe an oder importiert sie.", 3),
            new RaumnutzungSaatkategorie(RaumnutzungSchema.ART_VDI_2078, KATEGORIE_VDI,
                "Nutzungszeiten und innere Lasten nach VDI 2078; ausgeliefert ohne Profile",
                "Richtlinie VDI 2078, Berechnung der thermischen Lasten und Raumtemperaturen; lizenzpflichtig. " +
                "Profile legt der Anwender aus seiner Ausgabe an oder importiert sie.", 4),
        };

        /// <summary>
        /// Das Zeilenbild eines alten Musters: die Vorgabezeilen aller ausgelieferten Konditionierungsvorlagen mit
        /// dieser Nutzung, in Schemareihenfolge der Größen und Matrixreihenfolge der Zeilen.
        /// </summary>
        public static IReadOnlyList<RaumnutzungSaatzeile> ZeilenbildDerVorlagen(string nutzung)
            => KonditionierungsvorlagenSaattabelle.Alle
                .Where(v => string.Equals(v.Nutzung, nutzung, StringComparison.Ordinal))
                .SelectMany(v => v.Zeilen.Select(z => new RaumnutzungSaatzeile(v.Kennwort, z.Zeile, z.Wert, z.Aus, z.Von, z.Bis)))
                .ToList();

        private static RaumnutzungSaatzeile[] KuehlenAus()
            => new[] { new RaumnutzungSaatzeile(DbWerte.KOND_GROESSE_KUEHLSOLL, DbWerte.KOND_ZEILE_TAG, null, true, null, null) };

        /// <summary>Die neun Profile der Kategorie EPOS-Muster.</summary>
        public static IReadOnlyList<RaumnutzungSaatprofil> Muster { get; } = new[]
        {
            // ---- Die heutigen Muster: Kennwerte aus Konzept 2.3, Zeilenbild = Vorlagen gleicher Nutzung ----
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = WOHNEN, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 7, Nutzung_Bis = 17, Betrieb_Von = 6, Betrieb_Bis = 22, Nutzungstage_Woche = "1111100",
                Feiertage_Wie_Sonntag = false,
                Heiz_Soll = 20.0, Heiz_Soll_Ausserhalb = 18.0,
                Kuehl_Soll = 26.0, Kuehl_Soll_Ausserhalb = 28.0,
                Personen_Anteil = 0.5, Personen_Anteil_Ausserhalb = 1.0,
                Geraete_Anteil = 1.0, Geraete_Anteil_Ausserhalb = 1.0,
                Zeilen = ZeilenbildDerVorlagen(DbWerte.KOND_NUTZUNG_WOHNEN),
            },
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = BUERO, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 8, Nutzung_Bis = 17, Betrieb_Von = 7, Betrieb_Bis = 18, Nutzungstage_Woche = "1111100",
                Feiertage_Wie_Sonntag = true,
                Heiz_Soll = 20.0, Heiz_Soll_Ausserhalb = 16.0,
                Kuehl_Soll = 26.0, Kuehl_Aus_Ausserhalb = true,
                Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE, Aussenluft_Ausserhalb = 0.1,
                Personen_Anteil = 1.0, Personen_Anteil_Ausserhalb = 0.0,
                Geraete_Anteil = 1.0, Geraete_Anteil_Ausserhalb = 0.1,
                Zeilen = ZeilenbildDerVorlagen(DbWerte.KOND_NUTZUNG_BUERO),
            },
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = SCHULE, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 8, Nutzung_Bis = 14, Betrieb_Von = 7, Betrieb_Bis = 15, Nutzungstage_Woche = "1111100",
                Feiertage_Wie_Sonntag = true,
                Heiz_Soll = 20.0, Heiz_Soll_Ausserhalb = 16.0,
                Kuehl_Soll = 26.0, Kuehl_Aus_Ausserhalb = true,
                Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE, Aussenluft_Ausserhalb = 0.1,
                Personen_Anteil = 1.0, Personen_Anteil_Ausserhalb = 0.0,
                Geraete_Anteil = 1.0, Geraete_Anteil_Ausserhalb = 0.1,
                Zeilen = ZeilenbildDerVorlagen(DbWerte.KOND_NUTZUNG_SCHULE),
            },
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = SONSTIGE, Beschreibung = BESCHREIBUNG_SONSTIGE,
            },

            // ---- Die neuen Muster: Vorschlag mit runden Werten (Konzept 5.1, Q43), ohne Nennwerte ----
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = SPORT, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 8, Nutzung_Bis = 22, Nutzungstage_Woche = "1111111", Feiertage_Wie_Sonntag = false,
                Heiz_Soll = 18.0, Heiz_Soll_Ausserhalb = 15.0,
                Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE, Aussenluft_Ausserhalb = 0.1,
                Personen_Anteil = 1.0, Personen_Anteil_Ausserhalb = 0.0,
                Geraete_Anteil = 1.0, Geraete_Anteil_Ausserhalb = 0.1,
                Zeilen = KuehlenAus(),
            },
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = GASTRONOMIE, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 10, Nutzung_Bis = 23, Nutzungstage_Woche = "0111111", Feiertage_Wie_Sonntag = false,
                Heiz_Soll = 20.0, Heiz_Soll_Ausserhalb = 16.0,
                Kuehl_Soll = 26.0, Kuehl_Aus_Ausserhalb = true,
                Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE, Aussenluft_Ausserhalb = 0.1,
                Personen_Anteil = 1.0, Personen_Anteil_Ausserhalb = 0.0,
                Geraete_Anteil = 1.0, Geraete_Anteil_Ausserhalb = 0.2,
            },
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = LAGER, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 7, Nutzung_Bis = 16, Nutzungstage_Woche = "1111100", Feiertage_Wie_Sonntag = true,
                Heiz_Soll = 12.0, Heiz_Soll_Ausserhalb = 12.0,
                Personen_Anteil = 0.1, Personen_Anteil_Ausserhalb = 0.0,
                Geraete_Anteil = 0.1, Geraete_Anteil_Ausserhalb = 0.1,
                Zeilen = KuehlenAus(),
            },
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = VERKEHR, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 7, Nutzung_Bis = 18, Nutzungstage_Woche = "1111100", Feiertage_Wie_Sonntag = true,
                Heiz_Soll = 18.0, Heiz_Soll_Ausserhalb = 16.0,
                Personen_Anteil = 0.0, Personen_Anteil_Ausserhalb = 0.0,
                Geraete_Anteil = 0.1, Geraete_Anteil_Ausserhalb = 0.1,
                Zeilen = KuehlenAus(),
            },
            new RaumnutzungSaatprofil
            {
                Kategorie = KATEGORIE_EPOS, Bezeichner = TECHNIK, Beschreibung = BESCHREIBUNG_MUSTER,
                Nutzung_Von = 0, Nutzung_Bis = 24, Nutzungstage_Woche = "1111111", Feiertage_Wie_Sonntag = false,
                Heiz_Soll = 15.0,
                Personen_Anteil = 0.0,
                Geraete_Anteil = 1.0,
                Zeilen = KuehlenAus(),
            },
        };

        /// <summary>
        /// Die 43 Nutzungen der DIN/TS 18599-10:2025-10, Tabelle 6 (E96, Konzept 5.2): Nummer und Name, keine Werte.
        /// Durchgehend ganzzahlig 1 bis 43 — die Ausgabe 2018 zählte 22.1 bis 22.3 und 23 bis 41 (Umbau in
        /// <see cref="RaumnutzungDinTsSchema"/>). Die Wohngebäude (Tabelle 5) tragen keine Nummer und fehlen hier.
        /// </summary>
        public static IReadOnlyList<RaumnutzungSaatprofil> Din { get; } = new (string Nummer, string Name)[]
        {
            ("1", "Einzelbüro"), ("2", "Gruppenbüro (zwei bis sechs Arbeitsplätze)"),
            ("3", "Großraumbüro (ab sieben Arbeitsplätze)"), ("4", "Besprechung, Sitzung, Seminar"),
            ("5", "Schalterhalle"), ("6", "Einzelhandel/Kaufhaus"),
            ("7", "Einzelhandel/Kaufhaus (Lebensmittelabteilung mit Kühlprodukten)"),
            ("8", "Klassenzimmer (Schule), Gruppenraum (Kindergarten)"), ("9", "Hörsaal, Auditorium"),
            ("10", "Bettenzimmer"), ("11", "Hotelzimmer"), ("12", "Kantine"), ("13", "Restaurant"),
            ("14", "Küchen in Nichtwohngebäuden"), ("15", "Küche – Vorbereitung, Lager"),
            ("16", "WC und Sanitärräume in Nichtwohngebäuden"), ("17", "Sonstige Aufenthaltsräume"),
            ("18", "Nebenflächen (ohne Aufenthaltsräume)"), ("19", "Verkehrsflächen"), ("20", "Lager, Technik, Archiv"),
            ("21", "Rechenzentrum"),
            ("22", "Gewerbliche und industrielle Hallen – schwere Arbeit, stehende Tätigkeit"),
            ("23", "Gewerbliche und industrielle Hallen – mittelschwere Arbeit"),
            ("24", "Gewerbliche und industrielle Hallen – leichte Arbeit"),
            ("25", "Zuschauerbereich (Theater und Veranstaltungsbauten)"), ("26", "Foyer (Theater und Veranstaltungsbauten)"),
            ("27", "Bühne (Theater und Veranstaltungsbauten)"), ("28", "Messe/Kongress"),
            ("29", "Ausstellungsräume und Museum mit konservatorischen Anforderungen"),
            ("30", "Bibliothek – Lesesaal"), ("31", "Bibliothek – Freihandbereich"), ("32", "Bibliothek – Magazin und Depot"),
            ("33", "Turnhalle (ohne Zuschauerbereich)"), ("34", "Parkhäuser (Büro- und Privatnutzung)"),
            ("35", "Parkhäuser (öffentliche Nutzung)"), ("36", "Saunabereich"), ("37", "Fitnessraum"), ("38", "Labor"),
            ("39", "Untersuchungs- und Behandlungsräume"), ("40", "Spezialpflegebereiche"),
            ("41", "Flure des allgemeinen Pflegebereichs"), ("42", "Arztpraxen und Therapeutische Praxen"),
            ("43", "Lagerhallen, Logistikhallen"),
        }.Select(p => new RaumnutzungSaatprofil { Kategorie = KATEGORIE_DIN, Nummer = p.Nummer, Bezeichner = p.Name }).ToList();

        /// <summary>Alle ausgelieferten Profile: die Muster, dann die DIN-Profile.</summary>
        public static IReadOnlyList<RaumnutzungSaatprofil> Profile { get; } = Muster.Concat(Din).ToList();

        private static IEnumerable<RaumnutzungSaatzuordnung> Paare(string art, string profil, params string[] schluessel)
            => schluessel.Select(s => new RaumnutzungSaatzuordnung(art, s, KATEGORIE_EPOS, profil));

        /// <summary>
        /// Die ausgelieferte Zuordnung (Konzept 5.4): die heutigen festen Paare und die der neuen Muster (Q42).
        /// Jede zeigt auf ein EPOS-Muster, nie auf ein leeres DIN-Profil (NP-F13). Der Schlüssel <c>DIN_NUMMER</c> ist die
        /// Nummer der Projektdatei und zählt nach DIN V 18599-10:2018-09, nicht nach der Kategorie DIN/TS 18599-10 (Kopf);
        /// 19 und 20 zählen in beiden Ausgaben gleich (E96, <see cref="RaumnutzungDinTsSchema.SCHLUESSEL_ZUORDNUNG"/>).
        /// </summary>
        public static IReadOnlyList<RaumnutzungSaatzuordnung> Zuordnungen { get; } =
            Paare(RaumnutzungSchema.ZUORDNUNG_DIN, BUERO, "1", "2", "3", "4", "5")
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_DIN, SCHULE, "8", "9", "28", "29"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_DIN, WOHNEN, "70", "71"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_DIN, SPORT, "31", "35"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_DIN, GASTRONOMIE, "12", "13"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_DIN, LAGER, "20", "41"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_DIN, VERKEHR, "19"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_IFC, BUERO, "Buero"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_IFC, WOHNEN, "Wohnen", "Schlafen", "Kueche"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_IFC, SPORT, "Sport"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_IFC, GASTRONOMIE, "Gastronomie"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_IFC, LAGER, "Lager"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_IFC, VERKEHR, "Verkehr"))
            .Concat(Paare(RaumnutzungSchema.ZUORDNUNG_IFC, TECHNIK, "Technik"))
            .ToList();

        /// <summary>
        /// Die Pufferzuordnung der Musternamen unter Quelle <see cref="NutzungsprofilQuelle.KONDITIONIERUNG"/>
        /// (NP-F15): dieselben Profile wie die Kennungen <c>BUERO</c> und <c>SCHULE</c>.
        /// </summary>
        public static IReadOnlyList<(string Schluessel, PufferNutzungsprofil Profil)> Pufferzuordnungen { get; } = new[]
        {
            (BUERO, PufferNutzungsprofil.BUERO_SCHULE),
            (SCHULE, PufferNutzungsprofil.BUERO_SCHULE),
        };
    }
}
