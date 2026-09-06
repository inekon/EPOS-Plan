using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorbelegung von Vor- und Rücklauf einer Anlagenzeile aus dem Katalog</b>
    /// (Anwenderentscheid <b>W6‑E‑4</b> vom 06.09.2026, wörtlich: „Die Vor- und
    /// Rücklauftemperatur sollen beim Anlegen der Komponenten und Energieerzeuger die
    /// Vor- und Rücklauftemperatur aus dem Katalog übernommen werden. Diese können dann
    /// vom Benutzer für das Projekt geändert werden.").
    ///
    /// <para><b>Warum eine eigene Klasse.</b> Die Vorbelegung stand dreimal in der
    /// Windows-Oberfläche — <c>BhkwHuelle.Aufnehmen</c>, <c>HeizkesselHuelle.Aufnehmen</c>
    /// und <c>SolarkollektorHuelle.Aufnehmen</c> schrieben jeweils
    /// <c>Vorlauf = stamm.…</c> in den Feldsatz. Wer nicht über eine dieser drei Hüllen
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
    /// </summary>
    public static class AnlagenTemperaturen
    {
        // =================================================================================
        // Die sechs Abfragen
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
        private const string SQL_STAMM_SOLAR =
            "SELECT Vorlauf, Ruecklauf FROM Tab_Solarkollektoren_STAMM WHERE ID = ?";

        private const string SQL_KOPIE_BHKW =
            "SELECT Vorlauf, Ruecklauf FROM Tab_BHKW WHERE ID = ?";
        private const string SQL_KOPIE_KESSEL =
            "SELECT Vorlauf, Ruecklauf FROM Tab_Heizkessel WHERE ID = ?";
        private const string SQL_KOPIE_SOLAR =
            "SELECT Vorlauf, Ruecklauf FROM Tab_Solarkollektoren WHERE ID = ?";

        /// <summary>Die kleinste Vorlaufstufe der PROJEKTKOPIE eines Wärmepumpengeräts.</summary>
        private const string SQL_STUFE_PROJEKT =
            "SELECT MIN(Vorlauf) FROM Tab_Kenndaten WHERE ID_WP = ? AND Vorlauf > 0";

        /// <summary>Dieselbe Frage an den Stammkatalog.</summary>
        private const string SQL_STUFE_STAMM =
            "SELECT MIN(Vorlauf) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ? AND Vorlauf > 0";

        // =================================================================================
        // Die drei Wege
        // =================================================================================

        /// <summary>
        /// <b>Beim Aufnehmen aus dem Katalog</b>: das Paar des STAMMSATZES
        /// <paramref name="stammId"/> in den Feldsatz, wenn dieser noch kein
        /// vollständiges Paar trägt.
        ///
        /// <para>Die Tabelle ergibt sich aus <c>item.ID_Type</c> — BHKW, Heizkessel
        /// (auch als Referenzanlage) und Solarkollektor. Jeder andere Typ führt im
        /// Katalog keine Temperaturen; für ihn tut die Methode nichts.</para>
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

            if (AnlagenSql.CheckType(item, WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP))
                return PaarUebernehmen(item, SQL_STAMM_SOLAR, stammId);

            return false;
        }

        /// <summary>
        /// <b>Im Schreibweg</b>: dasselbe Paar aus der PROJEKTKOPIE des Geräts, über den
        /// Fremdschlüssel des Feldsatzes (<c>ID_BHKW</c>, <c>ID_Kessel</c>,
        /// <c>ID_Solar</c>).
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

            if (AnlagenSql.CheckType(item, WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP))
                return item.ID_Solar > 0 && PaarUebernehmen(item, SQL_KOPIE_SOLAR, item.ID_Solar);

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
