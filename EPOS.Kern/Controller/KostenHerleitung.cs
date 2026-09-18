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
        internal static Angabe Bilde(KostenVorlagenPosition p, int komponentenId,
                                     KostenProjektPositionenCtrl.Zeile pz, bool projektModus)
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
            a.OhneBasis = pz != null && !pz.Basis.HasValue && GrundText(pz.BasisGrund).Length > 0;

            a.BasisText = BasisText(a.Basis, p, komponentenId);
            a.Kurztext = Kurztext(a, p, komponentenId, pz, projektModus);
            a.Zeile = Zeilentext(a, projektModus);
            return a;
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
            string einheit = BemessungKatalog.Einheit(p.Bemessung, komponentenId);
            bool prozent = string.Equals(einheit, "%", StringComparison.Ordinal);
            string basisEinheit = prozent
                ? DbWerte.KOSTEN_EINHEIT_EURO
                : (einheit.StartsWith("€/", StringComparison.Ordinal)
                    ? einheit.Substring(2) : "");
            return (basis.Value.ToString("#,##0.00", CultureInfo.CurrentCulture) +
                    " " + basisEinheit).Trim();
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
                                       KostenProjektPositionenCtrl.Zeile pz, bool projektModus)
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

            string einheit = BemessungKatalog.Einheit(p.Bemessung, komponentenId);
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
        private static string Zeilentext(Angabe a, bool projektModus)
        {
            if (!projektModus) return "";
            if (a.Absolut) return MyResource.Resource.KDLG_HERL_ABSOLUT;
            if (!a.Basis.HasValue || a.BasisText.Length == 0) return "";
            if (a.HerkunftText.Length == 0) return "";

            return a.Runde > 0
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KDLG_HERL_BASIS,
                                a.BasisText, a.HerkunftText,
                                a.Runde.ToString(CultureInfo.CurrentCulture))
                : string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.KDLG_HERL_BASIS_OHNE_RUNDE,
                                a.BasisText, a.HerkunftText);
        }
    }
}
