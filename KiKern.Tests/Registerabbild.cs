using KiKern;

namespace KiKern.Tests
{
    /// <summary>
    /// Ein ABBILD des echten Aktionsregisters aus <c>Allgemein\KI\Aktionen\</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein Abbild und keine Referenz.</b> Das Testprojekt darf
    /// <c>WindowsFormsApplication1</c> nicht referenzieren (COM-Referenzen, MSB4803 -
    /// derselbe Grund wie bei <c>SpeicherEngine.Tests</c>, Fachkonzept 3.7). Der Datensatz
    /// der 20 Anwenderaeusserungen soll aber gegen die WIRKLICHEN Aktionsnamen und
    /// Parameter laufen, sonst prueft er eine Erfindung.
    /// </para>
    /// <para>
    /// <b>Was hier bewusst FEHLT.</b> Die Delegaten <c>Ausfuehren</c>, <c>Vorbedingung</c>
    /// und <c>Vorschau</c>. Sie greifen auf Controller und Datenbank zu und gehoeren nicht
    /// in einen Kerntest; geprueft wird hier ausschliesslich die ABBILDUNG von der
    /// Modellantwort auf den <see cref="KiAufruf"/>. Genau dafuer reichen Name, Zweck,
    /// Stufe und Parameterdeklaration.
    /// </para>
    /// <para>
    /// <b>Abgleich mit dem Original.</b> Namen, Reihenfolge, Typen, Pflichtangaben und
    /// Grenzen sind aus <c>KiAktionenProjekt</c>, <c>KiAktionenWirtschaft</c>,
    /// <c>KiAktionenUebernahme</c>, <c>KiAktionenLastgang</c> und <c>KiAktionenSitzung</c>
    /// uebernommen. Waechst dem Register eine Aktion zu, faellt das hier NICHT von selbst
    /// auf - der Aktionsharnisch (<c>KiHarnisch</c>) laeuft dagegen gegen das echte
    /// Register und deckt die andere Haelfte ab.
    /// </para>
    /// </remarks>
    internal static class Registerabbild
    {
        /// <summary>Persistenzwerte aus <c>DbWerte</c> - deutsch und eingefroren.</summary>
        internal static readonly string[] Komponenten =
        {
            "Wärmepumpe", "Heizkessel", "Photovoltaik", "Solarthermie",
            "Stromspeicher", "Pufferspeicher", "BHKW"
        };

        /// <summary>Schluessel aus <c>KomponentenUebernahmeCtrl.Plaene</c>, ordinal sortiert.</summary>
        internal static readonly string[] Gewerke =
        {
            "BHKW", "Photovoltaik", "Pufferspeicher", "Solarthermie",
            "Spitzenkessel", "Stromspeicher", "Wärmepumpe"
        };

        private static KiParameter ProjektId(bool pflicht = true, double min = 1)
            => new KiParameter("projekt_id", KiParameterTyp.Ganzzahl,
                               "Schlüssel des Projekts, wie ihn projekte_auflisten liefert.",
                               pflicht: pflicht, anzeigename: "Projekt (ID)", min: min);

