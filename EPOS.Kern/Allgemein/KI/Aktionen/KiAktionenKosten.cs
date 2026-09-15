using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die zwei LESENDEN Aktionen auf den Anlagen und Kosten eines Projekts
    /// (Anwenderbefunde vom 14./15.09.2026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was gefehlt hat.</b> Das Register kannte Projekte, Varianten, Ergebnisse und
    /// die Kostenlage EINER Komponente - aber keine Aktion, die sagt, WAS im Projekt
    /// verbaut ist und WELCHE Kostenpositionen es fuehrt. Drei Fragen liefen deshalb
    /// ins Leere:
    /// </para>
    /// <list type="bullet">
    /// <item><description>„Liste alle Heizkessel im Projekt auf" - kein Werkzeug, also
    /// Freitext aus den Hilfeabschnitten.</description></item>
    /// <item><description>„wieviel kostet der Stromspeicher — Shenzhen Growatt" - das
    /// Modell hielt den GERAETENAMEN fuer einen Projektnamen und suchte ein Projekt
    /// dieses Namens (Protokoll vom 15.09.2026, 00:00:08).</description></item>
    /// <item><description>„Liste alle Kostenpositionen auf" - <c>kostenlage_pruefen</c>
    /// antwortet je Komponente und nennt keine einzelne Position; die Ids, die
    /// <c>kostenposition_setzen</c> braucht, standen nirgends.</description></item>
    /// </list>
    /// <para>
    /// <b>Beide Aktionen sprechen die KOMPONENTEN-Sprache des Hauses</b>
    /// (<see cref="DbWerte.ERZEUGER_HEIZKESSEL"/> &amp; Co.) und docken an derselben
    /// Landkarte an wie die Kostenverwaltung selbst (<c>TechnikPlanwertCtrl.Plaene</c>).
    /// Eine zweite Zuordnung „Komponente → Tabelle" gibt es damit nicht.
    /// </para>
    /// <para>
    /// <b>Die Positionsliste ist der Einstieg zum SETZEN.</b> Sie fuehrt je Zeile
    /// <c>id_position</c> - genau den Wert, den <c>kostenposition_setzen</c> erwartet.
    /// Ohne sie muesste das Modell eine Id erfinden, und genau das ist ihm verboten.
    /// </para>
    /// </remarks>
    internal static class KiAktionenKosten
    {
        /// <summary>
        /// Die sieben Komponenten in Anzeigereihenfolge - dieselbe Auswahl wie in
        /// <c>kostenlage_pruefen</c> und in <c>TechnikPlanwertCtrl.Plaene</c>.
        /// </summary>
        internal static readonly string[] Komponenten =
        {
            DbWerte.ERZEUGER_WAERMEPUMPE,
            DbWerte.ERZEUGER_HEIZKESSEL,
            DbWerte.ERZEUGER_PHOTOVOLTAIK,
            DbWerte.ERZEUGER_SOLARTHERMIE,
            DbWerte.ERZEUGER_STROMSPEICHER,
            DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER,
            DbWerte.ERZEUGER_BHKW
        };

        // =====================================================================
        // anlagen_auflisten
        // =====================================================================

        /// <summary>
        /// Was im Projekt verbaut ist: je Geraet eine Zeile mit Komponente,
        /// Geraetebezeichnung und Technik-Planwert. Andockpunkt
        /// <c>TechnikPlanwertCtrl.LiesAnlagen</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das ist die Antwort auf „Liste alle Heizkessel auf".</b> Ohne
        /// <c>komponente</c> laeuft sie ueber alle sieben Gewerke, mit ueber genau eines.
        /// Der <c>Bezeichner</c> stammt aus der GERAETETABELLE des Gewerks (Tab_Heizkessel,
        /// Tab_Stromspeicher, …) - er ist damit der Name, den der Anwender in der Maske
        /// liest („Vitocrossal 200 CM2 raumluftabhaengig").
        /// </para>
        /// <para>
        /// <b>Der Betrag ist ein PLANWERT, kein erfasster Kostenbetrag.</b> Er kommt aus
        /// der Technik (Geraetepreis mal Stueckzahl) und kann von der erfassten
        /// Kostenposition abweichen - genau diese Abweichung ist der Gegenstand von
        /// <c>kostenlage_pruefen</c>. Pflegt die Technik MEHRERE Basen, bleibt er 0 und
        /// <c>basis_mehrdeutig</c> sagt warum; eine der beiden Zahlen einfach zu waehlen
        /// waere geraten.
        /// </para>
        /// </remarks>
        internal static KiAktion AnlagenAuflisten()
        {
            return new KiAktion(
                name: "anlagen_auflisten",
                zweck: KiAktionsTexte.ZweckAnlagenAuflisten,
                titel: KiAktionsTexte.TitelAnlagenAuflisten,
                beispiel: KiAktionsTexte.BeispielAnlagenAuflisten,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "TechnikPlanwertCtrl.LiesAnlagen",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(pflicht: false),
                    KomponenteParameter(),
                    VariantenParameter()
                },
                vorbedingung: a => KiHilfe.ProjektMussAufloesbarSein(a),
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    string grund = KiHilfe.ProjektMussAufloesbarSein(a);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    IReadOnlyList<string> gewerke = Gewerke(a, out string ungueltig);
                    if (ungueltig != null) return KiErgebnis.Abgelehnt(ungueltig);

                    IReadOnlyList<KiHilfe.Kandidat> projekte = Projektgruppe(a, id);
                    var zeilen = KiHilfe.Liste();

                    foreach (KiHilfe.Kandidat p in projekte)
                        foreach (string komponente in gewerke)
                            foreach (TechnikPlanwertCtrl.Anlage anlage in
                                     TechnikPlanwertCtrl.LiesAnlagen(p.Id, komponente))
                                zeilen.Add(KiHilfe.Zeile(
                                    "id_projekt", p.Id,
                                    "projekt", KiHilfe.Text(p.Name),
                                    "komponente", komponente,
                                    "geraet", KiHilfe.Text(anlage.Bezeichner),
                                    "geraet_id", anlage.GeraetID,
                                    "stueckzahl", KiHilfe.Wert(anlage.Menge),
                                    "planwert_eur", KiHilfe.Wert(anlage.EindeutigerWert),
                                    "basis_mehrdeutig", anlage.Mehrdeutig,
                                    "nebenkosten_eur", KiHilfe.Wert(Nebensumme(anlage))));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.AnlagenGefunden,
                                      zeilen.Count),
                        zeilen);
                });
        }

        // =====================================================================
        // kostenpositionen_auflisten
        // =====================================================================

        /// <summary>
        /// Die erfassten Kostenpositionen eines Projekts - je Position eine Zeile mit
        /// ihrer ID. Andockpunkt <c>KostenProjektPositionenCtrl.Lies</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Sie ist der Einstieg zum Setzen.</b> <c>id_position</c> ist der Wert, den
        /// <c>kostenposition_setzen</c> im Parameter <c>kostenposition</c> erwartet.
        /// Damit steht der Weg „auflisten, aussuchen, setzen, bestaetigen" vollstaendig -
        /// bis hierher fehlte ihm das erste Glied.
        /// </para>
        /// <para>
        /// <b>Vorgabe ist die INVESTITION</b>, weil danach gefragt wird, wenn von
        /// „Kosten" die Rede ist; die Betriebskosten stehen ueber <c>kategorie</c>
        /// daneben. Gelesen wird je Komponente - die Landkarte kennt keine Abfrage
        /// „alle Positionen eines Projekts", und eine neue waere eine zweite Wahrheit
        /// neben der, aus der die Kostenverwaltung selbst liest.
        /// </para>
        /// </remarks>
        internal static KiAktion KostenpositionenAuflisten()
        {
            return new KiAktion(
                name: "kostenpositionen_auflisten",
                zweck: KiAktionsTexte.ZweckKostenpositionenAuflisten,
                titel: KiAktionsTexte.TitelKostenpositionenAuflisten,
                beispiel: KiAktionsTexte.BeispielKostenpositionenAuflisten,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KostenProjektPositionenCtrl.Lies",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(pflicht: false),
                    KomponenteParameter(),
                    VariantenParameter(),
                    new KiParameter("kategorie", KiParameterTyp.Aufzaehlung,
                                    KiAktionsTexte.ErlKostenkategorie,
                                    pflicht: false,
                                    anzeigename: KiAktionsTexte.KostenkategorieName,
                                    werte: new[] { KATEGORIE_INVESTITION, KATEGORIE_BETRIEB })
                },
                vorbedingung: a => KiHilfe.ProjektMussAufloesbarSein(a),
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    string grund = KiHilfe.ProjektMussAufloesbarSein(a);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    IReadOnlyList<string> gewerke = Gewerke(a, out string ungueltig);
                    if (ungueltig != null) return KiErgebnis.Abgelehnt(ungueltig);

                    int kategorie = Kategorie(a.Text("kategorie"));
                    IReadOnlyList<KiHilfe.Kandidat> projekte = Projektgruppe(a, id);
                    var zeilen = KiHilfe.Liste();
                    double summe = 0.0;

                    foreach (KiHilfe.Kandidat p in projekte)
                        foreach (string komponente in gewerke)
                        {
                            int komponentenId = KomponentenId(komponente);
                            if (komponentenId <= 0) continue;

                            foreach (KostenProjektPositionenCtrl.Zeile z in
                                     KostenProjektPositionenCtrl.Lies(p.Id, komponentenId, kategorie))
                            {
                                double betrag = z.Raster.BetragNetto ?? 0.0;
                                summe += betrag;

                                zeilen.Add(KiHilfe.Zeile(
                                    "id_position", z.Raster.Id,
                                    "id_projekt", p.Id,
                                    "projekt", KiHilfe.Text(p.Name),
                                    "komponente", komponente,
                                    "position", KiHilfe.Text(z.Raster.Bezeichnung),
                                    "bemessung", KiHilfe.Text(z.Raster.Bemessung),
                                    "satz", KiHilfe.Wert(z.Raster.Satz ?? 0.0),
                                    "betrag_netto_eur", KiHilfe.Wert(betrag),
                                    "nutzungsdauer_a", KiHilfe.Wert(z.Raster.Nutzungsdauer ?? 0.0)));
                            }
                        }

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture,
                                      KiAktionsTexte.KostenpositionenGefunden,
                                      zeilen.Count, summe),
                        zeilen);
                });
        }

        // ===================================================================== Hilfen

        /// <summary>Steuerwert der Kategorie „Investition" - Text fuer das Modell.</summary>
        private const string KATEGORIE_INVESTITION = "Investition";

        /// <summary>Steuerwert der Kategorie „Betrieb".</summary>
        private const string KATEGORIE_BETRIEB = "Betrieb";

        /// <summary>Der Komponentenparameter - in beiden Aktionen derselbe, und OPTIONAL.</summary>
        /// <remarks>
        /// Ohne Angabe laeuft die Aktion ueber ALLE Gewerke. Das ist die Frage, die
        /// gestellt wird („liste alle Kostenpositionen auf"); ein Pflichtfeld zwaenge das
        /// Modell, sieben Aufrufe zu machen - und der Rundendeckel liesse nur zwei davon zu.
        /// </remarks>
        private static KiParameter KomponenteParameter()
        {
            return new KiParameter("komponente", KiParameterTyp.Aufzaehlung,
                                   KiAktionsTexte.ErlKomponenteOptional,
                                   pflicht: false,
                                   anzeigename: KiAktionsTexte.KomponenteName,
                                   werte: Komponenten);
        }

        /// <summary>
        /// Der Schalter „samt Varianten" - in beiden Aktionen derselbe.
        /// </summary>
        /// <remarks>
        /// <b>Die Frage wird ueber die GRUPPE gestellt</b> (Anwenderbefund vom
        /// 15.09.2026: „Liste alle Komponenten der Stamm und Variante Projekte auf").
        /// Die Vergleichsseite zeigt Stammprojekt und Varianten nebeneinander, und danach
        /// wird gefragt. Ohne diesen Schalter muesste das Modell erst
        /// <c>varianten_auflisten</c> rufen und dann je Variante einen weiteren Aufruf -
        /// bei vier Varianten sind das fuenf Runden, und der Deckel liegt bei sechs.
        /// </remarks>
        private static KiParameter VariantenParameter()
        {
            return new KiParameter("mit_varianten", KiParameterTyp.Wahrheitswert,
                                   KiAktionsTexte.ErlMitVarianten,
                                   pflicht: false,
                                   anzeigename: KiAktionsTexte.MitVariantenName);
        }

        /// <summary>
        /// Die zu lesenden Projekte: nur das genannte, oder - mit <c>mit_varianten</c> -
        /// sein Stammprojekt samt allen Varianten.
        /// </summary>
        /// <remarks>
        /// <b>Aufgeloest wird ueber den STAMM.</b> Nennt der Anwender eine Variante, wird
        /// zuerst ihr Stammprojekt ermittelt (<c>StammRefDerVariante</c>) und dann die
        /// ganze Gruppe geladen - genau wie in <c>varianten_auflisten</c>. So bekommt er
        /// dieselbe Gruppe, gleich welches Glied er genannt hat.
        /// </remarks>
        private static IReadOnlyList<KiHilfe.Kandidat> Projektgruppe(KiAufruf a, int id)
        {
            var eines = new[] { new KiHilfe.Kandidat(id, KiHilfe.ProjektName(id), "") };
            if (!a.Wahrheit("mit_varianten")) return eines;

            try
            {
                var ctrl = new VariantenCtrl();
                int stammRef = ctrl.StammRefDerVariante(id);
                int idStamm = stammRef > 0 ? stammRef : id;

                List<VariantenCtrl.VarianteInfo> gruppe =
                    ctrl.LadeGruppe(idStamm, KiHilfe.ProjektName(idStamm));
                if (gruppe == null || gruppe.Count == 0) return eines;

                var liste = new List<KiHilfe.Kandidat>(gruppe.Count);
                foreach (VariantenCtrl.VarianteInfo v in gruppe)
                    liste.Add(new KiHilfe.Kandidat(v.IdProjekt, v.Projektname, ""));
                return liste;
            }
            catch (Exception)
            {
                // Eine Gruppe, die sich nicht lesen laesst, ist kein Grund, die Auskunft
                // zum genannten Projekt fallen zu lassen.
                return eines;
            }
        }

        /// <summary>
        /// Die zu lesenden Gewerke: das genannte, oder alle sieben. <paramref name="fehler"/>
        /// traegt den Klartextgrund, wenn ein unbekanntes genannt wurde.
        /// </summary>
        private static IReadOnlyList<string> Gewerke(KiAufruf a, out string fehler)
        {
            fehler = null;
            string genannt = (a.Text("komponente") ?? "").Trim();
            if (genannt.Length == 0) return Komponenten;

            foreach (string k in Komponenten)
                if (string.Equals(k, genannt, StringComparison.CurrentCultureIgnoreCase))
                    return new[] { k };

            fehler = string.Format(CultureInfo.CurrentCulture,
                                   KiAktionsTexte.KomponenteUnbekannt, genannt);
            return Array.Empty<string>();
        }

        /// <summary>Kategorie-Id zum Steuerwert; Vorgabe ist die Investition.</summary>
        private static int Kategorie(string text)
        {
            return string.Equals((text ?? "").Trim(), KATEGORIE_BETRIEB,
                                 StringComparison.CurrentCultureIgnoreCase)
                ? DbWerte.KOSTEN_KATEGORIE_BETRIEB
                : DbWerte.KOSTEN_KATEGORIE_INVESTITION;
        }

        /// <summary>Summe der Nebenkosten einer Anlage.</summary>
        private static double Nebensumme(TechnikPlanwertCtrl.Anlage anlage)
        {
            double summe = 0.0;
            foreach (TechnikPlanwertCtrl.Nebenposten n in anlage.Nebenkosten) summe += n.Betrag;
            return summe;
        }

        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> einer Komponente - dieselbe Abfrage wie in
        /// <c>kostenlage_pruefen</c>.
        /// </summary>
        internal static int KomponentenId(string komponente)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT MIN(ID) FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new DbParam("@k", komponente ?? ""));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }
    }
}
