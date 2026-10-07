using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein älteres Projektpaket beim Import auf den Zielstand heben</b>
    /// (Konzept <c>Dokumentation/ueberholt/Konzept_Projektpaket_Migration_EPOS-Plan.md</c>).
    ///
    /// <para>Der Import ist spaltentolerant: Neue Zielspalten bekommen ihre Vorgabe,
    /// entfallene Paketspalten fallen weg. Was er nicht leisten kann, sind die Schritte,
    /// die Projektwerte UMRECHNEN — sie liefen an der Datenbank des Ziels genau einmal und
    /// nie an den Zeilen eines später eingespielten Pakets. Dieses Register nennt deshalb
    /// je Schemaschritt seine <see cref="Art"/> und für die umrechnenden Schritte die
    /// Umformung. Die Umformung fährt die Anweisung des Kern-Bausteins, den auch die
    /// Migration ruft, auf der <see cref="Paketarbeitsdatenbank"/> — kein zweites
    /// Regelwerk.</para>
    ///
    /// <para><b>Wer einen Schemaschritt anlegt, trägt ihn hier ein.</b> Die Wache
    /// <c>ProjektpaketAnhebungTests</c> verlangt für jede Nummer von <see cref="UNTERE_GRENZE"/>+1
    /// bis <see cref="SchemaStand.Zielversion"/> genau einen Eintrag.</para>
    /// </summary>
    public static class Paketanhebung
    {
        /// <summary>
        /// Der letzte Stand, den das Register NICHT mehr beschreibt. Ein Paket mit Stand
        /// 1 bis <see cref="UNTERE_GRENZE"/> wird trotzdem eingespielt — mit den Stufen des
        /// Registers und dem Hinweis, dass ältere Umformungen fehlen (Konzept, Grenzen).
        /// </summary>
        public const int UNTERE_GRENZE = 61;

        /// <summary>Was ein Schemaschritt an einem Paket bewirkt.</summary>
        public enum Art
        {
            /// <summary>Nur Schema (Spalte, Tabelle, Sicht, Index, Spaltenabbau) — die
            /// Schnittmenge des Imports genügt.</summary>
            Ddl,

            /// <summary>Nur Katalog, Saat oder globale Tabelle — das Ziel führt sie schon.</summary>
            Katalog,

            /// <summary>Deckt der Importweg selbst ab (Umschlüsselung, Namensnachtrag).</summary>
            Import,

            /// <summary>Rechnet Projektwerte um — die Stufe formt die Paketzeilen um.</summary>
            Umformung,
        }

        /// <summary>Ein Eintrag des Registers.</summary>
        public sealed class Stufe
        {
            internal Stufe(int nr, Art art, string text, Func<Paketarbeitsdatenbank, string> umformung = null)
            {
                Nr = nr;
                Wirkung = art;
                Text = text;
                Umformung = umformung;
            }

            /// <summary>Die Nummer des Schemaschritts.</summary>
            public int Nr { get; }

            /// <summary>Die Wirkung auf ein Paket.</summary>
            public Art Wirkung { get; }

            /// <summary>Was der Schritt tut, knapp.</summary>
            public string Text { get; }

            /// <summary>Die Umformung (nur bei <see cref="Art.Umformung"/>); liefert eine
            /// Berichtszeile oder <c>null</c>, wenn sie nichts fand.</summary>
            internal Func<Paketarbeitsdatenbank, string> Umformung { get; }
        }

        /// <summary>Ein gescheiterter Schritt — der Import bricht vor der Transaktion ab.</summary>
        public sealed class AnhebungFehler : Exception
        {
            internal AnhebungFehler(int schritt, string meldung, Exception innen)
                : base(meldung, innen) { Schritt = schritt; }

            /// <summary>Die Nummer des Schritts, der scheiterte.</summary>
            public int Schritt { get; }
        }

        /// <summary>Was die Anhebung eines Pakets bedeutet — für Dialog und Bericht.</summary>
        public sealed class Vorschau
        {
            /// <summary>Stand des Pakets.</summary>
            public int Von { get; internal set; }

            /// <summary>Zielstand dieses Programms.</summary>
            public int Bis { get; internal set; }

            /// <summary>Schemaschritte zwischen den Ständen.</summary>
            public int Schritte { get; internal set; }

            /// <summary>Davon mit Umformung der Projektdaten.</summary>
            public int Umformungen { get; internal set; }

            /// <summary>Das Paket liegt unter der unteren Grenze des Registers.</summary>
            public bool UnterGrenze { get; internal set; }

            /// <summary>Muss das Paket gehoben werden?</summary>
            public bool Noetig => Von > 0 && Von < Bis;

            /// <summary>Ist das Paket neuer als dieses Programm?</summary>
            public bool Neuer => Von > Bis;
        }

        // =================================================================
        //  Das Register
        // =================================================================

        private static readonly Stufe[] STUFEN =
        {
            new Stufe(62, Art.Katalog, "Klimadaten-Waisen der Stammtabellen abgeräumt"),
            new Stufe(63, Art.Ddl, "Wechselrichter-Wirkungsgrad und Systemverluste der PV-Anlage"),
            new Stufe(64, Art.Ddl, "PV-Modellwahl, Modultechnologie und Degradation"),
            new Stufe(65, Art.Ddl, "Wechselrichterkatalog und Projektkopie"),
            new Stufe(66, Art.Ddl, "Strangzuordnung und Wechselrichterweg"),
            new Stufe(67, Art.Umformung, "BHKW-Leistungsuntergrenze ohne Wert wird 30 %", Schritt67),
            new Stufe(68, Art.Katalog, "Hersteller des Stromspeicherkatalogs"),
            new Stufe(69, Art.Umformung, "verdorbene PV-Modulkoeffizienten repariert oder geleert", Schritt69),
            new Stufe(70, Art.Ddl, "Kurzschlussstrom des Wechselrichters und Auslegungstemperaturen"),
            new Stufe(71, Art.Ddl, "Szenario-Parametersatz der Wirtschaftlichkeit"),
            new Stufe(72, Art.Ddl, "Preisänderung der Ersatzbeschaffung, Freitext nicht monetärer Wirkungen"),
            new Stufe(73, Art.Ddl, "Speicherauslegung"),
            new Stufe(74, Art.Ddl, "Speicherauslegung als STRICT-Tabelle"),
            new Stufe(75, Art.Katalog, "Nutzungsdauertabelle samt Saat; die Verweisspalte der Projektkosten bleibt leer"),
            new Stufe(76, Art.Umformung, "ein Trägersatz je Projekt und Energieträger", Schritt76),
            new Stufe(77, Art.Katalog, "Volumenbemessung der Pufferspeicher-Vorlage"),
            new Stufe(78, Art.Katalog, "fester Betrag der PV-Vorlagenposition Batteriespeicher"),
            new Stufe(79, Art.Umformung, "Heizstab-Schalter vom Projekt an jede Wärmepumpe", Schritt79),
            new Stufe(80, Art.Import, "Katalogverweis der Wärmepumpen-Projektkopie — der Import findet ihn über den Bezeichner"),
            new Stufe(81, Art.Ddl, "Löschregel der Projektkosten"),
            new Stufe(82, Art.Ddl, "Merkspalte der gepflegten Kaskade"),
            new Stufe(83, Art.Umformung, "Strompreis-Anteile zerlegen den Arbeitspreis", Schritt83),
            new Stufe(84, Art.Umformung, "Einspeisevergütung von der Trägerkarte in die Wirtschaftlichkeitsparameter", Schritt84),
            new Stufe(85, Art.Ddl, "Altspalten der Strompreis-Welle entfernt"),
            new Stufe(86, Art.Ddl, "Lastspitzenkappung als Berechnungsart"),
            new Stufe(87, Art.Umformung, "eine aktive Speichervariante je Projekt; den entdoppelten Gesetzeskatalog führt das Ziel", Schritt87),
            new Stufe(88, Art.Ddl, "Modus der Stromsteuerbefreiung"),
            new Stufe(89, Art.Umformung, "KWK-Zuschlag an der Anlage statt am Projekt", Schritt89),
            new Stufe(90, Art.Umformung, "Nullzeilen der Erfassungsgruppen entfernt, KWKG-Projektspalten abgebaut", Schritt90),
            new Stufe(91, Art.Ddl, "KWKG-Kostenanteil am Projekt abgebaut"),
            new Stufe(92, Art.Ddl, "wählbares Vergleichsprojekt"),
            new Stufe(93, Art.Import, "PV-Vergütungswahl je Variante — ein Paket ohne die Spalte gilt als eigene Werte (Importweg)"),
            new Stufe(94, Art.Katalog, "Hilfsstrom-Bemessung der Kostenvorlage"),
            new Stufe(95, Art.Ddl, "Klimaspalten Gegenstrahlung, Luftfeuchte, Bedeckungsgrad, Quelle"),
            new Stufe(96, Art.Import, "Fremdschlüssel der Projekttabellen — der Import schlüsselt um und heilt Waisen"),
            new Stufe(97, Art.Ddl, "Klimaszenario und Bezugsjahr"),
            new Stufe(98, Art.Umformung, "BHKW-Wirkungsgrad vom Prozentwert auf den Faktor", Schritt98),
            new Stufe(99, Art.Umformung, "BHKW-Wirkungsgrad aufgeteilt in elektrisch und thermisch", Schritt99),
            new Stufe(100, Art.Import, "Fremdschlüssel ohne Vorgabe 0 — verwaiste Verweise macht der Import leer"),
            new Stufe(101, Art.Umformung, "Wohnfläche heißt Nutzfläche, Gebäudespalten", Schritt101),
            new Stufe(102, Art.Umformung, "leere KWKG-Anlagenart wird NULL", Schritt102),
            new Stufe(103, Art.Katalog, "Zapfprofil-Katalog und -Tabellen"),
            new Stufe(104, Art.Umformung, "Zeitzonentarif abgelöst", Schritt104),
            new Stufe(105, Art.Ddl, "KWK-Abwärmeabfuhr und Stromkennzahl"),
            new Stufe(106, Art.Umformung, "Verweise auf den Lauf eines anderen Projekts werden NULL", Schritt106),
            new Stufe(107, Art.Ddl, "Ergebnis je Gebäude"),
            new Stufe(108, Art.Ddl, "Kühleingaben des Gebäudes"),
            new Stufe(109, Art.Ddl, "Projekteinstellung Kühlbetrieb"),
            new Stufe(110, Art.Ddl, "Ergebnisspalten des Kühlkanals"),
            new Stufe(111, Art.Ddl, "Kennzeichen Ersatz und Restwert je Kostenposition"),
            new Stufe(112, Art.Umformung, "Preisbasis der Trägerkarte", Schritt112),
            new Stufe(113, Art.Umformung, "Preiszeilen der Gase auf Nm³", Schritt113),
            new Stufe(114, Art.Ddl, "Kühlbetrieb des Kälteerzeugers"),
            new Stufe(115, Art.Katalog, "Zapfkategorien"),
            new Stufe(116, Art.Ddl, "Betrachtungszeitraum und Mengenfaktor je Szenario"),
            new Stufe(117, Art.Ddl, "Trägerpreise je Szenario"),
            new Stufe(118, Art.Ddl, "Erlössätze je Szenario"),
            new Stufe(119, Art.Ddl, "Kältestrom"),
            new Stufe(120, Art.Katalog, "Instandsetzungssätze der Nutzungsdauertabelle"),
            new Stufe(121, Art.Import, "Katalogverweis des Projektgebäudes — der Import findet ihn über den Namen"),
            new Stufe(122, Art.Ddl, "Wärmeübergabe und Kopplungsstufe"),
            new Stufe(123, Art.Ddl, "Ergebnisspalten der Anlagenkopplung"),
            new Stufe(124, Art.Import, "Zapfprofil-Laufangaben — der Import nennt Werte ohne Zielspalte"),
            new Stufe(125, Art.Ddl, "Risikomodul"),
            new Stufe(126, Art.Katalog, "Reparatur des Gebäudekatalogs"),
            new Stufe(127, Art.Umformung, "Freitext der nicht monetären Wirkungen wird eine Wirkung", Schritt127),
            new Stufe(128, Art.Ddl, "Heizkreis je Gebäude im Ergebnis"),
            new Stufe(129, Art.Ddl, "Wiederholperiode der Kostenposition"),
            new Stufe(130, Art.Katalog, "Anschlusslängen des Gebäudekatalogs"),
            new Stufe(131, Art.Ddl, "Typtage des Zapfprofilgenerators"),
            new Stufe(132, Art.Katalog, "Baustoffkatalog"),
            new Stufe(133, Art.Ddl, "Bauteilaufbauten"),
            new Stufe(134, Art.Ddl, "Zonen und Bauteile"),
            new Stufe(135, Art.Ddl, "Kühlübergabe des Gebäudes"),
            new Stufe(136, Art.Ddl, "Ergebnisspalten der Kühlübergabe"),
            new Stufe(137, Art.Ddl, "Kühlübergabe je Zone"),
            new Stufe(138, Art.Ddl, "Importquelle und -zuordnung"),
            new Stufe(139, Art.Ddl, "Baujahr des Gebäudes"),
            new Stufe(140, Art.Ddl, "Messreihen des Zapfprofilgenerators"),
            new Stufe(141, Art.Katalog, "Folgereparatur des Gebäudekatalogs"),
            new Stufe(142, Art.Katalog, "dritte Reparatur des Gebäudekatalogs"),
            new Stufe(143, Art.Katalog, "Quellen des Baustoffkatalogs"),
            new Stufe(144, Art.Ddl, "Nachtzeit des Gebäudes"),
            new Stufe(145, Art.Ddl, "Konstruktorzeilen des Zapfprofilgenerators"),
            new Stufe(146, Art.Katalog, "Baustoffsynonyme und -zuordnung"),
            new Stufe(147, Art.Ddl, "Zonenkopplung"),
            new Stufe(148, Art.Umformung, "Baualtersklassen umgeschlüsselt, Energiestandard", Schritt148),
            new Stufe(149, Art.Katalog, "Gebäudesätze der Klassen M und A"),
            new Stufe(150, Art.Ddl, "Vorlauf und Rücklauf am Kollektor entfernt"),
            new Stufe(151, Art.Ddl, "Konditionierungskalender, Perioden und Vorgabezellen"),
            new Stufe(152, Art.Ddl, "Konditionierungsvorlagen, Fremdschlüssel und Eindeutigkeit, Nachtauskühlstunden"),
            new Stufe(TwwBezugsartSchema.SCHRITT, Art.Import,
                      "Bezugsart Zimmer — der Import liest eine Paketzeile des Hotels in einem früheren Stand als die heutige"),
            new Stufe(154, Art.Ddl, "Heizgrenze der Kesselbereitschaft"),
            new Stufe(TwwFuellstandSchema.SCHRITT, Art.Ddl,
                      "Verfahrensvolumina als Bezug der Füllstandslinie — Tab_TwwProjekt mit der Prüfklausel 1 bis 8"),
            new Stufe(KesselKennlinieSchema.SCHRITT, Art.Ddl, "Kennlinienspalten des Heizkessels"),
            new Stufe(KonditionierungsvorlagenSaatSchema.SCHRITT, Art.Katalog,
                      "die 14 ausgelieferten Konditionierungsvorlagen"),
            new Stufe(KesselBrennwertNachzug.SCHRITT, Art.Umformung,
                      "Brennwertkennzeichen der Projektkessel nach Katalogsatz oder Beschreibung", SchrittKesselBrennwert),
            // Ein Paket führt keine Trigger: Die Stempelspalten kommen leer an, und der Import selbst
            // stempelt im Ziel - dessen Trigger feuern beim Einfügen der Projektzeilen.
            new Stufe(KostenStempelSchema.SCHRITT, Art.Ddl,
                      "Änderungsstempel für Kosten, Preise und Kostenkatalog (zwei Spalten und ihre Trigger)"),
            // KP-S2 und KP-S3 (Entwurf KP3 Abschnitt 4): reine Spalten. Ein Paket davor kommt mit dem
            // Schalter 0 (Spaltenvorgabe) und leeren Aufheizspalten an - „aus", wie es gerechnet hat.
            new Stufe(AufheizvorgabeSchema.SCHRITT, Art.Ddl,
                      "Aufheizoptimierung als Projekteinstellung (Schalter, Bemessung, Abzug, Reserve, Art)"),
            new Stufe(AufheizErgebnisSchema.SCHRITT, Art.Ddl,
                      "Ergebnisspalten der Aufheizoptimierung je Gebäude und Zone"),
            // Die Spalte kommt mit der Vorgabe kW an - die Einheit, in der ein Paketwert rechnet.
            new Stufe(KesselBereitschaftEinheitSchema.SCHRITT, Art.Ddl,
                      "Einheit des Bereitschaftsverlusts am Heizkessel (kW oder % der Nennleistung)"),
            // Die Spalte kommt leer an - leer rechnet die Vorgabe 0,2, wie das Paket gerechnet hat.
            new Stufe(AlbedoSchema.SCHRITT, Art.Ddl,
                      "Bodenalbedo je Photovoltaik- und Solarthermie-Anlage"),
            // Die Spalten kommen leer an - ein Paketsatz ohne Temperaturpaar rechnet wie zuvor.
            new Stufe(ProzesswaermeTemperaturSchema.SCHRITT, Art.Ddl,
                      "Temperaturpaar je Prozesswärmesatz (Vorlauf, Rücklauf) und Katalog typischer Betriebsweisen"),
            // Die Anlagenspalten kommen leer an (Vorgaben: kein Pumpenstrom, 8 % Verluste, feste
            // Arbeitstemperatur), der Kollektorsatz mit der Vorgabe apertur - so, wie er rechnet.
            new Stufe(SolarthermieFelderSchema.SCHRITT, Art.Ddl,
                      "Felder des Kollektorfelds (Pumpe, Verluste, Grädigkeit, Spreizung, Arbeitstemperatur) und Bezugsfläche des Kollektorsatzes"),
            // Alles kommt leer an - leer rechnet wie zuvor (Projektwert der Netzverluste, kein Kalender).
            new Stufe(BedarfNetzKalenderSchema.SCHRITT, Art.Ddl,
                      "Netzverluste je Kanal, Zirkulation im Bestandsweg und Betriebskalender der Bedarfsprofile"),
            // Die Spalten kommen leer an - ohne Teillastfelder rechnen Wärmepumpe und BHKW wie das Paket.
            new Stufe(ErzeugerTeillastSchema.SCHRITT, Art.Ddl,
                      "Teillastfelder der Wärmepumpe (Mindestleistung, C_d) und des BHKW (Wirkungsgrade bei 50 % Last, Anfahrverlust, Mindestlaufzeit)"),
            // Die Spalten kommen leer an - keine Einspeisegrenze, keine Selbstentladung; ein Paketsatz
            // rechnet wie zuvor.
            new Stufe(StromViertelstundenSchema.SCHRITT, Art.Ddl,
                      "Einspeisegrenze des Projekts (kW oder % der PV-Leistung) und Selbstentladung des Stromspeichers"),
            // Die Auslegungstabelle kommt leer an, die Vorgabetabelle mit ihrer Saat - die Auslegung
            // rechnet nur auf Zuruf, der Paketstand rechnet wie zuvor.
            new Stufe(PufferAuslegungSchema.SCHRITT, Art.Ddl,
                      "Pufferspeicher-Auslegung: Auslegungstabelle je Projektpuffer und Vorgabewerte"),
            // Ein Paket fuehrt keine Kostenvorlagen; die Empfehlung ist Hinweis, kein Projektwert.
            new Stufe(HilfsenergieEmpfehlungNachzug.SCHRITT, Art.Katalog,
                      "Empfehlung der Hilfsenergiekosten von BHKW und Heizkessel auf den Endenergiebedarf (Weg B)"),
            // Die Spalten kommen leer an - Bereitschaft als Tageswert, gleich große Zonen, kein
            // Frischwassermodul, keine Desinfektion; ein Paketsatz rechnet wie zuvor.
            new Stufe(PufferOptionenSchema.SCHRITT, Art.Ddl,
                      "Optionen des Pufferspeichers (Bereitschaftsweg, Aufstellraum, Zonenanteile, Frischwassermodul) und thermische Desinfektion"),
            // Katalogspalten und Saat betreffen nur Kataloge und globale Tabellen, die das Ziel schon
            // führt; die Erdreichprüfung kommt mit einem Paket davor schlicht nicht mit - der Dialog
            // zeigt sie nach dem nächsten Lauf.
            new Stufe(KatalogfassungSchema.SCHRITT, Art.Katalog,
                      "Katalogfassung (Schlüssel, Prüfsumme, Auslaufkennzeichen der ausgelieferten Sätze, Protokoll des Abgleichs) und gespeicherte Erdreichprüfung"),
            // Dieselben Katalogspalten an den übrigen Katalogen; ein Paket führt keine Kataloge.
            new Stufe(KatalogfassungStufe2Schema.SCHRITT, Art.Katalog,
                      "Katalogfassung der übrigen Kataloge (Schlüssel, Prüfsumme, Auslaufkennzeichen der ausgelieferten Sätze)"),
            new Stufe(AufheizManuellSchema.SCHRITT, Art.Ddl,
                      "Aufschlag und manuelle Aufheizzeit der Aufheizoptimierung, Art, Auslegungsheizlast und Aufheizzuschlag im Ergebnis, Zustand GEKOPPELT der Zone"),
            // Ein älteres Paket bringt keine Projektkopien der Brennstoffe und Pufferauslegungs-Vorgaben
            // mit; die Projektanlage und der Paketimport legen sie aus dem Katalog des Ziels an.
            new Stufe(ProjektkopienKatalogeSchema.SCHRITT, Art.Ddl,
                      "Projektkopien der Brennstoffe und der Vorgaben der Pufferauslegung"),
            // Ein älteres Paket bringt die Nutzung seiner Kalender nicht mit; sie bleibt leer, bis
            // „Vorlage übernehmen" sie setzt - die Vorbelegung der Pufferauslegung kennt dann keine.
            new Stufe(KonditionierungNutzungSchema.SCHRITT, Art.Ddl,
                      "Nutzung der Konditionierungsvorlage am Kalender des Projekts"),
            // Die Tabelle kommt leer an; ein Paket ohne Sperrfenster rechnet mit dem Altfenster wie zuvor.
            new Stufe(WaermepumpeSperrprofilSchema.SCHRITT, Art.Ddl,
                      "Sperrfenster der Wärmepumpe (Beginn, Dauer, Wochentage, Heizstab)"),
            // Ein älteres Paket bringt die Zuordnung der Nutzungsprofile nicht mit; die Auslegung fällt auf
            // die Vorgabe im Code zurück, bis die Datenbank des Ziels den Schritt trägt.
            new Stufe(ProzessNutzungSchema.SCHRITT, Art.Ddl,
                      "Zuordnung der Nutzungsprofile über IDs, Zapf-Nutzungsarten Büro, Schule und Gewerbe"),
            // Ein älteres Paket bringt die Ergänzungsspalten der Pufferauslegung nicht mit; sie bleiben leer.
            new Stufe(PufferAuslegungErgaenzungSchema.SCHRITT, Art.Ddl,
                      "Sitzungseingaben der Pufferauslegung, Katalogverweis am Projektpuffer, Vorgaben des Aufheizkriteriums"),
            // Ein älteres Paket bringt den wirksamen U-Wert der Bodenplatte nicht mit; leer rechnet nach DIN EN ISO 13370.
            new Stufe(ErdreichVorgabeSchema.SCHRITT, Art.Ddl,
                      "Wirksamer U-Wert der Bodenplatte als Vorgabe am Gebäude"),
            // Ein älteres Paket bringt Auslegungspunkt und Proportionalband der Zonen nicht mit; leer rechnet die Zone wie ihr Gebäude.
            new Stufe(ZonenUebergabeSchema.SCHRITT, Art.Ddl,
                      "Auslegungspunkt und Regler der Wärmeübergabe je Zone"),
            // Ein älteres Paket führt keine Kältemaschine; die Tabellen kommen leer an, der Katalog mit der Saat.
            new Stufe(KaeltemaschineSchema.SCHRITT, Art.Ddl,
                      "Katalog, Projektkopie und Kennlinien der Kältemaschine"),
            // Ein älteres Paket führt keine Anlagenzeile der Kältemaschine; die Spalten kommen leer bzw. mit 1 an.
            new Stufe(KaeltemaschineAnlageSchema.SCHRITT, Art.Ddl,
                      "Kältemaschine als Anlage: Verweis, Anzahl, Kühleingaben, Kostenkomponente, Ergebnis je Maschine"),
            // Ein älteres Paket führt kein Ergebnis der Kältemaschine mit Abrechnung; die Spalten kommen leer an.
            new Stufe(KaeltestromabrechnungSchema.SCHRITT, Art.Ddl,
                      "Kältestromabrechnung der Kältemaschine: Netzbezug, Kühlträger, Stromspitze; Stempeltrigger"),
            // Ein älteres Paket führt keine Kältespitze je Zone; die Spalten kommen leer an.
            new Stufe(ZonenKaeltespitzeSchema.SCHRITT, Art.Ddl,
                      "Kältespitze und Kühlstunden je Zone im Ergebnis"),
            // Ein älteres Paket führt kein Zeitprogramm und kein Vorlaufangebot am Erzeuger und keine Komfortspalten;
            // die Spalten kommen leer an (immer verfügbar, Vorlauf der Anlage, „nicht erhoben").
            new Stufe(AnlagenfahrplanSchema.SCHRITT, Art.Ddl,
                      "Anlagenfahrplan: Zeitprogramm und Vorlauf_Max am Erzeuger, Komfort und Fahrplanbegrenzung im Ergebnis"),
            // Ein älteres Paket führt keine freie Kühlung über die Wärmequelle; der Schalter kommt aus (0), Grädigkeit,
            // Leistungsgrenze und die Zähler im Ergebnis kommen leer an (Festwert, Kälteleistung, „nicht erhoben").
            new Stufe(FreieKuehlungSoleSchema.SCHRITT, Art.Ddl,
                      "Freie Kühlung über die Wärmequelle: Schalter, Grädigkeit, Leistungsgrenze; Kälte und Stunden im Ergebnis"),
            // Ein älteres Paket führt keinen Ausweis der Vorlaufwahl; die Spalten kommen leer an („keine Wahl").
            new Stufe(VorlaufwahlSchema.SCHRITT, Art.Ddl,
                      "Vorlaufwahl der Wärmepumpe: Stunden je Kennlinienstützstelle, darüber und darunter im Ergebnis"),
            // Ein Paket führt keinen Katalog der Nutzungsprofile (das Ziel führt ihn samt Saat); die Nutzung seiner
            // Kalender bleibt, wie sie ist, und der Profilname der Zone kommt leer an.
            new Stufe(RaumnutzungSchema.SCHRITT, Art.Ddl,
                      "Katalog der Nutzungsprofile, freie Nutzung an Kalender und Vorlage, Profilname an der Zone"),
            // Ein Paket führt keinen Katalog der Nutzungsprofile; die Kategorie DIN des Ziels steht schon auf der Ausgabe 2025.
            new Stufe(RaumnutzungDinTsSchema.SCHRITT, Art.Katalog,
                      "Kategorie DIN der Nutzungsprofile nach DIN/TS 18599-10:2025-10: Nummern und Namen ohne Werte"),
            // Ein älteres Paket führt keine Grundrisse je importiertem Raum; die Tabelle entsteht leer, die
            // Gebäude exportieren schematisch wie vor dem Schritt.
            new Stufe(RaumgrundrissSchema.SCHRITT, Art.Ddl,
                      "Grundriss je importiertem Raum (Ringe, Boden, Höhe, Herleitung) an der Importquelle"),
            // Ein älteres Paket führt kein Kennzeichen des Ersatzaufbaus; die Spalte kommt leer an (= echter Aufbau).
            new Stufe(TypaufbauSchema.SCHRITT, Art.Ddl,
                      "Kennzeichen Typaufbau an Projekt- und Katalogaufbau, Saat der Typaufbauten"),
            // Ein Paket führt keinen Katalog des Strombedarfs (das Ziel führt die drei Sätze samt Saat); die
            // Projektkopien seiner Stromverbraucher bleiben, wie sie sind.
            new Stufe(StandardlastprofilSchema.SCHRITT, Art.Katalog,
                      "BDEW-Standardlastprofile Strom 2025 (H25, G25, L25) im Katalog des Strombedarfs"),
            // Ein älteres Paket führt weder verwendeten Aufschlag noch bemessene Aufheizzeit im Ergebnis; die Spalten
            // kommen leer an (der Bericht nennt dann den Aufschlag der Projekteinstellung).
            new Stufe(AufheizAufschlagErgebnisSchema.SCHRITT, Art.Ddl,
                      "Verwendeter Aufschlag und bemessene Aufheizzeit in der Ergebniszeile des Gebäudes"),
            // Ein älteres Paket führt keine Kennzahlen des Sondenfeldes; die Spalten kommen leer an (= Normvorgabe).
            new Stufe(ErdsondenfeldSchema.SCHRITT, Art.Ddl,
                      "Geometrie und Bohrlochkennwerte des Erdsondenfeldes je Anlage"),
            // Ein Paket führt keinen Katalog des Strombedarfs (das Ziel führt die zwei Sätze samt Saat); die
            // Projektkopien seiner Stromverbraucher bleiben, wie sie sind.
            new Stufe(StandardlastprofilPvSchema.SCHRITT, Art.Katalog,
                      "BDEW-Netzbezugsprofile Strom 2025 (P25, S25) im Katalog des Strombedarfs"),
        };

        /// <summary>Das Register, aufsteigend nach Schrittnummer.</summary>
        public static IReadOnlyList<Stufe> Stufen => STUFEN;

        /// <summary>Was die Anhebung eines Pakets mit diesem Stand bedeutet.</summary>
        public static Vorschau Vorschauen(int paketstand)
        {
            int ziel = SchemaStand.Zielversion;
            var v = new Vorschau { Von = paketstand, Bis = ziel };
            if (!v.Noetig) return v;
            v.Schritte = ziel - paketstand;
            v.Umformungen = STUFEN.Count(s => s.Nr > paketstand && s.Nr <= ziel && s.Wirkung == Art.Umformung);
            v.UnterGrenze = paketstand < UNTERE_GRENZE;
            return v;
        }

        /// <summary>
        /// Hebt die Bäume eines Pakets vom <paramref name="paketstand"/> auf den Zielstand.
        /// Jeder Baum bekommt seine eigene Arbeitsdatenbank; die Kataloge des Pakets dienen
        /// dort zum Nachschlagen. Scheitert eine Stufe, wirft sie <see cref="AnhebungFehler"/>
        /// — die Bäume sind dann womöglich teilweise umgeformt, und der Aufrufer bricht ab.
        /// </summary>
        /// <param name="neueTabellen">Je Baum die Tabellen, die eine Stufe angelegt hat.</param>
        /// <returns>Die Berichtszeilen der Stufen, die etwas fanden.</returns>
        internal static List<string> Anheben(int paketstand,
            IReadOnlyList<Dictionary<string, List<Dictionary<string, JsonElement>>>> baeume,
            IEnumerable<KeyValuePair<string, List<Dictionary<string, JsonElement>>>> nachschlagen,
            out List<List<string>> neueTabellen)
        {
            var bericht = new List<string>();
            neueTabellen = new List<List<string>>();
            var kataloge = nachschlagen?.ToList() ?? new List<KeyValuePair<string, List<Dictionary<string, JsonElement>>>>();
            int ziel = SchemaStand.Zielversion;

            for (int b = 0; b < baeume.Count; b++)
            {
                using (var db = new Paketarbeitsdatenbank(baeume[b], kataloge))
                {
                    foreach (Stufe s in STUFEN)
                    {
                        if (s.Nr <= paketstand || s.Nr > ziel || s.Umformung == null) continue;
                        string zeile;
                        try { zeile = s.Umformung(db); }
                        catch (Exception ex) { throw new AnhebungFehler(s.Nr, ex.Message, ex); }
                        if (!string.IsNullOrEmpty(zeile))
                            bericht.Add("Schritt " + s.Nr.ToString(CultureInfo.InvariantCulture) +
                                        (baeume.Count > 1 ? " (Projekt " + (b + 1).ToString(CultureInfo.InvariantCulture) + ")" : "") +
                                        ": " + zeile);
                    }
                    try { db.Zurueckschreiben(); }
                    catch (Exception ex) { throw new AnhebungFehler(ziel, ex.Message, ex); }
                    neueTabellen.Add(db.NeueTabellen.ToList());
                }
            }
            return bericht;
        }

        // =================================================================
        //  Die Umformungen — je eine Anweisung des Kern-Bausteins
        // =================================================================

        private static string Zeilen(int n, string was) =>
            n.ToString(CultureInfo.InvariantCulture) + " " + was;

        /// <summary>
        /// Der lokalisierte Text eines Registerschlüssels; der deutsche Text bleibt der Rückfall,
        /// falls der Schlüssel fehlt (Hausregel, wie <c>ProjektExportImportCtrl.T</c>).
        /// </summary>
        private static string Text(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

        /// <summary>67 — <see cref="BhkwLeistungsgrenzeVorgabe.Anhebung"/>: eine leere
        /// BHKW-Leistungsuntergrenze bekommt die 30 %, mit denen sie gerechnet hat.</summary>
        private static string Schritt67(Paketarbeitsdatenbank db)
        {
            if (!db.SpalteVorhanden(BhkwLeistungsgrenzeVorgabe.TABELLE, BhkwLeistungsgrenzeVorgabe.SPALTE)) return null;
            int n = db.Ausfuehren(BhkwLeistungsgrenzeVorgabe.Anhebung());
            return n > 0 ? Zeilen(n, string.Format(CultureInfo.InvariantCulture,
                Text("TRANSFER_ANHEBUNG_S67", "Projekteinstellung(en) ohne BHKW-Leistungsuntergrenze auf {0} % gesetzt"),
                BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT)) : null;
        }

        /// <summary>
        /// 69 — <see cref="PvKoeffizientenReparatur"/> an der Projektkopie der PV-Module:
        /// reparieren, was die Wertequelle (Auslieferung, CEC-Liste) kennt, übernehmen, was
        /// der mitgereiste Stammsatz gesund führt, den Rest leeren — dieselbe Folge wie die
        /// Migration.
        /// </summary>
        private static string Schritt69(Paketarbeitsdatenbank db)
        {
            string t = PvKoeffizientenReparatur.TAB_PROJEKT;
            var spalten = new List<string> { PvKoeffizientenReparatur.SPALTE_BEZEICHNER, "ID" };
            spalten.AddRange(PvKoeffizientenReparatur.SPALTEN);
            if (!Hat(db, t, spalten.ToArray())) return null;
            db.SpalteSicherstellen(t, PvKoeffizientenReparatur.SPALTE_ISC);
            db.SpalteSicherstellen(t, PvKoeffizientenReparatur.SPALTE_FIRMA);

            long vorher = Ganz(db.Skalar(PvKoeffizientenReparatur.ZaehlungVerdorben(t)));
            IList<string> bezeichner = PvKoeffizientenReparatur.Zerlege(
                Convert.ToString(db.Skalar(PvKoeffizientenReparatur.BezeichnerAbfrage(t)), CultureInfo.InvariantCulture));
            if (bezeichner.Count > 0)
            {
                PvKoeffizientenquelle quelle;
                try { quelle = PvKoeffizientenReparatur.Quelle(); }
                catch (Exception) { quelle = new PvKoeffizientenquelle(PvKoeffizientenReparatur.AUSLIEFERUNG); }
                foreach (string b in bezeichner)
                {
                    if (!quelle.Finde(b, null, out PvModulKoeffizienten satz)) continue;
                    string sql = PvKoeffizientenReparatur.Reparatur(t, satz);
                    if (sql != null) db.Ausfuehren(sql);
                }
            }

            var stammspalten = new List<string> { "ID", PvKoeffizientenReparatur.SPALTE_BEZEICHNER, PvKoeffizientenReparatur.SPALTE_ISC };
            stammspalten.AddRange(PvKoeffizientenReparatur.SPALTEN);
            db.NachschlagenSicherstellen(PvKoeffizientenReparatur.TAB_STAMM, stammspalten.ToArray());
            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                db.Ausfuehren(PvKoeffizientenReparatur.UebernahmeAusStamm(s));

            long ohneTreffer = Ganz(db.Skalar(PvKoeffizientenReparatur.ZaehlungVerdorben(t)));
            foreach (string s in PvKoeffizientenReparatur.SPALTEN)
                db.Ausfuehren(PvKoeffizientenReparatur.Leerung(t, s));

            if (vorher <= 0) return null;
            return Zeilen((int)vorher, string.Format(CultureInfo.InvariantCulture,
                Text("TRANSFER_ANHEBUNG_S69", "PV-Modul(e) mit verdorbenem Koeffizienten: {0} repariert, {1} ohne Treffer auf leer gesetzt"),
                vorher - ohneTreffer, ohneTreffer));
        }

        /// <summary>76 — <see cref="ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN"/>: je Projekt
        /// und Träger bleibt der Satz mit der kleinsten Id.</summary>
        private static string Schritt76(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, ProjektEnergietraegerEindeutig.TABELLE, "ID", ProjektEnergietraegerEindeutig.SPALTE_PROJEKT,
                     ProjektEnergietraegerEindeutig.SPALTE_TRAEGER)) return null;
            int n = db.Ausfuehren(ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN);
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S76", "überzählige(r) Trägersatz/-sätze entfernt")) : null;
        }

        /// <summary>79 — <see cref="HeizstabJeWaermepumpe.SqlUebernahme"/>: jede Wärmepumpe
        /// übernimmt den Heizstab-Schalter ihres Projekts.</summary>
        private static string Schritt79(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, HeizstabJeWaermepumpe.TABELLE_EINSTELLUNGEN, "ID", "ID_Projekt", HeizstabJeWaermepumpe.SPALTE_PROJEKT) ||
                !Hat(db, HeizstabJeWaermepumpe.TABELLE_ANLAGEN, "ID_Type", "ID_Projekt")) return null;
            db.SpalteSicherstellen(HeizstabJeWaermepumpe.TABELLE_ANLAGEN, HeizstabJeWaermepumpe.SPALTE_ANLAGE);
            int n = db.Ausfuehren(HeizstabJeWaermepumpe.SqlUebernahme());
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S79", "Wärmepumpe(n) tragen den Heizstab-Schalter ihres Projekts")) : null;
        }

        /// <summary>83 — <see cref="StrompreisZerlegung.Falten(Umformzugriff)"/>: der wirksame
        /// Aufschlag geht in den Arbeitspreis, die Anteile zerlegen ihn.</summary>
        private static string Schritt83(Paketarbeitsdatenbank db)
        {
            string t = StrompreisZerlegung.TABELLE;
            if (!Hat(db, t, "ID_Projekt", "ID_Energieträger")) return null;
            foreach (string s in StrompreisZerlegung.BESTANDSANTEILE)
            {
                db.SpalteSicherstellen(t, s);
                db.SpalteSicherstellen(t, s + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX);
            }
            foreach (string s in new[] { StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS, StrompreisAltspalten.SPALTE_AUFSCHLAG_OVERRIDE,
                                         "custom_price_work", "custom_hi", SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG,
                                         SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX })
                db.SpalteSicherstellen(t, s);
            db.NachschlagenSicherstellen(StrompreisZerlegung.TABELLE_TRAEGER, "id", "pricing_model", "hi_kwh_per_unit", "price_work");
            db.NachschlagenSicherstellen(StrompreisZerlegung.TABELLE_PREIS, "ID_Projekt", "carrier_id", "arbeitspreis", "valid_from");
            int n = StrompreisZerlegung.Falten(db.Zugriff).Count;
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S83", "Stromträgersatz/-sätze: Aufschlag in den Arbeitspreis gefaltet oder Anteile stillgelegt")) : null;
        }

        /// <summary>84 — <see cref="VerguetungUmzug.Umziehen(Umformzugriff)"/>: die Vergütung
        /// der Trägerkarte (ct/kWh) wird Parameter der Wirtschaftlichkeit (EUR/kWh).</summary>
        private static string Schritt84(Paketarbeitsdatenbank db)
        {
            string karte = VerguetungUmzug.TABELLE_KARTE;
            if (!Hat(db, karte, "ID_Projekt", "ID_Energieträger", StrompreisAltspalten.SPALTE_VERGUETUNG_PV,
                     StrompreisAltspalten.SPALTE_VERGUETUNG_BHKW)) return null;
            string p = VerguetungUmzug.TABELLE_PARAMETER;
            if (!db.TabelleVorhanden(p))
                db.Ausfuehren("CREATE TABLE \"" + p + "\" (\"ID\", \"ID_Projekt\")");
            foreach (string s in new[] { "ID", "ID_Projekt", "Einspeiseverguetung", SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK, "GeaendertAm" })
                db.SpalteSicherstellen(p, s);
            db.NachschlagenSicherstellen(VerguetungUmzug.TABELLE_TRAEGER, "id", "pricing_model");
            int n = VerguetungUmzug.Umziehen(db.Zugriff).Count(z => z.Contains("(Parametersatz"));
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S84", "Projekt(e): Einspeisevergütung der Trägerkarte in die Wirtschaftlichkeitsparameter übernommen")) : null;
        }

        /// <summary>87 — <see cref="SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN"/>: je Projekt
        /// bleibt die aktive Speichervariante mit der kleinsten Id aktiv.</summary>
        private static string Schritt87(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, SpeicherVarianteAktivEindeutig.TABELLE, "ID", SpeicherVarianteAktivEindeutig.SPALTE_AKTIV,
                     SpeicherVarianteAktivEindeutig.SPALTE_ANLAGE) ||
                !Hat(db, SpeicherVarianteAktivEindeutig.TABELLE_ANLAGEN, "ID", "ID_Projekt")) return null;
            int n = db.Ausfuehren(SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN);
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S87", "zweite aktive Speichervariante(n) abgeschaltet")) : null;
        }

        /// <summary>89 — <see cref="KwkAnlagenwahrheit.Uebertragung"/> je Spaltenpaar: der
        /// KWK-Zuschlag des Projekts geht an jede BHKW-Anlage, die ihn nicht selbst führt.</summary>
        private static string Schritt89(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, KwkAnlagenwahrheit.TABELLE, "ID_Type", "ID_Projekt") ||
                !Hat(db, KwkAnlagenwahrheit.QUELLE, "ID_Projekt")) return null;
            int n = 0;
            foreach (KwkAnlagenwahrheit.Paar paar in KwkAnlagenwahrheit.Paare)
            {
                if (!db.SpalteVorhanden(KwkAnlagenwahrheit.QUELLE, paar.Projekt)) continue;
                db.SpalteSicherstellen(KwkAnlagenwahrheit.TABELLE, paar.Anlage);
                n += db.Ausfuehren(KwkAnlagenwahrheit.Uebertragung(paar));
            }
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S89", "KWKG-Angabe(n) vom Projekt an die BHKW-Anlage übertragen")) : null;
        }

        /// <summary>90 — <see cref="KostenErfassungsgruppenAltzeilen.SqlLoeschen"/>: Nullzeilen
        /// der drei Erfassungsgruppen, nachgeschlagen in den mitgereisten Kostenkatalogen.</summary>
        private static string Schritt90(Paketarbeitsdatenbank db)
        {
            string t = KostenErfassungsgruppenAltzeilen.TABELLE;
            if (!Hat(db, t, "ID", "ProjektID", "KategorieID", "KomponentenID", "StammID")) return null;
            foreach (string s in new[] { "EingegebenerWert", "Worstcase", "Bestcase", "Menge", "Einheitpreis" })
                db.SpalteSicherstellen(t, s);
            db.NachschlagenSicherstellen(KostenErfassungsgruppenAltzeilen.TABELLE_KOMPONENTE, "ID", SchemaKatalog.SPALTE_KK_KOMPONENTE);
            db.NachschlagenSicherstellen(KostenErfassungsgruppenAltzeilen.TABELLE_FAKTOR, "StammID",
                                         KostenErfassungsgruppenAltzeilen.SPALTE_HAUPTKOMPONENTE);
            int n = db.Ausfuehren(KostenErfassungsgruppenAltzeilen.SqlLoeschen());
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S90", "Nullzeile(n) der Erfassungsgruppen entfernt")) : null;
        }

        /// <summary>98 — <see cref="BhkwWirkungsgradFaktor.SqlUmrechnen"/> an der Projektkopie.</summary>
        private static string Schritt98(Paketarbeitsdatenbank db)
        {
            string t = BhkwWirkungsgradFaktor.TAB_PROJEKT;
            if (!Hat(db, t, BhkwWirkungsgradFaktor.SPALTE, BhkwWirkungsgradFaktor.SPALTE_PEL, BhkwWirkungsgradFaktor.SPALTE_PTHERM))
                return null;
            int n = db.Ausfuehren(BhkwWirkungsgradFaktor.SqlUmrechnen(t), BhkwWirkungsgradFaktor.ParameterUmrechnen());
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S98",
                "BHKW-Wirkungsgrad(e) vom Prozentwert auf den Faktor umgerechnet")) : null;
        }

        /// <summary>99 — <see cref="BhkwWirkungsgradAnteile.SqlAufteilen"/> an der Projektkopie.</summary>
        private static string Schritt99(Paketarbeitsdatenbank db)
        {
            string t = BhkwWirkungsgradAnteile.TAB_PROJEKT;
            if (!Hat(db, t, BhkwWirkungsgradAnteile.SPALTE_GESAMT, BhkwWirkungsgradAnteile.SPALTE_PEL,
                     BhkwWirkungsgradAnteile.SPALTE_PTHERM)) return null;
            db.SpalteSicherstellen(t, BhkwWirkungsgradAnteile.SPALTE_EL);
            db.SpalteSicherstellen(t, BhkwWirkungsgradAnteile.SPALTE_TH);
            int n = db.Ausfuehren(BhkwWirkungsgradAnteile.SqlAufteilen(t), BhkwWirkungsgradAnteile.ParameterAufteilen());
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S99",
                "BHKW-Wirkungsgrad(e) in elektrisch und thermisch aufgeteilt")) : null;
        }

        /// <summary>101 — <see cref="GebaeudeSchema.UmbenennungSql"/> an der Projektkopie.</summary>
        private static string Schritt101(Paketarbeitsdatenbank db)
        {
            string t = GebaeudeSchema.TAB_GEBAEUDE;
            if (!db.SpalteVorhanden(t, GebaeudeSchema.SPALTE_WOHNFLAECHE_ALT)) return null;
            if (db.SpalteVorhanden(t, GebaeudeSchema.SPALTE_NUTZFLAECHE))
                return Text("TRANSFER_ANHEBUNG_S101_BEIDE",
                    "Gebäude führen Wohn- und Nutzfläche — die Wohnfläche bleibt unberücksichtigt");
            db.Ausfuehren(GebaeudeSchema.UmbenennungSql(t));
            return Text("TRANSFER_ANHEBUNG_S101", "Wohnfläche der Gebäude als Nutzfläche übernommen");
        }

        /// <summary>102 — <see cref="KwkgAnlagenartLeer.SQL_SETZEN"/>.</summary>
        private static string Schritt102(Paketarbeitsdatenbank db)
        {
            if (!db.SpalteVorhanden(KwkgAnlagenartLeer.TABELLE, KwkgAnlagenartLeer.SPALTE)) return null;
            int n = db.Ausfuehren(KwkgAnlagenartLeer.SQL_SETZEN, KwkgAnlagenartLeer.Parameter());
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S102",
                "leere KWKG-Anlagenart(en) auf „nicht gepflegt“ gesetzt")) : null;
        }

        /// <summary>
        /// 104 — die Anweisungen von <see cref="ZeitzonentarifAbloesung"/> am Baum: Zonensätze
        /// löschen, einen mit Zonentarif gerechneten Lauf verwerfen, die Zonenzeilen der
        /// Strommatrix zur Jahreszeile zusammenfassen. Die Staffel eines rechnenden Satzes
        /// wird genannt, nicht geschrieben — ihr Ziel (der Stromträger des Projekts) ist eine
        /// Auskunft der Datenbank, nicht des Pakets.
        /// </summary>
        private static string Schritt104(Paketarbeitsdatenbank db)
        {
            var teile = new List<string>();
            var projekte = new HashSet<long>();
            string tarif = ZeitzonentarifAbloesung.TAB_TARIF;

            if (db.TabelleVorhanden(tarif))
            {
                bool mitModus = db.SpalteVorhanden(tarif, ZeitzonentarifAbloesung.SPALTE_MODUS);
                foreach (string s in new[] { "ID_Projekt", "Aktiv", "Bezug_W_HT", "Bezug_W_NT", "Bezug_S_HT", "Bezug_S_NT",
                                             "Staffel_Grenze", "Staffel_Preis1", "Staffel_Preis2" })
                    db.SpalteSicherstellen(tarif, s);

                foreach (DataRow r in db.Lesen(ZeitzonentarifAbloesung.SqlStaffelquellen(mitModus),
                                               ZeitzonentarifAbloesung.Modusparameter(mitModus)).Rows)
                    teile.Add(string.Format(CultureInfo.CurrentCulture,
                        Text("TRANSFER_ANHEBUNG_S104_STAFFEL",
                             "Leistungspreis-Staffel des Zonentarifs nicht übernommen (Grenze {0} kW, {1} / {2} EUR/(kW·a)) — bitte am Stromträger pflegen"),
                        Zahl(r["Staffel_Grenze"]), Zahl(r["Staffel_Preis1"]), Zahl(r["Staffel_Preis2"])));

                foreach (DataRow r in db.Lesen("SELECT [ID_Projekt] FROM [" + tarif + "] WHERE " +
                                               ZeitzonentarifAbloesung.Zonenbedingung(mitModus),
                                               ZeitzonentarifAbloesung.Modusparameter(mitModus)).Rows)
                    if (r[0] != DBNull.Value) projekte.Add(Convert.ToInt64(r[0], CultureInfo.InvariantCulture));

                int n = db.Ausfuehren(ZeitzonentarifAbloesung.SqlZonensaetzeLoeschen(mitModus),
                                      ZeitzonentarifAbloesung.Modusparameter(mitModus));
                if (n > 0) teile.Add(Zeilen(n, Text("TRANSFER_ANHEBUNG_S104_TARIF",
                    "Tarifsatz/-sätze des Zonenmodells entfernt")));
            }

            string ergebnis = ZeitzonentarifAbloesung.TAB_ERGEBNIS;
            var verworfen = new HashSet<long>();
            if (projekte.Count > 0 && Hat(db, ergebnis, "ID_Projekt", ZeitzonentarifAbloesung.SPALTE_STROMKOSTEN_TARIF))
                foreach (long p in projekte)
                {
                    object o = db.Skalar(ZeitzonentarifAbloesung.SQL_TARIFERGEBNISSE_ZAEHLEN, new DbParam("@p", p));
                    if (o == null || o == DBNull.Value || Convert.ToInt64(o, CultureInfo.InvariantCulture) <= 0) continue;
                    db.Ausfuehren(ZeitzonentarifAbloesung.SQL_ERGEBNIS_LOESCHEN, new DbParam("@p", p));
                    if (Hat(db, ZeitzonentarifAbloesung.TAB_SENS, "ID_Projekt"))
                        db.Ausfuehren(ZeitzonentarifAbloesung.SQL_SENS_LOESCHEN, new DbParam("@p", p));
                    if (Hat(db, ZeitzonentarifAbloesung.TAB_MATRIX, "ID_Projekt"))
                        db.Ausfuehren(ZeitzonentarifAbloesung.SQL_MATRIX_LOESCHEN, new DbParam("@p", p));
                    verworfen.Add(p);
                }
            if (verworfen.Count > 0)
                teile.Add(Text("TRANSFER_ANHEBUNG_S104_ERGEBNIS",
                    "mit Zonentarif gerechnetes Wirtschaftlichkeitsergebnis verworfen — der nächste Lauf rechnet neu"));

            string matrix = ZeitzonentarifAbloesung.TAB_MATRIX;
            if (Hat(db, matrix, "ID_Projekt", "Zone"))
            {
                foreach (string s in new[] { "ID", "BezugMWh", "EinspPvMWh", "KwkEigenMWh", "KwkEinspMWh", "MaxBezugKW", "BedarfMWh", "Zeitstempel" })
                    db.SpalteSicherstellen(matrix, s);
                DataTable summen = db.Lesen(ZeitzonentarifAbloesung.SQL_ZONENZEILEN_SUMMEN, ZeitzonentarifAbloesung.Zonenparameter());
                if (summen.Rows.Count > 0)
                {
                    object o = db.Skalar(ZeitzonentarifAbloesung.SQL_MATRIX_MAX_ID);
                    long id = (o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture)) + 1;
                    foreach (DataRow r in summen.Rows)
                    {
                        long p = Convert.ToInt64(r["ID_Projekt"], CultureInfo.InvariantCulture);
                        db.Ausfuehren(ZeitzonentarifAbloesung.SQL_JAHRESZEILE,
                            new DbParam("@id", id++), new DbParam("@p", p), new DbParam("@z", StromMatrix.ZEILE_JAHR),
                            new DbParam("@b", Math.Round(Wert(r["Bezug"]), 3)),
                            new DbParam("@pv", Math.Round(Wert(r["EinspPv"]), 3)),
                            new DbParam("@ke", Math.Round(Wert(r["KwkEigen"]), 3)),
                            new DbParam("@ki", Math.Round(Wert(r["KwkEinsp"]), 3)),
                            new DbParam("@mx", Math.Round(Wert(r["MaxBezug"]), 1)),
                            new DbParam("@bd", Math.Round(Wert(r["Bedarf"]), 3)),
                            new DbParam("@zeit", r["Zeit"] == DBNull.Value ? (object)DBNull.Value
                                                                         : Convert.ToString(r["Zeit"], CultureInfo.InvariantCulture)));
                        db.Ausfuehren(ZeitzonentarifAbloesung.SQL_ZONENZEILEN_LOESCHEN,
                                      ZeitzonentarifAbloesung.Zonenparameter(new DbParam("@p", p)));
                    }
                    teile.Add(Text("TRANSFER_ANHEBUNG_S104_MATRIX",
                        "Zonenzeilen der Strommatrix zur Jahreszeile zusammengefasst"));
                }
            }
            return teile.Count > 0 ? string.Join("; ", teile) : null;
        }

        /// <summary>106 — <see cref="WirtschaftlichkeitFremdverweis.SQL_SETZEN"/>; fehlt der
        /// Lauf im Paket, ist jeder Verweis fremd.</summary>
        private static string Schritt106(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, WirtschaftlichkeitFremdverweis.TABELLE, WirtschaftlichkeitFremdverweis.SPALTE, "ID_Projekt"))
                return null;
            db.NachschlagenSicherstellen(WirtschaftlichkeitFremdverweis.TAB_LAUF, "ID", "ID_Projekt");
            int n = db.Ausfuehren(WirtschaftlichkeitFremdverweis.SQL_SETZEN);
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S106",
                "Verweis(e) der Wirtschaftlichkeit auf einen fremden Lauf geleert")) : null;
        }

        /// <summary>112 — <see cref="PreisbasisUebernahme"/>, mit den Katalogen des Pakets.</summary>
        private static string Schritt112(Paketarbeitsdatenbank db)
        {
            string t = PreisbasisUebernahme.TABELLE;
            if (!Hat(db, t, "ID_Energieträger")) return null;
            db.SpalteSicherstellen(t, PreisbasisUebernahme.SPALTE);
            db.SpalteSicherstellen(t, "ID_Umrechnung");
            db.NachschlagenSicherstellen("energy_conversion", "ID", "to_unit");
            db.NachschlagenSicherstellen("energy_carrier", "id", "billing_unit");
            int kwh = db.Ausfuehren(PreisbasisUebernahme.SQL_KWH,
                new DbParam("@kwh", PreisbasisUebernahme.KWH),
                new DbParam("@schluessel", PreisbasisUebernahme.KWH.ToUpperInvariant()));
            int einheit = db.Ausfuehren(PreisbasisUebernahme.SQL_ABRECHNUNGSEINHEIT);
            int gesetzt = kwh + Math.Max(0, einheit);
            return gesetzt > 0 ? Zeilen(gesetzt, Text("TRANSFER_ANHEBUNG_S112",
                "Preisbasis/-basen der Trägerkarte gesetzt")) : null;
        }

        /// <summary>113 — <see cref="GaseNormkubikmeter.SQL_PREISZEILEN"/> an den Preiszeilen des Projekts.</summary>
        private static string Schritt113(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, "energy_price", "arbeitspreis_unit", "carrier_id")) return null;
            db.NachschlagenSicherstellen("energy_carrier", "id", "ID_Brennstoff");
            int n = db.Ausfuehren(GaseNormkubikmeter.SQL_PREISZEILEN,
                new DbParam("@neu", GaseNormkubikmeter.NEU), new DbParam("@alt", GaseNormkubikmeter.ALT));
            return n > 0 ? Zeilen(n, Text("TRANSFER_ANHEBUNG_S113",
                "Preiszeile(n) eines Gasträgers von m³ auf Nm³")) : null;
        }

        /// <summary>127 — <see cref="ProjektWirkungSchema"/>: der Freitext wird eine Wirkung.</summary>
        private static string Schritt127(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, "Tab_ProjektWirtschaftlichkeit", "Nicht_Monetaer", "ID_Projekt")) return null;
            db.NachschlagenSicherstellen("Tab_Projekt", "ID");
            if (!db.TabelleVorhanden(ProjektWirkungSchema.TABELLE))
            {
                db.Ausfuehren(ProjektWirkungSchema.SQL_CREATE);
                db.Ausfuehren(ProjektWirkungSchema.SQL_INDEX);
            }
            int n = db.Ausfuehren(ProjektWirkungSchema.SQL_UEBERNAHME);
            return n > 0 ? Zeilen(n, string.Format(CultureInfo.CurrentCulture,
                Text("TRANSFER_ANHEBUNG_S127", "Freitext(e) als Wirkung der Kategorie {0} übernommen"),
                ProjektWirkungSchema.KATEGORIE_UEBERNAHME)) : null;
        }

        /// <summary>148 — <see cref="BaualtersklassenSchema.Umschluesseln"/> je Projektgebäude.</summary>
        private static string Schritt148(Paketarbeitsdatenbank db)
        {
            string t = GebaeudeSchema.TAB_GEBAEUDE;
            if (!db.SpalteVorhanden(t, "Baualtersklasse")) return null;
            db.SpalteSicherstellen(t, GebaeudeSchema.SPALTE_ENERGIESTANDARD);
            bool mitBaujahr = db.SpalteVorhanden(t, "Baujahr");
            bool mitName = db.SpalteVorhanden(t, "Gebaeudename");
            DataTable dt = db.Lesen("SELECT [" + Paketarbeitsdatenbank.ZEILE + "] AS Zeile, Baualtersklasse" +
                                    (mitBaujahr ? ", Baujahr" : ", NULL AS Baujahr") +
                                    (mitName ? ", Gebaeudename" : ", NULL AS Gebaeudename") + " FROM [" + t + "]");
            int geaendert = 0;
            var unklar = new List<string>();
            foreach (DataRow r in dt.Rows)
            {
                string alt = r["Baualtersklasse"] == DBNull.Value ? null : Convert.ToString(r["Baualtersklasse"], CultureInfo.InvariantCulture);
                int? baujahr = r["Baujahr"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["Baujahr"], CultureInfo.InvariantCulture);
                BaualtersklassenSchema.Umschluesselung u = BaualtersklassenSchema.Umschluesseln(alt, baujahr);
                if (!u.Eindeutig)
                    unklar.Add("„" + Convert.ToString(r["Gebaeudename"], CultureInfo.InvariantCulture) + "“: " + u.Grund);
                if (string.Equals(u.Klasse, alt, StringComparison.Ordinal) && u.Energiestandard == null) continue;
                db.Ausfuehren("UPDATE [" + t + "] SET Baualtersklasse = ?, " + GebaeudeSchema.SPALTE_ENERGIESTANDARD +
                              " = ? WHERE [" + Paketarbeitsdatenbank.ZEILE + "] = ?",
                              new DbParam("@k", (object)u.Klasse ?? DBNull.Value),
                              new DbParam("@e", (object)u.Energiestandard ?? DBNull.Value),
                              new DbParam("@z", r["Zeile"]));
                geaendert++;
            }
            if (geaendert == 0 && unklar.Count == 0) return null;
            string zeile = Zeilen(geaendert, Text("TRANSFER_ANHEBUNG_S148", "Gebäude auf die Baualtersklassen A bis M umgeschlüsselt"));
            return unklar.Count > 0
                ? string.Format(CultureInfo.CurrentCulture,
                    Text("TRANSFER_ANHEBUNG_S148_UNKLAR", "{0} (unklar: {1})"), zeile, string.Join("; ", unklar))
                : zeile;
        }

        /// <summary>
        /// <see cref="KesselBrennwertNachzug.SCHRITT"/> — <see cref="KesselBrennwertNachzug"/> an den
        /// Projektkesseln des Pakets. Den Katalogsatz sucht die Stufe dort, wo ihn das Programm nach
        /// dem Einspielen sucht: im Katalog des Ziels, über den Bezeichner. Ohne lesbaren Katalog gilt
        /// die Beschreibung.
        /// </summary>
        private static string SchrittKesselBrennwert(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, KesselBrennwertNachzug.TAB_PROJEKT, "ID", KesselBrennwertNachzug.SPALTE, "Bezeichner"))
                return null;
            db.SpalteSicherstellen(KesselBrennwertNachzug.TAB_PROJEKT, "Beschreibung");
            db.SpalteSicherstellen(KesselBrennwertNachzug.TAB_PROJEKT, "Ptherm");
            db.SpalteSicherstellen(KesselBrennwertNachzug.TAB_PROJEKT, "ID_Projekt");
            KesselBrennwertNachzug.Bericht b = KesselBrennwertNachzug.Ausfuehren(db.Zugriff, Umformzugriff.Datenbank, null);
            if (b.Gesetzt == 0) return null;
            return Zeilen(b.Gesetzt, Text("TRANSFER_ANHEBUNG_KESSEL_BRENNWERT",
                "Projektkessel als Brennwertkessel gekennzeichnet (nach Katalogsatz oder Beschreibung)"));
        }

        // =================================================================
        //  Handwerkszeug
        // =================================================================

        private static bool Hat(Paketarbeitsdatenbank db, string tabelle, params string[] spalten)
        {
            if (!db.TabelleVorhanden(tabelle)) return false;
            foreach (string s in spalten) if (!db.SpalteVorhanden(tabelle, s)) return false;
            return true;
        }

        private static long Ganz(object o) =>
            o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);

        private static double Wert(object o) =>
            o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        private static string Zahl(object o) => Wert(o).ToString("0.###", CultureInfo.InvariantCulture);
    }
}
