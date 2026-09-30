using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE VIERZEHN AUSGELIEFERTEN KONDITIONIERUNGSVORLAGEN - KP-S1b (Konzept
    // Konditionierungsprofile 3.5 "Saat (E56)", 5.7, 9.6; Entwurf KP2 Abschnitt 4), gesaet mit
    // dem Schemaschritt KonditionierungsvorlagenSaatSchema.SCHRITT.
    //
    // WAS. Je Groesse eine Liste, zusammen 14 Vorlagen in fuenf Listen (die Lueftung fuehrt kein
    // "Wohnen"): Heizen, Kuehlen, Geraete und Personen je Wohnen, Buero und Schule, die Lueftung
    // Buero und Schule. Eine Vorlage traegt die Nutzungszeilen der Spalte ihrer Groesse (Tag,
    // Nacht mit Zeiten, Wochenende, Ferien) - 46 Vorgabezeilen in 3 + 3 + 2 + 3 + 3 Vorlagen.
    // Die WERTE sind die verbindliche Tabelle des Entwurfs KP2 (Abschnitt 4), entschieden mit
    // E56 F1 (b): "sonst" heisst Nacht, Wochenende und Ferien; Schule mit eigenen Zeiten
    // (Heizen 15-7, Personen 14-8 Uhr) statt "wie Buero"; das Nachtfenster steht in JEDER
    // Nachtzeile ausdruecklich - keine Vorlage haengt am Fenster des Ziels (F19).
    //
    // FEIERTAGE (E56 F1 (b)). Buero und Schule tragen in allen fuenf Listen die neun
    // bundeseinheitlichen Feiertage "wie Sonntag" als REGELN OHNE EIGENE WOCHE: ein Kalender der
    // Vorlage mit neun Perioden der Art FEIERTAG (Rang 100 + k, WieWochentag 7) - dieselbe Form,
    // die das Werkzeug der Karte schreibt (Kalenderwerkzeuge.Feiertagsregeln). Der Kalender
    // traegt KEINE Standardwoche; seine Grundangabe ist der Wochenendwert der Vorlage (der Wert,
    // den "wie Sonntag" meint), und sie wirkt beim Uebernehmen nicht: Ohne Woche der Vorlage nimmt
    // KonditionierungsvorlageCtrl.Uebernehmen die Grundangabe des Generators aus der Matrix des
    // Ziels, die Vorlage bringt nur ihre Regeln mit.
    //
    // WAS EINE VORLAGE NICHT TRAEGT (E54): keinen Nennwert, keine Saison (Heiz- oder
    // Kuehlperiode), keine datierten Ferien - beides gehoert dem Objekt und bleibt beim Ziel.
    //
    // EINHEITEN. Sollwerte in degC, Lueftung in 1/h (absolut, Nutzerlueftung), Anteile von
    // Geraeten und Personen als 0 ... 1 (die Tabelle zeigt Prozent). "aus" ist eine eigene Angabe
    // (Aus = 1), kein Zahlenwert.
    //
    // QUELLE. EPOS-Muster mit runden Werten, weder Norm- noch Messwerte (Konzept 3.5, F22) - so
    // steht es in der Beschreibung jeder Vorlage. Namen und Datenwerte bleiben deutsch
    // (Glossar Lokalisierung, Abschnitt 10); keine Hersteller- oder Produktdaten.
    //
    // SCHLUESSEL IST (GROESSE, NAME) ohne Unterschied von Gross- und Kleinschreibung
    // (OrdinalIgnoreCase, wie die Namensregel des Controllers); feste Ids gibt es nicht.
    // ====================================================================================

    /// <summary>
    /// <b>Eine Vorgabezeile einer ausgelieferten Vorlage</b> — so, wie sie in
    /// <c>Tab_Konditionierungsvorgabe</c> steht (Eigentümer <c>ID_Vorlage</c>).
    /// </summary>
    public sealed class KonditionierungsvorlagenSaatzeile
    {
        internal KonditionierungsvorlagenSaatzeile(string zeile, double? wert, bool aus, int? von, int? bis)
        {
            Zeile = zeile;
            Wert = wert;
            Aus = aus;
            Von = von;
            Bis = bis;
        }

        /// <summary>Die Zeile der Matrix (<see cref="DbWerte.KOND_ZEILE_TAG"/> … <see cref="DbWerte.KOND_ZEILE_FERIEN"/>).</summary>
        public string Zeile { get; }

        /// <summary>Der Wert in der Einheit der Größe (Anteile 0 … 1); <c>null</c> bei „aus".</summary>
        public double? Wert { get; }

        /// <summary>„aus" — die Zelle schaltet die Größe ab (Konzept 3.1).</summary>
        public bool Aus { get; }

        /// <summary>Beginn des Nachtfensters [Uhr 0 … 23], nur in der Zeile <c>NACHT</c>.</summary>
        public int? Von { get; }

        /// <summary>Ende des Nachtfensters [Uhr 0 … 23], ausschließlich; nur in der Zeile <c>NACHT</c>.</summary>
        public int? Bis { get; }

        /// <summary>Die Zeile als Zelle der Vorgabe-Matrix.</summary>
        public Matrixzelle AlsZelle()
            => Aus ? Matrixzelle.Abgeschaltet(Von, Bis) : Matrixzelle.AusWert(Wert ?? double.NaN, Von, Bis);
    }

    /// <summary>
    /// <b>Eine ausgelieferte Konditionierungsvorlage</b> (Konzept 3.5, 5.7): eine Größe, ein Name, eine
    /// Nutzung, ihre Vorgabezeilen und — bei Büro und Schule — die neun Feiertagsregeln.
    /// </summary>
    public sealed class KonditionierungsvorlagenSaat
    {
        internal KonditionierungsvorlagenSaat(Konditionierungsgroesse groesse, string bezeichner, string nutzung,
                                              bool feiertage, params KonditionierungsvorlagenSaatzeile[] zeilen)
        {
            Groesse = groesse;
            Bezeichner = bezeichner;
            Nutzung = nutzung;
            Feiertage = feiertage;
            Zeilen = zeilen;
        }

        /// <summary>Die eine Größe der Vorlage (P11).</summary>
        public Konditionierungsgroesse Groesse { get; }

        /// <summary>Das Kennwort der Größe (<see cref="DbWerte.KOND_GROESSEN"/>).</summary>
        public string Kennwort => Konditionierungsgroessen.Kennwort(Groesse);

        /// <summary>Der Name — „Wohnen", „Büro" oder „Schule", eindeutig je Größe.</summary>
        public string Bezeichner { get; }

        /// <summary>Die Nutzung (<see cref="DbWerte.KOND_NUTZUNGEN"/>).</summary>
        public string Nutzung { get; }

        /// <summary>Die Beschreibung — für alle 14 dieselbe (<see cref="KonditionierungsvorlagenSaattabelle.BESCHREIBUNG"/>).</summary>
        public string Beschreibung => KonditionierungsvorlagenSaattabelle.BESCHREIBUNG;

        /// <summary>
        /// Trägt die Vorlage die neun Feiertage „wie Sonntag" (E56 F1 (b))? Dann gehört ihr ein
        /// Kalender ohne Standardwoche mit neun Perioden der Art <c>FEIERTAG</c>.
        /// </summary>
        public bool Feiertage { get; }

        /// <summary>Die Vorgabezeilen in Matrixreihenfolge (Tag, Nacht, Wochenende, Ferien).</summary>
        public IReadOnlyList<KonditionierungsvorlagenSaatzeile> Zeilen { get; }

        /// <summary>Die Zeile mit diesem Kennwort oder <c>null</c> (dann bleibt die Zelle beim Ziel).</summary>
        public KonditionierungsvorlagenSaatzeile Zeile(string kennwort)
        {
            foreach (KonditionierungsvorlagenSaatzeile z in Zeilen)
                if (string.Equals(z.Zeile, kennwort, StringComparison.Ordinal)) return z;
            return null;
        }

        /// <summary>
        /// Die <b>Grundangabe des Feiertagskalenders</b>: die Wochenendzeile der Vorlage — der Wert, den
        /// „wie Sonntag" meint. Sie wirkt beim Übernehmen nicht (die Vorlage trägt keine Woche, das Ziel
        /// bekommt die Grundangabe seines Generators); <c>null</c> ohne Feiertage.
        /// </summary>
        public KonditionierungsvorlagenSaatzeile Kalendergrund => Feiertage ? Zeile(DbWerte.KOND_ZEILE_WOCHENENDE) : null;

        /// <summary>Kurzform für Bericht und Meldung: <c>HEIZSOLL/Büro</c>.</summary>
        public override string ToString() => Kennwort + "/" + Bezeichner;
    }

    /// <summary>
    /// <b>Die Saattabelle der 14 ausgelieferten Konditionierungsvorlagen</b> (KP-S1b, E56 F1 (b)) —
    /// EINE Quelle für den Schemaschritt (<see cref="KonditionierungsvorlagenSaatSchema"/>), die
    /// Testdatenbank, die Wache, den Prüfbericht der Auslieferungsvorlage und die Wirtseite der
    /// Oberfläche. Die Werte und ihre Herkunft stehen im Kopf der Datei.
    /// </summary>
    public static class KonditionierungsvorlagenSaattabelle
    {
        /// <summary>Die Beschreibung jeder ausgelieferten Vorlage (Konzept 3.5, F22).</summary>
        public const string BESCHREIBUNG = "EPOS-Muster mit runden Werten, weder Norm- noch Messwerte";

        /// <summary>Der Name der Wohnvorlage.</summary>
        public const string WOHNEN = "Wohnen";

        /// <summary>Der Name der Bürovorlage.</summary>
        public const string BUERO = "Büro";

        /// <summary>Der Name der Schulvorlage.</summary>
        public const string SCHULE = "Schule";

        /// <summary>„wie Sonntag" — der Wochentag 7 der Feiertagsregeln (1 = Montag … 7 = Sonntag).</summary>
        public const int WIE_WOCHENTAG = 7;

        /// <summary>Die Zahl der Vorlagen.</summary>
        public const int VORLAGEN = 14;

        /// <summary>Die Zahl der Vorgabezeilen aller Vorlagen zusammen.</summary>
        public const int VORGABEZEILEN = 46;

        /// <summary>
        /// Die Namen der neun Feiertagsperioden in der Reihenfolge von <see cref="DbWerte.KOND_FEIERTAGE"/> —
        /// deutsch, wie jeder Datenwert der Auslieferung (Glossar Lokalisierung, Abschnitt 10), und
        /// gleich dem deutschen Text <c>KOND_TEXT_FEIERTAGE</c>, den das Werkzeug der Karte schreibt.
        /// Fest verdrahtet, damit die Saat nicht von der Oberflächensprache des Startenden abhängt.
        /// </summary>
        public static readonly IReadOnlyList<string> FEIERTAGSNAMEN = new[]
        {
            "Neujahr", "Karfreitag", "Ostermontag", "Erster Mai", "Christi Himmelfahrt", "Pfingstmontag",
            "Tag der Deutschen Einheit", "Erster Weihnachtstag", "Zweiter Weihnachtstag",
        };

        private static KonditionierungsvorlagenSaatzeile Tag(double wert)
            => new KonditionierungsvorlagenSaatzeile(DbWerte.KOND_ZEILE_TAG, wert, false, null, null);

        private static KonditionierungsvorlagenSaatzeile Nacht(double wert, int von, int bis)
            => new KonditionierungsvorlagenSaatzeile(DbWerte.KOND_ZEILE_NACHT, wert, false, von, bis);

        private static KonditionierungsvorlagenSaatzeile NachtAus(int von, int bis)
            => new KonditionierungsvorlagenSaatzeile(DbWerte.KOND_ZEILE_NACHT, null, true, von, bis);

        private static KonditionierungsvorlagenSaatzeile Wochenende(double wert)
            => new KonditionierungsvorlagenSaatzeile(DbWerte.KOND_ZEILE_WOCHENENDE, wert, false, null, null);

        private static KonditionierungsvorlagenSaatzeile WochenendeAus()
            => new KonditionierungsvorlagenSaatzeile(DbWerte.KOND_ZEILE_WOCHENENDE, null, true, null, null);

        private static KonditionierungsvorlagenSaatzeile Ferien(double wert)
            => new KonditionierungsvorlagenSaatzeile(DbWerte.KOND_ZEILE_FERIEN, wert, false, null, null);

        private static KonditionierungsvorlagenSaatzeile FerienAus()
            => new KonditionierungsvorlagenSaatzeile(DbWerte.KOND_ZEILE_FERIEN, null, true, null, null);

        private const Konditionierungsgroesse H = Konditionierungsgroesse.Heizsoll;
        private const Konditionierungsgroesse K = Konditionierungsgroesse.Kuehlsoll;
        private const Konditionierungsgroesse L = Konditionierungsgroesse.Lueftung;
        private const Konditionierungsgroesse G = Konditionierungsgroesse.Geraete;
        private const Konditionierungsgroesse P = Konditionierungsgroesse.Personen;

        /// <summary>
        /// <b>Die 14 Vorlagen</b> in Schemareihenfolge der Größen, je Liste Wohnen, Büro, Schule — die
        /// Tabelle des Entwurfs KP2, Abschnitt 4, Zelle für Zelle.
        /// </summary>
        public static IReadOnlyList<KonditionierungsvorlagenSaat> Alle { get; } = new[]
        {
            // ---- Heizen [degC] ----
            new KonditionierungsvorlagenSaat(H, WOHNEN, DbWerte.KOND_NUTZUNG_WOHNEN, false,
                Tag(20.0), Nacht(18.0, 22, 6)),
            new KonditionierungsvorlagenSaat(H, BUERO, DbWerte.KOND_NUTZUNG_BUERO, true,
                Tag(20.0), Nacht(16.0, 18, 7), Wochenende(16.0), Ferien(16.0)),
            new KonditionierungsvorlagenSaat(H, SCHULE, DbWerte.KOND_NUTZUNG_SCHULE, true,
                Tag(20.0), Nacht(16.0, 15, 7), Wochenende(16.0), Ferien(16.0)),

            // ---- Kuehlen [degC] ----
            new KonditionierungsvorlagenSaat(K, WOHNEN, DbWerte.KOND_NUTZUNG_WOHNEN, false,
                Tag(26.0), Nacht(28.0, 22, 6)),
            new KonditionierungsvorlagenSaat(K, BUERO, DbWerte.KOND_NUTZUNG_BUERO, true,
                Tag(26.0), NachtAus(18, 7), WochenendeAus(), FerienAus()),
            new KonditionierungsvorlagenSaat(K, SCHULE, DbWerte.KOND_NUTZUNG_SCHULE, true,
                Tag(26.0), NachtAus(15, 7), WochenendeAus(), FerienAus()),

            // ---- Lueftung [1/h] - kein "Wohnen"; der Tagwert bleibt beim Ziel ----
            new KonditionierungsvorlagenSaat(L, BUERO, DbWerte.KOND_NUTZUNG_BUERO, true,
                Nacht(0.1, 18, 7), Wochenende(0.1), Ferien(0.1)),
            new KonditionierungsvorlagenSaat(L, SCHULE, DbWerte.KOND_NUTZUNG_SCHULE, true,
                Nacht(0.1, 15, 7), Wochenende(0.1), Ferien(0.1)),

            // ---- Geraete [Anteil 0 ... 1] ----
            new KonditionierungsvorlagenSaat(G, WOHNEN, DbWerte.KOND_NUTZUNG_WOHNEN, false,
                Tag(1.0)),
            new KonditionierungsvorlagenSaat(G, BUERO, DbWerte.KOND_NUTZUNG_BUERO, true,
                Tag(1.0), Nacht(0.1, 18, 7), Wochenende(0.1), Ferien(0.1)),
            new KonditionierungsvorlagenSaat(G, SCHULE, DbWerte.KOND_NUTZUNG_SCHULE, true,
                Tag(1.0), Nacht(0.1, 15, 7), Wochenende(0.1), Ferien(0.1)),

            // ---- Personen [Anteil 0 ... 1] ----
            new KonditionierungsvorlagenSaat(P, WOHNEN, DbWerte.KOND_NUTZUNG_WOHNEN, false,
                Tag(0.5), Nacht(1.0, 17, 7), Wochenende(1.0)),
            new KonditionierungsvorlagenSaat(P, BUERO, DbWerte.KOND_NUTZUNG_BUERO, true,
                Tag(1.0), Nacht(0.0, 17, 8), Wochenende(0.0), Ferien(0.0)),
            new KonditionierungsvorlagenSaat(P, SCHULE, DbWerte.KOND_NUTZUNG_SCHULE, true,
                Tag(1.0), Nacht(0.0, 14, 8), Wochenende(0.0), Ferien(0.0)),
        };
    }
}
