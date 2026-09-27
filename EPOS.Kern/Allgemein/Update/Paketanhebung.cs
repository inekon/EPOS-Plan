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
    /// (Konzept <c>Dokumentation/aktuell/Konzept_Projektpaket_Migration_EPOS-Plan.md</c>).
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

        /// <summary>67 — <see cref="BhkwLeistungsgrenzeVorgabe.Anhebung"/>: eine leere
        /// BHKW-Leistungsuntergrenze bekommt die 30 %, mit denen sie gerechnet hat.</summary>
        private static string Schritt67(Paketarbeitsdatenbank db)
        {
            if (!db.SpalteVorhanden(BhkwLeistungsgrenzeVorgabe.TABELLE, BhkwLeistungsgrenzeVorgabe.SPALTE)) return null;
            int n = db.Ausfuehren(BhkwLeistungsgrenzeVorgabe.Anhebung());
            return n > 0 ? Zeilen(n, "Projekteinstellung(en) ohne BHKW-Leistungsuntergrenze auf " +
                                     BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT.ToString(CultureInfo.InvariantCulture) + " % gesetzt") : null;
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
            return Zeilen((int)vorher, "PV-Modul(e) mit verdorbenem Koeffizienten: " +
                          (vorher - ohneTreffer).ToString(CultureInfo.InvariantCulture) + " repariert, " +
                          ohneTreffer.ToString(CultureInfo.InvariantCulture) + " ohne Treffer auf leer gesetzt");
        }

        /// <summary>76 — <see cref="ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN"/>: je Projekt
        /// und Träger bleibt der Satz mit der kleinsten Id.</summary>
        private static string Schritt76(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, ProjektEnergietraegerEindeutig.TABELLE, "ID", ProjektEnergietraegerEindeutig.SPALTE_PROJEKT,
                     ProjektEnergietraegerEindeutig.SPALTE_TRAEGER)) return null;
            int n = db.Ausfuehren(ProjektEnergietraegerEindeutig.SQL_ENTDOPPELN);
            return n > 0 ? Zeilen(n, "überzählige(r) Trägersatz/-sätze entfernt") : null;
        }

        /// <summary>79 — <see cref="HeizstabJeWaermepumpe.SqlUebernahme"/>: jede Wärmepumpe
        /// übernimmt den Heizstab-Schalter ihres Projekts.</summary>
        private static string Schritt79(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, HeizstabJeWaermepumpe.TABELLE_EINSTELLUNGEN, "ID", "ID_Projekt", HeizstabJeWaermepumpe.SPALTE_PROJEKT) ||
                !Hat(db, HeizstabJeWaermepumpe.TABELLE_ANLAGEN, "ID_Type", "ID_Projekt")) return null;
            db.SpalteSicherstellen(HeizstabJeWaermepumpe.TABELLE_ANLAGEN, HeizstabJeWaermepumpe.SPALTE_ANLAGE);
            int n = db.Ausfuehren(HeizstabJeWaermepumpe.SqlUebernahme());
            return n > 0 ? Zeilen(n, "Wärmepumpe(n) tragen den Heizstab-Schalter ihres Projekts") : null;
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
            return n > 0 ? Zeilen(n, "Stromträgersatz/-sätze: Aufschlag in den Arbeitspreis gefaltet oder Anteile stillgelegt") : null;
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
            return n > 0 ? Zeilen(n, "Projekt(e): Einspeisevergütung der Trägerkarte in die Wirtschaftlichkeitsparameter übernommen") : null;
        }

        /// <summary>87 — <see cref="SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN"/>: je Projekt
        /// bleibt die aktive Speichervariante mit der kleinsten Id aktiv.</summary>
        private static string Schritt87(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, SpeicherVarianteAktivEindeutig.TABELLE, "ID", SpeicherVarianteAktivEindeutig.SPALTE_AKTIV,
                     SpeicherVarianteAktivEindeutig.SPALTE_ANLAGE) ||
                !Hat(db, SpeicherVarianteAktivEindeutig.TABELLE_ANLAGEN, "ID", "ID_Projekt")) return null;
            int n = db.Ausfuehren(SpeicherVarianteAktivEindeutig.SQL_ENTDOPPELN);
            return n > 0 ? Zeilen(n, "zweite aktive Speichervariante(n) abgeschaltet") : null;
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
            return n > 0 ? Zeilen(n, "KWKG-Angabe(n) vom Projekt an die BHKW-Anlage übertragen") : null;
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
            return n > 0 ? Zeilen(n, "Nullzeile(n) der Erfassungsgruppen entfernt") : null;
        }

        /// <summary>98 — <see cref="BhkwWirkungsgradFaktor.SqlUmrechnen"/> an der Projektkopie.</summary>
        private static string Schritt98(Paketarbeitsdatenbank db)
        {
            string t = BhkwWirkungsgradFaktor.TAB_PROJEKT;
            if (!Hat(db, t, BhkwWirkungsgradFaktor.SPALTE, BhkwWirkungsgradFaktor.SPALTE_PEL, BhkwWirkungsgradFaktor.SPALTE_PTHERM))
                return null;
            int n = db.Ausfuehren(BhkwWirkungsgradFaktor.SqlUmrechnen(t), BhkwWirkungsgradFaktor.ParameterUmrechnen());
            return n > 0 ? Zeilen(n, "BHKW-Wirkungsgrad(e) vom Prozentwert auf den Faktor umgerechnet") : null;
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
            return n > 0 ? Zeilen(n, "BHKW-Wirkungsgrad(e) in elektrisch und thermisch aufgeteilt") : null;
        }

        /// <summary>101 — <see cref="GebaeudeSchema.UmbenennungSql"/> an der Projektkopie.</summary>
        private static string Schritt101(Paketarbeitsdatenbank db)
        {
            string t = GebaeudeSchema.TAB_GEBAEUDE;
            if (!db.SpalteVorhanden(t, GebaeudeSchema.SPALTE_WOHNFLAECHE_ALT)) return null;
            if (db.SpalteVorhanden(t, GebaeudeSchema.SPALTE_NUTZFLAECHE))
                return "Gebäude führen Wohn- und Nutzfläche — die Wohnfläche bleibt unberücksichtigt";
            db.Ausfuehren(GebaeudeSchema.UmbenennungSql(t));
            return "Wohnfläche der Gebäude als Nutzfläche übernommen";
        }

        /// <summary>102 — <see cref="KwkgAnlagenartLeer.SQL_SETZEN"/>.</summary>
        private static string Schritt102(Paketarbeitsdatenbank db)
        {
            if (!db.SpalteVorhanden(KwkgAnlagenartLeer.TABELLE, KwkgAnlagenartLeer.SPALTE)) return null;
            int n = db.Ausfuehren(KwkgAnlagenartLeer.SQL_SETZEN, KwkgAnlagenartLeer.Parameter());
            return n > 0 ? Zeilen(n, "leere KWKG-Anlagenart(en) auf „nicht gepflegt“ gesetzt") : null;
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
                    teile.Add("Leistungspreis-Staffel des Zonentarifs nicht übernommen (Grenze " + Zahl(r["Staffel_Grenze"]) +
                              " kW, " + Zahl(r["Staffel_Preis1"]) + " / " + Zahl(r["Staffel_Preis2"]) +
                              " EUR/(kW·a)) — bitte am Stromträger pflegen");

                foreach (DataRow r in db.Lesen("SELECT [ID_Projekt] FROM [" + tarif + "] WHERE " +
                                               ZeitzonentarifAbloesung.Zonenbedingung(mitModus),
                                               ZeitzonentarifAbloesung.Modusparameter(mitModus)).Rows)
                    if (r[0] != DBNull.Value) projekte.Add(Convert.ToInt64(r[0], CultureInfo.InvariantCulture));

                int n = db.Ausfuehren(ZeitzonentarifAbloesung.SqlZonensaetzeLoeschen(mitModus),
                                      ZeitzonentarifAbloesung.Modusparameter(mitModus));
                if (n > 0) teile.Add(Zeilen(n, "Tarifsatz/-sätze des Zonenmodells entfernt"));
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
                teile.Add("mit Zonentarif gerechnetes Wirtschaftlichkeitsergebnis verworfen — der nächste Lauf rechnet neu");

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
                    teile.Add("Zonenzeilen der Strommatrix zur Jahreszeile zusammengefasst");
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
            return n > 0 ? Zeilen(n, "Verweis(e) der Wirtschaftlichkeit auf einen fremden Lauf geleert") : null;
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
            return gesetzt > 0 ? Zeilen(gesetzt, "Preisbasis/-basen der Trägerkarte gesetzt") : null;
        }

        /// <summary>113 — <see cref="GaseNormkubikmeter.SQL_PREISZEILEN"/> an den Preiszeilen des Projekts.</summary>
        private static string Schritt113(Paketarbeitsdatenbank db)
        {
            if (!Hat(db, "energy_price", "arbeitspreis_unit", "carrier_id")) return null;
            db.NachschlagenSicherstellen("energy_carrier", "id", "ID_Brennstoff");
            int n = db.Ausfuehren(GaseNormkubikmeter.SQL_PREISZEILEN,
                new DbParam("@neu", GaseNormkubikmeter.NEU), new DbParam("@alt", GaseNormkubikmeter.ALT));
            return n > 0 ? Zeilen(n, "Preiszeile(n) eines Gasträgers von m³ auf Nm³") : null;
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
            return n > 0 ? Zeilen(n, "Freitext(e) als Wirkung der Kategorie " + ProjektWirkungSchema.KATEGORIE_UEBERNAHME + " übernommen") : null;
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
            string zeile = Zeilen(geaendert, "Gebäude auf die Baualtersklassen A bis M umgeschlüsselt");
            return unklar.Count > 0 ? zeile + " (unklar: " + string.Join("; ", unklar) + ")" : zeile;
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
