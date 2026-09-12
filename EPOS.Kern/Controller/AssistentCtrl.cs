using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der AUSGANG eines Speicherlaufs des Assistenten (iU9-W16a.4).
    ///
    /// <para>Der Vorläufer kannte diesen Begriff nicht: Er brach bei jedem Fehlschlag
    /// mit einem nackten <c>return</c> ab — <b>siebzehnmal</b>, ohne Meldung und ohne
    /// Rücknahme (Befund W16-B16). Wer nach dem Speichern kein Projekt vorfand, erfuhr
    /// nicht, woran es lag.</para>
    /// </summary>
    public enum AssistentAusgang
    {
        /// <summary>Alles geschrieben.</summary>
        Gespeichert,

        /// <summary>Pflichtprüfung: keine Klimazone gewählt.</summary>
        KlimazoneFehlt,

        /// <summary>Pflichtprüfung: kein Projektname eingegeben.</summary>
        ProjektnameFehlt,

        /// <summary>Ein Schreibschritt ist fehlgeschlagen; <c>Schritt</c> nennt ihn.</summary>
        Fehlgeschlagen
    }

    /// <summary>Ausgang und — im Fehlerfall — der Schritt, an dem es lag.</summary>
    public sealed class AssistentErgebnis
    {
        internal AssistentErgebnis(AssistentAusgang ausgang, string schritt)
        {
            Ausgang = ausgang;
            Schritt = schritt ?? "";
        }

        /// <summary>Wie der Lauf ausgegangen ist.</summary>
        public AssistentAusgang Ausgang { get; private set; }

        /// <summary>
        /// Der Name des fehlgeschlagenen Schrittes (z. B. <c>Add_Stromganglinie</c>);
        /// leer, wenn keiner fehlgeschlagen ist.
        /// </summary>
        public string Schritt { get; private set; }

        /// <summary>Kurzform für <c>Ausgang == AssistentAusgang.Gespeichert</c>.</summary>
        public bool Erfolg { get { return Ausgang == AssistentAusgang.Gespeichert; } }
    }

    /// <summary>
    /// Die DATENSEITE des Projektassistenten (iU9-W16a.4, K3 der Vermessung) — die
    /// sieben Zustandslisten, die sechs Ladewege, die Seitenschaltung und der
    /// Speicherlauf mit seinen beiden Zweigen.
    ///
    /// <para><b>Warum sie im Kern liegt.</b> Bis W16a stand das alles im
    /// RAHMENFENSTER (<c>Views/Wizard/WizardParent.cs</c>, ≈ 340 der 962 Zeilen):
    /// sechs <c>Load*FromDB</c> mit sechs Inline-SQL, der 138-zeilige
    /// <c>SpeichernAusfuehren</c> mit 23 Controlleraufrufen und die beiden
    /// Filterroutinen. Eine Razor-Seite darf davon nichts kennen (Hausregel
    /// <c>EPOS.UI</c>), und auf iOS gibt es das Fenster gar nicht.</para>
    ///
    /// <para><b>Die Reihenfolge der Schreibschritte ist BITGLEICH übernommen.</b> Sie
    /// ist keine Geschmacksfrage: <c>Add_Projekt</c> liefert erst die echte
    /// <c>Tab_Projekt.ID</c>, an der die Energieträgersätze hängen; der
    /// Bearbeiten-Zweig löscht je Gewerk und legt neu an; und
    /// <c>Del_WaermebedarfExtern</c> steht im NEU-Zweig VOR dem Anlegen, obwohl es
    /// dort nichts zu löschen gibt. Wer hier umsortiert, ändert den Datenbestand.</para>
    ///
    /// <para><b>EINE Meldung statt siebzehn stiller <c>return</c>s</b> (Befund
    /// W16-B16, Entscheid E-4): <see cref="Speichern"/> liefert ein
    /// <see cref="AssistentErgebnis"/> und nennt den fehlgeschlagenen Schritt; der
    /// Aufrufer zeigt es EINMAL an. Der Vorläufer brach kommentarlos ab und ließ ein
    /// halb geschriebenes Projekt stehen.</para>
    ///
    /// <para><b>Die TRANSAKTION ist seit W16a-O-1 da</b> (Anwenderfrage E-4, zweite
    /// Hälfte): <see cref="Speichern"/> öffnet EINEN <c>DbVorgang</c>, reicht ihn in
    /// jede der 23 Schreibmethoden von <see cref="WizardCtrl"/> hinein und schreibt ihn
    /// erst fest, wenn ALLE Schritte gelungen sind. Scheitert einer — oder wirft einer —,
    /// wird der ganze Lauf zurückgerollt; die Meldung nennt den Schritt wie bisher. Ein
    /// halb geschriebenes Projekt kann es damit nicht mehr geben. Die Reihenfolge der
    /// Schreibschritte ist dabei Zeichen für Zeichen dieselbe geblieben: Geändert hat
    /// sich die Klammer, nicht der Inhalt.</para>
    ///
    /// <para>Damit die Klammer auch die Katalogcontroller UNTER den 23 Methoden erreicht
    /// (<c>CopyFromStamm</c>, <c>ApplyGanglinieToProjekt</c> …), meldet jede
    /// Schreibmethode den Vorgang über <c>Vorgangsklammer</c> am Faden an; die
    /// Zugriffsschicht leiht sich dessen Verbindung, statt eine eigene zu öffnen.</para>
    ///
    /// <para><b>Was damit NICHT eingelöst ist:</b> Risiko R-W16-6 verlangt für jeden
    /// Umbau des Schreibwegs den Feld-für-Feld-Vergleich am Windows-Gerät
    /// (<c>Referenzlauf.exe projekt</c>). Der steht aus; auf Linux belegen zwei
    /// Kern-Prüffälle den Rückzug und den unveränderten Erfolgsfall.</para>
    /// </summary>
    public class AssistentCtrl
    {
        /// <summary>Betriebsart „neues Projekt" (<c>WizardParent.WIZARD_MODE_NEU</c>).</summary>
        public const int BETRIEBSART_NEU = 0;

        /// <summary>Betriebsart „vorhandenes Projekt bearbeiten".</summary>
        public const int BETRIEBSART_BEARBEITEN = 1;

        /// <summary>Zahl der Assistentenseiten (<c>KOMPONENTEN_ITEM</c> … <c>BHKW_ITEM</c>).</summary>
        public const int SEITEN = 13;

        // =============================================================================
        // Der Zustand eines Assistentenlaufs - die sieben Listen aus WizardParent :85-93
        // =============================================================================

        /// <summary>Die Energieanlagen ALLER Erzeugertypen — eine Liste, gefiltert je Seite.</summary>
        public List<WErzeugerModel> Erzeuger { get; private set; }

        /// <summary>Die Gebäudezuordnungen des Projekts.</summary>
        public List<Z_ProjGebModel> Gebaeude { get; private set; }

        /// <summary>Die Wärmebedarfszuordnungen (extern).</summary>
        public List<Z_ProjWaermebedarfModel> Waermebedarf { get; private set; }

        /// <summary>Die Prozesswärmezuordnungen.</summary>
        public List<Z_ProjektProzesswaermeModel> Prozess { get; private set; }

        /// <summary>Die Stromverbraucherzuordnungen (Standardprofile).</summary>
        public List<Z_ProjektStromverbraucherModel> Stromverbraucher { get; private set; }

        /// <summary>Die Stromganglinienzuordnungen.</summary>
        public List<Z_ProjektStromganglinieModel> Stromganglinie { get; private set; }

        /// <summary>
        /// Der Projektkopf als EINELEMENTIGE geteilte Liste — die erste
        /// Assistentenseite bearbeitet ihn an Ort und Stelle (iU9-W15a.6, Weg (a)).
        /// </summary>
        public List<ProjektKopfDaten> Kopf { get; private set; }

        /// <summary>Der Projektsatz, der geschrieben wird.</summary>
        public ProjektModel Projekt { get; private set; }

        /// <summary><see cref="BETRIEBSART_NEU"/> oder <see cref="BETRIEBSART_BEARBEITEN"/>.</summary>
        public int Betriebsart { get; set; }

        /// <summary>
        /// <c>Tab_Projekt.ID</c> des Projekts; im Neu-Zweig vor dem Speichern eine
        /// geratene <c>MAX(ID)+1</c>.
        /// </summary>
        public int ProjektId { get; set; }

        /// <summary>
        /// Sind die sechs Ladewege für dieses Projekt schon gelaufen? Der Rahmen setzt
        /// das Kennzeichen zurück, wenn in der linken Spalte ein anderes Projekt
        /// markiert wird (<c>WizardParent.bBereitsGeladen</c>).
        /// </summary>
        public bool BereitsGeladen { get; set; }

        /// <summary>Hat der letzte Speicherlauf geschrieben?</summary>
        public bool Gespeichert { get; private set; }

        private readonly bool[] _seiteAktiv = new bool[SEITEN];

        /// <summary>
        /// Baut einen leeren Assistentenlauf — wörtlich der Konstruktor des Rahmens:
        /// alle Listen leer, Betriebsart „neu", Projekt-Id 0 und der Schaltzustand der
        /// dreizehn Seiten wie in <c>WizardParent</c> (:150-162): Komponentenschritt
        /// und Projektkopf an, alles andere aus.
        /// </summary>
        public AssistentCtrl()
        {
            Erzeuger = new List<WErzeugerModel>();
            Gebaeude = new List<Z_ProjGebModel>();
            Waermebedarf = new List<Z_ProjWaermebedarfModel>();
            Prozess = new List<Z_ProjektProzesswaermeModel>();
            Stromverbraucher = new List<Z_ProjektStromverbraucherModel>();
            Stromganglinie = new List<Z_ProjektStromganglinieModel>();
            Kopf = new List<ProjektKopfDaten> { new ProjektKopfDaten() };
            Projekt = new ProjektModel();

            Betriebsart = BETRIEBSART_NEU;
            ProjektId = 0;
            BereitsGeladen = false;

            _seiteAktiv[WizardItemClass.KOMPONENTEN_ITEM] = true;
            _seiteAktiv[WizardItemClass.PROJEKT_ITEM] = true;

            // Der Ausgangsstand ist der Vergleichsstand: Was DANACH anders ist, hat
            // der Anwender getan (Anwenderentscheid 62b-E-1).
            ZustandMerken();
        }

        // =============================================================================
        // UNGESPEICHERTE EINGABEN (Anwenderentscheid 62b-E-1 vom 11.09.2026)
        // =============================================================================

        /// <summary>Der gemerkte Stand des Projektkopfs.</summary>
        private string _abdruckKopf = "";

        /// <summary>Der gemerkte Stand der sechs Listen und der dreizehn Schalter.</summary>
        private string _abdruckListen = "";

        /// <summary>
        /// Hat der Lauf Eingaben, die noch nicht geschrieben sind?
        ///
        /// <para><b>Woraus die Antwort kommt.</b> Aus dem ZUSTAND, nicht aus einem
        /// Ereigniszähler: Der Assistent nimmt an drei Stellen einen Abdruck seines
        /// Zustands — beim Anlegen des Laufs, nach den sechs Ladewegen
        /// (<see cref="Laden"/>) und nach einem gelungenen Speicherlauf — und
        /// vergleicht ihn hier mit dem jetzigen. Ein Fokuswechsel, ein Seitenwechsel
        /// und ein zweites Betreten derselben Seite ändern daran nichts; ein
        /// getipptes Zeichen, eine aufgenommene Anlage und eine abgewählte Kachel
        /// schon.</para>
        ///
        /// <para><b>Zwei Abdrücke statt einem</b>, weil die zwei Teile zu
        /// verschiedenen Zeitpunkten vom PROGRAMM gefüllt werden: Die sechs Listen
        /// füllt <see cref="Laden"/>, den Kopf füllt die Oberfläche beim Betreten der
        /// Projektseite aus der Datenbank (<c>ProjektKopfHuelle.Gaben</c>) — sie
        /// meldet das über <see cref="KopfMerken"/>. Ein gemeinsamer Abdruck
        /// verschlänge beim Laden die Kopfeingabe, die der Anwender unmittelbar davor
        /// gemacht hat.</para>
        ///
        /// <para><c>Projekt</c> steht bewusst NICHT im Abdruck: Es ist der
        /// Schreibpuffer, den <see cref="ProjektkopfUebernehmen"/> aus
        /// <see cref="Kopf"/> ableitet, und es trägt mit
        /// <c>m_Aenderungsdatum = DateTime.Now</c> einen Wert, der sich von selbst
        /// ändert.</para>
        /// </summary>
        public bool HatAenderungen
        {
            get { return KopfAbdruck() != _abdruckKopf || ListenAbdruck() != _abdruckListen; }
        }

        /// <summary>Merkt den JETZIGEN Zustand als den ungeänderten — Kopf und Listen.</summary>
        public void ZustandMerken()
        {
            _abdruckKopf = KopfAbdruck();
            _abdruckListen = ListenAbdruck();
        }

        /// <summary>
        /// Merkt allein den Projektkopf. Die Oberfläche ruft das, nachdem sie ihn aus
        /// der Datenbank bestückt oder vorbelegt hat — was DAS Programm einträgt, ist
        /// keine Eingabe des Anwenders.
        /// </summary>
        public void KopfMerken()
        {
            _abdruckKopf = KopfAbdruck();
        }

        /// <summary>
        /// Der Abdruck des Projektkopfs — die acht Datenfelder, ausdrücklich OHNE
        /// <c>NameAenderbar</c> (das ist eine Anzeigeregel der Betriebsart, keine
        /// Eingabe).
        /// </summary>
        public string KopfAbdruck()
        {
            if (Kopf == null || Kopf.Count == 0) return "";

            ProjektKopfDaten k = Kopf[0];
            return string.Join("", new[]
            {
                k.Name ?? "", k.Beschreibung ?? "", k.Kunde ?? "", k.Bearbeiter ?? "",
                k.Erstelldatum.ToString("O", CultureInfo.InvariantCulture),
                k.Aenderungsdatum.ToString("O", CultureInfo.InvariantCulture),
                k.IdKlimaregion.ToString(CultureInfo.InvariantCulture),
                k.Klimaname ?? ""
            });
        }

        /// <summary>
        /// Der Abdruck der sechs Listen und der dreizehn Seitenschalter.
        /// </summary>
        /// <remarks>
        /// Über REFLEXION, und zwar mit Absicht: Die sieben Modelle führen zusammen
        /// weit über hundert Felder, und eine abgeschriebene Feldliste wäre genau die
        /// zweite Wahrheit, die beim ersten neuen Feld auseinanderliefe — dann meldete
        /// der Assistent eine Eingabe nicht mehr. Gelesen werden nur WERTE (Zahl, Text,
        /// Wahrheitswert, Datum, Aufzählung); Verweisfelder wie das Selbstfeld
        /// <c>items</c> bleiben außen vor.
        /// </remarks>
        private string ListenAbdruck()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SEITEN; i++) sb.Append(_seiteAktiv[i] ? '1' : '0');

            Anhaengen(sb, Erzeuger);
            Anhaengen(sb, Gebaeude);
            Anhaengen(sb, Waermebedarf);
            Anhaengen(sb, Prozess);
            Anhaengen(sb, Stromverbraucher);
            Anhaengen(sb, Stromganglinie);
            return sb.ToString();
        }

        private static void Anhaengen<T>(StringBuilder sb, List<T> liste)
        {
            sb.Append('|').Append(liste == null ? 0 : liste.Count);
            if (liste == null) return;

            foreach (T eintrag in liste)
            {
                sb.Append(';');
                if (eintrag == null) { sb.Append('-'); continue; }

                foreach (Func<object, object> lesen in Leser(eintrag.GetType()))
                {
                    object wert;
                    try { wert = lesen(eintrag); } catch (Exception) { wert = null; }
                    sb.Append(Convert.ToString(wert, CultureInfo.InvariantCulture)).Append('=');
                }
            }
        }

        /// <summary>
        /// Die Wertleser eines Modelltyps, nach Namen sortiert und je Typ einmal
        /// gebaut. Die Reihenfolge von <c>GetFields</c> ist nicht zugesichert; der
        /// Abdruck muss aber über die Laufzeit eines Assistentenlaufs derselbe sein.
        /// </summary>
        private static readonly Dictionary<Type, List<Func<object, object>>> _leser =
            new Dictionary<Type, List<Func<object, object>>>();

        private static List<Func<object, object>> Leser(Type typ)
        {
            lock (_leser)
            {
                List<Func<object, object>> fertig;
                if (_leser.TryGetValue(typ, out fertig)) return fertig;

                var namen = new List<KeyValuePair<string, Func<object, object>>>();

                foreach (FieldInfo f in typ.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (IstWert(f.FieldType))
                        namen.Add(new KeyValuePair<string, Func<object, object>>(
                            "f:" + f.Name, o => f.GetValue(o)));

                foreach (PropertyInfo p in typ.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (p.CanRead && p.GetIndexParameters().Length == 0 && IstWert(p.PropertyType))
                        namen.Add(new KeyValuePair<string, Func<object, object>>(
                            "p:" + p.Name, o => p.GetValue(o)));

                fertig = namen.OrderBy(e => e.Key, StringComparer.Ordinal)
                              .Select(e => e.Value)
                              .ToList();
                _leser[typ] = fertig;
                return fertig;
            }
        }

        /// <summary>Trägt dieser Typ einen VERGLEICHBAREN Wert?</summary>
        private static bool IstWert(Type typ)
        {
            Type t = Nullable.GetUnderlyingType(typ) ?? typ;
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal)
                || t == typeof(DateTime) || t == typeof(TimeSpan) || t == typeof(Guid);
        }

        // =============================================================================
        // Die Seitenschaltung - WizardParent GetNextUpIndex/GetNextDownIndex/lastIndex
        // =============================================================================

        /// <summary>Ist die Seite freigeschaltet?</summary>
        public bool SeiteAktiv(int seitenIndex)
        {
            return seitenIndex >= 0 && seitenIndex < SEITEN && _seiteAktiv[seitenIndex];
        }

        /// <summary>
        /// Schaltet eine Assistentenseite frei oder ab. Der Komponentenschritt tut das
        /// je Kachel; ein Index außerhalb (Brauchwasser, Pufferspeicher: <c>-1</c>)
        /// wird still übergangen — sie haben keine Seite.
        /// </summary>
        public void SeiteSchalten(int seitenIndex, bool aktiv)
        {
            if (seitenIndex < 0 || seitenIndex >= SEITEN) return;
            _seiteAktiv[seitenIndex] = aktiv;
        }

        /// <summary>
        /// Die nächste AKTIVE Seite in einer Richtung — <b>eine</b> Methode statt
        /// <c>GetNextUpIndex</c> und <c>GetNextDownIndex</c> (zusammen 24 Zeilen).
        ///
        /// <para><b>Wörtlich übernommen sind die beiden Grenzfälle:</b> Aufwärts
        /// läuft die Schleife nur bis <c>SEITEN - 1</c> und liefert diesen Index auch
        /// dann, wenn die Seite gar nicht aktiv ist (der Vorläufer bricht die
        /// <c>for</c>-Schleife über die Bedingung ab, nicht über einen Treffer);
        /// abwärts läuft sie bis <c>KOMPONENTEN_ITEM</c>, der immer aktiv ist.</para>
        /// </summary>
        /// <param name="von">Der Schritt, auf dem der Assistent gerade steht.</param>
        /// <param name="richtung">+1 vorwärts, -1 rückwärts.</param>
        public int NaechsteAktive(int von, int richtung)
        {
            int index;

            if (richtung >= 0)
            {
                for (index = von + 1; index < SEITEN - 1; index++)
                    if (_seiteAktiv[index]) break;
                return index;
            }

            for (index = von - 1; index >= WizardItemClass.KOMPONENTEN_ITEM; index--)
                if (_seiteAktiv[index]) break;
            return index;
        }

        /// <summary>
        /// Steht die letzte AKTIVE Seite an? Dann trägt der Weiter-Knopf den
        /// Speichern-Text (wörtlich <c>WizardParent.lastIndex</c>).
        /// </summary>
        public bool LetzteAktive(int von)
        {
            if (von >= SEITEN - 1) return true;

            for (int i = von + 1; i < SEITEN; i++)
                if (_seiteAktiv[i]) return false;

            return true;
        }

        // =============================================================================
        // Laden - die sechs Load*FromDB (WizardParent :500-895)
        // =============================================================================

        /// <summary>
        /// Die sechs Ladewege in ihrer Reihenfolge — wörtlich
        /// <c>WizardParent.Next</c> (:277-282). Ein leerer Projektname lädt nichts;
        /// das ist der Neu-Zweig.
        /// </summary>
        public void Laden(string projektName)
        {
            LadeErzeuger(projektName);
            LadeGebaeude(projektName);
            LadeProzess(projektName);
            LadeStromganglinie(projektName);
            LadeWaermebedarf(projektName);
            LadeStromverbraucher(projektName);
            BereitsGeladen = true;

            // Was die sechs Ladewege eintragen, ist der Stand der DATENBANK und keine
            // Eingabe des Anwenders (62b-E-1). Der Kopfabdruck bleibt unberuehrt: Er
            // kann in diesem Augenblick bereits eine Eingabe tragen - Laden laeuft
            // unmittelbar nachdem die Projektseite verlassen wurde.
            _abdruckListen = ListenAbdruck();
        }

        /// <summary>
        /// Die Energieanlagen eines bestehenden Projekts.
        ///
        /// <para>Die vollständig gelesenen Modelle werden DURCHGEREICHT statt Feld für
        /// Feld umkopiert — die frühere Teilkopie führte 28 der 57 Spalten und verlor
        /// beim Speichern (Löschen + Neuanlegen) alles Übrige, gemessen 34
        /// Feldabweichungen in Projekt 1023.</para>
        /// </summary>
        public void LadeErzeuger(string projekt)
        {
            if (string.IsNullOrEmpty(projekt)) return;

            ProjektCtrl projctrl = new ProjektCtrl();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();

            projctrl.ReadSingle(projekt);
            werzctrl.ReadAllFilter("ID_Projekt=" + projctrl.m_ID);

            Erzeuger.Clear();

            for (int n = 0; n < werzctrl.rows; n++)
            {
                // Einziges Feld, das die Teilkopie BEWUSST gesetzt hat: Es bleibt
                // erhalten. Die Zuweisung greift nur, falls die Spalte in einer alten
                // Datenbank fehlt (AusZeile laesst ID_Projekt dann auf 0 stehen).
                werzctrl.items[n].ID_Projekt = projctrl.m_ID;

                Erzeuger.Add(werzctrl.items[n]);
            }
        }

        /// <summary>
        /// Die Gebäudezuordnungen samt Namen — der Verbund über
        /// <c>Tab_Gebaeude.ID_ProjektGebaeude</c>, den <c>Z_ProjektGebaeude</c> seit
        /// dem Schemawechsel braucht.
        /// </summary>
        public void LadeGebaeude(string projekt)
        {
            if (string.IsNullOrEmpty(projekt)) return;

            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            projctrl.ReadSingle(projekt);

            string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.[ID_Projekt], " +
                "[Tab_Gebaeude].Gebaeudename, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, " +
                "dezWarmwasserbereitung, Gebaeudeart, Beschreibung  FROM [Tab_Gebaeude] " +
                "INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID_ProjektGebaeude = Z_ProjektGebaeude.ID" +
                " where Z_ProjektGebaeude.ID_Projekt=" + projctrl.m_ID;

            rs.Open(sql);
            Gebaeude.Clear();

            while (rs.Next())
            {
                Z_ProjGebModel item = new Z_ProjGebModel();

                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = projctrl.m_ID;
                item.Gebaeudename = (string)rs.Read("Gebaeudename");
                // ID_Gebaeude = Katalog-ID (Tab_Gebaeude_STAMM)
                item.ID_Gebaeude = DataRepository.GetIdByName("Tab_Gebaeude_STAMM", "Bezeichner", item.Gebaeudename);
                item.Wohnflaeche = (double)rs.Read("Wohnflaeche_Waermebedarf");
                item.Einheit = (string)rs.Read("Einheit_Waermebedarf_Wohnflaeche");
                item.Jahresnutzungsgrad = (double)rs.Read("Jahresnutzungsgrad");
                item.DezentralWarmwasser = (bool)rs.Read("dezWarmwasserbereitung");
                item.Gebaeudeart = (string)rs.Read("Gebaeudeart");
                item.Beschreibung = (string)rs.Read("Beschreibung");

                Gebaeude.Add(item);
            }

            // iU9-W16a.4: Der Vorlaeufer liess dieses RecordSet offen stehen - wie
            // LoadWBedarfFromDB (Befund W16-B18). Die fuenf Geschwister schlossen es.
            rs.Close();
        }

        /// <summary>Die Prozesswärmezuordnungen.</summary>
        public void LadeProzess(string projekt)
        {
            if (string.IsNullOrEmpty(projekt)) return;

            ProjektCtrl projctrl = new ProjektCtrl();
            Z_ProjektProzesswaermeCtrl prozctrl = new Z_ProjektProzesswaermeCtrl();

            projctrl.ReadSingle(projekt);
            prozctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + projctrl.m_ID);

            Prozess.Clear();

            for (int n = 0; n < prozctrl.rows; n++)
            {
                Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();

                item.ID_Z = prozctrl.items[n].ID_Z;
                item.ID_Projekt = projctrl.m_ID;
                item.szProzessname = prozctrl.items[n].szProzessname;
                item.ID_Prozesswaerme = prozctrl.items[n].ID_Prozesswaerme;
                item.Summe = prozctrl.items[n].Summe;

                Prozess.Add(item);
            }
        }

        /// <summary>Die Stromganglinienzuordnungen.</summary>
        public void LadeStromganglinie(string projekt)
        {
            if (string.IsNullOrEmpty(projekt)) return;

            ProjektCtrl projctrl = new ProjektCtrl();
            Z_ProjektStromganglinieCtrl prozctrl = new Z_ProjektStromganglinieCtrl();

            projctrl.ReadSingle(projekt);
            prozctrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + projctrl.m_ID);

            Stromganglinie.Clear();

            for (int n = 0; n < prozctrl.rows; n++)
            {
                Z_ProjektStromganglinieModel item = new Z_ProjektStromganglinieModel();

                item.m_ID_Z = prozctrl.items[n].m_ID_Z;
                item.m_ID_Projekt = projctrl.m_ID;
                item.m_szStromganglinie = prozctrl.items[n].m_szStromganglinie;
                item.m_ID_Stromganglinie = prozctrl.items[n].m_ID_Stromganglinie;

                Stromganglinie.Add(item);
            }
        }

        /// <summary>
        /// Die Wärmebedarfszuordnungen (extern).
        ///
        /// <para><b>Befund W16-B18 behoben:</b> Der Vorläufer schloss sein
        /// <c>RecordSet</c> nicht — als einziger der sechs Ladewege.</para>
        /// </summary>
        public void LadeWaermebedarf(string projekt)
        {
            if (string.IsNullOrEmpty(projekt)) return;

            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            projctrl.ReadSingle(projekt);
            rs.Open("select * from Z_ProjektWaermebedarf where ID_Projekt=" + projctrl.m_ID);

            Waermebedarf.Clear();

            while (rs.Next())
            {
                Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                item.m_ID_Z = (int)rs.Read("ID_Z");
                item.m_ID_Projekt = projctrl.m_ID;
                item.m_szBezeichner = (string)rs.Read("Bezeichner");
                item.m_ID_Ganglinie = (int)rs.Read("ID_Ganglinie");

                Waermebedarf.Add(item);
            }

            rs.Close();
        }

        /// <summary>Die Stromverbraucherzuordnungen (Standardprofile).</summary>
        public void LadeStromverbraucher(string projekt)
        {
            if (string.IsNullOrEmpty(projekt)) return;

            ProjektCtrl projctrl = new ProjektCtrl();
            Z_ProjektStromverbraucherCtrl svctrl = new Z_ProjektStromverbraucherCtrl();

            projctrl.ReadSingle(projekt);
            svctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + projctrl.m_ID);

            Stromverbraucher.Clear();

            for (int n = 0; n < svctrl.rows; n++)
            {
                Z_ProjektStromverbraucherModel item = new Z_ProjektStromverbraucherModel();

                item.m_ID_Z = svctrl.items[n].m_ID_Z;
                item.m_ID_Projekt = projctrl.m_ID;
                item.m_szVerbraucher = svctrl.items[n].m_szVerbraucher;
                item.m_ID_Stromverbraucher = svctrl.items[n].m_ID_Stromverbraucher;
                item.m_Summe = svctrl.items[n].m_Summe;

                Stromverbraucher.Add(item);
            }
        }

        // =============================================================================
        // Der Projektkopf
        // =============================================================================

        /// <summary>
        /// Übernimmt die sieben Kopffelder der ersten Assistentenseite in
        /// <see cref="Projekt"/> (wörtlich <c>WizardParent.ProjektkopfUebernehmen</c>).
        /// Das Änderungsdatum steht ausdrücklich auf JETZT.
        /// </summary>
        public void ProjektkopfUebernehmen()
        {
            ProjektKopfDaten kopf = Kopf[0];
            Projekt.m_szProjektname = kopf.Name ?? "";
            Projekt.m_szBeschreibung = kopf.Beschreibung ?? "";
            Projekt.m_szBearbeiter = kopf.Bearbeiter ?? "";
            Projekt.m_szKunde = kopf.Kunde ?? "";
            Projekt.m_Aenderungsdatum = DateTime.Now;
            Projekt.m_Erstelldatum = kopf.Erstelldatum;
            Projekt.m_ID_Klimaregion = kopf.IdKlimaregion;
        }

        // =============================================================================
        // Die beiden Filter - WizardParent :692-740
        // =============================================================================

        /// <summary>
        /// Soll diese Anlagenzeile beim Speichern verschwinden? Filter über
        /// <see cref="Erzeuger"/> nach den ABGEWÄHLTEN Seiten.
        ///
        /// <para><b>FR-1:</b> Der Assistent führt keine Pufferseite;
        /// <c>ID_Type</c>-12-Modelle kommen nur über <see cref="LadeErzeuger"/> in die
        /// Liste. Der Bearbeiten-Zweig löscht die Pufferzeilen nicht mehr
        /// (<c>Del_Projekt_Waermeerzeuger</c> verschont ID_Type 12) — blieben die
        /// Modelle in der Liste, legte <c>Add_WP_Waermeerzeuger</c> die stehen
        /// gebliebenen Anlagenzeilen doppelt an.</para>
        ///
        /// <para><b>P5/E3 — die Kessel-Lücke ist geschlossen.</b> Die Zeile für den
        /// Spitzenkessel fehlte: Ein abgewählter Kessel ließ seine Anlagenzeilen in der
        /// Liste stehen, der Speicherweg legte sie danach wieder an — die Kachel blieb
        /// entgegen der Anzeige belegt.</para>
        /// </summary>
        public bool NichtAktivesElement(WErzeugerModel item)
        {
            if (item.ID_Type == WizardItemClass.PUFFER_TYP) return true;

            if (!_seiteAktiv[WizardItemClass.SOLAR_ITEM] && item.ID_Type == WizardItemClass.SOLAR_TYP) return true;
            if (!_seiteAktiv[WizardItemClass.SP_ITEM] && item.ID_Type == WizardItemClass.SP_TYP) return true;
            if (!_seiteAktiv[WizardItemClass.PV_ITEM] && item.ID_Type == WizardItemClass.PV_TYP) return true;
            if (!_seiteAktiv[WizardItemClass.WP_ITEM] && item.ID_Type == WizardItemClass.WP_TYP) return true;
            if (!_seiteAktiv[WizardItemClass.BHKW_ITEM] && item.ID_Type == WizardItemClass.BHKW_TYP) return true;
            if (!_seiteAktiv[WizardItemClass.KESSEL_ITEM] && item.ID_Type == WizardItemClass.KESSEL_TYP) return true;

            return false;
        }

        /// <summary>
        /// Gegenstück zu <see cref="NichtAktivesElement"/> für die
        /// ZUORDNUNGSTABELLEN.
        ///
        /// <para><b>Warum das nötig ist.</b> Der Bearbeiten-Zweig schreibt jede
        /// Zuordnung als „Löschen + Neuanlegen". Ob eine abgewählte Komponente dabei
        /// verschwand, hing bis P5 vom Zufall ab: Gebäude, Wärmebedarf und
        /// Prozesswärme wurden aus der jeweiligen SEITE gelesen — nie besucht hieß
        /// leere Liste hieß gelöscht; Stromganglinie und Stromverbraucher wurden gar
        /// nicht angetastet. Seither entscheidet allein die Kachel, und sie fragt
        /// vorher.</para>
        /// </summary>
        public void EntferneNichtAktiveZuordnungen()
        {
            if (!_seiteAktiv[WizardItemClass.GEBAEUDE_ITEM]) Gebaeude.Clear();
            if (!_seiteAktiv[WizardItemClass.WAERMEBEDARF_ITEM]) Waermebedarf.Clear();
            if (!_seiteAktiv[WizardItemClass.PROZESS_ITEM]) Prozess.Clear();
            if (!_seiteAktiv[WizardItemClass.STROMSTD_ITEM]) Stromverbraucher.Clear();
            if (!_seiteAktiv[WizardItemClass.STROMLASTGANG_ITEM]) Stromganglinie.Clear();
        }

        // =============================================================================
        // Speichern - WizardParent :552-690, BITGLEICHE Reihenfolge
        // =============================================================================

        /// <summary>
        /// Schreibt den Assistentenlauf. Die Reihenfolge der Schreibschritte ist
        /// bitgleich die des Vorläufers; neu ist allein, dass ein Fehlschlag GEMELDET
        /// statt verschwiegen wird (Entscheid E-4).
        /// </summary>
        public AssistentErgebnis Speichern()
        {
            WizardCtrl ctrl = WizardCtrl.Aktueller;
            if (ctrl == null) return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "WizardCtrl");

            ctrl.Klimazone = Kopf[0].Klimaname ?? "";
            ctrl.Projektname = Kopf[0].Name ?? "";
            ctrl.speichern = false;

            if (ctrl.Klimazone == "")
                return new AssistentErgebnis(AssistentAusgang.KlimazoneFehlt, "");

            if (ctrl.Projektname == "")
                return new AssistentErgebnis(AssistentAusgang.ProjektnameFehlt, "");

            ProjektkopfUebernehmen();
            // Nur den NAMEN der Klimaregion fuehren; die korrekte ID_Klimaregion
            // (Projekt-Kopie) setzt WizardCtrl.Add_Projekt/Update_Projekt.
            Projekt.m_szKlimaregion = Kopf[0].Klimaname ?? "";
            Projekt.m_ID_Klimaregion = 0;

            Gespeichert = false;

            // ===== DIE KLAMMER (W16a-O-1) ====================================
            // EIN Vorgang ueber den GANZEN Lauf. Festgeschrieben wird nur, wenn der
            // Zweig "gespeichert" meldet; jeder andere Ausgang - gescheiterter Schritt
            // wie geworfene Ausnahme - rollt alles zurueck. Der Vorgang ist fuer die
            // Dauer des Laufs am Faden angemeldet (Vorgangsklammer), damit auch die
            // Katalogcontroller unter den 23 Schreibmethoden darin arbeiten.
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                AssistentErgebnis ergebnis;
                try
                {
                    ergebnis = Betriebsart == BETRIEBSART_NEU
                                   ? Anlegen(ctrl, vorgang)
                                   : Fortschreiben(ctrl, vorgang);
                }
                catch (Exception)
                {
                    Gespeichert = false;
                    vorgang.Rollback();
                    throw;
                }

                if (!ergebnis.Erfolg)
                {
                    // Der Schritt hat FALSE gemeldet: nichts von diesem Lauf bleibt
                    // stehen. Die Meldung selbst ist unveraendert (E-4).
                    Gespeichert = false;
                    vorgang.Rollback();
                    return ergebnis;
                }

                try
                {
                    vorgang.Commit();
                }
                catch (Exception)
                {
                    Gespeichert = false;
                    vorgang.Rollback();
                    return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Commit");
                }

                return ergebnis;
            }
        }

        /// <summary>
        /// Der NEU-Zweig — neun Schreibschritte in fester Reihenfolge, alle im selben
        /// <paramref name="vorgang"/>.
        /// </summary>
        private AssistentErgebnis Anlegen(WizardCtrl ctrl, DbVorgang vorgang)
        {
            int id = ProjektId;
            if (!ctrl.Add_Projekt(ref id, Projekt, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt");
            ProjektId = id;

            if (!ctrl.Add_Projekt_ZuordungGebäude(ProjektId, Gebaeude, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_ZuordungGebaeude");

            if (!ctrl.Add_WP_Waermeerzeuger(ProjektId, Erzeuger, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_WP_Waermeerzeuger");

            // Erst hier steht die ECHTE Projekt-ID (Add_Projekt/@@IDENTITY). Die Seiten
            // haben in ihrem CreateNewEnergyCarrier nur den Katalogtraeger angelegt;
            // energy_price und energy_Project_settings haengen an Tab_Projekt.ID und
            // entstehen deshalb erst jetzt.
            if (!ctrl.Add_Projekt_Energietraeger(ProjektId, Erzeuger, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_Energietraeger");

            if (!ctrl.Add_Projekt_Prozess(ProjektId, Prozess, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_Prozess");

            if (!ctrl.Add_Stromganglinie(ProjektId, Stromganglinie, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Stromganglinie");

            if (!ctrl.Del_WaermebedarfExtern(ProjektId, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Del_WaermebedarfExtern");

            if (!ctrl.Add_WaermebedarfExtern(ProjektId, Waermebedarf, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_WaermebedarfExtern");

            if (!ctrl.Add_Projekt_Stromverbraucher(ProjektId, Stromverbraucher, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_Stromverbraucher");

            Gespeichert = true;
            return new AssistentErgebnis(AssistentAusgang.Gespeichert, "");
        }

        /// <summary>
        /// Der BEARBEITEN-Zweig — erst filtern, dann je Gewerk löschen und neu
        /// anlegen, zuletzt der Projektsatz.
        /// </summary>
        private AssistentErgebnis Fortschreiben(WizardCtrl ctrl, DbVorgang vorgang)
        {
            Erzeuger.RemoveAll(NichtAktivesElement);
            EntferneNichtAktiveZuordnungen();

            if (!ctrl.Del_Projekt_Waermeerzeuger(ProjektId, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Del_Projekt_Waermeerzeuger");

            if (!ctrl.Add_WP_Waermeerzeuger(ProjektId, Erzeuger, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_WP_Waermeerzeuger");

            // Auch hier: neu hinzugekommene Traeger bekommen ihre projektgebundenen
            // Saetze, bereits zugeordnete faengt der COUNT-Test ab.
            if (!ctrl.Add_Projekt_Energietraeger(ProjektId, Erzeuger, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_Energietraeger");

            if (!ctrl.Del_Projekt_ZuordungGebäude(ProjektId, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Del_Projekt_ZuordungGebaeude");

            if (!ctrl.Add_Projekt_ZuordungGebäude(ProjektId, Gebaeude, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_ZuordungGebaeude");

            if (!ctrl.Del_Projekt_Prozess(ProjektId, vorgang: vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Del_Projekt_Prozess");

            if (!ctrl.Add_Projekt_Prozess(ProjektId, Prozess, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_Prozess");

            if (!ctrl.Del_Stromganglinie(ProjektId, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Del_Stromganglinie");

            if (!ctrl.Add_Stromganglinie(ProjektId, Stromganglinie, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Stromganglinie");

            if (!ctrl.Del_WaermebedarfExtern(ProjektId, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Del_WaermebedarfExtern");

            if (!ctrl.Add_WaermebedarfExtern(ProjektId, Waermebedarf, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_WaermebedarfExtern");

            if (!ctrl.Del_Projekt_Stromverbraucher(ProjektId, vorgang: vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Del_Projekt_Stromverbraucher");

            if (!ctrl.Add_Projekt_Stromverbraucher(ProjektId, Stromverbraucher, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Add_Projekt_Stromverbraucher");

            Projekt.m_Aenderungsdatum = DateTime.Now;
            Projekt.m_szBearbeiter = Kopf[0].Bearbeiter ?? "";
            Projekt.m_szKunde = Kopf[0].Kunde ?? "";
            Projekt.m_szBeschreibung = Kopf[0].Beschreibung ?? "";
            Projekt.m_szKlimaregion = Kopf[0].Klimaname ?? "";

            if (!ctrl.Update_Projekt(ProjektId, Projekt, vorgang))
                return new AssistentErgebnis(AssistentAusgang.Fehlgeschlagen, "Update_Projekt");

            Gespeichert = true;
            return new AssistentErgebnis(AssistentAusgang.Gespeichert, "");
        }

        // =============================================================================
        // Die Meldungen - vier deutsche Literale werden vier Ressourcenschluessel
        // =============================================================================

        /// <summary>
        /// Der ANZEIGETEXT zu einem Ausgang; leer, wenn es nichts zu melden gibt.
        ///
        /// <para><b>Befund W16-B17:</b> Die beiden Pflichtprüfungen des Vorläufers
        /// trugen die einzigen vier unlokalisierten Literale einer sonst vollständig
        /// zweisprachigen Maske (<c>WizardParent</c> :572, :578). Hier sind es vier
        /// Ressourcenschlüssel, und der dritte Fall — der fehlgeschlagene
        /// Schreibschritt — kommt als fünfter dazu (E-4).</para>
        /// </summary>
        public static string Meldungstext(AssistentErgebnis ergebnis)
        {
            if (ergebnis == null) return "";

            switch (ergebnis.Ausgang)
            {
                case AssistentAusgang.KlimazoneFehlt:
                    return Text("WIZ_KLIMA_FEHLT", "Bitte eine Klimazone auswählen!");
                case AssistentAusgang.ProjektnameFehlt:
                    return Text("WIZ_NAME_FEHLT", "Bitte einen Projektnamen eingeben!");
                case AssistentAusgang.Fehlgeschlagen:
                    return string.Format(
                        Text("WIZ_SPEICHERN_FEHLER",
                             "Das Projekt konnte nicht vollständig gespeichert werden.\n\n" +
                             "Der Schritt „{0}“ ist fehlgeschlagen; die bereits geschriebenen " +
                             "Angaben bleiben stehen."),
                        ergebnis.Schritt);
                default:
                    return "";
            }
        }

        /// <summary>Die ÜBERSCHRIFT zu einem Ausgang; leer, wenn es nichts zu melden gibt.</summary>
        public static string Meldungstitel(AssistentErgebnis ergebnis)
        {
            if (ergebnis == null) return "";

            switch (ergebnis.Ausgang)
            {
                case AssistentAusgang.KlimazoneFehlt:
                    return Text("WIZ_KLIMA_FEHLT_TITEL", "Klimazone fehlt");
                case AssistentAusgang.ProjektnameFehlt:
                    return Text("WIZ_NAME_FEHLT_TITEL", "Projektname fehlt");
                case AssistentAusgang.Fehlgeschlagen:
                    return Text("WIZ_SPEICHERN_FEHLER_TITEL", "Speichern fehlgeschlagen");
                default:
                    return "";
            }
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture); }
            catch (Exception) { }
            return string.IsNullOrEmpty(t) ? rueckfall : Zeilenumbruch.Normalisieren(t);
        }
    }
}
