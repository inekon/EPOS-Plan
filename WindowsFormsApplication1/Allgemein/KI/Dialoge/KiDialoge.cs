using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der gefuellte Dialogkatalog: die vier Startmasken der Etappe 3b
    /// (Fachkonzept 11.3/11.6, Umsetzungskonzept Paket F3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum hier und nicht im Kern.</b> Nur an dieser Stelle darf Wissen ueber die
    /// Masken stehen (Fachkonzept 3.7); <c>KiKern</c> haelt die Verwaltung und die
    /// Bauartsperre gegen Loeschknoepfe, kennt aber kein einziges Control. Dieselbe
    /// Arbeitsteilung wie zwischen <see cref="KiRegister"/> und <see cref="KiAktionen"/>.
    /// </para>
    /// <para>
    /// <b>Der Feldumfang ist nicht frei gewaehlt.</b> Aufgenommen ist je Maske GENAU das,
    /// was ihre Knopfpruefung erfasst - also die Felder, die durch
    /// <c>Program.ZahlPruefen</c>/<c>Program.GanzzahlPruefen</c> laufen (Fachkonzept 11.6:
    /// „Feldumfang v1 = die von der Knopfpruefung erfassten Eingabefelder"). Das ist der
    /// Grund, warum drei der vier Masken so wenige Felder fuehren: Ihre uebrigen Felder
    /// werden beim Speichern still geparst (<c>double.TryParse</c>,
    /// <c>Program.GanzzahlParsen</c>) und sind damit noch nicht auf das Knopfmuster
    /// umgestellt. Fachkonzept 11.7 haelt genau dafuer fest: solche Felder kommen erst
    /// NACH ihrer Umstellung in den Katalog - die Umstellung selbst ist Bestandspflege
    /// ausserhalb dieses Konzepts. Ein Feld hier aufzunehmen, dessen Wert beim Speichern
    /// stillschweigend verworfen wird, waere die schlechtere Wahl: Der Assistent zeigte
    /// eine Wirkung an, die es nicht gibt.
    /// </para>
    /// <para>
    /// <b>Die Knopfliste ist eine Positivliste.</b> Aufgenommen sind nur Knoepfe, die die
    /// Eingaben der Maske verarbeiten oder sie verwerfen. Ausdruecklich NICHT aufgenommen:
    /// Loeschknoepfe (die weist schon <see cref="KiDialogKnopf"/> per Bauart ab), Knoepfe,
    /// die eine weitere Maske oeffnen (<c>btn_Bearbeiten</c>, <c>btn_Kenndaten</c>,
    /// <c>btn_Katalog</c>), Knoepfe, die eine Auswahlliste veraendern
    /// (<c>btn__Hinzu</c>, <c>btn__Entfernen</c>) und <c>btn_Neu</c> der Waermepumpenmaske,
    /// der die Eingabefelder ohne Rueckfrage ueberschreibt.
    /// </para>
    /// <para>
    /// <b>Kein Feld traegt einen Hilfe-Slug.</b> Die Zuordnung Control -&gt; Slug liest
    /// <c>HelpExtender.RegisterControl</c> aus <c>help_mapping.txt</c>
    /// (<c>Allgemein\Hilfe\HelpCatalog.cs:254</c>); diese Datei liegt nicht im Repository,
    /// und es gibt im ganzen Baum keinen Aufruf von <c>SetHelpKey</c>. Es ist also fuer
    /// keines dieser Felder ein Slug nachweisbar. Der Weg dorthin steht trotzdem
    /// (<c>KiAktionenDialog</c> fragt <c>Program.HelpCatalog.Get</c>, sobald ein Slug
    /// deklariert ist) - deklariert wird aber nur, was belegt ist.
    /// </para>
    /// </remarks>
    internal static class KiDialoge
    {
        private static KiDialogKatalog _katalog;
        private static readonly object _sperre = new object();

        /// <summary>
        /// Der Katalog dieser Sitzung - einmal gebaut, dann fest.
        /// </summary>
        /// <remarks>
        /// Dieselbe Bauart wie <c>KiAusfuehrer.Register</c>: Der Katalog entsteht beim
        /// ersten Zugriff und wird danach nur noch gelesen. Ein Katalog, dem zur Laufzeit
        /// eine Maske zuwachsen koennte, waere genau der Weg, auf dem eine nicht
        /// freigegebene Maske doch noch steuerbar wuerde (<see cref="KiDialogKatalog"/>).
        /// </remarks>
        internal static KiDialogKatalog Katalog
        {
            get
            {
                if (_katalog != null) return _katalog;
                lock (_sperre)
                {
                    if (_katalog == null) _katalog = Erzeuge();
                }
                return _katalog;
            }
        }

        /// <summary>Baut den vollstaendigen Katalog.</summary>
        internal static KiDialogKatalog Erzeuge()
        {
            return new KiDialogKatalog(
                Heizkessel(),
                Photovoltaik(),
                Pufferspeicher(),
                Waermepumpe());
        }

        // =====================================================================
        // Form_Heizkessel_Bearbeiten
        // =====================================================================

        /// <summary>
        /// Heizkessel bearbeiten - die 15 Felder aus
        /// <c>Views\Heizkessel\Form_Heizkessel_Bearbeiten.cs:532-546</c>
        /// (<c>EingabenPruefen</c>) und die vier Aktionsknoepfe.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Knopfposition ist gemessen, nicht geschaetzt.</b> Die Maske fuehrt oben
        /// rechts eine SENKRECHTE Knopfleiste: <c>btn_Ueberschreiben</c> (616/19),
        /// <c>btn_Speichern_Unter</c> (616/59), <c>btn_Speichern</c> (616/98),
        /// <c>btn_Abbrechen</c> (616/137), jeweils 105x31 - der Streifen x 616..721 ist
        /// also von y 19 bis y 168 durchgehend belegt (Masse aus
        /// <c>Form_Heizkessel_Bearbeiten.resx</c>, Client 744x589). Der Regelplatz 8/8 des
        /// Aufrufknopfs liegt mitten auf <c>btn_Ueberschreiben</c>; auch ein Platz bei
        /// AbstandOben 55 laege noch auf <c>btn_Speichern_Unter</c> (59..90). Der Knopf
        /// geht deshalb UNTER die Leiste: 176 = Unterkante 168 plus die uebliche Luft von
        /// 8. Darunter beginnt erst bei y 416 wieder etwas (<c>groupBox5</c>, x 536..732),
        /// und links davon endet die breiteste Rubrik bei x 576.
        /// </para>
        /// <para>
        /// <b>Wartungskosten entsteht zur Laufzeit</b> (<c>WartungsfeldAufbauen</c>,
        /// <c>:121</c>) und steht deshalb in keiner Designer-Datei. Fuer den Katalog macht
        /// das keinen Unterschied - aufgeloest wird ueber den Controlnamen in der
        /// aufgebauten Maske, und <c>EingabenPruefen</c> prueft das Feld wie jedes andere.
        /// </para>
        /// </remarks>
        private static KiDialog Heizkessel()
        {
            return new KiDialog(
                maskenname: "Form_Heizkessel_Bearbeiten",
                anzeigename: KiDialogTexte.MaskeHeizkessel,
                felder: new[]
                {
                    new KiDialogFeld("th_leistung", "tb_th_Leistung",
                                     KiDialogTexte.HkLeistungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkLeistungErl,
                                     einheit: KiDialogTexte.EINHEIT_KW, leerErlaubt: true),
                    new KiDialogFeld("wirkungsgrad_gas", "tb_Wirkungsgrad",
                                     KiDialogTexte.HkWgGasName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWgGasErl,
                                     leerErlaubt: true),

                    // Der Controlname traegt ein „Ö" - der Bestand fuehrt hier einen
                    // Nicht-ASCII-Bezeichner (Umsetzungskonzept 3b, Bestandsanker B9).
                    // KiControlpfad laesst das ausdruecklich zu; die Aufloesung vergleicht
                    // ohne Ruecksicht auf Gross-/Kleinschreibung, aber zeichengenau.
                    new KiDialogFeld("wirkungsgrad_oel", "tb_Wirkungsgrad_Öl",
                                     KiDialogTexte.HkWgOelName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWgOelErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("bereitschaftsverlust", "tb_B_Verlust",
                                     KiDialogTexte.HkBbVerlustName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkBbVerlustErl,
                                     einheit: KiDialogTexte.EINHEIT_PROZENT, leerErlaubt: true),
                    new KiDialogFeld("investitionskosten", "tb_Investitionskosten",
                                     KiDialogTexte.HkInvestName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkInvestErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: true),
                    new KiDialogFeld("wartungskosten", "tb_Wartungskosten",
                                     KiDialogTexte.HkWartungName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkWartungErl,
                                     leerErlaubt: true),
                    new KiDialogFeld("raumbedarf", "tb_Raumbedarf",
                                     KiDialogTexte.HkRaumbedarfName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkRaumbedarfErl,
                                     einheit: KiDialogTexte.EINHEIT_M3, leerErlaubt: true),
                    new KiDialogFeld("nutzungsdauer", "tb_Nutzungsdauer",
                                     KiDialogTexte.HkNutzungsdauerName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkNutzungsdauerErl,
                                     einheit: KiDialogTexte.EinheitJahre, leerErlaubt: true),
                    new KiDialogFeld("co2", "tb_CO2",
                                     KiDialogTexte.HkCo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkCo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("so2", "tb_SO2",
                                     KiDialogTexte.HkSo2Name, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkSo2Erl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("nox", "tb_NOx",
                                     KiDialogTexte.HkNoxName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkNoxErl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("co", "tb_CO",
                                     KiDialogTexte.HkCoName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkCoErl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("staub", "tb_Staub",
                                     KiDialogTexte.HkStaubName, KiParameterTyp.Zahl,
                                     KiDialogTexte.HkStaubErl,
                                     einheit: KiDialogTexte.EINHEIT_G_MWH, leerErlaubt: true),
                    new KiDialogFeld("vorlauf", "textBox_Vorlauf",
                                     KiDialogTexte.HkVorlaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkVorlaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true),
                    new KiDialogFeld("ruecklauf", "textBox_Ruecklauf",
                                     KiDialogTexte.HkRuecklaufName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.HkRuecklaufErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD_C, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                },
                knopfposition: new KiKnopfposition(abstandRechts: 8, abstandOben: 176));
        }

        // =====================================================================
        // Form_PV
        // =====================================================================

        /// <summary>
        /// Photovoltaik-Module - die drei Felder aus
        /// <c>Views\Photovoltaik\Form_PV.cs:276-278</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Maske fuehrt keinen Speichern-Knopf.</b> <c>btn_Speichern_Click</c>
        /// (<c>:268</c>) traegt die umgestellte Knopfpruefung, ist aber an KEIN Control
        /// gebunden - <c>Form_PV.Designer.cs</c> verdrahtet nur
        /// <c>btn__Hinzu</c>, <c>btn__Entfernen</c>, <c>btn_Abbrechen</c>, <c>btn_OK</c>,
        /// <c>btn_Bearbeiten</c> und <c>btn_Löschen</c>. Uebernommen werden die drei Felder
        /// im Bestand ueber <c>panel1_Leave</c> -&gt; <c>UpdateProerties</c> (<c>:132</c>,
        /// stille Parser), abgeschlossen wird die Maske mit <c>btn_OK</c>. Der Katalog
        /// deklariert deshalb nur Knoepfe, die es wirklich gibt.
        /// </para>
        /// <para>
        /// <b>Knopfposition wegen des Kopfbandes.</b> <c>label_Type</c> liegt bei 0/0 und
        /// ist 762x31 gross; der Regelplatz 8/8 laege darauf. Direkt darunter beginnt bei
        /// y 59 schon <c>listBox_DB</c> (x 449..746), ein Platz bei AbstandOben 39 schnitte
        /// ihre obere rechte Ecke an. Bleiben genau die 27 Bildpunkte dazwischen:
        /// AbstandOben 33 setzt den 24 Punkte hohen Knopf mit je zwei Punkten Luft
        /// zwischen Kopfband und Liste (Masse aus <c>Form_PV.Designer.cs</c>,
        /// Client 762x582).
        /// </para>
        /// </remarks>
        private static KiDialog Photovoltaik()
        {
            return new KiDialog(
                maskenname: "Form_PV",
                anzeigename: KiDialogTexte.MaskePv,
                felder: new[]
                {
                    new KiDialogFeld("neigung", "textBox_Neigung",
                                     KiDialogTexte.PvNeigungName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvNeigungErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("azimut", "textBox_Azimut",
                                     KiDialogTexte.PvAzimutName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PvAzimutErl,
                                     einheit: KiDialogTexte.EINHEIT_GRAD, leerErlaubt: true),
                    new KiDialogFeld("anzahl_module", "textBox_AnlagenLeistung",
                                     KiDialogTexte.PvAnzahlName, KiParameterTyp.Zahl,
                                     KiDialogTexte.PvAnzahlErl,
                                     leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("ok", "btn_OK", KiDialogTexte.KnopfOk),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                },
                knopfposition: new KiKnopfposition(abstandRechts: 8, abstandOben: 33));
        }

        // =====================================================================
        // Form_PufferSp_Bearbeiten
        // =====================================================================

        /// <summary>
        /// Pufferspeicher bearbeiten - das eine Feld aus
        /// <c>Views\Pufferspeicher\Form_PufferSp_Bearbeiten.cs:261</c>
        /// (<c>VolumenPruefen</c>) und die vier Aktionsknoepfe.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nur ein Feld - und das ist kein Versehen.</b> Die uebrigen Eingaben der Maske
        /// (Hersteller, Speichertyp, Bereitschaftsverluste, Investitionskosten) uebernimmt
        /// <c>InitDatensatzUpdate</c> (<c>:269</c>) ueber <c>double.TryParse</c> ohne jede
        /// Meldung: ein unlesbarer Text wird dort stillschweigend zu 0. Solche Felder
        /// gehoeren nach Fachkonzept 11.7 erst nach ihrer Umstellung in den Katalog.
        /// </para>
        /// <para>
        /// <b>Keine Knopfposition noetig.</b> Die Knopfleiste beginnt bei x 502 und y 35,
        /// <c>groupBox1</c> endet bei x 487 - der Regelplatz 8/8 (bei Client 619x355 also
        /// x 568..611, y 8..32) bleibt in jedem Fall frei, auch bei der breiteren
        /// Beschriftung des Hilfe-Betriebs. Masse aus
        /// <c>Form_PufferSp_Bearbeiten.resx</c>.
        /// </para>
        /// </remarks>
        private static KiDialog Pufferspeicher()
        {
            return new KiDialog(
                maskenname: "Form_PufferSp_Bearbeiten",
                anzeigename: KiDialogTexte.MaskePufferSp,
                felder: new[]
                {
                    new KiDialogFeld("gesamtvolumen", "textBox_Volumen",
                                     KiDialogTexte.PspVolumenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.PspVolumenErl,
                                     einheit: KiDialogTexte.EINHEIT_LITER, leerErlaubt: true)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("speichern_unter", "btn_Speichern_Unter",
                                      KiDialogTexte.KnopfSpeichernUnter),
                    new KiDialogKnopf("ueberschreiben", "btn_Ueberschreiben",
                                      KiDialogTexte.KnopfUeberschreiben),
                    new KiDialogKnopf("abbrechen", "btn_Abbrechen", KiDialogTexte.KnopfAbbrechen)
                });
        }

        // =====================================================================
        // Form_WP
        // =====================================================================

        /// <summary>
        /// Waermepumpen verwalten - das eine Feld aus
        /// <c>Views\Wärmepumpe\Form_WP.cs:324</c> und die zwei Aktionsknoepfe.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nur ein Feld.</b> <c>btn_Speichern_Click</c> prueft ausschliesslich
        /// <c>textBox_Modulkosten</c> (und zwar mit <c>leerErlaubt: false</c>); Nennleistung,
        /// Heizstab und Baujahr laufen daneben ueber <c>Program.GanzzahlParsen</c> und
        /// lassen bei unlesbarem Text stillschweigend den gelesenen Datensatzwert stehen.
        /// Begruendung wie beim Pufferspeicher (Fachkonzept 11.7).
        /// </para>
        /// <para>
        /// <b>Der Abbrechen-Knopf existiert nicht.</b> <c>btn_Abbrechen_Click</c>
        /// (<c>:269</c>) ist wie bei <c>Form_PV</c> ein Ereignisbehandler ohne Control;
        /// <c>Form_WP.Designer.cs</c> verdrahtet <c>btn_Beenden</c> (Beschriftung „OK"),
        /// <c>btn_Neu</c>, <c>btn_Kenndaten</c>, <c>btn_Speichern</c>, <c>btn_Loeschen</c>
        /// und <c>btn_Katalog</c>. Deklariert sind deshalb nur <c>btn_Speichern</c> und
        /// <c>btn_Beenden</c>.
        /// </para>
        /// <para>
        /// <b>Knopfposition wegen des Kopfbandes.</b> <c>label1</c> liegt bei 0/0 und ist
        /// 877x28 gross; AbstandOben 36 setzt den Knopf mit 8 Punkten Luft darunter. Der
        /// Streifen x 826..869 ist dort frei - die naechsten Nachbarn sind <c>label5</c>
        /// („kW", x 820..852) erst bei y 89 und <c>textBox_Name</c> (endet bei x 814).
        /// Masse aus <c>Form_WP.resx</c>, Client 877x642.
        /// </para>
        /// </remarks>
        private static KiDialog Waermepumpe()
        {
            return new KiDialog(
                maskenname: "Form_WP",
                anzeigename: KiDialogTexte.MaskeWp,
                felder: new[]
                {
                    new KiDialogFeld("modulkosten", "textBox_Modulkosten",
                                     KiDialogTexte.WpModulkostenName, KiParameterTyp.Ganzzahl,
                                     KiDialogTexte.WpModulkostenErl,
                                     einheit: KiDialogTexte.EINHEIT_EURO, leerErlaubt: false)
                },
                knoepfe: new[]
                {
                    new KiDialogKnopf("speichern", "btn_Speichern", KiDialogTexte.KnopfSpeichern),
                    new KiDialogKnopf("ok", "btn_Beenden", KiDialogTexte.KnopfOk)
                },
                knopfposition: new KiKnopfposition(abstandRechts: 8, abstandOben: 36));
        }
    }
}
