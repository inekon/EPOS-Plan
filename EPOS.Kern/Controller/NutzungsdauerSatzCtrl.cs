using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die aufgelöste Satzvorgabe einer Betriebskostenposition aus der Nutzungsdauertabelle
    /// (Etappe E10, Stufe S3). <c>Satz</c> ist <c>null</c>, wenn die Position keinen Satz der
    /// Tabelle nimmt oder die Zeile ihrer Technik keinen trägt — dann wird NICHTS vorbelegt
    /// und nichts erfunden.
    /// </summary>
    public sealed class BetriebssatzVorgabe
    {
        /// <summary>Der Satz [% der Investition je Jahr]; <c>null</c> = keiner.</summary>
        public double? Satz;

        /// <summary>Instandsetzung oder Wartung.</summary>
        public Satzart Art;

        /// <summary>Die Zeile, deren Satz gilt; <c>null</c> = keine gefunden.</summary>
        public NutzungsdauerZeile Quellzeile;

        /// <summary>Die Herkunft im Klartext — „Satz aus Nutzungsdauertabelle: Heizkessel ·
        /// Wärmeerzeuger (Instandsetzung) 2 %"; leer ohne Satz.</summary>
        public string Herleitung = "";
    }

    /// <summary>
    /// Ein LESESTAND der Nutzungsdauertabelle für Leseschleifen: Die Betriebskostenschleifen
    /// fragen je Position; gelesen wird die Tabelle trotzdem nur einmal je Schleife.
    /// </summary>
    public sealed class NutzungsdauerSatztafel
    {
        private readonly IList<NutzungsdauerZeile> _zeilen;

        internal NutzungsdauerSatztafel(IList<NutzungsdauerZeile> zeilen)
        {
            _zeilen = zeilen ?? new List<NutzungsdauerZeile>();
        }

        /// <summary>
        /// <b>Die Auflösung</b> einer Betriebsposition — über ihre Zuordnung
        /// (<see cref="NutzungsdauerSaetze.ZuordnungZu"/>) zur Zeile (Technik, Positionsart)
        /// und, trägt die keinen Satz, zur Standardzeile derselben Technik (dieselbe Staffel
        /// wie <see cref="NutzungsdauerCtrl.Vorgabe"/> für die Nutzungsdauer).
        /// </summary>
        /// <param name="komponentenIdDerPosition">Die Komponente, an der die Position steht —
        /// sie gilt, wo die Zuordnung keine eigene Technik nennt.</param>
        /// <param name="bezeichnung">Der Positionsschlüssel (<c>Tab_Kostenfaktor.Bezeichnung</c>).</param>
        public BetriebssatzVorgabe Vorgabe(int komponentenIdDerPosition, string bezeichnung)
        {
            var v = new BetriebssatzVorgabe();
            BetriebssatzZuordnung z = NutzungsdauerSaetze.ZuordnungZu(bezeichnung);
            if (z == null) return v;
            v.Art = z.Art;

            int technik = z.KomponentenId ?? komponentenIdDerPosition;
            if (technik <= 0) return v;

            NutzungsdauerZeile gewaehlt = null;
            if (!string.IsNullOrEmpty(z.Positionsart))
                foreach (NutzungsdauerZeile r in _zeilen)
                    if (r.KomponentenId == technik &&
                        string.Equals(r.Positionsart, z.Positionsart, StringComparison.Ordinal))
                    { gewaehlt = r; break; }

            NutzungsdauerZeile wirksam = gewaehlt != null && Satz(gewaehlt, z.Art).HasValue ? gewaehlt : null;
            if (wirksam == null)
                foreach (NutzungsdauerZeile r in _zeilen)
                    if (r.IstStandard && r.KomponentenId == technik) { wirksam = r; break; }

            if (wirksam == null) return v;
            double? satz = Satz(wirksam, z.Art);
            if (!satz.HasValue) return v;

            v.Satz = satz;
            v.Quellzeile = wirksam;
            v.Herleitung = NutzungsdauerSatzCtrl.HerleitungText(wirksam, z.Art, satz.Value);
            return v;
        }

        /// <summary>Der Satz einer Zeile nach seiner Art.</summary>
        internal static double? Satz(NutzungsdauerZeile r, Satzart art)
        {
            if (r == null) return null;
            return art == Satzart.Wartung ? r.WartungProzent : r.InstandsetzungProzent;
        }
    }

    /// <summary>
    /// <b>Die Sätze der Nutzungsdauertabelle im Rechenweg und in der Vorbelegung</b>
    /// (Etappe E10, Stufe S3; Empfehlung E10‑Q1 (a)).
    ///
    /// <para><b>Die Regel, EINMAL:</b> Ein gepflegter Satz der Position hat Vorrang. Trägt
    /// sie keinen (NULL) und auch keinen erfassten Betrag, und ist sie „% der Investition"
    /// und eine Instandsetzungs- oder Wartungsposition (<see cref="NutzungsdauerSaetze.Zuordnungen"/>),
    /// gilt der Satz der Tabelle für ihre Technik. Sonst bleibt es beim Bestandsweg — kein
    /// Satz, Betrag wie erfasst bzw. 0 (Anwenderentscheid I‑2: ein erfasster Betrag
    /// verschwindet nicht wortlos, deshalb schlägt auch er die Tabelle).</para>
    ///
    /// <para><b>Wer fragt:</b> die zwei Leseschleifen der Betriebskosten
    /// (<c>WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe</c>, <c>…Positionen</c>) — über
    /// <see cref="WirksamerSatz"/> —, die Vorlagenübernahme, der Knopf „Sätze vorbelegen…"
    /// und die Herkunftszeile des Kostendialogs. Eine zweite Formel gibt es nicht.</para>
    /// </summary>
    public static class NutzungsdauerSatzCtrl
    {
        /// <summary>
        /// Steuerwert der Herkunft eines Satzes, der aus der Tabelle kam
        /// (<c>KostenPositionNachweis.SatzHerkunft</c>). Sprachneutral, eingefroren — er steht
        /// im Nachweisumschlag gespeicherter Läufe.
        /// </summary>
        public const string HERKUNFT_TABELLE = "NUTZUNGSDAUERTABELLE";

        /// <summary>Ein Lesestand der ganzen Tabelle; leer, wo es sie nicht gibt.</summary>
        public static NutzungsdauerSatztafel Tafel()
        {
            return new NutzungsdauerSatztafel(NutzungsdauerCtrl.Alle());
        }

        /// <summary>Die Satzvorgabe EINER Position — liest die Tabelle einmal.</summary>
        public static BetriebssatzVorgabe Vorgabe(int komponentenId, string bezeichnung)
        {
            if (NutzungsdauerSaetze.ZuordnungZu(bezeichnung) == null) return new BetriebssatzVorgabe();
            return Tafel().Vorgabe(komponentenId, bezeichnung);
        }

        /// <summary>
        /// Nimmt eine Position dieser Bemessung und dieses Namens ÜBERHAUPT einen Satz der
        /// Tabelle? Die Bedingung ohne Datenbank: „% der Investition" und eine Zuordnung.
        /// </summary>
        public static bool Satzfaehig(string bemessung, string bezeichnung)
        {
            return string.Equals(bemessung, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) &&
                   NutzungsdauerSaetze.ZuordnungZu(bezeichnung) != null;
        }

        /// <summary>
        /// <b>Der Rechenweg:</b> der WIRKSAME Satz einer Betriebsposition — der gepflegte, sonst
        /// der der Tabelle, sonst <c>null</c> wie bisher (Regel im Klassenkopf).
        /// </summary>
        /// <param name="bemessung">Die Bemessung der Position.</param>
        /// <param name="satz">Der gepflegte Satz (<c>Tab_ProjektWerte.Einheitpreis</c>).</param>
        /// <param name="eingegeben">Der erfasste Betrag (<c>EingegebenerWert</c>).</param>
        /// <param name="komponentenId">Die Komponente der Position.</param>
        /// <param name="bezeichnung">Der Positionsschlüssel.</param>
        /// <param name="tafel">Der Lesestand; wird beim ersten Bedarf gelesen.</param>
        /// <param name="herkunft">Die Vorgabe, wenn der Satz aus der Tabelle kam; sonst <c>null</c>.</param>
        public static double? WirksamerSatz(string bemessung, double? satz, double eingegeben,
                                            int komponentenId, string bezeichnung,
                                            ref NutzungsdauerSatztafel tafel,
                                            out BetriebssatzVorgabe herkunft)
        {
            herkunft = null;
            if (satz.HasValue) return satz;
            if (Math.Abs(eingegeben) > 1e-9) return null;
            if (!Satzfaehig(bemessung, bezeichnung)) return null;

            if (tafel == null) tafel = Tafel();
            BetriebssatzVorgabe v = tafel.Vorgabe(komponentenId, bezeichnung);
            if (!v.Satz.HasValue) return null;

            herkunft = v;
            return v.Satz;
        }

        /// <summary>
        /// Die Herkunftszeile unter dem Satzfeld des Kostendialogs: „2 % · Satz aus
        /// Nutzungsdauertabelle: Heizkessel · Wärmeerzeuger" — wenn der Satz aus der Tabelle
        /// KOMMT (Feld leer) oder ihr ENTSPRICHT (gemessen am Wert, wie bei der
        /// Nutzungsdauer, <see cref="NutzungsdauerCtrl.Herkunft"/>). Leer bei einem eigenen
        /// Satz und dort, wo die Tabelle nichts anbietet.
        /// </summary>
        public static string Herleitungszeile(BetriebssatzVorgabe v, double? gepflegterSatz)
        {
            if (v == null || !v.Satz.HasValue) return "";
            if (gepflegterSatz.HasValue && Math.Abs(gepflegterSatz.Value - v.Satz.Value) > 1e-9) return "";
            return v.Herleitung;
        }

        /// <summary>Der Klartext der Herkunft — EINE Formulierung für Dialog und Bericht.</summary>
        internal static string HerleitungText(NutzungsdauerZeile zeile, Satzart art, double satz)
        {
            string technik = zeile == null || string.IsNullOrEmpty(zeile.Technik)
                ? Text("ND_TECHNIKUEBERGREIFEND", "technikübergreifend") : zeile.Technik;
            return string.Format(CultureInfo.CurrentCulture,
                Text("ND_SATZ_HERLEITUNG", "{0} % · Satz aus Nutzungsdauertabelle: {1} · {2} ({3})"),
                satz.ToString("0.###", CultureInfo.CurrentCulture), technik,
                zeile == null ? "" : zeile.Positionsart, ArtText(art));
        }

        /// <summary>Die Anzeige einer Satzart.</summary>
        public static string ArtText(Satzart art)
        {
            return art == Satzart.Wartung
                ? Text("ND_SATZART_WARTUNG", "Wartung")
                : Text("ND_SATZART_INSTANDSETZUNG", "Instandsetzung");
        }

        /// <summary>Die kurze Herkunft für den Bericht — „Satz aus Nutzungsdauertabelle".</summary>
        public static string HerkunftKurz()
        {
            return Text("ND_SATZ_HERKUNFT", "Satz aus Nutzungsdauertabelle");
        }

        /// <summary>Ressourcentext mit deutschem Rückfall (Hausmuster der Hüllen).</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