        /// <summary>Baut das Abbild des vollstaendigen Registers (13 Aktionen, alle Stufe 1).</summary>
        internal static KiRegister Erzeuge()
        {
            var r = new KiRegister();

            r.Aufnehmen(new KiAktion(
                "projekte_auflisten",
                "Listet alle Projekte der Datenbank mit Name, Kunde und Änderungsdatum.",
                Schutzstufe.Lesen, "ProjektCtrl.ReadAll"));

            r.Aufnehmen(new KiAktion(
                "projekt_lesen",
                "Liest die Kopfdaten eines Projekts (Name, Kunde, Bearbeiter, Klimaregion).",
                Schutzstufe.Lesen, "ProjektCtrl.ReadSingle",
                new[] { ProjektId() }));

            r.Aufnehmen(new KiAktion(
                "varianten_auflisten",
                "Listet Stammprojekt und alle Varianten einer Vergleichsgruppe.",
                Schutzstufe.Lesen, "VariantenCtrl.LiesGruppe",
                new[] { ProjektId() }));

            r.Aufnehmen(new KiAktion(
                "speichervarianten_auflisten",
                "Listet die Stromspeicher-Varianten eines Projekts und sagt, welche aktiv ist.",
                Schutzstufe.Lesen, "StromspeicherVariantenCtrl.Lies",
                new[] { ProjektId() }));

            r.Aufnehmen(new KiAktion(
                "ergebnisse_lesen",
                "Liest die gespeicherten Wirtschaftlichkeitsergebnisse mehrerer Projekte.",
                Schutzstufe.Lesen, "WirtschaftlichkeitCtrl.LadeErgebnisse",
                new[]
                {
                    new KiParameter("projekt_ids", KiParameterTyp.GanzzahlListe,
                                    "Schlüssel der Projekte, deren Ergebnisse gelesen werden sollen.",
                                    anzeigename: "Projekte", min: 1)
                }));

            r.Aufnehmen(new KiAktion(
                "wirtschaftlichkeit_parameter_lesen",
                "Liest Wirtschaftlichkeitsparameter und Stromtarif eines Stammprojekts.",
                Schutzstufe.Lesen, "WirtschaftlichkeitCtrl.LadeParameter",
                new[] { ProjektId() }));

            r.Aufnehmen(new KiAktion(
                "kostenlage_pruefen",
                "Vergleicht die erfasste Investitionsposition einer Komponente mit den Technik-Planwerten.",
                Schutzstufe.Lesen, "KostenPositionCtrl.Pruefe",
                new[]
                {
                    ProjektId(),
                    new KiParameter("komponente", KiParameterTyp.Aufzaehlung,
                                    "Kostenkomponente, deren Investitionsposition geprüft wird.",
                                    anzeigename: "Komponente", werte: Komponenten)
                }));

            r.Aufnehmen(new KiAktion(
                "uebernahme_vorschau",
                "Zeigt, was die Übernahme eines Gewerks von einem Projekt in ein anderes ändern würde. Schreibt nichts.",
                Schutzstufe.Lesen, "KomponentenUebernahmeCtrl.Planen",
                new[]
                {
                    new KiParameter("von_projekt", KiParameterTyp.Ganzzahl,
                                    "Schlüssel des Projekts, aus dem übernommen würde.",
                                    anzeigename: "Quellprojekt (ID)", min: 1),
                    new KiParameter("nach_projekt", KiParameterTyp.Ganzzahl,
                                    "Schlüssel des Projekts, in das übernommen würde.",
                                    anzeigename: "Zielprojekt (ID)", min: 1),
                    new KiParameter("gewerk", KiParameterTyp.Aufzaehlung,
                                    "Gewerk der Komponenten-Übernahme.",
                                    anzeigename: "Gewerk", werte: Gewerke)
                }));

            r.Aufnehmen(new KiAktion(
                "merkmal_vorschau",
                "Zeigt, ob und wie ein einzelnes Merkmal von einem Projekt in ein anderes übernommen werden könnte. Schreibt nichts.",
                Schutzstufe.Lesen, "MerkmalUebernahmeCtrl.Pruefe",
                new[]
                {
                    new KiParameter("von_projekt", KiParameterTyp.Ganzzahl,
                                    "Schlüssel des Projekts, aus dem übernommen würde.",
                                    anzeigename: "Quellprojekt (ID)", min: 1),
                    new KiParameter("nach_projekt", KiParameterTyp.Ganzzahl,
                                    "Schlüssel des Projekts, in das übernommen würde.",
                                    anzeigename: "Zielprojekt (ID)", min: 1),
                    new KiParameter("merkmal", KiParameterTyp.Text,
                                    "Schlüssel des Merkmals als Tabelle.Spalte, z. B. Tab_WP.Bauart.",
                                    anzeigename: "Merkmal", maxLaenge: 120)
                }));

            r.Aufnehmen(new KiAktion(
                "lastgang_pruefen",
                "Prüft eine Lastgangdatei: Format, Spalten, Raster und Lesbarkeit. Importiert nichts.",
                Schutzstufe.Lesen, "LastgangImportCtrl.Pruefe",
                new[]
                {
                    new KiParameter("dateipfad", KiParameterTyp.Text,
                                    "Vollständiger Pfad der zu prüfenden Datei (CSV oder Excel).",
                                    anzeigename: "Dateipfad", maxLaenge: 260)
                }));

            r.Aufnehmen(new KiAktion(
                "ganglinien_auflisten",
                "Listet die wählbaren Stromganglinien eines Projekts und des Stammkatalogs.",
                Schutzstufe.Lesen, "GanglinienCtrl.Lies",
                new[] { ProjektId(pflicht: false, min: 0) }));

            r.Aufnehmen(new KiAktion(
                "minimale_spitze_ermitteln",
                "Ermittelt die kleinste Netzbezugsspitze, die ein Speicher über den ganzen Lastgang halten kann.",
                Schutzstufe.Lesen, "PeakShaving.MinimaleSchwelleKw",
                new[]
                {
                    new KiParameter("ganglinie_id", KiParameterTyp.Ganzzahl,
                                    "Schlüssel der Ganglinie aus ganglinien_auflisten.",
                                    anzeigename: "Ganglinie (ID)", min: 1),
                    new KiParameter("kapazitaet_kwh", KiParameterTyp.Zahl,
                                    "Nutzbare Speicherkapazität.",
                                    anzeigename: "Nutzbare Kapazität",
                                    min: 0.001, max: 10000000, einheit: "kWh"),
                    new KiParameter("leistung_kw", KiParameterTyp.Zahl,
                                    "Lade- und Entladeleistung des Speichers.",
                                    anzeigename: "Lade-/Entladeleistung",
                                    min: 0.001, max: 10000000, einheit: "kW"),
                    new KiParameter("wirkungsgrad_rt", KiParameterTyp.Zahl,
                                    "Round-Trip-Wirkungsgrad des Speichers.",
                                    pflicht: false, anzeigename: "Round-Trip-Wirkungsgrad",
                                    min: 0.01, max: 1.0),
                    new KiParameter("soc_min_prozent", KiParameterTyp.Zahl,
                                    "Untere Grenze des nutzbaren Ladebands.",
                                    pflicht: false, anzeigename: "Untere Bandgrenze",
                                    min: 0, max: 100, einheit: "%"),
                    new KiParameter("soc_max_prozent", KiParameterTyp.Zahl,
                                    "Obere Grenze des nutzbaren Ladebands.",
                                    pflicht: false, anzeigename: "Obere Bandgrenze",
                                    min: 0, max: 100, einheit: "%"),
                    ProjektId(pflicht: false, min: 0)
                }));

            r.Aufnehmen(new KiAktion(
                "letzte_aktionen",
                "Nennt die zuletzt ausgeführten Assistentenaktionen dieser Sitzung.",
                Schutzstufe.Lesen, "KiAusfuehrer.LetzteAktionen",
                new[]
                {
                    new KiParameter("anzahl", KiParameterTyp.Ganzzahl,
                                    "Wie viele der zuletzt ausgeführten Aktionen genannt werden sollen.",
                                    pflicht: false, anzeigename: "Anzahl", min: 1, max: 50)
                }));

            return r;
        }

