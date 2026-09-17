using System;
using System.Collections.Generic;
using System.Data;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf die Preisbestandteile eines BRENNSTOFFpreises in
    // energy_project_settings (Konzept BHKW-Wirtschaftlichkeit § 5.1, Etappe B2
    // Paket A; Spalten aus SchemaMigration Schritt 60).
    //
    // EIN BAUPLAN FÜR BEIDE TRÄGER: Vorsorge, Lesen und Schreiben der
    // (Wert, Aktiv)-Paare, Summe und Rest stehen EINMAL in <see cref="Preisanteile"/>
    // und werden von diesem Controller wie von StrompreisZerlegungCtrl benutzt —
    // durchgängig NAMENSBASIERT mit Columns.Contains-Wache, kein
    // Zeichenkette-zu-Zahl, DDL-Vorsorge ohne Dialog.
    //
    // DER EINE UNTERSCHIED — und er ist der Grund für diese eigene Klasse:
    // StrompreisZerlegungCtrl.Read lässt NULL auf die VORSCHLAGSWERTE des Modells
    // zurückfallen (Preisanteile.Paar). Dieser Controller tut das NICHT
    // (Preisanteile.PaarNullbar): NULL heisst hier „kein Anteil erfasst" und bleibt
    // null (Konzept § 5.1, E5-Falle: bei Projekt 1030 wurden so 11,746 ct/kWh
    // wirksam, obwohl alle fünf Flags aus waren). Die Werte sind deshalb double?
    // und nicht double.
    //
    // KEIN MODUS MEHR (Anwenderentscheid 17.09.2026, wie beim Strom). Anteile mit
    // Wert und Aktiv-Schalter sagen alles, was die zwei Modi sagten; die Spalte
    // Anteil_Modus wird weder gelesen noch geschrieben.
    // ---------------------------------------------------------------------------
    public class BrennstoffBestandteilCtrl
    {
        public const string TABLE = "energy_project_settings";

        // --- Sprachneutrale Komponentenschluessel (Schicht 2 der Drei-Schichten-Regel) ---
        //
        // Sie verbinden die Datenbankspalte, den Engine-Satz und den Anzeigetext, ohne
        // selbst Anzeigetext zu sein. Die Beschriftung holt die Oberflaeche ueber
        // MyResource.

        public const string KOMP_ENERGIESTEUER = "ENERGIESTEUER";
        public const string KOMP_CO2 = "CO2";
        public const string KOMP_NETZENTGELT = "NETZENTGELT";
        public const string KOMP_VERTRIEB = "VERTRIEB";

        /// <summary>Die vier Bestandteile in Anzeigereihenfolge (Konzept § 6.2).</summary>
        public static readonly string[] KOMPONENTEN =
        {
            KOMP_ENERGIESTEUER, KOMP_CO2, KOMP_NETZENTGELT, KOMP_VERTRIEB
        };

        /// <summary>Die Wertspalten der vier Bestandteile, in derselben Reihenfolge.</summary>
        private static readonly string[] SPALTEN =
        {
            SchemaKatalog.SPALTE_BB_ENERGIESTEUER, SchemaKatalog.SPALTE_BB_CO2,
            SchemaKatalog.SPALTE_BB_NETZENTGELT, SchemaKatalog.SPALTE_BB_VERTRIEB
        };

        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt die Bestandteilsspalten an, falls die Migration noch nicht gelaufen ist —
        /// die tolerante Rückfallebene, die sich dieser Träger mit dem Strom teilt
        /// (<see cref="Preisanteile.SpaltenSicherstellen"/>).
        /// </summary>
        /// <remarks>
        /// <b>Bewusst OHNE jede Vorbelegung.</b> Hier entstehen nur die Spalten, damit
        /// ein Lesezugriff nicht scheitert; für die ANTEILE gibt es ohnehin nichts
        /// vorzubelegen: NULL ist ihre fachliche Aussage, nicht ihr Mangel. Die
        /// übrigen Zusagen — kein Dialog, Schema je Tabelle einmal gelesen, DDL über
        /// <c>StilleDb</c> — stehen beim Helfer.
        /// </remarks>
        public static void StelleSpaltenSicher()
        {
            Preisanteile.SpaltenSicherstellen(nameof(BrennstoffBestandteilCtrl),
                                              SchemaKatalog.Schritt60_BrennstoffBestandteile);
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Liest die Preisbestandteile einer (Projekt, Energieträger)-Zeile. Fehlt die
        /// Zeile oder fehlen die Spalten, kommt ein leeres Modell zurück und
        /// <see cref="BrennstoffBestandteilModel.AusDatenbank"/> steht auf false.
        /// </summary>
        /// <remarks>
        /// <b>NULL bleibt null.</b> Anders als <c>StrompreisZerlegungCtrl.Read</c> setzt
        /// dieser Weg bei einem nicht gepflegten Wert KEINEN Vorschlagssatz ein. Ein
        /// Anteil, den niemand erfasst hat, ist kein Anteil — die Kohärenzprüfung (BW2)
        /// hängt genau an dieser Unterscheidung.
        /// </remarks>
        public BrennstoffBestandteilModel Read(int idProjekt, int idEnergietraeger)
        {
            BrennstoffBestandteilModel m = new BrennstoffBestandteilModel();
            m.ID_Projekt = idProjekt;
            m.ID_Energietraeger = idEnergietraeger;

            if (idProjekt <= 0 || idEnergietraeger <= 0) return m;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@proj", idProjekt),
                new DbParam("@eid", idEnergietraeger));

            if (dt == null || dt.Rows.Count == 0) return m;

            DataRow r = dt.Rows[0];

            Preisanteile.PaarNullbar(dt, r, SchemaKatalog.SPALTE_BB_ENERGIESTEUER, ref m.Energiesteuer, ref m.Energiesteuer_Aktiv);
            Preisanteile.PaarNullbar(dt, r, SchemaKatalog.SPALTE_BB_CO2, ref m.CO2, ref m.CO2_Aktiv);
            Preisanteile.PaarNullbar(dt, r, SchemaKatalog.SPALTE_BB_NETZENTGELT, ref m.Netzentgelt, ref m.Netzentgelt_Aktiv);
            Preisanteile.PaarNullbar(dt, r, SchemaKatalog.SPALTE_BB_VERTRIEB, ref m.Vertrieb, ref m.Vertrieb_Aktiv);

            // Anteil_Modus wird NICHT gelesen: Die Spalte steht noch im Schema, hat
            // aber keine Bedeutung mehr (Anwenderentscheid 17.09.2026).

            m.AusDatenbank = true;
            return m;
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Schreibt die Bestandteile zurück — ein zielgenaues UPDATE über (Projekt,
        /// Energieträger), das die übrigen Spalten der Zeile (Arbeitspreis, Heizwert,
        /// Emissionen, den Strom-Aufschlagsblock) nicht anfasst.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>null wird DBNull, nicht 0.</b> Eine 0 wäre die Aussage „der Anteil ist
        /// null ct/kWh"; NULL ist die Aussage „es ist keiner erfasst". Der Unterschied
        /// ist genau der, den die Kohärenzprüfung braucht, und er muss deshalb auch den
        /// Weg in die Datenbank überstehen.
        /// </para>
        /// <para>
        /// <b>Der Modus wird NICHT mehr geschrieben.</b> <c>Anteil_Modus</c> bleibt im
        /// Schema stehen, rechnet und zeigt aber nichts mehr. Sie hier weiter zu
        /// beschreiben hiesse, eine tote Wahrheit zu pflegen — denselben Weg ist der
        /// Strom mit <c>Aufschlag_Modus</c> gegangen.
        /// </para>
        /// </remarks>
        /// <returns>
        /// true, wenn eine Zeile geschrieben wurde. false heisst: Es gibt keine Zeile —
        /// der Energieträger ist dem Projekt nicht zugeordnet. Angelegt wird sie hier
        /// NICHT; das ist Sache des Kostenmoduls (<c>ucFuelSettings</c>), das die
        /// Pflichtfelder kennt.
        /// </returns>
        public bool Update(BrennstoffBestandteilModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (m.ID_Projekt <= 0 || m.ID_Energietraeger <= 0) return false;

            StelleSpaltenSicher();

            // Seit dem Wegfall des Modus endet die SET-Liste mit einem Anteilspaar;
            // SetzPaar haengt jedem ein Komma an, das letzte faellt wieder weg.
            string satz = "";
            foreach (string spalte in SPALTEN) satz += Preisanteile.SetzPaar(spalte);

            string sql =
                "UPDATE [" + TABLE + "] SET " + satz.TrimEnd(' ', ',') + " " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?";

            int betroffen = DataRepository.ExecuteNonQuery(sql,
                Preisanteile.Wert("@est", m.Energiesteuer),
                Preisanteile.Aktiv("@estA", m.Energiesteuer_Aktiv),
                Preisanteile.Wert("@co2", m.CO2),
                Preisanteile.Aktiv("@co2A", m.CO2_Aktiv),
                Preisanteile.Wert("@netz", m.Netzentgelt),
                Preisanteile.Aktiv("@netzA", m.Netzentgelt_Aktiv),
                Preisanteile.Wert("@vt", m.Vertrieb),
                Preisanteile.Aktiv("@vtA", m.Vertrieb_Aktiv),
                new DbParam("@proj", DbParamTyp.Integer) { Wert = m.ID_Projekt },
                new DbParam("@eid", DbParamTyp.Integer) { Wert = m.ID_Energietraeger });

            if (betroffen > 0) m.AusDatenbank = true;
            return betroffen > 0;
        }

        // =====================================================================
        // Abbildung auf die Engine
        // =====================================================================

        /// <summary>
        /// Bildet die Preisbestandteile auf den Engine-Satz ab. Ab hier rechnet
        /// ausschliesslich die Engine — Summe, wirksamer Wert und der nicht
        /// aufgeschlüsselte Rest stehen dort und sind headless getestet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>null geht als 0 in die Komponente.</b> Die Engine kennt keinen
        /// unbekannten Wert, und sie braucht auch keinen: Ein nicht erfasster Anteil
        /// trägt zur Summe nichts bei. Die Unterscheidung „null" gegen „0" bleibt im
        /// Modell, wo Dialog und Kohärenzprüfung sie brauchen — die Engine bekommt nur
        /// die Zahl.
        /// </para>
        /// <para>
        /// <b>Kein Modus.</b> Dieser Block zerlegt einen Preis, statt ihn zu erhöhen;
        /// einen Gesamtaufschlag gibt es hier nicht (siehe
        /// <see cref="BrennstoffBestandteilModel"/>). Weder Engine noch Maske kennen
        /// einen Modus — der aussagekräftige Wert ist <c>SummeAktivCtKwh</c> („soviel
        /// des Preises ist ausgewiesen"), und was darüber hinaus im Arbeitspreis
        /// steckt, nennt die Restzeile.
        /// </para>
        /// </remarks>
        public static Preiszerlegung AlsPreiszerlegung(BrennstoffBestandteilModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));

            List<Preisanteil> k = new List<Preisanteil>
            {
                new Preisanteil(KOMP_ENERGIESTEUER, m.Energiesteuer ?? 0.0, m.Energiesteuer_Aktiv),
                new Preisanteil(KOMP_CO2, m.CO2 ?? 0.0, m.CO2_Aktiv),
                new Preisanteil(KOMP_NETZENTGELT, m.Netzentgelt ?? 0.0, m.Netzentgelt_Aktiv),
                new Preisanteil(KOMP_VERTRIEB, m.Vertrieb ?? 0.0, m.Vertrieb_Aktiv)
            };

            return new Preiszerlegung(k);
        }
    }
}
