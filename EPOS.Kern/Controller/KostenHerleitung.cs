using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DIE EINE HERLEITUNG EINER KOSTENZEILE (U28) — woher der Betrag einer Zeile
    /// des Positionsrasters kommt: Bezugsgröße mit Einheit, Herkunft und
    /// Kaskadenrunde.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Der Werkzeugtipp des Betragsfeldes
    /// („Aus Satz und Bezugsgröße des Projekts berechnet: 653,60 €/kW × 300,00 kW.")
    /// entstand bis hierher in der Windows-Hülle
    /// (<c>KostenKomponenteHuelle.BasisKurztext</c>) — Fachtext in einer Schale. Die
    /// sichtbare Herleitungszeile unter dem Betrag kommt hinzu; beide speisen sich
    /// aus DIESER Methode, damit sie nicht auseinanderlaufen können. Die Hülle
    /// reicht nur noch durch.</para>
    ///
    /// <para><b>Sie rechnet nichts.</b> Bezugsgröße, Runde und Herkunft liegen im
    /// Dialog längst vor: Die Zahl kommt aus <see cref="InvestKaskade"/> bzw. aus
    /// der Nachweisliste der Betriebskosten, die Runde und die Herkunft schreibt die
    /// Kaskade beim Ableiten mit. Hier wird nur formuliert.</para>
    /// </summary>
    internal static class KostenHerleitung
    {
        // =====================================================================
        // Steuerwerte der HERKUNFT — die vier Quellen einer Bezugsgröße
        // =====================================================================

        /// <summary>Die Baugröße der Anlage (Gerätewelt, Runde 1).</summary>
        internal const string HERKUNFT_ANLAGE = "ANLAGE";

        /// <summary>Die Hauptpositionen derselben Komponente (Runde 2).</summary>
        internal const string HERKUNFT_HAUPT = "HAUPT";

        /// <summary>Die eingefrorene Investitionssumme der Anlage (Runde 3).</summary>
        internal const string HERKUNFT_STUFE_ANLAGE = "STUFE_ANLAGE";

        /// <summary>… der Komponente (Runde 3, Rückfall).</summary>
        internal const string HERKUNFT_STUFE_KOMPONENTE = "STUFE_KOMPONENTE";

        /// <summary>… des Projekts (Runde 3, letzter Rückfall).</summary>
        internal const string HERKUNFT_STUFE_PROJEKT = "STUFE_PROJEKT";

        /// <summary>Die ausgewiesene Menge des Simulationslaufs.</summary>
        internal const string HERKUNFT_LAUF = "LAUF";

        /// <summary>Die Investitionssumme (Betriebszeile „% der Investition", H4a).</summary>
        internal const string HERKUNFT_INVEST = "INVEST";

        // =====================================================================
        // Die Auskunft
        // =====================================================================

        /// <summary>Alles, was Werkzeugtipp und Herleitungszeile einer Zeile brauchen.</summary>
        internal sealed class Angabe
        {
            /// <summary>Absolute Bemessung: Satz und Betrag sind EIN Wert (§ 5.4).</summary>
            public bool Absolut;

            /// <summary>Zeigt das Kettensymbol — gleichbedeutend mit <see cref="Absolut"/>.</summary>
            public bool Kette;

            /// <summary>Die angesetzte Bezugsgröße; <c>null</c> = keine.</summary>
            public double? Basis;

            /// <summary>Die Bezugsgröße samt Einheit, fertig gesetzt („300,00 kW").</summary>
            public string BasisText = "";

            /// <summary>Herkunft als Steuerwert <c>HERKUNFT_*</c>; leer = unbekannt.</summary>
            public string Herkunft = "";

            /// <summary>Herkunft im Klartext („P_el der Anlage"); leer = keine.</summary>
            public string HerkunftText = "";

            /// <summary>Kaskadenrunde 1…3; 0 = keine Kaskade (Betriebsseite).</summary>
            public int Runde;

            /// <summary>Der Werkzeugtipp des Betragsfeldes — wortgleich zum Bestand.</summary>
            public string Kurztext = "";

            /// <summary>Die Herleitungszeile unter dem Betrag; leer = keine.</summary>
            public string Zeile = "";

            /// <summary>Die Zeile rechnet OHNE Bezugsgröße und nennt dafür einen Grund.</summary>
            public bool OhneBasis;

            /// <summary>U31: Der Empfehlungsbereich als Werkzeugtipp des Satzfeldes
            /// („Empfehlung: 0,02 – 0,04 €/kWh"); leer = keiner gepflegt.</summary>
            public string EmpfehlungKurztext = "";

            /// <summary>U31: Derselbe Bereich als SICHTBARE Zeile unter dem Satzfeld
            /// („Empfehlung 0,02 bis 0,04 €/kWh"); leer = keine Zeile.</summary>
            public string EmpfehlungZeile = "";

            /// <summary>
            /// ANWENDERENTSCHEID 19.09.2026: Die Hinweiszeile unter der Herleitung —
            /// „Vorlage ‚Standard': % des Endenergiebedarfs". Sie steht NUR, wenn die
            /// Standardvorlage dieselbe Position führt und dort eine ANDERE Bemessung
            /// gilt als in dieser Projektzeile; leer heißt keine Zeile.
            /// </summary>
            public string VorlagenZeile = "";

            /// <summary>Die Bemessung der Vorlagenposition als PERSISTENZWERT — den
            /// braucht die Oberfläche, um „übernehmen" auf denselben Eintrag der
            /// Auswahlliste zu setzen. Leer, wo <see cref="VorlagenZeile"/> leer ist.</summary>
            public string VorlagenBemessung = "";
        }

        /// <summary>
        /// Die Herleitung EINER Rasterzeile.
        /// </summary>
        /// <param name="p">Die Position mit Bemessung und Satz.</param>
        /// <param name="komponentenId"><c>Tab_KostenKomponente.ID</c> — das Gewerk
        /// entscheidet über Einheit und Baugröße (Anwenderentscheid 15.09.2026).</param>
        /// <param name="pz">Die Projektzeile mit Bezugsgröße, Grund, Runde und
        /// Herkunft; <c>null</c> im Stammkontext.</param>
        /// <param name="projektModus">Gibt es ein Projekt? Nur dort gibt es
        /// Bezugsgrößen — im Katalog steht im Betragsfeld ein Strich.</param>
        /// <param name="betrieb">U33: true = Betriebsraster. Nur das Einheitenzeichen des
        /// SATZES hängt daran („€/kWp·a" statt „€/kWp"); die Bezugsgröße trägt ihre eigene
        /// physikalische Einheit und bleibt davon unberührt.</param>
        internal static Angabe Bilde(KostenVorlagenPosition p, int komponentenId,
                                     KostenProjektPositionenCtrl.Zeile pz, bool projektModus,
                                     bool betrieb = false)
        {
            var a = new Angabe();
            BemessungKatalog.Info info = p != null ? BemessungKatalog.Finde(p.Bemessung) : null;
            a.Absolut = info != null && info.Absolut;
            a.Kette = a.Absolut;
            a.Basis = pz != null ? pz.Basis : null;
            a.Runde = pz != null ? pz.Runde : 0;
            a.Herkunft = pz != null ? (pz.BasisHerkunft ?? "") : "";
            a.HerkunftText = HerkunftText(a.Herkunft, p, komponentenId);

            // ANWENDERBEFUND 14.09.2026: KEIN STILLES 0. Eine absolute Bemessung
            // braucht keine Bezugsgröße und trägt deshalb auch kein Zeichen.
            a.OhneBasis = pz != null && !pz.Basis.HasValue &&
                          GrundText(pz.BasisGrund).Length > 0;

            a.BasisText = BasisText(a.Basis, p, komponentenId);
            a.Kurztext = Kurztext(a, p, komponentenId, pz, projektModus, betrieb);
            a.Zeile = Zeilentext(a, projektModus, pz);

            // U31: Der Empfehlungsbereich stand bis hierher als deutscher Satzbaukasten
            // in der Windows-Hülle — Fachtext in einer Schale, einsprachig. Er entsteht
            // jetzt hier, in zwei Fassungen aus EINER Quelle: dem Werkzeugtipp am
            // Satzfeld (wortgleich zum Bestand) und der sichtbaren Zeile darunter.
            Empfehlung(a, p, komponentenId, betrieb);

            // ANWENDERENTSCHEID 19.09.2026: Der Vorlagenhinweis der Projektzeile.
            Vorlagenhinweis(a, p, komponentenId, pz);
            return a;
        }

        /// <summary>
        /// ANWENDERENTSCHEID 19.09.2026 — DER HINWEIS AUF DIE VORLAGE.
        ///
        /// <para>Ändert sich die Bemessung einer Position der Standardvorlage, werden
        /// die längst angelegten Projektzeilen NICHT nachgeführt: Ein gepflegtes
        /// Projekt ändert seine gerechnete Wirtschaftlichkeit nicht still. Damit die
        /// Abweichung trotzdem sichtbar ist, nennt die Zeile, was die Vorlage sagt —
        /// und der Dialog bietet die Übernahme an.</para>
        ///
        /// <para><b>Verglichen wird mit der Bemessung, die die Zeile GERADE trägt</b>
        /// (<paramref name="p"/>), nicht mit der zuletzt gespeicherten: Wer im Dialog
        /// auf den Vorlagenwert umstellt, sieht den Hinweis sofort verschwinden.
        /// Ohne Vorlagenposition und im Stammkontext gibt es keine Zeile.</para>
        /// </summary>
        private static void Vorlagenhinweis(Angabe a, KostenVorlagenPosition p,
                                            int komponentenId,
                                            KostenProjektPositionenCtrl.Zeile pz)
        {
            if (pz == null || p == null) return;
            string vorlage = pz.VorlagenBemessung ?? "";
            if (vorlage.Length == 0) return;
            if (string.Equals(vorlage, p.Bemessung ?? "", StringComparison.Ordinal)) return;

            a.VorlagenBemessung = vorlage;
            a.VorlagenZeile = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KDLG_VORLAGE_ZEILE,
                pz.VorlagenName ?? "",
                BemessungKatalog.Anzeige(vorlage, komponentenId));
        }

        // =====================================================================
        // Die Bezugsgröße
        // =====================================================================

        /// <summary>
        /// Die Bezugsgröße samt ihrer Einheit („300,00 kW", „196.080,00 €").
        /// <para>Die Einheit fällt aus der Satzeinheit: „%" bemisst sich an einem
        /// Geldbetrag, „€/kW" an kW. Einheitenzeichen werden nicht übersetzt
        /// (dokumentierte Ausnahme, <c>BetriebskostenCtrl.SatzEinheit</c>).</para>
        /// </summary>
        private static string BasisText(double? basis, KostenVorlagenPosition p, int komponentenId)
        {
            if (!basis.HasValue || p == null) return "";
            // U33: Hier gilt IMMER die Einheit der Investitionsseite — sie ist die
            // physikalische Einheit der Bezugsgröße. Ein Betriebssatz „€/kWp·a" bemisst
            // sich an kWp, nicht an „kWp·a"; das Jahr steckt im SATZ, nicht in der Größe.
            string einheit = BemessungKatalog.Einheit(p.Bemessung, komponentenId);
            bool prozent = string.Equals(einheit, "%", StringComparison.Ordinal);
            string basisEinheit = prozent
                ? DbWerte.KOSTEN_EINHEIT_EURO
                : (einheit.StartsWith("€/", StringComparison.Ordinal)
                    ? einheit.Substring(2) : "");
            return (basis.Value.ToString("#,##0.00", CultureInfo.CurrentCulture) +
                    " " + basisEinheit).Trim();
        }

        // =====================================================================
        // Der Empfehlungsbereich (U31)
        // =====================================================================

        /// <summary>
        /// U31 — der Empfehlungsbereich der Position in beiden Fassungen.
        ///
        /// <para>Ohne gepflegten Bereich bleiben beide leer: Es gibt dann weder einen
        /// Werkzeugtipp noch eine Zeile. Die Einheit ist die des SATZES und hängt am
        /// Gewerk (Anwenderentscheid 15.09.2026) — dieselbe Quelle wie am Satzfeld,
        /// damit Empfehlung und Eingabe nicht in verschiedenen Einheiten stehen.</para>
        /// </summary>
        private static void Empfehlung(Angabe a, KostenVorlagenPosition p, int komponentenId,
                                       bool betrieb)
        {
            if (p == null || (!p.EmpfehlungVon.HasValue && !p.EmpfehlungBis.HasValue)) return;

            string einheit = BemessungKatalog.Einheit(p.Bemessung, komponentenId, betrieb);
            string von = Zahl(p.EmpfehlungVon);
            string bis = Zahl(p.EmpfehlungBis);

            a.EmpfehlungKurztext = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KDLG_TT_EMPFEHLUNG, von, bis, einheit);
            a.EmpfehlungZeile = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KDLG_EMPF_ZEILE, von, bis, einheit);
        }

        /// <summary>Eine Satzzahl, wie sie im Satzfeld steht (bis zwei Nachkommastellen).</summary>
        private static string Zahl(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString("0.##", CultureInfo.CurrentCulture) : "";
        }

        /// <summary>Der Klartext zur Herkunft; leer, wo keine benannt ist.</summary>
        private static string HerkunftText(string herkunft, KostenVorlagenPosition p,
                                           int komponentenId)
        {
            switch (herkunft)
            {
                case HERKUNFT_ANLAGE:
                    string groesse = p == null
                        ? "" : TechnikPlanwertCtrl.BaugroessenName(komponentenId, p.Bemessung);
                    return groesse.Length == 0 ? ""
                        : string.Format(CultureInfo.CurrentCulture,
                                        MyResource.Resource.KDLG_HERK_ANLAGE, groesse);
                case HERKUNFT_HAUPT: return MyResource.Resource.KDLG_HERK_HAUPT;
                case HERKUNFT_STUFE_ANLAGE: return MyResource.Resource.KDLG_HERK_STUFE_ANLAGE;
                case HERKUNFT_STUFE_KOMPONENTE: return MyResource.Resource.KDLG_HERK_STUFE_KOMPONENTE;
                case HERKUNFT_STUFE_PROJEKT: return MyResource.Resource.KDLG_HERK_STUFE_PROJEKT;
                case HERKUNFT_LAUF: return MyResource.Resource.KDLG_HERK_LAUF;
                case HERKUNFT_INVEST: return MyResource.Resource.KDLG_HERK_INVEST;
                default: return "";
            }
        }

        /// <summary>
        /// H4c: Der Klartext zum Steuerwert <c>WirtschaftlichkeitCtrl.BASISGRUND_*</c>.
        /// Leerer Steuerwert = die Bemessungsart braucht überhaupt keine Bezugsgröße.
        /// </summary>
        /// <param name="grund">Einer der <c>BASISGRUND_*</c>-Steuerwerte.</param>
        internal static string GrundText(string grund)
        {
            switch (grund)
            {
                case WirtschaftlichkeitCtrl.BASISGRUND_GEWERK:
                    return MyResource.Resource.KDLG_BASIS_GRUND_GEWERK;
                case WirtschaftlichkeitCtrl.BASISGRUND_GERAET:
                    return MyResource.Resource.KDLG_BASIS_GRUND_GERAET;
                case WirtschaftlichkeitCtrl.BASISGRUND_LAUF:
                    return MyResource.Resource.KDLG_BASIS_GRUND_LAUF;
                // #363: die drei Lagen, die bis hierher ebenfalls „kein
                // Simulationslauf" hießen. Jeder Text nennt die ABHILFE mit, denn
                // genau daran fehlte es dem Anwender.
                case WirtschaftlichkeitCtrl.BASISGRUND_ANLAGE:
                    return MyResource.Resource.KDLG_BASIS_GRUND_ANLAGE;
                case WirtschaftlichkeitCtrl.BASISGRUND_MENGE:
                    return MyResource.Resource.KDLG_BASIS_GRUND_MENGE;
                case WirtschaftlichkeitCtrl.BASISGRUND_PREIS:
                    return MyResource.Resource.KDLG_BASIS_GRUND_PREIS;
                // ANWENDERENTSCHEID 19.09.2026: der fehlende Arbeitspreis des
                // PROJEKT-Stromträgers. Er trägt einen eigenen Steuerwert, weil beide
                // Lagen „kein Arbeitspreis" heißen, aber auf verschiedene Einträge der
                // Energieträgerverwaltung zeigen — den eigenen Träger der Anlage
                // (BASISGRUND_PREIS) und den Stromträger des Projekts.
                case WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS:
                    return MyResource.Resource.KDLG_BASIS_GRUND_STROMPREIS;
                case WirtschaftlichkeitCtrl.BASISGRUND_INVEST:
                    return MyResource.Resource.KDLG_BASIS_GRUND_INVEST;
                case WirtschaftlichkeitCtrl.BASISGRUND_KONSERVE:
                    return MyResource.Resource.KDLG_BASIS_GRUND_KONSERVE;
                default: return "";
            }
        }

        // =====================================================================
        // Werkzeugtipp und Herleitungszeile
        // =====================================================================

        /// <summary>
        /// W5‑B‑7: Der Werkzeugtipp des Betragsfelds — Wort für Wort der Bestand aus
        /// <c>KostenKomponenteHuelle.BasisKurztext</c> samt der drei Kontexte
        /// (absolut, Projekt, Katalog).
        /// </summary>
        private static string Kurztext(Angabe a, KostenVorlagenPosition p, int komponentenId,
                                       KostenProjektPositionenCtrl.Zeile pz, bool projektModus,
                                       bool betrieb)
        {
            if (a.Absolut) return MyResource.Resource.KDLG_TT_KETTE;
            if (!projektModus) return MyResource.Resource.KDLG_TT_BETRAG_ADMIN;

            // ANWENDERBEFUND 10.09.2026 (H4c): Ohne Bezugsgröße nennt der Werkzeugtipp
            // den GRUND — sonst steht dort derselbe Satz wie an einer gerechneten
            // Zeile, im Feld aber die 0 des Anwenderentscheids I-2.
            if (pz != null && !pz.Basis.HasValue)
            {
                string grund = GrundText(pz.BasisGrund);
                if (grund.Length > 0)
                    return string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.KDLG_TT_OHNE_BASIS, grund);
            }

            if (pz == null || !pz.Basis.HasValue || p == null || !p.Satz.HasValue ||
                BemessungKatalog.Finde(p.Bemessung) == null)
                return MyResource.Resource.KDLG_TT_BETRAG_PROJEKT;

            string einheit = BemessungKatalog.Einheit(p.Bemessung, komponentenId, betrieb);
            bool prozent = string.Equals(einheit, "%", StringComparison.Ordinal);
            string satz = p.Satz.Value.ToString("#,##0.00", CultureInfo.CurrentCulture) +
                          " " + einheit;
            return string.Format(CultureInfo.CurrentCulture,
                                 prozent ? MyResource.Resource.KDLG_TT_BETRAG_BASIS_PROZENT
                                         : MyResource.Resource.KDLG_TT_BETRAG_BASIS_MENGE,
                                 satz, a.BasisText);
        }

        /// <summary>
        /// U28: Die sichtbare Zeile unter dem Betrag. Nur im Projektmodus — im
        /// Stammkontext gibt es keine Bezugsgröße und deshalb nichts herzuleiten.
        /// Ohne Bezugsgröße bleibt es beim ⚠ der Zeile und beim Grund unter dem
        /// Raster; eine zweite Zeile sagte dasselbe noch einmal.
        /// </summary>
        private static string Zeilentext(Angabe a, bool projektModus,
                                         KostenProjektPositionenCtrl.Zeile pz)
        {
            if (!projektModus) return "";
            if (a.Absolut) return MyResource.Resource.KDLG_HERL_ABSOLUT;
            if (!a.Basis.HasValue || a.BasisText.Length == 0) return "";
            if (a.HerkunftText.Length == 0) return "";

            string zeile = a.Runde > 0
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KDLG_HERL_BASIS,
                                a.BasisText, a.HerkunftText,
                                a.Runde.ToString(CultureInfo.CurrentCulture))
                : string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.KDLG_HERL_BASIS_OHNE_RUNDE,
                                a.BasisText, a.HerkunftText);

            // U35: Eine GERECHNETE Bezugsgröße nennt auch ihre Faktoren — „750 Module ×
            // 400 Wp = 300,00 kWp". Ohne sie steht im Raster eine Zahl, die in keiner
            // Gerätemaske zu finden ist; im Werkzeugtipp stand sie schon
            // (TechnikPlanwertCtrl.BaugroesseHerleitung), sichtbar bisher nicht. Wo die
            // Größe unmittelbar am Gerät steht, ist der Text leer und die Zeile bleibt,
            // wie sie war.
            string rechnung = pz != null ? (pz.BasisHerleitung ?? "") : "";
            return rechnung.Length == 0 ? zeile : zeile + " · " + rechnung;
        }
    }
}
