using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorbelegung von Vor- und Rücklauf einer Anlagenzeile aus dem Katalog</b>
    /// (Anwenderentscheid <b>W6‑E‑4</b> vom 06.09.2026, wörtlich: „Die Vor- und
    /// Rücklauftemperatur sollen beim Anlegen der Komponenten und Energieerzeuger die
    /// Vor- und Rücklauftemperatur aus dem Katalog übernommen werden. Diese können dann
    /// vom Benutzer für das Projekt geändert werden.").
    ///
    /// <para><b>Warum eine eigene Klasse.</b> Die Vorbelegung stand mehrfach in der
    /// Windows-Oberfläche — <c>BhkwHuelle.Aufnehmen</c> und
    /// <c>HeizkesselHuelle.Aufnehmen</c> schrieben jeweils
    /// <c>Vorlauf = stamm.…</c> in den Feldsatz. Wer nicht über eine dieser Hüllen
    /// kam — der Assistent ohne Hülle, der Projektimport, die künftige iOS-Oberfläche —,
    /// legte eine Anlage mit 0/0 an. Hier steht sie EINMAL, und der EINE Schreibweg
    /// aller Anlagen (<c>WizardCtrl.Add_WP_Waermeerzeuger</c>) ruft sie mit.</para>
    ///
    /// <para><b>Die eine Regel: ein vorhandenes vollständiges Paar wird NIE
    /// überschrieben.</b> „Vollständig" ist, was auch die Engine als Betriebsvorgabe
    /// gelten lässt (<c>ProjektPuffer.IstTemperaturpaar</c>: Rücklauf &gt; 0 und
    /// Vorlauf &gt; Rücklauf). Damit bleibt die Projektänderung des Anwenders stehen —
    /// die zweite Hälfte des Entscheids —, und nur ein FEHLENDES Paar wird ergänzt.</para>
    ///
    /// <para><b>Übertragen wird das PAAR, nicht das einzelne Feld</b> — und darin liegt
    /// der einzige Unterschied zu den drei abgelösten Hüllenzeilen: Sie kopierten
    /// Vorlauf und Rücklauf einzeln und trugen damit auch eine HALBE Angabe („90/0",
    /// im BHKW-Katalog der Testdatenbank 5 von 79 Sätzen) in die Anlagenzeile. Ein
    /// halbes Paar ist keine Betriebsvorgabe: Es taugt weder für die Kesselkette noch
    /// für die Kapazitätsformel, zieht aber über
    /// <c>ProjektPuffer.SQL_SYSTEM_VORLAUF</c> (MIN über <c>Vorlauf &gt; 0</c>) die
    /// Systemvorgabe des Projekts auf einen Wert herunter, dessen Rücklauf niemand
    /// gepflegt hat. Wer die Zahl braucht, trägt sie im Dialog ein — beide Felder
    /// bleiben frei änderbar.</para>
    ///
    /// <para><b>Still gelesen</b> (<see cref="StilleDb"/>, Konzept 13.4): kein Dialog,
    /// keine Meldung, kein Abbruch. Fehlt der Katalogsatz oder die Spalte, bleibt der
    /// Feldsatz, wie er war — die Vorbelegung ist eine Bequemlichkeit, kein
    /// Rechenweg.</para>
    ///
    /// <para><b>Die Wärmepumpe hat keine Katalogtemperaturen.</b> Ihr „Katalog" sind die
    /// Vorlaufstufen der Kennlinien; dafür steht
    /// <see cref="VorlaufAusKennlinien"/>.</para>
    ///
    /// <para><b>Der Solarkollektor hat KEIN Temperaturpaar</b> (Anwenderentscheid
    /// 26.09.2026, „Katalogspalten VL/RL entfernen — keine Funktion"): Der Ertrag rechnet
    /// mit einer festen Speichertemperatur (<c>SimulationSolarthermie.Kollektorfelder_Lesen</c>),
    /// Katalog und Projektkopie führen die Spalten nicht mehr (Schemaschritt
    /// <see cref="SolarkollektorTemperaturen.SCHRITT"/>), und ein an der Anlagenzeile
    /// stehengebliebenes Paar wird überall übergangen, wo es wirken könnte
    /// (<see cref="FuehrtTemperaturpaar"/>).</para>
    /// </summary>
    public static class AnlagenTemperaturen
    {
        // =================================================================================
        // Die vier Abfragen der Katalogtemperaturen und die zwei der Kennlinien
        // =================================================================================
        //
        // AUSGESCHRIEBEN statt ueber einen Tabellennamen zusammengesetzt: So sieht
        // Werkzeuge/SqlDialektPruefer jede Anweisung als Ganzes. Die Spalten heissen in
        // BEIDEN Ebenen "Vorlauf" und "Ruecklauf" OHNE Umlaut - anders als
        // Tab_Energieanlagen.[Rücklauf], das ihn fuehrt (Befund B0-4).

        private const string SQL_STAMM_BHKW =
            "SELECT Vorlauf, Ruecklauf FROM Tab_BHKW_STAMM WHERE ID = ?";
        private const string SQL_STAMM_KESSEL =
            "SELECT Vorlauf, Ruecklauf FROM Tab_Heizkessel_STAMM WHERE ID = ?";

        private const string SQL_KOPIE_BHKW =
            "SELECT Vorlauf, Ruecklauf FROM Tab_BHKW WHERE ID = ?";
        private const string SQL_KOPIE_KESSEL =
            "SELECT Vorlauf, Ruecklauf FROM Tab_Heizkessel WHERE ID = ?";

        /// <summary>Die kleinste Vorlaufstufe der PROJEKTKOPIE eines Wärmepumpengeräts.</summary>
        private const string SQL_STUFE_PROJEKT =
            "SELECT MIN(Vorlauf) FROM Tab_Kenndaten WHERE ID_WP = ? AND Vorlauf > 0";

        /// <summary>Dieselbe Frage an den Stammkatalog.</summary>
        private const string SQL_STUFE_STAMM =
            "SELECT MIN(Vorlauf) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ? AND Vorlauf > 0";

        // =================================================================================
        // Die drei Wege und die Frage nach dem Paar
        // =================================================================================

        /// <summary>
        /// <b>Führt dieser Anlagentyp ein Temperaturpaar?</b> Nein nur beim
        /// Solarkollektor (<c>SOLAR_TYP</c>, <c>REF_SOLAR_TYP</c>): Seine Vor- und
        /// Rücklauftemperatur hat keinen Rechenweg. Die Leser der Anlagenzeile
        /// (Systemvorgabe neuer Puffer, Erzeugerkarte, Hydraulikbild und Warnregel W3)
        /// fragen hier, damit ein aus älteren Ständen stehengebliebenes Paar nichts
        /// bewirkt.
        /// </summary>
        public static bool FuehrtTemperaturpaar(int idType)
        {
            return idType != WizardItemClass.SOLAR_TYP && idType != WizardItemClass.REF_SOLAR_TYP;
        }

        /// <summary>
        /// <b>Beim Aufnehmen aus dem Katalog</b>: das Paar des STAMMSATZES
        /// <paramref name="stammId"/> in den Feldsatz, wenn dieser noch kein
        /// vollständiges Paar trägt.
        ///
        /// <para>Die Tabelle ergibt sich aus <c>item.ID_Type</c> — BHKW und Heizkessel
        /// (auch als Referenzanlage). Jeder andere Typ führt im Katalog keine
        /// Temperaturen, auch der Solarkollektor nicht; für ihn tut die Methode
        /// nichts.</para>
        /// </summary>
        /// <returns><c>true</c>, wenn ein Paar gesetzt wurde.</returns>
        public static bool AusStammsatz(WErzeugerModel item, int stammId)
        {
            if (item == null || stammId <= 0) return false;
            if (ProjektPuffer.IstTemperaturpaar(item.Vorlauf, item.Ruecklauf)) return false;

            if (item.ID_Type == WizardItemClass.BHKW_TYP)
                return PaarUebernehmen(item, SQL_STAMM_BHKW, stammId);

            if (AnlagenSql.CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP))
                return PaarUebernehmen(item, SQL_STAMM_KESSEL, stammId);

            return false;
        }

        /// <summary>
        /// <b>Im Schreibweg</b>: dasselbe Paar aus der PROJEKTKOPIE des Geräts, über den
        /// Fremdschlüssel des Feldsatzes (<c>ID_BHKW</c>, <c>ID_Kessel</c>).
        ///
        /// <para><b>Warum die Kopie und nicht der Stammsatz.</b> An dieser Stelle ist die
        /// Gerätekopie bereits angelegt (<c>CopyFromStamm</c> hat sie eben aufgelöst);
        /// sie trägt die Temperaturen des Katalogs mit und ist zugleich das, was der
        /// Anwender im Gerätedialog pflegt. Der Fremdschlüssel wird ohne Projektfilter
        /// gelesen — er IST der Primärschlüssel der Kopie, und der Aufrufer hat ihn eine
        /// Zeile zuvor aufgelöst (dieselbe Bauart wie
        /// <c>WaermepumpeGeraeteCtrl.GeraetedatenFuellen</c>).</para>
        /// </summary>
        /// <returns><c>true</c>, wenn ein Paar gesetzt wurde.</returns>
        public static bool AusGeraetekopie(WErzeugerModel item)
        {
            if (item == null) return false;
            if (ProjektPuffer.IstTemperaturpaar(item.Vorlauf, item.Ruecklauf)) return false;

            if (item.ID_Type == WizardItemClass.BHKW_TYP)
                return item.ID_BHKW > 0 && PaarUebernehmen(item, SQL_KOPIE_BHKW, item.ID_BHKW);

            if (AnlagenSql.CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP))
                return item.ID_Kessel > 0 && PaarUebernehmen(item, SQL_KOPIE_KESSEL, item.ID_Kessel);

            return false;
        }

        /// <summary>
        /// <b>Die Wärmepumpe</b>: Ist <c>item.Vorlauf</c> noch 0, wird die KLEINSTE
        /// Vorlaufstufe der Kennlinien des Geräts eingesetzt — Projektkopie
        /// (<c>Tab_Kenndaten</c>) vor Stammkatalog (<c>Tab_Kenndaten_STAMM</c>), wie es
        /// <c>WaermepumpeGeraeteCtrl.GeraetedatenFuellen</c> für die Stammfelder tut.
        /// Ein bereits gesetzter Vorlauf bleibt stehen.
        ///
        /// <para><b>Der RÜCKLAUF bleibt unberührt.</b> Für ihn gibt es im Bestand keine
        /// eindeutige Regel: <c>WaermepumpeAnlageDialog.RuecklaufVorschlaege</c> ist eine
        /// FESTE Liste üblicher Werte (20…45 °C) ohne jeden Bezug zur gewählten
        /// Vorlaufstufe, und die Kennlinientabellen führen keinen Rücklauf. Eine
        /// Vorbelegung wäre hier eine erfundene Zahl.</para>
        /// </summary>
        /// <returns><c>true</c>, wenn ein Vorlauf gesetzt wurde.</returns>
        public static bool VorlaufAusKennlinien(WErzeugerModel item)
        {
            if (item == null || item.Vorlauf > 0) return false;
            if (item.ID_WP <= 0) return false;

            int stufe = KleinsteVorlaufstufe(SQL_STUFE_PROJEKT, item.ID_WP);
            if (stufe <= 0) stufe = KleinsteVorlaufstufe(SQL_STUFE_STAMM, item.ID_WP);
            if (stufe <= 0) return false;

            item.Vorlauf = stufe;
            return true;
        }

        // =================================================================================
        // Die Vorbelegung im Projektdialog (Anwenderauftrag 30.09.2026)
        // =================================================================================
        //
        // „Dialogfelder Vorlauf und Rücklauftemperatur übersichtlicher und Vorbelegung,
        // falls 0, mit sinnvollen Vorgaben." Der Dialog zeigt bei einem unvollständigen
        // Paar das Paar, mit dem die Simulation OHNE Eintrag rechnet, und die Hülle legt es
        // in den Feldsatz — mit OK wird es gespeichert. Die Regel steht hier EINMAL; die
        // Zahlen sind die der Simulation und werden referenziert, nicht abgeschrieben.

        /// <summary>
        /// Vorlauf der Vorgabe eines Heizkessels [°C] — der Rückfall der Simulation
        /// (<see cref="SimulationControl.KESSEL_VORLAUF_RUECKFALL"/>, 70 °C).
        /// </summary>
        public const int KESSEL_VORLAUF_VORGABE = (int)SimulationControl.KESSEL_VORLAUF_RUECKFALL;

        /// <summary>
        /// Rücklauf der Vorgabe eines Heizkessels [°C] — der Rückfall der Simulation
        /// (<see cref="SimulationControl.KESSEL_RUECKLAUF_RUECKFALL"/>, 50 °C), zugleich der
        /// Rückfall der Brennwertkennlinie (<see cref="Kesselkennlinie.RUECKLAUF_RUECKFALL_C"/>).
        /// </summary>
        public const int KESSEL_RUECKLAUF_VORGABE = (int)SimulationControl.KESSEL_RUECKLAUF_RUECKFALL;

        /// <summary>
        /// Spreizung [K], mit der ein fehlender Rücklauf der WÄRMEPUMPE aus ihrem Vorlauf
        /// vorbelegt wird — die generische Rückfall-Spreizung des Laufs für einen Speicher
        /// ohne gepflegtes Paar (<see cref="Warnkriterien.RUECKFALL_DELTA_T"/>, dieselben
        /// 10 K wie in <c>SimulationPufferspeicher.Init</c>).
        ///
        /// <para><b>Warum diese Zahl.</b> Für den Rücklauf der Wärmepumpe gibt es im Kern
        /// keinen Rechenweg (die Kennlinienwahl liest nur den Vorlauf) und damit auch keine
        /// Rückfallregel. Das Haus kennt genau eine Spreizung für „kein Paar gepflegt" —
        /// diese. Ein Puffer, der sein Paar später aus der Systemvorgabe des Projekts erbt
        /// (<c>PufferSpCtrl.SystemRuecklauf</c>), bekommt damit dieselbe nutzbare
        /// Kapazität, mit der der Lauf ihn ohne Paar ohnehin rechnen würde.</para>
        /// </summary>
        public const int WAERMEPUMPE_SPREIZUNG_VORGABE_K = (int)Warnkriterien.RUECKFALL_DELTA_T;

        /// <summary>Woher das Temperaturpaar einer Kesselzeile im Dialog stammt.</summary>
        public enum PaarHerkunft
        {
            /// <summary>
            /// Die Anlagenzeile trägt beide Werte (&gt; 0) — nichts wird vorbelegt, auch
            /// nicht bei einem vertauschten Paar: Das ist eine Eingabe des Anwenders.
            /// </summary>
            Anlage,

            /// <summary>Der Kesseldatensatz (Projektkopie bzw. Katalogsatz) trägt ein vollständiges Paar.</summary>
            Geraet,

            /// <summary>Weder Anlage noch Kessel tragen ein Paar — es gilt die Vorgabe 70/50 °C.</summary>
            Vorgabe,

            /// <summary>
            /// Der Temperaturbezug der Anlage steht auf „Fest" (<see cref="DbWerte.WQ_TEMPMODUS_FEST"/>),
            /// und die Zeile trägt kein vollständiges Paar — es wird NICHT vorbelegt
            /// (Anwenderentscheid 30.09.2026). Ohne Paar rechnet die Simulation in diesem Modus
            /// über den Weg „Berechnet" (erst das Paar des Senkenspeichers, dann 70/50 °C) und
            /// meldet es; ein gespeichertes 70/50 machte daraus still eine feste Vorgabe, die
            /// niemand gemacht hat, und änderte das Ergebnis. Die Felder bleiben leer, die
            /// Herleitungszeile bittet um ein Paar.
            /// </summary>
            Fest
        }

        /// <summary>Das Paar, das der Dialog zeigt, und seine Herkunft.</summary>
        public readonly struct PaarVorbelegung
        {
            public PaarVorbelegung(int? vorlauf, int? ruecklauf, PaarHerkunft herkunft)
            {
                Vorlauf = vorlauf;
                Ruecklauf = ruecklauf;
                Herkunft = herkunft;
            }

            /// <summary>Vorlauf [°C]; bei <see cref="PaarHerkunft.Anlage"/> der Wert der Zeile.</summary>
            public int? Vorlauf { get; }

            /// <summary>Rücklauf [°C]; bei <see cref="PaarHerkunft.Anlage"/> der Wert der Zeile.</summary>
            public int? Ruecklauf { get; }

            /// <summary>Woher das Paar stammt.</summary>
            public PaarHerkunft Herkunft { get; }

            /// <summary>Wurde vorbelegt — trägt die Zeile jetzt etwas, das der Anwender nicht eingegeben hat?</summary>
            public bool Vorbelegt => Herkunft == PaarHerkunft.Geraet || Herkunft == PaarHerkunft.Vorgabe;
        }

        /// <summary>
        /// <b>Ist das Paar einer Anlagenzeile unvollständig</b> — steht in einem der beiden
        /// Felder 0 oder nichts? Nur dann wird vorbelegt. Ein vollständig eingetragenes,
        /// aber vertauschtes Paar (50/70) ist KEIN Fall der Vorbelegung: Es ist eine Eingabe,
        /// und die überschreibt niemand still.
        /// </summary>
        public static bool PaarUnvollstaendig(int? vorlauf, int? ruecklauf)
            => !(vorlauf > 0) || !(ruecklauf > 0);

        /// <summary>
        /// <b>Die Vorbelegung eines Heizkessels</b> — rein, ohne Datenbank. Die Kette ist die
        /// der Simulation ohne Eintrag (<c>SimulationControl.KesselTemperaturpaarGepflegt</c>
        /// und ihr Rückfall): erst das Paar der ANLAGE, dann das des KESSELS
        /// (<c>Tab_Heizkessel</c> über <c>ID_Kessel</c>), sonst die Vorgabe
        /// <see cref="KESSEL_VORLAUF_VORGABE"/>/<see cref="KESSEL_RUECKLAUF_VORGABE"/>.
        /// Vollständig ist ein Kesselpaar nach <c>ProjektPuffer.IstTemperaturpaar</c>, wie
        /// in der Simulation. Steht der Temperaturbezug auf „Fest" (<paramref name="festerBezug"/>),
        /// bleibt ein unvollständiges Paar, wie es ist (<see cref="PaarHerkunft.Fest"/>).
        /// </summary>
        public static PaarVorbelegung KesselPaar(int? anlageVorlauf, int? anlageRuecklauf,
                                                 int? kesselVorlauf, int? kesselRuecklauf,
                                                 bool festerBezug = false)
        {
            if (!PaarUnvollstaendig(anlageVorlauf, anlageRuecklauf))
                return new PaarVorbelegung(anlageVorlauf, anlageRuecklauf, PaarHerkunft.Anlage);

            if (festerBezug)
                return new PaarVorbelegung(anlageVorlauf, anlageRuecklauf, PaarHerkunft.Fest);

            if (ProjektPuffer.IstTemperaturpaar(kesselVorlauf, kesselRuecklauf))
                return new PaarVorbelegung(kesselVorlauf, kesselRuecklauf, PaarHerkunft.Geraet);

            return new PaarVorbelegung(KESSEL_VORLAUF_VORGABE, KESSEL_RUECKLAUF_VORGABE, PaarHerkunft.Vorgabe);
        }

        /// <summary>
        /// <b>Belegt das Paar einer Kesselzeile vor</b> (<see cref="KesselPaar"/>) und legt
        /// es in den Feldsatz. Der Kessel wird über <c>ID_Kessel</c> gelesen — still, wie die
        /// übrigen Wege dieser Klasse.
        /// </summary>
        /// <param name="item">Die Anlagenzeile; nur Heizkessel (auch als Referenzanlage).</param>
        /// <param name="stammverweis">
        /// <c>true</c>, wenn <c>ID_Kessel</c> noch auf den KATALOGSATZ zeigt — im Assistenten
        /// bei einer frisch aufgenommenen Zeile, deren Projektkopie erst beim Speichern
        /// entsteht. Sonst zeigt er auf die Projektkopie in <c>Tab_Heizkessel</c>.
        /// </param>
        public static PaarVorbelegung KesselPaarVorbelegen(WErzeugerModel item, bool stammverweis)
        {
            if (item == null ||
                !AnlagenSql.CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP))
                return new PaarVorbelegung(item?.Vorlauf, item?.Ruecklauf, PaarHerkunft.Anlage);

            if (!PaarUnvollstaendig(item.Vorlauf, item.Ruecklauf))
                return new PaarVorbelegung(item.Vorlauf, item.Ruecklauf, PaarHerkunft.Anlage);

            int? kv = null, kr = null;
            if (item.ID_Kessel > 0)
            {
                DataTable dt = StilleDb.Tabelle(stammverweis ? SQL_STAMM_KESSEL : SQL_KOPIE_KESSEL,
                                                StilleDb.Par("@id", DbParamTyp.Integer, item.ID_Kessel));
                if (dt != null && dt.Rows.Count > 0)
                {
                    kv = StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "Vorlauf"));
                    kr = StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "Ruecklauf"));
                }
            }

            PaarVorbelegung p = KesselPaar(item.Vorlauf, item.Ruecklauf, kv, kr, FesterBezug(item, stammverweis));
            if (!p.Vorbelegt) return p;
            item.Vorlauf = p.Vorlauf ?? 0;
            item.Ruecklauf = p.Ruecklauf ?? 0;
            return p;
        }

        /// <summary>
        /// Steht der Temperaturbezug einer GESPEICHERTEN Anlage auf „Fest"? Gelesen wie in der
        /// Simulation (<c>SimulationControl.KesselKopplungSetzen</c>: Spalte
        /// <see cref="SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS"/>, ausgewertet mit
        /// <see cref="DbWerte.TemperaturModusOderDefault"/>). Eine frisch aufgenommene Zeile
        /// hat noch keinen Satz und damit den Vorgabemodus „Berechnet".
        /// </summary>
        private static bool FesterBezug(WErzeugerModel item, bool stammverweis)
        {
            if (stammverweis || item.ID <= 0 || item.ID >= WizardItemClass.ID_UNGESPEICHERT_START) return false;
            string modus = DbWerte.TemperaturModusOderDefault(
                WaermequelleClass.WertLesenStill(item.ID, SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS));
            return string.Equals(modus, DbWerte.WQ_TEMPMODUS_FEST, System.StringComparison.Ordinal);
        }

        /// <summary>
        /// Die HERLEITUNGSZEILE unter dem Paar — fertig formuliert; leer, wenn nichts
        /// vorbelegt wurde. Im Modus „Fest" ohne Paar bittet sie um ein Paar.
        /// </summary>
        public static string Herleitung(PaarVorbelegung p)
        {
            switch (p.Herkunft)
            {
                case PaarHerkunft.Geraet:
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ANL_TEMP_AUS_KESSEL,
                                         p.Vorlauf, p.Ruecklauf);
                case PaarHerkunft.Vorgabe:
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ANL_TEMP_VORGABE_KESSEL,
                                         p.Vorlauf, p.Ruecklauf);
                case PaarHerkunft.Fest:
                    return MyResource.Resource.ANL_TEMP_FEST_OHNE_PAAR;
                default:
                    return "";
            }
        }

        /// <summary>
        /// <b>Der vorbelegte Rücklauf einer Wärmepumpe</b> — rein, ohne Datenbank:
        /// Vorlauf − <see cref="WAERMEPUMPE_SPREIZUNG_VORGABE_K"/>, wenn der Rücklauf 0 oder
        /// leer ist. <c>null</c> heißt „nichts vorbelegen": Der Rücklauf ist eingetragen
        /// (auch ein unpassender — die Prüfung des Dialogs meldet ihn), oder es gibt keinen
        /// Vorlauf, aus dem er folgen könnte.
        /// </summary>
        public static int? WaermepumpeRuecklaufVorgabe(int? vorlauf, int? ruecklauf)
        {
            if (ruecklauf > 0) return null;
            if (!(vorlauf > WAERMEPUMPE_SPREIZUNG_VORGABE_K)) return null;
            return vorlauf.Value - WAERMEPUMPE_SPREIZUNG_VORGABE_K;
        }

        /// <summary>Die Herleitungszeile zum vorbelegten Rücklauf einer Wärmepumpe — fertig formuliert.</summary>
        public static string WaermepumpeRuecklaufHerleitung(int vorlauf, int ruecklauf)
            => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.ANL_TEMP_VORGABE_WP_RUECKLAUF,
                             ruecklauf, vorlauf, WAERMEPUMPE_SPREIZUNG_VORGABE_K);

        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        /// <summary>
        /// Liest <c>Vorlauf</c>/<c>Ruecklauf</c> einer Zeile und setzt sie in den
        /// Feldsatz — aber nur als VOLLSTÄNDIGES Paar. Eine halbe Angabe (nur Vorlauf;
        /// im Bestand mehrfach vorhanden, etwa „90/0") ist als Betriebsvorgabe wertlos
        /// und sähe an der Anlagenzeile gepflegt aus, ohne es zu sein.
        /// </summary>
        private static bool PaarUebernehmen(WErzeugerModel item, string sql, int id)
        {
            DataTable dt = StilleDb.Tabelle(sql, StilleDb.Par("@id", DbParamTyp.Integer, id));
            if (dt == null || dt.Rows.Count == 0) return false;

            int v = StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "Vorlauf"));
            int r = StilleDb.Zahl(StilleDb.Feld(dt.Rows[0], "Ruecklauf"));
            if (!ProjektPuffer.IstTemperaturpaar(v, r)) return false;

            item.Vorlauf = v;
            item.Ruecklauf = r;
            return true;
        }

        /// <summary>
        /// Die kleinste Vorlaufstufe eines Geräts; 0, wenn die Tabelle für dieses Gerät
        /// keine Zeile führt.
        /// </summary>
        private static int KleinsteVorlaufstufe(string sql, int idWp)
        {
            return StilleDb.Zahl(StilleDb.Scalar(sql, StilleDb.Par("@id", DbParamTyp.Integer, idWp)));
        }
    }
}
