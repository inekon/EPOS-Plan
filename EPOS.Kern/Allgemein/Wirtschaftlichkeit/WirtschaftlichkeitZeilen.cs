using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E7 — <b>eine</b> Definition der Kennzahlentabelle für alle drei Ausgaben.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Bis E7 stand dieselbe Zeilenliste dreimal
    /// im Code: im Word-Baustein, im Excel-Generator und im Ergebnisreiter. Aus vierzehn
    /// Zeilen waren über die Etappen E2 bis E5 zweiundzwanzig geworden, jede davon
    /// dreimal geschrieben. Die Zahlen liefen dabei nicht auseinander — das Drumherum
    /// aber schon: unterschiedliche Stammspalten-Platzhalter, unterschiedliche
    /// Sichtbarkeitsprüfungen, gemischte Textschichten. Jede weitere Zeile hätte den
    /// Fehler dreimal wiederholt.</para>
    ///
    /// <para><b>Was hier steht und was nicht.</b> Hier steht, WELCHE Zeilen es gibt, wie
    /// sie heißen, woher ihr Wert kommt, wie er formatiert wird und wann die Zeile
    /// überhaupt erscheint. Nicht hier steht, WIE gerendert wird — Word baut Tabellen,
    /// Excel schreibt Zellen mit Zahlformat, der Reiter füllt ein Grid. Diese drei
    /// bleiben getrennt.</para>
    ///
    /// <para><b>Drei-Schichten-Regel.</b> <see cref="WirtZeile.Schluessel"/> ist
    /// sprachneutral und ASCII, <see cref="WirtZeile.Titel"/> kommt ausschließlich aus
    /// <c>MyResource.Resource.WIRT_ZEILE_*</c>. Die Titel dürfen deshalb <b>nicht</b>
    /// noch einmal durch <c>BerichtTexte.T()</c> laufen — sie sind bereits übersetzt.</para>
    /// </summary>
    public sealed class WirtZeile
    {
        /// <summary>Sprachneutraler Schlüssel der Zeile (ASCII, eingefroren).</summary>
        public string Schluessel = "";

        /// <summary>Anzeigetitel aus <c>MyResource</c> — bereits lokalisiert.</summary>
        public string Titel = "";

        /// <summary>.NET-Zahlformat für Word und Reiter („N0", „N1", „N3").</summary>
        public string Format = "N0";

        /// <summary>Zellformat für Excel („#,##0", „#,##0.0", „#,##0.000").</summary>
        public string ExcelFormat = "#,##0";

        /// <summary>Zahlenwert der Zeile; <c>null</c> = kein Wert (Anzeige „—", Excel leer).</summary>
        public Func<WirtschaftlichkeitErgebnis, double?> Wert;

        /// <summary>
        /// Textwert statt Zahl (z. B. die Herkunft der Steuersätze). Ist er gesetzt,
        /// bleibt <see cref="Wert"/> unbenutzt und die Zeile ist eine <b>Textzeile</b>.
        /// </summary>
        public Func<WirtschaftlichkeitErgebnis, string> Text;

        /// <summary>
        /// Was in der Stammspalte steht, wenn die Größe dort keine Bedeutung hat
        /// (Kapitalwert gegenüber Stamm, Annuität, Amortisation, interner Zinsfuß).
        /// <c>null</c> = die Zeile gilt auch für den Stamm.
        ///
        /// <para><b>Excel schreibt hier trotzdem nichts</b> — die Wertspalten müssen
        /// numerisch bleiben, sonst sind Filter und Diagramme hinüber. Das ist der eine
        /// bewusst verbliebene Unterschied zwischen den Ausgaben; er steht hier, statt
        /// dreimal zufällig zu entstehen.</para>
        /// </summary>
        public string StammAnzeige;

        /// <summary>true, wenn die Zeile Text statt einer Zahl führt.</summary>
        public bool IstText { get { return Text != null; } }

        // =====================================================================
        // ETAPPE B7 — die Rubrik „Erlöse und Vorteile" (Konzept § 2.6)
        // =====================================================================

        /// <summary>
        /// Zugehörigkeit zur Erlösrubrik: <see cref="BLOCK_A"/> zahlungswirksam,
        /// <see cref="BLOCK_B"/> Ausweis, leer = gewöhnliche Kennzahl.
        ///
        /// <para><b>Warum das am Zeilenobjekt steht und nicht in der Ausgabe.</b> Die
        /// Teilung in zwei Blöcke ist keine Kosmetik, sondern eine Rechenaussage:
        /// Block B darf nicht addiert werden. Stünde sie in Word, Excel und Reiter je
        /// einmal, wäre sie dreimal geschrieben — und der erste Ausgabeweg, der sie
        /// vergisst, summiert eine Doppelzählung.</para>
        /// </summary>
        public string Block = "";

        /// <summary>Blocküberschrift ohne eigene Werte; sie entfällt mit ihrem Block.</summary>
        public bool IstUeberschrift;

        /// <summary>Summenzeile des Blocks A — sie summiert nur, was in
        /// <see cref="Block"/> <c>A</c> steht.</summary>
        public bool IstSumme;

        /// <summary>Einzugstiefe: 0 = Hauptzeile, 1 = Unter-/Herleitungszeile.</summary>
        public int Einzug;

        /// <summary>
        /// ETAPPE B7 — die Zeile erscheint, sobald das Projekt eine Anlage führt, für
        /// die die Position gilt, <b>auch wenn der Betrag 0 ist</b>.
        ///
        /// <para><b>Der Anwenderbefund vom 17.09.2026, der das erzwang:</b> „Vergütungen
        /// und Reduktionen sind in den Ergebnissen nicht dargestellt." Sie waren
        /// gerechnet — nur blendete die Konvention „nie 0-Zeilen" jede Position aus,
        /// deren Satz ungepflegt war. Der Anwender sah damit nicht, dass es die Position
        /// gibt, und erst recht nicht, warum sie 0 ist. Seit B7 steht sie da, und
        /// <see cref="Grundtext"/> sagt, woran es liegt.</para>
        /// </summary>
        public bool ImmerZeigen;

        /// <summary>
        /// Klartext der fehlenden Grundlage, wenn der Betrag 0 ist („— kein
        /// KWK-Zuschlagssatz gepflegt"). <c>null</c> oder leer = ohne Zusatz.
        /// </summary>
        public Func<WirtschaftlichkeitErgebnis, string> Grundtext;

        /// <summary>Kennung des zahlungswirksamen Blocks A (Konzept § 2.6).</summary>
        public const string BLOCK_A = "A";

        /// <summary>Kennung des Ausweisblocks B — <b>nicht addieren</b>.</summary>
        public const string BLOCK_B = "B";

        /// <summary>
        /// Der formatierte Zellinhalt für Word und Reiter; <c>„—"</c>, wenn es keinen
        /// Wert gibt.
        /// </summary>
        public string Anzeige(WirtschaftlichkeitErgebnis e, System.Globalization.CultureInfo kultur)
        {
            if (e == null) return "—";
            if (IstText) { string t = Text(e); return string.IsNullOrEmpty(t) ? "—" : t; }
            if (e.IstStamm && StammAnzeige != null) return StammAnzeige;
            double? v = Wert == null ? null : Wert(e);
            if (!v.HasValue) return "—";
            string zahl = v.Value.ToString(Format, kultur);

            // ETAPPE B7: Eine 0 ohne Begründung ist eine Behauptung. Steht hinter der
            // Position eine ungepflegte Grundlage, sagt die Zelle es im Klartext — in
            // ALLEN drei Ausgaben, weil es hier steht und nicht dreimal im Rendercode.
            // Excel bekommt die Zahl weiter numerisch (ExcelWert), sonst wären Filter
            // und Diagramme des Blattes hinüber.
            if (Grundtext != null && v.Value == 0)
            {
                string g = Grundtext(e);
                if (!string.IsNullOrEmpty(g)) return zahl + " — " + g;
            }
            return zahl;
        }

        /// <summary>Der Zahlenwert für Excel; <c>null</c> = Zelle bleibt leer.</summary>
        public double? ExcelWert(WirtschaftlichkeitErgebnis e)
        {
            if (e == null || IstText || Wert == null) return null;
            if (e.IstStamm && StammAnzeige != null) return null;
            return Wert(e);
        }
    }

    /// <summary>
    /// Baut die Kennzahlenliste der Wirtschaftlichkeit — die eine Wahrheit für
    /// Word-Baustein, Excel-Blatt und Ergebnisreiter (Etappe E7).
    /// </summary>
    public static class WirtschaftlichkeitZeilen
    {
        /// <summary>
        /// Die sichtbaren Kennzahlzeilen einer Vergleichsgruppe.
        /// </summary>
        /// <param name="menge">
        /// Alle Ergebnisse der Gruppe (alle Szenarien). Über diese Menge entscheidet
        /// sich, ob eine Zeile überhaupt erscheint — nie über ein einzelnes Szenario,
        /// sonst hätten Word, Excel und Reiter wieder verschiedene Tabellen.
        /// </param>
        /// <param name="tarif">
        /// Tarifparameter der Gruppe; er entscheidet über die Beschriftung der
        /// Stromkostenzeile. <c>null</c> = Zonenmodell (Bestandsverhalten).
        /// </param>
        public static List<WirtZeile> Kennzahlen(IList<WirtschaftlichkeitErgebnis> menge,
                                                 TarifParameter tarif)
        {
            var z = new List<WirtZeile>();
            if (menge == null) return z;

            // AUFTRAG VF-1 (Anwenderbefund 17.09.2026): WOMIT rechnet diese Spalte?
            //
            // In der Vergleichsgruppe stand stumm eine Spalte „mit Flotte" neben einer
            // „ohne" — der Anwender hielt die Variante für „mit Speicher". Der
            // Simulationsreiter sagte es seit jeher, die Wirtschaftlichkeit nicht; und
            // gerade sie ist der Ort, an dem zwei Zahlen gegeneinander gehalten werden.
            //
            // Die Zeile steht GANZ OBEN, vor der ersten Geldzeile: Sie ist der Kontext,
            // unter dem alles darunter zu lesen ist. Stamm und Varianten dürfen
            // verschiedene Flotten führen — die Zeile ist die Antwort darauf, nicht ein
            // Hinweis auf eine Abweichung.
            //
            // Ohne lesbaren Kontext (kein Projektbezug, keine Datenbank — so laufen die
            // Zeilenproben) entfällt sie wie jede andere Zeile ohne Wert.
            Dictionary<int, string> speicher = Speicherkontexte(menge);
            if (speicher.Count > 0)
                z.Add(new WirtZeile
                {
                    Schluessel = "SPEICHER_KONTEXT",
                    Titel = MyResource.Resource.WIRT_ZEILE_SPEICHER,
                    Text = e =>
                    {
                        string t;
                        return e != null && speicher.TryGetValue(e.IdProjekt, out t) ? t : "";
                    }
                });

            z.Add(Zahl("INVESTITION", MyResource.Resource.WIRT_ZEILE_INVESTITION,
                       e => (double?)e.Investition));

            // ETAPPE K5 (Konzept § 7.4, L7) — der Investitionszuschuss als EIGENE Zeile,
            // unmittelbar unter der Investition und NEGATIV dargestellt: Er ist der
            // Betrag, um den die Anfangsauszahlung geringer ausfällt, und genau so soll
            // ihn der Leser sehen („Zuschuss: −X €"). Gespeichert ist er positiv; das
            // Vorzeichen entsteht erst hier, in der Anzeige.
            //
            // Die Zeile erscheint nur, wenn irgendein Projekt der Gruppe einen Zuschuss
            // führt — dasselbe Muster wie bei der KWKG- und der BEHG-Zeile. Sonst stünde
            // in jedem Bericht ohne Förderung eine Nullzeile.
            if (Irgendein(menge, e => e.Zuschuss > 0))
                z.Add(Zahl("ZUSCHUSS", MyResource.Resource.WIRT_ZEILE_ZUSCHUSS,
                           e => (double?)(-e.Zuschuss)));

            z.Add(Zahl("BETRIEBSKOSTEN", MyResource.Resource.WIRT_ZEILE_BETRIEBSKOSTEN,
                       e => e.BetriebskostenJahr));
            z.Add(Zahl("ENERGIEKOSTEN", MyResource.Resource.WIRT_ZEILE_ENERGIEKOSTEN,
                       e => e.EnergiekostenJahr));

            // ETAPPE B7 (Konzept § 3.5) — die ANLAGENSCHARFE AUFSCHLÜSSELUNG.
            //
            // Die Energiekostenzeile ist in fast jedem Projekt die größte laufende
            // Position und sagte bis B7 nichts über sich. Darunter steht seither je
            // Anlage eine Herleitungszeile „Menge × Preis"; die SUMME bleibt die Zeile
            // darüber — hier wird nichts zweites gerechnet.
            //
            // Die Anlagen der GANZEN Gruppe bilden die Zeilenliste, nicht die eines
            // Projekts: Stamm und Varianten führen verschiedene Anlagen, und eine
            // Zeile, die nur in einer Spalte einen Wert hat, gehört trotzdem in die
            // Tabelle — in den übrigen steht dann „—".
            foreach (string anlage in Anlagennamen(menge))
            {
                string name = anlage;      // Fangkopie für den Abschluss
                z.Add(Unter("ENERGIEKOSTEN_ANLAGE_" + Schluesselform(name),
                            string.Format(MyResource.Resource.WIRT_ENK_ZEILE, name),
                            e => AnlageKosten(e, name), ""));
            }

            // ETAPPE E7 — die Zeile hieß bis hierher in BEIDEN Tarifmodellen
            // „Stromkosten Tarif". Im Rollenmodell trägt sie aber den RESTSTROM-Betrag,
            // also die Kosten MIT Anlage — und steht damit direkt neben den vermiedenen
            // Kosten, die sich auf den Bezug OHNE Anlage beziehen. Der Titel sagt
            // seither, welche der beiden Größen gemeint ist.
            if (Irgendein(menge, e => e.StromkostenTarif.HasValue))
                z.Add(Zahl("STROMKOSTEN_TARIF",
                           tarif != null && tarif.Aktiv && tarif.RollenModus
                               ? MyResource.Resource.WIRT_ZEILE_STROMKOSTEN_RESTSTROM
                               : MyResource.Resource.WIRT_ZEILE_STROMKOSTEN_BEZUG,
                           e => e.StromkostenTarif));

            // Die BEZUGSSPITZE — eine HERLEITUNG, keine Geldzeile: Sie steht zwischen
            // den Stromkosten und dem Rest, weil sie erklärt, wie der Leistungsanteil
            // des Strompreises zustande kommt, und weil an ihr der Effekt der
            // Lastspitzenkappung abzulesen ist (Stamm gegen Speichervariante). Sie
            // erscheint nur, wenn irgendein Lauf der Gruppe Zeitreihen geführt hat —
            // ohne sie stünde in jedem Bericht eine „—"-Zeile.
            if (Irgendein(menge, e => e.BezugsspitzeKW.HasValue))
                z.Add(Zahl("BEZUGSSPITZE_STROM", MyResource.Resource.WIRT_ZEILE_BEZUGSSPITZE_STROM,
                           e => e.BezugsspitzeKW));

            if (Irgendein(menge, e => e.CO2AbgabeJahr > 0))
                z.Add(Zahl("CO2_BEHG", MyResource.Resource.WIRT_ZEILE_CO2_BEHG,
                           e => (double?)e.CO2AbgabeJahr));

            // =================================================================
            // ETAPPE B7 — DIE RUBRIK „Erlöse und Vorteile" (Konzept § 2.6)
            //
            // Bis hierher standen die Erlöspositionen verstreut zwischen den Kosten:
            // Einspeiseerlös, dann der PV-Block, dann KWKG, dann die drei Steuerzeilen,
            // dann die vermiedenen Kosten. Jede für sich richtig — zusammen aber keine
            // Aussage, und vor allem keine, die Zahlung von Ausweis trennt.
            //
            // Seit B7 sind es ZWEI Blöcke, und die Teilung ist die Aussage:
            //   BLOCK A geht in den Kapitalwert und wird summiert.
            //   BLOCK B wird AUSGEWIESEN und NIE summiert — der Vorteil steckt bereits
            //   in einer anderen Position (kleinere Bezugsrechnung), ein zweites Buchen
            //   wäre Doppelzählung (E5).
            // Die Reihenfolge folgt dem Entwurf „Ergebnis, Bandbreite, Herkunft", damit
            // die Rubrik später ohne Umbau in die neue Ergebnisansicht wandert.
            // =================================================================

            // Führt das Projekt überhaupt eine Anlage, für die die Position gilt? Daran
            // — nicht am Betrag — hängt, ob eine A-Zeile erscheint (ImmerZeigen).
            bool hatBhkw = Irgendein(menge, e => e.KwkgVbhElektrisch > 0 ||
                                                 e.KwkgErloesJahr1 > 0 ||
                                                 e.EnergiesteuerJahr1 > 0 ||
                                                 e.StromsteuerBefreiungJahr1 > 0 ||
                                                 e.EinspeiseerloesKwkJahr != 0 ||
                                                 (e.KwkgModule != null && e.KwkgModule.Count > 0));

            var blockA = new List<WirtZeile>();   // Grundlage der Summenzeile

            z.Add(Kopf("ERL_KOPF_A", MyResource.Resource.WIRT_ERL_KOPF_A, WirtZeile.BLOCK_A));

            // ---- A1/A2: KWK-Zuschlag nach § 7 KWKG --------------------------------
            // OHNE BHKW entsteht die Zeile gar nicht erst. „Immer zeigen" heißt: auch
            // bei Betrag 0 — nicht: in jedem Projekt. Ein reines PV- oder Kesselprojekt
            // bekäme sonst zwei Nullzeilen über Vorschriften, die es nicht betreffen.
            if (hatBhkw)
            {
                WirtZeile aKwkg = Erloes(blockA, "ERL_A_KWKG", MyResource.Resource.WIRT_ERL_A_KWKG,
                                         e => (double?)e.KwkgErloesJahr1, true);
                aKwkg.Grundtext = e => MyResource.Resource.WIRT_GRUND_KWKG;
                z.Add(aKwkg);
            }

            // Die anlagenscharfe Aufteilung in Einspeisung (§ 7 Abs. 1) und Eigenstrom
            // (§ 7 Abs. 2) trägt der Modulnachweis des Laufs. Er reist seit B7P im
            // Nachweisumschlag des Ergebnisses mit — die beiden Unterzeilen erscheinen
            // deshalb auch beim gebuchten Stand, nicht nur im frisch gerechneten Lauf.
            if (Irgendein(menge, e => e.KwkgModule != null && e.KwkgModule.Count > 0))
            {
                z.Add(Unter("ERL_A1_EINSPEISUNG", MyResource.Resource.WIRT_ERL_A1_EINSPEISUNG,
                            e => KwkgAnteil(e, true), WirtZeile.BLOCK_A));
                z.Add(Unter("ERL_A2_EIGEN", MyResource.Resource.WIRT_ERL_A2_EIGEN,
                            e => KwkgAnteil(e, false), WirtZeile.BLOCK_A));
            }
            if (Irgendein(menge, e => e.KwkgVbhElektrisch > 0))
                z.Add(Unter("VBH_ELEKTRISCH", MyResource.Resource.WIRT_ZEILE_VBH_ELEKTRISCH,
                            e => e.KwkgVbhElektrisch > 0 ? (double?)e.KwkgVbhElektrisch : null,
                            WirtZeile.BLOCK_A));

            // ---- A3: Pauschale nach § 9 KWKG (AUFTRAG U17) ------------------------
            // Sie erscheint NUR, wenn sie greift — also wenn der Schalter gesetzt ist
            // UND die Anlage unter der Leistungsgrenze von 2 kW(el) bleibt. Über der
            // Grenze bleibt der Schalter ohne Wirkung, der laufende Zuschlag rechnet
            // weiter, und eine Nullzeile über eine Vorschrift, die nicht greift, wäre
            // eine Behauptung. Deshalb KEIN „ImmerZeigen": Der Betrag ist die
            // Bedingung, nicht die Anlagenart.
            //
            // KEIN SUMMAND. Die Summe darunter ist eine €/a-Summe des Jahres 1; die
            // Pauschale ist ein EINMALBETRAG in € zum Zeitpunkt 0. Beides zu addieren
            // wäre derselbe Einheitenfehler, aus dem auch der Restwert-Barwert nicht in
            // dieser Summe steht. Im KAPITALWERT ist sie seit jeher enthalten (Index 0
            // der Erlösreihe KWKG_PAUSCHALE, unabgezinst) — diese Zeile macht sie
            // sichtbar, sie bucht nichts.
            if (Irgendein(menge, e => e.KwkgPauschaleEur > 0))
                z.Add(new WirtZeile
                {
                    Schluessel = "ERL_A_KWKG_PAUSCHALE",
                    Titel = MyResource.Resource.WIRT_ERL_A_KWKG_PAUSCHALE,
                    Block = WirtZeile.BLOCK_A,
                    Wert = e => e != null && e.KwkgPauschaleEur > 0
                                ? (double?)e.KwkgPauschaleEur : null
                });

            // ---- A4/A5: Energiesteuer-Entlastung ---------------------------------
            // § 53/§ 53a (BHKW-Brennstoff) und § 54 (Heizstoff) stehen in EINER Zahl:
            // Der Rechner gibt eine Summe zurück (SteuerErgebnis.EnergiesteuerEur), die
            // Aufteilung wäre eine neue Größe im Rechner. Der Zeilentitel nennt deshalb
            // beide Vorschriften; welche gegriffen hat, sagt die Herkunft der Sätze.
            if (hatBhkw)
            {
                WirtZeile aEnst = Erloes(blockA, "ERL_A_ENERGIESTEUER",
                                         MyResource.Resource.WIRT_ERL_A_ENERGIESTEUER,
                                         e => (double?)e.EnergiesteuerJahr1, true);
                aEnst.Grundtext = e => MyResource.Resource.WIRT_GRUND_ENERGIESTEUER;
                z.Add(aEnst);
            }

            // ---- A6: Stromsteuer-Entlastung Netzbezug (§ 9b) ---------------------
            // Sie hängt am RESTBEZUG, nicht an einer Anlage — deshalb gilt sie für
            // jedes Projekt und steht immer da (Konzept § 2.6, Klarstellung 2).
            WirtZeile aEntl = Erloes(blockA, "ERL_A_STROMST_ENTLASTUNG",
                                     MyResource.Resource.WIRT_ERL_A_STROMST_ENTLASTUNG,
                                     e => (double?)e.StromsteuerEntlastungJahr1, true);
            aEntl.Grundtext = e => MyResource.Resource.WIRT_GRUND_NUR_PROD_GEWERBE;
            z.Add(aEntl);

            // ---- A7: Stromsteuer-Befreiung Eigenverbrauch (§ 9 Abs. 1 Nr. 3) -----
            // Sie folgt dem Modus aus B6: ERLOES bucht sie (Block A), AUSWEIS zeigt sie
            // nur (Block B, weiter unten). Ein Projekt kann nur eines von beidem sein.
            bool befreiungAlsErloes = Irgendein(menge, e => e.StromsteuerBefreiungAlsErloes);
            if (Irgendein(menge, e => e.StromsteuerBefreiungJahr1 > 0) && befreiungAlsErloes)
                z.Add(Erloes(blockA, "ERL_A_STROMST_BEFREIUNG",
                             MyResource.Resource.WIRT_ERL_A_STROMST_BEFREIUNG,
                             e => (double?)e.StromsteuerBefreiungJahr1, false));

            // ---- A8: Einspeiseerlös Strom ----------------------------------------
            WirtZeile aEinsp = Erloes(blockA, "EINSPEISEERLOES",
                                      MyResource.Resource.WIRT_ERL_A_EINSPEISUNG,
                                      e => (double?)e.EinspeiseerloesJahr, true);
            aEinsp.Grundtext = e => MyResource.Resource.WIRT_GRUND_EINSPEISUNG;
            z.Add(aEinsp);
            // Aufschlüsselung nur, wenn beide Anteile vorkommen; bei einem reinen PV-
            // oder reinen KWK-Projekt wäre sie die Gesamtzeile ein zweites Mal.
            if (Irgendein(menge, e => e.EinspeiseerloesPvJahr != 0) &&
                Irgendein(menge, e => e.EinspeiseerloesKwkJahr != 0))
            {
                z.Add(Unter("EINSPEISEERLOES_PV", MyResource.Resource.WIRT_ZEILE_EINSPEISEERLOES_PV,
                            e => (double?)e.EinspeiseerloesPvJahr, WirtZeile.BLOCK_A));
                z.Add(Unter("EINSPEISEERLOES_KWK", MyResource.Resource.WIRT_ZEILE_EINSPEISEERLOES_KWK,
                            e => (double?)e.EinspeiseerloesKwkJahr, WirtZeile.BLOCK_A));
            }

            // ---- A9: PV-Vergütung (PV-Konzept § 6.4, Etappe P6) ------------------
            // Der Block erscheint nur, wenn irgendein Lauf der Gruppe den
            // Vergütungsdialog aktiv hatte; die MENGENzeilen des Ausweises (Kappung,
            // Vergütungsausfall, vermiedener Bezug) stehen seit B7 in Block B.
            if (Irgendein(menge, e => !string.IsNullOrEmpty(e.PvVerguetungsform)))
            {
                z.Add(new WirtZeile
                {
                    Schluessel = "PV_FORM",
                    Titel = MyResource.Resource.WIRT_ZEILE_PV_FORM,
                    Block = WirtZeile.BLOCK_A,
                    Einzug = 1,
                    Text = e => PvFormText(e.PvVerguetungsform)
                });
                WirtZeile aw = Unter("PV_AW", MyResource.Resource.WIRT_ZEILE_PV_AW,
                                     e => e.PvAnzulegenderWert, WirtZeile.BLOCK_A);
                aw.Format = "N2"; aw.ExcelFormat = "#,##0.00";
                z.Add(aw);
                if (Irgendein(menge, e => e.PvMarktpraemie > 0))
                    z.Add(Erloes(blockA, "PV_MARKTPRAEMIE", MyResource.Resource.WIRT_ZEILE_PV_MARKTPRAEMIE,
                                 e => (double?)e.PvMarktpraemie, false));
                if (Irgendein(menge, e => e.PvKompensation51a > 0))
                    z.Add(Erloes(blockA, "PV_51A", MyResource.Resource.WIRT_ZEILE_PV_51A,
                                 e => (double?)e.PvKompensation51a, false));
            }

            // ---- Summenzeile: NUR Block A ----------------------------------------
            // Sie summiert die €/a-Zeilen des Jahres 1. Der Restwert (A10) steht
            // bewusst NICHT darin: Er ist ein BARWERT über T und stünde in einer
            // €/a-Summe als Einheitenfehler. Seine Zeile bleibt unten beim Kapitalwert,
            // wo sie mit dem Nettobarwert zusammen gelesen wird. Aus demselben Grund
            // fehlt die KWKG-Pauschale (A3): ein Einmalbetrag in € zum Zeitpunkt 0.
            var summanden = new List<WirtZeile>(blockA);
            z.Add(new WirtZeile
            {
                Schluessel = "ERL_A_SUMME",
                Titel = MyResource.Resource.WIRT_ERL_A_SUMME,
                Block = WirtZeile.BLOCK_A,
                IstSumme = true,
                Wert = e =>
                {
                    if (e == null) return null;
                    double summe = 0;
                    foreach (WirtZeile s in summanden)
                    {
                        double? w = s.Wert == null ? null : s.Wert(e);
                        if (w.HasValue) summe += w.Value;
                    }
                    return (double?)summe;
                }
            });

            // =================================================================
            // BLOCK B — Ausweis, NICHT addieren
            // =================================================================
            bool hatVermieden = Irgendein(menge, e => e.VermiedenGesamtJahr != 0 ||
                                                      e.VermiedenArbeitJahr != 0);
            bool hatBefreiungAusweis = Irgendein(menge, e => e.StromsteuerBefreiungJahr1 > 0) &&
                                       !befreiungAlsErloes;
            bool hatPvAusweis = Irgendein(menge, e => e.PvVermiedenerBezug.HasValue ||
                                                      e.PvKappungsverlustKwh > 0 ||
                                                      e.PvVerguetungsausfallKwh > 0);
            if (hatVermieden || hatBefreiungAusweis || hatPvAusweis)
            {
                z.Add(Kopf("ERL_KOPF_B", MyResource.Resource.WIRT_ERL_KOPF_B, WirtZeile.BLOCK_B));

                // A7 im Modus AUSWEIS (B6): Auf selbst erzeugten und selbst verbrauchten
                // Strom entsteht gar keine Stromsteuer — der Vorteil steckt bereits in
                // der kleineren Bezugsrechnung.
                if (hatBefreiungAusweis)
                    z.Add(Ausweis("ERL_B_STROMST_BEFREIUNG",
                                  MyResource.Resource.WIRT_ZEILE_STROMST_BEFREIUNG_AUSWEIS,
                                  e => (double?)e.StromsteuerBefreiungJahr1));

                // B1 — vermiedene Stromkosten, BRUTTO, darunter die Korrektur und der
                // effektive Betrag (Konzept § 2.6, Klarstellung 1).
                if (hatVermieden)
                {
                    z.Add(Ausweis("VERMIEDEN_GESAMT", MyResource.Resource.WIRT_ERL_B1_BRUTTO,
                                  e => (double?)e.VermiedenGesamtJahr));
                    z.Add(Unter("VERMIEDEN_ARBEIT", MyResource.Resource.WIRT_ZEILE_VERMIEDEN_ARBEIT,
                                e => (double?)e.VermiedenArbeitJahr, WirtZeile.BLOCK_B));
                    z.Add(Unter("VERMIEDEN_LEISTUNG", MyResource.Resource.WIRT_ZEILE_VERMIEDEN_LEISTUNG,
                                e => (double?)e.VermiedenLeistungJahr, WirtZeile.BLOCK_B));

                    // Die Korrektur erscheint NUR, wo sie gilt — beim produzierenden
                    // Gewerbe mit bestimmbarer vermiedener Menge. Sonst IST brutto
                    // effektiv, und zwei gleiche Zahlen untereinander erklärten nichts.
                    if (Irgendein(menge, e => e.VermiedenEntlastung9bJahr != 0))
                    {
                        z.Add(Unter("ERL_B1_ABZUG_9B", MyResource.Resource.WIRT_ERL_B1_ABZUG_9B,
                                    e => (double?)(-e.VermiedenEntlastung9bJahr), WirtZeile.BLOCK_B));
                        WirtZeile eff = Ausweis("ERL_B1_EFFEKTIV",
                                                MyResource.Resource.WIRT_ERL_B1_EFFEKTIV,
                                                e => (double?)e.VermiedenEffektivJahr);
                        eff.IstSumme = true;      // Zwischenergebnis des Ausweises
                        z.Add(eff);
                    }
                }

                // B2 — PV-Ausweis: vermiedener Bezug sowie Kappungs- und Ausfallmengen.
                if (Irgendein(menge, e => e.PvVermiedenerBezug.HasValue))
                    z.Add(Ausweis("PV_VERMIEDEN", MyResource.Resource.WIRT_ZEILE_PV_VERMIEDEN,
                                  e => e.PvVermiedenerBezug));
                if (Irgendein(menge, e => e.PvVerguetungsausfallKwh > 0))
                {
                    z.Add(Ausweis("PV_AUSFALL_KWH", MyResource.Resource.WIRT_ZEILE_PV_AUSFALL_KWH,
                                  e => (double?)e.PvVerguetungsausfallKwh));
                    z.Add(Unter("PV_AUSFALL_EUR", MyResource.Resource.WIRT_ZEILE_PV_AUSFALL_EUR,
                                e => (double?)e.PvVerguetungsausfall, WirtZeile.BLOCK_B));
                }
                if (Irgendein(menge, e => e.PvKappungsverlustKwh > 0))
                    z.Add(Ausweis("PV_KAPPUNG", MyResource.Resource.WIRT_ZEILE_PV_KAPPUNG,
                                  e => (double?)e.PvKappungsverlustKwh));
            }

            // SP-E-2: Die Zeile „Aufschläge auf den Strombezug" ist entfallen. Die
            // Preisanteile zerlegen den Arbeitspreis und stecken damit vollständig in
            // den Energiekosten; eine eigene Zeile wäre eine zweite Ansage derselben
            // Zahl.

            // ETAPPE W5-B-10 (VALERI-Abgleich): Die Ersatzbeschaffungen wurden seit W1
            // GERECHNET, aber nie AUSGEWIESEN - sie steckten stumm in den Barwerten der
            // Ausgaben. DIN EN 17463 verlangt sie als eigene Position; zusammen mit dem
            // Restwert darunter sind sie die zwei Groessen, an denen haengt, ob ein
            // Betrachtungszeitraum ueberhaupt zur Nutzungsdauer passt. Die Zeile
            // erscheint nur, wo es Ersatz gibt (n < T bei irgendeiner Position).
            if (Irgendein(menge, e => e.ErsatzBarwert != 0))
                z.Add(Zahl("ERSATZ", MyResource.Resource.WIRT_ZEILE_ERSATZ,
                           e => (double?)e.ErsatzBarwert));

            z.Add(Zahl("RESTWERT", MyResource.Resource.WIRT_ZEILE_RESTWERT,
                       e => (double?)e.RestwertBarwert));
            z.Add(Zahl("NETTOBARWERT", MyResource.Resource.WIRT_ZEILE_NETTOBARWERT,
                       e => e.Kapitalwert));

            WirtZeile diff = Zahl("KAPITALWERT_DIFF", MyResource.Resource.WIRT_ZEILE_KAPITALWERT_DIFF,
                                  e => e.KapitalwertDiff);
            diff.StammAnzeige = MyResource.Resource.WIRT_ZEILE_STAMM_REFERENZ;
            z.Add(diff);

            WirtZeile ann = Zahl("ANNUITAET", MyResource.Resource.WIRT_ZEILE_ANNUITAET,
                                 e => e.AnnuitaetKW);
            ann.StammAnzeige = "—";
            z.Add(ann);

            WirtZeile amo = Zahl("AMORTISATION", MyResource.Resource.WIRT_ZEILE_AMORTISATION,
                                 e => e.AmortisationJahre);
            amo.Format = "N1"; amo.ExcelFormat = "#,##0.0"; amo.StammAnzeige = "—";
            z.Add(amo);

            if (Irgendein(menge, e => e.IRR.HasValue))
            {
                WirtZeile irr = Zahl("IRR", MyResource.Resource.WIRT_ZEILE_IRR, e => e.IRR);
                irr.Format = "N1"; irr.ExcelFormat = "#,##0.0"; irr.StammAnzeige = "—";
                z.Add(irr);
            }

            WirtZeile geste = Zahl("GESTEHUNGSKOSTEN", MyResource.Resource.WIRT_ZEILE_GESTEHUNGSKOSTEN,
                                   e => e.Gestehungskosten);
            geste.Format = "N3"; geste.ExcelFormat = "#,##0.000";
            z.Add(geste);

            // ETAPPE E7 — die Herkunft der verwendeten Steuersätze stand bisher NUR im
            // Ergebnisreiter. Der Bericht ist aber das Dokument, mit dem der Rechtsstand
            // gegenüber Dritten nachgewiesen wird; der Reiter ist es nicht.
            if (Irgendein(menge, e => !string.IsNullOrEmpty(e.SteuerHerkunft)))
                z.Add(new WirtZeile
                {
                    Schluessel = "STEUER_HERKUNFT",
                    Titel = MyResource.Resource.WIRT_ZEILE_STEUER_HERKUNFT,
                    Text = e => e.SteuerHerkunft
                });

            return z;
        }

        private static WirtZeile Zahl(string schluessel, string titel,
                                      Func<WirtschaftlichkeitErgebnis, double?> wert)
        {
            return new WirtZeile { Schluessel = schluessel, Titel = titel, Wert = wert };
        }

        // =====================================================================
        // ETAPPE B7 — Bausteine der Rubrik (Konzept § 2.6)
        // =====================================================================

        /// <summary>Blocküberschrift ohne Werte.</summary>
        private static WirtZeile Kopf(string schluessel, string titel, string block)
        {
            return new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Block = block,
                IstUeberschrift = true,
                Text = e => ""          // Überschriften tragen in jeder Spalte nichts
            };
        }

        /// <summary>
        /// Eine Zeile des zahlungswirksamen Blocks A. Sie wird in
        /// <paramref name="summanden"/> vermerkt — daraus entsteht die Summenzeile, und
        /// zwar aus genau denselben Zeilen, die darüber stehen. Eine von Hand
        /// nachgeführte Summe wäre die zweite Stelle, an der jemand eine neue Position
        /// vergessen kann.
        /// </summary>
        private static WirtZeile Erloes(List<WirtZeile> summanden, string schluessel, string titel,
                                        Func<WirtschaftlichkeitErgebnis, double?> wert,
                                        bool immerZeigen)
        {
            var z = new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Wert = wert,
                Block = WirtZeile.BLOCK_A,
                ImmerZeigen = immerZeigen
            };
            summanden.Add(z);
            return z;
        }

        /// <summary>
        /// Eine Zeile des Ausweisblocks B. Sie steht NIE in der Summe — sie wird
        /// deshalb auch keiner Summandenliste beigelegt; die Trennung ist kein Flag,
        /// das jemand setzen oder vergessen könnte, sondern zwei verschiedene Wege in
        /// die Liste.
        /// </summary>
        private static WirtZeile Ausweis(string schluessel, string titel,
                                         Func<WirtschaftlichkeitErgebnis, double?> wert)
        {
            return new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Wert = wert,
                Block = WirtZeile.BLOCK_B
            };
        }

        /// <summary>Unter-/Herleitungszeile eines Blocks (Einzug 1, nie in der Summe).</summary>
        private static WirtZeile Unter(string schluessel, string titel,
                                       Func<WirtschaftlichkeitErgebnis, double?> wert, string block)
        {
            return new WirtZeile
            {
                Schluessel = schluessel,
                Titel = titel,
                Wert = wert,
                Block = block,
                Einzug = 1
            };
        }

        /// <summary>
        /// Der Anteil des KWK-Zuschlags, der auf die EINSPEISUNG (§ 7 Abs. 1 KWKG) bzw.
        /// auf den EIGENSTROM (§ 7 Abs. 2 KWKG) entfällt [€/a], über alle Module des
        /// Laufs. <c>null</c> = kein Modulnachweis (geladener statt frisch gerechneter
        /// Stand) — dann entfällt die Zeile wie jede andere ohne Wert.
        ///
        /// <para>Gerechnet wird aus Menge × Satz je Modul, also aus denselben Größen,
        /// mit denen der KWKG-Rechner den Jahresbetrag gebildet hat. Der Eigenanteil
        /// eines Moduls ohne Tatbestand nach § 6 Abs. 3 ist dort 0 und bleibt es
        /// hier — der Satz steht dann auf 0.</para>
        /// </summary>
        private static double? KwkgAnteil(WirtschaftlichkeitErgebnis e, bool einspeisung)
        {
            if (e == null || e.KwkgModule == null || e.KwkgModule.Count == 0) return null;
            double summe = 0;
            foreach (KwkgModulNachweis n in e.KwkgModule)
            {
                if (n == null) continue;
                summe += einspeisung
                    ? n.EinspeisungMWh * 1000.0 * n.SatzEinspeisungCt / 100.0
                    : n.EigenMWh * 1000.0 * n.SatzEigenCt / 100.0;
            }
            return (double?)summe;
        }

        /// <summary>
        /// ETAPPE B7 — die Anlagen der Gruppe in Ausgabereihenfolge (je Name einmal).
        /// Ohne Modulnachweis (geladener Stand) leer — dann entfällt die
        /// Aufschlüsselung, und die Energiekostenzeile steht allein wie zuvor.
        /// </summary>
        private static List<string> Anlagennamen(IList<WirtschaftlichkeitErgebnis> menge)
        {
            var namen = new List<string>();
            foreach (WirtschaftlichkeitErgebnis e in menge)
            {
                if (e == null || e.EnergiekostenJeAnlage == null) continue;
                foreach (EnergieAnlageNachweis n in e.EnergiekostenJeAnlage)
                    if (n != null && !namen.Contains(n.Anlage)) namen.Add(n.Anlage);
            }
            return namen;
        }

        /// <summary>Die Energiekosten EINER Anlage [€/a]; <c>null</c> = dieses Projekt
        /// führt die Anlage nicht (Anzeige „—").</summary>
        private static double? AnlageKosten(WirtschaftlichkeitErgebnis e, string anlage)
        {
            if (e == null || e.EnergiekostenJeAnlage == null) return null;
            double summe = 0;
            bool gefunden = false;
            foreach (EnergieAnlageNachweis n in e.EnergiekostenJeAnlage)
                if (n != null && n.Anlage == anlage) { summe += n.KostenEur; gefunden = true; }
            return gefunden ? (double?)summe : null;
        }

        /// <summary>
        /// Sprachneutrale ASCII-Form eines Anlagennamens für den Zeilenschlüssel — der
        /// Schlüssel ist eingefroren und darf keinen Anzeigetext tragen (Drei-Schichten-
        /// Regel). Alles außerhalb von A–Z, a–z und 0–9 wird zum Unterstrich.
        /// </summary>
        private static string Schluesselform(string name)
        {
            if (string.IsNullOrEmpty(name)) return "X";
            var b = new System.Text.StringBuilder(name.Length);
            foreach (char c in name)
                b.Append((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                         (c >= '0' && c <= '9') ? char.ToUpperInvariant(c) : '_');
            return b.ToString();
        }

        /// <summary>
        /// ETAPPE B7 — die Herleitung EINER Anlagenzeile als Klartext
        /// („4.200,00 L × 0,9500 €/L = 3.990,00 €/a"). Sie steht im Bericht als
        /// Untertabelle und im Reiter als Kurztext; leer, wenn die Anlage in diesem
        /// Ergebnis nicht vorkommt.
        /// </summary>
        public static string AnlageHerleitung(WirtschaftlichkeitErgebnis e, string anlage,
                                              System.Globalization.CultureInfo kultur)
        {
            if (e == null || e.EnergiekostenJeAnlage == null) return "";
            if (kultur == null) kultur = System.Globalization.CultureInfo.CurrentCulture;
            foreach (EnergieAnlageNachweis n in e.EnergiekostenJeAnlage)
            {
                if (n == null || n.Anlage != anlage) continue;
                return n.MengeAbrechnung.ToString("N2", kultur) + " " + n.Einheit + " × " +
                       n.PreisJeEinheit.ToString("N4", kultur) + " €/" + n.Einheit + " = " +
                       n.KostenEur.ToString("N2", kultur) + " €/a (" + n.Traeger + ")";
            }
            return "";
        }

        /// <summary>
        /// ETAPPE B7 — DIE EINE Sichtbarkeitsregel für Reiter, Word und Excel.
        ///
        /// <para><b>Der Befund, der sie erzwang.</b> Bis B7 filterte der Ergebnisreiter
        /// die Zeilenliste ein zweites Mal (über die gerade gewählten Spalten), Excel
        /// ein drittes Mal (über den Szenarioblock), Word gar nicht. Seite und Bericht
        /// konnten damit verschiedene Tabellen zeigen — genau das, was
        /// <see cref="Kennzahlen"/> seit E7 verhindern soll. Die Regel steht seither
        /// hier, und die drei Ausgaben rufen sie, statt je eine eigene zu führen.</para>
        ///
        /// <para>Über die Sichtbarkeit entscheidet die MENGE der Gruppe, nie ein
        /// einzelnes Ergebnis: Eine Zeile, die nur in der Variante einen Wert trägt,
        /// gehört auch in die Stammspalte — dort steht dann „—", und das ist die
        /// Auskunft.</para>
        /// </summary>
        public static List<WirtZeile> Sichtbare(IList<WirtZeile> zeilen,
                                                IList<WirtschaftlichkeitErgebnis> menge)
        {
            var sichtbar = new List<WirtZeile>();
            if (zeilen == null) return sichtbar;

            foreach (WirtZeile z in zeilen)
            {
                if (z == null) continue;
                if (z.IstUeberschrift || z.IstSumme || z.ImmerZeigen) { sichtbar.Add(z); continue; }
                if (menge == null) continue;
                bool hat = false;
                foreach (WirtschaftlichkeitErgebnis e in menge)
                {
                    if (e == null) continue;
                    if (z.IstText) { if (!string.IsNullOrEmpty(z.Text(e))) { hat = true; break; } }
                    else if (e.IstStamm && z.StammAnzeige != null) { hat = true; break; }
                    else if (z.Wert != null && z.Wert(e).HasValue) { hat = true; break; }
                }
                if (hat) sichtbar.Add(z);
            }

            // Eine Blocküberschrift ohne Zeilen und eine Summe ohne Summanden sind
            // Behauptungen — beide fallen weg. Geprüft wird nach dem Filtern, weil erst
            // dann feststeht, was im Block übrig blieb.
            var ergebnis = new List<WirtZeile>();
            for (int i = 0; i < sichtbar.Count; i++)
            {
                WirtZeile z = sichtbar[i];
                if (z.IstUeberschrift && !FolgtZeile(sichtbar, i, z.Block)) continue;
                if (z.IstSumme && z.Block == WirtZeile.BLOCK_A &&
                    !StehtZeile(ergebnis, WirtZeile.BLOCK_A)) continue;
                ergebnis.Add(z);
            }
            return ergebnis;
        }

        /// <summary>true, wenn HINTER der Überschrift an <paramref name="ab"/> noch
        /// eine gewöhnliche Zeile desselben Blocks folgt (vor der nächsten
        /// Überschrift).</summary>
        private static bool FolgtZeile(IList<WirtZeile> zeilen, int ab, string block)
        {
            for (int i = ab + 1; i < zeilen.Count; i++)
            {
                WirtZeile z = zeilen[i];
                if (z == null) continue;
                if (z.IstUeberschrift) return false;      // der nächste Kopf, nichts dazwischen
                if (z.Block == block && !z.IstSumme) return true;
            }
            return false;
        }

        /// <summary>true, wenn in der bereits gefüllten Liste eine gewöhnliche Zeile
        /// des Blocks steht — die Bedingung der Summenzeile.</summary>
        private static bool StehtZeile(IList<WirtZeile> zeilen, string block)
        {
            foreach (WirtZeile z in zeilen)
                if (z != null && z.Block == block && !z.IstUeberschrift && !z.IstSumme)
                    return true;
            return false;
        }

        /// <summary>
        /// Der Speicherkontext je PROJEKT der Gruppe (VF-1) — einmal gelesen, nicht je
        /// Zelle: Die Zeilenliste entsteht bei jedem Szenariowechsel neu, und derselbe
        /// Text stünde sonst mehrfach in der Datenbank nachgefragt.
        /// </summary>
        private static Dictionary<int, string> Speicherkontexte(
            IList<WirtschaftlichkeitErgebnis> menge)
        {
            var karte = new Dictionary<int, string>();
            foreach (WirtschaftlichkeitErgebnis e in menge)
            {
                if (e == null || e.IdProjekt <= 0 || karte.ContainsKey(e.IdProjekt)) continue;
                string text = SpeicherAnzeigeCtrl.SpeicherKontextText(e.IdProjekt);
                if (!string.IsNullOrEmpty(text)) karte[e.IdProjekt] = text;
            }
            return karte;
        }

        /// <summary>Klartext der Vermarktungsform (Persistenzwert ist ASCII, P6).</summary>
        private static string PvFormText(string form)
        {
            if (form == DbWerte.PV_VERMARKTUNG_EV) return MyResource.Resource.WIRT_ZEILE_PV_FORM_EV;
            if (form == DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE) return MyResource.Resource.WIRT_ZEILE_PV_FORM_MP;
            if (form == DbWerte.PV_VERMARKTUNG_SONSTIGE_DV) return MyResource.Resource.WIRT_ZEILE_PV_FORM_DV;
            if (form == DbWerte.PV_VERMARKTUNG_KEINE) return MyResource.Resource.WIRT_ZEILE_PV_FORM_KEINE;
            return form;
        }

        private static bool Irgendein(IList<WirtschaftlichkeitErgebnis> menge,
                                      Func<WirtschaftlichkeitErgebnis, bool> bedingung)
        {
            foreach (WirtschaftlichkeitErgebnis e in menge)
                if (e != null && bedingung(e)) return true;
            return false;
        }

        // =====================================================================
        // Anzeigetexte der Steuerwerte aus Etappe E3
        // =====================================================================

        /// <summary>Anzeigetext einer Kostenart (<c>DbWerte.KOSTENART_*</c>).</summary>
        public static string KostenartText(string steuerwert)
        {
            if (string.Equals(steuerwert, DbWerte.KOSTENART_KAPITALGEBUNDEN, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_KAPITALGEBUNDEN;
            if (string.Equals(steuerwert, DbWerte.KOSTENART_BETRIEBSGEBUNDEN, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_BETRIEBSGEBUNDEN;
            if (string.Equals(steuerwert, DbWerte.KOSTENART_BEDARFSGEBUNDEN, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_BEDARFSGEBUNDEN;
            if (string.Equals(steuerwert, DbWerte.KOSTENART_SONSTIGE, StringComparison.Ordinal))
                return MyResource.Resource.KOSTENART_SONSTIGE;
            return MyResource.Resource.KOSTENART_OHNE;
        }

        /// <summary>Reihenfolge der Kostenarten im Bericht — nach VDI 2067.</summary>
        public static readonly string[] Kostenarten =
        {
            DbWerte.KOSTENART_KAPITALGEBUNDEN,
            DbWerte.KOSTENART_BEDARFSGEBUNDEN,
            DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
            DbWerte.KOSTENART_SONSTIGE,
            ""                                   // nicht eingeordnet
        };

        /// <summary>Anzeigetext einer Bemessungsart (<c>DbWerte.BEMESSUNG_*</c>).</summary>
        public static string BemessungText(string steuerwert)
        {
            if (string.Equals(steuerwert, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal))
                return MyResource.Resource.BEMESSUNG_PROZENT_INVESTITION;
            if (string.Equals(steuerwert, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal))
                return MyResource.Resource.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN;
            if (string.Equals(steuerwert, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal))
                return MyResource.Resource.BEMESSUNG_EUR_PRO_H;
            if (string.Equals(steuerwert, DbWerte.BEMESSUNG_EUR_PRO_KWH, StringComparison.Ordinal))
                return MyResource.Resource.BEMESSUNG_EUR_PRO_KWH;
            return MyResource.Resource.BEMESSUNG_BETRAG;
        }

        /// <summary>
        /// Herleitung einer Kostenposition als Klartext („1.500 h/a × 2,50 €/h").
        /// Leer, wenn die Position ein fester Betrag ist oder ein Szenariowert die
        /// Ableitung geschlagen hat — dann steht keine Herleitung dahinter.
        /// </summary>
        public static string Herleitung(KostenPositionNachweis n,
                                        System.Globalization.CultureInfo kultur)
        {
            if (n == null || n.SzenarioGepflegt) return "";
            if (string.IsNullOrEmpty(n.Bemessung) ||
                string.Equals(n.Bemessung, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal))
                return "";
            if (!n.Menge.HasValue || !n.Einheitpreis.HasValue) return "";
            // ANWENDERENTSCHEID 15.09.2026: Die Satzeinheit folgt der Bezugsgröße des
            // GEWERKS — am Pufferspeicher ist sie „€/Ltr.", nicht „€/kW".
            // U33 (18.09.2026): Diese Liste sind die BETRIEBSKOSTENzeilen; ein
            // Leistungssatz heißt hier „€/kWp·a" und nicht „€/kWp".
            return n.Menge.Value.ToString("N2", kultur) + " " +
                   BetriebskostenCtrl.MengenEinheit(n.Bemessung) + " × " +
                   n.Einheitpreis.Value.ToString("N3", kultur) + " " +
                   BetriebskostenCtrl.SatzEinheit(n.Bemessung, n.Komponente, true);
        }
    }

    // =========================================================================
    /// <summary>
    /// ETAPPE E7 — eine Positionsspalte der Mehrjahrestabelle.
    /// </summary>
    public sealed class MehrjahresSpalte
    {
        /// <summary>Sprachneutraler Schlüssel (ASCII, eingefroren).</summary>
        public string Schluessel = "";

        /// <summary>Anzeigetitel aus <c>MyResource</c> — bereits lokalisiert.</summary>
        public string Titel = "";

        /// <summary>Nominaler Betrag je Jahr [€], Index 0…T. Ausgaben negativ.</summary>
        public double[] JeJahr;

        /// <summary>true = Summenspalte (Netto, Barwert, kumuliert); sie bleibt auch
        /// dann stehen, wenn sie nur Nullen führt.</summary>
        public bool IstSumme;

        public double Wert(int t) { return JeJahr != null && t >= 0 && t < JeJahr.Length ? JeJahr[t] : 0; }

        /// <summary>true, wenn die Spalte irgendeinen Betrag ungleich 0 führt.</summary>
        public bool Belegt
        {
            get
            {
                if (JeJahr == null) return false;
                for (int t = 0; t < JeJahr.Length; t++) if (JeJahr[t] != 0) return true;
                return false;
            }
        }
    }

    /// <summary>
    /// ETAPPE E7 — die Mehrjahrestabelle EINES Projekts: Zeilen sind die Jahre 0…T,
    /// Spalten die Positionen des Zahlungsstroms.
    ///
    /// <para><b>Warum Jahre als Zeilen.</b> Bei T = 20 wären 21 Jahresspalten auf A4
    /// nicht darstellbar; der Kapitalwert-Verlauf im Excel-Bericht macht es seit Phase 11
    /// bereits andersherum, und die Tabelle passt damit in beide Ausgaben ohne zweites
    /// Layout.</para>
    ///
    /// <para><b>Vorzeichen.</b> Ausgaben negativ, Einnahmen positiv — dadurch ist die
    /// Summe der Positionsspalten die Spalte „Netto nominal", und die Tabelle prüft sich
    /// selbst. Die letzte Zeile schließt mit dem Restwert-Barwert auf den Nettobarwert
    /// auf.</para>
    ///
    /// <para><b>Was hier NICHT steht:</b> vermiedene Kosten und Aufschlagsbetrag. Beide
    /// stecken bereits in anderen Positionen (in der kleineren Bezugsmenge bzw. in den
    /// Energiekosten); eine eigene Zahlungszeile wäre eine Doppelzählung. Sie erscheinen
    /// unter der Tabelle als ausdrücklich benannter Nachweisblock.</para>
    /// </summary>
    public sealed class Mehrjahresbild
    {
        /// <summary>Betrachtungszeitraum T [a].</summary>
        public int Jahre;

        /// <summary>Die belegten Positionsspalten in Ausgabereihenfolge.</summary>
        public List<MehrjahresSpalte> Spalten = new List<MehrjahresSpalte>();

        /// <summary>Restwert-Barwert zum Zeitpunkt T [€].</summary>
        public double RestwertBarwert;

        /// <summary>Kumulierter Barwert im Jahr T (ohne Restwert) [€].</summary>
        public double KumuliertT;

        /// <summary>Nettobarwert = <see cref="KumuliertT"/> + <see cref="RestwertBarwert"/>.</summary>
        public double Kapitalwert { get { return KumuliertT + RestwertBarwert; } }

        /// <summary>
        /// Baut das Bild aus dem Zahlungsbild einer Verlaufslinie.
        /// <c>null</c>, wenn keine Reihe vorliegt.
        /// </summary>
        public static Mehrjahresbild Baue(VerlaufSerie serie)
        {
            if (serie == null || serie.Bild == null || serie.Kumuliert == null) return null;
            KapitalwertRechner.Zahlungsbild b = serie.Bild;
            if (b.NominalReihe == null || b.BarwertReihe == null) return null;

            int T = b.NominalReihe.Length - 1;
            if (T < 1) return null;

            var m = new Mehrjahresbild
            {
                Jahre = T,
                RestwertBarwert = serie.RestwertBarwert,
                KumuliertT = serie.Kumuliert[Math.Min(T, serie.Kumuliert.Length - 1)]
            };

            // Investition und Ersatzbeschaffung in EINER Spalte: beides ist dieselbe
            // Art Zahlung, nur zu verschiedenen Zeitpunkten — und Jahr 0 trägt ohnehin
            // nichts anderes.
            var investErsatz = new double[T + 1];
            investErsatz[0] = -b.Investition;
            for (int t = 1; t <= T; t++)
                investErsatz[t] = b.ErsatzJeJahr != null && t < b.ErsatzJeJahr.Length
                                  ? -b.ErsatzJeJahr[t] : 0;

            m.Nimm("INVEST_ERSATZ", MyResource.Resource.WIRT_MJ_INVEST_ERSATZ, investErsatz);

            // PAKET FX3 (Anwenderentscheid R-2): Die Spalte „Betrieb" trägt seither
            // zwei verschieden fortgeschriebene Anteile — den Betriebs-Topf mit p_B und
            // den Endenergie-Topf mit p_E (Hilfsenergie „x % der Endenergie…", seit
            // PAKET FX4-b auch „% der Brennstoff-/Stromkosten").
            // KapitalwertRechner.BetriebJeJahr liefert die SUMME beider, genau damit
            // diese Tabelle unverändert bleibt: Die Summe der Positionsspalten ist
            // weiterhin die Spalte „Netto nominal", und die Selbstprüfung darunter
            // (kumuliert(T) + Restwert = Kapitalwert) bleibt gültig. Der p_E-Anteil ist
            // in KapitalwertRechner.Zahlungsbild.EndenergieAnteilJeJahr einzeln
            // ausgewiesen; eine eigene Spalte bekäme er erst mit einem eigenen
            // Anzeigetext (offener Punkt FX3-1).
            m.Nimm("BETRIEB", MyResource.Resource.WIRT_MJ_BETRIEB, Negativ(b.BetriebJeJahr, T));
            m.Nimm("ENERGIE", MyResource.Resource.WIRT_MJ_ENERGIE, Negativ(b.EnergieJeJahr, T));
            m.Nimm("BEHG", MyResource.Resource.WIRT_MJ_BEHG, Negativ(b.BehgJeJahr, T));
            m.Nimm("EINSPEISUNG", MyResource.Resource.WIRT_MJ_EINSPEISUNG,
                   Positiv(b.EinspeiseerloesJeJahr, T));

            // Die vier benannten Erlösreihen aus E4 — hier werden sie zum ersten Mal
            // einzeln sichtbar. Der KWK-Zuschlag zeigt dabei sein Auslaufen.
            m.Reihe(b, KapitalwertRechner.ErloesReihe.KWKG, MyResource.Resource.WIRT_REIHE_KWKG, T);
            m.Reihe(b, KapitalwertRechner.ErloesReihe.ENERGIESTEUER,
                    MyResource.Resource.WIRT_REIHE_ENERGIESTEUER, T);
            m.Reihe(b, KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG,
                    MyResource.Resource.WIRT_REIHE_STROMSTEUER_BEFREIUNG, T);
            m.Reihe(b, KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG,
                    MyResource.Resource.WIRT_REIHE_STROMSTEUER_ENTLASTUNG, T);

            // AUFTRAG U17 — die Pauschale des § 9 KWKG. Sie braucht einen EIGENEN
            // Aufruf, weil ihr Betrag im INDEX 0 steht und Reihe() erst bei t = 1
            // beginnt; ohne die Spalte stimmte die Selbstprüfung der Tabelle im Jahr 0
            // nicht: „Netto nominal" trägt dort −I₀ + Pauschale, die Summe der
            // Positionsspalten aber nur −I₀.
            m.ReiheAbJahr0(b, KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE,
                           MyResource.Resource.WIRT_REIHE_KWKG_PAUSCHALE, T);

            m.Summe("NETTO", MyResource.Resource.WIRT_MJ_NETTO, Kopie(b.NominalReihe, T));
            m.Summe("BARWERT", MyResource.Resource.WIRT_MJ_BARWERT, Kopie(b.BarwertReihe, T));
            m.Summe("KUMULIERT", MyResource.Resource.WIRT_MJ_KUMULIERT, Kopie(serie.Kumuliert, T));
            return m;
        }

        private void Nimm(string schluessel, string titel, double[] werte)
        {
            var s = new MehrjahresSpalte { Schluessel = schluessel, Titel = titel, JeJahr = werte };
            if (s.Belegt) Spalten.Add(s);   // nie eine Spalte aus lauter Nullen
        }

        private void Summe(string schluessel, string titel, double[] werte)
        {
            Spalten.Add(new MehrjahresSpalte
            { Schluessel = schluessel, Titel = titel, JeJahr = werte, IstSumme = true });
        }

        private void Reihe(KapitalwertRechner.Zahlungsbild b, string name, string titel, int T)
        {
            if (!b.HatReihe(name)) return;
            var werte = new double[T + 1];
            for (int t = 1; t <= T; t++) werte[t] = b.ReihenWert(name, t);
            Nimm(name, titel, werte);
        }

        /// <summary>
        /// AUFTRAG U17 — wie <see cref="Reihe"/>, nur ab dem JAHR 0. Genau eine Reihe
        /// führt dort einen Betrag: die Pauschale nach § 9 KWKG, die einmalig zum
        /// Zeitpunkt der Investition fließt.
        ///
        /// <para><b>Ohne <c>HatReihe</c>.</b> Auch jene Wache beginnt bei t = 1 — für
        /// eine Reihe, die ihren einzigen Betrag im Index 0 trägt, meldet sie „leer".
        /// Gefiltert wird deshalb erst in <see cref="Nimm"/>, das die fertige Spalte auf
        /// einen Betrag ungleich 0 prüft — über ALLE Jahre.</para>
        /// </summary>
        private void ReiheAbJahr0(KapitalwertRechner.Zahlungsbild b, string name, string titel, int T)
        {
            var werte = new double[T + 1];
            for (int t = 0; t <= T; t++) werte[t] = b.ReihenWert(name, t);
            Nimm(name, titel, werte);
        }

        private static double[] Negativ(double[] quelle, int T)
        {
            var z = new double[T + 1];
            if (quelle != null)
                for (int t = 1; t <= T && t < quelle.Length; t++) z[t] = -quelle[t];
            return z;
        }

        private static double[] Positiv(double[] quelle, int T)
        {
            var z = new double[T + 1];
            if (quelle != null)
                for (int t = 1; t <= T && t < quelle.Length; t++) z[t] = quelle[t];
            return z;
        }

        private static double[] Kopie(double[] quelle, int T)
        {
            var z = new double[T + 1];
            if (quelle != null)
                for (int t = 0; t <= T && t < quelle.Length; t++) z[t] = quelle[t];
            return z;
        }
    }
}
