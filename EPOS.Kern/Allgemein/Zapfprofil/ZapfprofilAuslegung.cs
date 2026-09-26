using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was die Auslegung neben dem Eingang der Bilanz braucht (4.5, 4.7): die DIN-4708-Werte des
    /// Katalogs, die Bedarfstage des Katalogs (A100-Referenz, DIN-4708-Profil, Konstruktor,
    /// Ecodesign) und die Nenninhalte (Einstellung <c>Zapfprofil.Nenninhalte</c>).
    /// </summary>
    internal sealed record Auslegungseingang
    {
        public Din4708Katalog Din4708 { get; init; } = Din4708Katalog.Leer;
        public IReadOnlyList<BedarfstagKatalogzeile> Bedarfstage { get; init; } = new BedarfstagKatalogzeile[0];

        /// <summary>Die Nenninhalte; <c>null</c> = keine Rundung.</summary>
        public Nenninhaltsliste Nenninhalte { get; init; }

        /// <summary>
        /// Die Erzeugerart am Speicher — wählt die Schätzformel der Übertragerfläche (Kessel NA.1,
        /// Wärmepumpe NA.2); <c>null</c> = unbekannt, dann ohne Übertrager im Projekt nicht
        /// rechenbar. Eine Laufangabe (N10), gespeichert wird sie erst mit der Oberfläche (Z4).
        /// </summary>
        public ZapfErzeugerart? Erzeugerart { get; init; }

        /// <summary>Der Werkstoff des Übertragers — wählt den U-Wert; <c>null</c> = unbekannt (N10, Laufangabe).</summary>
        public ZapfUebertragerwerkstoff? Uebertragerwerkstoff { get; init; }

        /// <summary>
        /// „Stochastisch rechnen" (4.5 b): zieht je Topologiegruppe das Auslegungsensemble des
        /// Bedarfstags und füllt das Perzentil. Ohne diese Angabe bleibt das Perzentil „noch nicht
        /// gerechnet" — das Ensemble läuft nur auf Zuruf. Seed, Realisierungen und Perzentil kommen aus
        /// den Projektgrößen, die Kategorien aus dem Eingang (<see cref="Zapfprofileingang.Zapfkategorien"/>).
        /// </summary>
        public bool Stochastisch { get; init; }

        /// <summary>
        /// Die Abbruchmarke des nebenläufigen Laufs „Stochastisch rechnen" (5.1): Sie beendet die
        /// Ziehung des Auslegungsensembles mit <see cref="OperationCanceledException"/> — kein halbes
        /// Perzentil. Ohne Marke (Vorgabe) läuft die Rechnung durch.
        /// </summary>
        public CancellationToken Abbruch { get; init; }

        /// <summary>
        /// Die Stufe des Dialogs, aus dem die Auslegung rechnet (N11 (c)): In der Stufe
        /// <see cref="ZapfStufe.Einfach"/> trägt jeder Punkt die Marke „Schnellauslegung"
        /// (<see cref="Auslegungsempfehlung.Schnellauslegung"/>, 4.5) — ebenso jeder Punkt aus dem
        /// Schnellpfad des Vereinfachungsverfahrens. <c>null</c> = ein Lauf ohne Dialog (Test,
        /// Referenzlauf): Dann setzt allein der Schnellpfad die Marke.
        /// </summary>
        public ZapfStufe? Stufe { get; init; }
    }

    /// <summary>
    /// <b>Die Fassade der Auslegung</b> (Umsetzungskonzept Zapfprofilgenerator 2.1, 2.3 Satz 5,
    /// 4.5, 4.7). Sie liest Mengengerüst, Tagesgang, Wochenfaktoren, einen Bedarfstag und eine
    /// eigens gebildete Wochenreihe — <b>nie</b> die Jahresreihe der Bilanz; sie kennt weder
    /// Bilanzreihe noch Bilanzfassade (Wache <c>ZapfprofilTrennungWacheTests</c>).
    ///
    /// <para><b>Ablauf:</b> je Zone Temperaturen, Mengengerüst, Zeitstruktur und Kalender wie die
    /// Bilanz; Zirkulationsansatz und Kalibrierung; Laufzeitfenster aus dem Schwerpunkt der
    /// Tagesmengen mal Tagesgang (dieselben Stunden wie die Bilanz); Tagesmengen der Auslegung bei
    /// θ_KW,Auslegung; dann je <b>Topologiegruppe</b> (ZU13) Wochenreihe, Bedarfstag nach der
    /// Vorgaberegel, Normvergleich, bei Speicher Summenlinie, Speicherauslegung nach V4 und
    /// Großanlage, sonst die Minutenspitze. Je Gruppe genau eine Empfehlung. Mit „Stochastisch
    /// rechnen" (<see cref="Auslegungseingang.Stochastisch"/>) zieht jede Gruppe ihr
    /// Auslegungsensemble (<see cref="Zapfensemble"/>) und füllt das Perzentil (4.5 b) — bei
    /// Speicher das Volumen beim Φ_N des Summenlinienpunkts, sonst die Minutenspitze —, dazu
    /// Gleichzeitigkeit, Konsistenzhinweis und den Vergleich μ + z·σ/√N; sonst bleibt es „noch nicht
    /// gerechnet".</para>
    ///
    /// <para><b>Kein stiller Rückfall.</b> Eine Zone, die nicht rechenbar ist, steht in
    /// <see cref="Auslegungsergebnis.Ablehnungen"/>; eine Gruppe ohne Bedarfstag trägt eine nicht
    /// rechenbare Empfehlung mit Grund („Konstruktor öffnen"), nie das Stundenprofil.</para>
    /// </summary>
    internal static class ZapfprofilAuslegung
    {
        private sealed class Zonenarbeit
        {
            internal ZonenStand Stand;
            internal string Name;
            internal Nutzungsart Art;
            internal Zonentemperaturen Temperaturen;
            internal Mengenergebnis Menge;
            internal Zeitstruktur Struktur;
            internal double[] Kaltwasserfaktor;
            internal ZapfTagtyp[] Kalender;
            internal Messwert Messwert;
            internal bool InZ1;
            internal bool Abgelehnt;
            internal double ZapfungKwh;
            internal double ZirkulationKwh;
            internal Wochenbaustein Baustein;
            internal int Index;
        }

        /// <summary>Rechnet die Auslegung. Kalender, Projektgrößen und Parametersatz sind Pflicht.</summary>
        internal static Auslegungsergebnis Rechnen(Zapfprofileingang e, IReadOnlyList<Nutzungsart> katalog,
                                                   Auslegungseingang a)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            a ??= new Auslegungseingang();
            Zapfkalender.Pruefen(e.WochentagJan1, e.We);
            if (e.Projekt == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_PROJEKTGROESSEN_FEHLEN"));
            if (e.Parameter == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.ParameterFehlt, ZapfSatz.Neu("AUSLEGUNG_PARAMETERSATZ_FEHLT"));
            ProjektStand p = e.Projekt;
            Parametersatz ps = e.Parameter;
            var prot = new Herkunftsprotokoll();
            var hinweise = new List<Auslegungshinweis>();
            var zapfHinweise = new List<ZapfHinweis>();
            var ablehnungen = new List<Auslegungsablehnung>();
            IReadOnlyDictionary<string, double> belegung = e.BelegungJeRaumzahl ?? a.Din4708?.BelegungJeRaumzahl;

            // --- 1. je Zone: Temperaturen, Mengengerüst, Zeitstruktur, Kalender (wie die Bilanz) ---
            var arbeit = new List<Zonenarbeit>();
            foreach (ZonenStand z in e.Zonen ?? new ZonenStand[0])
            {
                var w = new Zonenarbeit { Stand = z, Name = z?.Name ?? "", Index = arbeit.Count };
                arbeit.Add(w);
                if (z == null)
                {
                    Ablehnen(w, ZapfSatz.Neu("EINGABE_ZONE_OHNE_ANGABEN"), ablehnungen);
                    continue;
                }
                try
                {
                    w.Art = Suchen(katalog, z.IdNutzungsart)
                            ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.NutzungsartFehlt, w.Name,
                                   ZapfSatz.Neu("EINGABE_NUTZUNGSART_FEHLT", w.Name));
                    Tagesgangsatz satz = w.Art.Tagesgaenge;
                    if (z.IdTagesgangsatz.HasValue)
                        satz = SuchenSatz(e.Tagesgangsaetze, z.IdTagesgangsatz.Value)
                               ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.TagesgangsatzFehlt, w.Name,
                                      ZapfSatz.Neu("EINGABE_TAGESGANGSATZ_FEHLT", z.IdTagesgangsatz.Value, w.Name));
                    w.Temperaturen = Mengengeruest.Temperaturen(z, w.Art, ps, prot);
                    w.Menge = Mengengeruest.JahresenergieKwh(z, w.Art, w.Temperaturen, ps, belegung, prot, zapfHinweise);
                    w.Struktur = Formvektor.Bilden(z, w.Art, satz, ps, prot, zapfHinweise);
                    w.Kaltwasserfaktor = Kaltwassergang.Monatsfaktoren(w.Temperaturen, w.Name);
                    w.Kalender = Zapfkalender.Bilden(e.WochentagJan1, e.We, Zapfkalender.FensterDerZone(z));
                    w.Messwert = Mengengeruest.MesswertAus(z, w.Temperaturen, prot);
                    w.InZ1 = z.Zirkulation && w.Art.Grenze == ZapfBilanzgrenze.Zapfstelle;
                    w.ZapfungKwh = w.Menge.JahresenergieKwh;
                }
                catch (ZapfprofilEingabeException ex) { Ablehnen(w, ex.Satz, ablehnungen); }
                catch (ParametersatzException ex) { Ablehnen(w, ex.Satz, ablehnungen); }
            }

            // --- 2. Zirkulation aus den Mengen vor der Kalibrierung (4.3) ----------------------
            var anteile = new List<Zonenanteil>();
            var anteilIndex = new List<Zonenarbeit>();
            foreach (Zonenarbeit w in arbeit)
            {
                if (w.Abgelehnt) continue;
                anteile.Add(new Zonenanteil(w.Name, w.ZapfungKwh, w.InZ1, w.Menge.FlaecheM2));
                anteilIndex.Add(w);
            }
            Zirkulationsansatz ansatz = null;
            double zirkVorschlagKw = 0.0;
            try
            {
                ansatz = Zirkulationskanal.Ansetzen(p, anteile, ps, prot, zapfHinweise);
                for (int i = 0; i < anteilIndex.Count; i++) anteilIndex[i].ZirkulationKwh = ansatz.AnteilJeZoneKwh[i];
                zirkVorschlagKw = ansatz.LeistungKw;
                if (!p.ZirkAuto)
                {
                    try
                    {
                        zirkVorschlagKw = Zirkulationskanal.Ansetzen(p with { ZirkAuto = true }, anteile, ps, null, null).LeistungKw;
                    }
                    catch (ZapfprofilEingabeException) { }
                    catch (ParametersatzException) { }
                }
            }
            catch (ZapfprofilEingabeException ex) { hinweise.Add(new Auslegungshinweis("ZIRKULATION_NICHT_RECHENBAR", ex.Satz, true)); }
            catch (ParametersatzException ex) { hinweise.Add(new Auslegungshinweis("ZIRKULATION_NICHT_RECHENBAR", ex.Satz, true)); }

            // --- 3. Kalibrierung (4.1) ---------------------------------------------------------
            foreach (Zonenarbeit w in arbeit)
            {
                if (w.Abgelehnt || w.Messwert == null) continue;
                try
                {
                    Kalibrierergebnis k = Mengengeruest.Kalibrieren(w.Messwert, w.ZapfungKwh, w.ZirkulationKwh, w.Name);
                    w.ZapfungKwh = k.ZapfungKwh;
                    w.ZirkulationKwh = k.ZirkulationKwh;
                    prot.Vermerken(w.Name, ZapfFeld.KALIBRIERFAKTOR, k.Faktor, "-", Wertstatus.Kalibriert, null);
                }
                catch (ZapfprofilEingabeException ex) { Ablehnen(w, ex.Satz, ablehnungen); }
            }

            // --- 4. Laufzeitfenster: dieselben Stunden wie die Bilanz (4.3, N7 (g)) -------------
            Tagesfenster laufzeit = Tagesfenster.Leer;
            if (ansatz != null)
            {
                double mitte = Tagesmitte(arbeit, e.WochentagJan1);
                laufzeit = new Tagesfenster(Zirkulationskanal.Laufzeitbeginn(ansatz.LaufzeitH, mitte), ansatz.LaufzeitH);
            }

            // --- 5. Tagesmengen der Auslegung bei θ_KW,Auslegung (4.2, 4.5) --------------------
            double kwAuslegung = ZapfAuslegungParameter.ProjektOderParameter(p.KaltwasserAuslegungC,
                ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG, ps, prot, ZapfFeld.AUSLEGUNG_KALTWASSER_C, "°C",
                ZapfSatz.Neu("BEGRIFF_KALTWASSER_AUSLEGUNG"));
            foreach (Zonenarbeit w in arbeit)
            {
                if (w.Abgelehnt) continue;
                try
                {
                    double f = Wochenreihe.KaltwasserfaktorAuslegung(w.Temperaturen.ZapfC, kwAuslegung, w.Temperaturen.KaltwasserMittelC);
                    prot.Vermerken(w.Name, ZapfFeld.AUSLEGUNG_KALTWASSERFAKTOR, f, "-", Wertstatus.Umgerechnet, null,
                                   ZapfSatz.Neu("HERKUNFT_KALTWASSERFAKTOR_FORMEL"));
                    w.Baustein = new Wochenbaustein(w.Name,
                        Wochenreihe.TagesmengenAuslegung(w.ZapfungKwh, f, w.Struktur, w.Kalender, e.WochentagJan1, w.Name),
                        w.Struktur, w.Kalender);
                }
                catch (ZapfAuslegungException ex) { Ablehnen(w, ex.Satz, ablehnungen); }
                catch (ZapfprofilEingabeException ex) { Ablehnen(w, ex.Satz, ablehnungen); }
            }

            // --- 6. je Topologiegruppe ---------------------------------------------------------
            ZapfTagtyp[] region = Zapfkalender.Bilden(e.WochentagJan1, e.We, null);
            double zirkSumme = ansatz?.RestKwh ?? 0.0;
            foreach (Zonenarbeit w in arbeit) if (!w.Abgelehnt) zirkSumme += w.ZirkulationKwh;
            var gruppen = new List<Auslegungsgruppe>();
            bool ersteSpeichergruppe = true;
            foreach (ZapfTopologie topo in new[] { ZapfTopologie.Speicher, ZapfTopologie.Frischwasserstation,
                                                   ZapfTopologie.Durchfluss, ZapfTopologie.Wohnungsstation })
            {
                var zonen = arbeit.FindAll(w => !w.Abgelehnt && w.Stand.Topologie == topo);
                if (zonen.Count == 0) continue;
                double zirkGruppe = 0.0;
                foreach (Zonenarbeit w in zonen) zirkGruppe += w.ZirkulationKwh;
                if (topo == ZapfTopologie.Speicher && ersteSpeichergruppe) zirkGruppe += ansatz?.RestKwh ?? 0.0;
                double anteil = zirkSumme > 0 ? zirkGruppe / zirkSumme : 0.0;
                if (topo == ZapfTopologie.Speicher) ersteSpeichergruppe = false;
                var zirk = new Schaetzwert(p.ZirkAuto, zirkVorschlagKw * anteil,
                                           p.ZirkManuellKw.HasValue ? p.ZirkManuellKw.Value * anteil : (double?)null);
                gruppen.Add(Gruppe(topo, zonen, p, ps, a, e.WochentagJan1, region, zirk, laufzeit, kwAuslegung,
                                   e.Zapfkategorien, prot));
            }

            // Die Befunde der Bilanz gehen mit ihrer Stufe in die Warnliste (Warnlogik Z4): Eine Warnung
            // der Bilanz — etwa ein Bedarf außerhalb der Bandbreite — bleibt in der Auslegung eine Warnung.
            foreach (ZapfHinweis h in zapfHinweise) hinweise.Add(new Auslegungshinweis(h.Code, h.Satz, h.Warnung));
            return new Auslegungsergebnis
            {
                Gruppen = gruppen.AsReadOnly(),
                Ablehnungen = ablehnungen.AsReadOnly(),
                Hinweise = hinweise.AsReadOnly(),
                Herkunft = prot.Abschrift()
            };
        }

        // =================================================================================
        // Eine Topologiegruppe
        // =================================================================================

        /// <summary>Kennung: Erst das empfohlene Volumen erkennt die Großanlage — die Gruppe rechnet neu mit W 551.</summary>
        internal const string HINWEIS_TEMPERATUR_GROSSANLAGE = "SPEICHERTEMPERATUR_GROSSANLAGE";

        /// <summary>
        /// Kennung: Ein Ecodesign-Zapfprofil ist über mehr als <see cref="ECODESIGN_HINWEIS_WOHNEINHEITEN"/>
        /// Wohneinheiten linear skaliert (Folgeposten #546) — ein Hinweis, keine Sperre.
        /// </summary>
        internal const string HINWEIS_ECODESIGN_SKALIERT = "ECODESIGN_SKALIERT";

        /// <summary>
        /// <b>Die Grenze des Skalierungshinweises</b> [Wohneinheiten] (numerische Setzung, Folgeposten
        /// #546): Ein Ecodesign-Zapfprofil beschreibt einen Haushalt bzw. den Bruchteil
        /// <c>Q_ref / Q_ref(L)</c> eines Haushalts; die Auslegung skaliert es linear auf die
        /// Wohneinheiten der Gruppe — ohne Gleichzeitigkeit. Über dieser Zahl nennt die Auslegung
        /// das und verweist auf den stochastischen Bedarfstag, der die Gleichzeitigkeit zieht.
        /// </summary>
        internal const double ECODESIGN_HINWEIS_WOHNEINHEITEN = 10.0;

        /// <summary>Ein Durchgang der Speichergruppe bei einer Speichertemperatur.</summary>
        private sealed class Speicherlauf
        {
            internal Speichertemperaturwahl Temperatur;
            internal Din4708Ergebnis Din;
            internal Speicherauslegungseingang Eingang;
            internal Summenlinienergebnis Summenlinie;
            internal ZapfSatz Grund;
            internal double? NenninhaltL;
            internal Summenlinienparameter Parameter;
            internal readonly List<Auslegungshinweis> Hinweise = new List<Auslegungshinweis>();

            /// <summary>Das empfohlene Volumen der Großanlagenerkennung: Nenninhalt, sonst Punkt der Summenlinie (N10).</summary>
            internal double? EmpfohlenL => NenninhaltL ?? Summenlinie?.Punkt.VolumenL;
        }

        private static Auslegungsgruppe Gruppe(ZapfTopologie topo, List<Zonenarbeit> zonen, ProjektStand p, Parametersatz ps,
                                               Auslegungseingang a, int wochentagJan1, ZapfTagtyp[] region, Schaetzwert zirk,
                                               Tagesfenster laufzeit, double kwAuslegung, IReadOnlyList<Zapfkategorie> kategorien,
                                               Herkunftsprotokoll prot)
        {
            var h = new List<Auslegungshinweis>();
            var bausteine = new List<Wochenbaustein>();
            var namen = new List<string>();
            var paare = new List<(ZonenStand, Nutzungsart)>();
            bool wohnen = true, allePersonen = true;
            double personenBezug = 0.0;
            var bezugsmengen = new SortedDictionary<ZapfBezugsart, double>();
            foreach (Zonenarbeit w in zonen)
            {
                bausteine.Add(w.Baustein);
                namen.Add(w.Name);
                paare.Add((w.Stand, w.Art));
                wohnen &= w.Art.Kalender == ZapfKalenderart.Wohnen;
                allePersonen &= w.Art.Bezug == ZapfBezugsart.Personen;
                bezugsmengen.TryGetValue(w.Art.Bezug, out double m);
                bezugsmengen[w.Art.Bezug] = m + w.Menge.Bezugsmenge;
                if (w.Art.Bezug == ZapfBezugsart.Personen) personenBezug += w.Menge.Bezugsmenge;
            }
            Wochenreihe woche = Wochenreihe.Bilden(bausteine, wochentagJan1, region);
            bool speicher = topo == ZapfTopologie.Speicher;

            // --- Speichergrößen, Großanlage vorab, die EINE Speichertemperatur (nur Speicher) -----
            double nutzanteil = 0.0, zuschlag = 0.0;
            ZapfSatz speicherGrund = null;
            double? leitung = null;
            Grossanlagenbefund vorab = null;
            bool grossPruefbar = false;
            Speichertemperaturwahl temperatur = null;
            if (speicher)
            {
                try
                {
                    nutzanteil = ZapfAuslegungParameter.ProjektOderParameter(p.Nutzanteil, ZapfAuslegungParameter.NUTZANTEIL,
                        ps, prot, ZapfFeld.AUSLEGUNG_NUTZANTEIL, "-", ZapfSatz.Neu("BEGRIFF_NUTZANTEIL"));
                    zuschlag = ZapfAuslegungParameter.ProjektOderParameter(p.Zuschlag, ZapfAuslegungParameter.ZUSCHLAG,
                        ps, prot, ZapfFeld.AUSLEGUNG_ZUSCHLAG, "-", ZapfSatz.Neu("BEGRIFF_ZUSCHLAG"));
                }
                catch (ParametersatzException ex) { speicherGrund = ex.Satz; }
                catch (ZapfAuslegungException ex) { speicherGrund = ex.Satz; }

                // Großanlage vorab: Projektvolumen und Leitungsinhalt, noch ohne Summenlinie.
                try
                {
                    leitung = Grossanlage.Leitungsinhalt(p, ps);
                    vorab = Grossanlage.Erkennen(Grossanlage.Speichervolumen(p, null), leitung, ps);
                    grossPruefbar = true;
                }
                catch (ParametersatzException ex)
                {
                    Auslegungshinweis.Einmal(h, Auslegungshinweis.ParameterFehlt(ex, ZapfSatz.Neu("FOLGE_GROSSANLAGE_NICHT_ERKANNT")));
                }
                catch (ZapfAuslegungException ex) { h.Add(new Auslegungshinweis("GROSSANLAGE_NICHT_RECHENBAR", ex.Satz, true)); }

                // Schnellpfad: Wohnen bis zur Anwendungsgrenze, das Projekt nennt weder Sensorhöhe noch Temperatur.
                bool schnellpfad = false;
                if (wohnen && !p.SensorhoeheAnteil.HasValue && !p.SpeicherC.HasValue)
                {
                    try { schnellpfad = Summenlinie.SchnellpfadGilt(true, Wohneinheiten(zonen), ps); }
                    catch (ParametersatzException ex)
                    {
                        Auslegungshinweis.Einmal(h, Auslegungshinweis.ParameterFehlt(ex, ZapfSatz.Neu("FOLGE_SCHNELLPFAD_ENTFAELLT")));
                    }
                }
                if (speicherGrund == null)
                {
                    try
                    {
                        temperatur = Speichertemperaturwahl.Waehlen(p, ps, vorab?.Gross == true, schnellpfad, prot);
                        Auslegungspruefung.Spreizung(temperatur.SpeicherC, kwAuslegung, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_SPEICHER"));
                    }
                    catch (ParametersatzException ex) { speicherGrund = ex.Satz; temperatur = null; }
                    catch (ZapfAuslegungException ex) { speicherGrund = ex.Satz; temperatur = null; }
                }
            }

            // --- Normvergleich: Gültigkeit, N und W_z hängen nicht an der Temperatur ----------------
            Din4708Ergebnis din = Normvergleich(topo, paare, a, ps, temperatur, kwAuslegung, nutzanteil, speicherGrund);

            // --- Bedarfstag nach der Vorgaberegel (4.5) ------------------------------------------
            bool bloecke = false;
            try { bloecke = Zapfblock.AusParametern(ps).Count > 0; }
            catch (ParametersatzException) { }
            catch (ZapfAuslegungException) { }
            BedarfstagKatalogzeile gewaehlt = null;
            if (p.IdBedarfstag.HasValue)
                foreach (BedarfstagKatalogzeile t in a.Bedarfstage ?? new BedarfstagKatalogzeile[0])
                    if (t != null && t.Id == p.IdBedarfstag.Value) gewaehlt = t;
            Bedarfstagwahl wahl = Bedarfstagregel.Waehlen(p.BedarfstagQuelle, wohnen, din.Gueltig && bloecke, gewaehlt);
            Bedarfstag tag = null;
            ZapfSatz tagGrund = wahl.Grund;
            try
            {
                switch (wahl.Quelle)
                {
                    case null:
                        break;
                    case ZapfBedarfstagquelle.Stundenprofil:
                        int d = Wochenreihe.GroessterTag(bausteine);
                        tag = Bedarfstag.AusStunden(Wochenreihe.Tagesstunden(bausteine, d), null,
                            ZapfSatz.Neu("AUSTEXT_TAG_STUNDENPROFIL", d));
                        break;
                    case ZapfBedarfstagquelle.Din4708Profil:
                        tag = Bedarfstag.Din4708(din.WzKwh.Value, Zapfblock.AusParametern(ps), din.KennzahlN.Value);
                        break;
                    default:
                        tag = Katalogtag(gewaehlt, bezugsmengen, zonen, ps, kwAuslegung, prot, h);
                        break;
                }
            }
            catch (ZapfAuslegungException ex) { tagGrund = ex.Satz; }
            catch (ParametersatzException ex) { tagGrund = ex.Satz; }
            if (tag == null)
                h.Add(new Auslegungshinweis(wahl.KonstruktorOeffnen ? "KONSTRUKTOR_OEFFNEN" : "BEDARFSTAG_NICHT_RECHENBAR", tagGrund, true));
            else if (tag.SpitzenUnterschaetzt && !speicher)
                h.Add(new Auslegungshinweis(Bedarfstag.VERMERK_SPITZEN_UNTERSCHAETZT, ZapfSatz.Neu("AUSHINWEIS_SPITZEN_UNTERSCHAETZT"), true));

            Auslegungswert haupt;
            Auslegungsempfehlung empfehlung;
            Summenlinienergebnis sl = null;
            Summenlinienparameter slp = null;
            Speicherauslegungsergebnis sa = null;
            Grossanlagenbefund gross = null;
            if (speicher)
            {
                Speicherlauf lauf = null;
                if (temperatur != null)
                {
                    lauf = SpeicherRechnen(p, ps, a, temperatur, din, vorab?.Gross, tag, tagGrund, woche, kwAuslegung, nutzanteil,
                                           zuschlag, zirk, laufzeit, wohnen, allePersonen, personenBezug, prot);
                    // Großanlage am empfohlenen Volumen: Nenninhalt, sonst Punkt (N10).
                    if (grossPruefbar) gross = Grossanlage.Erkennen(Grossanlage.Speichervolumen(p, lauf.EmpfohlenL), leitung, ps);

                    // Die Mindesttemperatur nach DVGW W 551 ist die Vorgabe nur bei Großanlage (4.0, N10).
                    // Erkennt erst das empfohlene Volumen die Großanlage, rechnet die Gruppe einmal neu mit ihr.
                    if (gross != null && gross.Gross && (temperatur.Quelle == Speichertemperaturquelle.Vorgabe
                                                         || temperatur.Quelle == Speichertemperaturquelle.Schnellpfad))
                    {
                        try
                        {
                            Speichertemperaturwahl mindest = Speichertemperaturwahl.Waehlen(p, ps, true, false, prot);
                            Auslegungspruefung.Spreizung(mindest.SpeicherC, kwAuslegung, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_SPEICHER"));
                            h.Add(new Auslegungshinweis(HINWEIS_TEMPERATUR_GROSSANLAGE,
                                ZapfSatz.Neu("AUSHINWEIS_TEMPERATUR_GROSSANLAGE", temperatur.SpeicherC, lauf.EmpfohlenL.Value, mindest.SpeicherC)));
                            temperatur = mindest;
                            din = Normvergleich(topo, paare, a, ps, temperatur, kwAuslegung, nutzanteil, null);
                            lauf = SpeicherRechnen(p, ps, a, temperatur, din, true, tag, tagGrund, woche, kwAuslegung, nutzanteil,
                                                   zuschlag, zirk, laufzeit, wohnen, allePersonen, personenBezug, prot);
                            gross = Grossanlage.Erkennen(Grossanlage.Speichervolumen(p, lauf.EmpfohlenL), leitung, ps);
                        }
                        catch (ParametersatzException ex)
                        {
                            h.Add(new Auslegungshinweis(HINWEIS_TEMPERATUR_GROSSANLAGE,
                                ZapfSatz.Neu("AUSHINWEIS_TEMPERATUR_GROSSANLAGE_BLEIBT", ex.Satz, temperatur.SpeicherC), true));
                        }
                        catch (ZapfAuslegungException ex)
                        {
                            h.Add(new Auslegungshinweis(HINWEIS_TEMPERATUR_GROSSANLAGE,
                                ZapfSatz.Neu("AUSHINWEIS_TEMPERATUR_GROSSANLAGE_BLEIBT", ex.Satz, temperatur.SpeicherC), true));
                        }
                    }
                    h.AddRange(lauf.Hinweise);
                    sl = lauf.Summenlinie;
                    slp = lauf.Parameter;

                    // --- Speicherauslegung nach V4 (nachrichtlich), mit Summenlinienpunkt und Großanlage ---
                    if (lauf.Eingang != null)
                    {
                        try
                        {
                            sa = TwwSpeicherauslegung.Rechnen(lauf.Eingang with
                            {
                                SummenlinienpunktL = sl?.Punkt.VolumenL,
                                Grossanlage = gross?.Gross
                            }, ps);
                        }
                        catch (ZapfAuslegungException ex) { h.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Satz, true)); }
                        catch (ParametersatzException ex) { h.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Satz, true)); }
                    }
                }

                // --- Summenlinie: die Empfehlung ---------------------------------------------------
                ZapfSatz slGrund = speicherGrund ?? lauf?.Grund ?? tagGrund;
                if (sl != null)
                {
                    // Schnellauslegung (N11 (c)): der Schnellpfad des Vereinfachungsverfahrens oder die Stufe Einfach.
                    bool schnell = sl.Schnellpfad || a.Stufe == ZapfStufe.Einfach;
                    var vermerke = new List<ZapfSatz> { ZapfSatz.Neu(Summenlinie.VERMERK_ENTWURF) };
                    if (schnell) vermerke.Add(ZapfSatz.Neu("AUSTEXT_VERMERK_SCHNELLAUSLEGUNG"));
                    if (tag.SpitzenUnterschaetzt) vermerke.Add(ZapfSatz.Neu("AUSTEXT_VERMERK_SPITZEN"));
                    haupt = new Auslegungswert(ZapfAuslegungsverfahren.Summenlinie, Auslegungsstatus.Gerechnet, sl.Punkt.VolumenL,
                        sl.Punkt.LeistungKw, true,
                        ZapfSatz.Neu("AUSTEXT_SUMMENLINIE", sl.Punkt.VolumenL, sl.Punkt.LeistungKw, sl.Punkt.LadezeitH));
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Summenlinie, true, sl.Punkt.VolumenL,
                        sl.Punkt.LeistungKw, lauf.NenninhaltL, schnell, vermerke.AsReadOnly(), null);
                }
                else
                {
                    haupt = new Auslegungswert(ZapfAuslegungsverfahren.Summenlinie, Auslegungsstatus.NichtRechenbar, null, null,
                                               false, slGrund);
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Summenlinie, false, null, null, null, false,
                                                          new ZapfSatz[0], slGrund);
                }

                // --- Großanlage ------------------------------------------------------------------
                if (grossPruefbar && gross == null) gross = vorab;
                if (gross != null)
                {
                    bool zirkulationJa = false;
                    foreach (Zonenarbeit w in zonen) zirkulationJa |= w.Stand.Zirkulation;
                    h.AddRange(Grossanlage.Hinweise(gross, zirkulationJa));
                }
            }
            else
            {
                if (tag != null)
                {
                    haupt = new Auslegungswert(ZapfAuslegungsverfahren.Minutenspitze, Auslegungsstatus.Gerechnet, null,
                        tag.GroessteMinutenleistungKw, true,
                        ZapfSatz.Neu("AUSTEXT_MINUTENSPITZE", tag.GroessteMinutenleistungKw, tag.GroessteStundenleistungKw));
                    bool einfach = a.Stufe == ZapfStufe.Einfach;
                    var vermerke = new List<ZapfSatz>();
                    if (einfach) vermerke.Add(ZapfSatz.Neu("AUSTEXT_VERMERK_SCHNELLAUSLEGUNG"));
                    if (tag.SpitzenUnterschaetzt) vermerke.Add(ZapfSatz.Neu("AUSTEXT_VERMERK_SPITZEN"));
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Minutenspitze, true, null,
                        tag.GroessteMinutenleistungKw, null, einfach, vermerke.AsReadOnly(), null);
                }
                else
                {
                    haupt = new Auslegungswert(ZapfAuslegungsverfahren.Minutenspitze, Auslegungsstatus.NichtRechenbar, null, null,
                                               false, tagGrund);
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Minutenspitze, false, null, null, null, false,
                                                          new ZapfSatz[0], tagGrund);
                }
            }

            // --- Perzentil aus dem Auslegungsensemble (4.5 b) — nur auf Zuruf -------------------------
            Auslegungswert perzentilwert = Dreiergruppe.PerzentilOffen();
            Perzentilergebnis perzentil = null;
            if (a.Stochastisch)
                perzentilwert = Stochastik(topo, zonen, bausteine, p, ps, kategorien, kwAuslegung, sl, slp, h, a.Abbruch, out perzentil);
            if (topo == ZapfTopologie.Wohnungsstation && perzentil == null)
                h.Add(new Auslegungshinweis("WOHNUNGSSTATION_JE_EINHEIT", ZapfSatz.Neu("AUSHINWEIS_WOHNUNGSSTATION_SUMME")));

            IReadOnlyList<Auslegungswert> dreier = Dreiergruppe.Bilden(haupt, perzentilwert, Dreiergruppe.Normvergleich(din));
            h.AddRange(Dreiergruppe.Reihenfolge(topo, dreier[0], dreier[1], dreier[2]));
            h.AddRange(din.Hinweise);
            if (sa != null) h.AddRange(sa.Hinweise);

            return new Auslegungsgruppe
            {
                Topologie = topo,
                Zonen = namen.AsReadOnly(),
                Wohnen = wohnen,
                Bedarfstagwahl = wahl,
                Bedarfstag = tag,
                Woche = woche,
                Summenlinie = sl,
                Dreiergruppe = dreier,
                Empfehlung = empfehlung,
                Normvergleich = din,
                Speicherauslegung = sa,
                Grossanlage = gross,
                Speichertemperatur = temperatur,
                ZirkulationLaufzeit = laufzeit,
                Perzentil = perzentil,
                Hinweise = h.AsReadOnly()
            };
        }

        // =================================================================================
        // Das Perzentil aus dem Auslegungsensemble (4.4, 4.5 b)
        // =================================================================================

        /// <summary>
        /// <b>Das Perzentil einer Topologiegruppe</b> (4.5 b): je Zone n_E Einheiten mit der
        /// Tagesmenge des maßgebenden Tags (größte Tagessumme der Gruppe bei θ_KW,Auslegung, derselbe
        /// Tag wie beim Stundenprofil) und der Tageszeitdichte seines Tagtyps; R Realisierungen
        /// (Projekt, sonst Vielfaches der Mindestzahl aus dem Parametersatz), Seed des Projekts. Bei
        /// Speicher das erforderliche Volumen beim Φ_N des Summenlinienpunkts, sonst die Minutenspitze;
        /// dazu die Gleichzeitigkeit, „nicht belastbar" bei R &lt; 1/(1 − p), der Konsistenzhinweis
        /// (N11 (f)), der Vergleich μ + z·σ/√N und bei der Wohnungsstation die Spitze je Einheit
        /// (N10 (d)). Ist das Ensemble nicht rechenbar, bleibt das Perzentil benannt „nicht rechenbar".
        /// </summary>
        private static Auslegungswert Stochastik(ZapfTopologie topo, List<Zonenarbeit> zonen, List<Wochenbaustein> bausteine,
                                                 ProjektStand p, Parametersatz ps, IReadOnlyList<Zapfkategorie> kategorien,
                                                 double kwAuslegung, Summenlinienergebnis sl, Summenlinienparameter slp,
                                                 List<Auslegungshinweis> h, CancellationToken abbruch, out Perzentilergebnis ergebnis)
        {
            ergebnis = null;
            bool speicher = topo == ZapfTopologie.Speicher;
            try
            {
                if (speicher && (sl == null || slp == null))
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig, ZapfSatz.Neu("AUSLEGUNG_PERZENTIL_OHNE_PUNKT"));
                int perzentil = p.Perzentil;
                int realisierungen = Zapfensemble.RealisierungenAuslegung(p.RealisierungenAuslegung, perzentil, ps);
                int tag = Wochenreihe.GroessterTag(bausteine);
                var ensemblezonen = new List<Ensemblezone>(zonen.Count);
                foreach (Zonenarbeit w in zonen)
                    ensemblezonen.Add(new Ensemblezone(w.Index, w.Name,
                        Zapfeinheiten.Anzahl(w.Stand, w.Art, w.Menge.Bezugsmenge, ps),
                        Zapfkategoriensatz.Aus(kategorien, w.Art, w.Name),
                        w.Baustein.TagesmengenKwh[tag - 1],
                        Auslegungspruefung.Spreizung(w.Temperaturen.ZapfC, kwAuslegung, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_ZAPF_AUSLEGUNG")),
                        Tageszeitdichte.Aus(w.Struktur, w.Kalender[tag - 1])));
                // Bei Speicher rechnet jede Realisierung ihr Volumen beim Φ_N des Summenlinienpunkts gleich mit
                // (Volumenauftrag) — das Ensemble bewahrt keine gezogenen Tage auf.
                Bedarfstagensemble ens = Zapfensemble.Ziehen(ensemblezonen, p.Seed, realisierungen, perzentil,
                    speicher ? new Volumenauftrag(slp, sl.Punkt.LeistungKw) : null, abbruch: abbruch);
                Speicherensemble volumen = ens.Volumina;

                ergebnis = new Perzentilergebnis
                {
                    Topologie = topo, Perzentil = perzentil, Seed = p.Seed, Realisierungen = realisierungen,
                    Tag = tag, Belastbar = ens.Belastbar, MinutenspitzeKw = ens.MinutenspitzeKw, StundenspitzeKw = ens.StundenspitzeKw,
                    VolumenL = volumen?.VolumenL, LeistungKw = volumen?.LeistungKw, OhneNachweis = volumen?.OhneNachweis ?? 0,
                    GleichzeitigkeitLeistung = ens.GleichzeitigkeitLeistung, GleichzeitigkeitVolumen = volumen?.GleichzeitigkeitVolumen,
                    Zonen = ens.Zonen
                };
                ZapfSatz glf = GleichzeitigkeitTeil(ergebnis.GleichzeitigkeitVolumen, ergebnis.GleichzeitigkeitLeistung);
                ZapfSatz belastbar = ens.Belastbar ? null : ZapfSatz.Neu("AUSTEXT_NICHT_BELASTBAR_TEIL");
                if (!ens.Belastbar)
                    h.Add(new Auslegungshinweis("PERZENTIL_NICHT_BELASTBAR",
                        ZapfSatz.Neu("AUSHINWEIS_PERZENTIL_NICHT_BELASTBAR", perzentil, realisierungen, Zapfensemble.Mindestzahl(perzentil)), true));

                // Vergleich μ + z·σ/√N aus der Einzelstatistik (Konzept S4c) — nur ein Hinweis.
                double? quantil = ZapfAuslegungParameter.Wahlweise(ps, ZapfStochastikParameter.QUANTIL + perzentil.ToString(CultureInfo.InvariantCulture),
                    ZapfSatz.Neu("FOLGE_WURZEL_N_ENTFAELLT"), h);
                if (quantil.HasValue)
                {
                    ergebnis = ergebnis with { WurzelNSchaetzungKw = ens.WurzelNSchaetzungKw(quantil.Value) };
                    h.Add(new Auslegungshinweis("WURZEL_N_VERGLEICH",
                        ZapfSatz.Neu("AUSHINWEIS_WURZEL_N_VERGLEICH", perzentil, ens.MinutenspitzeKw.Wert(perzentil),
                                     ergebnis.WurzelNSchaetzungKw.Value, quantil.Value)));
                }

                if (topo == ZapfTopologie.Wohnungsstation)
                {
                    Ensemblezonenstatistik groesste = null;
                    foreach (Ensemblezonenstatistik z in ens.Zonen)
                        if (groesste == null || z.SpitzeJeEinheitKw.Wert(perzentil) > groesste.SpitzeJeEinheitKw.Wert(perzentil)) groesste = z;
                    ergebnis = ergebnis with
                    {
                        SpitzeJeEinheitKw = groesste.SpitzeJeEinheitKw.Wert(perzentil), SpitzeJeEinheitZone = groesste.Zone
                    };
                    h.Add(new Auslegungshinweis("WOHNUNGSSTATION_JE_EINHEIT",
                        ZapfSatz.Neu("AUSHINWEIS_WOHNUNGSSTATION_JE_EINHEIT", perzentil, groesste.SpitzeJeEinheitKw.Wert(perzentil),
                                     groesste.Zone ?? "", ens.MinutenspitzeKw.Wert(perzentil))));
                }

                if (speicher)
                {
                    double v = volumen.VolumenL.Wert(perzentil);
                    double phi = sl.Punkt.LeistungKw;
                    double? schwelle = ZapfAuslegungParameter.Wahlweise(ps, ZapfStochastikParameter.KONSISTENZSCHWELLE,
                        ZapfSatz.Neu("FOLGE_KONSISTENZ_ENTFAELLT"), h);
                    double spitze = ens.StundenspitzeKw.Wert(perzentil);
                    // Die Probe als Werte (N11 (k)): die Oberfläche baut ihren Satz daraus, nicht aus dem Satz des Kerns.
                    bool auffaellig = schwelle.HasValue && spitze > schwelle.Value * phi;
                    if (schwelle.HasValue)
                        ergebnis = ergebnis with
                        {
                            KonsistenzSchwelle = schwelle.Value, KonsistenzSpitzeKw = spitze,
                            KonsistenzGrenzeKw = schwelle.Value * phi, KonsistenzAuffaellig = auffaellig
                        };
                    if (auffaellig)
                        h.Add(new Auslegungshinweis("KONSISTENZ_STOCHASTISCHE_SPITZE",
                            ZapfSatz.Neu("AUSHINWEIS_KONSISTENZ_STOCHASTISCHE_SPITZE", perzentil, spitze, schwelle.Value, phi), true));
                    if (double.IsPositiveInfinity(v))
                        return new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.NichtRechenbar, null, phi, false,
                            ZapfSatz.Neu("AUSTEXT_PERZENTIL_KEIN_VOLUMEN", perzentil, phi, volumen.OhneNachweis, realisierungen));
                    object band = double.IsPositiveInfinity(volumen.VolumenL.Maximum) ? "∞" : (object)volumen.VolumenL.Maximum;
                    return new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.Gerechnet, v, phi, false,
                        ZapfSatz.Neu("AUSTEXT_PERZENTIL_SPEICHER", perzentil, v, phi, realisierungen, p.Seed,
                                     volumen.VolumenL.Minimum, band, glf, belastbar));
                }
                double pk = ens.MinutenspitzeKw.Wert(perzentil);
                return new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.Gerechnet, null, pk, false,
                    ZapfSatz.Neu("AUSTEXT_PERZENTIL_MINUTE", perzentil, pk, ens.StundenspitzeKw.Wert(perzentil), realisierungen, p.Seed,
                                 ens.MinutenspitzeKw.Minimum, ens.MinutenspitzeKw.Maximum, glf, belastbar));
            }
            // Die benannte Ablehnung des Rechenwegs reist mit Kennung und Werten weiter — nicht nur ihr Satz.
            catch (ZapfprofilEingabeException ex) { return PerzentilNichtRechenbar(ex.Satz, h, ZapfAblehnung.Aus(ex.Zone, ex)); }
            catch (ParametersatzException ex) { return PerzentilNichtRechenbar(ex.Satz, h, null); }
            catch (ZapfAuslegungException ex) { return PerzentilNichtRechenbar(ex.Satz, h, null); }
        }

        /// <summary>Der Teil „; GLF_V … ; GLF_P …" der Karte (b) als Satz — <c>null</c> (leer) ohne Gleichzeitigkeit.</summary>
        private static ZapfSatz GleichzeitigkeitTeil(double? glfV, double? glfP)
        {
            if (glfV.HasValue && glfP.HasValue) return ZapfSatz.Neu("AUSTEXT_GLF_TEIL_VP", glfV.Value, glfP.Value);
            if (glfV.HasValue) return ZapfSatz.Neu("AUSTEXT_GLF_TEIL_V", glfV.Value);
            if (glfP.HasValue) return ZapfSatz.Neu("AUSTEXT_GLF_TEIL_P", glfP.Value);
            return null;
        }

        private static Auslegungswert PerzentilNichtRechenbar(ZapfSatz grund, List<Auslegungshinweis> h, ZapfAblehnung ablehnung)
        {
            h.Add(new Auslegungshinweis("STOCHASTIK_NICHT_RECHENBAR", grund, true) { Ablehnung = ablehnung });
            return new Auslegungswert(ZapfAuslegungsverfahren.Perzentil, Auslegungsstatus.NichtRechenbar, null, null, false, grund)
            {
                Ablehnung = ablehnung
            };
        }

        /// <summary>
        /// Der Normvergleich der Gruppe: bei Speicher mit der EINEN Speichertemperatur; ohne sie
        /// (ein Pflichtwert fehlt) nicht rechenbar mit Grund; außerhalb der Topologie Speicher ist
        /// jede Zone außerhalb des Gültigkeitsbereichs, Spreizung und Nutzanteil werden nicht gelesen.
        /// </summary>
        private static Din4708Ergebnis Normvergleich(ZapfTopologie topo, List<(ZonenStand, Nutzungsart)> paare, Auslegungseingang a,
                                                     Parametersatz ps, Speichertemperaturwahl temperatur, double kwAuslegung,
                                                     double nutzanteil, ZapfSatz speicherGrund)
        {
            bool speicher = topo == ZapfTopologie.Speicher;
            if (speicher && temperatur != null)
                return Din4708Kennzahl.Rechnen(paare, a.Din4708, ps, temperatur.SpeicherC - kwAuslegung, nutzanteil);
            Din4708Ergebnis din = Din4708Kennzahl.Rechnen(speicher ? new (ZonenStand, Nutzungsart)[0] : paare, a.Din4708, ps, 1.0, 1.0);
            if (speicher)
                din = din with
                {
                    Grund = ZapfSatz.Neu("AUSTEXT_DIN_NICHT_RECHENBAR_GRUND", (object)speicherGrund ?? ""),
                    Fehler = ZapfAuslegungsfehler.ParameterFehlt
                };
            return din;
        }

        /// <summary>
        /// Ein Durchgang der Speichergruppe bei der Speichertemperatur <paramref name="temperatur"/>:
        /// Speicherauslegung nach V4 (für die angesetzte Ladeleistung), Summenlinie mit derselben
        /// Temperatur (im Schnellpfad mit den Setzungen des Vereinfachungsverfahrens) und der
        /// Nenninhalt des Punkts. Hinweise sammelt der Durchgang für sich.
        /// </summary>
        private static Speicherlauf SpeicherRechnen(ProjektStand p, Parametersatz ps, Auslegungseingang a,
                                                    Speichertemperaturwahl temperatur, Din4708Ergebnis din, bool? grossanlage,
                                                    Bedarfstag tag, ZapfSatz tagGrund, Wochenreihe woche, double kwAuslegung,
                                                    double nutzanteil, double zuschlag, Schaetzwert zirk, Tagesfenster laufzeit,
                                                    bool wohnen, bool allePersonen, double personenBezug, Herkunftsprotokoll prot)
        {
            var lauf = new Speicherlauf { Temperatur = temperatur, Din = din };
            Speicherauslegungsergebnis vorlage = null;
            try
            {
                lauf.Eingang = new Speicherauslegungseingang
                {
                    Woche = woche, SpeicherC = temperatur.SpeicherC, KaltwasserAuslegungC = kwAuslegung,
                    Nutzanteil = nutzanteil, Zuschlag = zuschlag,
                    Ladefenster = Ladefenster(p, ps, prot),
                    LadeAuto = p.LadeAuto, LadeManuellKw = p.LadeManuellKw,
                    PersonenAuto = p.PersonenAuto, PersonenManuell = p.PersonenManuell, FuellstandBezugWahl = p.FuellstandBezug,
                    Zirkulation = zirk, ZirkulationLaufzeit = laufzeit,
                    Din = din,
                    Personen = din.Gueltig ? din.Personen : allePersonen ? personenBezug : (double?)null,
                    Wohnen = wohnen, Topologie = ZapfTopologie.Speicher, Nenninhalte = a.Nenninhalte,
                    Grossanlage = grossanlage
                };
                vorlage = TwwSpeicherauslegung.Rechnen(lauf.Eingang, ps);
            }
            catch (ZapfAuslegungException ex)
            {
                lauf.Eingang = null;
                lauf.Hinweise.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Satz, true));
            }
            catch (ParametersatzException ex)
            {
                lauf.Eingang = null;
                lauf.Hinweise.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Satz, true));
            }

            if (tag == null)
            {
                lauf.Grund = tagGrund;
                return lauf;
            }
            try
            {
                Summenlinienparameter slp = Summenlinie.Parameter(p, ps, temperatur.SpeicherC, vorlage?.Ladeleistung.Angesetzt,
                    new Zirkulationslast(zirk.Angesetzt, laufzeit), prot, lauf.Hinweise, a.Erzeugerart, a.Uebertragerwerkstoff);
                if (temperatur.Schnellpfad) slp = Summenlinie.Schnellpfad(slp, ps);
                lauf.Parameter = slp;
                double? n = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.WERTEPAARE,
                    ZapfSatz.Neu("FOLGE_WERTEPAARKURVE_ENTFAELLT"), lauf.Hinweise);
                lauf.Summenlinie = Summenlinie.Rechnen(tag, slp, n.HasValue && n.Value >= 1 ? (int)n.Value : 0);
                lauf.Hinweise.AddRange(lauf.Summenlinie.Hinweise);
            }
            catch (ZapfAuslegungException ex) { lauf.Grund = ex.Satz; }
            catch (ParametersatzException ex) { lauf.Grund = ex.Satz; }

            if (lauf.Summenlinie != null && a.Nenninhalte != null)
            {
                // Über dem Listenende: Mehrspeicheranlage prüfen — auch ohne Raster-Parameter (N10).
                double v = lauf.Summenlinie.Punkt.VolumenL;
                lauf.NenninhaltL = a.Nenninhalte.Runden(v, ps, lauf.Hinweise, out bool ueberEnde);
                if (ueberEnde)
                    lauf.Hinweise.Add(new Auslegungshinweis("MEHRSPEICHER",
                        ZapfSatz.Neu("AUSHINWEIS_MEHRSPEICHER_PUNKT", v, a.Nenninhalte.GroessterL), true));
            }
            return lauf;
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Ein Katalogtag der Gruppe (Quellen 2, 4, 5 und gespeicherte Tage, 4.5; N10): skaliert auf
        /// die Bezugsmenge der Gruppe, wenn der Tag eine trägt — nur, wenn alle Zonen DIESELBE
        /// Bezugsart haben (Mengen verschiedener Bezugsarten werden nie summiert, sonst benannte
        /// Ablehnung) — und auf θ_KW,Auslegung des Projekts umgerechnet. Ein Katalogtag gilt bei
        /// θ_KW,A des Parametersatzes (<c>A100.Kaltwasser.Auslegung</c>):
        /// <c>f = (θ_Zapf − θ_KW,A) / (θ_Zapf − θ_KW,A,Katalog)</c> mit der Zapftemperatur der
        /// Zonen; tragen die Zonen verschiedene Zapftemperaturen, ist die Umrechnung nicht eindeutig
        /// und wird benannt abgelehnt. Ohne abweichendes θ_KW,A ist f = 1.
        ///
        /// <para><b>Ein Tag je Wohneinheit</b> (Bezugsart Wohneinheiten, etwa die Ecodesign-Zapfprofile
        /// mit ihrer Bezugsmenge <c>Q_ref / Q_ref(L)</c>, Folgeposten #546): Tragen die Zonen keine
        /// Bezugsmenge in Wohneinheiten, nennen aber ihre Wohnungstabellen eine WE-Zahl, gilt diese
        /// als Ziel — die Wohneinheiten der Gruppe. Ein Ecodesign-Tag über mehr als
        /// <see cref="ECODESIGN_HINWEIS_WOHNEINHEITEN"/> Wohneinheiten bekommt den Hinweis
        /// <see cref="HINWEIS_ECODESIGN_SKALIERT"/>: linear skaliert, ohne Gleichzeitigkeit.</para>
        /// </summary>
        private static Bedarfstag Katalogtag(BedarfstagKatalogzeile zeile, SortedDictionary<ZapfBezugsart, double> bezugsmengen,
                                             List<Zonenarbeit> zonen, Parametersatz ps, double kwAuslegung,
                                             Herkunftsprotokoll prot, List<Auslegungshinweis> hinweise = null)
        {
            double? ziel = null;
            double we = zeile?.Bezugsart == ZapfBezugsart.Wohneinheiten && !bezugsmengen.ContainsKey(ZapfBezugsart.Wohneinheiten)
                ? Wohneinheiten(zonen) : 0.0;
            if (zeile?.Bezugsmenge != null && zeile.Bezugsmenge.Value > 0 && we > 0.0)
            {
                // Die Wohneinheiten der Wohnungstabellen tragen den Tag je Wohneinheit, gleich welche
                // Bezugsart die Zonen führen (Folgeposten #546).
                ziel = we;
            }
            else if (zeile?.Bezugsmenge != null && zeile.Bezugsmenge.Value > 0)
            {
                if (bezugsmengen.Count != 1)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_KATALOGTAG_BEZUGSARTEN", zeile.Bezeichner ?? "",
                                     bezugsmengen.Keys.Select(Bezugsartbegriff).ToArray()));
                foreach (KeyValuePair<ZapfBezugsart, double> m in bezugsmengen)
                {
                    // Die Bezugsart des Tages (Schemaschritt 124, N10 (j)): Skaliert wird nur auf eine
                    // Menge derselben Bezugsart; ein Tag ohne Bezugsart gilt wie bisher für jede.
                    if (zeile.Bezugsart.HasValue && zeile.Bezugsart.Value != m.Key)
                        throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                            ZapfSatz.Neu("AUSLEGUNG_KATALOGTAG_BEZUGSART", zeile.Bezeichner ?? "",
                                         Bezugsartbegriff(zeile.Bezugsart.Value), Bezugsartbegriff(m.Key)));
                    ziel = m.Value;
                }
            }
            double faktor = Bedarfstag.Skalierung(zeile, ziel);
            if (hinweise != null && zeile.QuelleArt == ZapfBedarfstagquelle.Ecodesign && ziel.HasValue
                && ziel.Value > ECODESIGN_HINWEIS_WOHNEINHEITEN)
                hinweise.Add(new Auslegungshinweis(HINWEIS_ECODESIGN_SKALIERT,
                    ZapfSatz.Neu("AUSHINWEIS_ECODESIGN_SKALIERT", zeile.Bezeichner ?? "", ziel.Value,
                                 ECODESIGN_HINWEIS_WOHNEINHEITEN), false));

            double kwKatalog = ps.Wert(ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG);
            if (kwAuslegung != kwKatalog)
            {
                double? zapf = null;
                foreach (Zonenarbeit w in zonen)
                {
                    if (zapf.HasValue && zapf.Value != w.Temperaturen.ZapfC)
                        throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                            ZapfSatz.Neu("AUSLEGUNG_KATALOGTAG_ZAPFTEMPERATUREN", zeile?.Bezeichner ?? "", kwKatalog, kwAuslegung));
                    zapf = w.Temperaturen.ZapfC;
                }
                double oben = Auslegungspruefung.Spreizung(zapf.Value, kwAuslegung, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_ZAPF_AUSLEGUNG"));
                double unten = Auslegungspruefung.Spreizung(zapf.Value, kwKatalog, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_ZAPF_KATALOGTAG"));
                double f = oben / unten;
                prot?.Vermerken("", ZapfFeld.AUSLEGUNG_BEDARFSTAGFAKTOR, f, "-", Wertstatus.Umgerechnet, zeile?.Herkunft,
                                ZapfSatz.Neu("HERKUNFT_BEDARFSTAGFAKTOR", zapf.Value, kwAuslegung, kwKatalog));
                faktor *= f;
            }
            return Bedarfstag.AusKatalog(zeile, faktor);
        }

        /// <summary>
        /// Die Tagesmitte der Zapfung [h] wie in der Bilanz (Zirkulationskanal.Tagesmitte, N7 (g)):
        /// Schwerpunkt der Stundensummen <c>Σ_d Q_d · φ_Tagtyp(d)(h)</c> der Zonen in Z1 (ohne Z1
        /// aller Zonen), mit den Tagesmengen der Bilanz — ohne eine Stundenreihe zu bilden.
        /// </summary>
        private static double Tagesmitte(List<Zonenarbeit> arbeit, int wochentagJan1)
        {
            var z1 = arbeit.FindAll(w => !w.Abgelehnt && w.InZ1);
            List<Zonenarbeit> quelle = z1.Count > 0 ? z1 : arbeit.FindAll(w => !w.Abgelehnt);
            var e = new double[Zapfkalender.STUNDEN_TAG];
            foreach (Zonenarbeit w in quelle)
            {
                double[] tage = Formvektor.Tagesmengen(w.ZapfungKwh, w.Struktur, w.Kalender, wochentagJan1, w.Kaltwasserfaktor, w.Name);
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    for (int d = 0; d < Zapfkalender.TAGE; d++)
                        e[h] += tage[d] * w.Struktur.Tagesgaenge[(int)w.Kalender[d] - 1, h];
            }
            double zaehler = 0.0, nenner = 0.0;
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
            {
                zaehler += (h + 0.5) * e[h];
                nenner += e[h];
            }
            return nenner > 0 ? zaehler / nenner : Zirkulationskanal.TAGESMITTE_OHNE_ZAPFUNG_H;
        }

        /// <summary>Die WE-Zahl der Gruppe: Wohnungstabellen, sonst Bezugsmengen der Bezugsart Wohneinheiten.</summary>
        private static double Wohneinheiten(List<Zonenarbeit> zonen)
        {
            double we = 0.0;
            foreach (Zonenarbeit w in zonen)
            {
                if (w.Stand.Wohnungen != null && w.Stand.Wohnungen.Count > 0)
                    foreach (WohnungstypStand t in w.Stand.Wohnungen) we += t.Anzahl;
                else if (w.Art.Bezug == ZapfBezugsart.Wohneinheiten) we += w.Menge.Bezugsmenge;
            }
            return we;
        }

        private static void Ablehnen(Zonenarbeit w, ZapfSatz satz, List<Auslegungsablehnung> liste)
        {
            w.Abgelehnt = true;
            liste.Add(new Auslegungsablehnung(w.Name, satz));
        }

        /// <summary>Die Bezugsart als Begriff (<c>BEGRIFF_BEZUGSART_1</c> … <c>_7</c>).</summary>
        internal static ZapfSatz Bezugsartbegriff(ZapfBezugsart b)
            => ZapfSatz.Neu("BEGRIFF_BEZUGSART_" + ((int)b).ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Das Ladefenster der Speicherauslegung (4.7): Beginn und Länge aus dem Projekt, sonst aus dem
        /// Parametersatz — derselbe Weg für die Auslegung und die Schätzhilfe der Ladeleistung.
        /// </summary>
        internal static Tagesfenster Ladefenster(ProjektStand p, Parametersatz ps, Herkunftsprotokoll prot)
            => new Tagesfenster(
                ZapfAuslegungParameter.ProjektOderParameter(p.LadefensterBeginnH, ZapfAuslegungParameter.LADEFENSTER_BEGINN,
                    ps, prot, ZapfFeld.AUSLEGUNG_LADEFENSTER_BEGINN, "h", ZapfSatz.Neu("BEGRIFF_LADEFENSTER_BEGINN")),
                ZapfAuslegungParameter.ProjektOderParameter(p.LadefensterH, ZapfAuslegungParameter.LADEFENSTER_LAENGE,
                    ps, prot, ZapfFeld.AUSLEGUNG_LADEFENSTER, "h", ZapfSatz.Neu("BEGRIFF_LADEFENSTER_LAENGE")));

        private static Nutzungsart Suchen(IReadOnlyList<Nutzungsart> katalog, int id)
        {
            if (katalog == null) return null;
            foreach (Nutzungsart n in katalog) if (n != null && n.Id == id) return n;
            return null;
        }

        private static Tagesgangsatz SuchenSatz(IReadOnlyList<Tagesgangsatz> saetze, int id)
        {
            if (saetze == null) return null;
            foreach (Tagesgangsatz s in saetze) if (s != null && s.Id == id) return s;
            return null;
        }
    }
}
