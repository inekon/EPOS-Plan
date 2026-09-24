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
    /// <b>Die Sätze der Nutzungsdauertabelle in Vorbelegung und Anzeige</b> (Etappe E10,
    /// Stufe S3; Empfehlung E10‑Q1 (a) in der Fassung E10/9).
    ///
    /// <para><b>Die Tabelle rechnet nicht selbst.</b> Der Rechenweg liest den Satz der
    /// Position, wie er gepflegt ist — ohne Satz ihren Betrag (ein Betrag bleibt ein Betrag),
    /// sonst 0. Ein Satz der Tabelle wirkt allein, wenn er AUSDRÜCKLICH in die Position
    /// geschrieben wird: bei der Vorlagenübernahme für eine neue Position, deren Vorlage keinen
    /// Satz trägt, und über den Knopf „Sätze vorbelegen…" der Kostenverwaltung für bestehende —
    /// die bewusste Handlung des Anwenders. So ändert nichts eine gerechnete Wirtschaftlichkeit
    /// ohne sein Zutun (Anwenderentscheid ND‑Q4).</para>
    ///
    /// <para><b>Die Regel, EINMAL</b> (<see cref="WirksamerSatz"/>): Ein gepflegter Satz
    /// bleibt, was er ist. Eine Position „% der Investition" mit Zuordnung
    /// (<see cref="NutzungsdauerSaetze.Zuordnungen"/>) und OHNE Satz bekommt den Satz der
    /// Tabelle ihrer Technik — als Wert, den die Vorbelegung schreibt. Ist ein gepflegter
    /// Satz genau der der Tabelle, trägt er ihre HERKUNFT: Herkunftszeile im Dialog,
    /// Herleitung und Formelmappe nennen sie.</para>
    ///
    /// <para><b>Wer fragt:</b> die Vorlagenübernahme, der Knopf „Sätze vorbelegen…", die
    /// Herkunftszeile des Kostendialogs und — allein für die Herkunft
    /// (<see cref="AusTabelle"/>) — die Nachweisliste der Betriebskosten
    /// (<c>WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen</c>).</para>
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
        /// <b>Der Satz für Vorbelegung und Anzeige</b> (Regel im Klassenkopf): der gepflegte,
        /// sonst — bei einer satzfähigen Position — der der Tabelle, sonst <c>null</c>. Der
        /// Rechenweg ruft diese Funktion NICHT für seinen Betrag; er rechnet mit dem Satz, der in
        /// der Zeile steht.
        /// </summary>
        /// <param name="bemessung">Die Bemessung der Position.</param>
        /// <param name="satz">Der gepflegte Satz (<c>Einheitpreis</c>); <c>null</c> = keiner.</param>
        /// <param name="komponentenId">Die Komponente der Position.</param>
        /// <param name="bezeichnung">Der Positionsschlüssel.</param>
        /// <param name="tafel">Der Lesestand; wird beim ersten Bedarf gelesen.</param>
        /// <param name="herkunft">Die Vorgabe der Tabelle, wenn der Rückgabewert ihr Satz ist —
        /// für eine leere Position vorgeschlagen oder als gepflegter Satz gleich; sonst
        /// <c>null</c>.</param>
        public static double? WirksamerSatz(string bemessung, double? satz, int komponentenId,
                                            string bezeichnung, ref NutzungsdauerSatztafel tafel,
                                            out BetriebssatzVorgabe herkunft)
        {
            herkunft = null;
            if (!Satzfaehig(bemessung, bezeichnung)) return satz;

            if (tafel == null) tafel = Tafel();
            BetriebssatzVorgabe v = tafel.Vorgabe(komponentenId, bezeichnung);
            if (!v.Satz.HasValue) return satz;

            if (satz.HasValue)
            {
                if (Math.Abs(satz.Value - v.Satz.Value) < 1e-9) herkunft = v;
                return satz;
            }
            herkunft = v;
            return v.Satz;
        }

        /// <summary>
        /// Stammt ein GEPFLEGTER Satz aus der Tabelle — ist er genau ihr Satz für diese
        /// Position? Die Nachweisliste fragt so nach der Herkunft, ohne je einen Satz zu
        /// setzen. Ohne gepflegten Satz: nein.
        /// </summary>
        public static bool AusTabelle(string bemessung, double? satz, int komponentenId,
                                      string bezeichnung, ref NutzungsdauerSatztafel tafel)
        {
            if (!satz.HasValue) return false;
            WirksamerSatz(bemessung, satz, komponentenId, bezeichnung, ref tafel,
                          out BetriebssatzVorgabe herkunft);
            return herkunft != null;
        }

        /// <summary>
        /// Die Herkunftszeile unter dem Satzfeld des Kostendialogs: „2 % · Satz aus
        /// Nutzungsdauertabelle: Heizkessel · Wärmeerzeuger (Instandsetzung)" — wenn der
        /// gepflegte Satz dem der Tabelle ENTSPRICHT (gemessen am Wert, wie bei der
        /// Nutzungsdauer, <see cref="NutzungsdauerCtrl.Herkunft"/>). Leer bei einem leeren
        /// Feld — das rechnet mit nichts, bis „Sätze vorbelegen…" den Satz einträgt —, bei einem
        /// eigenen Satz und dort, wo die Tabelle nichts anbietet.
        /// </summary>
        public static string Herleitungszeile(BetriebssatzVorgabe v, double? gepflegterSatz)
        {
            if (v == null || !v.Satz.HasValue || !gepflegterSatz.HasValue) return "";
            return Math.Abs(gepflegterSatz.Value - v.Satz.Value) < 1e-9 ? v.Herleitung : "";
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
