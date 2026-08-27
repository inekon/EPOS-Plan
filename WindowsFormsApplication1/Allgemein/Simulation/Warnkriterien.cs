using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein einzelner Befund des Warnkriterienkatalogs.
    ///
    /// <para><see cref="Kriterium"/> ist ein SPRACHNEUTRALER Schluessel
    /// (<c>"W1"</c>… bzw. <c>"HART_…"</c>, siehe <see cref="Warnkriterien"/>), mit dem
    /// Aufrufer filtern und gruppieren; <see cref="Text"/> ist der fertige,
    /// lokalisierte Anzeigetext. Beides gehoert getrennt — Drei-Schichten-Regel.</para>
    /// </summary>
    public class Warnbefund
    {
        /// <summary>Sprachneutraler Kriteriumsschluessel (<c>W1</c>…<c>HART_LEERES_SET</c>).</summary>
        public string Kriterium = "";

        /// <summary>
        /// true = HART. Die Konstellation ist physikalisch bzw. konstruktiv unmoeglich
        /// und wird von den Bestandsguards in Dialog und Engine abgewiesen; dieser
        /// Katalog meldet sie zusaetzlich VORAB.
        /// </summary>
        public bool Hart;

        /// <summary>Betroffene <c>Tab_Energieanlagen.ID</c>; 0, wenn der Befund an keiner Anlage haengt.</summary>
        public int ID_Anlage;

        /// <summary>Betroffene <c>Tab_Pufferspeicher.ID</c>; 0, wenn der Befund an keinem Speicher haengt.</summary>
        public int ID_Puffer;

        /// <summary>Fertiger, lokalisierter Anzeigetext.</summary>
        public string Text = "";
    }

    /// <summary>
    /// PAKET S2 — der WARNKRITERIENKATALOG aus Konzept 6.2 (Entscheidung F6) als EINE
    /// Wahrheit fuer Dialog und Laufstart.
    ///
    /// <para><b>Der Grundsatz.</b> Zuordnungen zwischen Erzeugern und Pufferspeichern
    /// sind frei — aber nur SINNVOLLE Konstellationen bleiben unkommentiert. Die
    /// sperrende Pufferfilterung (<c>WaermesenkeClass.PufferPasst</c>) faellt mit diesem
    /// Paket; an ihre Stelle tritt ein definierter Katalog, der beim Speichern einer
    /// Senkenzeile (<see cref="PruefeSenke"/>) und beim Laufstart
    /// (<see cref="PruefeProjekt"/>) dasselbe prueft und dieselben Texte liefert.</para>
    ///
    /// <para><b>Umgesetzte Kriterien.</b>
    /// <list type="table">
    ///   <item><term>W1</term><description>Das Puffer-Ziel einer Senkenzeile liegt
    ///     ausserhalb des Klassen-Sets des gewaehlten Speichers — geladen wuerde mit
    ///     einem Zweck, den der Speicher nie entlaedt.</description></item>
    ///   <item><term>W2</term><description>Die BAUFORM (<c>Tab_Pufferspeicher.Speichertyp</c>)
    ///     widerspricht dem Klassen-Set.</description></item>
    ///   <item><term>W3</term><description>Der Erzeuger-Vorlauf liegt unter dem wirksamen
    ///     Vorlauf <c>VL_eff</c> des Zielspeichers.</description></item>
    ///   <item><term>W5</term><description>Ein als Waermequelle konfigurierter Puffer hat
    ///     keinen einzigen Lader.</description></item>
    ///   <item><term>HART_*</term><description>Kurzschluss (Quelle = eigenes Ladeziel),
    ///     Ring in der Kaskadenkette, leeres Klassen-Set.</description></item>
    /// </list></para>
    ///
    /// <para><b>BEWUSST VERTAGT auf Paket P1 (Schichtmodell).</b> Drei Anteile des
    /// Katalogs verlangen Groessen, die es im Datenmodell noch nicht gibt; ihre
    /// Schluessel sind hier bereits reserviert, damit sie spaeter ohne Umbenennung
    /// nachrutschen:
    /// <list type="bullet">
    ///   <item><description>der <c>T_Nutz</c>-Anteil von W3 („Erzeuger-Vorlauf &lt;
    ///     <c>T_Nutz</c> des Zielkanals") — <c>T_Nutz_BW</c> entsteht erst mit
    ///     Schema-Schritt 53;</description></item>
    ///   <item><description><see cref="W4_TNUTZ_UEBER_VLEFF"/> (<c>T_Nutz_BW</c> &gt;
    ///     <c>VL_eff</c>) — dieselbe Spalte;</description></item>
    ///   <item><description><see cref="W6_SCHICHTUNG_AM_VERBUND"/>
    ///     (<c>Schichten_Anzahl</c> &gt; 1 am Leitspeicher eines Parallelverbunds) —
    ///     <c>Schichten_Anzahl</c> entsteht ebenfalls erst mit Schritt 53. W6 wird
    ///     ausserdem ABGEWIESEN und nicht nur gewarnt (Konzept 6.3); der Guard gehoert
    ///     deshalb in den Speicherdialog von P1, dieser Katalog fuehrt nur den
    ///     Schluessel.</description></item>
    /// </list></para>
    ///
    /// <para><b>Verhaeltnis zu den Bestandsguards.</b> Kurzschluss und Ring werden heute
    /// schon abgefangen — im Senkendialog (<c>Form_Waermesenke.ListePruefen</c>), in der
    /// Quellenpruefung (<c>WaermesenkeClass.QuellePruefen</c>), beim Aufbau der
    /// Quellbezuege (<c>SimulationControl.QuellbezuegeAufbauen</c>, E-K2-1) und in der
    /// Ebenen-Relaxation der Kaskadenschleife (Ring, ABBRUCH des Laufs). Diese Guards
    /// bleiben unangetastet: Sie sitzen tiefer und decken Wege ab, die nicht ueber den
    /// Dialog laufen. Der Katalog DUPLIZIERT sie nicht, sondern
    /// <list type="bullet">
    ///   <item><description>liefert dem Senkendialog den Kurzschluss-Text ueber
    ///     <see cref="PruefeSenke"/> — dieselbe Ressource wie bisher, damit die Meldung
    ///     Wort fuer Wort dieselbe bleibt;</description></item>
    ///   <item><description>rechnet den Ring ueber DIESELBE Ebenen-Relaxation wie Dialog
    ///     und Engine (<see cref="Hydraulikbild.Ebenen"/>) statt einer zweiten
    ///     Ringsuche;</description></item>
    ///   <item><description>meldet beim Laufstart NUR ins Protokoll. Kein harter Befund
    ///     dieses Katalogs bricht einen Lauf ab — sonst gaebe es zwei Abbruchstellen fuer
    ///     dieselbe Sache.</description></item>
    /// </list></para>
    ///
    /// <para><b>Dialogfrei</b> (Konzept 13.4) und ohne Oberflaeche aufrufbar: gelesen
    /// wird ueber <see cref="StilleDb"/> — den <c>?</c>-parametrisierten, MessageBox-freien
    /// Weg auf <c>DataRepository.GetConnectionString()</c> — sowie ueber die vorhandenen
    /// Bausteine <see cref="Hydraulikbild"/>, <see cref="Z_AnlageSenkeCtrl"/>,
    /// <see cref="WaermesenkeClass"/> und <see cref="PufferSpCtrl"/>. Spaltentolerant:
    /// Fehlt <c>Z_AnlageSenke</c> (Migrationsschritt 50 nicht gelaufen), gelten die
    /// Altspalten <c>WS_*</c>; fehlen die Klassen-Set-Spalten (Schritt 49), gilt die
    /// Ableitung aus <c>Verwendung</c>.</para>
    /// </summary>
    public static class Warnkriterien
    {
        // =====================================================================
        // Kriteriumsschluessel — sprachneutral und ASCII (Drei-Schichten-Regel,
        // Schicht "Schluessel"). Sie stehen in Protokollen, Filtern und Chips und
        // duerfen NIE angezeigt werden.
        // =====================================================================

        /// <summary>Puffer-Ziel der Senkenzeile liegt ausserhalb des Klassen-Sets.</summary>
        public const string W1_ZIEL_AUSSERHALB_SET = "W1";

        /// <summary>Bauform (<c>Speichertyp</c>) widerspricht dem Klassen-Set.</summary>
        public const string W2_BAUFORM_WIDERSPRUCH = "W2";

        /// <summary>Erzeuger-Vorlauf &lt; <c>VL_eff</c> des Zielspeichers.</summary>
        public const string W3_VORLAUF_ZU_NIEDRIG = "W3";

        /// <summary>RESERVIERT fuer Paket P1: <c>T_Nutz_BW</c> &gt; <c>VL_eff</c>.</summary>
        public const string W4_TNUTZ_UEBER_VLEFF = "W4";

        /// <summary>Quellpuffer ohne einen einzigen Lader.</summary>
        public const string W5_QUELLE_OHNE_LADER = "W5";

        /// <summary>RESERVIERT fuer Paket P1: Schichtung am Leitspeicher eines Verbunds.</summary>
        public const string W6_SCHICHTUNG_AM_VERBUND = "W6";

        /// <summary>
        /// Sole-/Wasser-Wasser-Waermepumpe OHNE konfigurierte Waermequelle
        /// (<c>WQ_Typ</c> leer): Der Lauf rechnet ersatzweise mit der Aussenluft —
        /// fuer diese Bauart ein Kategorienfehler (die Kennlinie ist auf Sole-/
        /// Wassertemperaturen bezogen). Nutzerbefund 27.08.2026 (Booster-Kette 1042);
        /// die echte Quellkopplung kommt mit Paket B1 (Konzept 8.2), bis dahin macht
        /// dieses Kriterium den Zustand sichtbar.
        /// </summary>
        public const string QUELLE_NICHT_KONFIGURIERT = "QUELLE_FEHLT";

        /// <summary>HART: derselbe Speicher ist Quelle UND Ladeziel derselben Anlage.</summary>
        public const string HART_KURZSCHLUSS = "HART_KURZSCHLUSS";

        /// <summary>HART: Ring in der Booster-/Kaskadenkette.</summary>
        public const string HART_RING = "HART_RING";

        /// <summary>HART: leeres Klassen-Set — kein Kanal entlaedt den Speicher.</summary>
        public const string HART_LEERES_SET = "HART_LEERES_SET";

        // =====================================================================
        // Rueckfall-Spreizung fuer VL_eff (Konzept 7.2)
        // =====================================================================

        /// <summary>
        /// Generische Rueckfall-Spreizung [K], wenn ein Speicher kein gepflegtes
        /// Temperaturpaar hat — dieselben 10 K, mit denen
        /// <c>SimulationPufferspeicher.Init</c> dann sein <c>Q_max</c> bildet.
        /// </summary>
        public const double RUECKFALL_DELTA_T = 10;

        /// <summary>
        /// Rueckfall-Spreizung des BHKW-PENDELSPEICHERS [K] (Befund N2): Die Altformel
        /// <c>Liter · 20 / 860</c> hatte 20 K fest verdrahtet, und ein Rueckfall auf
        /// 10 K halbierte seine Kapazitaet ohne fachlichen Grund
        /// (<c>SimulationControl</c>, Aufbau des Ersatz-Pendelspeichers).
        /// </summary>
        public const double RUECKFALL_DELTA_T_PENDELSPEICHER = 20;

        // =====================================================================
        // Oeffentliche Pruefwege
        // =====================================================================

        /// <summary>
        /// Prueft ALLE Anlagen und Speicher eines Projekts; nie <c>null</c>, leer =
        /// keine Beanstandung.
        ///
        /// <para>Reihenfolge der Befunde: erst die harten (Kurzschluss, Ring, leeres
        /// Set), dann die weichen je Anlage in Rangfolge, zuletzt die speicherbezogenen.
        /// Sie ist die Reihenfolge, in der die Befunde ins Protokoll gehen.</para>
        ///
        /// <para>Geprueft werden nur BETEILIGTE Speicher — solche, die eine Senkenzeile
        /// laedt oder die eine Anlage als Quelle fuehrt. Projekt 1023 der Referenzmenge
        /// zeigt, warum: Es traegt ueber 80 Pufferkopien aus wiederholtem „Projekt
        /// duplizieren", von denen genau einer an der Hydraulik teilnimmt.</para>
        /// </summary>
        public static List<Warnbefund> PruefeProjekt(int idProjekt)
        {
            List<Warnbefund> befunde = new List<Warnbefund>();

            Projektbild bild = Projektbild.Lesen(idProjekt);
            if (bild == null) return befunde;

            RingPruefen(bild, befunde);
            SoleOhneQuellePruefen(idProjekt, befunde);

            foreach (int idAnlage in bild.AnlagenReihenfolge)
            {
                List<Z_AnlageSenkeModel> kette = bild.Senken(idAnlage);
                for (int i = 0; i < kette.Count; i++)
                {
                    // Der GESPEICHERTE Rang, nicht die Listenposition: Beim Laufstart
                    // steht er in der Tabelle, und nur er ist die Zahl, die der Anwender
                    // im Dialog sieht. Erst wenn er fehlt, zaehlt die Position.
                    int rang = kette[i] != null && kette[i].Rang > 0 ? kette[i].Rang : i + 1;
                    ZeilePruefen(bild, idAnlage, kette[i], rang, befunde);
                }

                QuelleOhneLaderPruefen(bild, idAnlage, befunde);
            }

            foreach (int idPuffer in bild.BeteiligtePuffer)
                SpeicherPruefen(bild, idPuffer, befunde);

            return befunde;
        }

        /// <summary>
        /// Prueft EINE Senkenzeile — die Sofortpruefung des Senkendialogs; nie
        /// <c>null</c>.
        ///
        /// <para>Geprueft wird die Zeile, wie sie GESPEICHERT WERDEN SOLL, gegen den
        /// vorhandenen Bestand: Der Quellbezug der Anlage, das Klassen-Set und die
        /// Temperaturen des Zielspeichers kommen aus der Datenbank, die Zeile selbst vom
        /// Aufrufer. <paramref name="senke"/> traegt ihren Rang in
        /// <c>Z_AnlageSenkeModel.Rang</c>; ist er 0, erscheint er nicht im Text.</para>
        ///
        /// <para>Projektweite Kriterien (Ring, W5) sind hier NICHT enthalten: Sie haengen
        /// an der Gesamtkonfiguration und nicht an dieser einen Zeile. Den Ring haelt der
        /// Quellendialog ab (<c>WaermesenkeClass.QuellePruefen</c>), W5 meldet der
        /// Laufstart.</para>
        /// </summary>
        public static List<Warnbefund> PruefeSenke(int idProjekt, int idAnlage,
                                                   Z_AnlageSenkeModel senke)
        {
            List<Warnbefund> befunde = new List<Warnbefund>();
            if (senke == null) return befunde;

            Projektbild bild = Projektbild.Lesen(idProjekt);
            if (bild == null) return befunde;

            ZeilePruefen(bild, idAnlage, senke, senke.Rang, befunde);
            return befunde;
        }

        /// <summary>
        /// Dieselbe Pruefung fuer eine GANZE Senkenliste — der Weg, den der Senkendialog
        /// beim Speichern geht. Sie liest das Projektbild EINMAL statt je Zeile; bei
        /// vier Raengen waeren das sonst sechzehn Abfragen fuer einen Knopfdruck.
        ///
        /// <para>Der RANG kommt aus der Listenposition, nicht aus
        /// <c>Z_AnlageSenkeModel.Rang</c>: Im Dialog wird er erst beim Speichern
        /// festgeschrieben, und bis dahin zaehlt allein die Reihenfolge.</para>
        /// </summary>
        public static List<Warnbefund> PruefeSenken(int idProjekt, int idAnlage,
                                                    IList<Z_AnlageSenkeModel> senken)
        {
            List<Warnbefund> befunde = new List<Warnbefund>();
            if (senken == null || senken.Count == 0) return befunde;

            Projektbild bild = Projektbild.Lesen(idProjekt);
            if (bild == null) return befunde;

            for (int i = 0; i < senken.Count; i++)
                ZeilePruefen(bild, idAnlage, senken[i], i + 1, befunde);

            return befunde;
        }

        // =====================================================================
        // Hilfsgroessen, die auch die Oberflaeche braucht
        // =====================================================================

        /// <summary>
        /// Die KANAELE, die ein Senkenziel bedient sehen will — die Abbildung, gegen die
        /// W1 das Klassen-Set haelt. Leeres Feld = Direktsenke oder unbekanntes Ziel
        /// (dann gibt es nichts zu pruefen).
        ///
        /// <code>
        ///   PufferHeizung       -> { HEIZUNG }
        ///   PufferBrauchwasser  -> { BRAUCHWASSER }
        ///   PufferProzess       -> { PROZESS }
        ///   PufferKombi         -> { HEIZUNG, BRAUCHWASSER }
        /// </code>
        /// </summary>
        public static int[] ZielKanaele(string ziel)
        {
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                return new[] { Kanal.HEIZUNG };
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return new[] { Kanal.BRAUCHWASSER };
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_PROZESS, StringComparison.Ordinal))
                return new[] { Kanal.PROZESS };
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                return new[] { Kanal.HEIZUNG, Kanal.BRAUCHWASSER };

            return new int[0];
        }

        /// <summary>
        /// Das WIRKSAME Vorlaufniveau <c>VL_eff</c> eines Speichers [°C] nach der
        /// Bestandsregel (Konzept 7.2):
        ///
        /// <code>
        ///   Delta = Vorlauf − Ruecklauf
        ///   Delta &gt;  0  ->  VL_eff = Vorlauf                       (gepflegtes Paar)
        ///   Delta &lt;= 0  ->  VL_eff = Ruecklauf + Rueckfall-Delta_T  (10 K; 20 K am
        ///                                                             BHKW-Pendelspeicher)
        /// </code>
        ///
        /// Dieselbe Rueckfallregel, mit der <c>SimulationPufferspeicher.Init</c> ohne
        /// Temperaturpaar sein <c>Q_max</c> bildet — dort als Spreizung, hier als
        /// absolutes Niveau. Ein Speicher ohne jede Temperaturangabe kommt damit auf
        /// <c>VL_eff</c> = 10 °C und loest W3 nie aus; das ist gewollt, denn eine
        /// Warnung aus Unkenntnis waere schlechter als keine.
        /// </summary>
        public static double WirksamerVorlauf(int vorlauf, int ruecklauf, string bezeichner)
        {
            if (vorlauf - ruecklauf > 0) return vorlauf;

            double delta = string.Equals(bezeichner, DbWerte.PSP_BEZ_PENDELSPEICHER,
                                         StringComparison.Ordinal)
                ? RUECKFALL_DELTA_T_PENDELSPEICHER
                : RUECKFALL_DELTA_T;

            return ruecklauf + delta;
        }

        /// <summary>Die weichen Befunde einer Liste; nie <c>null</c>.</summary>
        public static List<Warnbefund> NurWeiche(List<Warnbefund> befunde)
        {
            List<Warnbefund> gefiltert = new List<Warnbefund>();
            if (befunde != null)
                foreach (Warnbefund b in befunde)
                    if (b != null && !b.Hart) gefiltert.Add(b);
            return gefiltert;
        }

        /// <summary>Der ERSTE harte Befund einer Liste; <c>null</c> = keiner.</summary>
        public static Warnbefund ErsterHarter(List<Warnbefund> befunde)
        {
            if (befunde != null)
                foreach (Warnbefund b in befunde)
                    if (b != null && b.Hart) return b;
            return null;
        }

        // =====================================================================
        // Die Kriterien
        // =====================================================================

        /// <summary>W1, W3 und der Kurzschluss — alles, was an EINER Senkenzeile haengt.</summary>
        private static void ZeilePruefen(Projektbild bild, int idAnlage,
                                         Z_AnlageSenkeModel senke, int rang,
                                         List<Warnbefund> befunde)
        {
            if (senke == null || senke.ID_Puffer <= 0) return;

            int[] kanaele = ZielKanaele(senke.Ziel);
            if (kanaele.Length == 0) return;              // Direktsenke: kein Speicherbezug

            Pufferdaten p = bild.Puffer(senke.ID_Puffer);
            if (p == null) return;                        // Puffer eines fremden Projekts o. Ae.

            // --- HART: Quelle und Ladeziel derselben Anlage ---------------------------
            //
            // Wort fuer Wort dieselbe Meldung wie der Bestandsguard in
            // Form_Waermesenke.ListePruefen — der Dialog uebernimmt sie von hier, statt
            // sie ein zweites Mal zu bauen.
            //
            // GEFRAGT IST DIE ANZEIGE-AUFLOESUNG des Quellpuffers (Fremdschluessel,
            // sonst Alt-Bezeichner), nicht die Engine-Auflösung: Genau die benutzt der
            // abgeloeste Dialogguard (WaermesenkeClass.QuellPufferDerAnlage), und der
            // Katalog darf einen Fall, den der Dialog bisher gesperrt hat, nicht
            // durchlassen. W5 und der Ring fragen umgekehrt die ENGINE-Auflösung — was
            // die Engine nie aufbaut, kann weder leerlaufen noch einen Ring schliessen.
            if (bild.QuellpufferAnzeige(idAnlage) == senke.ID_Puffer)
                befunde.Add(Befund(HART_KURZSCHLUSS, true, idAnlage, p.ID,
                    string.Format(
                        Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_PUFFER_QUELLE_UND_SENKE),
                        p.Anzeigename)));

            // --- W1: Ziel ausserhalb des Klassen-Sets ---------------------------------
            List<string> fehlend = new List<string>();
            foreach (int k in kanaele)
                if (!p.Set_BedientKanal(k)) fehlend.Add(KanalAnzeige(k));

            if (fehlend.Count > 0)
                befunde.Add(Befund(W1_ZIEL_AUSSERHALB_SET, false, idAnlage, p.ID,
                    string.Format(MyResource.Resource.SIMWARN_W1_ZIEL_AUSSERHALB_SET,
                                  bild.Anlagenname(idAnlage), RangText(rang),
                                  p.Anzeigename, Form_Waermesenke.ZielAnzeige(senke.Ziel),
                                  KlassenSetAnzeige(p.Set), Verbinden(fehlend))));

            // --- W3: Erzeuger-Vorlauf unter VL_eff ------------------------------------
            //
            // Nur bei GEPFLEGTEM Erzeuger-Vorlauf: 0 heisst „nicht angegeben", nicht
            // „0 °C". Der T_Nutz-Anteil dieses Kriteriums kommt mit Paket P1.
            int vorlaufAnlage = bild.AnlagenVorlauf(idAnlage);
            double vlEff = p.VL_eff;

            if (vorlaufAnlage > 0 && vlEff > 0 && vorlaufAnlage < vlEff)
                befunde.Add(Befund(W3_VORLAUF_ZU_NIEDRIG, false, idAnlage, p.ID,
                    string.Format(MyResource.Resource.SIMWARN_W3_VORLAUF_ZU_NIEDRIG,
                                  bild.Anlagenname(idAnlage), Grad(vorlaufAnlage),
                                  Grad(vlEff), p.Anzeigename)));
        }

        /// <summary>W2 und das leere Klassen-Set — was an EINEM Speicher haengt.</summary>
        private static void SpeicherPruefen(Projektbild bild, int idPuffer,
                                            List<Warnbefund> befunde)
        {
            Pufferdaten p = bild.Puffer(idPuffer);
            if (p == null) return;

            // --- HART: leeres Klassen-Set ---------------------------------------------
            //
            // AUF HEUTIGEN DATEN NICHT ERREICHBAR, und das mit Absicht:
            // PufferSpCtrl.KlassenSetAusZeile faellt bei fehlenden oder durchweg
            // falschen Flags geordnet auf die Ableitung aus Verwendung zurueck, und die
            // liefert im schlechtesten Fall {Heizung}. Der Guard ist das NETZ fuer die
            // programmatischen Schreibwege (dieselbe Rolle wie die Hebung in
            // PufferSpCtrl.KlassenSetBestimmen) und fuer den Tag, an dem die
            // Verwendungs-Altlast mit Paket A1 stillgelegt wird.
            if (p.Set.Leer)
            {
                befunde.Add(Befund(HART_LEERES_SET, true, 0, p.ID,
                    string.Format(MyResource.Resource.SIMWARN_HART_LEERES_SET, p.Anzeigename)));
                return;                                   // ohne Set ist W2 nicht bewertbar
            }

            // --- W2: Bauform gegen Klassen-Set ----------------------------------------
            //
            // Geprueft wird die eine Richtung, die das Konzept nennt (6.2: „der vom
            // Auftrag genannte Fall Warmwasserpuffer fuer Heizung"): eine Bauform, die
            // fuer die WARMWASSERbereitung gebaut ist — Kombispeicher (ein Behaelter,
            // zwei Zonen) und Solarspeicher (klassisch der Trinkwasserspeicher der
            // Solaranlage) —, deren Klassen-Set den Brauchwasserkanal aber gar nicht
            // enthaelt. Dann bleibt die Warmwasserseite des Behaelters ungenutzt.
            //
            // Die Gegenrichtung (reine Pufferspeicher-Bauform mit Brauchwasser im Set)
            // ist AUSDRUECKLICH KEIN Befund: Ein Pufferspeicher mit Frischwasserstation
            // ist der haeufigste Weg zur Warmwasserbereitung ueberhaupt — eine Warnung
            // darauf waere Rauschen. Auch die Prozessklasse bewertet W2 nicht: Fuer
            // Prozesswaerme gibt es keine eigene Bauform im Katalog.
            //
            // Eine LEERE Bauform ist kein Befund (Konzept 6.2: „nur pruefen, wenn
            // Speichertyp gepflegt ist").
            if (!p.BauformWarmwasserseitig || p.Set.Brauchwasser) return;

            befunde.Add(Befund(W2_BAUFORM_WIDERSPRUCH, false, 0, p.ID,
                string.Format(MyResource.Resource.SIMWARN_W2_BAUFORM_WIDERSPRUCH,
                              p.Anzeigename, p.Speichertyp, KlassenSetAnzeige(p.Set))));
        }

        /// <summary>
        /// QUELLE_FEHLT — Sole-/Wasser-Wasser-Waermepumpen ohne konfigurierte
        /// Waermequelle (Nutzerbefund 27.08.2026, Booster-Kette). Eine solche Anlage
        /// rechnet heute ersatzweise mit der Aussenluft — fuer diese Bauarten fachlich
        /// falsch (die Kennlinie ist auf Sole-/Wassertemperaturen bezogen). Eigene
        /// stille Abfrage statt einer Erweiterung des Projektbilds: Die Bauart
        /// (<c>Tab_WP.Typ</c>) braucht sonst kein Kriterium.
        /// </summary>
        private static void SoleOhneQuellePruefen(int idProjekt, List<Warnbefund> befunde)
        {
            DataTable dt = StilleDb.Tabelle(
                "SELECT a.ID, a.Bezeichner, a.WQ_Typ, w.Typ AS Bauart " +
                "FROM Tab_Energieanlagen AS a INNER JOIN Tab_WP AS w ON a.ID_WP = w.ID " +
                "WHERE a.ID_Projekt = ? AND a.ID_Type = " + WizardItemClass.WP_TYP,
                new OleDbParameter("@p", idProjekt));
            if (dt == null) return;

            foreach (DataRow r in dt.Rows)
            {
                string bauart = StilleDb.Text(StilleDb.Feld(r, "Bauart")).Trim();
                if (bauart != DbWerte.WP_BAUART_SOLE_WASSER &&
                    bauart != DbWerte.WP_BAUART_WASSER_WASSER) continue;

                if (StilleDb.Text(StilleDb.Feld(r, "WQ_Typ")).Trim().Length > 0) continue;

                int idAnlage = (int)StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                befunde.Add(Befund(QUELLE_NICHT_KONFIGURIERT, false, idAnlage, 0,
                    string.Format(MyResource.Resource.SIMWARN_QUELLE_FEHLT,
                                  StilleDb.Text(StilleDb.Feld(r, "Bezeichner")), bauart)));
            }
        }

        /// <summary>W5 — Quellpuffer ohne einen einzigen Lader.</summary>
        private static void QuelleOhneLaderPruefen(Projektbild bild, int idAnlage,
                                                   List<Warnbefund> befunde)
        {
            int idQuelle = bild.Quellpuffer(idAnlage);
            if (idQuelle <= 0) return;
            if (bild.Lader(idQuelle).Count > 0) return;

            Pufferdaten p = bild.Puffer(idQuelle);
            string name = p != null ? p.Anzeigename : WaermesenkeClass.PufferName(idQuelle);

            befunde.Add(Befund(W5_QUELLE_OHNE_LADER, false, idAnlage, idQuelle,
                string.Format(MyResource.Resource.SIMWARN_W5_QUELLE_OHNE_LADER,
                              bild.Anlagenname(idAnlage), name)));
        }

        /// <summary>
        /// HART: Ring in der Kaskadenkette — ueber DIESELBE Ebenen-Relaxation, mit der
        /// die Engine abbricht und der Quellendialog vorbeugt
        /// (<see cref="Hydraulikbild.Ebenen"/>). Eine eigene Ringsuche daneben waere eine
        /// zweite Auslegung derselben Frage.
        /// </summary>
        private static void RingPruefen(Projektbild bild, List<Warnbefund> befunde)
        {
            bool ring;
            Dictionary<int, int> ebene = bild.Bild.Ebenen(0, 0, out ring);
            if (!ring) return;

            befunde.Add(Befund(HART_RING, true, 0, 0,
                string.Format(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMWARN_HART_RING),
                    bild.Bild.RingBeteiligte(ebene, 0, 0))));
        }

        // =====================================================================
        // Anzeige-Bausteine
        // =====================================================================

        /// <summary>
        /// Das Klassen-Set als Anzeigetext, z. B. „Heizung + Brauchwasser".
        ///
        /// <c>internal</c>, nicht <c>public</c>: <c>PufferSpCtrl</c> ist selbst
        /// assemblyintern, und eine öffentliche Signatur mit internem Parametertyp
        /// übersetzt der Compiler nicht (CS0051). Alle Aufrufer liegen ohnehin in
        /// dieser Assembly.
        /// </summary>
        internal static string KlassenSetAnzeige(PufferSpCtrl.KlassenSet set)
        {
            if (set == null || set.Leer) return MyResource.Resource.PSP_KLASSENSET_LEER;

            List<string> teile = new List<string>();
            if (set.Heizung) teile.Add(KanalAnzeige(Kanal.HEIZUNG));
            if (set.Brauchwasser) teile.Add(KanalAnzeige(Kanal.BRAUCHWASSER));
            if (set.Prozess) teile.Add(KanalAnzeige(Kanal.PROZESS));

            return Verbinden(teile);
        }

        /// <summary>Anzeigename eines Kanals (Schicht „Anzeige", nie <c>Kanal.Name</c>).</summary>
        public static string KanalAnzeige(int kanal)
        {
            switch (kanal)
            {
                case Kanal.BRAUCHWASSER: return MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE;
                case Kanal.PROZESS: return MyResource.Resource.KANAL_PROZESS_ANZEIGE;
                default: return MyResource.Resource.KANAL_HEIZUNG_ANZEIGE;
            }
        }

        private static string Verbinden(List<string> teile)
        {
            return teile.Count > 0
                ? string.Join(MyResource.Resource.SIMWARN_TRENNER, teile.ToArray())
                : MyResource.Resource.PSP_KLASSENSET_LEER;
        }

        /// <summary>Rangangabe fuer die Meldung; „–", solange kein Rang feststeht.</summary>
        private static string RangText(int rang)
        {
            return rang > 0 ? rang.ToString(CultureInfo.CurrentCulture) : "–";
        }

        private static string Grad(double wert)
        {
            return wert.ToString("0.#", CultureInfo.CurrentCulture);
        }

        private static Warnbefund Befund(string kriterium, bool hart, int idAnlage,
                                         int idPuffer, string text)
        {
            return new Warnbefund
            {
                Kriterium = kriterium,
                Hart = hart,
                ID_Anlage = idAnlage,
                ID_Puffer = idPuffer,
                Text = text ?? ""
            };
        }

        // =====================================================================
        // Ein Speicher, so wie der Katalog ihn braucht
        // =====================================================================

        /// <summary>
        /// Die Puffer-Zeile in der Auflösung des Katalogs: Klassen-Set, Bauform und
        /// Temperaturen. Aufgebaut aus EINER Projektabfrage
        /// (<c>SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?</c>) — der
        /// <c>*</c>-Zugriff ist hier der spaltentolerante Weg: Ob die Flags des
        /// Schemastands 49 vorhanden sind, entscheidet
        /// <c>PufferSpCtrl.KlassenSetAusZeile</c> an der DataRow, nicht die Abfrage.
        /// </summary>
        private sealed class Pufferdaten
        {
            public int ID;
            public string Bezeichner = "";
            public string Speichertyp = "";
            public int Vorlauf;
            public int Ruecklauf;
            public PufferSpCtrl.KlassenSet Set;

            /// <summary>Name fuer Meldungen; der Ersatztext, wenn kein Bezeichner gepflegt ist.</summary>
            public string Anzeigename
            {
                get
                {
                    return Bezeichner.Length > 0
                        ? Bezeichner : MyResource.Resource.PSP_BEZEICHNER_ERSATZ;
                }
            }

            /// <summary>Wirksames Vorlaufniveau [°C], siehe <see cref="WirksamerVorlauf"/>.</summary>
            public double VL_eff
            {
                get { return WirksamerVorlauf(Vorlauf, Ruecklauf, Bezeichner); }
            }

            /// <summary>
            /// true, wenn die BAUFORM auf Warmwasser ausgelegt ist — Kombispeicher und
            /// Solarspeicher. Die Bauform ist ein Persistenzwert (deutsch, eingefroren);
            /// verglichen wird deshalb gegen <see cref="DbWerte"/>, nicht gegen einen
            /// Anzeigetext. Gross-/Kleinschreibung wird toleriert (Befund L0-1: Aeltere
            /// Staende haben den lokalisierten ComboBox-Text in die Spalte geschrieben).
            /// </summary>
            public bool BauformWarmwasserseitig
            {
                get
                {
                    return string.Equals(Speichertyp, DbWerte.PSP_SPEICHERTYP_KOMBI,
                                         StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(Speichertyp, DbWerte.PSP_SPEICHERTYP_SOLAR,
                                         StringComparison.OrdinalIgnoreCase);
                }
            }

            public bool Set_BedientKanal(int kanal)
            {
                if (Set == null) return false;
                switch (kanal)
                {
                    case Kanal.BRAUCHWASSER: return Set.Brauchwasser;
                    case Kanal.PROZESS: return Set.Prozess;
                    default: return Set.Heizung;
                }
            }
        }

        // =====================================================================
        // Das Projektbild — EINMAL lesen, alle Kriterien bedienen
        // =====================================================================

        /// <summary>
        /// Alles, was der Katalog ueber ein Projekt wissen muss, in drei Abfragen:
        /// die Verschaltung (<see cref="Hydraulikbild"/>), die Speicherzeilen und die
        /// Senkenlisten. Ein Aufruf je Kriterium waere bei fuenf Erzeugern und drei
        /// Speichern ein Dutzend Rundreisen fuer dieselbe Auskunft.
        /// </summary>
        private sealed class Projektbild
        {
            public Hydraulikbild Bild;
            public readonly List<int> AnlagenReihenfolge = new List<int>();
            public readonly List<int> BeteiligtePuffer = new List<int>();

            private readonly Dictionary<int, Pufferdaten> _puffer =
                new Dictionary<int, Pufferdaten>();
            private readonly Dictionary<int, List<Z_AnlageSenkeModel>> _senken =
                new Dictionary<int, List<Z_AnlageSenkeModel>>();

            /// <summary>
            /// Die Projekt-Puffer in der Bestandsdarstellung — gebraucht allein von
            /// <see cref="QuellpufferAnzeige"/>, das die Alt-Bezeichner-Aufloesung
            /// genau ueber diese Liste macht.
            /// </summary>
            private List<WaermesenkeClass.PufferInfo> _pufferInfo =
                new List<WaermesenkeClass.PufferInfo>();

            public static Projektbild Lesen(int idProjekt)
            {
                if (idProjekt <= 0) return null;

                Hydraulikbild bild = Hydraulikbild.Lesen(idProjekt);
                if (bild == null) return null;

                Projektbild pb = new Projektbild();
                pb.Bild = bild;

                foreach (Hydraulikbild.AnlagenEintrag a in bild.Anlagen)
                    pb.AnlagenReihenfolge.Add(a.ID);

                pb.PufferLesen(idProjekt);
                pb.SenkenLesen(idProjekt);
                pb.BeteiligteSammeln();

                return pb;
            }

            /// <summary>Die Senkenzeilen einer Anlage in Rangfolge; nie <c>null</c>.</summary>
            public List<Z_AnlageSenkeModel> Senken(int idAnlage)
            {
                List<Z_AnlageSenkeModel> kette;
                return _senken.TryGetValue(idAnlage, out kette)
                    ? kette : new List<Z_AnlageSenkeModel>();
            }

            /// <summary>Ein Speicher des Projekts; <c>null</c>, wenn er nicht dazugehoert.</summary>
            public Pufferdaten Puffer(int idPuffer)
            {
                Pufferdaten p;
                return _puffer.TryGetValue(idPuffer, out p) ? p : null;
            }

            /// <summary>Quellpuffer einer Anlage (Engine-Wahrheit: Fremdschluessel, WP/Kessel); 0 = keiner.</summary>
            public int Quellpuffer(int idAnlage)
            {
                int id;
                return Bild.QuelleJeAnlage.TryGetValue(idAnlage, out id) ? id : 0;
            }

            /// <summary>
            /// Quellpuffer in der ANZEIGE-Auflösung (Fremdschluessel, sonst
            /// Alt-Bezeichner) — dieselbe, die <c>WaermesenkeClass.QuellPufferDerAnlage</c>
            /// und die Erzeugerkarte benutzen. Sie ist die weitere der beiden und
            /// deshalb die richtige fuer einen GUARD.
            /// </summary>
            public int QuellpufferAnzeige(int idAnlage)
            {
                return Bild.QuellpufferAnzeige(idAnlage, _pufferInfo);
            }

            /// <summary>Die Anlagen, die einen Speicher laden — ueber ALLE Raenge.</summary>
            public List<int> Lader(int idPuffer)
            {
                return Bild.Lader(idPuffer);
            }

            public string Anlagenname(int idAnlage)
            {
                return Bild.Name(idAnlage);
            }

            public int AnlagenVorlauf(int idAnlage)
            {
                Hydraulikbild.AnlagenEintrag a;
                return Bild.JeId.TryGetValue(idAnlage, out a) ? a.Vorlauf : 0;
            }

            // --- Innenleben --------------------------------------------------------

            private void PufferLesen(int idProjekt)
            {
                _pufferInfo = WaermesenkeClass.ProjektPufferListe(idProjekt, null);
                if (_pufferInfo == null) _pufferInfo = new List<WaermesenkeClass.PufferInfo>();

                DataTable dt = StilleDb.Tabelle(
                    "SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?",
                    StilleDb.Par("@proj", OleDbType.Integer, idProjekt));
                if (dt == null) return;

                foreach (DataRow r in dt.Rows)
                {
                    int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                    if (id <= 0 || _puffer.ContainsKey(id)) continue;

                    _puffer[id] = new Pufferdaten
                    {
                        ID = id,
                        Bezeichner = StilleDb.Text(StilleDb.Feld(r, "Bezeichner")),
                        Speichertyp = StilleDb.Text(StilleDb.Feld(r, "Speichertyp")).Trim(),
                        Vorlauf = StilleDb.Zahl(StilleDb.Feld(r, "Vorlauf")),
                        Ruecklauf = StilleDb.Zahl(StilleDb.Feld(r, "Ruecklauf")),
                        Set = PufferSpCtrl.KlassenSetAusZeile(r)
                    };
                }
            }

            /// <summary>
            /// Die geordneten Senkenlisten des Projekts. Fuer eine Anlage OHNE Zeile in
            /// <c>Z_AnlageSenke</c> gelten die Slot-Daten aus <see cref="Hydraulikbild"/>
            /// (Rang-1-Vorbelegung Heizkreis/Beides) — seit Paket A1 der einzige
            /// Rueckfall: Die WS_-Spiegelung ist abgerissen, und die Migrationspflicht
            /// (Schritt 50 laeuft vor jedem Programmstart) macht den Fall „Tabelle
            /// fehlt" unerreichbar.
            ///
            /// <para>Dabei wird auch die Laderabbildung des Hydraulikbilds ERGAENZT: Sie
            /// entsteht dort aus den zwei Altspalten und kennt die Raenge ab 3 nicht.
            /// Fuer W5 („kein Lader") und den Ring waere das eine falsche Antwort — ein
            /// Speicher, den nur eine drittrangige Senke laedt, gaelte sonst als
            /// ladelos.</para>
            /// </summary>
            private void SenkenLesen(int idProjekt)
            {
                List<Z_AnlageSenkeModel> alle = new Z_AnlageSenkeCtrl().LesenJeProjekt(idProjekt);

                foreach (Z_AnlageSenkeModel z in alle)
                {
                    if (z == null || z.ID_Anlage <= 0) continue;

                    List<Z_AnlageSenkeModel> kette;
                    if (!_senken.TryGetValue(z.ID_Anlage, out kette))
                    {
                        kette = new List<Z_AnlageSenkeModel>();
                        _senken[z.ID_Anlage] = kette;
                    }
                    kette.Add(z);
                }

                foreach (Hydraulikbild.AnlagenEintrag a in Bild.Anlagen)
                    if (!_senken.ContainsKey(a.ID))
                        _senken[a.ID] = AusAltspalten(a);

                LaderErgaenzen();
            }

            /// <summary>Die zwei Altslots einer Anlage als Senkenliste (Rang 1 und 2).</summary>
            private static List<Z_AnlageSenkeModel> AusAltspalten(Hydraulikbild.AnlagenEintrag a)
            {
                List<Z_AnlageSenkeModel> kette = new List<Z_AnlageSenkeModel>();

                kette.Add(new Z_AnlageSenkeModel
                {
                    ID_Anlage = a.ID,
                    Rang = 1,
                    Ziel = a.Senke.Ziel,
                    Bedarfsart = a.Senke.Bedarfsart,
                    ID_Puffer = a.Senke.ID_Puffer
                });

                if (a.Senke.HatZweitsenke)
                    kette.Add(new Z_AnlageSenkeModel
                    {
                        ID_Anlage = a.ID,
                        Rang = 2,
                        Ziel = a.Senke.Ziel2,
                        Bedarfsart = a.Senke.Bedarfsart,
                        ID_Puffer = a.Senke.ID_Puffer2
                    });

                return kette;
            }

            /// <summary>Traegt die Lader ab Rang 3 in die Abbildung des Hydraulikbilds nach.</summary>
            private void LaderErgaenzen()
            {
                foreach (KeyValuePair<int, List<Z_AnlageSenkeModel>> e in _senken)
                    foreach (Z_AnlageSenkeModel z in e.Value)
                    {
                        if (z == null || z.ID_Puffer <= 0) continue;
                        if (ZielKanaele(z.Ziel).Length == 0) continue;

                        List<int> lader;
                        if (!Bild.LaderJePuffer.TryGetValue(z.ID_Puffer, out lader))
                        {
                            lader = new List<int>();
                            Bild.LaderJePuffer[z.ID_Puffer] = lader;
                        }
                        if (!lader.Contains(e.Key)) lader.Add(e.Key);
                    }
            }

            /// <summary>
            /// Die Speicher, die an der Hydraulik teilnehmen: Ladeziel einer Senkenzeile
            /// oder Waermequelle einer Anlage. Alles andere ist Kopienballast und wird
            /// nicht bewertet (siehe <see cref="PruefeProjekt"/>).
            /// </summary>
            private void BeteiligteSammeln()
            {
                foreach (KeyValuePair<int, List<Z_AnlageSenkeModel>> e in _senken)
                    foreach (Z_AnlageSenkeModel z in e.Value)
                        if (z != null && z.ID_Puffer > 0 && ZielKanaele(z.Ziel).Length > 0)
                            Aufnehmen(z.ID_Puffer);

                foreach (KeyValuePair<int, int> q in Bild.QuelleJeAnlage) Aufnehmen(q.Value);
            }

            private void Aufnehmen(int idPuffer)
            {
                if (idPuffer <= 0 || !_puffer.ContainsKey(idPuffer)) return;
                if (!BeteiligtePuffer.Contains(idPuffer)) BeteiligtePuffer.Add(idPuffer);
            }
        }
    }
}
