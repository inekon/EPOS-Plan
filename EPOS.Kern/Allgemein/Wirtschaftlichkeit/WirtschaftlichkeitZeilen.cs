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
            return v.HasValue ? v.Value.ToString(Format, kultur) : "—";
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

            if (Irgendein(menge, e => e.CO2AbgabeJahr > 0))
                z.Add(Zahl("CO2_BEHG", MyResource.Resource.WIRT_ZEILE_CO2_BEHG,
                           e => (double?)e.CO2AbgabeJahr));

            z.Add(Zahl("EINSPEISEERLOES", MyResource.Resource.WIRT_ZEILE_EINSPEISEERLOES,
                       e => (double?)e.EinspeiseerloesJahr));
            // ETAPPE E7 — Aufschlüsselung. Sie erscheint nur, wenn beide Anteile
            // vorkommen; bei einem reinen PV- oder reinen KWK-Projekt wäre sie die
            // Gesamtzeile ein zweites Mal.
            if (Irgendein(menge, e => e.EinspeiseerloesPvJahr != 0) &&
                Irgendein(menge, e => e.EinspeiseerloesKwkJahr != 0))
            {
                z.Add(Zahl("EINSPEISEERLOES_PV", MyResource.Resource.WIRT_ZEILE_EINSPEISEERLOES_PV,
                           e => (double?)e.EinspeiseerloesPvJahr));
                z.Add(Zahl("EINSPEISEERLOES_KWK", MyResource.Resource.WIRT_ZEILE_EINSPEISEERLOES_KWK,
                           e => (double?)e.EinspeiseerloesKwkJahr));
            }

            // ETAPPE P6 (PV-Konzept § 6.4): Ausweis des PV-Vergütungsdialogs —
            // direkt hinter den Einspeisezeilen, deren PV-Anteil er erklärt. Der
            // Block erscheint nur, wenn irgendein Lauf der Gruppe den Dialog aktiv
            // hatte; die Unterzeilen folgen dem üblichen Nullzeilen-Muster.
            if (Irgendein(menge, e => !string.IsNullOrEmpty(e.PvVerguetungsform)))
            {
                z.Add(new WirtZeile
                {
                    Schluessel = "PV_FORM",
                    Titel = MyResource.Resource.WIRT_ZEILE_PV_FORM,
                    Text = e => PvFormText(e.PvVerguetungsform)
                });
                WirtZeile aw = Zahl("PV_AW", MyResource.Resource.WIRT_ZEILE_PV_AW,
                                    e => e.PvAnzulegenderWert);
                aw.Format = "N2"; aw.ExcelFormat = "#,##0.00";
                z.Add(aw);
                if (Irgendein(menge, e => e.PvMarktpraemie > 0))
                    z.Add(Zahl("PV_MARKTPRAEMIE", MyResource.Resource.WIRT_ZEILE_PV_MARKTPRAEMIE,
                               e => (double?)e.PvMarktpraemie));
                if (Irgendein(menge, e => e.PvVerguetungsausfallKwh > 0))
                {
                    z.Add(Zahl("PV_AUSFALL_KWH", MyResource.Resource.WIRT_ZEILE_PV_AUSFALL_KWH,
                               e => (double?)e.PvVerguetungsausfallKwh));
                    z.Add(Zahl("PV_AUSFALL_EUR", MyResource.Resource.WIRT_ZEILE_PV_AUSFALL_EUR,
                               e => (double?)e.PvVerguetungsausfall));
                }
                if (Irgendein(menge, e => e.PvKompensation51a > 0))
                    z.Add(Zahl("PV_51A", MyResource.Resource.WIRT_ZEILE_PV_51A,
                               e => (double?)e.PvKompensation51a));
                if (Irgendein(menge, e => e.PvKappungsverlustKwh > 0))
                    z.Add(Zahl("PV_KAPPUNG", MyResource.Resource.WIRT_ZEILE_PV_KAPPUNG,
                               e => (double?)e.PvKappungsverlustKwh));
                if (Irgendein(menge, e => e.PvVermiedenerBezug.HasValue))
                    z.Add(Zahl("PV_VERMIEDEN", MyResource.Resource.WIRT_ZEILE_PV_VERMIEDEN,
                               e => e.PvVermiedenerBezug));
            }

            if (Irgendein(menge, e => e.KwkgErloesJahr1 > 0))
                z.Add(Zahl("KWKG", MyResource.Resource.WIRT_ZEILE_KWKG,
                           e => (double?)e.KwkgErloesJahr1));
            if (Irgendein(menge, e => e.KwkgVbhElektrisch > 0))
                z.Add(Zahl("VBH_ELEKTRISCH", MyResource.Resource.WIRT_ZEILE_VBH_ELEKTRISCH,
                           e => e.KwkgVbhElektrisch > 0 ? (double?)e.KwkgVbhElektrisch : null));

            if (Irgendein(menge, e => e.EnergiesteuerJahr1 > 0))
                z.Add(Zahl("ENERGIESTEUER", MyResource.Resource.WIRT_ZEILE_ENERGIESTEUER,
                           e => (double?)e.EnergiesteuerJahr1));
            if (Irgendein(menge, e => e.StromsteuerBefreiungJahr1 > 0))
                z.Add(Zahl("STROMST_BEFREIUNG", MyResource.Resource.WIRT_ZEILE_STROMST_BEFREIUNG,
                           e => (double?)e.StromsteuerBefreiungJahr1));
            if (Irgendein(menge, e => e.StromsteuerEntlastungJahr1 > 0))
                z.Add(Zahl("STROMST_ENTLASTUNG", MyResource.Resource.WIRT_ZEILE_STROMST_ENTLASTUNG,
                           e => (double?)e.StromsteuerEntlastungJahr1));

            // ETAPPE E5/E7: vermiedene Kosten. Der Leistungsanteil ist regelmäßig
            // NEGATIV — die Bedingung prüft deshalb auf „ungleich 0". Die Titel tragen
            // seit E7 den Zusatz „(Ausweis)": Es sind keine Zahlungen.
            if (Irgendein(menge, e => e.VermiedenGesamtJahr != 0 || e.VermiedenArbeitJahr != 0))
            {
                z.Add(Zahl("VERMIEDEN_ARBEIT", MyResource.Resource.WIRT_ZEILE_VERMIEDEN_ARBEIT,
                           e => (double?)e.VermiedenArbeitJahr));
                z.Add(Zahl("VERMIEDEN_LEISTUNG", MyResource.Resource.WIRT_ZEILE_VERMIEDEN_LEISTUNG,
                           e => (double?)e.VermiedenLeistungJahr));
                z.Add(Zahl("VERMIEDEN_GESAMT", MyResource.Resource.WIRT_ZEILE_VERMIEDEN_GESAMT,
                           e => (double?)e.VermiedenGesamtJahr));
            }
            if (Irgendein(menge, e => e.AufschlagJahr != 0))
                z.Add(Zahl("AUFSCHLAG", MyResource.Resource.WIRT_ZEILE_AUFSCHLAG,
                           e => (double?)e.AufschlagJahr));

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
            return n.Menge.Value.ToString("N2", kultur) + " " +
                   BetriebskostenCtrl.MengenEinheit(n.Bemessung) + " × " +
                   n.Einheitpreis.Value.ToString("N3", kultur) + " " +
                   BetriebskostenCtrl.SatzEinheit(n.Bemessung);
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
