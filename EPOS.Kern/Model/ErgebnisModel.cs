using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Ergebnis-Datenmodell (Kopf + Detailmodelle je Simulationsart).
    // Pro Simulationsart eine eigene Detailklasse/-tabelle, da die Ergebnisgroessen
    // stark variieren. Fuer Auswertungen/Berichte ist damit "alles zu Waermepumpe"
    // usw. gezielt abfragbar.
    // ---------------------------------------------------------------------------

    // Kopf eines Simulationslaufs (eine Zeile in Tab_Ergebnis).
    public class ErgebnisModel
    {
        public int ID;
        public int ID_Projekt;
        public string Bezeichner = "";
        public DateTime Zeitstempel;
        public int ID_Klimaregion;

        // Welche Simulationsarten dieser Lauf enthaelt.
        public bool Sim_Energiebedarf;
        public bool Sim_Waermepumpe;
        public bool Sim_Heizkessel;
        public bool Sim_Solarthermie;
        public bool Sim_BHKW;
        public bool Sim_PV;
        public bool Sim_Stromspeicher;

        // Detailergebnisse je Art (null = fuer diesen Lauf nicht vorhanden).
        public ErgebnisEnergiebedarfModel Energiebedarf;
        public ErgebnisWaermepumpeModel Waermepumpe;
        public ErgebnisBHKWModel BHKW;
        public ErgebnisHeizkesselModel Heizkessel;
        public ErgebnisSolarthermieModel Solarthermie;
        public ErgebnisPhotovoltaikModel Photovoltaik;

        // Pufferspeicher des Laufs (Tab_ErgebnisPufferspeicher, Konzept 6.6):
        // eine Zeile je beteiligtem Speicher - Senkenspeicher UND Quellspeicher.
        // Leere Liste = dieser Lauf hatte keinen Speicher.
        public List<ErgebnisPufferspeicherModel> Pufferspeicher = new List<ErgebnisPufferspeicherModel>();

        // Stromspeicher des Laufs (Tab_ErgebnisStromspeicher, Fachkonzept
        // Stromspeicher 7.1): eine Zeile je gerechneter Speicheranlage.
        // Leere Liste = dieser Lauf hatte keinen Stromspeicher; das Flag
        // Sim_Stromspeicher sagt, ob die Speicherrechnung ueberhaupt lief.
        public List<ErgebnisStromspeicherModel> Stromspeicher = new List<ErgebnisStromspeicherModel>();

        // Gebaeude des Laufs (Tab_ErgebnisGebaeude, Entscheid E30): eine Zeile je
        // Gebaeude mit Rechenweg und Kennzahlen, nach Merkplatz geordnet. Leere Liste =
        // der Lauf hatte kein Gebaeude (oder die Datenbank steht vor Schritt 107).
        public List<ErgebnisGebaeudeModel> Gebaeude = new List<ErgebnisGebaeudeModel>();

        public ErgebnisModel()
        {
            Zeitstempel = DateTime.Now;
        }
    }

    /// <summary>
    /// Detail: <b>ein Gebäude des Laufs</b> (<c>Tab_ErgebnisGebaeude</c>, Entscheid E30,
    /// Konzept Gebäudesimulation N1.35). Die Einheit steht im Namen. Wärmebedarf und die drei
    /// Spitzenwerte haben beide Rechenwege; die übrigen Größen gibt es nur auf dem VDI-Weg —
    /// auf dem Tagesbilanz-Weg sind sie <c>null</c> („nicht gerechnet", nie 0).
    /// </summary>
    public class ErgebnisGebaeudeModel
    {
        /// <summary>Die Gebäudezeile des Projekts (<c>Tab_Gebaeude.ID</c>).</summary>
        public int ID_Gebaeude;

        /// <summary>Der Merkplatz des Gebäudes im Lauf (ab 0) — der Index <c>n</c> von <c>Geb[n]</c> im Referenzlauf.</summary>
        public int Merkplatz;

        /// <summary>Der Gebäudename der Projektkopie.</summary>
        public string Gebaeudename = "";

        /// <summary>Der wirksame Rechenweg (<c>DbWerte.GEBAEUDE_MODELL_*</c>).</summary>
        public string Rechenweg = "";

        /// <summary>Heizwärme des Gebäudes im Jahr [MWh] — sein Anteil am Heizkanal.</summary>
        public double HeizwaermeMwh;

        /// <summary>Höchste Stundenlast [kW].</summary>
        public double SpitzeKw;

        /// <summary>Größtes gleitendes Mittel über 24 Stunden [kW].</summary>
        public double SpitzeTagesmittelKw;

        /// <summary>95-%-Quantil der Stundenlast nach nächstgelegenem Rang [kW].</summary>
        public double Spitze95Kw;

        /// <summary>
        /// Kühlbedarf [MWh]; nur VDI-Weg. Mit wirksamer Kühlung der Kältebedarf des Gebäudes am
        /// Kühlsollwert, sonst die Wärme, die bis zur oberen Raumtemperatur abzuführen wäre.
        /// </summary>
        public double? KuehlenergieMwh;

        /// <summary>Stunden mit Kühlbedarf [h]; nur VDI-Weg.</summary>
        public int? KuehlstundenH;

        /// <summary>Mittlere Raumlufttemperatur über die Nutzungszeit [°C]; nur VDI-Weg.</summary>
        public double? MittlereRaumtemperaturC;

        /// <summary>Stunden der Nutzungszeit über der oberen Raumtemperatur [h]; nur VDI-Weg.</summary>
        public int? UeberhitzungsstundenH;

        /// <summary>Stunden mit eingeschalteter Sommerlüftung [h]; nur VDI-Weg.</summary>
        public int? SommerlueftungsstundenH;

        /// <summary>Die obere Raumtemperatur, gegen die die Überhitzung gezählt ist [°C]; nur VDI-Weg.</summary>
        public double? ObereRaumtemperaturC;

        // ---- Anlagenkopplung (AK1): der Heizkreis je Gebäude (Schemaschritt 128) -----------
        //
        // Die drei Größen der Projektzeile (Schritt 123) je Gebäude — gebildet aus dem
        // Heizkreis des Gebäudes — und die Übergabeart, mit der es gekoppelt gerechnet hat.
        // ErgebnisCtrl legt sie nach Tab_ErgebnisGebaeude (Muster E30). null ohne wirksame
        // Kopplung („nicht gekoppelt gerechnet").

        /// <summary>
        /// Die Übergabeart des gekoppelt gerechneten Gebäudes (<c>DbWerte.UEBERGABE_*</c>);
        /// <c>null</c> = nicht gekoppelt — zugleich die Kennung der Kopplung in der Ergebniszeile.
        /// </summary>
        public string UebergabeArt;

        /// <summary>Hat das Gebäude gekoppelt gerechnet (Anlagenkopplung AK1)?</summary>
        public bool IstGekoppelt => !string.IsNullOrEmpty(UebergabeArt);

        /// <summary>Heizzeitgewichtetes Mittel des Vorlaufs [°C]; nur mit wirksamer Kopplung.</summary>
        public double? VorlaufMittelC;

        /// <summary>Heizzeitgewichtetes Mittel des Rücklaufs [°C]; nur mit wirksamer Kopplung.</summary>
        public double? RuecklaufMittelC;

        /// <summary>Stunden, in denen die Übergabe die Grenze war [h]; nur mit wirksamer Kopplung.</summary>
        public double? UebergabeBegrenztStundenH;

        // ---- Kälteseite der Kopplung (E37, KAK-S3) — dasselbe Muster: null ohne Kühlkopplung ----

        /// <summary>
        /// Die Kühlübergabeart des kühlgekoppelt gerechneten Gebäudes (<c>DbWerte.KUEHLUEBERGABE_*</c>);
        /// <c>null</c> = nicht kühlgekoppelt — zugleich die Kennung in der Ergebniszeile.
        /// </summary>
        public string KuehlUebergabeArt;

        /// <summary>Hat das Gebäude kühlgekoppelt gerechnet (Kälteseite E37)?</summary>
        public bool IstKuehlgekoppelt => !string.IsNullOrEmpty(KuehlUebergabeArt);

        /// <summary>Kältebedarfsgewichtetes Mittel des Kaltwasser-Vorlaufs [°C]; nur mit Kühlkopplung.</summary>
        public double? KuehlVorlaufMittelC;

        /// <summary>Kältebedarfsgewichtetes Mittel des Rücklaufs [°C]; nur mit Kühlkopplung.</summary>
        public double? KuehlRuecklaufMittelC;

        /// <summary>Stunden, in denen die Kühlübergabe die Grenze war [h], einschließlich der Vorlaufgrenze; nur mit Kühlkopplung.</summary>
        public double? KuehlUebergabeBegrenztStundenH;

        /// <summary>Davon die Stunden an der Vorlaufgrenze [h] (7.2); nur mit Kühlkopplung.</summary>
        public double? KuehlVorlaufgrenzeStundenH;

        /// <summary>Rechnet das Gebäude auf dem VDI-Weg?</summary>
        public bool IstVdi6007 => Rechenweg == DbWerte.GEBAEUDE_MODELL_VDI6007;

        /// <summary>
        /// Die Zonen eines Mehrzonengebäudes (Stufe G6b, <c>Tab_ErgebnisZone</c>, Anwenderentscheid A6):
        /// eine Zeile je Zone, nur Skalare, nach Rang; leer bei höchstens einer Zone.
        /// </summary>
        public List<ErgebnisZoneModel> Zonen = new List<ErgebnisZoneModel>();
    }

    /// <summary>
    /// EINE Zone eines Mehrzonengebäudes im Ergebnis (Stufe G6b, <c>Tab_ErgebnisZone</c>, A6). NULL heißt
    /// „nicht gerechnet": die Energiespalten einer unbeheizten Zone (A2), die Kühlenergie ohne wirksame
    /// Kühlung, Δϑ_max einer Zone ohne Nachbarzone.
    /// </summary>
    public class ErgebnisZoneModel
    {
        /// <summary>Die Zone (<c>Tab_Zone.ID</c>); <c>null</c>, wenn sie inzwischen gelöscht ist.</summary>
        public int? ID_Zone;

        /// <summary>Die Reihenfolge der Zone im Gebäude, ab 1.</summary>
        public int Rang;

        /// <summary>Der Name der Zone zum Zeitpunkt des Laufs.</summary>
        public string Bezeichner = "";

        /// <summary>Wurde die Zone beheizt?</summary>
        public bool IstBeheizt;

        /// <summary>Jahresheizwärme [MWh]; <c>null</c> für eine unbeheizte Zone.</summary>
        public double? HeizwaermeMwh;

        /// <summary>Spitzenheizlast [kW]; <c>null</c> für eine unbeheizte Zone.</summary>
        public double? SpitzeKw;

        /// <summary>Jahreskühlenergie [MWh]; <c>null</c> ohne wirksame Kühlung.</summary>
        public double? KuehlenergieMwh;

        /// <summary>Mittlere Raumlufttemperatur in der Nutzungszeit [°C].</summary>
        public double? MittlereRaumtemperaturC;

        /// <summary>Stunden der Nutzungszeit über der oberen Raumtemperatur der Zone [h] (RS 8.2, E32).</summary>
        public int? UeberhitzungsstundenH;

        /// <summary>Größter Abstand der Raumluft zu einer Nachbarzone über das Jahr [K]; <c>null</c> ohne Nachbarzone.</summary>
        public double? DeltaThetaMaxK;

        /// <summary>Höchstzahl der Durchläufe der Zonenschleife in einer Stunde.</summary>
        public int? DurchlaeufeMax;

        /// <summary>Stunden, in denen das Muster des ersten Durchlaufs gehalten wurde [h].</summary>
        public int? MusterwechselH;
    }

    // Detail: Waerme-/Strombedarf (Tab_ErgebnisEnergiebedarf).
    public class ErgebnisEnergiebedarfModel
    {
        public double Waermebedarf_Gesamt;   // MWh
        public double Waermelast_Max;         // kW
        public double Strombedarf_Gesamt;     // MWh
        public double Strombedarf_Max;        // kW
        public double Waermerestbedarf;       // MWh (Restwärmebedarf nach allen Erzeugern, sim.RestwaermeMwh)
        public double Stromrestbedarf;        // MWh (Reststrombedarf/Netzbezug, sim.ReststromMwh)

        /// <summary>
        /// PAKET E1 (Konzept 4.4): Jahres-Wärmebedarf JE KANAL [MWh], indiziert mit
        /// <see cref="Kanal.HEIZUNG"/>/<c>BRAUCHWASSER</c>/<c>PROZESS</c>.
        ///
        /// <para>Die AUFSCHLÜSSELUNG von <see cref="Waermebedarf_Gesamt"/>, nicht eine
        /// zweite Rechnung: Quelle ist derselbe Kanalsatz, aus dem seit Paket K1 auch der
        /// Summenvektor gebildet wird (<c>SimulationWaermebedarf.KanaeleDrei</c> ist die
        /// FÜHRENDE Größe, die Summe die abgeleitete). Ihre Summe ist deshalb der
        /// Gesamtbedarf — bis auf die Rundung, mit der die Kanäle stundenweise zum
        /// Summenvektor addiert werden (Konzept 4.2, 1-ULP-Klasse; seit W8‑O‑5d ist das
        /// eine <c>double</c>-, keine <c>float</c>-Rundung mehr).</para>
        ///
        /// <para>Spalten <c>Waermebedarf_Heizung/_Brauchwasser/_Prozess</c>, angelegt in
        /// Migrationsschritt 52. Der vierte Eintrag (<see cref="Kanal.KUEHLUNG"/>) ist der
        /// Kältebedarf des Kühlkanals [MWh] — Spalte <c>Waermebedarf_Kuehlung</c>
        /// (Schemaschritt 110, KU-S4; Name nach dem Bestandsmuster, K13). Er geht NICHT in
        /// <see cref="Waermebedarf_Gesamt"/> ein und wird nur geschrieben, wenn der Lauf Kälte
        /// ERHOBEN hat (<see cref="KaelteErhoben"/>); sonst steht die Spalte auf NULL.</para>
        /// </summary>
        public double[] Waermebedarf_Kanal = new double[Kanal.ANZAHL];

        // ---- Die Kälteseite (Schemaschritt 110, KU-S4; Kühlkonzept 7.4, E21) ----------
        //
        // null heißt „nicht erhoben" — das Projekt rechnet keine Kälte
        // (Tab_Einstellungen.Kuehlbetrieb = 0). Ein Wert, auch 0, heißt „erhoben". Die
        // Unterscheidung ist Pflicht, nicht Zier: Der Referenzlauf-Export nimmt eine NULL-
        // Spalte nicht in die Kennzahlendatei auf, und ein Projekt ohne Kühlung bleibt so
        // byte-gleich (Kühlkonzept 10.5).

        /// <summary>
        /// Jahreskältebedarf [MWh] — Gegenstück zu <see cref="Waermebedarf_Gesamt"/>, Summe von
        /// <c>Kanalsatz.SummeKaelte()</c>; Nenner des Deckungsgrads der Kälteseite (6.4).
        /// Solange nur ein Kältekanal besteht, wertgleich mit
        /// <c>Waermebedarf_Kanal[Kanal.KUEHLUNG]</c>.
        /// </summary>
        public double? Kaeltebedarf_Gesamt;

        /// <summary>Kältespitze [kW] — Gegenstück zu <see cref="Waermelast_Max"/>, gesetzt aus <c>SimulationKaeltebedarf.Kaeltebedarf_Max</c>.</summary>
        public double? Kaeltelast_Max;

        /// <summary>
        /// Ungedeckte Kälte [MWh] — Gegenstück zu <see cref="Waermerestbedarf"/>. In KU1 deckt
        /// niemand Kälte: Der Rest ist der ganze Bedarf, benannt als Warnung im Protokoll
        /// (F-K12).
        /// </summary>
        public double? Kaelterestbedarf;

        /// <summary>Hat der Lauf Kälte erhoben? Bestimmt, ob die Kältespalten Werte oder NULL tragen.</summary>
        public bool KaelteErhoben => Kaeltebedarf_Gesamt.HasValue;

        // ---- Anlagenkopplung, Wärmeteil (Schemaschritt 123, AK-S3; Anlagenkopplung 8.3) --
        //
        // null heißt „nicht erhoben" — kein Gebäude des Laufs rechnete gekoppelt. Wie bei der
        // Kälteseite nimmt der Referenzlauf-Export eine NULL-Spalte nicht auf, und ein Projekt
        // ohne Kopplung schreibt dieselben Zeilen wie vorher.

        /// <summary><c>Vorlauf_Mittel</c> [°C]: Mittel des gefahrenen Vorlaufs über die Stunden mit gekoppeltem Bedarf.</summary>
        public double? VorlaufMittelC;

        /// <summary><c>Ruecklauf_Mittel</c> [°C]: dasselbe für den Rücklauf.</summary>
        public double? RuecklaufMittelC;

        /// <summary><c>Uebergabe_Begrenzt_Stunden</c> [h]: Stunden, in denen die Übergabe (mindestens eines Gebäudes) die Grenze war.</summary>
        public double? UebergabeBegrenztStundenH;

        // ---- Anlagenkopplung, Kälteseite (E37, KAK-S3) — null heißt „nicht erhoben" ----

        /// <summary><c>Kuehl_Vorlauf_Mittel</c> [°C]: kältebedarfsgewichtetes Mittel des Kaltwasser-Vorlaufs über die Stunden mit kühlgekoppeltem Bedarf.</summary>
        public double? KuehlVorlaufMittelC;

        /// <summary><c>Kuehl_Ruecklauf_Mittel</c> [°C]: dasselbe für den Rücklauf.</summary>
        public double? KuehlRuecklaufMittelC;

        /// <summary><c>Kuehl_Uebergabe_Begrenzt_Stunden</c> [h]: Stunden, in denen die Kühlübergabe (mindestens eines Gebäudes) die Grenze war.</summary>
        public double? KuehlUebergabeBegrenztStundenH;
    }

    // Detail: Waermepumpe-Aggregat (Tab_ErgebnisWaermepumpe) + Modulliste.
    public class ErgebnisWaermepumpeModel
    {
        public double Waermebedarf;               // MWh/a
        public double Restwaermebedarf;           // MWh/a
        public double Waermeproduktion_WP;        // MWh/a
        public double Stromverbrauch_WP;          // MWh/a
        public double Stromverbrauch_Heizstab;    // MWh/a
        public double Kapazitaet_Pufferspeicher;  // kWh
        public double Min_Spitzenkesselleistung;  // kW
        public double Waermebedarfsdeckung;       // %
        public double Vollbenutzungsstunden;      // h/a
        public double? Bivalenzpunkt;             // Grad C (null = kein Bivalenzpunkt)

        /// <summary>
        /// PAKET E1 (Konzept 4.4): Wärmebedarfsdeckung dieses Erzeugers JE KANAL [%],
        /// indiziert mit <see cref="Kanal.HEIZUNG"/>/<c>BRAUCHWASSER</c>/<c>PROZESS</c>.
        ///
        /// <para><b>Es ist die AUFSCHLÜSSELUNG von <see cref="Waermebedarfsdeckung"/></b>,
        /// nicht der Deckungsgrad des einzelnen Kanals: gleicher Nenner (Wärmebedarf des
        /// PROJEKTS), gleiche Eigenanteils-Logik des Runners, nur der Zähler ist
        /// kanalindiziert (Direktdeckung + zugerechnete Speicherentladung + Heizstab je
        /// Kanal — die Buchführung aus Paket K2). Die Summe der drei Werte IST der
        /// Skalar; darauf normiert <c>SimulationRunner.DeckungJeKanal</c>.
        /// Dieselbe Bedeutung haben die gleichnamigen Felder der Modelle für BHKW,
        /// Heizkessel und Solarthermie.</para>
        ///
        /// <para>Der Deckungsgrad EINES Kanals („die WP deckt 80 % des
        /// Brauchwasserbedarfs") ergibt sich daraus zusammen mit
        /// <see cref="ErgebnisEnergiebedarfModel.Waermebedarf_Kanal"/>:
        /// <c>Deckung_Kanal[k] · Waermebedarf_Gesamt / Waermebedarf_Kanal[k]</c>.</para>
        ///
        /// <para>Spalten <c>Deckung_Heizung/_Brauchwasser/_Prozess</c>, angelegt in
        /// Migrationsschritt 52.</para>
        /// </summary>
        public double[] Deckung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// STUFE KU2 WELLE 3 (Schemaschritt 119; Kühlkonzept 6.4, 7.4, E21): die gedeckte Kälte aller
        /// Wärmepumpen [MWh/a] — Gegenstück zu <see cref="Waermeproduktion_WP"/>. <c>null</c> = keine
        /// Kälteerzeugung gerechnet (kein Kälteerzeuger im Lauf); Spalte <c>Kaelteproduktion_WP</c>.
        /// </summary>
        public double? Kaelteproduktion_WP;

        /// <summary>
        /// Der Kältestrom aller Wärmepumpen samt Hilfsstrom [MWh/a] (Kühlkonzept 6.1) — Gegenstück zu
        /// <see cref="Stromverbrauch_WP"/>, der ihn nicht enthält. <c>null</c> wie
        /// <see cref="Kaelteproduktion_WP"/>; Spalte <c>Stromverbrauch_Kuehlung</c>.
        /// </summary>
        public double? Stromverbrauch_Kuehlung;

        public List<ErgebnisWaermepumpeModulModel> Module = new List<ErgebnisWaermepumpeModulModel>();
    }

    // Eine WP-Modulzeile (Tab_ErgebnisWaermepumpeModul).
    public class ErgebnisWaermepumpeModulModel
    {
        public string Modul = "";
        public double Leistung;           // kW
        public double Waermeproduktion;   // MWh/a
        public double Stromverbrauch;     // MWh/a
        public double Heizstab;           // MWh/a
        public double Betriebsstunden;    // h/a

        // ---- Kälteseite (Schemaschritt 119; Kühlkonzept 6.1, 8.4; E34) - null = keine
        //      Kälteerzeugung gerechnet (kein Kälteerzeuger im Lauf).

        /// <summary>Gedeckte Kälte der Anlage [MWh/a] — Gegenstück zu <see cref="Waermeproduktion"/>.</summary>
        public double? Kaelteproduktion;

        /// <summary>Kältestrom der Anlage samt Hilfsstrom [MWh/a] — Gegenstück zu <see cref="Stromverbrauch"/>.</summary>
        public double? Stromverbrauch_Kuehlung;

        /// <summary>
        /// Der Netzbezug, der dem Kältestrom der Anlage zukommt [MWh/a] (E34): anteilig am Netzbezug
        /// ein Teil von <c>Stromrestbedarf</c>, mit eigenem Zähler der ganze Kältestrom daneben.
        /// </summary>
        public double? Kaeltestrom_Netzbezug;

        /// <summary>Der abweichende Kühlträger des Laufs (<c>energy_carrier.id</c>); <c>null</c> = Stromträger des Projekts.</summary>
        public int? Kuehl_CarrierId;

        /// <summary>Die Abrechnungsart des Laufs bei abweichendem Kühlträger: <c>true</c> = eigener Zähler; <c>null</c>/<c>false</c> = anteilig.</summary>
        public bool? Kuehl_EigenerZaehler;
    }

    // Detail: BHKW-Aggregat (Tab_ErgebnisBHKW) + Modulliste.
    public class ErgebnisBHKWModel
    {
        public double Waermebedarf;                 // MWh/a
        public double Restwaermebedarf;             // MWh/a
        public double Strombedarf;                  // MWh/a
        public double Reststrombedarf;              // MWh/a
        public double Waermeproduktion;             // MWh/a
        public double Waermeueberschuss;            // MWh/a
        public double Stromproduktion;              // MWh/a
        // ETAPPE E2 - WAS DIESE BEIDEN FELDER SIND (und was nicht):
        // Betriebsstunden_Gesamt ist die SUMME der THERMISCHEN Vollbenutzungsstunden
        // ueber alle Module (SimulationBHKW.Laufzeiten[] = Waerme / Waermeleistung),
        // KEINE Betriebsstundenzahl - Taktung bildet das Modell nicht ab. Die Summe
        // kann 8.760 h ueberschreiten, sobald mehr als ein Modul laeuft. Die Feldnamen
        // bleiben, weil die gleichnamigen Spalten in Tab_ErgebnisBHKW seit jeher so
        // heissen; die ANZEIGE benennt sie seit E2 richtig.
        public double Betriebsstunden_Gesamt;       // h/a (Summe THERMISCHER Vbh je Modul)
        public double Betriebsstunden_Durchschnitt; // h/a (Mittel THERMISCHER Vbh je Modul)

        /// <summary>
        /// ETAPPE E2 - LEISTUNGSGEWICHTETE elektrische Vollbenutzungsstunden [h/a]:
        /// Summe Stromproduktion [MWh] x 1000 / Summe P_el [kW] aller Module.
        ///
        /// <para>Die massgebliche Groesse fuer die KWKG-Deckelung (der Zuschlag haengt
        /// an KWK-STROM) und die einzige der drei Vbh-Groessen dieser Zeile, die
        /// 8.760 h nicht ueberschreiten kann.</para>
        ///
        /// <para>0 = nicht erhoben (Ergebniszeile vor Etappe E2) oder keine elektrische
        /// Nennleistung gepflegt. Die Leseseite behandelt beides gleich.</para>
        /// </summary>
        public double VbhElektrisch;                // h/a
        public double Waermebedarfsdeckung;         // %
        public double Strombedarfsdeckung;          // %
        public double Gasverbrauch;
        public double Oelverbrauch;
        public double Koks;
        public double Rapsoelverbrauch;
        public double Holzverbrauch;
        public double Kohle;
        public double Sonstigverbrauch;
        public double Pellets;
        public double TierischeFette;

        /// <summary>PAKET E1: Deckung je Kanal [%] — Bedeutung und Normierung wie bei
        /// <see cref="ErgebnisWaermepumpeModel.Deckung_Kanal"/>.</summary>
        public double[] Deckung_Kanal = new double[Kanal.ANZAHL];

        public List<ErgebnisBHKWModulModel> Module = new List<ErgebnisBHKWModulModel>();
    }

    // Eine BHKW-Modulzeile (Tab_ErgebnisBHKWModul).
    public class ErgebnisBHKWModulModel
    {
        public string Modul = "";
        public double Waermeproduktion;   // MWh/a
        public double Stromproduktion;    // MWh/a
        public string Brennstoff = "";
        public double Verbrauch = 0.0;          // MWh/a
        public int CarrierId;             // energy_carrier.id (0 = keine Zuordnung)

        /// <summary>
        /// ETAPPE E2 - THERMISCHE Vollbenutzungsstunden dieses Moduls [h/a]:
        /// Waermeproduktion [MWh] x 1000 / P_therm [kW] (SimulationBHKW.Laufzeiten[i]).
        ///
        /// <para><b>Ausdruecklich KEINE Betriebsstundenzahl.</b> Der Rechenkern kennt
        /// keine Taktung: Ein Modul, das ein Jahr lang halb moduliert laeuft, hat
        /// 8.760 Betriebsstunden, aber 4.380 thermische Vbh. Die Groesse heisst deshalb
        /// so, wie sie gebildet wird - wer spaeter eine Wartung „je Betriebsstunde"
        /// darauf bemisst (Etappe E3), rechnet mit einer NAEHERUNG und muss das
        /// wissen.</para>
        ///
        /// <para>0 = nicht erhoben (Zeile vor Etappe E2) oder keine Waermeleistung
        /// gepflegt.</para>
        /// </summary>
        public double VbhThermisch;       // h/a

        /// <summary>
        /// ETAPPE E2 - ELEKTRISCHE Vollbenutzungsstunden dieses Moduls [h/a]:
        /// Stromproduktion [MWh] x 1000 / P_el [kW]. Bemessungsgrundlage des
        /// KWK-Zuschlags; Etappe E6 deckelt damit modulscharf. Kann 8.760 h nicht
        /// ueberschreiten.
        ///
        /// <para>0 = nicht erhoben (Zeile vor Etappe E2) oder P_el nicht gepflegt.</para>
        /// </summary>
        public double VbhElektrisch;      // h/a

        /// <summary>
        /// ETAPPE B3 Paket a - Hilfsenergie dieses Moduls [MWh/a] (Konzept
        /// <c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan</c> Paragraf 4.5 und 5.2).
        ///
        /// <para><b>Bleibt 0, bis Paket b sie bildet.</b> Paket a legt allein die Spalte
        /// an und schreibt sie mit - keine Rechnung liest sie. In Paket b wird sie zur
        /// Bezugsgroesse der Nettostromerzeugung
        /// (Stromproduktion minus Hilfsstrom).</para>
        ///
        /// <para>0 = nicht erhoben (Zeile vor Etappe B3) oder kein Hilfsenergieanteil
        /// gepflegt - beides ist rechnerisch dasselbe.</para>
        /// </summary>
        public double Hilfsenergie;       // MWh/a
    }

    // Detail: Heizkessel/Spitzenkessel-Aggregat (Tab_ErgebnisHeizkessel) + Modulliste.
    public class ErgebnisHeizkesselModel
    {
        public double Waermebedarf;             // MWh/a
        public double Restwaermebedarf;         // MWh/a
        public double Waermeproduktion;         // MWh/a (Waerme Spitzenkessel)
        public double Strombedarf;              // MWh/a
        public double Reststrombedarf;          // MWh/a
        public double Waermebedarfsdeckung;     // %
        public double Stromverbrauch;           // MWh/a (Hilfsstrom Kessel)
        public double Maximale_Kesselleistung;  // kW
        public double Gasspitze;                // kW

        // ETAPPE D4: Wärme, die die Kessel in der Kaskade aus ihrem QUELLPUFFER bezogen
        // haben (SimulationSPK.QuellwaermeGesamtKwh, hier in MWh/a wie die übrigen
        // Wärmegrößen dieser Zeile). Ohne Quellbezug exakt 0.
        public double Quellwaerme;              // MWh/a
        // Brennstoffverbrauch je Traeger (MWh/a)
        public double Gasverbrauch;
        public double Oelverbrauch;
        public double Koks;
        public double Rapsoelverbrauch;
        public double Holzverbrauch;
        public double Kohle;
        public double Sonstigverbrauch;
        public double Pellets;
        public double TierischeFette;

        /// <summary>PAKET E1: Deckung je Kanal [%] — Bedeutung und Normierung wie bei
        /// <see cref="ErgebnisWaermepumpeModel.Deckung_Kanal"/>.</summary>
        public double[] Deckung_Kanal = new double[Kanal.ANZAHL];

        public List<ErgebnisHeizkesselModulModel> Module = new List<ErgebnisHeizkesselModulModel>();
    }

    // Eine Heizkessel-Modulzeile (Tab_ErgebnisHeizkesselModul).
    public class ErgebnisHeizkesselModulModel
    {
        public string Modul = "";
        public double Waerme_Gas;          // MWh/a (Gas/Biogas/Rapsoel/Holz...)
        public double Waerme_Oel;          // MWh/a
        public double Waermeproduktion;   // MWh/a

        /// <summary>
        /// Das BRENNSTOFFWORT dieses Kessels (Gas, Öl, Koks, Kohle, Holz, Strom,
        /// Pellets, Rapsöl, Tierische Fette, Sonstige) — gebildet in
        /// <c>SimulationSPK.BrennstoffWort</c> aus derselben Bereichsverzweigung, die
        /// den Verbrauch auf die Anlagenzähler bucht. Persistenzwert, immer deutsch;
        /// leer = Zeile vor Befund B-1 oder aus der Rücklesung ohne Wort.
        /// </summary>
        public string Brennstoff = "";

        /// <summary>
        /// Brennstoffeinsatz DIESES Kessels [MWh/a] — die Endenergie, aus der
        /// Energiekosten, CO₂-Bilanz und BEHG-Abgabe entstehen
        /// (<c>SimulationSPK.Kessel_Verbrauch_MWh_Spk</c>, übernommen im
        /// <c>SimulationRunner</c>).
        ///
        /// <para><b>0 heißt zweierlei</b> und die Leser müssen beides vertragen:
        /// ein ELEKTROKESSEL (er bucht auf den Stromzähler und steht im Netzbezug —
        /// <c>SimulationSPK.IstStromkessel</c>), oder eine GESPEICHERTE Zeile aus einem
        /// Lauf vor Befund B-1, in der die Spalte nie gefüllt wurde.</para>
        /// </summary>
        public double Verbrauch = 0.0;          // MWh/a
        public int CarrierId;              // energy_carrier.id (0 = keine Zuordnung)

        /// <summary>
        /// Jahresnutzungsgrad dieses Kessels in PROZENT (nicht als Anteil):
        /// <c>(Waerme_Gas + Waerme_Oel) / Brennstoffeinsatz x 100</c>, gebildet in
        /// <c>SimulationSPK.Bilanz_und_Nutzungsgrad</c> und dort auf 1..108 % geklemmt.
        /// 0 = der Kessel stand still oder der Wert wurde nicht erhoben.
        ///
        /// <para><b>Die Einheit ist tragend</b> (Etappe B3 Paket a): Aus ihr leitet
        /// <c>HilfsstromRechner.KesselBrennstoffMWh</c> die Bemessungsmenge des
        /// Paragrafen 54 zurueck (<c>(Waerme_Gas + Waerme_Oel) / (Jahresnutzungsgrad /
        /// 100)</c>), wo <see cref="Verbrauch"/> nicht steht — seit Befund B-1 sind das
        /// gespeicherte Altzeilen und der Elektrokessel.</para>
        /// </summary>
        public double Jahresnutzungsgrad;  // %

        /// <summary>
        /// ETAPPE B3 Paket a - Hilfsenergie dieses Kessels [MWh/a]; Bedeutung, Herkunft
        /// und Vorbelegung wie bei <see cref="ErgebnisBHKWModulModel.Hilfsenergie"/>.
        /// </summary>
        public double Hilfsenergie;        // MWh/a
    }

    // Detail: Solarthermie-Aggregat (Tab_ErgebnisSolarthermie) + Kollektor-Liste.
    public class ErgebnisSolarthermieModel
    {
        public double Waermebedarf;          // MWh/a
        public double Restwaermebedarf;      // MWh/a
        public double Waermeproduktion;      // MWh/a (Gesamte Waermeleistung der Module)
        public double Waermebedarfsdeckung;  // %
        public double Ueberschuss;           // MWh/a

        /// <summary>PAKET E1: Deckung je Kanal [%] — Bedeutung und Normierung wie bei
        /// <see cref="ErgebnisWaermepumpeModel.Deckung_Kanal"/>.</summary>
        public double[] Deckung_Kanal = new double[Kanal.ANZAHL];

        public List<ErgebnisSolarthermieModulModel> Module = new List<ErgebnisSolarthermieModulModel>();
    }

    // Eine Solarkollektor-Zeile (Tab_ErgebnisSolarthermieModul).
    public class ErgebnisSolarthermieModulModel
    {
        public string Modul = "";
        public double Flaeche;           // m^2 (Aperturflaeche gesamt)
        public long Anzahl;
        public double Waermeproduktion;  // MWh/a
        public double Ueberschuss;       // MWh/a
    }

    // Detail: Photovoltaik-Aggregat (Tab_ErgebnisPhotovoltaik) + Modul-Liste.
    public class ErgebnisPhotovoltaikModel
    {
        public double Strombedarf;           // MWh/a
        public double Reststrombedarf;       // MWh/a
        public double Stromproduktion;       // MWh/a (Gesamte Stromerzeugung der Module)
        public double Strombedarfsdeckung;   // %
        public double Ueberschuss;           // MWh/a
        public double MaxSolareLeistung;     // W/m^2

        public List<ErgebnisPhotovoltaikModulModel> Module = new List<ErgebnisPhotovoltaikModulModel>();
    }

    // Eine PV-Modul-Zeile (Tab_ErgebnisPhotovoltaikModul).
    public class ErgebnisPhotovoltaikModulModel
    {
        public string Modul = "";
        public double Flaeche;          // m^2 gesamt
        public long Anzahl;             // Modulanzahl
        public double Stromproduktion;  // MWh/a
    }

    // ---------------------------------------------------------------------------
    // Detail: Stromspeicher (Tab_ErgebnisStromspeicher) - der Kennzahlenblock aus
    // Fachkonzept Stromspeicher 7.1, eine Zeile je gerechneter Speicheranlage.
    //
    // AUSSCHLIESSLICH SKALARE. Ergebniszeitreihen (SoC-Gang, Geldwert je Intervall,
    // Netzbezug vor/nach) werden bewusst NICHT persistiert (AP0-Entscheid vom
    // 16.08.2026, Frage 2): Ein Jahreslauf liegt im Millisekundenbereich, Neurechnen
    // ist billiger als Speichern, und fuer Ergebniszeitreihen gibt es im Bestand kein
    // Muster - alle Tab_Ergebnis*-Tabellen fuehren Skalare. Wer die Reihen dauerhaft
    // braucht, exportiert sie als CSV (7.2).
    //
    // Anders als die Geschwister dieser Datei traegt der Satz KEINE Modulliste: die
    // Aufteilung auf mehrere Speicher IST die Liste (eine Zeile je Anlage), und die
    // Variantengliederung haengt an ID_Energieanlage (Fachkonzept 7.3).
    // ---------------------------------------------------------------------------
    public class ErgebnisStromspeicherModel
    {
        // --- Kopf ---

        /// <summary>Anlagenzeile (Tab_Energieanlagen.ID) der gerechneten Variante, 0 = unbekannt.</summary>
        public int ID_Energieanlage;

        /// <summary>Bezeichner der Anlage bzw. Variante zum Zeitpunkt der Rechnung.</summary>
        public string Bezeichner = "";

        /// <summary>Betriebsart des Laufs (DbWerte.SP_BETRIEBSART_*) - festgehalten, weil die Variante danach umgestellt werden kann.</summary>
        public string Betriebsart = "";

        /// <summary>Berechnungsart des Laufs (DbWerte.SP_BERECHNUNG_*).</summary>
        public string Berechnungsart = "";

        // --- Energie (7.1, Block 1) ---

        public double Ladung_PV;             // kWh/a aus PV-Ueberschuss
        public double Ladung_BHKW;           // kWh/a aus BHKW-Ueberschuss
        public double Ladung_Netz;           // kWh/a aus dem Netz (Graustrom, AP10)
        public double Ladung_Gesamt;         // kWh/a
        public double Entladung_Gesamt;      // kWh/a
        public double Verluste_Gesamt;       // kWh/a (Lade- und Entladeverluste)
        public double Netzbezug_Mit;         // kWh/a mit Speicher
        public double Netzbezug_Ohne;        // kWh/a ohne Speicher
        public double Einspeisung_Mit;       // kWh/a mit Speicher
        public double Einspeisung_Ohne;      // kWh/a ohne Speicher
        public double Eigenverbrauchsquote;  // % (mit Speicher)
        public double Autarkiegrad;          // % (mit Speicher)

        // --- Speicher (7.1, Block 2) ---

        public double Vollzyklen;             // - aequivalente Vollzyklen p. a. (n_zyk)
        public double SoC_Min;                // kWh Jahresminimum
        public double SoC_Mittel;             // kWh Jahresmittel
        public double SoC_Max;                // kWh Jahresmaximum
        public double Zeitanteil_Untergrenze; // % der Intervalle an SoC_min
        public double Zeitanteil_Obergrenze;  // % der Intervalle an SoC_max
        public double Zyklen_Hochrechnung;    // - Zyklen ueber die Nutzungsdauer (gegen N_zyk)

        // --- Wirtschaft (7.1, Block 3) ---

        public double Ertrag_Bezugsersparnis;       // EUR/a vermiedener Netzbezug
        public double Ertrag_Verguetung_Entgangen;  // EUR/a entgangene Einspeiseverguetung (Abzug)
        public double Ertrag_Netzerloes;            // EUR/a Verkauf ins Netz (AP10)
        public double Kosten_Ladung;                // EUR/a Ladekosten (Netzladung, AP10)
        public double Ertrag_Leistungspreis;        // EUR/a Leistungspreisersparnis (Peak-Shaving)
        public double Verschleisskosten;            // EUR/a K_ver - eigene Betriebskostenzeile (5.4)
        public double Investition;                  // EUR   I = c_cap*C_nom + c_pow*P + I_fix
        public double Annuitaet;                    // EUR/a
        public double Jahresueberschuss;            // EUR/a Delta J
        public double Ertrag_Jahr1;                 // EUR/a E_a,1 (unskaliertes Referenzjahr)
        public double Ertrag_Aequivalent;           // EUR/a E_a,aeq (degradationsaequivalent)
        public double Amortisation_Statisch;        // a     T_stat
        public double Amortisation_Dynamisch;       // a     T_dyn
        public double Kapitalwert;                  // EUR   NPV

        /// <summary>
        /// Verwendete Preisversion (Fachkonzept 4.1, Stichtagsregel) - damit ein
        /// Ergebnis reproduzierbar bleibt, auch wenn der Preis danach neu versioniert
        /// wird. Leer, solange das Preismodul (AP4) fehlt.
        /// </summary>
        public string Preisversion = "";
    }
}
