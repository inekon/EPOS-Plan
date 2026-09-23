using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// Großanlage, sonst die Minutenspitze. Je Gruppe genau eine Empfehlung; das Perzentil bleibt
    /// bis Z3 „noch nicht gerechnet".</para>
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
        }

        /// <summary>Rechnet die Auslegung. Kalender, Projektgrößen und Parametersatz sind Pflicht.</summary>
        internal static Auslegungsergebnis Rechnen(Zapfprofileingang e, IReadOnlyList<Nutzungsart> katalog,
                                                   Auslegungseingang a)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            a ??= new Auslegungseingang();
            Zapfkalender.Pruefen(e.WochentagJan1, e.We);
            if (e.Projekt == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — die Projektgrößen der Auslegung fehlen.");
            if (e.Parameter == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.ParameterFehlt,
                    "Nicht rechenbar — der Parametersatz der Auslegung fehlt.");
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
                var w = new Zonenarbeit { Stand = z, Name = z?.Name ?? "" };
                arbeit.Add(w);
                if (z == null)
                {
                    Ablehnen(w, "Eine Zone ohne Angaben.", ablehnungen);
                    continue;
                }
                try
                {
                    w.Art = Suchen(katalog, z.IdNutzungsart)
                            ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.NutzungsartFehlt, w.Name,
                                   "Nicht rechenbar — die Nutzungsart " + z.IdNutzungsart + " der Zone „" + w.Name
                                   + "“ steht nicht im Katalog.");
                    Tagesgangsatz satz = w.Art.Tagesgaenge;
                    if (z.IdTagesgangsatz.HasValue)
                        satz = SuchenSatz(e.Tagesgangsaetze, z.IdTagesgangsatz.Value)
                               ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.TagesgangsatzFehlt, w.Name,
                                      "Nicht rechenbar — der Tagesgangsatz " + z.IdTagesgangsatz.Value + " der Zone „"
                                      + w.Name + "“ fehlt.");
                    w.Temperaturen = Mengengeruest.Temperaturen(z, w.Art, ps, prot);
                    w.Menge = Mengengeruest.JahresenergieKwh(z, w.Art, w.Temperaturen, ps, belegung, prot, zapfHinweise);
                    w.Struktur = Formvektor.Bilden(z, w.Art, satz, ps, prot, zapfHinweise);
                    w.Kaltwasserfaktor = Kaltwassergang.Monatsfaktoren(w.Temperaturen, w.Name);
                    w.Kalender = Zapfkalender.Bilden(e.WochentagJan1, e.We, Zapfkalender.FensterDerZone(z));
                    w.Messwert = Mengengeruest.MesswertAus(z, w.Temperaturen, prot);
                    w.InZ1 = z.Zirkulation && w.Art.Grenze == ZapfBilanzgrenze.Zapfstelle;
                    w.ZapfungKwh = w.Menge.JahresenergieKwh;
                }
                catch (ZapfprofilEingabeException ex) { Ablehnen(w, ex.Message, ablehnungen); }
                catch (ParametersatzException ex) { Ablehnen(w, ex.Message, ablehnungen); }
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
            catch (ZapfprofilEingabeException ex) { hinweise.Add(new Auslegungshinweis("ZIRKULATION_NICHT_RECHENBAR", ex.Message, true)); }
            catch (ParametersatzException ex) { hinweise.Add(new Auslegungshinweis("ZIRKULATION_NICHT_RECHENBAR", ex.Message, true)); }

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
                catch (ZapfprofilEingabeException ex) { Ablehnen(w, ex.Message, ablehnungen); }
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
                ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG, ps, prot, "Auslegung.KaltwasserC", "°C");
            foreach (Zonenarbeit w in arbeit)
            {
                if (w.Abgelehnt) continue;
                try
                {
                    double f = Wochenreihe.KaltwasserfaktorAuslegung(w.Temperaturen.ZapfC, kwAuslegung, w.Temperaturen.KaltwasserMittelC);
                    prot.Vermerken(w.Name, "Auslegung.Kaltwasserfaktor", f, "-", Wertstatus.Umgerechnet, null,
                                   "(θ_Zapf − θ_KW,Auslegung) / (θ_Zapf − θ̄_KW)");
                    w.Baustein = new Wochenbaustein(w.Name,
                        Wochenreihe.TagesmengenAuslegung(w.ZapfungKwh, f, w.Struktur, w.Kalender, e.WochentagJan1, w.Name),
                        w.Struktur, w.Kalender);
                }
                catch (ZapfAuslegungException ex) { Ablehnen(w, ex.Message, ablehnungen); }
                catch (ZapfprofilEingabeException ex) { Ablehnen(w, ex.Message, ablehnungen); }
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
                gruppen.Add(Gruppe(topo, zonen, p, ps, a, e.WochentagJan1, region, zirk, laufzeit, kwAuslegung, prot));
            }

            foreach (ZapfHinweis h in zapfHinweise) hinweise.Add(new Auslegungshinweis(h.Code, h.Text));
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

        /// <summary>Ein Durchgang der Speichergruppe bei einer Speichertemperatur.</summary>
        private sealed class Speicherlauf
        {
            internal Speichertemperaturwahl Temperatur;
            internal Din4708Ergebnis Din;
            internal Speicherauslegungseingang Eingang;
            internal Summenlinienergebnis Summenlinie;
            internal string Grund;
            internal double? NenninhaltL;
            internal readonly List<Auslegungshinweis> Hinweise = new List<Auslegungshinweis>();

            /// <summary>Das empfohlene Volumen der Großanlagenerkennung: Nenninhalt, sonst Punkt der Summenlinie (N10).</summary>
            internal double? EmpfohlenL => NenninhaltL ?? Summenlinie?.Punkt.VolumenL;
        }

        private static Auslegungsgruppe Gruppe(ZapfTopologie topo, List<Zonenarbeit> zonen, ProjektStand p, Parametersatz ps,
                                               Auslegungseingang a, int wochentagJan1, ZapfTagtyp[] region, Schaetzwert zirk,
                                               Tagesfenster laufzeit, double kwAuslegung, Herkunftsprotokoll prot)
        {
            var h = new List<Auslegungshinweis>();
            var bausteine = new List<Wochenbaustein>();
            var namen = new List<string>();
            var paare = new List<(ZonenStand, Nutzungsart)>();
            bool wohnen = true, allePersonen = true;
            double bezugsmenge = 0.0, personenBezug = 0.0;
            foreach (Zonenarbeit w in zonen)
            {
                bausteine.Add(w.Baustein);
                namen.Add(w.Name);
                paare.Add((w.Stand, w.Art));
                wohnen &= w.Art.Kalender == ZapfKalenderart.Wohnen;
                allePersonen &= w.Art.Bezug == ZapfBezugsart.Personen;
                bezugsmenge += w.Menge.Bezugsmenge;
                if (w.Art.Bezug == ZapfBezugsart.Personen) personenBezug += w.Menge.Bezugsmenge;
            }
            Wochenreihe woche = Wochenreihe.Bilden(bausteine, wochentagJan1, region);
            bool speicher = topo == ZapfTopologie.Speicher;

            // --- Speichergrößen, Großanlage vorab, die EINE Speichertemperatur (nur Speicher) -----
            double nutzanteil = 0.0, zuschlag = 0.0;
            string speicherGrund = null;
            double? leitung = null;
            Grossanlagenbefund vorab = null;
            bool grossPruefbar = false;
            Speichertemperaturwahl temperatur = null;
            if (speicher)
            {
                try
                {
                    nutzanteil = ZapfAuslegungParameter.ProjektOderParameter(p.Nutzanteil, ZapfAuslegungParameter.NUTZANTEIL,
                        ps, prot, "Auslegung.Nutzanteil", "-");
                    zuschlag = ZapfAuslegungParameter.ProjektOderParameter(p.Zuschlag, ZapfAuslegungParameter.ZUSCHLAG,
                        ps, prot, "Auslegung.Zuschlag", "-");
                }
                catch (ParametersatzException ex) { speicherGrund = ex.Message; }
                catch (ZapfAuslegungException ex) { speicherGrund = ex.Message; }

                // Großanlage vorab: Projektvolumen und Leitungsinhalt, noch ohne Summenlinie.
                try
                {
                    leitung = Grossanlage.Leitungsinhalt(p, ps);
                    vorab = Grossanlage.Erkennen(Grossanlage.Speichervolumen(p, null), leitung, ps);
                    grossPruefbar = true;
                }
                catch (ParametersatzException ex)
                {
                    Auslegungshinweis.Einmal(h, new Auslegungshinweis(ZapfHinweis.PARAMETER_FEHLT,
                        ex.Message + " Die Großanlage wird nicht erkannt."));
                }
                catch (ZapfAuslegungException ex) { h.Add(new Auslegungshinweis("GROSSANLAGE_NICHT_RECHENBAR", ex.Message, true)); }

                // Schnellpfad: Wohnen bis zur Anwendungsgrenze, das Projekt nennt weder Sensorhöhe noch Temperatur.
                bool schnellpfad = false;
                if (wohnen && !p.SensorhoeheAnteil.HasValue && !p.SpeicherC.HasValue)
                {
                    try { schnellpfad = Summenlinie.SchnellpfadGilt(true, Wohneinheiten(zonen), ps); }
                    catch (ParametersatzException ex)
                    {
                        Auslegungshinweis.Einmal(h, new Auslegungshinweis(ZapfHinweis.PARAMETER_FEHLT,
                            ex.Message + " Der Schnellpfad entfällt."));
                    }
                }
                if (speicherGrund == null)
                {
                    try
                    {
                        temperatur = Speichertemperaturwahl.Waehlen(p, ps, vorab?.Gross == true, schnellpfad, prot);
                        Auslegungspruefung.Spreizung(temperatur.SpeicherC, kwAuslegung, "Speicher − Kaltwasser der Auslegung");
                    }
                    catch (ParametersatzException ex) { speicherGrund = ex.Message; temperatur = null; }
                    catch (ZapfAuslegungException ex) { speicherGrund = ex.Message; temperatur = null; }
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
            string tagGrund = wahl.Grund;
            try
            {
                switch (wahl.Quelle)
                {
                    case null:
                        break;
                    case ZapfBedarfstagquelle.Stundenprofil:
                        int d = Wochenreihe.GroessterTag(bausteine);
                        tag = Bedarfstag.AusStunden(Wochenreihe.Tagesstunden(bausteine, d),
                            "Stundenprofil, Tag " + d.ToString(CultureInfo.InvariantCulture));
                        break;
                    case ZapfBedarfstagquelle.Din4708Profil:
                        tag = Bedarfstag.Din4708(din.WzKwh.Value, Zapfblock.AusParametern(ps), din.KennzahlN.Value);
                        break;
                    default:
                        tag = Bedarfstag.AusKatalog(gewaehlt, Bedarfstag.Skalierung(gewaehlt, bezugsmenge));
                        break;
                }
            }
            catch (ZapfAuslegungException ex) { tagGrund = ex.Message; }
            catch (ParametersatzException ex) { tagGrund = ex.Message; }
            if (tag == null)
                h.Add(new Auslegungshinweis(wahl.KonstruktorOeffnen ? "KONSTRUKTOR_OEFFNEN" : "BEDARFSTAG_NICHT_RECHENBAR", tagGrund, true));
            else if (tag.SpitzenUnterschaetzt && !speicher)
                h.Add(new Auslegungshinweis(Bedarfstag.VERMERK_SPITZEN_UNTERSCHAETZT,
                    "Der Bedarfstag stammt aus einem Stundenprofil — Spitzen unter einer Stunde sind unterschätzt.", true));

            Auslegungswert haupt;
            Auslegungsempfehlung empfehlung;
            Summenlinienergebnis sl = null;
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
                            Auslegungspruefung.Spreizung(mindest.SpeicherC, kwAuslegung, "Speicher − Kaltwasser der Auslegung");
                            h.Add(new Auslegungshinweis(HINWEIS_TEMPERATUR_GROSSANLAGE,
                                "Mit " + Auslegungstext.Z(temperatur.SpeicherC) + " °C ergibt die Auslegung "
                                + Auslegungstext.G(lauf.EmpfohlenL.Value) + " l — eine Großanlage nach DVGW W 551; die Gruppe rechnet "
                                + "deshalb mit der Mindesttemperatur " + Auslegungstext.Z(mindest.SpeicherC) + " °C."));
                            temperatur = mindest;
                            din = Normvergleich(topo, paare, a, ps, temperatur, kwAuslegung, nutzanteil, null);
                            lauf = SpeicherRechnen(p, ps, a, temperatur, din, true, tag, tagGrund, woche, kwAuslegung, nutzanteil,
                                                   zuschlag, zirk, laufzeit, wohnen, allePersonen, personenBezug, prot);
                            gross = Grossanlage.Erkennen(Grossanlage.Speichervolumen(p, lauf.EmpfohlenL), leitung, ps);
                        }
                        catch (ParametersatzException ex)
                        {
                            h.Add(new Auslegungshinweis(HINWEIS_TEMPERATUR_GROSSANLAGE, ex.Message
                                + " Die Gruppe bleibt bei " + Auslegungstext.Z(temperatur.SpeicherC) + " °C.", true));
                        }
                        catch (ZapfAuslegungException ex)
                        {
                            h.Add(new Auslegungshinweis(HINWEIS_TEMPERATUR_GROSSANLAGE, ex.Message
                                + " Die Gruppe bleibt bei " + Auslegungstext.Z(temperatur.SpeicherC) + " °C.", true));
                        }
                    }
                    h.AddRange(lauf.Hinweise);
                    sl = lauf.Summenlinie;

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
                        catch (ZapfAuslegungException ex) { h.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Message, true)); }
                        catch (ParametersatzException ex) { h.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Message, true)); }
                    }
                }

                // --- Summenlinie: die Empfehlung ---------------------------------------------------
                string slGrund = speicherGrund ?? lauf?.Grund ?? tagGrund;
                if (sl != null)
                {
                    string vermerk = Summenlinie.VERMERK_ENTWURF + (sl.Schnellpfad ? "; Schnellauslegung" : "")
                                     + (tag.SpitzenUnterschaetzt ? "; Spitzen unterschätzt" : "");
                    haupt = new Auslegungswert(ZapfAuslegungsverfahren.Summenlinie, Auslegungsstatus.Gerechnet, sl.Punkt.VolumenL,
                        sl.Punkt.LeistungKw, true, "Summenlinie: " + Auslegungstext.G(sl.Punkt.VolumenL) + " l bei "
                        + Auslegungstext.Z(sl.Punkt.LeistungKw) + " kW, Ladezeit " + Auslegungstext.Z(sl.Punkt.LadezeitH) + " h/d");
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Summenlinie, true, sl.Punkt.VolumenL,
                        sl.Punkt.LeistungKw, lauf.NenninhaltL, sl.Schnellpfad, vermerk, "");
                }
                else
                {
                    haupt = new Auslegungswert(ZapfAuslegungsverfahren.Summenlinie, Auslegungsstatus.NichtRechenbar, null, null,
                                               false, slGrund ?? "");
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Summenlinie, false, null, null, null, false, "",
                                                          slGrund ?? "");
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
                        tag.GroessteMinutenleistungKw, true, "Minutenspitze des Bedarfstags " + Auslegungstext.Z(tag.GroessteMinutenleistungKw)
                        + " kW (größte Stundenleistung " + Auslegungstext.Z(tag.GroessteStundenleistungKw) + " kW nachrichtlich)");
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Minutenspitze, true, null,
                        tag.GroessteMinutenleistungKw, null, false, tag.SpitzenUnterschaetzt ? "Spitzen unterschätzt" : "", "");
                }
                else
                {
                    haupt = new Auslegungswert(ZapfAuslegungsverfahren.Minutenspitze, Auslegungsstatus.NichtRechenbar, null, null,
                                               false, tagGrund ?? "");
                    empfehlung = new Auslegungsempfehlung(ZapfAuslegungsverfahren.Minutenspitze, false, null, null, null, false, "",
                                                          tagGrund ?? "");
                }
                if (topo == ZapfTopologie.Wohnungsstation)
                    h.Add(new Auslegungshinweis("WOHNUNGSSTATION_JE_EINHEIT",
                        "Die Spitze je Wohnungsstation folgt mit dem Ensemble (Z3); angegeben ist die Summe."));
            }

            IReadOnlyList<Auslegungswert> dreier = Dreiergruppe.Bilden(haupt, Dreiergruppe.PerzentilOffen(), Dreiergruppe.Normvergleich(din));
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
                Hinweise = h.AsReadOnly()
            };
        }

        /// <summary>
        /// Der Normvergleich der Gruppe: bei Speicher mit der EINEN Speichertemperatur; ohne sie
        /// (ein Pflichtwert fehlt) nicht rechenbar mit Grund; außerhalb der Topologie Speicher ist
        /// jede Zone außerhalb des Gültigkeitsbereichs, Spreizung und Nutzanteil werden nicht gelesen.
        /// </summary>
        private static Din4708Ergebnis Normvergleich(ZapfTopologie topo, List<(ZonenStand, Nutzungsart)> paare, Auslegungseingang a,
                                                     Parametersatz ps, Speichertemperaturwahl temperatur, double kwAuslegung,
                                                     double nutzanteil, string speicherGrund)
        {
            bool speicher = topo == ZapfTopologie.Speicher;
            if (speicher && temperatur != null)
                return Din4708Kennzahl.Rechnen(paare, a.Din4708, ps, temperatur.SpeicherC - kwAuslegung, nutzanteil);
            Din4708Ergebnis din = Din4708Kennzahl.Rechnen(speicher ? new (ZonenStand, Nutzungsart)[0] : paare, a.Din4708, ps, 1.0, 1.0);
            if (speicher)
                din = din with { Grund = "DIN 4708: nicht rechenbar — " + speicherGrund, Fehler = ZapfAuslegungsfehler.ParameterFehlt };
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
                                                    Bedarfstag tag, string tagGrund, Wochenreihe woche, double kwAuslegung,
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
                    Ladefenster = new Tagesfenster(
                        ZapfAuslegungParameter.ProjektOderParameter(p.LadefensterBeginnH, ZapfAuslegungParameter.LADEFENSTER_BEGINN,
                            ps, prot, "Auslegung.LadefensterBeginn", "h"),
                        ZapfAuslegungParameter.ProjektOderParameter(p.LadefensterH, ZapfAuslegungParameter.LADEFENSTER_LAENGE,
                            ps, prot, "Auslegung.Ladefenster", "h")),
                    LadeAuto = p.LadeAuto, LadeManuellKw = p.LadeManuellKw,
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
                lauf.Hinweise.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Message, true));
            }
            catch (ParametersatzException ex)
            {
                lauf.Eingang = null;
                lauf.Hinweise.Add(new Auslegungshinweis("SPEICHERAUSLEGUNG_NICHT_RECHENBAR", ex.Message, true));
            }

            if (tag == null)
            {
                lauf.Grund = tagGrund;
                return lauf;
            }
            try
            {
                Summenlinienparameter slp = Summenlinie.Parameter(p, ps, temperatur.SpeicherC, vorlage?.Ladeleistung.Angesetzt,
                    new Zirkulationslast(zirk.Angesetzt, laufzeit), prot, lauf.Hinweise);
                if (temperatur.Schnellpfad) slp = Summenlinie.Schnellpfad(slp, ps);
                double? n = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.WERTEPAARE,
                    "Die Wertepaarkurve der Summenlinie entfällt.", lauf.Hinweise);
                lauf.Summenlinie = Summenlinie.Rechnen(tag, slp, n.HasValue && n.Value >= 1 ? (int)n.Value : 0);
                lauf.Hinweise.AddRange(lauf.Summenlinie.Hinweise);
            }
            catch (ZapfAuslegungException ex) { lauf.Grund = ex.Message; }
            catch (ParametersatzException ex) { lauf.Grund = ex.Message; }

            if (lauf.Summenlinie != null && a.Nenninhalte != null)
            {
                double v = lauf.Summenlinie.Punkt.VolumenL;
                double? raster = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.NENNINHALT_RASTER,
                    "Über dem Ende der Nenninhaltsliste wird nicht gerundet.", lauf.Hinweise);
                IReadOnlyList<double> liste = a.Nenninhalte.WerteL;
                if (v <= liste[liste.Count - 1] || raster.HasValue)
                    lauf.NenninhaltL = a.Nenninhalte.Naechster(v, raster ?? 1.0, out _);
            }
            return lauf;
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

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

        private static void Ablehnen(Zonenarbeit w, string text, List<Auslegungsablehnung> liste)
        {
            w.Abgelehnt = true;
            liste.Add(new Auslegungsablehnung(w.Name, text));
        }

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
