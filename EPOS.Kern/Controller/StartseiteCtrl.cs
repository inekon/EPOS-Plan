using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>Ausgang des Speicherwegs der Klimaregion (<see cref="StartseiteCtrl"/>).</summary>
    public enum KlimaStand
    {
        /// <summary>Gespeichert.</summary>
        Gespeichert,
        /// <summary>Es ist kein Projekt offen.</summary>
        KeinProjekt,
        /// <summary>Es ist keine Region gewählt.</summary>
        KeineRegion,
        /// <summary>Zu dem Namen gibt es keinen Stammsatz.</summary>
        RegionNichtGefunden,
        /// <summary>Die Kopie in das Projekt ist gescheitert.</summary>
        NichtUebernommen
    }

    /// <summary>Ein Eintrag des Variantenfeldes im Kopfband der Startseite.</summary>
    /// <param name="Id"><c>Tab_Projekt.ID</c>.</param>
    /// <param name="Name">Anzeigename — beim Stamm der Projektname, sonst „&lt;Stamm&gt; - &lt;Bezeichner&gt;".</param>
    /// <param name="IstStamm">true = das Stammprojekt der Gruppe.</param>
    public sealed record VariantenEintrag(int Id, string Name, bool IstStamm);

    /// <summary>
    /// Das Variantenfeld des Kopfbandes: die Gruppe zum offenen Projekt samt
    /// Vorauswahl.
    /// </summary>
    /// <param name="Eintraege">Stamm und Varianten in Ladereihenfolge.</param>
    /// <param name="GewaehltId">Vorauswahl — das offene Projekt, sonst der erste Eintrag; 0 = keine.</param>
    /// <param name="Anzahl">Zahl der VARIANTEN (ohne den Stamm) — der Rückgabewert des Vorläufers.</param>
    /// <param name="StammName">Anzeigename des Stammprojekts; <c>""</c>, wenn keiner zu lesen war.</param>
    public sealed record VariantenAnzeige(
        IReadOnlyList<VariantenEintrag> Eintraege,
        int GewaehltId,
        int Anzahl,
        string StammName);

    /// <summary>
    /// Die DATENSEITE der Startseite (iU9-W16b.0, K4 der Vermessung) — alles, was
    /// <c>Form_Start</c> an Datenbank anfasste, ausgenommen der Projektkontext
    /// (<see cref="ProjektKontextCtrl"/>) und die Kachelbitmaske
    /// (<see cref="KomponentenBestandCtrl"/>).
    ///
    /// <para><b>Vier Abfragen weniger in der Oberfläche</b> (Befund W16-B34): Die
    /// Startmaske führte zwölf Inline-SQL, vier davon in dieser Ecke —
    /// <c>Form_Start:356</c> (Regionsname zur Stamm-Id), <c>:369</c> (Stamm-Id zum
    /// Namen, mit <b>Zeichenkettenverkettung</b>, Befund W16-B11), <c>:382</c> und
    /// <c>:390</c> (Klimaregion des Projekts über die Projektkopie). Alle vier stehen
    /// hier parametriert (<c>DbParam</c>) und laufen damit durch den
    /// <c>SqlDialektPruefer</c>.</para>
    ///
    /// <para><b>Seit dem Anwenderentscheid W16b‑O‑3 (04.09.2026) sind es drei
    /// Methoden statt vier:</b> <c>ProjektKlimaregion</c> (nur die Projektkopie) und
    /// der Stammnachschlag <see cref="KlimaregionName"/> sind zu
    /// <see cref="ProjektKlimazone"/> zusammengelegt — Stammname zuerst, Projektkopie
    /// als Rückfall. Die zwei SQL-Texte von <c>:382</c> und <c>:390</c> stehen
    /// unverändert dort, <see cref="KlimaregionName"/> bleibt als eigener Weg für
    /// <c>:356</c> bestehen.</para>
    ///
    /// <para><b>Was hier NICHT steht.</b> Die Fensteranteile der Vorlage: das Füllen
    /// eines <c>ComboBox</c>, das Ab- und Anhängen von <c>SelectedIndexChanged</c>, das
    /// Ausrechnen der Aufklappbreite (<c>SetzeDropDownBreite</c>) und die sieben
    /// <c>MessageBox</c>. Der Speicherweg der Klimaregion meldet seinen Ausgang als
    /// <see cref="KlimaStand"/>; welchen Text die Oberfläche daraus macht, ist ihre
    /// Sache — die Schlüssel sind unverändert die des Bestands
    /// (<c>Text_Form_Start_*</c>).</para>
    /// </summary>
    public static class StartseiteCtrl
    {
        // =====================================================================
        //  Klimaregion — lesen
        // =====================================================================

        /// <summary>
        /// Die wählbaren Klimaregionen des Auslieferungskatalogs, nach Namen sortiert —
        /// der Inhalt des Auswahlfeldes (<c>Form_Start.ComboBox_Klimaregion</c>
        /// :1837-1850).
        ///
        /// <para>Der Vorläufer las dafür <c>KlimaregionStammCtrl.ReadAll()</c> und
        /// schrieb <c>items[i].m_szName</c> einzeln in die Liste;
        /// <c>KlimaregionStammCtrl.Bezeichner()</c> liefert genau diese Namen und ist
        /// seit iU9-W14c.0d der Weg für alle Aufrufer.</para>
        /// </summary>
        public static IReadOnlyList<string> Klimaregionen()
        {
            try
            {
                KlimaregionStammCtrl ctrl = new KlimaregionStammCtrl();
                ctrl.ReadAll();
                return ctrl.Bezeichner();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimaregionen konnten nicht gelesen werden: " + ex.Message);
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Der Name einer Klimaregion des STAMMKATALOGS — <c>Form_Start:354-365</c>
        /// (<c>select * from Tab_Klimaregion_STAMM where ID_Klimaregion = …</c>),
        /// parametriert und auf die eine gelesene Spalte eingeschränkt.
        /// </summary>
        public static string KlimaregionName(int idKlimaregion)
        {
            if (idKlimaregion <= 0) return "";

            try
            {
                object wert = DataRepository.ExecuteScalar(
                    "SELECT Name FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = ?",
                    new DbParam("@id", idKlimaregion));

                return wert == null || wert == DBNull.Value ? "" : (Convert.ToString(wert) ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimaregion konnte nicht gelesen werden: " + ex.Message);
                return "";
            }
        }

        /// <summary>
        /// Die Stamm-Id zu einem Regionsnamen — <c>Form_Start:367-377</c>. Der
        /// Vorläufer verkettete den ANWENDERTEXT in das <c>WHERE</c> (Befund W16-B11);
        /// die parametrierte Fassung liegt seit iU9-W15a.0f als
        /// <c>KlimaregionStammCtrl.IdVonName</c> im Kern und wird hier nur gerufen —
        /// keine dritte Methode desselben Inhalts (Lehre aus Befund W16a-B5).
        /// </summary>
        public static int KlimaregionStammId(string name)
        {
            try { return KlimaregionStammCtrl.IdVonName(name); }
            catch (Exception ex)
            {
                Console.WriteLine("Klimaregion konnte nicht gesucht werden: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Die Klimazone EINES PROJEKTS — die eine Antwort für Windows UND iOS
        /// (<b>Anwenderentscheid W16b‑O‑3 vom 04.09.2026: „nehme iOS-Lösung"</b>).
        ///
        /// <para><b>Die Reihenfolge ist der Entscheid.</b> Erst
        /// <c>Tab_Projekt.ID_Klimaregion</c> lesen (<c>Form_Start:379-385</c>), dann
        /// dazu den <b>STAMMNAMEN</b> (<see cref="KlimaregionName"/>,
        /// <c>Tab_Klimaregion_STAMM.Name</c>) — den Weg, den
        /// <c>EPOS.iOS/Dienste/IosProjektKontext</c> bisher allein ging. Gibt es zu
        /// der Id keinen Stammsatz, antwortet der <b>Rückfall</b> mit dem
        /// <c>Bezeichner</c> der PROJEKTKOPIE (<c>Tab_Klimaregion</c>, wörtlich
        /// <c>Form_Start:386-397</c>) — der bisherige Windows-Weg
        /// <c>ProjektKlimaregion</c>, der damit als eigene Methode entfällt.</para>
        ///
        /// <para><b>Der Rückfall ist der Normalfall, nicht die Ausnahme</b> (Messung
        /// vom 04.09.2026, W16b-Protokoll § 6). Die beiden Wege lesen ZWEI
        /// SCHLÜSSELRÄUME: An <c>Tab_Projekt.ID_Klimaregion</c> steht die Id der
        /// PROJEKTKOPIE (<c>Tab_Klimaregion.ID</c>) — so schreibt es
        /// <see cref="KlimaregionSpeichern"/> ausdrücklich —, der Stammzweig hält
        /// denselben Zahlenwert gegen <c>Tab_Klimaregion_STAMM.ID_Klimaregion</c>. In
        /// der Testdatenbank laufen die Stamm-Ids von 1 bis 50, die Kopie-Ids ab
        /// 1 006 017; die Überschneidung ist <b>0</b>, und <b>alle dreizehn</b>
        /// Referenzprojekte laufen in den Rückfall. Die angezeigte Klimazone bleibt
        /// dadurch Wort für Wort dieselbe wie vor dem Entscheid. Die Projektkopie
        /// führt gar keine Stammspalte — sie hängt über den TEXT am Stamm, und
        /// <c>Bezeichner</c> und <c>Name</c> sind im Bestand zeichengleich.</para>
        ///
        /// <para><b>Warum nicht <c>KlimaregionStammCtrl.NameZuProjektregion</c>:</b>
        /// Der dreht die Reihenfolge um (Kopie zuerst, Stamm als Rückfall) und
        /// verlangt zusätzlich die Projekt-Id in der <c>WHERE</c>-Bedingung. Er bleibt
        /// dem Assistentenkopf; hier gilt der Entscheid.</para>
        /// </summary>
        public static string ProjektKlimazone(int idProjekt)
        {
            if (idProjekt <= 0) return "";

            try
            {
                object v = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?",
                    new DbParam("@id", idProjekt));

                if (v == null || v == DBNull.Value) return "";
                int idRegion = Convert.ToInt32(v);
                if (idRegion == 0) return "";

                // W16b-O-3: Der STAMMNAME ist der fuehrende Weg (die iOS-Loesung).
                string stammname = KlimaregionName(idRegion);
                if (!string.IsNullOrEmpty(stammname)) return stammname;

                // Rueckfall: die PROJEKTKOPIE - woertlich Form_Start:386-397. An
                // Tab_Projekt.ID_Klimaregion steht die ID der Kopie, deshalb greift
                // dieser Zweig im Bestand IMMER; ohne ihn bliebe die Anzeige leer.
                object b = DataRepository.ExecuteScalar(
                    "SELECT Bezeichner FROM Tab_Klimaregion WHERE ID = ?",
                    new DbParam("@idRegion", idRegion));

                return b == null || b == DBNull.Value ? "" : (Convert.ToString(b) ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimazone des Projekts konnte nicht gelesen werden: " + ex.Message);
                return "";
            }
        }

        // =====================================================================
        //  Klimaregion — schreiben
        // =====================================================================

        /// <summary>
        /// Speichert die gewählte Klimaregion zum offenen Projekt — wörtlich
        /// <c>Form_Start.btn_Speichern_Click</c> (:1856-1896), ohne dessen fünf
        /// <c>MessageBox</c>.
        ///
        /// <para>Der Klima-Datensatz (Region + Klimadaten + Solar) wird aus den
        /// STAMM-Tabellen in das Projekt kopiert (falls noch nicht vorhanden); am
        /// Projekt wird die Id der PROJEKT-Kopie gespeichert, nicht die STAMM-Id.
        /// Reihenfolge, Prüfungen und das Fortschreiben des Änderungsdatums sind
        /// unverändert.</para>
        /// </summary>
        /// <param name="idProjekt">Rückfall-Id, wenn zum Namen nichts gefunden wird.</param>
        /// <param name="projektname">Der führende Schlüssel des offenen Projekts.</param>
        /// <param name="regionName">Der gewählte Regionsname aus dem Auswahlfeld.</param>
        public static KlimaStand KlimaregionSpeichern(int idProjekt, string projektname, string regionName)
        {
            if (string.IsNullOrEmpty(projektname)) return KlimaStand.KeinProjekt;
            if (string.IsNullOrEmpty(regionName)) return KlimaStand.KeineRegion;

            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            ctrl_projekt.ReadSingle(projektname);
            int id = ctrl_projekt.m_ID > 0 ? ctrl_projekt.m_ID : idProjekt;

            // STAMM-Region-ID zur gewaehlten Klimaregion ermitteln.
            int stammRegionId = KlimaregionStammId(regionName);
            if (stammRegionId <= 0) return KlimaStand.RegionNichtGefunden;

            // Klima-Datensatz ins Projekt kopieren (falls noch nicht vorhanden) und die
            // ID der Projekt-Kopie zurueckerhalten.
            int projektRegionId = KlimaregionStammCtrl.CopyRegionToProjekt(stammRegionId, id);
            if (projektRegionId <= 0) return KlimaStand.NichtUebernommen;

            // Am Projekt die ID der Projekt-Kopie speichern (nicht die STAMM-ID).
            ctrl_projekt.m_ID_Klimaregion = projektRegionId;
            ctrl_projekt.m_Aenderungsdatum = DateTime.Now;
            ctrl_projekt.Update();

            return KlimaStand.Gespeichert;
        }

        // =====================================================================
        //  Varianten und Projektname
        // =====================================================================

        /// <summary>
        /// Die Variantengruppe zum offenen Projekt — der Datenteil von
        /// <c>Form_Start.FuelleVariantenCombo</c> (:2054-2111).
        ///
        /// <para>Ist das offene Projekt selbst eine Variante, wird die Gruppe seines
        /// STAMMS geladen; sonst ist es selbst der Stamm. Die Vorauswahl ist das
        /// offene Projekt, sonst der erste Eintrag — wörtlich die Schleife des
        /// Vorläufers.</para>
        /// </summary>
        public static VariantenAnzeige Varianten(int idProjekt)
        {
            List<VariantenEintrag> eintraege = new List<VariantenEintrag>();
            if (idProjekt <= 0) return new VariantenAnzeige(eintraege, 0, 0, "");

            try
            {
                VariantenCtrl ctrl = new VariantenCtrl();

                // Stammprojekt bestimmen: ist das geoeffnete Projekt eine Variante,
                // dessen Stamm nehmen; sonst ist es selbst der Stamm.
                int stammId = ctrl.StammRefDerVariante(idProjekt);
                if (stammId <= 0) stammId = idProjekt;

                string stammName = Projektname(stammId);
                string anzeige = stammName == "" ? Projektname(idProjekt) : stammName;

                int anzahl = 0;
                foreach (VariantenCtrl.VarianteInfo vi in ctrl.LadeGruppe(stammId, stammName))
                {
                    eintraege.Add(new VariantenEintrag(vi.IdProjekt, vi.Projektname ?? "", vi.IstStamm));
                    if (!vi.IstStamm) anzahl++;
                }

                int gewaehlt = 0;
                foreach (VariantenEintrag e in eintraege)
                    if (e.Id == idProjekt) { gewaehlt = e.Id; break; }
                if (gewaehlt == 0 && eintraege.Count > 0) gewaehlt = eintraege[0].Id;

                return new VariantenAnzeige(eintraege, gewaehlt, anzahl, anzeige);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Variantengruppe konnte nicht gelesen werden: " + ex.Message);
                return new VariantenAnzeige(eintraege, 0, 0, "");
            }
        }

        /// <summary>
        /// Der Projektname zu einer Id; <c>""</c>, wenn es das Projekt nicht gibt.
        ///
        /// <para>Das ist zugleich <c>Form_Start.ProjektnameFuerReiter</c> (:2002-2014,
        /// die Kopfzeile des Reiters „Berichte &amp; Kosten") und
        /// <c>Form_Start.LiesProjektname</c> (:2126-2133). Der zweite las dafür ALLE
        /// Projekte (<c>ReadAll</c>) und suchte linear — bei jeder Variantenzeile
        /// erneut; hier steht die Einzelabfrage, die <c>ProjektCtrl.ReadSingle(int)</c>
        /// ohnehin führt.</para>
        /// </summary>
        public static string Projektname(int idProjekt)
        {
            if (idProjekt <= 0) return "";

            try
            {
                ProjektCtrl pc = new ProjektCtrl();
                pc.ReadSingle(idProjekt);
                return pc.rows > 0 ? (pc.m_szProjektname ?? "") : "";
            }
            catch { return ""; }
        }

        // =====================================================================
        //  Der Reiter „Simulation" — die Zusammenfassung
        // =====================================================================

        /// <summary>
        /// Die Id der Klimaregion eines Projekts; <c>0</c> = keine gesetzt. Der Reiter
        /// „Simulation" prüft damit, ob er überhaupt rechnen darf
        /// (<c>Form_Start.tabPage5_Enter</c> :1062-1071).
        /// </summary>
        public static int KlimaregionIdVonProjekt(string projektname)
        {
            if (string.IsNullOrEmpty(projektname)) return 0;

            try
            {
                ProjektCtrl ctrl = new ProjektCtrl();
                ctrl.ReadSingle(projektname);
                return ctrl.rows > 0 ? ctrl.m_ID_Klimaregion : 0;
            }
            catch { return 0; }
        }
    }
}
