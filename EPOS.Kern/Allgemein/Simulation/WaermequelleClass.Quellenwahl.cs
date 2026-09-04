using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was der Anwender in der Quellenwahl entschieden hat — der Satz, der von
    /// <see cref="WaermequelleClass.QuelleSchreiben"/> in <c>Tab_Energieanlagen</c>
    /// geschrieben wird (iU9-W10b.0b).
    ///
    /// <para><b>Warum ein Satz und nicht elf Einzelaufrufe.</b> Der Vorlaeufer
    /// (<c>Form_Simulation_Config.Uebersicht.WqCombo_SelectedIndexChanged</c>:841-1183)
    /// setzte je Zweig zwischen einem und elf Feldern mit einzelnen
    /// <c>WertSchreiben</c>-Aufrufen mitten im Ereignis der Klappliste. Welche Felder zu
    /// welchem Zweig gehoeren — und vor allem, welche ein Zweig bewusst NICHT anfasst
    /// (Befund W10-B15: der Kesselzweig laesst die Verdampferwerte der Waermepumpe
    /// stehen) — war damit ueber 340 Zeilen Oberflaechencode verteilt.</para>
    ///
    /// <para><b>Nur belegte Felder werden geschrieben.</b> Jeder Zweig fasst genau die
    /// Spalten an, die der Vorlaeufer angefasst hat; die uebrigen bleiben unberuehrt.</para>
    /// </summary>
    public sealed class QuelleErgebnis
    {
        /// <summary>Der gewaehlte Steuerwert (<c>WaermequelleClass.TYP_*</c>).</summary>
        public string Typ = "";

        /// <summary>true = die Anlage ist eine Waermepumpe (entscheidet zwei Zweige).</summary>
        public bool IstWaermepumpe;

        // --- TYP_KONSTANT -------------------------------------------------------------

        /// <summary>Konstante Quelltemperatur [°C].</summary>
        public double Temperatur;

        // --- TYP_PUFFER ---------------------------------------------------------------

        /// <summary><c>Tab_Pufferspeicher.ID</c> des Quellspeichers; 0 = keiner.</summary>
        public int IdPuffer;

        /// <summary>Bezeichner des Quellspeichers (Rueckfallkette der Engine).</summary>
        public string Pufferspeicher = "";

        public double Quelltemperatur;
        public double Spreizung;
        public double Regeneration;
        public bool Unbegrenzt;

        /// <summary>Quell-Entnahmehoehe [%]; <c>null</c> = „oben" (der Regelfall).</summary>
        public double? Anschlusshoehe;

        /// <summary>Temperaturbezug der Kessel-Kaskade (<c>DbWerte.WQ_TEMPMODUS_*</c>).</summary>
        public string TemperaturModus = "";

        public int VorlaufAnlage;
        public int RuecklaufAnlage;

        // --- TYP_PROFIL ---------------------------------------------------------------

        /// <summary><c>Tab_Quellprofil.ID</c>; 0 = keins.</summary>
        public int IdQuellprofil;

        // --- TYP_CSV ------------------------------------------------------------------

        /// <summary>Pfad der geprueften CSV-Datei.</summary>
        public string CsvPfad = "";

        // --- TYP_ERDREICH -------------------------------------------------------------

        public string Quellsystem = "";
        public double Tiefe;
        public double Flaeche;
        public int Anzahl;
        public string Bodentyp = "";

        /// <summary>Nutzbare Spreizung der Erdreichquelle [K] (Konzept 13.1).</summary>
        public double SpreizungErdreich;
    }

    public static partial class WaermequelleClass
    {
        /// <summary>
        /// Schreibt die Quellenwahl einer Anlage — der Kern-Ersatz fuer die sechs Zweige
        /// von <c>WqCombo_SelectedIndexChanged</c> (iU9-W10b.0b).
        ///
        /// <para>Die Reihenfolge der Schreibvorgaenge ist woertlich uebernommen: In
        /// jedem Zweig geht <c>WQ_Typ</c> ZULETZT weg. Bricht ein Schreibvorgang davor
        /// ab, steht der Typ noch auf dem alten Wert — die Engine rechnet dann weiter
        /// wie bisher statt mit einer halb geschriebenen neuen Quelle.</para>
        ///
        /// <para><b>Was NICHT hier steht.</b> Die Dialogpruefung
        /// <see cref="WaermesenkeClass.QuellePruefen"/> (Kurzschluss, Kaskadenzyklus)
        /// bleibt beim Aufrufer und laeuft VOR diesem Aufruf — sie soll verhindern, dass
        /// die Konfiguration ueberhaupt entsteht. Ebenso die Klimazone: Sie ist eine
        /// Eigenschaft der REGION, nicht der Anlage
        /// (<c>KlimaregionCtrl.KlimazoneJeProjektSchreiben</c>).</para>
        /// </summary>
        /// <returns><c>false</c>, sobald ein Schreibvorgang fehlschlaegt.</returns>
        public static bool QuelleSchreiben(int idAnlage, QuelleErgebnis e)
        {
            // ACHTUNG: TYP_OHNE ist die LEERE Zeichenkette (DbWerte.WQ_TYP_OHNE) - der
            // regulaere Steuerwert „Systemruecklauf" des Heizkessels. Die Wache prueft
            // deshalb auf null und nicht auf „leer".
            if (idAnlage <= 0 || e == null || e.Typ == null) return false;

            switch (e.Typ)
            {
                case TYP_OHNE:
                    // Heizkessel „Systemruecklauf": die Kaskade wird ABGEBAUT. Mit dem Typ
                    // geht auch der Fremdschluessel weg - ein stehengebliebener
                    // WQ_ID_Puffer waere genau der Altdatenrest aus Befund E-K2-4. NULL
                    // statt 0 wegen der erzwungenen Beziehung aus Schritt 4 der
                    // SchemaMigration.
                    return WertSchreiben(idAnlage, "WQ_Typ", e.Typ)
                         & WertSchreiben(idAnlage, "WQ_ID_Puffer", DbParamTyp.Integer, DBNull.Value);

                case TYP_AUSSENLUFT:
                    return WertSchreiben(idAnlage, "WQ_Typ", e.Typ);

                case TYP_KONSTANT:
                    return WertSchreiben(idAnlage, "WQ_Temp", e.Temperatur)
                         & WertSchreiben(idAnlage, "WQ_Typ", e.Typ);

                case TYP_PUFFER:
                    return PufferSchreiben(idAnlage, e);

                case TYP_PROFIL:
                    // FUEHREND ist der Fremdschluessel; 0 ist keine gueltige Profil-ID,
                    // und die Beziehung FK_Anlage_Quellprofil aus Schritt 54 wiese sie ab.
                    // WQ_Monatswerte/WQ_Wochenwerte werden NICHT geschrieben: Sie sind
                    // Lese-Altlast (Konzept 15) und der Rueckweg.
                    return WertSchreiben(idAnlage, "WQ_ID_Quellprofil", DbParamTyp.Integer,
                                         e.IdQuellprofil > 0 ? (object)e.IdQuellprofil : DBNull.Value)
                         & WertSchreiben(idAnlage, "WQ_Typ", e.Typ);

                case TYP_CSV:
                    return WertSchreiben(idAnlage, "WQ_CSV", e.CsvPfad ?? "")
                         & WertSchreiben(idAnlage, "WQ_Typ", e.Typ);

                case TYP_ERDREICH:
                    return WertSchreiben(idAnlage, "WQ_Quellsystem", e.Quellsystem ?? "")
                         & WertSchreiben(idAnlage, "WQ_Tiefe", e.Tiefe)
                         & WertSchreiben(idAnlage, "WQ_Flaeche", e.Flaeche)
                         & WertSchreiben(idAnlage, "WQ_Anzahl", e.Anzahl)
                         & WertSchreiben(idAnlage, "WQ_Bodentyp", e.Bodentyp ?? "")
                         & WertSchreiben(idAnlage, "WQ_Spreizung", e.SpreizungErdreich)
                         & WertSchreiben(idAnlage, "WQ_Typ", e.Typ);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Der Zweig „Pufferspeicher" — elf Felder, davon vier nur fuer die Waermepumpe
        /// und drei nur fuer den Heizkessel (woertlich :951-1046).
        /// </summary>
        private static bool PufferSchreiben(int idAnlage, QuelleErgebnis e)
        {
            // E0: FUEHREND ist der Fremdschluessel (Ueberladung mit ausdruecklichem
            // DbParamTyp - 0 ist keine gueltige Puffer-ID). Der Bezeichner wird
            // MITGESCHRIEBEN: Anzeigen und die Rueckfallkette der Engine lesen ihn weiter.
            bool ok = WertSchreiben(idAnlage, "WQ_ID_Puffer", DbParamTyp.Integer,
                                    e.IdPuffer > 0 ? (object)e.IdPuffer : DBNull.Value);
            ok &= WertSchreiben(idAnlage, "WQ_Puffer", e.Pufferspeicher ?? "");

            // D5b: Die vier Parameter beschreiben die VERDAMPFERseite und werden
            // ausschliesslich von SimulationWaermepumpe bzw. Quellspeicher gelesen. Der
            // Kessel bezieht seinen Temperaturhub aus dem VORLAUF des Quellpuffers - fuer
            // ihn hat der Dialog die Felder gar nicht gezeigt, und dann darf er sie auch
            // nicht schreiben (Befund W10-B15).
            if (e.IstWaermepumpe)
            {
                ok &= WertSchreiben(idAnlage, "WQ_Temp", e.Quelltemperatur);
                ok &= WertSchreiben(idAnlage, "WQ_Spreizung", e.Spreizung);
                ok &= WertSchreiben(idAnlage, "WQ_Regeneration", e.Regeneration);
                ok &= WertSchreiben(idAnlage, "WQ_Unbegrenzt", e.Unbegrenzt);
            }

            // PAKET Q1: Die Quell-Entnahmehoehe gilt fuer Waermepumpe UND Heizkessel
            // (Konzept 8.4) und steht deshalb ausserhalb des Verdampfer-Blocks. Ueber die
            // Ueberladung mit ausdruecklichem DbParamTyp, weil NULL hier der Regelfall
            // ist („oben") und ACE aus DBNull allein keinen Spaltentyp ableitet.
            double? hoehe = e.Anschlusshoehe;
            ok &= WertSchreiben(idAnlage, "WQ_Anschlusshoehe", DbParamTyp.Double,
                                hoehe.HasValue ? (object)hoehe.Value : DBNull.Value);

            // PAKET B2: Der Temperaturbezug gilt nur fuer den HEIZKESSEL. Das
            // TEMPERATURPAAR geht nur im Modus „fest" weg; bei „berechnet" bleibt ein
            // einmal gepflegtes Paar an der Anlage stehen - es ist dort auch fuer andere
            // Auswertungen die Systemvorgabe (W3, PufferSpCtrl.SystemVorlauf).
            if (!e.IstWaermepumpe)
            {
                ok &= WertSchreiben(idAnlage, SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS,
                                    e.TemperaturModus ?? "");

                if (string.Equals(e.TemperaturModus, DbWerte.WQ_TEMPMODUS_FEST,
                                  StringComparison.Ordinal))
                {
                    ok &= WertSchreiben(idAnlage, "Vorlauf", e.VorlaufAnlage);
                    // Die Spalte traegt an der Datenbank den UMLAUT
                    // (ProjektPuffer.SQL_SYSTEM_RUECKLAUF); WertSchreiben klammert den
                    // Namen, der Zugriff traegt.
                    ok &= WertSchreiben(idAnlage, "Rücklauf", e.RuecklaufAnlage);
                }
            }

            ok &= WertSchreiben(idAnlage, "WQ_Typ", e.Typ);
            return ok;
        }
    }
}
