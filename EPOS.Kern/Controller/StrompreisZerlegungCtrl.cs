using System;
using System.Collections.Generic;
using System.Data;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf die Preiszerlegung „Strompreis Details" und die Verguetungssaetze in
    // energy_project_settings (Fachkonzept Stromspeicher 4.2/4.3; Spalten aus
    // SchemaMigration Schritt 12a und 83).
    //
    // BEDEUTUNG SEIT SP-E-2: Die Anteile ZERLEGEN den Arbeitspreis, sie schlagen nicht
    // mehr auf ihn auf. Der Controller liefert deshalb zwei Summen und keinen
    // "wirksamen Aufschlag" mehr:
    //
    //   AlsPreiszerlegung(m).SummeAktivCtKwh                 gegen den Arbeitspreis
    //   AlsPreiszerlegung(m).SummeAktivOhneCtKwh(BESCHAFFUNG) auf eine Spot-/Profilreihe
    //
    // EIN BAUPLAN FUER BEIDE TRAEGER: Vorsorge, Lesen und Schreiben der
    // (Wert, Aktiv)-Paare, Summe und Rest stehen EINMAL in Preisanteile und werden von
    // diesem Controller wie von BrennstoffBestandteilCtrl benutzt. Der Unterschied
    // bleibt benannt: Preisanteile.Paar (Strom, NULL laesst den Vorschlag stehen)
    // gegen Preisanteile.PaarNullbar (Brennstoff, NULL bleibt null).
    //
    // Durchgaengig NAMENSBASIERT mit Columns.Contains-Wache: Auf einer Datenbank, deren
    // Migration noch nicht durchgelaufen ist, liefert der Controller die Vorbelegung des
    // Modells statt einer Ausnahme - dasselbe Vorgehen wie StromspeicherVarianteCtrl.
    // Die Ordinalkette-Falle von Tab_Einstellungen gibt es hier nicht:
    // energy_project_settings wird im Bestand ausschliesslich ueber SELECT * mit
    // Spaltennamen-Zugriff gelesen.
    //
    // Kulturregel: Es wird nirgends eine Zeichenkette in eine Zahl umgewandelt - die
    // Werte kommen typisiert aus der DataTable.
    // ---------------------------------------------------------------------------
    public class StrompreisZerlegungCtrl
    {
        public const string TABLE = "energy_project_settings";

        /// <summary>Preismodell-Code des Strom-Carriers in <c>pricing_model</c>.</summary>
        public const string PRICING_MODEL_STROM = "ELECTRICITY";

        // --- Sprachneutrale Anteilsschluessel (Schicht 2 der Drei-Schichten-Regel) ---
        //
        // Sie verbinden die Datenbankspalte, den Engine-Satz und den Anzeigetext, ohne
        // selbst Anzeigetext zu sein. Die Beschriftung holt die Oberflaeche ueber
        // MyResource.Resource.PREIS_KOMP_*.

        public const string KOMP_BESCHAFFUNG = "BESCHAFFUNG";
        public const string KOMP_VERTRIEB = "VERTRIEB";
        public const string KOMP_NETZENTGELT = "NETZENTGELT";
        public const string KOMP_STROMSTEUER = "STROMSTEUER";
        public const string KOMP_KONZESSION = "KONZESSION";
        public const string KOMP_UMLAGEN = "UMLAGEN";
        public const string KOMP_UMLAGE_KWKG = "UMLAGE_KWKG";
        public const string KOMP_UMLAGE_OFFSHORE = "UMLAGE_OFFSHORE";
        public const string KOMP_UMLAGE_STROMNEV19 = "UMLAGE_STROMNEV19";

        /// <summary>
        /// Die Anteile in Anzeigereihenfolge - drei Gruppen (Beschaffung und Vertrieb;
        /// Netzentgelte; Steuern, Abgaben und Umlagen). Die drei Einzelumlagen stehen
        /// hier NICHT: Sie treten an die Stelle des Summenfelds, wenn
        /// <c>Umlagen_Einzeln</c> gesetzt ist, und nie neben ihm.
        /// </summary>
        public static readonly string[] KOMPONENTEN =
        {
            KOMP_BESCHAFFUNG, KOMP_VERTRIEB, KOMP_NETZENTGELT,
            KOMP_STROMSTEUER, KOMP_KONZESSION, KOMP_UMLAGEN
        };

        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt die Spalten der Preiszerlegung an, falls die Migration noch nicht
        /// gelaufen ist — die tolerante Rückfallebene, die sich dieser Träger mit dem
        /// Brennstoff teilt (<see cref="Preisanteile.SpaltenSicherstellen"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bewusst OHNE Vorbelegung und OHNE Faltung: Hier entstehen nur die Spalten,
        /// damit ein Lesezugriff nicht scheitert; die Leseseite fällt dann auf die
        /// Vorgaben des <see cref="StrompreisZerlegungModel"/> zurück. Die Faltung des
        /// Bestands gehört in den Migrationsschritt 83
        /// (<see cref="StrompreisZerlegung"/>) — sie ändert Geldwerte und darf nicht
        /// beiläufig beim Öffnen eines Dialogs laufen.
        /// </para>
        /// <para>
        /// <b>Zwei Tabellen.</b> <c>SchemaKatalog.Schritt12_Preismodell</c> führt die
        /// vierzehn Spalten an <c>energy_project_settings</c> UND die drei
        /// Preisquellen-Verweise an <c>Tab_StromspeicherVariante</c>; der Helfer liest
        /// das Schema deshalb je Tabelle einmal.
        /// </para>
        /// </remarks>
        public static void StelleSpaltenSicher()
        {
            List<SchemaSpalte> alle = new List<SchemaSpalte>();
            alle.AddRange(SchemaKatalog.Schritt12_Preismodell);
            alle.AddRange(SchemaKatalog.Schritt83_Strompreisdetails);

            Preisanteile.SpaltenSicherstellen(nameof(StrompreisZerlegungCtrl), alle);
        }

        // =====================================================================
        // Energietraeger
        // =====================================================================

        /// <summary>
        /// Der Strom-Energietraeger eines Projekts (<c>pricing_model = 'ELECTRICITY'</c>),
        /// oder 0, wenn das Projekt keinen fuehrt.
        /// </summary>
        /// <remarks>
        /// Bei mehreren Stromtraegern - moeglich, aber unueblich - gilt der mit der
        /// kleinsten ID. Eine Auswahl anzubieten waere eine Entscheidung, die der
        /// Anwender im Kostenmodul ohnehin schon getroffen hat.
        /// </remarks>
        public static int StromCarrierId(int idProjekt)
        {
            if (idProjekt <= 0) return 0;

            // ET-5 (Anwenderentscheid 08.09.2026): Der an der ANLAGE gewaehlte Stromtraeger
            // (Waermepumpe vor Heizstab vor Speicher vor Photovoltaik) gewinnt, wenn er dem
            // Projekt zugeordnet ist - Preis, Anteile und Emissionen lesen dieselbe Wahl.
            try
            {
                int gewaehlt = ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(idProjekt);
                if (gewaehlt > 0) return gewaehlt;
            }
            catch { }

            object v = DataRepository.ExecuteScalar(
                "SELECT MIN(ec.id) FROM [" + TABLE + "] AS eps " +
                "INNER JOIN energy_carrier AS ec ON eps.[ID_Energieträger] = ec.id " +
                "WHERE eps.ID_Projekt = ? AND ec.pricing_model = ?",
                new DbParam("@proj", idProjekt),
                new DbParam("@pm", PRICING_MODEL_STROM));

            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Liest die Preiszerlegung einer (Projekt, Energietraeger)-Zeile. Fehlt die
        /// Zeile oder fehlen die Spalten, kommt ein Modell mit den Vorgabewerten
        /// zurueck und <see cref="StrompreisZerlegungModel.AusDatenbank"/> steht auf false.
        /// </summary>
        /// <remarks>
        /// <b>NULL heisst „kein Anteil"</b> (dieselbe Regel wie beim Brennstoffblock,
        /// Konzept § 5.1). Ein nicht gepflegter Wert laesst den VORSCHLAG des Modells in
        /// der Maske stehen, sein Aktiv-Schalter bleibt aber auf false - die Summe einer
        /// ungepflegten Zeile ist damit 0. Das loest den E5-Restpunkt „Aktiv-Flags kein
        /// verlaessliches Aus" und den Restpunkt „Nach #266" in einem: Ein Projekt, an
        /// dem niemand etwas eingestellt hat, rechnet weder mit 11,746 ct/kWh noch mit
        /// einem Modus, den niemand gewaehlt hat.
        /// </remarks>
        public StrompreisZerlegungModel Read(int idProjekt, int idEnergietraeger)
        {
            StrompreisZerlegungModel m = new StrompreisZerlegungModel();
            m.ID_Projekt = idProjekt;
            m.ID_Energietraeger = idEnergietraeger;

            if (idProjekt <= 0 || idEnergietraeger <= 0) return m;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@proj", idProjekt),
                new DbParam("@eid", idEnergietraeger));

            if (dt == null || dt.Rows.Count == 0) return m;

            DataRow r = dt.Rows[0];

            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG, ref m.Beschaffung, ref m.Beschaffung_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_VERTRIEB, ref m.Vertrieb, ref m.Vertrieb_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT, ref m.Netzentgelt, ref m.Netzentgelt_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER, ref m.Stromsteuer, ref m.Stromsteuer_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_KONZESSION, ref m.Konzession, ref m.Konzession_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN, ref m.Umlagen, ref m.Umlagen_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_KWKG, ref m.Umlage_KWKG, ref m.Umlage_KWKG_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_OFFSHORE, ref m.Umlage_Offshore, ref m.Umlage_Offshore_Aktiv);
            Preisanteile.Paar(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_STROMNEV19, ref m.Umlage_StromNEV19, ref m.Umlage_StromNEV19_Aktiv);

            Schalter(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN_EINZELN, ref m.Umlagen_Einzeln);

            // SP-E-5 (a): Verguetung_PV/_BHKW werden NICHT MEHR GELESEN. Die
            // Einspeiseverguetung steht bei den Wirtschaftlichkeitsparametern; die
            // Spalten bleiben stehen, damit eine aeltere Programmfassung auf derselben
            // Datei nicht auf einen fehlenden Namen laeuft.

            m.AusDatenbank = true;
            return m;
        }

        /// <summary>
        /// Die Preiszerlegung des Strom-Carriers eines Projekts - die Kurzform, die
        /// Simulation und Ergebnisanzeige brauchen.
        /// </summary>
        public StrompreisZerlegungModel ReadStrom(int idProjekt)
        {
            return Read(idProjekt, StromCarrierId(idProjekt));
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Schreibt die Preiszerlegung zurueck - ein zielgenaues UPDATE ueber
        /// (Projekt, Energietraeger), das die uebrigen Spalten der Zeile (Arbeitspreis,
        /// Heizwert, Emissionen) nicht anfasst.
        /// </summary>
        /// <remarks>
        /// <b>Modus und Gesamtwert werden NICHT mehr geschrieben.</b> Die beiden Spalten
        /// <c>Aufschlag_Modus</c> und <c>Aufschlag_Override</c> bleiben im Schema
        /// stehen, rechnen aber nicht mehr mit (Schemaschritt 83). Sie hier weiter zu
        /// beschreiben hiesse, eine tote Wahrheit zu pflegen.
        /// </remarks>
        /// <returns>
        /// true, wenn eine Zeile geschrieben wurde. false heisst: Es gibt keine Zeile -
        /// der Energietraeger ist dem Projekt nicht zugeordnet. Angelegt wird sie hier
        /// NICHT; das ist Sache des Kostenmoduls, das die Pflichtfelder kennt.
        /// </returns>
        public bool Update(StrompreisZerlegungModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (m.ID_Projekt <= 0 || m.ID_Energietraeger <= 0) return false;

            StelleSpaltenSicher();

            string sql =
                "UPDATE [" + TABLE + "] SET " +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_VERTRIEB) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_KONZESSION) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_KWKG) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_OFFSHORE) +
                Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_STROMNEV19) +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN_EINZELN + "] = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?";

            int betroffen = DataRepository.ExecuteNonQuery(sql,
                new DbParam("@besch", DbParamTyp.Double) { Wert = m.Beschaffung },
                Preisanteile.Aktiv("@beschA", m.Beschaffung_Aktiv),
                new DbParam("@vt", DbParamTyp.Double) { Wert = m.Vertrieb },
                Preisanteile.Aktiv("@vtA", m.Vertrieb_Aktiv),
                new DbParam("@netz", DbParamTyp.Double) { Wert = m.Netzentgelt },
                Preisanteile.Aktiv("@netzA", m.Netzentgelt_Aktiv),
                new DbParam("@st", DbParamTyp.Double) { Wert = m.Stromsteuer },
                Preisanteile.Aktiv("@stA", m.Stromsteuer_Aktiv),
                new DbParam("@kz", DbParamTyp.Double) { Wert = m.Konzession },
                Preisanteile.Aktiv("@kzA", m.Konzession_Aktiv),
                new DbParam("@uml", DbParamTyp.Double) { Wert = m.Umlagen },
                Preisanteile.Aktiv("@umlA", m.Umlagen_Aktiv),
                new DbParam("@kwkg", DbParamTyp.Double) { Wert = m.Umlage_KWKG },
                Preisanteile.Aktiv("@kwkgA", m.Umlage_KWKG_Aktiv),
                new DbParam("@offs", DbParamTyp.Double) { Wert = m.Umlage_Offshore },
                Preisanteile.Aktiv("@offsA", m.Umlage_Offshore_Aktiv),
                new DbParam("@nev", DbParamTyp.Double) { Wert = m.Umlage_StromNEV19 },
                Preisanteile.Aktiv("@nevA", m.Umlage_StromNEV19_Aktiv),
                new DbParam("@einzeln", DbParamTyp.Boolean) { Wert = m.Umlagen_Einzeln },
                new DbParam("@proj", DbParamTyp.Integer) { Wert = m.ID_Projekt },
                new DbParam("@eid", DbParamTyp.Integer) { Wert = m.ID_Energietraeger });

            if (betroffen > 0) m.AusDatenbank = true;
            return betroffen > 0;
        }

        // =====================================================================
        // Abbildung auf die Engine
        // =====================================================================

        /// <summary>
        /// Bildet die Preiszerlegung auf den Engine-Satz ab. Ab hier rechnet
        /// ausschliesslich die Engine - beide Summen stehen dort und sind headless
        /// getestet.
        /// </summary>
        /// <remarks>
        /// <b>Die Umlagen kommen EINMAL vor.</b> Steht
        /// <see cref="StrompreisZerlegungModel.Umlagen_Einzeln"/>, treten KWKG-, Offshore-
        /// und § 19-StromNEV-Umlage an die Stelle des Summenfelds; sonst zaehlt allein
        /// das Summenfeld. Beides zugleich waere eine Doppelzaehlung.
        /// </remarks>
        public static Preiszerlegung AlsPreiszerlegung(StrompreisZerlegungModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));

            List<Preisanteil> k = new List<Preisanteil>
            {
                new Preisanteil(KOMP_BESCHAFFUNG, m.Beschaffung, m.Beschaffung_Aktiv),
                new Preisanteil(KOMP_VERTRIEB, m.Vertrieb, m.Vertrieb_Aktiv),
                new Preisanteil(KOMP_NETZENTGELT, m.Netzentgelt, m.Netzentgelt_Aktiv),
                new Preisanteil(KOMP_STROMSTEUER, m.Stromsteuer, m.Stromsteuer_Aktiv),
                new Preisanteil(KOMP_KONZESSION, m.Konzession, m.Konzession_Aktiv)
            };

            if (m.Umlagen_Einzeln)
            {
                k.Add(new Preisanteil(KOMP_UMLAGE_KWKG, m.Umlage_KWKG, m.Umlage_KWKG_Aktiv));
                k.Add(new Preisanteil(KOMP_UMLAGE_OFFSHORE, m.Umlage_Offshore, m.Umlage_Offshore_Aktiv));
                k.Add(new Preisanteil(KOMP_UMLAGE_STROMNEV19, m.Umlage_StromNEV19, m.Umlage_StromNEV19_Aktiv));
            }
            else
            {
                k.Add(new Preisanteil(KOMP_UMLAGEN, m.Umlagen, m.Umlagen_Aktiv));
            }

            return new Preiszerlegung(k);
        }

        /// <summary>
        /// Der Umlagenwert, der zaehlt [ct/kWh]: das Summenfeld, oder die Summe der drei
        /// AKTIVEN Einzelumlagen, wenn sie einzeln gepflegt werden. Die Maske zeigt ihn
        /// im Kopf der Gruppe.
        /// </summary>
        public static double UmlagenCtKwh(StrompreisZerlegungModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (!m.Umlagen_Einzeln) return m.Umlagen_Aktiv ? m.Umlagen : 0.0;

            return (m.Umlage_KWKG_Aktiv ? m.Umlage_KWKG : 0.0)
                 + (m.Umlage_Offshore_Aktiv ? m.Umlage_Offshore : 0.0)
                 + (m.Umlage_StromNEV19_Aktiv ? m.Umlage_StromNEV19 : 0.0);
        }

        /// <summary>
        /// Der Satz, der auf eine Spot- oder Profilreihe gehoert [ct/kWh]: die Summe der
        /// aktiven Anteile OHNE Beschaffung - denn die Reihe IST die Beschaffung
        /// (Fachkonzept 4.1 a/b).
        /// </summary>
        public static double SummeOhneBeschaffungCtKwh(StrompreisZerlegungModel m)
        {
            return AlsPreiszerlegung(m).SummeAktivOhneCtKwh(KOMP_BESCHAFFUNG);
        }

        // =====================================================================
        // Kleinigkeiten
        // =====================================================================

        /// <summary>Uebernimmt einen Ja/Nein-Wert, wenn Spalte UND Wert vorhanden sind.</summary>
        private static void Schalter(DataTable dt, DataRow r, string spalte, ref bool ziel)
        {
            if (!dt.Columns.Contains(spalte)) return;
            object v = r[spalte];
            if (v == null || v == DBNull.Value) return;
            ziel = Convert.ToBoolean(v);
        }
    }
}
