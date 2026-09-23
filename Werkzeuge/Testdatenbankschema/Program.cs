using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using WindowsFormsApplication1;

namespace Testdatenbankschema
{
    /// <summary>
    /// Zieht eine SQLite-Datenbank auf <see cref="SchemaStand.Zielversion"/> nach —
    /// gedacht fuer <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>, die Messlatte des
    /// SQL-Dialektpruefers und die Quelle jedes Referenzlaufs.
    ///
    /// <para><b>Der Weg ist der der Migration, nicht ein zweiter.</b> Die Spalten kommen
    /// aus <see cref="SchemaKatalog"/>, die Typen aus <c>StilleDb.SqliteSpaltenTyp</c>,
    /// die zwei DELETE-Texte des Schritts 62 aus <c>KlimaWaisenBereinigung</c> — also
    /// aus genau den Quellen, aus denen sich auch
    /// <c>SchemaMigration.Schritt_62_KlimaWaisen</c>,
    /// <c>Schritt_63_PvAnlagenparameter</c>, <c>Schritt_64_PvModellwahl</c>,
    /// <c>Schritt_65_Wechselrichterkatalog</c> (dessen zwei CREATE TABLE stehen in
    /// <c>WechselrichterSchema</c>), <c>Schritt_66_Strangzuordnung</c> (Tabelle in
    /// <c>AnlageStrangSchema</c>, Spalte in
    /// <c>SchemaKatalog.Schritt66_PvWechselrichterweg</c>) und
    /// <c>Schritt_67_BhkwLeistungsgrenze</c> (das eine UPDATE aus
    /// <c>BhkwLeistungsgrenzeVorgabe</c>) und <c>Schritt_68_StromspeicherFirma</c>
    /// (Spalten aus <c>SchemaKatalog.Schritt68_StromspeicherFirma</c>, Nachtrag aus
    /// <c>StromspeicherFirmaNachtrag</c>) und <c>Schritt_69_PvKoeffizienten</c> (Regel,
    /// Fenster, eingebettete Werte und alle Anweisungen aus
    /// <c>PvKoeffizientenReparatur</c>) bedienen. Hier steht keine
    /// abgeschriebene DDL und kein abgeschriebenes DML.</para>
    ///
    /// <para><b>Idempotent.</b> Eine vorhandene Spalte wird uebergangen, ein zweiter Lauf
    /// aendert nichts mehr. Rueckgabe 0 = Datei steht auf dem Zielstand.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Die Schritte 63 und 64 legen ausschliesslich Spalten
    /// an, Schritt 65 zwei LEERE Tabellen, Schritt 66 eine LEERE Tabelle und eine
    /// NULL-Spalte, und keiner von ihnen schreibt einen Wert (NULL heisst im Rechenweg genau die bisher fest
    /// verdrahtete Vorbelegung); Schritt 62 loescht nur Zeilen ohne Kopfsatz, die ueber
    /// keine Abfrage des Programms erreichbar sind. <b>Schritt 67 schreibt als einziger
    /// einen FACHWERT</b> (<c>Tab_Einstellungen.Leistungsgrenze</c> NULL → 30) — und ist
    /// gerade dadurch ergebnisneutral: Er setzt an die Stelle des stillen Fallbacks in
    /// <c>SimulationBHKW</c>, der mit dem Anwenderentscheid W6-E-7 gefallen ist, genau
    /// den Wert, mit dem diese Saetze bisher schon gerechnet haben. <b>Schritt 68</b>
    /// legt zwei Spalten an und traegt in eine davon nach, was der Bezeichner schon
    /// sagt — auch das ergebnisneutral, weil kein Rechenweg den Hersteller liest. Der
    /// Referenzlauf muss vor und nach dem Nachziehen byte-gleiche CSV liefern — das ist
    /// die Abnahme.</para>
    ///
    /// <para><b>Schritt 69 ist die Ausnahme, und das mit Absicht.</b> Er repariert die
    /// verdorbenen PV-Modulkoeffizienten (Befund W6-B-5, Entscheide Q1 bis Q3 vom
    /// 07.09.2026). <c>alpha_SC</c> und <c>beta_OC</c> liest kein Rechenweg, aber
    /// <c>T_NOCT</c> geht in beide PV-Modelle: Wo der Katalogwert ausserhalb des
    /// Fensters 20…60 °C lag, rechnete <c>SimulationPV.NoctDesModuls</c> mit dem
    /// Rueckfall 45 °C — steht dort danach der Listenwert, rechnet sie mit ihm.
    /// <b>Fuer diesen einen Schritt ist die Abnahme deshalb nicht die Byte-Gleichheit,
    /// sondern die ERKLAERTE Abweichung</b> und eine neu eingefrorene Basis; die
    /// Begruendung steht in <c>Referenzlaeufe/LIESMICH.md</c>.</para>
    ///
    /// <para><b>Schritt 73</b> legt <c>Tab_SpeicherAuslegung</c> samt eindeutigem Index
    /// an (DDL bei <c>SpeicherAuslegungCtrl</c>), <b>Schritt 74</b> (Auftrag #178) baut
    /// dieselbe Tabelle als <b>STRICT</b>-Tabelle NEU auf — der erste TABELLENNEUBAU
    /// dieses Werkzeugs, weil SQLite kein <c>ALTER TABLE … STRICT</c> kennt. Die sechs
    /// Anweisungen und die Vorabprobe stehen in <c>SpeicherAuslegungStrict</c>, also
    /// wieder in DERSELBEN Quelle, aus der sich
    /// <c>SchemaMigration.Schritt_74_SpeicherauslegungStrict</c> bedient. Beide Schritte
    /// sind ergebnisneutral: 73 legt keine Zeile an, 74 kopiert die vorhandenen samt
    /// ihrer <c>ID</c>.</para>
    ///
    /// <para><b>Schritt 75</b> (Auftrag #269) legt <c>Tab_Nutzungsdauer</c> samt
    /// eindeutigem Index an, haengt die nullbare Verweisspalte <c>NutzungsdauerID</c> an
    /// <c>Tab_KostenVorlagePosition</c> und <c>Tab_ProjektWerte</c>, saet die
    /// Auslieferungszeilen und ordnet die Auslieferungspositionen ueber ihren Namen zu.
    /// Alles aus <c>NutzungsdauerSchema</c> - wieder DIESELBE Quelle, aus der sich
    /// <c>SchemaMigration.Schritt_75_Nutzungsdauer</c> bedient. Ergebnisneutral:
    /// <c>Tab_ProjektWerte</c> bekommt die SPALTE, aber keinen Wert.</para>
    ///
    /// <para><b>Schritt 76</b> (Auftrag #278) entdoppelt <c>energy_project_settings</c>
    /// und legt darueber den eindeutigen Index ueber
    /// <c>(ID_Projekt, ID_Energietraeger)</c> an - beides aus
    /// <c>ProjektEnergietraegerEindeutig</c>, wieder DIESELBE Quelle, aus der sich
    /// <c>SchemaMigration.Schritt_76_TraegersatzEindeutig</c> bedient. Ergebnisneutral:
    /// Der Index aendert keinen Wert, und die Messlatte hat nichts zu entdoppeln.</para>
    ///
    /// <para><b>Schritt 77</b> (Auftrag #284) stellt die ausgelieferte
    /// Investitionsvorlage des Pufferspeichers auf die Bemessung je LITER
    /// Gesamtvolumen um - aus <c>PufferspeicherBemessungVolumen</c>, wieder DIESELBE
    /// Quelle, aus der sich <c>SchemaMigration.Schritt_77_PufferVolumenbemessung</c>
    /// bedient. Ergebnisneutral: Umgestellt wird nur eine Zeile OHNE gepflegten Satz;
    /// sie gibt allein die Art vor, keine Zahl.</para>
    ///
    /// <para><b>Schritt 78</b> (Auftrag #287) stellt die ausgelieferte
    /// Investitionsposition "Batteriespeicher" der Photovoltaik auf den FESTEN BETRAG um -
    /// aus <c>PvVorlageBatteriespeicher</c>, wieder DIESELBE Quelle, aus der sich
    /// <c>SchemaMigration.Schritt_78_PvBatteriespeicher</c> bedient. Ergebnisneutral:
    /// Umgestellt wird nur eine Zeile OHNE gepflegten Satz; sie gibt allein die Art vor,
    /// keine Zahl.</para>
    ///
    /// <para><b>Schritt 79</b> (Auftrag #299) uebergibt den Heizstab an die
    /// WAERMEPUMPEN-ANLAGEN und entfernt danach den Projektschalter
    /// <c>Tab_Einstellungen.WP_Heizstab</c> - aus <c>HeizstabJeWaermepumpe</c>, DERSELBEN
    /// Quelle, aus der sich <c>SchemaMigration.Schritt_79_HeizstabJeWp</c> bedient.
    /// Ergebnisneutral: Jede Waermepumpe bekommt genau den Wert, mit dem ihr Projekt
    /// gerechnet hat. <b>Schritt 80</b> (derselbe Auftrag) gibt <c>Tab_WP</c> den
    /// Katalogverweis <c>ID_Stamm</c> samt Index und traegt ihn bei EINDEUTIGEM
    /// Bezeichner nach - aus <c>WaermepumpeKatalogverweis</c>. Ergebnisneutral: Kein
    /// Rechenweg liest die Spalte. <b>Schritt 81</b> (Auftrag #302) baut
    /// <c>Tab_ProjektWerte</c> neu auf, damit der Fremdschluessel auf
    /// <c>Tab_Kostenfaktor</c> <c>ON DELETE RESTRICT</c> statt <c>CASCADE</c> traegt -
    /// aus <c>ProjektWerteLoeschschutz</c>. Ergebnisneutral: Zeilen, IDs und
    /// AUTOINCREMENT-Stand bleiben, nur die Loeschregel wechselt. <b>Schritt 82</b>
    /// (Auftrag #303) haengt <c>Tab_Einstellungen</c> die Merkspalte
    /// <c>Kaskade_Gepflegt</c> an (0/1, <c>NOT NULL DEFAULT 0</c>) - aus
    /// <c>SchemaKatalog.Schritt82_KaskadeGepflegt</c>. Ergebnisneutral: kein DML, im
    /// Bestand ueberall 0, und 0 heisst "wie bisher".</para>
    ///
    /// <para><b>Die Katalognachsaat</b> (Auftrag US-1) ist KEIN Schemaschritt und traegt
    /// deshalb keine Nummer. <c>GesetzKatalog.StelleKatalogSicher</c> holt die noch
    /// fehlenden Generationen von <c>Tab_Gesetzesparameter</c> nach - im Programm laeuft
    /// das beim Start, die Testdatenbank startet aber nie ein Programm. Ohne diesen
    /// Aufruf steht sie auf der Generation ihres letzten Anwendungsstarts; die Zeilen der
    /// Generation 7 fehlten genau deshalb. Ergebnisneutral: Zu jedem Schluessel des
    /// Katalogs fuehrt der Kern eine wertgleiche Code-Rueckfallebene.</para>
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1 || args[0] == "--hilfe" || args[0] == "-h")
            {
                Console.WriteLine("Aufruf: Testdatenbankschema <pfad-zur.sqlite> [--trocken]");
                Console.WriteLine();
                Console.WriteLine("  Zieht die Datei auf Schemastand " + SchemaStand.Zielversion +
                                  " nach (Schritte 62 bis " + SchemaStand.Zielversion + "), saet den Gesetzeskatalog nach");
                Console.WriteLine("  und fuehrt danach VACUUM aus.");
                Console.WriteLine("  --trocken  nur berichten, nichts aendern.");
                return 2;
            }

            string pfad = Path.GetFullPath(args[0]);
            bool trocken = Array.IndexOf(args, "--trocken") >= 0;

            if (!File.Exists(pfad))
            {
                Console.Error.WriteLine("Datei nicht gefunden: " + pfad);
                return 2;
            }

            DataRepository.PfadUeberschreibung = pfad;

            // DIE WERKZEUG-FREIGABE DER SCHREIBNAHT (Welle iF30) - EINE benannte Zeile,
            // ausdruecklich und nicht durch Auslassen. Dieses Werkzeug legt Spalten an und
            // schreibt den Schemamarker; eine Lizenz hat es nicht und braucht es nicht.
            Schreibnaht.WerkzeugFreigabe("Werkzeug Testdatenbankschema");

            Console.WriteLine("Datei:  " + pfad);
            Console.WriteLine("Groesse vorher: " + Mb(pfad));

            int vorher = SchemaVersionLesen();
            Console.WriteLine("Schemastand vorher: " + vorher + "   (Zielstand " + SchemaStand.Zielversion + ")");
            Console.WriteLine();

            if (trocken) Console.WriteLine("--trocken: es wird nichts geschrieben.");
            Console.WriteLine();

            int angelegt = 0;

            // ---- Schritt 62: die verwaisten Klimadaten-Zeilen (kein DDL, zwei DELETE) ----
            long waisen = 0;
            foreach (string tabelle in KlimaWaisenBereinigung.Datenblocktabellen())
            {
                long z = Zahl(KlimaWaisenBereinigung.ZaehlungZu(tabelle));
                waisen += Math.Max(0, z);
                Console.WriteLine("Schritt 62 - " + tabelle + ": Waisen " + z + ".");
                if (!trocken && z > 0)
                    DataRepository.ExecuteNonQuery(KlimaWaisenBereinigung.LoeschungZu(tabelle));
            }
            Console.WriteLine("Schritt 62: " + (waisen == 0
                ? "nichts zu tun - kein Datenblock ohne Kopfsatz."
                : waisen + " verwaiste Zeile(n) abgeraeumt."));
            Console.WriteLine();

            // ---- Schritt 63: zwei PV-Anlagenparameter. Der Katalog fuehrt DOUBLE, die
            //      STRICT-Tabelle nimmt REAL - wortgleich zu Schritt_63_PvAnlagenparameter.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt63_PvAnlagenparameter)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name, "REAL", 63, trocken);

            // ---- Schritt 64: sechs Spalten der Modellwahl, dazu Stammtabelle und
            //      Degradation. Typ ueber dieselbe Uebersetzung wie die Rueckfallebene.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvModellwahl)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 64, trocken);
            foreach (SchemaSpalte s in SchemaKatalog.Schritt64_PvStammUndDegradation)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 64, trocken);

            // ---- Schritt 65: der Wechselrichterkatalog und seine Projektkopie.
            //      Die DDL kommt aus WechselrichterSchema - DIESELBE Quelle, aus der
            //      sich SchemaMigration.Schritt_65_Wechselrichterkatalog bedient.
            //      CREATE TABLE IF NOT EXISTS ist selbst idempotent.
            int tabellen = 0;
            foreach (KeyValuePair<string, string> a in WechselrichterSchema.Anweisungen)
                tabellen += TabelleSicherstellen(a.Key, a.Value, 65, trocken);

            // ---- Schritt 66: die Strangzuordnung und der sichtbare Wechselrichterweg.
            //      Zwei Quellen, beide im Kern - die TABELLE aus AnlageStrangSchema, die
            //      SPALTE aus SchemaKatalog.Schritt66_PvWechselrichterweg. DIESELBEN,
            //      aus denen sich SchemaMigration.Schritt_66_Strangzuordnung bedient.
            foreach (KeyValuePair<string, string> a in AnlageStrangSchema.Anweisungen)
                tabellen += TabelleSicherstellen(a.Key, a.Value, 66, trocken);

            foreach (SchemaSpalte s in SchemaKatalog.Schritt66_PvWechselrichterweg)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 66, trocken);

            // ---- Schritt 67: die sichtbare BHKW-Leistungsuntergrenze (W6-E-7).
            //      DAS ERSTE DML DIESES WERKZEUGS mit einem FACHWERT - anders als die
            //      Schritte 63 bis 66, die nur Spalten und leere Tabellen anlegen. Es
            //      ist trotzdem ergebnisneutral: Genau die Saetze, die es anfasst,
            //      rechneten bisher ueber den stillen Fallback in SimulationBHKW mit
            //      denselben 30 %. Die Anweisung kommt aus BhkwLeistungsgrenzeVorgabe -
            //      DIESELBE Quelle, aus der sich SchemaMigration.Schritt_67_
            //      BhkwLeistungsgrenze bedient. Idempotent ueber ihr eigenes IS NULL.
            long ohneWert = Zahl(BhkwLeistungsgrenzeVorgabe.Zaehlung());
            Console.WriteLine("Schritt 67 - " + BhkwLeistungsgrenzeVorgabe.TABELLE + "." +
                              BhkwLeistungsgrenzeVorgabe.SPALTE + ": ohne gepflegten Wert " +
                              ohneWert + ".");
            if (!trocken && ohneWert > 0)
                DataRepository.ExecuteNonQuery(BhkwLeistungsgrenzeVorgabe.Anhebung());
            Console.WriteLine("Schritt 67: " + (ohneWert == 0
                ? "nichts zu tun - jeder Satz fuehrt einen gepflegten Wert."
                : ohneWert + " Satz/Saetze auf " +
                  BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT + " % gehoben."));
            Console.WriteLine();

            // ---- Schritt 68: der Hersteller des Stromspeicherkatalogs (W14a-E-10-Q7).
            //      ERST die zwei Spalten, DANN der Nachtrag - das UPDATE nennt Firma
            //      und liefe auf einer Datenbank ohne die Spalte in einen Fehler. Die
            //      Quellen sind dieselben, aus denen sich
            //      SchemaMigration.Schritt_68_StromspeicherFirma bedient:
            //      SchemaKatalog.Schritt68_StromspeicherFirma und
            //      StromspeicherFirmaNachtrag. Ergebnisneutral - kein Rechenweg liest
            //      den Hersteller.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt68_StromspeicherFirma)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 68, trocken);

            // ---- Schritt 70: die PV-Strangpruefung (W6-B-10 und W6-B-11). Vier
            //      Spalten, kein DML: der Kurzschlussstrom je MPPT in Katalog und
            //      Projektkopie des Wechselrichters und die zwei
            //      Auslegungstemperaturen an Tab_Einstellungen. Die Quellen sind
            //      dieselben, aus denen sich SchemaMigration.Schritt_70_
            //      PvStrangpruefung bedient. Ergebnisneutral - NULL heisst bei allen
            //      vieren "wie bisher".
            foreach (SchemaSpalte s in SchemaKatalog.Schritt70_WrKurzschlussstrom)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 70, trocken);
            foreach (SchemaSpalte s in SchemaKatalog.Schritt70_Auslegungstemperaturen)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 70, trocken);

            // ---- Schritt 71: der Szenario-Parametersatz der Wirtschaftlichkeit
            //      (W5-B-9). Zwoelf nullbare Spalten an Tab_ProjektWirtschaftlichkeit,
            //      kein DML - NULL heisst bei allen zwoelfen "Vorgabe". Die Quelle ist
            //      dieselbe, aus der sich SchemaMigration.Schritt_71_Szenarioparameter
            //      bedient. Erwartet bleibt zahlengleich; Best und Worst rechnen ab
            //      diesem Stand mit den Vorgaben.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt71_Szenarioparameter)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 71, trocken);

            // ---- Schritt 72: die VALERI-Ergaenzung (W5-B-12). Drei Spalten fuer den
            //      Preisaenderungssatz der kapitalgebundenen Kosten p_I (Projektwert,
            //      Best, Worst) und eine Freitextspalte fuer die nicht monetaeren
            //      Wirkungen, alle an Tab_ProjektWirtschaftlichkeit; kein DML. Die
            //      Quelle ist dieselbe, aus der sich SchemaMigration.Schritt_72_
            //      ValeriErgaenzung bedient. Ergebnisneutral - NULL heisst bei p_I
            //      "wie p_B" und wird erst wirksam, wenn der Parametersatz die Spalte
            //      liest; ein Projekt ohne Ersatzbeschaffung bleibt auch dann
            //      zahlengleich.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 72, trocken);

            // ---- Schritt 82: die Merkspalte der gepflegten Kaskade (Auftrag #303).
            //      EINE Ja/Nein-Spalte an Tab_Einstellungen, kein DML. Die Quelle ist
            //      dieselbe, aus der sich SchemaMigration.Schritt_82_KaskadeGepflegt
            //      bedient. Ergebnisneutral: Im Bestand steht ueberall 0, und 0 heisst
            //      "die Kaskade hat niemand von Hand angefasst" - die Automatik
            //      KonfigurationCtrl.HeizkesselNachziehen greift unveraendert. Die
            //      dreizehn Referenzprojekte behalten damit ihre Kaskade.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt82_KaskadeGepflegt)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 82, trocken);

            // ---- Schritt 86: die Lastspitzenkappung als Berechnungsart des
            //      Einzelspeichers (Entscheide LS-E-1 (a)/LS-E-3). ZWEI Spalten an
            //      Tab_StromspeicherVariante, kein DML. Die Quelle ist dieselbe, aus der
            //      sich SchemaMigration.Schritt_86_Lastspitzenkappung bedient.
            //      Ergebnisneutral: Gelesen wird beides nur bei Berechnungsart
            //      "Lastspitzenkappung", und die fuehrt im Bestand keine Variante - die
            //      dreizehn Referenzprojekte rechnen unveraendert.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt86_Lastspitzenkappung)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 86, trocken);

            // ---- Schritt 88: der Modus der Stromsteuerbefreiung § 9 Abs. 1 Nr. 3
            //      (Etappe B6, Befund B-1). EINE Spalte an Tab_ProjektWirtschaftlichkeit,
            //      kein DML. Die Quelle ist dieselbe, aus der sich
            //      SchemaMigration.Schritt_88_StromsteuerModus bedient. NULL heisst
            //      AUSWEIS; im Bestand bucht kein gespeicherter Lauf die Erloesreihe -
            //      die dreizehn Referenzprojekte rechnen unveraendert.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt88_StromsteuerModus)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 88, trocken);

            // ---- Schritt 88, zweiter Teil: die KONSERVENSPALTE des Ergebnisses.
            //      Tab_ErgebnisWirtschaftlichkeit ist keine Schematabelle - sie entsteht
            //      und waechst ueber WirtschaftlichkeitCtrl.SpalteSicher, also erst beim
            //      ersten Lauf der Anwendung. Fuer die REPO-Testdatenbank reicht das
            //      nicht: Sie ist die Messlatte des SqlDialektpruefers, und der loest
            //      das INSERT des Ergebnisses gegen genau diese Datei auf. Ohne die
            //      Spalte meldete er eine Fundstelle, die in der Anwendung keine ist.
            //      Die Quelle ist dieselbe Konstante, die auch der Ctrl nimmt.
            angelegt += SpalteSicherstellen(WirtschaftlichkeitCtrl.TAB_ERGEBNIS,
                                            WirtschaftlichkeitCtrl.SPALTE_STROMST_MODUS,
                                            "TEXT", 88, trocken);

            // ---- Etappe B7P: die KONSERVENSPALTE des Nachweisumschlags. KEIN eigener
            //      Schritt - Tab_ErgebnisWirtschaftlichkeit ist keine Schematabelle, und
            //      die Zielversion bleibt 89. Wortgleiche Begruendung wie oben: Der
            //      SqlDialektpruefer loest das INSERT des Ergebnisses gegen diese Datei
            //      auf und meldete ohne die Spalte eine Fundstelle, die in der
            //      Anwendung keine ist. Die Quelle ist dieselbe Konstante, die auch der
            //      Ctrl nimmt.
            angelegt += SpalteSicherstellen(WirtschaftlichkeitCtrl.TAB_ERGEBNIS,
                                            WirtschaftlichkeitCtrl.SPALTE_NACHWEIS_JSON,
                                            "TEXT", 89, trocken);

            // ---- Schritt 89: die Anlagenwahrheit des KWK-Zuschlags (Etappe BK1,
            //      Entscheid BK-E-1 a). EINE Spalte an Tab_Energieanlagen UND neun
            //      Datenanweisungen. Beide Quellen sind dieselben, aus denen sich
            //      SchemaMigration.Schritt_89_KwkAnlagenwahrheit bedient:
            //      SchemaKatalog.Schritt89_KwkAnlagenwahrheit (DDL) und
            //      KwkAnlagenwahrheit (DML).
            //      Ergebnisneutral: Jede BHKW-Anlage bekommt genau den Projektwert
            //      eingetragen, den der Rueckfall Anlage -> Projekt ihr bisher
            //      zugewiesen hat; eine gepflegte Anlagenzelle bleibt unangetastet. Die
            //      Basis fuehrt ohnehin keine Geldgroesse - die dreizehn
            //      Referenzprojekte bleiben byte-gleich.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt89_KwkAnlagenwahrheit)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 89, trocken);

            foreach (KwkAnlagenwahrheit.Paar paar in KwkAnlagenwahrheit.Paare)
            {
                object offen = DataRepository.ExecuteScalar(KwkAnlagenwahrheit.Zaehlung(paar));
                long z = offen == null || offen == DBNull.Value ? 0 : Convert.ToInt64(offen);
                Console.WriteLine("Schritt 89 - " + paar.Anlage + " aus " + paar.Projekt +
                                  ": " + z + " Anlagenzeile(n) nachzutragen.");
                if (!trocken && z > 0)
                    DataRepository.ExecuteNonQuery(KwkAnlagenwahrheit.Uebertragung(paar));
            }

            tabellen += TabelleSicherstellen("Tab_SpeicherAuslegung", SpeicherAuslegungCtrl.SQL_TABELLE, 73, trocken);
            if (!trocken) DataRepository.ExecuteNonQuery(SpeicherAuslegungCtrl.SQL_INDEX);

            // ---- Schritt 74: dieselbe Tabelle als STRICT-Tabelle (Auftrag #178).
            //      DER ERSTE TABELLENNEUBAU dieses Werkzeugs - SQLite kennt kein
            //      ALTER TABLE ... STRICT, also CREATE unter Hilfsnamen, INSERT ...
            //      SELECT mit namentlich genannten Spalten, DROP, RENAME und Index neu,
            //      alles in EINER Transaktion. Die sechs Anweisungen und die Vorabprobe
            //      kommen aus SpeicherAuslegungStrict - DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_74_SpeicherauslegungStrict bedient.
            //      Ergebnisneutral: Zeilen und IDs werden mitkopiert, kein Wert und kein
            //      Typ aendert sich. Wiederholbar ueber die Vorabprobe.
            long ohneStrict = Zahl(SpeicherAuslegungStrict.Zaehlung());
            Console.WriteLine("Schritt 74 - " + SpeicherAuslegungStrict.TABELLE +
                              " ohne STRICT: " + ohneStrict + ".");
            if (!trocken)
            {
                long zeilenVorher = Zahl("SELECT COUNT(*) FROM " + SpeicherAuslegungStrict.TABELLE);
                bool umgebaut = SpeicherAuslegungStrict.Umbauen();
                long zeilenNachher = Zahl("SELECT COUNT(*) FROM " + SpeicherAuslegungStrict.TABELLE);
                Console.WriteLine("Schritt 74: " + (umgebaut
                    ? "neu aufgebaut, " + zeilenVorher + " Zeile(n) vorher, " +
                      zeilenNachher + " nachher."
                    : "nichts zu tun - die Tabelle ist bereits STRICT."));
                if (umgebaut) tabellen++;
            }
            Console.WriteLine();

            // ---- Schritt 75: die Nutzungsdauertabelle (Auftrag #269, Konzept
            //      "Nutzungsdauer je Technik und Positionsart", Stufe S1). Vier
            //      Handgriffe in fester Reihenfolge - Tabelle samt Index, die zwei
            //      Verweisspalten (ihr REFERENCES zeigt auf die eben angelegte
            //      Tabelle), die Saat und die Saat-Zuordnung (sie braucht die Ids der
            //      Saat). Alle vier kommen aus NutzungsdauerSchema - DIESELBE Quelle,
            //      aus der sich SchemaMigration.Schritt_75_Nutzungsdauer bedient.
            //      Ergebnisneutral: Tab_ProjektWerte bekommt die Spalte, aber KEINEN
            //      Wert. Wiederholbar ueber IF NOT EXISTS, die Spaltenprobe und die
            //      Vorabfragen der Saat.
            tabellen += TabelleSicherstellen(NutzungsdauerSchema.TABELLE,
                                             NutzungsdauerSchema.SQL_CREATE, 75, trocken);
            if (!trocken) DataRepository.ExecuteNonQuery(NutzungsdauerSchema.SQL_INDEX);

            foreach (KeyValuePair<string, string> s in NutzungsdauerSchema.Verweisspalten)
                angelegt += SpalteSicherstellen(s.Key, NutzungsdauerSchema.SPALTE_VERWEIS,
                                                s.Value, 75, trocken);

            if (!trocken)
            {
                int gesaet = NutzungsdauerSchema.SaatSchreiben();
                int zugeordnet = NutzungsdauerSchema.ZuordnungSchreiben();
                Console.WriteLine("Schritt 75 - Saat: " + gesaet + " von " +
                                  NutzungsdauerSchema.Saat.Length + " Zeile(n) geschrieben, " +
                                  "Tabelle jetzt " + Zahl(NutzungsdauerSchema.Zaehlung()) +
                                  " Zeile(n).");
                Console.WriteLine("Schritt 75 - Zuordnung: " + zugeordnet +
                                  " Auslieferungsposition(en) gesetzt, insgesamt " +
                                  Zahl(NutzungsdauerSchema.ZaehlungZuordnung()) +
                                  " mit Verweis.");
                Console.WriteLine("Schritt 75 - " + NutzungsdauerSchema.TABELLE +
                                  " ohne STRICT: " + Zahl(NutzungsdauerSchema.ZaehlungOhneStrict()) +
                                  " (erwartet 0).");
            }
            Console.WriteLine();

            // ---- Schritt 76: ein Satz je Energietraeger und Projekt (Auftrag #278).
            //      Zwei Anweisungen in FESTER Reihenfolge - erst die Entdoppelung des
            //      Bestands, dann der eindeutige Index; umgekehrt scheiterte die Anlage
            //      an der ersten Dublette. Beide kommen aus
            //      ProjektEnergietraegerEindeutig - DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_76_TraegersatzEindeutig bedient.
            //      Ergebnisneutral: Behalten wird je Paar die Zeile mit der kleinsten ID,
            //      genau die, die jede Lesekette schon bisher genommen hat.
            if (!trocken)
            {
                long ueberzaehlig = Zahl(ProjektEnergietraegerEindeutig.Zaehlung());
                Console.WriteLine("Schritt 76 - " + ProjektEnergietraegerEindeutig.TABELLE +
                                  ": ueberzaehlige Zeilen " + ueberzaehlig + ".");
                if (ueberzaehlig > 0)
                    DataRepository.ExecuteNonQuery(ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN);
                DataRepository.ExecuteNonQuery(ProjektEnergietraegerEindeutig.SQL_INDEX);
                Console.WriteLine("Schritt 76 - Index " + ProjektEnergietraegerEindeutig.INDEX +
                                  ": " + Zahl(ProjektEnergietraegerEindeutig.ZaehlungIndex()) +
                                  " (erwartet 1), ueberzaehlig jetzt " +
                                  Zahl(ProjektEnergietraegerEindeutig.Zaehlung()) + ".");
            }
            Console.WriteLine();

            // ---- Schritt 77: das Volumen ist die einzige Bezugsgroesse des
            //      Pufferspeichers (Auftrag #284). Die eine Anweisung kommt aus
            //      PufferspeicherBemessungVolumen - DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_77_PufferVolumenbemessung bedient.
            //      Ergebnisneutral: Umgestellt wird nur eine Vorlagenzeile OHNE
            //      gepflegten Satz - sie gibt allein die Art vor, keine Zahl. Eine Zeile
            //      MIT Satz bliebe eine Zahl je kWh und wuerde durch die Umstellung zu
            //      einer Zahl je Liter; genau das ist ausgeschlossen.
            if (!trocken)
            {
                long offen = Zahl(PufferspeicherBemessungVolumen.Zaehlung());
                long gepflegt = Zahl(PufferspeicherBemessungVolumen.ZaehlungGepflegt());
                Console.WriteLine("Schritt 77 - Pufferspeicher-Vorlage: umzustellen " + offen +
                                  ", mit gepflegtem Satz (bleibt) " + gepflegt + ".");
                if (offen > 0)
                    DataRepository.ExecuteNonQuery(PufferspeicherBemessungVolumen.SQL_UMSTELLEN);
                Console.WriteLine("Schritt 77 - offen jetzt " +
                                  Zahl(PufferspeicherBemessungVolumen.Zaehlung()) +
                                  " (erwartet 0).");
            }
            Console.WriteLine();

            // ---- Schritt 78: der feste Betrag fuer die PV-Position "Batteriespeicher"
            //      (Auftrag #287). Die eine Anweisung kommt aus PvVorlageBatteriespeicher
            //      - DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_78_PvBatteriespeicher bedient.
            //      Ergebnisneutral: Umgestellt wird nur eine Vorlagenzeile OHNE
            //      gepflegten Satz - sie gibt allein die Art vor, keine Zahl. Eine Zeile
            //      MIT Satz bliebe eine Zahl je kWh und wuerde durch die Umstellung zu
            //      einem Gesamtbetrag; genau das ist ausgeschlossen.
            if (!trocken)
            {
                long offen = Zahl(PvVorlageBatteriespeicher.Zaehlung());
                long gepflegt = Zahl(PvVorlageBatteriespeicher.ZaehlungGepflegt());
                Console.WriteLine("Schritt 78 - PV-Vorlage \"" +
                                  PvVorlageBatteriespeicher.POSITION + "\": umzustellen " +
                                  offen + ", mit gepflegtem Satz (bleibt) " + gepflegt + ".");
                if (offen > 0)
                    DataRepository.ExecuteNonQuery(PvVorlageBatteriespeicher.SQL_UMSTELLEN);
                Console.WriteLine("Schritt 78 - offen jetzt " +
                                  Zahl(PvVorlageBatteriespeicher.Zaehlung()) +
                                  " (erwartet 0).");
            }
            Console.WriteLine();

            // ---- Schritt 79: der Heizstab gehoert der Waermepumpe (Auftrag #299).
            //      Zwei Anweisungen in FESTER Reihenfolge - erst die Uebernahme des
            //      Projektschalters an jede Waermepumpen-Anlage, dann das Entfernen der
            //      Projektspalte; umgekehrt waere der Wert weg, bevor er an den Anlagen
            //      steht. Beide kommen aus HeizstabJeWaermepumpe - DIESELBE Quelle, aus
            //      der sich SchemaMigration.Schritt_79_HeizstabJeWp bedient.
            //      Ergebnisneutral: Jede Waermepumpe bekommt genau den Wert, mit dem ihr
            //      Projekt gerechnet hat.
            if (!HeizstabJeWaermepumpe.ProjektschalterVorhanden())
            {
                Console.WriteLine("Schritt 79 - nichts zu tun: " +
                                  HeizstabJeWaermepumpe.TABELLE_EINSTELLUNGEN + "." +
                                  HeizstabJeWaermepumpe.SPALTE_PROJEKT +
                                  " gibt es nicht mehr.");
            }
            else
            {
                long umzustellen = Zahl(HeizstabJeWaermepumpe.Zaehlung());
                long mitStab = Zahl(HeizstabJeWaermepumpe.ZaehlungEinschalten());
                Console.WriteLine("Schritt 79 - Waermepumpen-Anlagen: umzustellen " +
                                  umzustellen + ", danach MIT Heizstab " + mitStab + ".");
                if (!trocken)
                {
                    DataRepository.ExecuteNonQuery(HeizstabJeWaermepumpe.SqlUebernahme());
                    Console.WriteLine("Schritt 79 - offen jetzt " +
                                      Zahl(HeizstabJeWaermepumpe.Zaehlung()) + " (erwartet 0).");
                    DataRepository.ExecuteNonQuery(HeizstabJeWaermepumpe.SqlSpalteEntfernen());
                    Console.WriteLine("Schritt 79 - Projektschalter entfernt: " +
                                      (HeizstabJeWaermepumpe.ProjektschalterVorhanden()
                                          ? "NEIN (Fehler)" : "ja"));
                }
            }
            Console.WriteLine();

            // ---- Schritt 80: der Katalogverweis der WP-Projektkopie (Auftrag #299).
            //      Drei Handgriffe in fester Reihenfolge - Spalte, Index, Nachtrag; alle
            //      aus WaermepumpeKatalogverweis, DERSELBEN Quelle, aus der sich
            //      SchemaMigration.Schritt_80_WpKatalogverweis bedient. NICHT ueber
            //      SpalteSicherstellen: Dessen Typuebersetzung kennt nur Access-Typnamen
            //      und schnitte das REFERENCES weg (wie bei Schritt 75).
            //      Ergebnisneutral: Kein Rechenweg liest die Spalte.
            if (!trocken)
            {
                if (!WaermepumpeKatalogverweis.SpalteVorhanden())
                {
                    DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SQL_SPALTE);
                    angelegt++;
                    Console.WriteLine("Schritt 80 - Spalte " + WaermepumpeKatalogverweis.TABELLE +
                                      "." + WaermepumpeKatalogverweis.SPALTE + " angelegt.");
                }
                else
                {
                    Console.WriteLine("Schritt 80 - Spalte " + WaermepumpeKatalogverweis.TABELLE +
                                      "." + WaermepumpeKatalogverweis.SPALTE + ": bereits vorhanden.");
                }

                DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SQL_INDEX);

                long nachzutragen = Zahl(WaermepumpeKatalogverweis.Zaehlung());
                Console.WriteLine("Schritt 80 - Projektkopien mit eindeutigem Katalogsatz: " +
                                  nachzutragen + ".");
                if (nachzutragen > 0)
                    DataRepository.ExecuteNonQuery(WaermepumpeKatalogverweis.SqlNachtrag());
                Console.WriteLine("Schritt 80 - offen jetzt " +
                                  Zahl(WaermepumpeKatalogverweis.Zaehlung()) +
                                  " (erwartet 0), ohne Verweis geblieben " +
                                  Zahl(WaermepumpeKatalogverweis.ZaehlungOhneVerweis()) + ".");
            }
            Console.WriteLine();

            if (!trocken)
            {
                long ohnePraefix = Zahl(StromspeicherFirmaNachtrag.Zaehlung());
                Console.WriteLine("Schritt 68 - " + StromspeicherFirmaNachtrag.TABELLE + "." +
                                  StromspeicherFirmaNachtrag.SPALTE +
                                  ": nachzutragen " + ohnePraefix + " von " +
                                  Zahl(StromspeicherFirmaNachtrag.Gesamtzahl()) + ".");
                if (ohnePraefix > 0)
                    DataRepository.ExecuteNonQuery(StromspeicherFirmaNachtrag.Nachtrag());
                Console.WriteLine("Schritt 68: " + (ohnePraefix == 0
                    ? "nichts zu tun - kein Satz traegt ein Bezeichnerpraefix."
                    : ohnePraefix + " Satz/Saetze aus dem Bezeichnerpraefix nachgetragen."));
            }
            Console.WriteLine();

            // ---- Schritt 69: die verdorbenen PV-Modulkoeffizienten (W6-B-5, Q1 bis Q3).
            //      DER ERSTE SCHRITT DIESES WERKZEUGS, DER EIN RECHENERGEBNIS AENDERT -
            //      mit Absicht: T_NOCT geht in beide PV-Modelle, und wo der Katalogwert
            //      ausserhalb des Fensters 20 bis 60 Grad C lag, rechnete SimulationPV
            //      mit dem Rueckfall 45 Grad C. Genau das war der Zweck des Entscheids
            //      Q3, und genau deshalb verlangt dieser Schritt eine neue
            //      Referenzbasis (Referenzlaeufe/LIESMICH.md). alpha_SC und beta_OC
            //      liest kein Rechenweg - sie sind die Ampel des PV-Dialogs.
            //      Die Anweisungen kommen aus PvKoeffizientenReparatur - DIESELBE
            //      Quelle, aus der sich SchemaMigration.Schritt_69_PvKoeffizienten
            //      bedient. Reihenfolge und Idempotenz sind dort begruendet.
            SchrittPvKoeffizienten(trocken);
            Console.WriteLine();

            // ---- Schritt 81: der Loeschschutz der Projektkosten (Auftrag #302).
            //      DER ZWEITE TABELLENNEUBAU DIESES WERKZEUGS. Er steht ZULETZT, weil er
            //      Tab_ProjektWerte vollstaendig kopiert - jede Spalte, die ein
            //      frueherer Schritt anlegt, muss vorher dastehen (Schritt 75 haengt die
            //      Verweisspalte NutzungsdauerID an). Die Anweisungen kommen aus
            //      ProjektWerteLoeschschutz - DERSELBEN Quelle, aus der sich
            //      SchemaMigration.Schritt_81_ProjektWerteLoeschschutz bedient; der Umbau
            //      laeuft dort in EINER Transaktion, hier wie dort ueber Umbauen().
            //      Ergebnisneutral: Zeilen, IDs und AUTOINCREMENT-Stand bleiben, nur die
            //      Loeschregel des Fremdschluessels auf Tab_Kostenfaktor wechselt von
            //      CASCADE auf RESTRICT.
            long mitKaskade = Zahl(ProjektWerteLoeschschutz.Zaehlung());
            long zeilenVorher81 = Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen());
            Console.WriteLine("Schritt 81 - " + ProjektWerteLoeschschutz.TABELLE +
                              " mit ON DELETE CASCADE auf " + ProjektWerteLoeschschutz.KATALOG +
                              ": " + mitKaskade + "   (Zeilen " + zeilenVorher81 + ").");
            if (!trocken)
            {
                if (ProjektWerteLoeschschutz.Umbauen())
                    Console.WriteLine("Schritt 81 - neu aufgebaut; Loeschregel jetzt ON DELETE " +
                                      ProjektWerteLoeschschutz.LOESCHREGEL + " (" +
                                      Zahl(ProjektWerteLoeschschutz.ZaehlungNeueRegel()) +
                                      ", erwartet 1), CASCADE noch " +
                                      Zahl(ProjektWerteLoeschschutz.Zaehlung()) +
                                      " (erwartet 0), Zeilen " +
                                      Zahl(ProjektWerteLoeschschutz.ZaehlungZeilen()) +
                                      " (erwartet " + zeilenVorher81 + ").");
                else
                    Console.WriteLine("Schritt 81: nichts zu tun - die Loeschregel steht bereits.");
            }
            Console.WriteLine();

            // ---- Schritt 83: die Strompreis-Details (Entscheide SP-E-2/SP-E-3).
            //      Neun Spalten an energy_project_settings UND die Faltung des bisher
            //      wirksamen Aufschlags in den Arbeitspreis. Spalten aus
            //      SchemaKatalog.Schritt83_Strompreisdetails, Faltung aus
            //      StrompreisZerlegung - DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_83_Strompreisdetails bedient.
            //      DIESER SCHRITT SCHREIBT GELDWERTE, und gerade dadurch bleibt er
            //      ergebnisneutral: Der Aufschlag kommt nicht mehr auf den Arbeitspreis,
            //      also muss er DARIN stehen. Nach der Faltung ist die Summe der aktiven
            //      Anteile der Arbeitspreis und die Summe ohne Beschaffung der alte
            //      Aufschlag - jede Preisreihe bleibt, wie sie war, der Referenzlauf
            //      byte-gleich. Wiederholbar: Ein zweiter Lauf findet nichts mehr.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt83_Strompreisdetails)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 83, trocken);

            if (!trocken)
            {
                int zuFalten = StrompreisZerlegung.ZaehlungFaltung();
                Console.WriteLine("Schritt 83 - Zeilen mit wirksamem Aufschlag: " + zuFalten + ".");
                foreach (string zeile in StrompreisZerlegung.Falten())
                    Console.WriteLine("Schritt 83 - " + zeile);
                if (zuFalten == 0)
                    Console.WriteLine("Schritt 83: nichts zu falten - kein wirksamer Aufschlag.");
            }
            Console.WriteLine();

            // ---- Schritt 84: der Umzug der Einspeiseverguetung (Entscheid SP-E-5 (a)).
            //      REINER DATENSCHRITT - keine Spalte. Die gepflegten Kartenwerte
            //      Verguetung_PV/_BHKW ziehen in Tab_ProjektWirtschaftlichkeit um
            //      (ct/kWh -> EUR/kWh); gepflegte Parameter gewinnen. Anweisungen aus
            //      VerguetungUmzug - DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_84_VerguetungUmzug bedient.
            //      Ergebnisneutral: Die Speicherwelt liest dieselbe Zahl von der neuen
            //      Stelle, der Referenzlauf bleibt byte-gleich. Wiederholbar: Ein
            //      zweiter Lauf findet nichts mehr.
            if (!trocken)
            {
                if (!VerguetungUmzug.KartenspaltenVorhanden())
                {
                    Console.WriteLine("Schritt 84: nichts umzuziehen - die Kartenspalten sind mit Schritt 85 entfallen.");
                }
                else
                {
                    int umzuziehen = VerguetungUmzug.ZaehlungUmzug();
                    Console.WriteLine("Schritt 84 - Projekte mit gepflegtem Kartenwert: " + umzuziehen + ".");
                    foreach (string zeile in VerguetungUmzug.Umziehen())
                        Console.WriteLine("Schritt 84 - " + zeile);
                    if (umzuziehen == 0)
                        Console.WriteLine("Schritt 84: nichts umzuziehen - kein gepflegter Kartenwert.");
                }
            }
            Console.WriteLine();

            // ---- Schritt 85: die Altspalten der Strompreis-Welle fallen weg.
            //      REINER ENTFERNUNGSSCHRITT - kein DML. Die fuenf Spalten, die die
            //      Schritte 83 und 84 ohne Leser zurueckgelassen haben (Aufschlag_Modus,
            //      Aufschlag_Override, Verguetung_PV, Verguetung_BHKW,
            //      Aufschlaege_Anwenden), werden entfernt. Anweisungen aus
            //      StrompreisAltspalten - DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_85_StrompreisAltspalten bedient.
            //      Ergebnisneutral: Keine der Spalten traegt eine Rechengroesse, der
            //      Referenzlauf bleibt byte-gleich. Wiederholbar: Ein zweiter Lauf
            //      findet keine der Spalten mehr.
            if (!trocken)
            {
                int altspalten = StrompreisAltspalten.Offen();
                Console.WriteLine("Schritt 85 - Altspalten der Strompreis-Welle: " + altspalten + ".");
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in StrompreisAltspalten.Anweisungen)
                {
                    DataRepository.ExecuteNonQuery(a.Value);
                    Console.WriteLine("Schritt 85 - " + a.Key + ".");
                }
                Console.WriteLine("Schritt 85 - offen jetzt " + StrompreisAltspalten.Offen() +
                                  " (erwartet 0).");
            }
            Console.WriteLine();

            // ---- Schritt 87: der entdoppelte Gesetzeskatalog (Entscheid US-E-1 (a)).
            //      Zwei Anweisungen in FESTER Reihenfolge - erst die Entdoppelung des
            //      Bestands, dann der eindeutige Index; umgekehrt scheiterte die Anlage
            //      an der ersten Dublette. Dazu der Beifang aus #321: je Projekt bleibt
            //      genau EINE aktive Speichervariante. Beides kommt aus
            //      GesetzesparameterEindeutig bzw. SpeicherVarianteAktivEindeutig -
            //      DENSELBEN Quellen, aus denen sich
            //      SchemaMigration.Schritt_87_GesetzesparameterEindeutig bedient.
            //      Ergebnisneutral: Behalten wird beide Male die kleinste ID, genau die,
            //      die jede Lesekette schon bisher genommen hat. Der Schritt steht VOR
            //      der Katalognachsaat - die saet in eine entdoppelte Tabelle.
            if (!trocken)
            {
                long dubletten = Zahl(GesetzesparameterEindeutig.Zaehlung());
                Console.WriteLine("Schritt 87 - " + GesetzesparameterEindeutig.TABELLE +
                                  ": ueberzaehlige Zeilen " + dubletten + ".");
                if (dubletten > 0)
                {
                    foreach (string zeile in PvKoeffizientenReparatur.Zerlege(
                                 Text(GesetzesparameterEindeutig.Protokollabfrage())))
                        Console.WriteLine("Schritt 87 - " + zeile);
                    DataRepository.ExecuteNonQuery(GesetzesparameterEindeutig.SQL_ENTDOPPELN);
                }
                DataRepository.ExecuteNonQuery(GesetzesparameterEindeutig.SQL_INDEX);
                Console.WriteLine("Schritt 87 - Index " + GesetzesparameterEindeutig.INDEX +
                                  ": " + Zahl(GesetzesparameterEindeutig.ZaehlungIndex()) +
                                  " (erwartet 1), ueberzaehlig jetzt " +
                                  Zahl(GesetzesparameterEindeutig.Zaehlung()) + ".");

                long mehrfach = Zahl(SpeicherVarianteAktivEindeutig.Zaehlung());
                Console.WriteLine("Schritt 87 - " + SpeicherVarianteAktivEindeutig.TABELLE +
                                  ": ueberzaehlige AKTIVE Zeilen " + mehrfach + ".");
                if (mehrfach > 0)
                {
                    foreach (string zeile in PvKoeffizientenReparatur.Zerlege(
                                 Text(SpeicherVarianteAktivEindeutig.Protokollabfrage())))
                        Console.WriteLine("Schritt 87 - " + zeile);
                    DataRepository.ExecuteNonQuery(SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN);
                }
                Console.WriteLine("Schritt 87 - aktive Varianten ueberzaehlig jetzt " +
                                  Zahl(SpeicherVarianteAktivEindeutig.Zaehlung()) +
                                  " (erwartet 0).");
            }
            Console.WriteLine();

            // ---- KATALOGNACHSAAT (Auftrag US-1). KEIN Schemaschritt und deshalb ohne
            //      Nummer: Tab_Gesetzesparameter steht seit dem Grundschema, und die
            //      Zeilen kommen aus GesetzKatalog.Vorbelegung - DERSELBEN Quelle, aus
            //      der sie auch beim Programmstart kommen. Der Katalog saet sich
            //      generationsweise selbst nach, aber nur, wenn ihn jemand aufruft; in
            //      der Testdatenbank tut das niemand. Sie stand deshalb auf der
            //      Generation ihres letzten Anwendungsstarts, und die Zeilen der
            //      Generation 7 (STROMST_REDUZIERT_SATZ und die drei UMLAGEN) fehlten in
            //      jeder Probe, obwohl der Quelltext sie fuehrt.
            //      Ergebnisneutral: Zu jedem dieser Schluessel rechnet der Kern bisher
            //      mit seiner WERTGLEICHEN Code-Rueckfallebene; der Referenzlauf bleibt
            //      byte-gleich. Wiederholbar: Ein zweiter Lauf saet nichts mehr nach.
            if (!trocken)
            {
                GesetzKatalog.StelleKatalogSicher();
                Console.WriteLine("Katalognachsaat - Gesetzeskatalog: " +
                                  GesetzKatalog.ZuletztNachgesaet +
                                  " Zeile(n) nachgesaet, Generation jetzt " +
                                  GesetzKatalog.AktuelleGeneration + ".");
                foreach (string w in GesetzKatalog.SaatWarnungen)
                    Console.WriteLine("Katalognachsaat - WARNUNG: " + w);
            }
            Console.WriteLine();

            // ---- Schritt 90: Aufraeumen nach der Anlagenwahrheit (Etappe BK1a,
            //      Entscheide BK1-1 / BK1-Q1 (c) / BK1-Q2 (a) und K-WZ-1 (a)).
            //      ZWEI TEILE. Beide Quellen sind dieselben, aus denen sich
            //      SchemaMigration.Schritt_90_KwkgProjektaltspalten bedient:
            //      KostenErfassungsgruppenAltzeilen (DML) und KwkgProjektaltspalten
            //      (DDL). Reihenfolge wie dort: erst die Datenzeilen, dann die
            //      Spalten. Der Schritt steht NACH Schritt 89 - der liest die sechs
            //      Spalten als Quelle seiner Uebertragung.
            //      Ergebnisneutral: Keine der sechs Spalten wird noch gelesen, und
            //      jede entfernte Kostenzeile traegt 0,00 in jedem Wertfeld; der
            //      Referenzlauf bleibt byte-gleich. Wiederholbar: Ein zweiter Lauf
            //      findet weder eine Zeile noch eine Spalte.
            if (!trocken)
            {
                int nullzeilen = KostenErfassungsgruppenAltzeilen.Offen();
                Console.WriteLine("Schritt 90 - Nullzeilen der drei Erfassungsgruppen: " +
                                  nullzeilen + ".");
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in KostenErfassungsgruppenAltzeilen.Anweisungen)
                {
                    DataRepository.ExecuteNonQuery(a.Value);
                    Console.WriteLine("Schritt 90 - " + a.Key + ".");
                }
                Console.WriteLine("Schritt 90 - Nullzeilen offen jetzt " +
                                  KostenErfassungsgruppenAltzeilen.Offen() + " (erwartet 0).");

                int kwkgSpalten = KwkgProjektaltspalten.Offen();
                Console.WriteLine("Schritt 90 - KWKG-Projektspalten: " + kwkgSpalten + ".");
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in KwkgProjektaltspalten.Anweisungen)
                {
                    DataRepository.ExecuteNonQuery(a.Value);
                    Console.WriteLine("Schritt 90 - " + a.Key + ".");
                }
                Console.WriteLine("Schritt 90 - Spalten offen jetzt " +
                                  KwkgProjektaltspalten.Offen() + " (erwartet 0).");
            }
            Console.WriteLine();

            // ---- Schritt 91: die siebte KWKG-Projektspalte (Etappe BK1b,
            //      Anwenderentscheid BK1-4 (a) vom 18.09.2026). Dieselbe Quelle,
            //      aus der sich SchemaMigration.Schritt_91_KwkgKostenanteil bedient:
            //      KwkgProjektaltspalten, zweite Liste (Spalten91/Anweisungen91).
            //      Der Schritt steht NACH 89 (der liest die Spalte als Quelle der
            //      Uebertragung in die Anlagen) und nach 90 (der laesst sie stehen).
            //      Ergebnisneutral: § 8 Abs. 2/3 KWKG leitet das Kontingent aus dem
            //      Kostenanteil DER ANLAGE ab; der Projektwert hat seit Etappe BK1a
            //      keinen Rechenleser mehr, der Referenzlauf bleibt byte-gleich.
            //      KEIN DML: Schritt 89 hat den Wert laengst uebertragen.
            //      Wiederholbar: Ein zweiter Lauf findet die Spalte nicht mehr.
            if (!trocken)
            {
                int kostenanteil = KwkgProjektaltspalten.Offen91();
                Console.WriteLine("Schritt 91 - KWKG-Projektspalte Kostenanteil: " +
                                  kostenanteil + ".");
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in KwkgProjektaltspalten.Anweisungen91)
                {
                    DataRepository.ExecuteNonQuery(a.Value);
                    Console.WriteLine("Schritt 91 - " + a.Key + ".");
                }
                Console.WriteLine("Schritt 91 - Spalten offen jetzt " +
                                  KwkgProjektaltspalten.Offen91() + " (erwartet 0).");
            }
            Console.WriteLine();

            // ---- Schritt 92: das waehlbare Vergleichsprojekt (Konzept § 2.9). EINE
            //      nullbare Verweisspalte an Tab_ProjektWirtschaftlichkeit, kein DML.
            //      Die Quelle ist dieselbe, aus der sich
            //      SchemaMigration.Schritt_92_Referenzprojekt bedient. NULL heisst
            //      Stamm - genau die Referenz, gegen die jede Bestandsrechnung schon
            //      gerechnet hat; die dreizehn Referenzprojekte rechnen unveraendert.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt92_Referenzprojekt)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 92, trocken);

            // ---- Schritt 93: die Verguetung je Variante (Konzept § 2.16). EINE
            //      nullbare 0/1-Spalte an Tab_ProjektPhotovoltaik und die zwei DML der
            //      Bestandsableitung. Beide Quellen sind dieselben, aus denen sich
            //      SchemaMigration.Schritt_93_PvUebernahme bedient. Die Testdatenbank
            //      fuehrt keine einzige Zeile in Tab_ProjektPhotovoltaik - beide DML
            //      fassen dort nichts an, die dreizehn Referenzprojekte rechnen
            //      unveraendert.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt93_VerguetungJeVariante)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 93, trocken);

            if (!trocken && PvVerguetungJeVariante.SpalteVorhanden())
            {
                int ohneWahl = PvVerguetungJeVariante.OhneWahl();
                int ohneZeile = PvVerguetungJeVariante.OhneZeileBeiAktivemStamm();
                Console.WriteLine("Schritt 93 - Zeilen ohne Wahl: " + ohneWahl +
                                  ", Varianten ohne Zeile bei aktivem Stamm: " + ohneZeile + ".");
                foreach (System.Collections.Generic.KeyValuePair<string, string> a
                         in PvVerguetungJeVariante.Anweisungen)
                {
                    DataRepository.ExecuteNonQuery(a.Value);
                    Console.WriteLine("Schritt 93 - " + a.Key + ".");
                }
            }

            // ---- Schritt 94: die Hilfsstrom-Bemessung der Saat (Anwenderentscheid
            //      19.09.2026). REIN DML, kein DDL: Die drei Hilfsstrom-Positionen der
            //      Katalogvorlage "Standard" (BHKW, Heizkessel, Waermepumpe) rechnen ab
            //      hier als Anteil des Endenergiebedarfs, bewertet mit dem
            //      Strombezugspreis. DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_94_HilfsstromBemessung bedient.
            //      Tab_ProjektWerte bleibt unberuehrt - die dreizehn Referenzprojekte
            //      rechnen unveraendert.
            if (!trocken && HilfsstromBemessungVorlage.Vorhanden())
            {
                Console.WriteLine("Schritt 94 - Vorlagenpositionen mit " +
                                  HilfsstromBemessungVorlage.VON + ": " +
                                  HilfsstromBemessungVorlage.Offen() + ".");
                foreach (System.Collections.Generic.KeyValuePair<string, HilfsstromBemessungVorlage.Anweisung> a
                         in HilfsstromBemessungVorlage.Anweisungen)
                {
                    DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);
                    Console.WriteLine("Schritt 94 - " + a.Key + ".");
                }
                Console.WriteLine("Schritt 94 - mit " + HilfsstromBemessungVorlage.NACH +
                                  " jetzt " + HilfsstromBemessungVorlage.Umgestellt() + ".");
            }

            // ---- Schritt 95: die Klimaspalten (Anwenderentscheid 19.09.2026). REIN
            //      DDL: drei Groessen der Stundenreihe an Tab_Solar und Tab_Solar_STAMM
            //      (Gegenstrahlung, Luftfeuchte, Bedeckungsgrad) und zwei Angaben des
            //      Kopfsatzes an Tab_Klimaregion und Tab_Klimaregion_STAMM (Quelle,
            //      Importdatum). DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_95_Klimaspalten bedient. Kein DML: Alle
            //      Spalten bleiben NULL, die dreizehn Referenzprojekte rechnen
            //      unveraendert.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt95_Klimaspalten)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 95, trocken);

            // ---- Schritt 97: Szenario und Bezugsjahr der Klimaregion
            //      (Anwenderentscheid 19.09.2026, Auftrag KL-6). REIN DDL: zwei
            //      Angaben des Kopfsatzes an Tab_Klimaregion und
            //      Tab_Klimaregion_STAMM. DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_97_KlimaSzenario bedient. Kein DML: Beide
            //      Spalten bleiben NULL, die dreizehn Referenzprojekte rechnen
            //      unveraendert.
            //
            //      ER STEHT VOR 96 - anders als in der Migration, und das mit Absicht:
            //      Schritt 96 baut achtundzwanzig Tabellen neu und kopiert dabei jede
            //      Spalte mit. Wer hier zuerst 96 laufen liesse und danach die zwei
            //      Spalten anhaengte, bekaeme dieselbe Datenbank - nur eben in zwei
            //      Anlaeufen. Ein Werkzeuglauf soll in EINEM Durchgang fertig sein.
            foreach (SchemaSpalte s in SchemaKatalog.Schritt97_KlimaSzenario)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 97, trocken);

            // ---- Schritt 96: der Fremdschluessel der Projekttabellen auf Tab_Projekt
            //      (Anwenderentscheid 19.09.2026). DER DRITTE TABELLENNEUBAU DIESES
            //      WERKZEUGS, und er steht ZULETZT UNTER ALLEN SCHRITTEN: Er kopiert
            //      achtundzwanzig Tabellen vollstaendig, also muss jede Spalte, die ein
            //      frueherer Schritt anlegt, vorher dastehen (Schritt 95 haengt drei
            //      Spalten an Tab_Solar). Die Anweisungen kommen aus
            //      ProjektFremdschluessel - DERSELBEN Quelle, aus der sich
            //      SchemaMigration.Schritt_96_ProjektFremdschluessel bedient; jede
            //      Tabelle laeuft dort wie hier in ihrer eigenen Transaktion mit
            //      abgeschalteten Fremdschluesseln. Ergebnisneutral: Werte, Ids und
            //      AUTOINCREMENT-Staende bleiben, entfernt wird nur, was zu keinem
            //      Projekt gehoert.
            Console.WriteLine();
            Console.WriteLine("Schritt 96 - Projekttabellen ohne Fremdschluessel auf " +
                              ProjektFremdschluessel.ZIEL + ": " + ProjektFremdschluessel.Offen() +
                              " von " + ProjektFremdschluessel.Katalog.Length + ".");
            if (!trocken)
            {
                var bericht96 = new List<string>();
                int umgebaut96 = ProjektFremdschluessel.Alle(bericht96);
                foreach (string zeile in bericht96)
                    Console.WriteLine("Schritt 96 - " + zeile);
                Console.WriteLine("Schritt 96 - " + umgebaut96 + " Tabelle(n) neu aufgebaut, offen " +
                                  ProjektFremdschluessel.Offen() + " (erwartet 0).");
            }
            else
            {
                // --trocken zaehlt nur - auch die Waisen, denn sie sind der Grund, aus
                // dem der Schritt ueberhaupt Zeilen anfasst.
                foreach (ProjektFremdschluessel.Eintrag e in ProjektFremdschluessel.Katalog)
                {
                    long ohneProjekt = ProjektFremdschluessel.Waisen(e.Tabelle);
                    if (ohneProjekt > 0)
                        Console.WriteLine("Schritt 96 - " + e.Tabelle + ": " + ohneProjekt +
                                          " Zeile(n) ohne Projekt.");
                }
            }

            // ---- Schritt 98: der BHKW-Wirkungsgrad ist ein FAKTOR (Anwenderentscheid
            //      19.09.2026, Auftrag BW-1). REIN DML, kein DDL: Wo
            //      Tab_BHKW_STAMM.Wirkungsgrad bzw. Tab_BHKW.Wirkungsgrad einen
            //      Prozentwert traegt (den des ELEKTRISCHEN Wirkungsgrads), rechnet der
            //      Schritt ihn in den Gesamtwirkungsgrad als Faktor um - je Zeile aus
            //      ihren eigenen Werten und nur, solange das Ergebnis im Band
            //      [0,5; 1,05] bleibt. DIESELBE Quelle, aus der sich
            //      SchemaMigration.Schritt_98_BhkwWirkungsgrad bedient.
            //
            //      ER STEHT NACH 96 wie in der Migration: 96 baut Tab_BHKW neu, und ein
            //      Wert, den 98 vorher setzte, wuerde zwar mitkopiert - die Reihenfolge
            //      der Migration nachzubilden ist trotzdem das Ehrlichere.
            //
            //      ER AENDERT ERGEBNISSE, und das ist sein Zweck: Der Brennstoff der
            //      betroffenen Module faellt ab hier richtig aus. Die Referenzbasis ist
            //      im selben Schritt neu eingefroren.
            Console.WriteLine();
            BhkwWirkungsgradFaktor.Aufnahme aufnahme98 = BhkwWirkungsgradFaktor.Bestandsaufnahme();
            foreach (string tabelle98 in BhkwWirkungsgradFaktor.Tabellen)
            {
                if (!aufnahme98.Gesamt.ContainsKey(tabelle98)) continue;
                Console.WriteLine("Schritt 98 - " + tabelle98 + ": " +
                                  aufnahme98.Umzurechnen[tabelle98] +
                                  " Zeile(n) umzurechnen, " +
                                  aufnahme98.AusgewiesenIn(tabelle98) +
                                  " Zeile(n) ueber 1 bleiben stehen; ueber der " +
                                  "Pflegegrenze " +
                                  BhkwWirkungsgradFaktor.UeberDerPflegegrenze(tabelle98) + ".");
            }

            if (!trocken)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                         in BhkwWirkungsgradFaktor.Anweisungen)
                {
                    DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);
                    Console.WriteLine("Schritt 98 - " + a.Key + ".");
                }

                Console.WriteLine("Schritt 98 - " + BhkwWirkungsgradFaktor.Bericht(aufnahme98));

                foreach (string tabelle98 in BhkwWirkungsgradFaktor.Tabellen)
                    Console.WriteLine("Schritt 98 - " + tabelle98 + ": offen " +
                                      BhkwWirkungsgradFaktor.Offen(tabelle98) +
                                      " (erwartet 0), ueber der Pflegegrenze " +
                                      BhkwWirkungsgradFaktor.UeberDerPflegegrenze(tabelle98) +
                                      " (erwartet 0).");
            }
            else
            {
                foreach (BhkwWirkungsgradFaktor.Ausweis a in aufnahme98.Ausgewiesen)
                    Console.WriteLine("Schritt 98 - " + a.Zeile() + ".");
            }

            // ---- Schritt 99: elektrischer und thermischer Wirkungsgrad des BHKW
            //      (Anwenderentscheid 20.09.2026, Auftrag BW-2). DDL UND DML: je zwei
            //      nullbare Spalten an Tab_BHKW_STAMM und Tab_BHKW, danach die
            //      Aufteilung des Gesamtwirkungsgrads im Verhaeltnis der Leistungen.
            //      DIESELBEN Quellen, aus denen sich
            //      SchemaMigration.Schritt_99_BhkwWirkungsgradAnteile bedient.
            //
            //      ER STEHT NACH 98: Ein Prozentwert liesse sich nicht sinnvoll teilen.
            //
            //      ERGEBNISNEUTRAL: Die Spalte Wirkungsgrad bleibt unveraendert, und nur
            //      sie liest der Rechenweg - die dreizehn Referenzprojekte rechnen
            //      byte-gleich weiter.
            Console.WriteLine();
            foreach (SchemaSpalte s in SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 99, trocken);

            BhkwWirkungsgradAnteile.Aufnahme aufnahme99 = BhkwWirkungsgradAnteile.Bestandsaufnahme();
            foreach (string tabelle99 in BhkwWirkungsgradAnteile.Tabellen)
            {
                if (!aufnahme99.Gesamt.ContainsKey(tabelle99)) continue;
                Console.WriteLine("Schritt 99 - " + tabelle99 + ": " +
                                  aufnahme99.Aufzuteilen[tabelle99] +
                                  " Zeile(n) aufzuteilen, " +
                                  aufnahme99.AusgewiesenIn(tabelle99) +
                                  " Zeile(n) ohne Aufteilung.");
            }

            if (!trocken)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                         in BhkwWirkungsgradAnteile.Anweisungen)
                {
                    DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);
                    Console.WriteLine("Schritt 99 - " + a.Key + ".");
                }

                Console.WriteLine("Schritt 99 - " + BhkwWirkungsgradAnteile.Bericht(aufnahme99));

                foreach (string tabelle99 in BhkwWirkungsgradAnteile.Tabellen)
                    Console.WriteLine("Schritt 99 - " + tabelle99 + ": offen " +
                                      BhkwWirkungsgradAnteile.Offen(tabelle99) +
                                      " (erwartet 0), aufgeteilt " +
                                      BhkwWirkungsgradAnteile.Aufgeteilt(tabelle99) +
                                      ", Summe weicht ab " +
                                      BhkwWirkungsgradAnteile.SummeWeichtAb(tabelle99) +
                                      " (erwartet 0).");
            }
            else
            {
                foreach (BhkwWirkungsgradAnteile.Ausweis a in aufnahme99.Ausgewiesen)
                    Console.WriteLine("Schritt 99 - " + a.Zeile() + ".");
            }

            // ---- Schritt 100: die Vorgabe 0 der Fremdschluesselspalten faellt
            //      (Anwenderentscheid 21.09.2026, Auftrag FK-1). DER VIERTE
            //      TABELLENNEUBAU DIESES WERKZEUGS, und er steht ZULETZT UNTER ALLEN
            //      SCHRITTEN: Er baut die betroffenen Tabellen vollstaendig neu, also
            //      muss jede Spalte eines frueheren Schritts vorher dastehen - und er
            //      findet seine Spalten ueber die Fremdschluessel, die erst Schritt 96
            //      setzt. Die Anweisungen kommen aus FremdschluesselVorgabe - DERSELBEN
            //      Quelle, aus der sich
            //      SchemaMigration.Schritt_100_FremdschluesselVorgabe bedient; jede
            //      Tabelle laeuft dort wie hier in ihrer eigenen Transaktion mit
            //      abgeschalteten Fremdschluesseln.
            //
            //      REIN DDL, ergebnisneutral: Werte, Ids und AUTOINCREMENT-Staende
            //      bleiben. Zeilen mit dem Wert 0 gibt es nicht - faende der Schritt
            //      welche, braeche er benannt ab.
            Console.WriteLine();
            Console.WriteLine("Schritt 100 - Fremdschluesselspalten mit der Vorgabe " +
                              FremdschluesselVorgabe.VORGABE + ": " +
                              FremdschluesselVorgabe.OffeneSpalten() + " in " +
                              FremdschluesselVorgabe.Offen() + " Tabelle(n).");
            if (!trocken)
            {
                var bericht100 = new List<string>();
                int umgebaut100 = FremdschluesselVorgabe.Alle(bericht100);
                foreach (string zeile in bericht100)
                    Console.WriteLine("Schritt 100 - " + zeile);
                Console.WriteLine("Schritt 100 - " + umgebaut100 + " Tabelle(n) neu aufgebaut, offen " +
                                  FremdschluesselVorgabe.OffeneSpalten() + " Spalte(n) (erwartet 0).");
            }
            else
            {
                // --trocken zaehlt nur - und nennt jede Spalte, damit der Bericht
                // nachlesbar macht, was der Lauf anfassen wuerde.
                foreach (FremdschluesselVorgabe.Spalte s in FremdschluesselVorgabe.Betroffene())
                    Console.WriteLine("Schritt 100 - " + s + ": Vorgabe " +
                                      FremdschluesselVorgabe.VORGABE + ", Zeilen mit diesem Wert " +
                                      FremdschluesselVorgabe.ZeilenMitVorgabe(s.Tabelle, s.Name) + ".");
            }

            // ---- Schritt 101: der Gebaeudespalten-Schritt M3 (Auftrag 23.09.2026, Stufe G1
            //      der Gebaeudesimulation). REIN DDL: Sicht verwerfen, Wohnflaeche ->
            //      Nutzflaeche in Tab_Gebaeude(_STAMM), fuenfzehn neue Spalten je Tabelle,
            //      Sicht Abfrage_Projektgebaeude neu. DIESELBE Quelle (GebaeudeSchema), aus
            //      der sich SchemaMigration.Schritt_101_Gebaeudespalten bedient.
            //
            //      ER STEHT NACH 100: 100 baut Tab_Gebaeude neu.
            //
            //      ERGEBNISNEUTRAL: Die neuen Spalten bleiben NULL (die zwei Schalter 0),
            //      kein Rechenweg liest sie; die Umbenennung traegt die Werte 1:1.
            Console.WriteLine();
            Console.WriteLine("Schritt 101 - Gebaeudespalten: " +
                              (GebaeudeSchema.Vollstaendig() ? "stehen bereits" : "offen") + ".");
            if (!trocken)
            {
                var bericht101 = new List<string>();
                int angelegt101 = GebaeudeSchema.Alle(bericht101);
                angelegt += angelegt101;
                foreach (string zeile in bericht101)
                    Console.WriteLine("Schritt 101 - " + zeile + ".");
                Console.WriteLine("Schritt 101 - vollstaendig: " + GebaeudeSchema.Vollstaendig() +
                                  " (erwartet True).");
            }

            // ---- Schritt 102: die leere Anlagenart wird NULL (Konzept Wirtschaftlichkeit
            //      § 6.3 Nr. 30, Anwenderentscheid 22.09.2026). REIN DML, eine Anweisung aus
            //      KwkgAnlagenartLeer - DERSELBEN Quelle, aus der sich
            //      SchemaMigration.Schritt_102_KwkgAnlagenartLeer bedient.
            //
            //      ERGEBNISNEUTRAL: Kein Rechenweg unterscheidet die leere Zeichenkette von
            //      NULL - die dreizehn Referenzprojekte rechnen byte-gleich weiter.
            Console.WriteLine();
            List<string> betroffene102 = KwkgAnlagenartLeer.Betroffene();
            Console.WriteLine("Schritt 102 - " + KwkgAnlagenartLeer.TABELLE + "." +
                              KwkgAnlagenartLeer.SPALTE + ": " + betroffene102.Count +
                              " Zeile(n) mit leerer Zeichenkette.");
            foreach (string zeile in betroffene102)
                Console.WriteLine("Schritt 102 - " + zeile);
            if (!trocken)
            {
                int gesetzt102 = KwkgAnlagenartLeer.Ausfuehren();
                Console.WriteLine("Schritt 102 - " + gesetzt102 + " Zeile(n) auf NULL gesetzt, offen " +
                                  KwkgAnlagenartLeer.Offen() + " (erwartet 0).");
            }

            // ---- Schritt 103: die zehn Tabellen des Zapfprofilgenerators (Umsetzungskonzept
            //      Zapfprofilgenerator 3.2, T1). REIN DDL aus TwwSchema - DERSELBEN Quelle,
            //      aus der sich SchemaMigration.Schritt_103_ZapfprofilKatalog bedient; erst
            //      die Tabellen, dann die Indizes auf den Kindspalten. CREATE ... IF NOT
            //      EXISTS ist selbst wiederholbar.
            //
            //      ERGEBNISNEUTRAL: Die Tabellen entstehen leer, kein Projekt steht auf dem
            //      Generator, kein Rechenweg liest sie. Den FIKTIVEN Testkatalog spielt
            //      danach Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py ein - er ist
            //      Testdatum, kein Schemaschritt.
            Console.WriteLine();
            foreach (KeyValuePair<string, string> a in TwwSchema.Anweisungen)
                tabellen += TabelleSicherstellen(a.Key, a.Value, 103, trocken);
            foreach (KeyValuePair<string, string> i in TwwSchema.Indizes)
            {
                Console.WriteLine("Schritt 103 - Index " + i.Key + (trocken ? ": (trocken) uebersprungen." : ": sichergestellt."));
                if (!trocken) DataRepository.ExecuteNonQuery(i.Value);
            }

            // ---- Schritt 104: der Zeitzonentarif wird abgeloest (Entscheid Q11,
            //      22.09.2026: "kein HT/NT"). DDL UND DML aus DENSELBEN Quellen, aus denen
            //      sich SchemaMigration.Schritt_104_ZeitzonentarifAbloesung bedient: erst
            //      die drei Spalten der Leistungspreis-Staffel an energy_project_settings
            //      (SchemaKatalog.Schritt104_LeistungspreisStaffel), dann der Datenteil
            //      (ZeitzonentarifAbloesung) - Staffel uebernehmen, Zonensaetze loeschen, mit
            //      einem Zonentarif gerechnete Ergebnisse verwerfen (Entscheid E7b-Q4),
            //      Zonenzeilen der Strommatrix je Projekt zu einer Jahreszeile.
            //
            //      REFERENZLAUF BYTE-GLEICH: Die Wirtschaftlichkeit steht nicht im Export,
            //      und kein Referenzprojekt traegt einen Tarifsatz.
            //
            //      ER STEHT NACH 103 (Zapfprofilgenerator) ohne Reihenfolgebedingung.
            Console.WriteLine();
            foreach (SchemaSpalte s in SchemaKatalog.Schritt104_LeistungspreisStaffel)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 104, trocken);
            Console.WriteLine("Schritt 104 - Tarifsaetze im Zonenmodell: " +
                              ZeitzonentarifAbloesung.OffeneZonensaetze() + ", Zonenzeilen der Strommatrix: " +
                              ZeitzonentarifAbloesung.OffeneZonenzeilen() + ".");
            if (!trocken)
            {
                ZeitzonentarifAbloesung.Bericht bericht104 = ZeitzonentarifAbloesung.Ausfuehren();
                Console.WriteLine("Schritt 104 - " + bericht104.Text() + ".");
                Console.WriteLine("Schritt 104 - offen: " + ZeitzonentarifAbloesung.OffeneZonensaetze() +
                                  " Zonensatz/-saetze, " + ZeitzonentarifAbloesung.OffeneZonenzeilen() +
                                  " Zonenzeile(n) (erwartet 0 / 0).");
            }
            else
            {
                foreach (ZeitzonentarifAbloesung.Uebernahme u in ZeitzonentarifAbloesung.Uebernahmen())
                    Console.WriteLine("Schritt 104 - " + u + ".");
            }

            // ---- Schritt 105: der zweite Fall des Paragraf 2 Nr. 16 KWKG (Befund K-1,
            //      Entscheide EZ-5 und E7-Q2, 23.09.2026). REIN DDL aus DERSELBEN Quelle,
            //      aus der sich SchemaMigration.Schritt_105_KwkgAbwaermeabfuhr bedient
            //      (SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr): das Kennzeichen
            //      KWKG_Abwaermeabfuhr (0/1, Vorgabe 0) und die nullbare Stromkennzahl an
            //      Tab_Energieanlagen.
            //
            //      REFERENZLAUF BYTE-GLEICH: Kein DML, das Kennzeichen steht ueberall auf 0
            //      (Fall 1), und die Wirtschaftlichkeit steht nicht im Export.
            //
            //      ER STEHT NACH 104 ohne Reihenfolgebedingung.
            Console.WriteLine();
            foreach (SchemaSpalte s in SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr)
                angelegt += SpalteSicherstellen(s.Tabelle, s.Name,
                                                StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition), 105, trocken);

            // ---- Schritt 106: fremde Ergebnisverweise der gespeicherten Wirtschaftlichkeit
            //      werden NULL (Anwenderentscheid 23.09.2026, Erbe des Duplizierens). REIN
            //      DML, eine Anweisung aus WirtschaftlichkeitFremdverweis - DERSELBEN Quelle,
            //      aus der sich SchemaMigration.Schritt_106_WirtschaftlichkeitFremdverweis
            //      bedient.
            //
            //      REFERENZLAUF BYTE-GLEICH: Kein Rechenweg liest den Verweis, und die
            //      Wirtschaftlichkeit steht nicht im Export.
            //
            //      ER STEHT NACH 105 ohne Reihenfolgebedingung.
            Console.WriteLine();
            List<string> betroffene106 = WirtschaftlichkeitFremdverweis.Betroffene();
            Console.WriteLine("Schritt 106 - " + WirtschaftlichkeitFremdverweis.TABELLE + "." +
                              WirtschaftlichkeitFremdverweis.SPALTE + ": " + betroffene106.Count +
                              " Zeile(n) mit fremdem Ergebnisverweis.");
            foreach (string zeile in betroffene106)
                Console.WriteLine("Schritt 106 - " + zeile);
            if (!trocken)
            {
                int gesetzt106 = WirtschaftlichkeitFremdverweis.Ausfuehren();
                Console.WriteLine("Schritt 106 - " + gesetzt106 + " Verweis(e) auf NULL gesetzt, offen " +
                                  WirtschaftlichkeitFremdverweis.Offen() + " (erwartet 0).");
            }

            // ---- Schritt 107: die Ergebnistabelle je Gebaeude (Entscheid E30, Konzept
            //      Gebaeudesimulation N1.35). REIN DDL aus ErgebnisGebaeudeSchema - DERSELBEN
            //      Quelle, aus der sich SchemaMigration.Schritt_107_ErgebnisGebaeude bedient;
            //      erst die Tabelle, dann die zwei Indizes. ERGEBNISNEUTRAL: Die Tabelle
            //      entsteht leer, kein Rechenweg liest sie, der Referenzlauf exportiert sie nicht.
            //
            //      ER STEHT NACH 106 ohne Reihenfolgebedingung.
            Console.WriteLine();
            foreach (KeyValuePair<string, string> a in ErgebnisGebaeudeSchema.Anweisungen)
            {
                if (a.Key == ErgebnisGebaeudeSchema.TAB)
                {
                    tabellen += TabelleSicherstellen(a.Key, a.Value, 107, trocken);
                    continue;
                }
                Console.WriteLine("Schritt 107 - Index " + a.Key + (trocken ? ": (trocken) uebersprungen." : ": sichergestellt."));
                if (!trocken) DataRepository.ExecuteNonQuery(a.Value);
            }

            Console.WriteLine();
            Console.WriteLine(angelegt + " Spalte(n) angelegt, " + tabellen + " Tabelle(n) angelegt.");

            if (trocken)
            {
                Console.WriteLine("--trocken: Marker und VACUUM uebersprungen.");
                return 0;
            }

            // ---- Marker und Verdichtung ----
            DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET SchemaVersion = " +
                                           SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture));
            DataRepository.ExecuteNonQuery("VACUUM");

            int nachher = SchemaVersionLesen();
            Console.WriteLine();
            Console.WriteLine("Schemastand nachher: " + nachher + "   (Zielstand " + SchemaStand.Zielversion + ")");
            Console.WriteLine("Groesse nachher: " + Mb(pfad));
            return nachher >= SchemaStand.Zielversion ? 0 : 1;
        }

        /// <summary>
        /// Schritt 69 — die verdorbenen PV-Modulkoeffizienten (Befund W6-B-5,
        /// Anwenderentscheide Q1 bis Q3 vom 07.09.2026).
        ///
        /// <para>Wortgleich zu <c>SchemaMigration.Schritt_69_PvKoeffizienten</c>: erst
        /// <c>Tab_PV_STAMM</c>, dann <c>Tab_PV</c>; je Tabelle reparieren, uebernehmen,
        /// Protokoll lesen, leeren. Alle Anweisungen kommen aus
        /// <c>PvKoeffizientenReparatur</c>.</para>
        /// </summary>
        private static void SchrittPvKoeffizienten(bool trocken)
        {
            PvKoeffizientenquelle quelle = PvKoeffizientenReparatur.Quelle();
            Console.WriteLine("Schritt 69 - Wertequelle: " + quelle.Eingebettet +
                              " eingebettete Auslieferungsmodule" +
                              (quelle.DateiGelesen
                                   ? ", dazu " + quelle.AusDerDatei + " aus " + quelle.Dateipfad
                                   : " (CEC-Liste nicht gefunden: " +
                                     (string.IsNullOrEmpty(quelle.Dateipfad) ? "kein Pfad" : quelle.Dateipfad) +
                                     ")") + ".");

            foreach (string tabelle in PvKoeffizientenReparatur.TABELLEN)
            {
                long vorher = Zahl(PvKoeffizientenReparatur.ZaehlungVerdorben(tabelle));
                Console.WriteLine("Schritt 69 - " + tabelle + ": verdorbene Saetze vorher " +
                                  vorher + " von " +
                                  Zahl(PvKoeffizientenReparatur.Gesamtzahl(tabelle)) + ".");

                if (trocken) continue;

                foreach (string bezeichner in PvKoeffizientenReparatur.Zerlege(
                             Text(PvKoeffizientenReparatur.BezeichnerAbfrage(tabelle))))
                {
                    if (!quelle.Finde(bezeichner, null, out PvModulKoeffizienten satz)) continue;
                    string sql = PvKoeffizientenReparatur.Reparatur(tabelle, satz);
                    if (sql == null) continue;   // die Liste fuehrt hier nichts Brauchbares
                    DataRepository.ExecuteNonQuery(sql);
                }

                if (tabelle == PvKoeffizientenReparatur.TAB_PROJEKT)
                    foreach (string spalte in PvKoeffizientenReparatur.SPALTEN)
                        DataRepository.ExecuteNonQuery(PvKoeffizientenReparatur.UebernahmeAusStamm(spalte));

                // Das Protokoll steht ZWISCHEN Reparatur und Leerung - vorher stuenden
                // darin auch die Saetze, die die Liste gerade heilt, nachher ist seine
                // Bedingung falsch.
                foreach (string zeile in PvKoeffizientenReparatur.Zerlege(
                             Text(PvKoeffizientenReparatur.Protokollabfrage(tabelle))))
                    Console.WriteLine("   " + zeile);

                foreach (string spalte in PvKoeffizientenReparatur.SPALTEN)
                    DataRepository.ExecuteNonQuery(PvKoeffizientenReparatur.Leerung(tabelle, spalte));

                Console.WriteLine("Schritt 69 - " + tabelle + ": verdorbene Saetze nachher " +
                                  Zahl(PvKoeffizientenReparatur.ZaehlungVerdorben(tabelle)) + ".");
            }
        }

        /// <summary>
        /// Legt eine Spalte an, wenn sie fehlt — dieselbe Vorpruefung wie
        /// <c>SchemaMigration.SqliteSpalteAnlegen</c>: vorhandene Spalte = nichts zu tun.
        /// Rueckgabe 1, wenn angelegt wurde, sonst 0.
        ///
        /// <para><b>Warum <c>pragma_table_info</c> und nicht <c>PRAGMA table_info</c>.</b>
        /// Der PRAGMA liefert die Spalte <c>dflt_value</c> ohne festen Typ; das Fuellen
        /// einer <see cref="DataTable"/> daraus scheitert an der ersten Zeile mit einer
        /// Vorgabe ("Couldn't store &lt;0&gt; in dflt_value Column"). Die Tabellenfunktion
        /// beantwortet dieselbe Frage als Zahl und ist damit unabhaengig vom Typraten -
        /// wichtig, weil sonst der zweite Lauf die Spalte fuer fehlend hielte und das
        /// Werkzeug seine Idempotenzzusage braeche.</para>
        /// </summary>
        private static int SpalteSicherstellen(string tabelle, string spalte, string typ, int schritt, bool trocken)
        {
            object da = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM pragma_table_info('" + tabelle.Replace("'", "''") + "') " +
                "WHERE lower(name) = lower('" + spalte.Replace("'", "''") + "')");
            if (da != null && da != DBNull.Value && Convert.ToInt64(da) > 0)
            {
                Console.WriteLine("Schritt " + schritt + " - " + tabelle + "." + spalte + ": vorhanden.");
                return 0;
            }

            Console.WriteLine("Schritt " + schritt + " - " + tabelle + "." + spalte + ": anlegen als " + typ + ".");
            if (!trocken)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + spalte + "\" " + typ);
            return 1;
        }

        /// <summary>
        /// Legt eine Tabelle an, wenn sie fehlt. Die Anweisung traegt ihr
        /// <c>IF NOT EXISTS</c> selbst; die Vorabfrage dient allein der Zaehlung im
        /// Bericht. Rueckgabe 1, wenn angelegt wurde, sonst 0.
        /// </summary>
        private static int TabelleSicherstellen(string tabelle, string ddl, int schritt, bool trocken)
        {
            object da = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '" +
                tabelle.Replace("'", "''") + "'");
            bool vorhanden = da != null && da != DBNull.Value && Convert.ToInt64(da) > 0;

            if (vorhanden)
            {
                Console.WriteLine("Schritt " + schritt + " - " + tabelle + ": vorhanden.");
                return 0;
            }

            Console.WriteLine("Schritt " + schritt + " - " + tabelle + ": anlegen.");
            if (!trocken) DataRepository.ExecuteNonQuery(ddl);
            return 1;
        }

        /// <summary>Der Schemamarker aus <c>Tab_Applikation</c>; -1, wenn nicht lesbar.</summary>
        private static int SchemaVersionLesen()
        {
            try
            {
                object o = DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation");
                return o == null || o == DBNull.Value ? -1 : Convert.ToInt32(o);
            }
            catch { return -1; }
        }

        private static long Zahl(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o);
            }
            catch { return -1; }
        }

        /// <summary>Ein TEXT statt einer Zahl — die Sammelabfragen des Schrittes 69.</summary>
        private static string Text(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value
                    ? "" : (Convert.ToString(o, CultureInfo.InvariantCulture) ?? "");
            }
            catch { return ""; }
        }

        private static string Mb(string pfad)
        {
            long b = new FileInfo(pfad).Length;
            return b.ToString("N0", CultureInfo.InvariantCulture) + " Byte (" +
                   (b / 1024.0 / 1024.0).ToString("N1", CultureInfo.InvariantCulture) + " MB)";
        }
    }
}
