using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der ERGEBNISZUSTAND der Schemamigration — losgelöst von der Migration selbst.
    ///
    /// <para><b>Warum diese Klasse existiert (Umsetzungskonzept iU3, Kante K1).</b> Der
    /// Rechenkern (<c>SimulationRunner</c>, <c>SimulationControl</c>) muss vor jedem Lauf
    /// wissen, ob die Datenbank auf dem benötigten Stand ist. Bisher fragte er das direkt
    /// bei <see cref="SchemaMigration"/> ab — und zog damit die vollständige Migration mit
    /// ihrem Access-Zweig (<c>System.Data.OleDb</c>) in den Kern hinein. Die Antwort auf
    /// „darf gerechnet werden?" sind aber nur vier Werte; die Migration, die sie erzeugt,
    /// gehört nicht dazu.</para>
    ///
    /// <para>Deshalb tragen die Werte jetzt hier. <see cref="SchemaMigration"/> behält
    /// seine öffentliche Fläche vollständig und LEITET WEITER — jeder bestehende Aufrufer
    /// bleibt gültig, der Rechenkern kommt ohne die Migration aus.</para>
    ///
    /// <para><b>Vorbelegung.</b> <see cref="MigrationOk"/> ist vor dem ersten Lauf
    /// <c>true</c>: Werkzeuge, die die Migration gar nicht anstoßen (Referenzlauf-Suite),
    /// sollen dadurch nicht blockiert werden. <see cref="Ausgefuehrt"/> bleibt bis zum
    /// ersten Lauf <c>false</c> — die Sperre greift erst, wenn tatsächlich migriert
    /// wurde und dabei etwas fehlschlug.</para>
    /// </summary>
    public static class SchemaStand
    {
        /// <summary>
        /// Der Schemastand, den ein vollständiger Migrationslauf dieser Programmfassung
        /// erreicht — die ZAHL, nicht die Migration.
        ///
        /// <para>Sie steht hier und nicht bei <c>SchemaMigration</c>, weil der
        /// PROJEKTTRANSFER sie braucht: <c>ProjektExportImportCtrl</c> schreibt sie in
        /// das Manifest eines <c>.wpx</c>-Pakets und lehnt beim Import ein Paket mit
        /// abweichendem Stand ab. Genau diese eine Konstante war bis iU9‑W15a die
        /// einzige Kante, die den Transfercontroller (1 278 Zeilen, ohne jede
        /// WinForms-Berührung) im Anwendungsprojekt festhielt — und damit den
        /// Projekttransfer auf iOS unmöglich machte (Befund W15a‑B30).</para>
        ///
        /// <para><c>SchemaMigration.ZIEL_VERSION</c> verweist seither HIERHER; die
        /// Fortschreibung der Nummer bleibt bei der Migration beschrieben (dort steht
        /// die Reihenfolge „erst Schrittkonstante, Methode und SCHRITTE-Eintrag, DANN
        /// das Ziel"). Wer eine neue Migrationsstufe anlegt, ändert die Zahl HIER.</para>
        ///
        /// <para><b>Nicht zu verwechseln mit dem FREEZE-Stand</b>
        /// (<c>SchemaMigration.FREEZE_VERSION</c> = 61): Das ist der Stand, den der
        /// eingefrorene ACCESS-Zweig erreicht. Seit dem ersten Schritt des SQLite-Zweigs
        /// (<c>SCHRITT_62_KLIMAWAISEN</c>, iU9‑W14c) sind die beiden Zahlen verschieden —
        /// das ZIEL stand auf 62, nach Merge 5 (05.09.2026, die PV-Schritte
        /// 63 und 64 des PV-Ertragsmodells, umnummeriert von 62/63) auf 64 und seit
        /// dem Wechselrichterkatalog (Schritt 65, Anwenderentscheid W6-E-2 vom
        /// 06.09.2026, Stufe S1) auf 65; mit der Strangzuordnung und dem sichtbaren
        /// Wechselrichterweg (Schritt 66, W6-E-2 / W6-E-3, Stufe S2) auf 66 und mit der
        /// sichtbaren BHKW-Leistungsuntergrenze (Schritt 67, W6-E-7 vom 07.09.2026) auf
        /// 67; mit dem Hersteller des Stromspeicherkatalogs (Schritt 68,
        /// W14a-E-10-Q7 vom 07.09.2026, Stufe S2) auf 68; mit der Reparatur der
        /// verdorbenen PV-Modulkoeffizienten (Schritt 69, Befund W6-B-5 und die
        /// Entscheide Q1 bis Q3 vom 07.09.2026) auf 69; mit der PV-Strangprüfung
        /// (Schritt 70 — der Kurzschlussstrom je MPPT und die zwei
        /// Auslegungstemperaturen, Anwenderentscheide W6-B-10 und W6-B-11 vom
        /// 09.09.2026) auf 70 und mit dem Szenario-Parametersatz der Wirtschaftlichkeit
        /// (Schritt 71 — zwölf nullbare Spalten an Tab_ProjektWirtschaftlichkeit,
        /// Anwenderentscheid W5-B-9 vom 09.09.2026) auf 71; mit der Preisindizierung
        /// der Ersatzbeschaffung und den nicht monetären Wirkungen (Schritt 72 — der
        /// Preisänderungssatz der kapitalgebundenen Kosten p_I in drei Spalten und das
        /// Freitextfeld, Anwenderentscheid W5-B-12 vom 09.09.2026, VALERI-Lücken G4
        /// und G6) auf 72; mit den gespeicherten Speicherauslegungsprofilen und
        /// Zeitreihen (Schritt 73 vom 11.09.2026) auf 73 — und mit dem NEUAUFBAU
        /// derselben Tabelle als <b>STRICT</b>-Tabelle (Schritt 74, Auftrag #178 vom
        /// 11.09.2026) auf <b>74</b>. Schritt 74 ist der erste TABELLENNEUBAU des
        /// SQLite-Zweigs (<c>CREATE</c> unter Hilfsnamen, <c>INSERT … SELECT</c>,
        /// <c>DROP</c>, <c>RENAME</c>, Index neu — alles in einer Transaktion); die
        /// Anweisungen stehen bei <see cref="SpeicherAuslegungStrict"/>, er ändert keinen
        /// Wert und ist wiederholbar. Mit der NUTZUNGSDAUERTABELLE (Schritt 75, Stufe S1
        /// des Konzepts „Nutzungsdauer je Technik und Positionsart", Anwenderentscheide
        /// ND-Q1 bis ND-Q8 vom 14.09.2026) steht das Ziel auf <b>75</b>: eine neue
        /// STRICT-Tabelle <c>Tab_Nutzungsdauer</c> samt Saat, zwei nullbare
        /// Verweisspalten <c>NutzungsdauerID</c> und die Saat-Zuordnung der
        /// Auslieferungspositionen; die Anweisungen stehen bei
        /// <see cref="NutzungsdauerSchema"/>, <c>Tab_ProjektWerte</c> bleibt
        /// wertgleich. Mit dem EINDEUTIGEN INDEX über
        /// <c>energy_project_settings</c> (Schritt 76, Auftrag #278 vom 15.09.2026)
        /// steht das Ziel auf <b>76</b>: Die Regel „ein Preis und ein Emissionssatz je
        /// Energieträger im Projekt" hielt bis dahin allein die Anwendungslogik, ab hier
        /// hält sie die Datenbank; die Anweisungen — die Entdoppelung des Bestands und
        /// der Index — stehen bei
        /// <see cref="ProjektEnergietraegerEindeutig"/>, und der Schritt ist
        /// ergebnisneutral. Mit der VOLUMENBEMESSUNG DER PUFFERSPEICHER-VORLAGE
        /// (Schritt 77, Auftrag #284 vom 15.09.2026) steht das Ziel auf <b>77</b>: Die
        /// ausgelieferte Investitionsvorlage des Pufferspeichers trug die Bemessung „je
        /// kWh Kapazität", für die dieses Gewerk keine Bezugsgröße führt; sie trägt ab
        /// hier die Bemessung je Liter Gesamtvolumen. Die Anweisung steht bei
        /// <see cref="PufferspeicherBemessungVolumen"/>; der Schritt fasst nur Zeilen ohne
        /// gepflegten Satz an und ist damit ergebnisneutral. Mit dem FESTEN BETRAG DER
        /// PV-POSITION „BATTERIESPEICHER" (Schritt 78, Auftrag #287 vom 15.09.2026) steht
        /// das Ziel auf <b>78</b>: Die ausgelieferte Investitionsvorlage der Photovoltaik
        /// trug für diese Position die Bemessung „je kWh Kapazität", für die das Gewerk
        /// keine Bezugsgröße führt — seine einzige Baugröße ist die installierte Leistung
        /// in kWp. Die Anweisung steht bei <see cref="PvVorlageBatteriespeicher"/>; wie
        /// Schritt 77 fasst auch dieser nur Zeilen ohne gepflegten Satz an und ist damit
        /// ergebnisneutral. Mit dem HEIZSTAB JE WÄRMEPUMPE (Schritt 79, Auftrag #299 vom
        /// 16.09.2026) steht das Ziel auf <b>79</b>: Der projektweite Schalter
        /// <c>Tab_Einstellungen.WP_Heizstab</c> — der einzige, den der Lauf las — geht an
        /// die Anlagenzeile über (<c>Tab_Energieanlagen.Heizstab</c>) und wird danach
        /// entfernt; die Anweisungen stehen bei <see cref="HeizstabJeWaermepumpe"/>. Der
        /// Schritt ist ergebnisneutral, weil die Übernahme jeder Wärmepumpen-Anlage genau
        /// den Wert gibt, mit dem ihr Projekt gerechnet hat. Und mit dem KATALOGVERWEIS
        /// DER WÄRMEPUMPEN-PROJEKTKOPIE (Schritt 80, derselbe Auftrag) steht es auf
        /// <b>80</b>: <c>Tab_WP</c> bekommt die nullbare Spalte <c>ID_Stamm</c> samt
        /// Index, nachgetragen wird sie nur bei EINDEUTIGEM Bezeichner; die Anweisungen
        /// stehen bei <see cref="WaermepumpeKatalogverweis"/>, kein Rechenweg liest sie.
        /// Mit dem LÖSCHSCHUTZ DER PROJEKTKOSTEN (Schritt 81, Auftrag #302 vom
        /// 16.09.2026) steht das Ziel auf <b>81</b>: Der Fremdschlüssel
        /// <c>Tab_ProjektWerte.StammID → Tab_Kostenfaktor</c> trug
        /// <c>ON DELETE CASCADE</c> — EINEN Katalogeintrag zu löschen riss jede
        /// Projektposition derselben <c>StammID</c> in allen Projekten und Gewerken mit.
        /// Ab hier trägt er <c>ON DELETE RESTRICT</c>; <c>ON UPDATE CASCADE</c> bleibt.
        /// Der Schritt ist der ZWEITE Tabellenneubau des SQLite-Zweigs (SQLite kann eine
        /// Fremdschlüsselregel nicht per <c>ALTER TABLE</c> ändern); die Anweisungen
        /// stehen bei <see cref="ProjektWerteLoeschschutz"/>. Er kopiert Zeilen, IDs und
        /// den AUTOINCREMENT-Stand unverändert und ist damit ergebnisneutral.
        /// Mit der MERKSPALTE DER GEPFLEGTEN KASKADE (Schritt 82, Anwenderentscheid vom
        /// 16.09.2026) steht das Ziel auf <b>82</b>: <c>Tab_Einstellungen</c> bekommt die
        /// Ja/Nein-Spalte <c>Kaskade_Gepflegt</c> (0/1, <c>NOT NULL DEFAULT 0</c>). Sie
        /// hält fest, dass der Anwender die Kaskade selbst in die Hand genommen hat —
        /// dann zieht <c>KonfigurationCtrl.HeizkesselNachziehen</c> keinen Heizkessel
        /// mehr nach, und ein entfernter Kessel bleibt draussen. Der Name steht bei
        /// <see cref="SchemaKatalog.SPALTE_KASKADE_GEPFLEGT"/>, die Spaltenliste bei
        /// <see cref="SchemaKatalog.Schritt82_KaskadeGepflegt"/>. Der Schritt ist
        /// ergebnisneutral: Er legt eine Spalte an und schreibt keinen Wert; im ganzen
        /// Bestand steht dort 0, und 0 heisst „wie bisher".
        /// Mit den STROMPREIS-DETAILS (Schritt 83, Anwenderentscheide SP-E-2/SP-E-3 vom
        /// 17.09.2026) steht das Ziel auf <b>83</b>: <c>energy_project_settings</c>
        /// bekommt die Beschaffung, die drei Einzelumlagen und die Merkspalte
        /// „Umlagen aufschlüsseln" (<see cref="SchemaKatalog.Schritt83_Strompreisdetails"/>),
        /// und der bis dahin wirksame Aufschlag wird in den Arbeitspreis gefaltet
        /// (<see cref="StrompreisZerlegung"/>). Der Schritt ändert Daten, aber kein
        /// Ergebnis: Nach der Faltung ist die Summe der Anteile der Arbeitspreis und die
        /// Summe ohne Beschaffung der bisherige Aufschlag — jede Preisreihe bleibt, wie
        /// sie war, der Referenzlauf byte-gleich.
        /// Mit dem UMZUG DER EINSPEISEVERGÜTUNG (Schritt 84, Anwenderentscheid SP-E-5 (a)
        /// vom 17.09.2026) steht das Ziel auf <b>84</b>: Die Trägerkarte trägt v_pv und
        /// v_bhkw nicht mehr; die eine Quelle sind
        /// <c>Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung</c> und
        /// <c>Einspeiseverguetung_KWK</c>. Der Schritt legt KEINE Spalte an — er rettet
        /// nur die gepflegten Kartenwerte hinüber (<see cref="VerguetungUmzug"/>),
        /// ct/kWh nach €/kWh, und lässt gepflegte Parameter unangetastet. Er ist
        /// ergebnisneutral: Die Speicherwelt liest dieselbe Zahl, nur von der neuen
        /// Stelle — der Referenzlauf bleibt byte-gleich.
        /// Mit dem WEGFALL DER ALTSPALTEN (Schritt 85, Aufräumen nach 83 und 84) steht
        /// das Ziel auf <b>85</b>: Die fünf Spalten, die die beiden Schritte ohne Leser
        /// zurückgelassen haben — <c>Aufschlag_Modus</c>, <c>Aufschlag_Override</c>,
        /// <c>Verguetung_PV</c>, <c>Verguetung_BHKW</c> und
        /// <c>Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden</c> — fallen weg
        /// (<see cref="StrompreisAltspalten"/>). Der Schritt schreibt kein DML und ist
        /// ergebnisneutral: Keine der Spalten trägt eine Rechengröße, der Referenzlauf
        /// bleibt byte-gleich.
        /// Mit der LASTSPITZENKAPPUNG ALS BERECHNUNGSART (Schritt 86, Anwenderbefund vom
        /// 17.09.2026 und Entscheide LS-E-1 (a)/LS-E-3) steht das Ziel auf <b>86</b>:
        /// <c>Tab_StromspeicherVariante</c> bekommt die Zielschwelle
        /// <c>PeakZiel_kW</c> (REAL, nullbar) und das Flag <c>PeakZiel_Adaptiv</c>
        /// (0/1, <c>NOT NULL DEFAULT 0</c>) — die Spaltenliste steht bei
        /// <see cref="SchemaKatalog.Schritt86_Lastspitzenkappung"/>. Der Schritt ist
        /// ergebnisneutral: Er legt zwei Spalten an und schreibt keinen Wert; beide
        /// werden nur gelesen, wenn die Variante die neue Berechnungsart führt, und das
        /// tut im Bestand keine — der Referenzlauf bleibt byte-gleich.
        /// Mit dem ENTDOPPELTEN GESETZESKATALOG (Schritt 87, Anwenderentscheid
        /// US-E-1 (a) vom 17.09.2026) steht das Ziel auf <b>87</b>:
        /// <c>Tab_Gesetzesparameter</c> führt ab hier höchstens EINE Zeile je Schlüssel,
        /// Klasse und Stichjahr. Was die Pflegemaske seit jeher prüft
        /// (<c>GesetzKatalog.Existiert</c>), hält ab hier die Datenbank; die Anweisungen
        /// — die Entdoppelung des Bestands und der eindeutige Index — stehen bei
        /// <see cref="GesetzesparameterEindeutig"/>, und jede entfernte Zeile bekommt
        /// ihre Protokollzeile. Im selben Schritt fällt der zweite Zustand, den ein
        /// Schreibweg ausschließt und die Datenbank zuließ: zwei aktive
        /// Speichervarianten in EINEM Projekt (<see cref="SpeicherVarianteAktivEindeutig"/>).
        /// Der Schritt ist ergebnisneutral — behalten wird beide Male die kleinste ID,
        /// genau die Zeile, die jede Lesekette schon bisher genommen hat.
        /// Mit dem MODUS DER STROMSTEUERBEFREIUNG (Schritt 88, Etappe B6 des
        /// Wirtschaftlichkeitskonzepts) steht das Ziel auf <b>88</b>:
        /// <c>Tab_ProjektWirtschaftlichkeit</c> bekommt die Spalte
        /// <c>Stromst_Befreiung_Modus</c> (TEXT, <c>AUSWEIS</c>/<c>ERLOES</c>, NULL =
        /// AUSWEIS) — die Spaltenliste steht bei
        /// <see cref="SchemaKatalog.Schritt88_StromsteuerModus"/>. Der Schritt selbst
        /// schreibt keinen Wert; die Rechenwirkung liegt in der VORGABE: § 9 Abs. 1
        /// Nr. 3 StromStG ist keine Rückerstattung, sondern eine kleinere
        /// Bezugsrechnung, und wird ab hier ausgewiesen statt als Erlös gebucht. Im
        /// Bestand bucht kein gespeicherter Lauf diese Reihe (Befund B-1) — die
        /// dreizehn Referenzprojekte rechnen unverändert, der Referenzlauf bleibt
        /// byte-gleich.
        /// Mit der ANLAGENWAHRHEIT DES KWK-ZUSCHLAGS (Schritt 89, Etappe BK1) steht das
        /// Ziel auf <b>89</b>: <c>Tab_Energieanlagen</c> bekommt die Spalte
        /// <c>KWKG_Kostenanteil</c> (DOUBLE, NULL = nicht gepflegt) — die Spaltenliste
        /// steht bei <see cref="SchemaKatalog.Schritt89_KwkAnlagenwahrheit"/>. Anders
        /// als Schritt 88 trägt dieser Schritt ein DML: Er schreibt die KWKG-Vorgaben
        /// des Projekts in jede BHKW-Anlagenzeile, die an der betreffenden Stelle leer
        /// ist. Erst danach gibt der Rechenweg den Rückfall Anlage → Projekt auf. Der
        /// Schritt ist ergebnisneutral — jede Anlage rechnet mit genau dem Wert, den ihr
        /// der Rückfall bisher zugewiesen hat; der Referenzlauf bleibt byte-gleich.
        /// Mit dem AUFRÄUMEN NACH DER ANLAGENWAHRHEIT (Schritt 90, Etappe BK1a) steht das
        /// Ziel auf <b>90</b>. Der Schritt hat ZWEI Teile aus zwei Befunden, und beide
        /// sind ergebnisneutral:
        /// <list type="number">
        ///   <item><description><b>DDL</b> — die sechs KWKG-Spalten von
        ///     <c>Tab_ProjektWirtschaftlichkeit</c> fallen
        ///     (<see cref="KwkgProjektaltspalten"/>). Seit Schritt 89 und Etappe BK1a
        ///     rechnet KEIN Weg mehr mit ihnen: Der Regelweg je Anlage und der Ersatzweg
        ///     (leistungsgewichtete virtuelle Gesamtanlage) lesen beide die Anlage.
        ///     <c>KWKG_Kostenanteil</c> des Projekts bleibt samt Dialogfeld stehen
        ///     (Anwenderentscheid BK1-Q1 c).</description></item>
        ///   <item><description><b>DML</b> — die Nullzeilen der drei nicht
        ///     anlagenfähigen Erfassungsgruppen in <c>Tab_ProjektWerte</c> fallen
        ///     (<see cref="KostenErfassungsgruppenAltzeilen"/>). Es sind
        ///     Hauptkomponentenzeilen der früheren Kostenmaske, ausnahmslos ohne Wert;
        ///     kein heutiger Rechenweg legt sie an, und die Kostenseite zeigte sie unter
        ///     „Anlagenkomponenten" ohne Kennzeichnung.</description></item>
        /// </list>
        /// Der Referenzlauf bleibt byte-gleich — keine der Größen steht in der Basis.
        /// Mit dem WEGFALL DER PROJEKTSPALTE <c>KWKG_Kostenanteil</c> (Schritt 91,
        /// Etappe BK1b) steht das Ziel auf <b>91</b>. Es ist die siebte und letzte der
        /// KWKG-Projektspalten (<see cref="KwkgProjektaltspalten.KOSTENANTEIL"/>).
        /// Schritt 90 hatte sie samt Dialogfeld stehen lassen (Anwenderentscheid
        /// BK1-Q1 c) und als offenen Punkt BK1-4 vermerkt; mit
        /// „BK1-4: (a) Entfernen" fällt sie nach. § 8 Abs. 2/3 KWKG leitet das
        /// Vbh-Kontingent aus dem Kostenanteil DER ANLAGE ab
        /// (<c>Tab_Energieanlagen.KWKG_Kostenanteil</c>, Schritt 89); der Projektwert
        /// war seit Etappe BK1a ohne Rechenleser und wurde nur noch von seinem eigenen
        /// Dialogfeld gepflegt. <b>Kein DML:</b> Schritt 89 hat den Wert einmalig in
        /// jede BHKW-Anlagenzeile übertragen; ein zweites Mal übertragen hieße, eine
        /// seither gepflegte Anlagenzelle zu überschreiben. Der Schritt ist
        /// ergebnisneutral — Projekt 1030 rechnet Zuschlag und Kapitalwert
        /// zahlengleich, der Referenzlauf bleibt byte-gleich.
        /// Mit dem WÄHLBAREN VERGLEICHSPROJEKT (Schritt 92, Konzept § 2.9) steht das
        /// Ziel auf <b>92</b>: <c>Tab_ProjektWirtschaftlichkeit.ID_Referenzprojekt</c>
        /// (<see cref="SchemaKatalog.SPALTE_PW_REFERENZPROJEKT"/>) nimmt je Gruppe auf,
        /// gegen welchen Stand die Differenzkennzahlen rechnen. <b>Kein DML:</b> Die
        /// Spalte bleibt NULL, und NULL heißt Stamm — genau die Referenz des Bestands;
        /// der Referenzlauf bleibt byte-gleich.
        /// Mit der VERGÜTUNG JE VARIANTE (Schritt 93, Konzept § 2.16) steht das Ziel auf
        /// <b>93</b>: <c>Tab_ProjektPhotovoltaik.Uebernahme_Stamm</c>
        /// (<see cref="SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM"/>) hält je Projektzeile
        /// fest, ob sie gilt („eigene Werte") oder die Variante die Vergütung ihres
        /// Stammprojekts übernimmt. Anders als 92 trägt dieser Schritt ein DML, und es
        /// ist ergebnisneutral (Anwenderentscheid VV‑Q4): Jede vorhandene Variantenzeile
        /// wird zur eigenen (0) und rechnet weiter wie bisher; eine Variante ohne Zeile,
        /// deren Stamm eine aktive Zeile führt, bekommt eine eigene, INAKTIVE Zeile und
        /// bleibt damit auf dem Flat-Pfad. Der Referenzlauf bleibt byte-gleich.
        /// Mit der HILFSSTROM-BEMESSUNG DER SAAT (Schritt 94) steht das Ziel auf
        /// <b>94</b>: Der Schritt trägt <b>kein DDL</b> — er stellt allein die drei
        /// Hilfsstrom-Positionen der Katalogvorlage „Standard" (BHKW, Heizkessel,
        /// Wärmepumpe) von <c>PROZENT_ENDENERGIEKOSTEN</c> auf
        /// <c>PROZENT_ENDENERGIEBEDARF</c> um; die Anweisung steht bei
        /// <see cref="HilfsstromBemessungVorlage"/>. Hilfsenergie ist Strom und wird ab
        /// hier auch mit dem Strombezugspreis bewertet statt mit dem Arbeitspreis des
        /// Brennstoffträgers. <b>Nur die Saat:</b> <c>Tab_ProjektWerte</c> fasst der
        /// Schritt nicht an — was in einem Projekt erfasst ist, bleibt erfasst, und erst
        /// die nächste Übernahme aus der Vorlage trägt die neue Bemessung hinein. Der
        /// Referenzlauf bleibt byte-gleich; keine Referenzrechnung liest eine
        /// Vorlagenposition.
        /// Mit den KLIMASPALTEN (Schritt 95, Anwenderentscheid 19.09.2026) steht das
        /// Ziel auf <b>95</b>: <c>Tab_Solar</c> und <c>Tab_Solar_STAMM</c> bekommen
        /// <c>Gegenstrahlung</c> [W/m²], <c>Luftfeuchte</c> [%] und
        /// <c>Bedeckungsgrad</c> [Achtel] — die Größen, die die Gebäudesimulation nach
        /// VDI 6007 braucht und die beide Klimaquellen längst liefern;
        /// <c>Tab_Klimaregion</c> und <c>Tab_Klimaregion_STAMM</c> bekommen
        /// <c>Quelle</c> und <c>Importdatum</c>. Die zehn Spalten stehen bei
        /// <see cref="SchemaKatalog.Schritt95_Klimaspalten"/>. <b>Kein DML:</b> Alle
        /// bleiben NULL, und NULL heißt „nicht verfügbar" bzw. „Altbestand"; kein
        /// Rechenweg liest eine von ihnen, der Referenzlauf bleibt byte-gleich.
        /// Mit dem PROJEKT-FREMDSCHLÜSSEL (Schritt 96, Anwenderentscheid 19.09.2026)
        /// steht das Ziel auf <b>96</b>: Die achtundzwanzig Projekttabellen, die ihre
        /// Beziehung zu <c>Tab_Projekt</c> bisher nur dem Namen nach führten, bekommen
        /// sie als Fremdschlüssel mit <c>ON DELETE CASCADE ON UPDATE CASCADE</c>. Der
        /// Katalog steht bei <see cref="ProjektFremdschluessel.Katalog"/>. <b>Der
        /// Schritt entfernt Zeilen</b> — aber nur solche, die zu keinem Projekt gehören
        /// und deshalb kein Rechenweg je gelesen hat; wo eine ungepflegte Projektspalte
        /// an einem gültigen Elternsatz hängt, wird sie nachgezogen statt die Zeile zu
        /// verlieren. Werte, Ids und Zählerstände bleiben, der Referenzlauf bleibt
        /// byte-gleich.
        /// Mit SZENARIO UND BEZUGSJAHR (Schritt 97, Anwenderentscheid 19.09.2026)
        /// steht das Ziel auf <b>97</b>: <c>Tab_Klimaregion</c> und
        /// <c>Tab_Klimaregion_STAMM</c> bekommen <c>Szenario</c> (Schlüssel
        /// <c>MITTEL</c> | <c>SOMMERWARM</c> | <c>WINTERKALT</c>) und
        /// <c>Bezugsjahr</c> (2015 oder 2045) — die Angabe, WELCHES Wetterjahr eine
        /// Region trägt. Sie stand bis hierher nur im Freitext <c>Details</c> und war
        /// damit weder sortierbar noch filterbar. Die vier Spalten stehen bei
        /// <see cref="SchemaKatalog.Schritt97_KlimaSzenario"/>. <b>Kein DML:</b> Beide
        /// bleiben NULL, und NULL heißt „sagt nichts dazu"; kein Rechenweg liest sie,
        /// der Referenzlauf bleibt byte-gleich.
        /// Mit dem BHKW-WIRKUNGSGRAD ALS FAKTOR (Schritt 98, Anwenderentscheid
        /// 19.09.2026) steht das Ziel auf <b>98</b>: <c>Tab_BHKW_STAMM.Wirkungsgrad</c>
        /// und <c>Tab_BHKW.Wirkungsgrad</c> sind der GESAMTwirkungsgrad als Faktor
        /// (0…1) — so sagt es die Maske, so rechnet <c>SimulationBHKW</c>
        /// (<c>Verbrauch = (Wärme + Strom) / Wirkungsgrad</c>). Ein Teil des Katalogs
        /// trug dort einen Prozentwert, und zwar den des ELEKTRISCHEN Wirkungsgrads;
        /// der Brennstoff des BHKW fiel dadurch um rund Faktor 32 zu klein aus. Der
        /// Schritt rechnet je Zeile um —
        /// <c>(Ptherm + Pel) · Wirkungsgrad / 100 / Pel</c>, auf vier Stellen —, und
        /// zwar nur, wenn das Ergebnis in [0,5; 1,05] fällt; sonst bleibt die Zeile
        /// stehen und wird benannt ausgewiesen. Die Anweisung steht bei
        /// <see cref="BhkwWirkungsgradFaktor"/>. <b>Kein DDL.</b> <b>Der Referenzlauf
        /// ändert sich</b> — die Basis wird im selben Schritt neu eingefroren
        /// (<c>2026-09-19_R10_BhkwWirkungsgrad</c>).
        /// Mit den ZWEI WIRKUNGSGRADEN DES BHKW (Schritt 99, Anwenderentscheid
        /// 20.09.2026) steht das Ziel auf <b>99</b>: <c>Tab_BHKW_STAMM</c> und
        /// <c>Tab_BHKW</c> bekommen <c>Wirkungsgrad_el</c> und <c>Wirkungsgrad_th</c>
        /// — die zwei Anteile, aus denen sich der Gesamtwirkungsgrad ergibt („Der
        /// Wirkungsgrad sollte sich aus dem elektrischen und dem thermischen
        /// Wirkungsgrad ergeben."). Die vier Spalten stehen bei
        /// <see cref="SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile"/>, der Datenteil
        /// bei <see cref="BhkwWirkungsgradAnteile"/>: Jeder gepflegte
        /// Gesamtwirkungsgrad wird im Verhältnis der Leistungen aufgeteilt
        /// (<c>Wirkungsgrad · Pel / (Pel + Ptherm)</c> und ebenso thermisch, vier
        /// Stellen); fehlt eine Angabe, bleiben beide NULL und die Zeile wird benannt
        /// ausgewiesen. <b>Die Spalte <c>Wirkungsgrad</c> bleibt die Summe</b> und
        /// bleibt der Wert, den <c>SimulationBHKW</c> liest — <b>der Referenzlauf
        /// bleibt byte-gleich</b>, die Basis <c>2026-09-19_R10_BhkwWirkungsgrad</c>
        /// gilt weiter.
        /// Mit der VORGABE 0 DER FREMDSCHLÜSSELSPALTEN (Schritt 100,
        /// Anwenderentscheid 21.09.2026, Auftrag FK-1) steht das Ziel auf <b>100</b>:
        /// Einundvierzig Fremdschlüsselspalten in fünfundzwanzig Tabellen tragen
        /// <c>DEFAULT 0</c>, und keine Elterntabelle hat eine Zeile 0 — jeder
        /// Schreibweg, der eine solche Spalte weglässt, bekam still die 0 und damit
        /// eine Fremdschlüsselmeldung weit weg von der Ursache. Der Schritt nimmt die
        /// Vorgabe heraus und lässt <c>NOT NULL</c> stehen, wo es steht; eine
        /// weggelassene Spalte meldet ab hier <c>NOT NULL constraint failed</c> mit
        /// Tabelle und Spalte im Klartext, und eine nullbare Spalte wird NULL, was
        /// SQLite bei einer Beziehung immer durchlässt. Der Schritt steht bei
        /// <see cref="FremdschluesselVorgabe"/>: gemessener Katalog
        /// (<c>pragma_foreign_key_list</c> × <c>pragma_table_info</c>), Zieltext aus
        /// dem Bestands-DDL, Tabellenneubau mit abgeschalteten Fremdschlüsseln,
        /// wiederholbar. <b>Kein DML:</b> Werte ändert er nicht — Zeilen mit dem Wert 0
        /// gibt es nicht, und fände er welche, bräche er benannt ab. <b>Der
        /// Referenzlauf bleibt byte-gleich</b>, die Basis
        /// <c>2026-09-19_R10_BhkwWirkungsgrad</c> gilt weiter.
        /// Mit dem GEBÄUDESPALTEN-SCHRITT M3 (Schritt 101, Stufe G1 der
        /// Gebäudesimulation, Auftrag vom 23.09.2026) steht das Ziel auf <b>101</b>:
        /// <c>Tab_Gebaeude</c> und <c>Tab_Gebaeude_STAMM</c> benennen
        /// <c>Wohnflaeche</c> in <c>Nutzflaeche</c> um (E19), bekommen je fünfzehn
        /// neue Spalten (zwölf der Stufe G1, drei der Stufe G2; U5), und die Sicht
        /// <c>Abfrage_Projektgebaeude</c> wird neu gebaut — der erste Sichtneubau des
        /// SQLite-Zweigs. Alles steht bei <see cref="GebaeudeSchema"/>. Die neuen
        /// Spalten bleiben NULL (die zwei Schalter 0), kein Rechenweg liest sie —
        /// <b>der Referenzlauf bleibt byte-gleich</b>, die Basis
        /// <c>2026-09-22_R11_Bestandsbefunde</c> gilt weiter.
        /// Mit der LEEREN ANLAGENART (Schritt 102, Konzept Wirtschaftlichkeit § 6.3
        /// Nr. 30, Anwenderentscheid 22.09.2026) steht das Ziel auf <b>102</b>: Die
        /// leere Zeichenkette in <c>Tab_Energieanlagen.KWKG_Anlagenart</c> wird NULL,
        /// und NULL heißt „nicht gepflegt". Die Anweisung steht bei
        /// <see cref="KwkgAnlagenartLeer"/>. <b>Reines DML, ergebnisneutral:</b> Kein
        /// Rechenweg unterscheidet die leere Zeichenkette von NULL; der Referenzlauf
        /// bleibt byte-gleich.
        /// Mit KATALOG, ZONEN UND PROJEKT DES ZAPFPROFILGENERATORS (Schritt 103,
        /// Papiername T1, Umsetzungskonzept Zapfprofilgenerator Stufe Z0) steht das Ziel
        /// auf <b>103</b>: zehn leere Tabellen <c>Tab_Tww*</c>, deren DDL bei
        /// <see cref="TwwSchema"/> steht. <b>Reines DDL, kein Wert</b> — der Katalog kommt
        /// aus einem Paket außerhalb des Repositoriums, kein Projekt steht auf dem
        /// Generator, und kein Rechenweg liest die Tabellen; <b>der Referenzlauf bleibt
        /// byte-gleich</b>.
        /// Mit der ABLÖSUNG DES ZEITZONENTARIFS (Schritt 103, Entscheid Q11 vom
        /// 22.09.2026: „kein HT/NT") steht das Ziel auf <b>103</b>:
        /// <c>energy_project_settings</c> bekommt die zweistufige Leistungspreis-Staffel
        /// des Stromträgers (<see cref="SchemaKatalog.Schritt103_LeistungspreisStaffel"/>),
        /// und der Datenteil (<see cref="ZeitzonentarifAbloesung"/>) übernimmt die Staffel
        /// aus jedem Tarifsatz, in dem sie rechnete, löscht die Sätze des Zonenmodells,
        /// verwirft die mit ihnen gerechneten gespeicherten Ergebnisse (Entscheid E7b‑Q4)
        /// und fasst die Zonenzeilen der gespeicherten Strommatrix zu je einer Jahreszeile
        /// zusammen. <b>Der Referenzlauf bleibt byte-gleich</b> — die Wirtschaftlichkeit
        /// steht nicht im Export; in der Testdatenbank trägt kein Projekt einen Tarifsatz.
        /// Der Freeze-Stand
        /// bleibt bei 61. Der Kern kennt nur das
        /// Ziel; der Freeze-Stand gehört dem Access-Zweig und bleibt dort.</para>
        /// </summary>
        public const int Zielversion = 103;

        /// <summary>
        /// Nummer der Vorbelegung von <c>Extrapolation_erlaubt</c> (Paket 8,
        /// Konzept 13.4). Die SPALTE entsteht bereits in Schritt 2; dieser Schritt setzt
        /// ihren WERT einmalig auf WAHR und ist damit das zweite DML des Vorhabens.
        ///
        /// <para>Steht hier und nicht bei <see cref="SchemaMigration"/>, weil
        /// <c>KonfigurationCtrl</c> die Nummer gegen den gespeicherten Schemastand prüft
        /// (Kante K2) — eine reine Zahl, für die der Kern die Migration nicht braucht.
        /// <see cref="SchemaMigration.SCHRITT_7_EXTRAPOLATION"/> verweist hierher.</para>
        /// </summary>
        public const int SCHRITT_7_EXTRAPOLATION = 7;

        /// <summary>
        /// DDL des Parallelverbunds (Kante K3). <c>AnlagePufferVerbundCtrl</c> legt die
        /// Tabelle bei Bedarf still selbst an, wenn die Migration sie noch nicht gebaut
        /// hat; dafür genügt der SQL-Text, nicht die Migration.
        ///
        /// <para>Keine DEFAULT-Werte auf den beiden FK-Spalten — dieselbe Regel wie im
        /// Spaltenkatalog: eine 0 verletzte die erzwungene Beziehung, „nicht gesetzt" wird
        /// durch NULL ausgedrückt.</para>
        /// </summary>
        public const string SQL_CREATE_ANLAGEPUFFERVERBUND =
            "CREATE TABLE Z_AnlagePufferVerbund (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Anlage LONG, ID_Puffer LONG)";

        /// <summary>
        /// Index über den Anlagenverweis — der Suchweg des Dialogs (Mitglieder EINER
        /// Anlage). Die Registry-Speisung liest projektweit über einen Verbund zu
        /// <c>Tab_Energieanlagen</c> und profitiert davon ebenfalls.
        /// </summary>
        public const string SQL_INDEX_ANLAGEPUFFERVERBUND =
            "CREATE INDEX idx_AnlagePufferVerbund ON Z_AnlagePufferVerbund (ID_Anlage)";

        /// <summary>
        /// Nummer des Migrationsschritts „Einheitenkonsistenz" (Kante iU4-2). Der
        /// Prüfer <c>EnergieEinheitenPruefung</c> nennt die Nummer nur in seiner
        /// Meldung „Migrationsschritt N steht aus" — eine reine Zahl, für die er die
        /// Migration mit ihrem Access-Zweig nicht braucht.
        ///
        /// <para><see cref="SchemaMigration.SCHRITT_25_EINHEITENKONSISTENZ"/> verweist
        /// hierher; die vollständige Begründung des Schritts steht dort.</para>
        /// </summary>
        public const int SCHRITT_25_EINHEITENKONSISTENZ = 25;

        /// <summary>
        /// DDL der Preisreihe (Kante iU4-2, Muster <see cref="SQL_CREATE_ANLAGEPUFFERVERBUND"/>).
        /// <c>PreisreiheCtrl.StelleTabellenSicher</c> legt Kopf- und Wertetabelle bei
        /// Bedarf still selbst an, wenn die Migration sie noch nicht gebaut hat; dafür
        /// genügt der SQL-Text, nicht die Migration.
        ///
        /// <para><c>ID_Energietraeger</c> ist seit Schritt 40 Teil des CREATE, damit auch
        /// diese tolerante Rückfallebene die Spalte mitbringt; Bestandstabellen rüstet
        /// Schritt 40 nach.</para>
        /// </summary>
        public const string SQL_CREATE_PREISREIHE =
            "CREATE TABLE Tab_Preisreihe (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Projekt LONG, Bezeichner TEXT(255), Jahr LONG, " +
            "Aufloesung TEXT(50), Einheit TEXT(50), ID_Energietraeger LONG)";

        /// <summary>Index über den Projektbezug - der Suchweg der Auswahllisten.</summary>
        public const string SQL_INDEX_PREISREIHE =
            "CREATE INDEX idx_Preisreihe ON Tab_Preisreihe (ID_Projekt)";

        /// <summary>Werte einer Preisreihe: eine Zeile je Intervall, Reihenfolge = ID-Reihenfolge.</summary>
        public const string SQL_CREATE_PREISREIHEDATEN =
            "CREATE TABLE Tab_PreisreiheDaten (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Preisreihe LONG, Wert DOUBLE)";

        /// <summary>Index über den Kopfverweis - der einzige Suchweg auf die Werte.</summary>
        public const string SQL_INDEX_PREISREIHEDATEN =
            "CREATE INDEX idx_PreisreiheDaten ON Tab_PreisreiheDaten (ID_Preisreihe)";

        /// <summary>
        /// Löschweitergabe vom Kopf auf die Werte - ohne sie blieben nach dem Löschen
        /// einer Reihe bis zu 35.040 Waisenzeilen stehen.
        ///
        /// <para><b>Nur der Access-Zweig.</b> <c>ALTER TABLE … ADD CONSTRAINT</c> ist
        /// Access-DDL; SQLite kann einer bestehenden Tabelle keinen Fremdschlüssel mehr
        /// anhängen — die Anweisung endet dort mit „near CONSTRAINT: syntax error"
        /// (SQL-Dialekt-Audit 03.09.2026). Verwendet wird sie deshalb ausschließlich von
        /// <c>SchemaMigration.PreisreiheTabellen</c>, dem Erststart-Weg über die
        /// ACE-Engine. Die SQLite-Rückfallebene nimmt
        /// <see cref="SQL_CREATE_PREISREIHEDATEN_MIT_FK"/>, das die Beziehung schon beim
        /// Anlegen mitbringt.</para>
        /// </summary>
        public const string SQL_FK_PREISREIHEDATEN =
            "ALTER TABLE Tab_PreisreiheDaten ADD CONSTRAINT FK_PreisreiheDaten " +
            "FOREIGN KEY (ID_Preisreihe) REFERENCES Tab_Preisreihe (ID) ON DELETE CASCADE";

        /// <summary>
        /// Dieselbe Tabelle wie <see cref="SQL_CREATE_PREISREIHEDATEN"/>, aber MIT der
        /// Löschweitergabe im CREATE - die SQLite-Schreibweise derselben Absicht.
        ///
        /// <para>Angelegt am 03.09.2026 (SQL-Dialekt-Audit). Die Rückfallebene
        /// <c>PreisreiheCtrl.StelleTabellenSicher</c> legte die Tabelle bisher ohne
        /// Beziehung an und schob den Fremdschlüssel per ALTER nach; unter SQLite
        /// scheiterte das lautlos (die Zeile steht in einem stillen catch), und
        /// gelöschte Reihen ließen ihre Werte stehen. Der Migrator erzeugt die Tabelle
        /// genau so, wie sie hier steht - beide Wege enden damit beim selben Schema.</para>
        /// </summary>
        public const string SQL_CREATE_PREISREIHEDATEN_MIT_FK =
            "CREATE TABLE Tab_PreisreiheDaten (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Preisreihe LONG, Wert DOUBLE, " +
            "FOREIGN KEY (ID_Preisreihe) REFERENCES Tab_Preisreihe (ID) ON DELETE CASCADE)";

        /// <summary>
        /// DDL des Kostenprofils (Kante iU4-2). <c>KostenprofilCtrl</c> legt die Tabelle
        /// bei Bedarf still selbst an - wie bei der Preisreihe genügt dafür der SQL-Text.
        /// </summary>
        public const string SQL_CREATE_KOSTENPROFIL =
            "CREATE TABLE Tab_Kostenprofil (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Projekt LONG, Bezeichner TEXT(255), Monatswerte TEXT(255), Wochenwerte MEMO)";

        /// <summary>Index über den Projektbezug.</summary>
        public const string SQL_INDEX_KOSTENPROFIL =
            "CREATE INDEX idx_Kostenprofil ON Tab_Kostenprofil (ID_Projekt)";

        /// <summary>
        /// false, sobald ein Lauf einen Schritt nicht abschließen konnte. Vor dem ersten
        /// Lauf true - Werkzeuge, die die Migration gar nicht anstoßen (Referenzlauf-Suite),
        /// sollen dadurch nicht blockiert werden.
        /// </summary>
        public static bool MigrationOk { get; set; } = true;

        /// <summary>Vollständiger Bericht des letzten Laufs; erste Zeile ist der DB-Pfad.</summary>
        public static string Fehlerbericht { get; set; } = "";

        /// <summary>true, sobald <c>SchemaMigration.Ausfuehren</c> mindestens einmal gelaufen ist.</summary>
        public static bool Ausgefuehrt { get; set; }

        /// <summary>
        /// Sperrt den Simulationsbereich, wenn ein Migrationslauf stattgefunden HAT und
        /// dabei etwas fehlschlug.
        ///
        /// <para>Verlangt ist die Semantik „Stand &lt;
        /// <c>SchemaMigration.ZIEL_VERSION</c> ⇒ gesperrt". Sie kommt über
        /// <see cref="MigrationOk"/> zustande: <c>SchemaMigration.Ausfuehren</c> liefert
        /// <c>alleOk &amp;&amp; StandNachher &gt;= ZIEL_VERSION</c>, und
        /// <c>SchritteAbarbeitenSqlite</c> bricht bei Stand 0 und bei Stand &lt; 61 mit
        /// <c>false</c> ab. Ein Stand unter 61 kann daher gar nicht als „ok" durchgehen.
        /// Eine zweite Prüfung auf <c>StandNachher</c> stünde hier nur als
        /// Wiederholung.</para>
        /// </summary>
        public static bool SimulationGesperrt(out string grund)
        {
            if (!Ausgefuehrt || MigrationOk)
            {
                grund = null;
                return false;
            }

            grund = "Die Datenbank ist nicht auf dem für die Simulation benötigten Stand." +
                    Environment.NewLine + Environment.NewLine +
                    FehlerKopf() + Environment.NewLine + Environment.NewLine +
                    "Der Simulationsbereich bleibt gesperrt, bis die Aktualisierung der " +
                    "Datenbank erfolgreich war.";
            return true;
        }

        /// <summary>
        /// Die ersten Zeilen des Berichts - genug für eine verständliche Meldung,
        /// ohne den Anwender mit dem vollständigen Protokoll zu erschlagen.
        /// </summary>
        public static string FehlerKopf()
        {
            if (string.IsNullOrEmpty(Fehlerbericht)) return "(kein Bericht vorhanden)";

            string[] zeilen = Fehlerbericht.Replace("\r\n", "\n").Split('\n');
            var kopf = new List<string>();
            foreach (string z in zeilen)
            {
                kopf.Add(z);
                if (kopf.Count >= 12) break;
            }
            return string.Join(Environment.NewLine, kopf).TrimEnd();
        }
    }
}
