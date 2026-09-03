using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Schweregrade einer Kohärenzzeile — sprachneutrale ASCII-Schlüssel (Schicht 2 der
    /// Drei-Schichten-Regel). Kein Anzeigetext: Die Beschriftung holt die Oberfläche
    /// über <c>MyResource</c>.
    /// </summary>
    public static class KohaerenzSchwere
    {
        /// <summary>Entlastung gebucht, Belastung im Preis fehlt (Fall 2 des Konzepts § 4.1).</summary>
        public const string WARNUNG = "WARNUNG";

        /// <summary>Belastung ohne Entlastung bzw. abweichender Satz (Fälle 3 und 4);
        /// seit FX5-b auch die Mischlage § 53/§ 53a neben § 54 (Fall 5).</summary>
        public const string HINWEIS = "HINWEIS";
    }

    /// <summary>
    /// EINE Zeile der Kohärenzprüfung (Konzept BHKW-Wirtschaftlichkeit § 4.1, BW2;
    /// Entscheidung BF2 „nur warnen" vom 30.08.2026).
    ///
    /// <para><b>Reine Ausgabe.</b> Die Zeile ändert keinen Rechenwert — sie benennt den
    /// Widerspruch zwischen einer gebuchten Steuergutschrift und dem Steueranteil, den
    /// der erfasste Energiepreis ausweist. Der <see cref="Betrag"/> ist immer die
    /// TATSÄCHLICH gebuchte Gutschrift aus dem einen Rechenweg
    /// (<see cref="SteuerGutschriftRechner"/>), nie eine Zweitrechnung.</para>
    /// </summary>
    public class KohaerenzHinweis
    {
        /// <summary>Steuerwert aus <see cref="KohaerenzSchwere"/>.</summary>
        public string Schwere = KohaerenzSchwere.HINWEIS;

        /// <summary>Fertig formatierter Anzeigetext (Sprache über <c>MyResource</c>).</summary>
        public string Text = "";

        /// <summary>Betroffener Betrag [€/a]; <c>null</c> = ohne Betrag (Fälle 3 und 4).</summary>
        public double? Betrag;
    }

    /// <summary>
    /// Die Größen EINES Wirtschaftlichkeitslaufs, welche die Kohärenzprüfung braucht.
    /// Alle Werte stammen aus dem Lauf selbst — die Prüfung rechnet nichts nach.
    /// </summary>
    internal sealed class KohaerenzLauf
    {
        /// <summary>Kalenderjahr des ersten Betrachtungsjahres (Förderbeginn) — bestimmt,
        /// welcher Katalogsatz für Fall 4 gilt.</summary>
        public int Jahr;

        /// <summary>Die Eingabe der Steuerrechnung dieses Laufs (Anlagen mit Träger,
        /// Heizwerten, Katalogschlüsseln und der gewählten Norm). <c>null</c> = kein
        /// Steuerpfad im Lauf; dann gibt es nichts zu prüfen.</summary>
        public SteuerEingabe Steuer;

        /// <summary>Schalter <c>Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden</c>:
        /// Nur wenn er an ist, wirkt der Strom-Aufschlagsblock überhaupt auf den Preis.</summary>
        public bool AufschlaegeAnwenden;

        /// <summary>Gebuchte Energiesteuer-Entlastung im Jahr 1 [€/a].</summary>
        public double EnergiesteuerEur;

        /// <summary>Gebuchte Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 im Jahr 1 [€/a].</summary>
        public double StromsteuerBefreiungEur;

        /// <summary>Gebuchte Stromsteuer-Entlastung § 9b im Jahr 1 [€/a].</summary>
        public double StromsteuerEntlastungEur;
    }

    /// <summary>
    /// ETAPPE B2 — Kohärenzprüfung nach Leitentscheidung BW2 des Konzepts
    /// <c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md</c> (§ 4.1, Vier-Fall-Tabelle).
    ///
    /// <para><b>Reine Leselogik, ergebnisneutral.</b> Entscheidung BF2 (30.08.2026)
    /// lautet: <i>nur warnen</i>. Die Klasse verändert deshalb keine Gutschrift, keine
    /// Reihe und keinen Kapitalwert — sie liest den Steueranteil, den der erfasste
    /// Energiepreis ausweist, vergleicht ihn mit der gebuchten Entlastung und liefert
    /// Hinweiszeilen. Eine stille Rechenänderung an einer Steuergröße wäre schlimmer als
    /// eine sichtbare Lücke (Konzept § 9, BF2).</para>
    ///
    /// <para><b>Die vier Fälle des § 4.1:</b>
    /// <list type="number">
    ///   <item>Entlastung gewählt, Anteil im Preis aktiv → <b>keine Zeile</b>. Die
    ///         positive Nennung kommt mit der Herleitungstafel (Etappe B6).</item>
    ///   <item>Entlastung gebucht, Anteil inaktiv oder 0 → <b>WARNUNG mit Betrag</b>.</item>
    ///   <item>Anteil aktiv, Entlastung nicht gewählt → <b>HINWEIS ohne Betrag</b>. Den
    ///         entgangenen Betrag zu beziffern hieße, die Entlastung ein zweites Mal zu
    ///         rechnen — genau das unterbleibt hier.</item>
    ///   <item>Gepflegter Anteil ≠ Katalogsatz des Jahres → <b>HINWEIS</b>, beide Sätze
    ///         genannt; Toleranz <see cref="TOLERANZ_CT_KWH"/>.</item>
    /// </list></para>
    ///
    /// <para><b>PAKET FX5-b (Anwenderentscheid 03.09.2026, offener Punkt S-2) — Fall 5
    /// neben den vieren des Konzepts:</b> Stehen im selben Projekt eine Entlastung nach
    /// § 53 / § 53a Abs. 5 EnergieStG und eine nach § 54 nebeneinander (Projekt- oder
    /// Anlagenwahl, seit B3a je Anlage möglich), erscheint ein <b>HINWEIS</b> mit den
    /// beteiligten Anlagen und ihren Wahlen — ohne Betrag, ohne Sperre, ohne
    /// Zahlenänderung (<see cref="MischlageEnergiesteuer"/>).</para>
    ///
    /// <para><b>Zwei Preisseiten mit zwei Leseregeln.</b> Für Brennstoffe gilt
    /// <see cref="BrennstoffBestandteilCtrl"/>: <c>NULL</c> heißt „kein Anteil erfasst"
    /// und bleibt <c>null</c>. Für Strom gilt der ältere <see cref="StromAufschlagCtrl"/>,
    /// dessen Leseweg <c>NULL</c> auf den <b>Vorschlagssatz</b> 2,05 ct/kWh zurückfallen
    /// lässt (E5-Falle, Konzept § 5.1). Ob ein Stromsteueranteil wirklich GEPFLEGT ist,
    /// erkennt man deshalb nur an der rohen Spalte — <see cref="StromsteuerRoh"/> liest
    /// sie eigens. Der Vorschlagswert taugt für Fall 2 (er wirkt im Preis wie ein
    /// gepflegter Wert, sobald der Aufschlagsschalter an ist), aber nicht für Fall 4:
    /// Einen nie erfassten Wert mit dem Katalog zu vergleichen wäre ein Vergleich des
    /// Katalogs mit sich selbst.</para>
    /// </summary>
    internal static class KohaerenzPruefung
    {
        /// <summary>Toleranz des Satzvergleichs in Fall 4 [ct/kWh] (Konzept § 4.1).</summary>
        public const double TOLERANZ_CT_KWH = 0.005;

        /// <summary>Gigajoule je Megawattstunde — für Sätze in EUR/GJ.</summary>
        private const double GJ_JE_MWH = 3.6;

        // =====================================================================
        // Einstieg
        // =====================================================================

        /// <summary>
        /// Prüft die Kohärenz zwischen gebuchten Steuergutschriften und den Steueranteilen
        /// der erfassten Energiepreise. Leere Liste = alles konsistent (Fall 1) oder kein
        /// Steuerpfad im Lauf.
        /// </summary>
        /// <param name="idProjekt">Projekt, dessen Preiszerlegung gelesen wird.</param>
        /// <param name="lauf">Die Größen des Laufs; <c>null</c> ergibt eine leere Liste.</param>
        internal static List<KohaerenzHinweis> Pruefe(int idProjekt, KohaerenzLauf lauf)
        {
            var liste = new List<KohaerenzHinweis>();
            if (idProjekt <= 0) return liste;

            CultureInfo kultur = BerichtTexte.Kultur;

            // ETAPPE B3 Paket b: Die Doppelpflege der Hilfsenergie hängt an KEINEM
            // Steuerpfad — sie ist auch an einem reinen Wärmepumpenprojekt möglich und
            // wird deshalb VOR der Prüfung auf lauf.Steuer erledigt.
            try { HilfsenergieDoppelpflege(idProjekt, kultur, liste); } catch { }

            if (lauf == null || lauf.Steuer == null) return liste;

            // Jede Seite für sich gekapselt: Ein Fehlschlag der Brennstoffseite darf die
            // Stromseite nicht mitnehmen — und keiner von beiden den Lauf.
            try { Brennstoffseite(idProjekt, lauf, kultur, liste); } catch { }
            // PAKET FX5-b (Anwenderentscheid 03.09.2026, offener Punkt S-2): Fall 5.
            // Eigener Schritt, nicht Teil von Brennstoffseite — er braucht weder
            // Energieträger noch Preiszerlegung, sondern allein die Normwahlen, und
            // dürfte deshalb nicht an deren Vorabfiltern hängenbleiben.
            try { MischlageEnergiesteuer(lauf, kultur, liste); } catch { }
            try { Stromseite(idProjekt, lauf, kultur, liste); } catch { }

            return liste;
        }

        // =====================================================================
        // Hilfsenergie — Doppelpflege Menge (Anlage) gegen Kosten (Position)
        // =====================================================================

        /// <summary>
        /// ETAPPE B3 Paket b — <b>Hilfsenergie an zwei Orten gepflegt</b>.
        ///
        /// <para>Seit Paket b gibt es zwei Wege, denselben Hilfsbedarf zu erfassen: den
        /// MENGENweg über <c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> (Konzept § 4.5
        /// Weg B — er mindert die zuschlagsfähige Nettostromerzeugung) und den
        /// KOSTENweg über die Betriebskostenposition
        /// <c>DbWerte.VDI_POS_HILFSENERGIE</c> (Wege A und C — sie belastet die
        /// Betriebskosten). Beide dürfen nebeneinander stehen; sinnvoll ist es selten,
        /// weil dann derselbe Strom zweimal in der Rechnung erscheint: einmal als
        /// entgangener Zuschlag und einmal als Kostenposition.</para>
        ///
        /// <para><b>Nur melden, nichts verrechnen</b> — dieselbe Haltung wie in der
        /// ganzen Klasse (BF2). Welcher der beiden Wege gemeint ist, kann die Anwendung
        /// nicht wissen; sie kann nur sagen, dass beide gesetzt sind.</para>
        ///
        /// <para><b>„Aktiv" heißt: die Position trägt eine Zahl</b> — einen Satz
        /// (<c>Einheitpreis</c>, Wege A und B) oder einen Jahresbetrag
        /// (<c>EingegebenerWert</c>, Weg C). Eine Vorlagenzeile mit 0 ist eine
        /// vorbereitete, keine gepflegte Position; im Bestand steht sie an fast jedem
        /// Projekt und dürfte niemals warnen.</para>
        /// </summary>
        private static void HilfsenergieDoppelpflege(int idProjekt, CultureInfo kultur,
                                                     List<KohaerenzHinweis> liste)
        {
            // Die billigste Frage zuerst: Gibt es überhaupt einen gepflegten Anteil? Im
            // gesamten Bestand ist die Antwort nein (alle Spalten NULL) — dann kostet die
            // Prüfung genau eine Abfrage je Projekt und endet hier. Die Spaltenprobe für
            // ID_Anlage ist ein SELECT MAX über die ganze Tabelle und läuft erst danach.
            List<HilfsstromRechner.AnlagenAnteil> mitAnteil = AnlagenMitAnteil(idProjekt);
            if (mitAnteil.Count == 0) return;

            // Ohne ID_Anlage (Datenbank vor Schritt 45) gibt es keine anlagenscharfe
            // Zuordnung — dann lässt sich die Doppelpflege nicht feststellen.
            if (!WirtschaftlichkeitCtrl.SpalteVorhanden("Tab_ProjektWerte",
                                                        SchemaKatalog.SPALTE_PW_ID_ANLAGE))
                return;

            List<int> mitPosition = AnlagenMitHilfsenergiePosition(idProjekt);
            if (mitPosition.Count == 0) return;

            foreach (HilfsstromRechner.AnlagenAnteil a in mitAnteil)
            {
                if (!mitPosition.Contains(a.IdAnlage)) continue;
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Text = string.Format(kultur, T("KOH_HILFSENERGIE_DOPPELT",
                            "Hilfsenergie doppelt gepflegt (Menge an der Anlage und " +
                            "Kostenposition {0}): {1} führt einen Hilfsenergieanteil von " +
                            "{2} % und zugleich eine aktive Hilfsenergie-Kostenposition. " +
                            "Die Mengenangabe mindert den KWK-Zuschlag, die Kostenposition " +
                            "belastet die Betriebskosten — verrechnet wird nichts."),
                        DbWerte.VDI_POS_HILFSENERGIE,
                        a.Bezeichner,
                        (a.AnteilProzent ?? 0).ToString("N2", kultur))
                });
            }
        }

        /// <summary>Anlagenzeilen des Projekts mit einem Hilfsenergieanteil &gt; 0 —
        /// über <b>alle</b> Anlagenarten, weil jede Komponente Hilfsenergie haben
        /// kann (Konzept § 5.2).</summary>
        private static List<HilfsstromRechner.AnlagenAnteil> AnlagenMitAnteil(int idProjekt)
        {
            var treffer = new List<HilfsstromRechner.AnlagenAnteil>();
            try
            {
                DataTable dt;
                using (DataRepository.EngineModus())
                    dt = DataRepository.GetDataTable(
                        "SELECT ID, Bezeichner, [" +
                        SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "] " +
                        "FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                        new DbParam("@p", idProjekt));

                if (dt == null ||
                    !dt.Columns.Contains(SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL))
                    return treffer;

                foreach (DataRow r in dt.Rows)
                {
                    object w = r[SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL];
                    if (w == DBNull.Value) continue;
                    double anteil = Convert.ToDouble(w);
                    if (anteil <= 0) continue;
                    treffer.Add(new HilfsstromRechner.AnlagenAnteil
                    {
                        IdAnlage = r["ID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ID"]),
                        Bezeichner = r["Bezeichner"] == DBNull.Value
                                   ? "" : Convert.ToString(r["Bezeichner"]).Trim(),
                        AnteilProzent = anteil
                    });
                }
            }
            catch { treffer.Clear(); }
            return treffer;
        }

        /// <summary>
        /// <c>ID_Anlage</c> jeder AKTIVEN Hilfsenergie-Kostenposition des Projekts.
        ///
        /// <para><b>Der Namensvergleich läuft in C#, nicht in SQL.</b> Der Katalog kennt
        /// neben <c>DbWerte.VDI_POS_HILFSENERGIE</c> die Spielarten
        /// „Hilfsenergiekosten (Strom)", „… (Pumpen)", „… (Solarpumpe)" und
        /// „… (Speicherladepumpe)" — sie sind dieselbe Kostenart unter anderem Namen und
        /// müssen mitwarnen. Ein <c>LIKE</c> dafür wäre in Access mit <c>*</c> und über
        /// OLE DB mit <c>%</c> zu schreiben; diese Falle wird hier nicht aufgestellt.</para>
        /// </summary>
        private static List<int> AnlagenMitHilfsenergiePosition(int idProjekt)
        {
            var treffer = new List<int>();
            try
            {
                DataTable dt;
                using (DataRepository.EngineModus())
                    dt = DataRepository.GetDataTable(
                        "SELECT f.Bezeichnung, w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "], " +
                        "w.[" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], w.EingegebenerWert " +
                        "FROM Tab_ProjektWerte AS w LEFT JOIN Tab_Kostenfaktor AS f " +
                        "ON w.StammID = f.StammID " +
                        "WHERE w.ProjektID = ? AND w.KategorieID = 2",
                        new DbParam("@p", idProjekt));
                if (dt == null) return treffer;

                foreach (DataRow r in dt.Rows)
                {
                    string name = r["Bezeichnung"] == DBNull.Value
                                ? "" : Convert.ToString(r["Bezeichnung"]).Trim();
                    if (!name.StartsWith(DbWerte.VDI_POS_HILFSENERGIE,
                                         StringComparison.OrdinalIgnoreCase)) continue;

                    object ida = r[SchemaKatalog.SPALTE_PW_ID_ANLAGE];
                    if (ida == DBNull.Value) continue;
                    int idAnlage = Convert.ToInt32(ida);
                    if (idAnlage <= 0 || treffer.Contains(idAnlage)) continue;

                    if (!Aktiv(r[SchemaKatalog.SPALTE_PW_EINHEITPREIS]) &&
                        !Aktiv(r["EingegebenerWert"])) continue;

                    treffer.Add(idAnlage);
                }
            }
            catch { treffer.Clear(); }
            return treffer;
        }

        /// <summary>Ein Zahlenfeld ist „gepflegt", wenn es weder NULL noch 0 ist.</summary>
        private static bool Aktiv(object wert)
        {
            if (wert == null || wert == DBNull.Value) return false;
            try { return Math.Abs(Convert.ToDouble(wert)) > 1e-9; }
            catch { return false; }
        }

        // =====================================================================
        // Brennstoffseite — Energiesteuer § 53 / § 53a Abs. 5 / § 54
        // =====================================================================

        /// <summary>
        /// Vergleicht die gebuchte Energiesteuer-Entlastung mit dem Energiesteueranteil,
        /// den die Brennstoffpreise der beteiligten Träger ausweisen.
        ///
        /// <para><b>Eine Zeile je Fall, nicht je Träger.</b> Der gebuchte Betrag ist eine
        /// Projektgröße; ihn auf mehrere Träger aufzuteilen wäre eine Rechnung, die es
        /// nicht gibt. Die betroffenen Träger stehen deshalb aufgezählt in EINER Zeile.
        /// Fall 4 dagegen vergleicht Sätze und bekommt je Träger eine eigene Zeile.</para>
        /// </summary>
        private static void Brennstoffseite(int idProjekt, KohaerenzLauf lauf,
                                            CultureInfo kultur, List<KohaerenzHinweis> liste)
        {
            // Ein Träger, so oft er auch in Anlagen vorkommt, wird einmal geprüft.
            var traeger = new List<SteuerAnlage>();
            var gesehen = new List<int>();
            foreach (SteuerAnlage a in lauf.Steuer.Anlagen)
            {
                if (a == null || a.CarrierId <= 0 || a.BrennstoffMWh <= 0) continue;
                if (gesehen.Contains(a.CarrierId)) continue;
                gesehen.Add(a.CarrierId);
                traeger.Add(a);
            }
            if (traeger.Count == 0) return;

            var ctrl = new BrennstoffBestandteilCtrl();
            var ohneAnteil = new List<string>();     // Träger ohne ausgewiesene Energiesteuer
            var mitAnteil = new List<string>();      // Träger mit ausgewiesener Energiesteuer

            foreach (SteuerAnlage a in traeger)
            {
                BrennstoffBestandteilModel m = ctrl.Read(idProjekt, a.CarrierId);
                string name = TraegerName(a.CarrierId);

                if (!m.Energiesteuer.HasValue)
                { ohneAnteil.Add(MitGrund(name, T("KOH_GRUND_BB_FEHLT", "kein Anteil erfasst"))); continue; }
                if (!m.Energiesteuer_Aktiv)
                { ohneAnteil.Add(MitGrund(name, T("KOH_GRUND_BB_INAKTIV", "Anteil abgeschaltet"))); continue; }
                if (m.Energiesteuer.Value <= 0)
                { ohneAnteil.Add(MitGrund(name, T("KOH_GRUND_BB_NULL", "Anteil 0 ct/kWh"))); continue; }

                mitAnteil.Add(name);

                // ---- Fall 4: gepflegter Satz gegen den Katalogsatz des Jahres ----
                Fall4Brennstoff(a, name, m.Energiesteuer.Value, lauf.Jahr, kultur, liste);
            }

            // ---- Fall 2: Entlastung gebucht, im Preis nicht ausgewiesen ----
            if (lauf.EnergiesteuerEur > 0 && ohneAnteil.Count > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Betrag = lauf.EnergiesteuerEur,
                    Text = string.Format(kultur, T("KOH_FALL2_ENERGIESTEUER",
                            "Die Energiesteuer-Gutschrift von {0} €/a setzt voraus, dass der erfasste " +
                            "Brennstoffpreis die Energiesteuer enthält. Im Preis ist sie nicht " +
                            "ausgewiesen: {1}."),
                        lauf.EnergiesteuerEur.ToString("N2", kultur),
                        string.Join(", ", ohneAnteil.ToArray()))
                });

            // ---- Fall 3: Anteil ausgewiesen, aber keine Entlastung gewählt ----
            //
            // Bewusst an der WAHL festgemacht, nicht am Betrag: Eine gewählte Norm, die
            // an einer Bedingung scheitert (Nutzungsgrad, Unternehmensart, Einheit),
            // begründet SteuerGutschriftRechner bereits selbst. Eine zweite Meldung
            // darüber wäre Rauschen.
            //
            // ETAPPE B3 Paket a: „gewählt" heißt seither Projektwahl ODER Anlagenwahl
            // (BF6). Ohne diese Erweiterung meldete die Prüfung „keine Entlastung
            // gewählt", während eine Anlage längst nach § 53 entlastet wird — genau der
            // Widerspruch, den sie aufdecken soll.
            if (mitAnteil.Count > 0 && !EntlastungGewaehlt(lauf.Steuer))
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_FALL3_ENERGIESTEUER",
                            "Der erfasste Brennstoffpreis weist eine Energiesteuer aus, es ist aber " +
                            "keine Entlastung gewählt (§ 53 / § 53a Abs. 5 / § 54): {0}."),
                        string.Join(", ", mitAnteil.ToArray()))
                });
        }

        /// <summary>
        /// ETAPPE B3 Paket a — true, sobald IRGENDEINE Entlastungsnorm im Spiel ist:
        /// entweder als Projektwahl oder als eigene Wahl einer Anlage
        /// (<c>Tab_Energieanlagen.Energiesteuer_Wahl</c>, BF6).
        ///
        /// <para>Die Aufteilungsmethode bleibt außen vor — sie sagt nur, WIE bemessen
        /// wird, nicht OB entlastet wird.</para>
        /// </summary>
        private static bool EntlastungGewaehlt(SteuerEingabe e)
        {
            if (Gewaehlt(e.EnergiesteuerWahl)) return true;
            foreach (SteuerAnlage a in e.Anlagen)
                if (a != null && Gewaehlt(a.EnergiesteuerWahl)) return true;
            return false;
        }

        /// <summary>Eine Wahl ist gesetzt, wenn sie weder leer noch
        /// <c>KEINE</c> ist — dasselbe Rückfallmuster wie im
        /// <see cref="SteuerGutschriftRechner"/>.</summary>
        private static bool Gewaehlt(string wahl)
        {
            if (string.IsNullOrEmpty(wahl)) return false;
            string w = wahl.Trim();
            return w.Length > 0 &&
                   !string.Equals(w, DbWerte.ENERGIESTEUER_WAHL_KEINE, StringComparison.Ordinal);
        }

        /// <summary>
        /// Fall 4 für einen Brennstoffträger: gepflegter Anteil gegen den <b>Regelsatz
        /// nach § 2 EnergieStG</b> des Jahres.
        ///
        /// <para><b>Warum gegen § 2 und nicht gegen den Entlastungssatz.</b> Im Preis
        /// steckt der volle Steuersatz — die Entlastung nach § 53a Abs. 5 bzw. § 54 ist
        /// die Rückerstattung eines Teils davon. Ein Vergleich gegen den Teilsatz würde
        /// jeden korrekt erfassten Preis als Abweichung melden.</para>
        ///
        /// <para>Ohne zugeordneten Katalogschlüssel (Biogas, Holz, Fernwärme …), ohne
        /// Satz im Jahr oder ohne belegbare Einheitenumrechnung entsteht KEINE Zeile:
        /// Eine geratene Vergleichszahl wäre schlimmer als keine.</para>
        /// </summary>
        private static void Fall4Brennstoff(SteuerAnlage a, string name, double anteilCtKwh,
                                            int jahr, CultureInfo kultur,
                                            List<KohaerenzHinweis> liste)
        {
            if (string.IsNullOrEmpty(a.SchluesselSatzVoll)) return;

            GesetzParameter p = new GesetzKatalog().WertMitHerkunft(a.SchluesselSatzVoll, jahr);
            if (p == null || !p.Wert.HasValue) return;

            double? katalogCt = InCtKwh(p.Wert.Value, p.Einheit, a);
            if (!katalogCt.HasValue) return;
            if (Math.Abs(anteilCtKwh - katalogCt.Value) <= TOLERANZ_CT_KWH) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_FALL4_ENERGIESTEUER",
                        "{0}: Der erfasste Energiesteueranteil {1} ct/kWh weicht vom Katalogsatz " +
                        "{2} {3} des Jahres {4} ab — das sind {5} ct/kWh."),
                    name,
                    anteilCtKwh.ToString("N4", kultur),
                    p.Wert.Value.ToString("N2", kultur), p.Einheit,
                    jahr.ToString(CultureInfo.InvariantCulture),
                    katalogCt.Value.ToString("N4", kultur))
            });
        }

        // =====================================================================
        // Fall 5 — Mischlage § 53 / § 53a Abs. 5 neben § 54
        // =====================================================================

        /// <summary>
        /// PAKET FX5-b (Anwenderentscheid 03.09.2026, offener Punkt S-2) —
        /// <b>Fall 5: zwei Entlastungswelten im selben Projekt.</b>
        ///
        /// <para>Seit B3a wählt jede Anlage ihre Entlastungsnorm selbst (Rückfall:
        /// Projektwahl). Damit ist eine Lage möglich, die es vorher nicht gab: eine
        /// Anlage rechnet nach § 53 / § 53a Abs. 5 EnergieStG (Entlastung für die
        /// Stromerzeugung bzw. die gekoppelte Erzeugung), eine andere nach § 54
        /// (Heizstoffe im produzierenden Gewerbe). Rechnerisch ist das sauber — jede
        /// Anlage bringt ihre eigene Brennstoffmenge mit, nichts wird zweimal
        /// entlastet. Im ANTRAG ist es eine Stolperstelle: § 54 nimmt Mengen aus, die
        /// bereits nach § 53 / § 53a entlastet wurden, und die Verfahren laufen als
        /// getrennte Anträge beim Hauptzollamt.</para>
        ///
        /// <para><b>Nur melden, nichts sperren, nichts verrechnen</b> — dieselbe Haltung
        /// wie in der ganzen Klasse (BF2). Die Zeile ändert keine Zahl; sie benennt die
        /// beteiligten Anlagen samt ihrer Wahl und deren Herkunft (Projekt- oder
        /// Anlagenwahl).</para>
        ///
        /// <para><b>Angesetzt wird an der WAHL, nicht am gebuchten Betrag</b> — genau wie
        /// in Fall 3. Eine gewählte Norm, die an einer Bedingung scheitert
        /// (Unternehmensart, Nutzungsgrad, Sockelbetrag), begründet der
        /// <see cref="SteuerGutschriftRechner"/> bereits selbst; eine zweite Meldung
        /// darüber wäre Rauschen. Ein Betrag steht deshalb NICHT an der Zeile: Die
        /// gebuchte Energiesteuer-Entlastung ist die Summe BEIDER Seiten und würde als
        /// „betroffener Betrag" in die Irre führen.</para>
        ///
        /// <para><b>Ohne Brennstoffeinsatz keine Zeile.</b> Eine Anlage mit
        /// <c>BrennstoffMWh = 0</c> bringt keine Menge mit, die doppelt entlastet werden
        /// könnte — dieselbe Vorbedingung wie in <see cref="Brennstoffseite"/>.</para>
        /// </summary>
        private static void MischlageEnergiesteuer(KohaerenzLauf lauf, CultureInfo kultur,
                                                   List<KohaerenzHinweis> liste)
        {
            var seiteStrom = new List<string>();   // § 53 / § 53a Abs. 5
            var seiteGewerbe = new List<string>(); // § 54

            foreach (SteuerAnlage a in lauf.Steuer.Anlagen)
            {
                if (a == null || a.BrennstoffMWh <= 0) continue;

                bool eigen;
                string wahl = WirksameWahl(a, lauf.Steuer, out eigen);
                if (!Gewaehlt(wahl)) continue;

                string herkunft = eigen
                    ? T("KOH_HERKUNFT_ANLAGE", "Anlagenwahl")
                    : T("KOH_HERKUNFT_PROJEKT", "Projektwahl");

                if (string.Equals(wahl, DbWerte.ENERGIESTEUER_WAHL_53, StringComparison.Ordinal))
                    seiteStrom.Add(MitGrund(AnlagenName(a),
                        T("KOH_NORM_53", "§ 53") + ", " + herkunft));
                else if (string.Equals(wahl, DbWerte.ENERGIESTEUER_WAHL_53A, StringComparison.Ordinal))
                    seiteStrom.Add(MitGrund(AnlagenName(a),
                        T("KOH_NORM_53A", "§ 53a Abs. 5") + ", " + herkunft));
                else if (string.Equals(wahl, DbWerte.ENERGIESTEUER_WAHL_54, StringComparison.Ordinal))
                    seiteGewerbe.Add(MitGrund(AnlagenName(a),
                        T("KOH_NORM_54", "§ 54") + ", " + herkunft));
            }

            // Beide Welten müssen wirklich besetzt sein; eine Seite allein ist der
            // Normalfall und schweigt.
            if (seiteStrom.Count == 0 || seiteGewerbe.Count == 0) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_FALL5_MISCHLAGE",
                        "Im Projekt stehen zwei Entlastungswelten nebeneinander: {0} gegen {1}. " +
                        "§ 54 EnergieStG nimmt Mengen aus, die bereits nach § 53 / § 53a Abs. 5 " +
                        "entlastet wurden — dieselbe Brennstoffmenge darf nicht zweimal entlastet " +
                        "werden; die Anträge laufen als getrennte Verfahren beim Hauptzollamt. " +
                        "Gerechnet wird unverändert je Anlage nach ihrer Wahl."),
                    string.Join(", ", seiteStrom.ToArray()),
                    string.Join(", ", seiteGewerbe.ToArray()))
            });
        }

        /// <summary>
        /// PAKET FX5-b — die für eine Anlage GELTENDE Entlastungsnorm: ihr eigener Wert,
        /// ersatzweise der Projektwert (B3a, BF6). <paramref name="eigen"/> sagt, welcher
        /// der beiden es war.
        /// <para><b>Spiegelt <c>SteuerGutschriftRechner.Wahl</c></b>, das dort privat ist.
        /// Dieselbe Regel, dieselbe Behandlung von <c>null</c> und Leerstring — wer eine
        /// von beiden ändert, muss die andere mitziehen.</para>
        /// </summary>
        private static string WirksameWahl(SteuerAnlage a, SteuerEingabe e, out bool eigen)
        {
            string wahl = a.EnergiesteuerWahl == null ? null : a.EnergiesteuerWahl.Trim();
            eigen = !string.IsNullOrEmpty(wahl);
            return eigen ? wahl : e.EnergiesteuerWahl;
        }

        /// <summary>Anzeigename einer Anlagenzeile; ohne Bezeichner der Trägername.</summary>
        private static string AnlagenName(SteuerAnlage a)
        {
            string s = (a.Bezeichner ?? "").Trim();
            return s.Length > 0 ? s : TraegerName(a.CarrierId);
        }

        // =====================================================================
        // Stromseite — Stromsteuer § 9b und § 9 Abs. 1 Nr. 3
        // =====================================================================

        /// <summary>
        /// Vergleicht die gebuchten Stromsteuergutschriften mit dem Stromsteueranteil des
        /// Strom-Aufschlagsblocks.
        ///
        /// <para><b>Der Schalter <c>Aufschlaege_Anwenden</c> entscheidet zuerst.</b> Steht
        /// er aus, wirkt der ganze Aufschlagsblock nicht auf den Preis — dann ist die
        /// Stromsteuer im angesetzten Bezugspreis nicht enthalten, gleichgültig was in den
        /// Komponenten steht (<c>WirtschaftlichkeitCtrl.RechneAufschlaege</c>).</para>
        /// </summary>
        private static void Stromseite(int idProjekt, KohaerenzLauf lauf,
                                       CultureInfo kultur, List<KohaerenzHinweis> liste)
        {
            bool gebucht9b = lauf.StromsteuerEntlastungEur > 0;
            bool gebucht913 = lauf.StromsteuerBefreiungEur > 0;
            bool prodGewerbe =
                string.Equals(lauf.Steuer.Unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                              StringComparison.Ordinal) ||
                string.Equals(lauf.Steuer.Unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST,
                              StringComparison.Ordinal);

            // Nichts gebucht und keine Entlastungsberechtigung — es gibt nichts zu prüfen
            // und nichts zu melden (sonst stünde an jedem Projekt eine Zeile).
            if (!gebucht9b && !gebucht913 && !prodGewerbe) return;

            int carrier = StromAufschlagCtrl.StromCarrierId(idProjekt);
            if (carrier <= 0)
            {
                Fall2Strom(lauf, T("KOH_GRUND_KEIN_STROMTRAEGER",
                    "dem Projekt ist kein Strom-Energieträger zugeordnet"), kultur, liste);
                return;
            }

            StromAufschlagModel m = new StromAufschlagCtrl().Read(idProjekt, carrier);
            bool gesamtwert = string.Equals(m.Modus, DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT,
                                            StringComparison.Ordinal);

            string grund = null;
            if (!lauf.AufschlaegeAnwenden)
                grund = T("KOH_GRUND_AUFSCHLAG_AUS",
                    "der Schalter „Aufschläge in der Wirtschaftlichkeit berücksichtigen\" ist aus");
            else if (!m.AusDatenbank)
                grund = T("KOH_GRUND_KEIN_STROMTRAEGER",
                    "dem Projekt ist kein Strom-Energieträger zugeordnet");
            else if (gesamtwert)
                grund = T("KOH_GRUND_STROM_GESAMTWERT",
                    "der Aufschlag ist als Gesamtwert erfasst und nicht aufgeschlüsselt");
            else if (!m.Stromsteuer_Aktiv)
                grund = T("KOH_GRUND_STROM_INAKTIV", "die Komponente Stromsteuer ist abgeschaltet");
            else if (m.Stromsteuer <= 0)
                grund = T("KOH_GRUND_STROM_NULL", "die Komponente Stromsteuer steht auf 0 ct/kWh");

            if (grund != null) { Fall2Strom(lauf, grund, kultur, liste); return; }

            // ---- Ab hier ist die Stromsteuer im angesetzten Preis enthalten ----

            // Fall 3: Belastung ohne Entlastung — nur für Berechtigte und nur bei
            // vorhandenem Netzbezug (ohne Bezug gibt es nichts zu entlasten; den Sockel
            // von 250 €/a begründet SteuerGutschriftRechner selbst).
            if (!gebucht9b && prodGewerbe && lauf.Steuer.NetzbezugMWh > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.HINWEIS,
                    Text = string.Format(kultur, T("KOH_FALL3_STROMSTEUER",
                            "Der erfasste Strompreis weist eine Stromsteuer von {0} ct/kWh aus, " +
                            "eine Entlastung nach § 9b StromStG wird nicht gebucht."),
                        m.Stromsteuer.ToString("N4", kultur))
                });

            // Fall 4: nur gegen einen WIRKLICH gepflegten Wert (siehe Klassenkommentar).
            double? roh = StromsteuerRoh(idProjekt, carrier);
            if (roh.HasValue) Fall4Strom(roh.Value, lauf.Jahr, kultur, liste);
        }

        /// <summary>
        /// Die beiden Fall-2-Zeilen der Stromseite — je Vorschrift eine, mit dem
        /// tatsächlich gebuchten Betrag.
        /// </summary>
        private static void Fall2Strom(KohaerenzLauf lauf, string grund, CultureInfo kultur,
                                       List<KohaerenzHinweis> liste)
        {
            if (lauf.StromsteuerEntlastungEur > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Betrag = lauf.StromsteuerEntlastungEur,
                    Text = string.Format(kultur, T("KOH_FALL2_STROMST_9B",
                            "Die Stromsteuer-Entlastung nach § 9b StromStG von {0} €/a setzt voraus, " +
                            "dass der erfasste Strompreis die Stromsteuer enthält. Im Preis ist sie " +
                            "nicht ausgewiesen ({1})."),
                        lauf.StromsteuerEntlastungEur.ToString("N2", kultur), grund)
                });

            if (lauf.StromsteuerBefreiungEur > 0)
                liste.Add(new KohaerenzHinweis
                {
                    Schwere = KohaerenzSchwere.WARNUNG,
                    Betrag = lauf.StromsteuerBefreiungEur,
                    Text = string.Format(kultur, T("KOH_FALL2_STROMST_9_1_3",
                            "Die Stromsteuer-Befreiung nach § 9 Abs. 1 Nr. 3 StromStG von {0} €/a wird " +
                            "als Erlös gebucht, obwohl der erfasste Strompreis die Stromsteuer nicht " +
                            "ausweist ({1})."),
                        lauf.StromsteuerBefreiungEur.ToString("N2", kultur), grund)
                });
        }

        /// <summary>Fall 4 der Stromseite: gepflegter Anteil gegen <c>STROMST_REGELSATZ</c>.</summary>
        private static void Fall4Strom(double anteilCtKwh, int jahr, CultureInfo kultur,
                                       List<KohaerenzHinweis> liste)
        {
            GesetzParameter p = new GesetzKatalog()
                .WertMitHerkunft(DbWerte.GESETZ_STROMST_REGELSATZ, jahr);
            if (p == null || !p.Wert.HasValue) return;

            // Der Stromsteuersatz steht in EUR/MWh. Anders als beim Erdgas gibt es hier
            // keine Brennwertbrücke — eine Kilowattstunde Strom ist eine Kilowattstunde.
            double? katalogCt = InCtKwh(p.Wert.Value, p.Einheit, null);
            if (!katalogCt.HasValue) return;
            if (Math.Abs(anteilCtKwh - katalogCt.Value) <= TOLERANZ_CT_KWH) return;

            liste.Add(new KohaerenzHinweis
            {
                Schwere = KohaerenzSchwere.HINWEIS,
                Text = string.Format(kultur, T("KOH_FALL4_STROMSTEUER",
                        "Der erfasste Stromsteueranteil {0} ct/kWh weicht vom Katalogsatz {1} {2} " +
                        "des Jahres {3} ab — das sind {4} ct/kWh."),
                    anteilCtKwh.ToString("N4", kultur),
                    p.Wert.Value.ToString("N2", kultur), p.Einheit,
                    jahr.ToString(CultureInfo.InvariantCulture),
                    katalogCt.Value.ToString("N4", kultur))
            });
        }

        // =====================================================================
        // Datenzugriff
        // =====================================================================

        /// <summary>
        /// Der ROHE Stromsteueranteil aus <c>energy_project_settings</c>;
        /// <c>null</c> = nie gepflegt (oder Spalte/Zeile fehlt).
        /// </summary>
        /// <remarks>
        /// <b>Warum nicht über <see cref="StromAufschlagCtrl.Read"/>.</b> Dessen Leseweg
        /// lässt <c>NULL</c> auf den Vorschlagssatz zurückfallen
        /// (<c>StromAufschlagModel.STROMSTEUER_REGELFALL</c> = 2,05 ct/kWh) — der Wert
        /// sähe dann gepflegt aus, obwohl ihn niemand erfasst hat. Für Fall 2 ist das
        /// richtig (der Vorschlagssatz wirkt tatsächlich im Preis), für Fall 4 nicht:
        /// Der Rückfallwert IST der Katalogsatz, der Vergleich wäre zirkulär.
        /// </remarks>
        private static double? StromsteuerRoh(int idProjekt, int carrierId)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + StromAufschlagCtrl.TABLE + "] " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@proj", idProjekt),
                    new DbParam("@eid", carrierId));

                if (dt == null || dt.Rows.Count == 0) return null;
                if (!dt.Columns.Contains(SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER)) return null;

                object v = dt.Rows[0][SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER];
                return (v == null || v == DBNull.Value) ? (double?)null : Convert.ToDouble(v);
            }
            catch { return null; }
        }

        /// <summary>Anzeigename eines Energieträgers; leer = nicht lesbar.</summary>
        private static string TraegerName(int carrierId)
        {
            try
            {
                object v = DataRepository.ExecuteScalar(
                    "SELECT [name] FROM energy_carrier WHERE id = ?",
                    new DbParam("@id", carrierId));
                string s = (v == null || v == DBNull.Value) ? "" : Convert.ToString(v).Trim();
                return s.Length > 0 ? s : ("#" + carrierId.ToString(CultureInfo.InvariantCulture));
            }
            catch { return "#" + carrierId.ToString(CultureInfo.InvariantCulture); }
        }

        // =====================================================================
        // Einheitenkette
        // =====================================================================

        /// <summary>
        /// Bringt einen Katalogsatz in ct/kWh — <b>dieselbe Kette wie die Schnellwahl des
        /// Dialogs</b> (<c>ucBrennstoffBestandteile.InCtKwh</c>, Konzept § 6.2). Beide
        /// Wege müssen zur selben Zahl kommen, sonst meldete Fall 4 eine Abweichung
        /// gegen den Wert, den die Schnellwahl selbst eingetragen hat.
        /// <c>null</c> = nicht belegbar (dann entsteht keine Zeile).
        /// </summary>
        /// <param name="a">Trägerangaben für die Umrechnung; <c>null</c> = reine
        /// Energieeinheit ohne Abrechnungsbezug (Strom).</param>
        private static double? InCtKwh(double wert, string einheit, SteuerAnlage a)
        {
            string e = (einheit ?? "").Trim();

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
                return wert;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
            {
                // EUR/MWh bemisst sich beim Gas am BRENNWERT; der Arbeitspreis entsteht
                // dagegen aus Preis ÷ Hi. Umgerechnet wird deshalb mit Hs/Hi — dieselbe
                // Regel wie in SteuerGutschriftRechner.MengeInGesetzlicherEinheit. Fehlt
                // der Brennwert (oder gibt es keinen — Strom), bleibt der Faktor 1.
                double faktor = (a != null && a.EffHi > 0 && a.EffHs > 0) ? a.EffHs / a.EffHi : 1.0;
                return wert / 10.0 * faktor;
            }

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_GJ, StringComparison.OrdinalIgnoreCase))
                return wert * GJ_JE_MWH / 10.0;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.OrdinalIgnoreCase))
                return JeTausendEinheiten(wert, "l", a);

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.OrdinalIgnoreCase))
                return JeTausendEinheiten(wert, "kg", a);

            return null;
        }

        /// <summary>
        /// Satz je 1.000 Abrechnungseinheiten → ct/kWh:
        /// <c>Satz ÷ 1000 [€/Einheit] × 100 [ct/€] ÷ Hi [kWh/Einheit]</c>. Passt die
        /// Abrechnungseinheit nicht, bleibt es bei <c>null</c>: Die Brücke Liter ↔
        /// Kilogramm bräuchte die Dichte, und <c>energy_carrier.density</c> ist im
        /// gesamten Bestand leer.
        /// </summary>
        private static double? JeTausendEinheiten(double wert, string erwartet, SteuerAnlage a)
        {
            if (a == null || a.EffHi <= 0) return null;
            if (a.Abrechnungseinheit == null) return null;
            if (!string.Equals(a.Abrechnungseinheit.Trim(), erwartet, StringComparison.OrdinalIgnoreCase))
                return null;
            return wert / (10.0 * a.EffHi);
        }

        // =====================================================================
        // Texte
        // =====================================================================

        /// <summary>„Name (Grund)" — die Aufzählungsform der Trägerliste.</summary>
        private static string MitGrund(string name, string grund)
        {
            return name + " (" + grund + ")";
        }

        /// <summary>
        /// MyResource mit deutschem Rückfall (Drei-Schichten-Regel) — dasselbe Muster wie
        /// <c>ucFuelSettings.TKd4</c> und <c>UcWirtschaftlichkeit.T</c>. Die Schlüssel
        /// tragen den Präfix <c>KOH_</c>; der Rückfall greift auf einer Ressourcendatei
        /// ohne die neuen Einträge.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
