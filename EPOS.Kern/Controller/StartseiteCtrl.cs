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
    /// <para><b>Seit dem Anwenderentscheid W16b‑O‑3 (04.09.2026) sind es DREI.</b>
    /// <c>:356</c> — der Name einer Region des STAMMKATALOGS, zuletzt
    /// <c>KlimaregionName(int)</c> — ist ersatzlos gefallen: Sie hatte nach K6‑a
    /// keinen Aufrufer mehr und stand nur noch für die Angleichung der iOS-Fassung im
    /// Kern (Befund W16b‑B3). Die Messung zu diesem Entscheid hat die Angleichung
    /// widerlegt (die iOS-Abfrage las den falschen Schlüsselraum), damit fällt auch
    /// der Grund, sie zu führen. <c>:382</c> und <c>:390</c> stehen unverändert in
    /// <see cref="ProjektKlimazone"/> — der EINEN Wahrheit für
    /// <c>IProjektKontext.Klimazone</c> auf beiden Plattformen —, <c>:369</c>
    /// unverändert in <see cref="KlimaregionStammId"/>.</para>
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
        /// Die Klimazone EINES PROJEKTS — <b>die eine Antwort für Windows UND iOS</b>
        /// (Anwenderentscheid W16b‑O‑3 vom 04.09.2026) und die Quelle von
        /// <c>IProjektKontext.Klimazone</c>.
        ///
        /// <para><b>Wörtlich der Windows-Weg</b> <c>Form_Start.GetProjektKlimaregion</c>
        /// (:379-400): Erst <c>Tab_Projekt.ID_Klimaregion</c> lesen, dann dazu den
        /// <c>Bezeichner</c> der PROJEKTKOPIE (<c>Tab_Klimaregion</c>) — beides
        /// parametriert. <b>Keine Stammabfrage, kein Rückfall.</b></para>
        ///
        /// <para><b>Warum der Entscheid so ausgegangen ist.</b> Er lautete „nehme
        /// iOS-Lösung": <c>EPOS.iOS/Dienste/IosProjektKontext</c> las hier den
        /// STAMMNAMEN (<c>Tab_Klimaregion_STAMM.Name</c>) über dieselbe Zahl
        /// (Befund W16b‑B2). Die Messung vom 04.09.2026 (W16b-Protokoll § 6) hat
        /// gezeigt, dass das <b>kein zweiter Weg war, sondern ein Fehler</b>: An
        /// <c>Tab_Projekt.ID_Klimaregion</c> steht die Id der PROJEKTKOPIE
        /// (<c>Tab_Klimaregion.ID</c>) — so schreibt es
        /// <see cref="KlimaregionSpeichern"/> ausdrücklich —, die iOS-Abfrage hielt
        /// dieselbe Zahl gegen <c>Tab_Klimaregion_STAMM.ID_Klimaregion</c>, also gegen
        /// einen ANDEREN Schlüsselraum. Stamm-Ids 1…50, Kopie-Ids ab 1 006 017,
        /// Überschneidung <b>0</b>: Sie antwortete für jedes Projekt des Bestands
        /// leer, und wo sie überhaupt antwortete, nur durch Kollision. Die
        /// Vereinheitlichung bleibt — sie geht in Richtung der PROJEKTKOPIE, und
        /// <c>IosProjektKontext</c> ruft seither DIESEN Weg.</para>
        ///
        /// <para><b>Die Stammabfrage ist damit gefallen</b> (<c>Form_Start:354-365</c>,
        /// zuletzt <c>KlimaregionName(int)</c>). Sie hatte nach K6‑a keinen Aufrufer
        /// mehr und stand nur noch für diese Angleichung im Kern (Befund W16b‑B3);
        /// nachdem die Angleichung sie widerlegt hat, gibt es keinen Grund mehr, sie
        /// zu führen. Den Nachschlag Name → Stamm-Id (<c>:369</c>) leistet
        /// unverändert <see cref="KlimaregionStammId"/>.</para>
        ///
        /// <para><b>Bewusst NICHT
        /// <c>KlimaregionStammCtrl.NameZuProjektregion</c>:</b> Der kennt einen
        /// Stamm-Rückfall und verlangt zusätzlich die Projekt-Id in der
        /// <c>WHERE</c>-Bedingung. Er bleibt dem Assistentenkopf; wer hier den
        /// Stammnamen einsetzte, wo die Projektkopie fehlt, änderte den gemeldeten
        /// Kontext — genau das, was Risiko R‑W16‑4 verbietet.</para>
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

                // Am Projekt ist die ID der Projekt-Kopie (Tab_Klimaregion.ID)
                // gespeichert - woertlich Form_Start:386-397. Gibt es die Kopie nicht,
                // ist die Klimazone leer; ein Stamm-Rueckfall waere ein Griff in den
                // falschen Schluesselraum (siehe oben).
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