        /// <summary>
        /// Dasselbe Register, zusaetzlich mit je einer Aktion der Stufen 2 und 3.
        /// </summary>
        /// <remarks>
        /// Heute ist NUR Stufe 1 registriert - der Schutzstufen-Riegel liesse sich am
        /// echten Register also gar nicht pruefen. Diese beiden Aktionen sind die
        /// Vorwegnahme aus Fachkonzept 5.2 und 5.3 und dienen ausschliesslich dem Nachweis,
        /// dass der Riegel greift, BEVOR es die Bestaetigungsschicht gibt.
        /// </remarks>
        internal static KiRegister MitHoeherenStufen()
        {
            KiRegister r = Erzeuge();

            r.Aufnehmen(new KiAktion(
                "variante_anlegen",
                "Legt eine neue Variante zu einem Stammprojekt an.",
                Schutzstufe.Schreiben, "VariantenCtrl.AnlegenAusStamm",
                new[]
                {
                    ProjektId(),
                    new KiParameter("bezeichner", KiParameterTyp.Text,
                                    "Name der neuen Variante.", anzeigename: "Bezeichner", maxLaenge: 60)
                },
                wirkung: "Legt einen neuen Datensatz an.",
                vorschau: _ => "Ich würde eine Variante anlegen."));

            r.Aufnehmen(new KiAktion(
                "simulation_rechnen",
                "Rechnet die Simulation eines Projekts.",
                Schutzstufe.Rechnen, "SimulationRunner.Lauf",
                new[] { ProjektId() },
                wirkung: "Rechnet und speichert das Ergebnis.",
                vorschau: _ => "Ich würde rechnen."));

            return r;
        }
    }
}
