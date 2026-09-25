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

    /// <summary>
    /// <b>Die HERKUNFT der Klimadaten eines Projekts</b> (Auftrag KL-4,
    /// Anwenderwunsch 19.09.2026: „Die verwendeten Klimadaten sollen sich auch auf
    /// der Übersicht befinden.").
    ///
    /// <para>Gelesen wird die PROJEKTKOPIE (<c>Tab_Klimaregion</c>) — dieselbe Zeile,
    /// mit der der Rechenlauf arbeitet, nicht der Katalogsatz daneben. Was auf der
    /// Startseite steht, muss die Rechnung beschreiben, die läuft.</para>
    /// </summary>
    /// <param name="Quelle">Sprachneutraler Schlüssel (<c>PVGIS</c>, <c>TRY_DATEI</c>,
    /// <c>TRY_REGIONAL</c>); leer = Altbestand vor Schemaschritt 95.</param>
    /// <param name="Bezeichner">Der Name der Region, wie ihn der Anwender gewählt hat.</param>
    /// <param name="Standort">Ortsname oder Koordinatenpaar
    /// (<c>KlimaregionStammCtrl.Standorttext</c>).</param>
    /// <param name="Importdatum">ISO-Text <c>yyyy-MM-dd</c>; leer = unbekannt.</param>
    /// <param name="Szenario">Sprachneutraler Schlüssel (<c>MITTEL</c>,
    /// <c>SOMMERWARM</c>, <c>WINTERKALT</c>, Schemaschritt 97); leer = sagt nichts
    /// dazu — Altbestand, PVGIS oder eine TRY-Datei ohne Art im Kopf.</param>
    /// <param name="Bezugsjahr">2015 oder 2045; <c>0</c> = sagt nichts dazu.</param>
    public sealed record KlimaHerkunft(string Quelle, string Bezeichner,
                                       string Standort, string Importdatum,
                                       string Szenario = "", int Bezugsjahr = 0);

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
        /// <b>Dieselben Regionen MIT ihrer Stamm-Id</b> (Auftrag KL-4) — der Inhalt
        /// des durchsuchbaren Auswahlfeldes auf der Startseite.
        ///
        /// <para><b>Warum die Id.</b> Die Klapplisten des Hauses melden ihre Wahl als
        /// <c>Tab_Klimaregion_STAMM.ID_Klimaregion</c>, nicht als Text (Hausregel
        /// „neue Beziehungen über IDs, nicht über Textfelder"). Damit fällt der
        /// Nachschlag Name → Id im Speicherweg weg, und zwei Regionen gleichen Namens
        /// bleiben unterscheidbar. <see cref="Klimaregionen"/> bleibt daneben stehen:
        /// Wer nur die Namen braucht, braucht nicht die Ids.</para>
        ///
        /// <para><b>Der Name ist seit Auftrag KL-6 der ANZEIGETEXT</b> — „hagelloch
        /// (TRY 2045 sommerwarm)", „München (PVGIS)". Er wird im Kern gebaut
        /// (<c>KlimaregionStammCtrl.Auswahlzeilen</c> über <see cref="KlimaAnzeige"/>),
        /// damit im Auswahlfeld derselbe Satz steht wie in der Regionsliste. Eine
        /// Region ohne Herkunft behält ihren blanken Namen — die Klammer wird nicht
        /// erfunden. <b>Die Wahl bleibt die Id</b>, der Text ist nur Text.</para>
        /// </summary>
        public static IReadOnlyList<(int Id, string Name)> KlimaregionenMitId()
        {
            var liste = new List<(int Id, string Name)>();
            foreach ((int Id, string Name, string Anzeige) e in KlimaregionAuswahlzeilen())
                liste.Add((e.Id, e.Anzeige));
            return liste;
        }

        /// <summary>
        /// <b>Die EINE Quelle jeder Klimaregion-Klappliste</b> — Stamm-Id, blanker Name
        /// und Anzeigetext („Heidelberg (TRY 2045 sommerwarm)"), nach Namen sortiert
        /// (<c>KlimaregionStammCtrl.Auswahlzeilen</c>).
        ///
        /// <para>Die Kopfleiste der Startseite nimmt daraus (Id, Anzeige)
        /// (<see cref="KlimaregionenMitId"/>), der Projektassistent dieselben Paare und
        /// dazu den blanken Namen, mit dem sein Speicherweg die Region in das Projekt
        /// kopiert. Zwei Klapplisten, ein Aufbau.</para>
        ///
        /// <para>Eine unlesbare Liste ist leer, kein Dialog.</para>
        /// </summary>
        public static IReadOnlyList<(int Id, string Name, string Anzeige)> KlimaregionAuswahlzeilen()
        {
            try
            {
                return KlimaregionStammCtrl.Auswahlzeilen();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimaregionen konnten nicht gelesen werden: " + ex.Message);
                return Array.Empty<(int, string, string)>();
            }
        }

        /// <summary>
        /// Die <b>STAMM-Id</b> der Klimaregion eines Projekts; <c>0</c>, wenn es keine
        /// führt oder ihr Name im Katalog fehlt.
        ///
        /// <para><c>Tab_Projekt.ID_Klimaregion</c> trägt die Id der PROJEKTKOPIE — ein
        /// anderer Schlüsselraum als die Einträge der Klapplisten. Der Weg zurück führt
        /// über den Bezeichner der Kopie (<see cref="ProjektKlimazone"/>), der der
        /// eindeutige Stammname ist (<see cref="KlimaregionStammId"/>) — derselbe Weg,
        /// auf dem die Kopfleiste ihre Wahl zeigt.</para>
        /// </summary>
        public static int ProjektKlimaregionStammId(int idProjekt)
        {
            string name = ProjektKlimazone(idProjekt);
            return name.Length == 0 ? 0 : KlimaregionStammId(name);
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

        /// <summary>
        /// <b>Woher die Klimadaten des Projekts stammen</b> (Auftrag KL-4,
        /// Anwenderwunsch 19.09.2026) — <c>null</c>, wenn das Projekt keine
        /// Klimaregion führt.
        ///
        /// <para><b>Gelesen wird die PROJEKTKOPIE</b> (<c>Tab_Klimaregion</c>) über
        /// <c>Tab_Projekt.ID_Klimaregion</c> — genau die Zeile, mit der der Rechenlauf
        /// arbeitet, und derselbe Weg wie <see cref="ProjektKlimazone"/>. Ein Griff in
        /// den Stammkatalog wäre der falsche Schlüsselraum (Befund W16b-B2) und
        /// beschriebe außerdem einen Satz, der seit dem Kopieren geändert worden sein
        /// kann.</para>
        ///
        /// <para><b>Tolerant ohne Schemaschritt 95</b> (Muster
        /// <c>KostenVorlagenCtrl.PflichtSpalteVorhanden</c>): Fehlen <c>Quelle</c> und
        /// <c>Importdatum</c>, bleiben sie leer — die Zeile nennt dann Bezeichner und
        /// Standort, und das ist mehr, als vorher dastand.</para>
        /// </summary>
        public static KlimaHerkunft KlimaHerkunft(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            try
            {
                object v = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?",
                    new DbParam("@id", idProjekt));

                if (v == null || v == DBNull.Value) return null;
                int idRegion = Convert.ToInt32(v);
                if (idRegion == 0) return null;

                return HerkunftLesen(KlimaregionStammCtrl.TAB_REGION_PROJEKT, "ID", "Bezeichner", idRegion);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Herkunft der Klimadaten konnte nicht gelesen werden: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// <b>Woher die Klimadaten eines KATALOGSATZES stammen</b> — dieselbe Auskunft
        /// wie <see cref="KlimaHerkunft"/>, gelesen aus <c>Tab_Klimaregion_STAMM</c>
        /// über die Stamm-Id; <c>null</c>, wenn es die Id nicht gibt.
        ///
        /// <para>Gebraucht vom Projektassistenten: Dort gibt es noch keine Projektkopie,
        /// nur die Wahl in der Klappliste. Beim Anlegen wird genau dieser Satz kopiert
        /// (<c>KlimaregionStammCtrl.CopyRegionToProjekt</c>, samt Quelle, Importdatum,
        /// Szenario und Bezugsjahr) — die Zeile im Assistenten sagt also dasselbe, was
        /// danach die Kopfleiste über die Kopie sagt.</para>
        /// </summary>
        public static KlimaHerkunft KlimaHerkunftStamm(int stammId)
        {
            if (stammId <= 0) return null;

            try
            {
                return HerkunftLesen(KlimaregionStammCtrl.TAB_REGION_STAMM, "ID_Klimaregion", "Name", stammId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Herkunft der Klimadaten konnte nicht gelesen werden: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Liest die Herkunft EINER Regionszeile — Projektkopie oder Katalogsatz; beide
        /// Tabellen führen dieselben Spalten, nur Schlüssel und Namensspalte heißen
        /// anders.
        ///
        /// <para><b>Tolerant ohne Schemaschritt 95/97</b> (Muster
        /// <c>KostenVorlagenCtrl.PflichtSpalteVorhanden</c>): Fehlen die Spalten, bleiben
        /// die Angaben leer — die Zeile nennt dann Bezeichner und Standort.</para>
        /// </summary>
        private static KlimaHerkunft HerkunftLesen(string tabelle, string idSpalte,
                                                   string nameSpalte, int id)
        {
            bool mitHerkunft =
                DataRepository.SpalteVorhanden(tabelle, SchemaKatalog.SPALTE_KR_QUELLE) &&
                DataRepository.SpalteVorhanden(tabelle, SchemaKatalog.SPALTE_KR_IMPORTDATUM);

            // Schemaschritt 97 wird eigens gefragt - eine Datenbank kann auf 95
            // stehen und dann Quelle und Importdatum fuehren, aber kein Szenario.
            bool mitSzenario =
                DataRepository.SpalteVorhanden(tabelle, SchemaKatalog.SPALTE_KR_SZENARIO) &&
                DataRepository.SpalteVorhanden(tabelle, SchemaKatalog.SPALTE_KR_BEZUGSJAHR);

            string felder = nameSpalte + ", Longitude, Latitude, Details";
            if (mitHerkunft)
                felder += ", " + SchemaKatalog.SPALTE_KR_QUELLE +
                          ", " + SchemaKatalog.SPALTE_KR_IMPORTDATUM;
            if (mitSzenario)
                felder += ", " + SchemaKatalog.SPALTE_KR_SZENARIO +
                          ", " + SchemaKatalog.SPALTE_KR_BEZUGSJAHR;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT " + felder + " FROM " + tabelle + " WHERE " + idSpalte + " = ?",
                new DbParam("@idRegion", id));

            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];

            return new KlimaHerkunft(
                mitHerkunft ? Katalogfeld.Text(r, SchemaKatalog.SPALTE_KR_QUELLE) : "",
                Katalogfeld.Text(r, nameSpalte),
                KlimaregionStammCtrl.Standorttext(Katalogfeld.Text(r, "Details"),
                                                  Katalogfeld.Zahl(r, "Longitude") ?? 0,
                                                  Katalogfeld.Zahl(r, "Latitude") ?? 0),
                mitHerkunft ? Katalogfeld.Text(r, SchemaKatalog.SPALTE_KR_IMPORTDATUM) : "",
                mitSzenario ? Katalogfeld.Text(r, SchemaKatalog.SPALTE_KR_SZENARIO) : "",
                mitSzenario ? Katalogfeld.Ganzzahl(r, SchemaKatalog.SPALTE_KR_BEZUGSJAHR) : 0);
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
            // Der NAMENSWEG ist der Id-Weg plus EIN Nachschlag - kein zweiter Ablauf
            // (Lehre aus Befund W16a-B5: zwei Methoden desselben Inhalts driften).
            // Die Reihenfolge der Pruefungen bleibt die des Vorlaeufers: erst das
            // Projekt, dann die Region.
            if (string.IsNullOrEmpty(projektname)) return KlimaStand.KeinProjekt;
            if (string.IsNullOrEmpty(regionName)) return KlimaStand.KeineRegion;

            int stammRegionId = KlimaregionStammId(regionName);
            if (stammRegionId <= 0) return KlimaStand.RegionNichtGefunden;

            return KlimaregionSpeichern(idProjekt, projektname, stammRegionId);
        }

        /// <summary>
        /// <b>Dieselbe Tat über die ID der Stammregion</b> (Auftrag KL-4) — der Weg des
        /// durchsuchbaren Auswahlfeldes, das seine Wahl als Id meldet.
        ///
        /// <para>Dies ist der EINE Ablauf; die Namensfassung schlägt nur die Id nach und
        /// ruft hierher. Der Klima-Datensatz (Region + Klimadaten + Solar) wird aus den
        /// STAMM-Tabellen in das Projekt kopiert (falls noch nicht vorhanden); am
        /// Projekt wird die Id der PROJEKT-Kopie gespeichert, nicht die STAMM-Id.</para>
        /// </summary>
        /// <param name="idProjekt">Rückfall-Id, wenn zum Namen nichts gefunden wird.</param>
        /// <param name="projektname">Der führende Schlüssel des offenen Projekts.</param>
        /// <param name="stammRegionId"><c>Tab_Klimaregion_STAMM.ID_Klimaregion</c>;
        /// <c>0</c> heißt „keine gewählt".</param>
        public static KlimaStand KlimaregionSpeichern(int idProjekt, string projektname,
                                                      int stammRegionId)
        {
            if (string.IsNullOrEmpty(projektname)) return KlimaStand.KeinProjekt;
            if (stammRegionId <= 0) return KlimaStand.KeineRegion;

            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            ctrl_projekt.ReadSingle(projektname);
            int id = ctrl_projekt.m_ID > 0 ? ctrl_projekt.m_ID : idProjekt;

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
