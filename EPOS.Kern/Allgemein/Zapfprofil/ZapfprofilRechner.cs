using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Fassade des Bilanzrechenwegs</b> (Umsetzungskonzept Zapfprofilgenerator 2.1, 2.3):
    /// aus Zonen, Projektgrößen, Kalender, Katalog und Parametersatz die Bilanzreihen Zapfung
    /// und Zirkulation je Zone und als Summe, die Kennzahlen (4.6) und das Herkunftsprotokoll.
    ///
    /// <para><b>Ablauf (2.3):</b> je Zone Temperaturen und Mengengerüst (S1) → Zirkulationsansatz
    /// und Zonenanteile (S5) aus den Mengen vor der Kalibrierung → Kalibrierung gegen Messwerte
    /// (4.1) → Kalender, Kaltwassergang und Formvektor (S2) → Laufzeitfenster um die Tagesmitte
    /// der Zapfung → Summen und Kennzahlen.</para>
    ///
    /// <para><b>Rein und reproduzierbar:</b> keine Datenbank, kein <c>Dienste.*</c>, feste
    /// Summationsfolge. <b>Kein stiller Rückfall:</b> Kalender, Projektgrößen und
    /// Parametersatz sind Pflicht (benannte Ausnahme); eine Zone, die nicht rechenbar ist, trägt 0
    /// und steht in <see cref="ZapfprofilErgebnis.Ablehnungen"/>; ebenso die Zirkulation.</para>
    ///
    /// <para><b>Rechenweg der Jahresreihe (4.4, 5.3).</b> Vorgabe ist „deterministisch": die
    /// Zapfreihe ist der Formvektor mal Jahresmenge. Steht <see cref="ProjektStand.JahresreiheStochastisch"/>,
    /// ist die Zapfreihe jeder Zone die eine gezogene Realisierung zum <see cref="ProjektStand.Seed"/>
    /// (2.3 Satz 3, 4.4 Ausgaben: „Stundenreihe zum Seed"), mit dem Faktor der Energieprobe auf die
    /// Jahresmenge des Mengengerüsts gebracht — die Energie ist in beiden Wegen dieselbe. Die
    /// <see cref="ProjektStand.Realisierungen"/> Jahre des <see cref="Jahresensemble"/> dienen nur der
    /// Konsistenzprobe (Mittel gegen den deterministischen Pfad, Streuband, s_R), die je Zone im
    /// Ergebnis steht; ihr Mittel geht nie in die Bilanz. Fehlen Kategorien oder Einheiten, trägt
    /// die Zone benannt 0, nie still den deterministischen Weg. Das Laufzeitfenster der
    /// Zirkulation bleibt das der deterministischen Reihe.</para>
    /// </summary>
    internal static class ZapfprofilRechner
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
            internal double? Kalibrierfaktor;
            internal Bilanzreihe Zapfreihe;
            internal int Index;
            internal Bilanzreihe Deterministisch;
            internal Jahreskonsistenz Konsistenz;
            internal Schaetzhilfe Tagesbedarf;
            internal Mengenergebnis TagesbedarfVorschlag;
            internal Typtagjahr Typtagjahr;
            internal double[] Tagesmengen;
        }

        /// <summary>
        /// Rechnet die Bilanz. Kalender, Projektgrößen und Parametersatz sind Pflicht. Mit
        /// <paramref name="abbruch"/> endet die Ziehung der stochastischen Jahresreihe mit
        /// <see cref="OperationCanceledException"/> (der nebenläufige Lauf der Oberfläche, 5.1);
        /// der deterministische Weg zieht nichts und bleibt davon unberührt.
        /// </summary>
        internal static ZapfprofilErgebnis Rechnen(Zapfprofileingang e, IReadOnlyList<Nutzungsart> katalog,
                                                   CancellationToken abbruch = default)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            Zapfkalender.Pruefen(e.WochentagJan1, e.We);
            if (e.Projekt == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ProjektFehlt, "",
                    ZapfSatz.Neu("EINGABE_PROJEKTGROESSEN_FEHLEN"));
            if (e.Parameter == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ParameterFehlt, "",
                    ZapfSatz.Neu("EINGABE_PARAMETERSATZ_FEHLT"));

            var prot = new Herkunftsprotokoll();
            var hinweise = new List<ZapfHinweis>();
            if (e.Vorhinweise != null) hinweise.AddRange(e.Vorhinweise);
            var ablehnungen = new List<ZapfAblehnung>();
            IReadOnlyList<ZonenStand> zonen = e.Zonen ?? new ZonenStand[0];
            var arbeit = new List<Zonenarbeit>(zonen.Count);

            // --- 1. je Zone: Temperaturen, Mengengerüst, Zeitstruktur (S1, S2) -------------
            foreach (ZonenStand z in zonen)
            {
                var a = new Zonenarbeit { Stand = z, Name = z?.Name ?? "", Index = arbeit.Count };
                arbeit.Add(a);
                if (z == null)
                {
                    a.Abgelehnt = true;
                    ablehnungen.Add(new ZapfAblehnung("", ZapfEingabefehler.BezugsmengeFehlt, ZapfSatz.Neu("EINGABE_ZONE_OHNE_ANGABEN")));
                    continue;
                }
                try
                {
                    a.Art = Suchen(katalog, z.IdNutzungsart)
                            ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.NutzungsartFehlt, a.Name,
                                   ZapfSatz.Neu("EINGABE_NUTZUNGSART_FEHLT", a.Name));
                    Tagesgangsatz satz = a.Art.Tagesgaenge;
                    if (z.IdTagesgangsatz.HasValue)
                        satz = SuchenSatz(e.Tagesgangsaetze, z.IdTagesgangsatz.Value)
                               ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.TagesgangsatzFehlt, a.Name,
                                      ZapfSatz.Neu("EINGABE_TAGESGANGSATZ_FEHLT", z.IdTagesgangsatz.Value, a.Name));
                    a.Temperaturen = Mengengeruest.Temperaturen(z, a.Art, e.Parameter, prot);
                    a.Menge = Mengengeruest.JahresenergieKwh(z, a.Art, a.Temperaturen, e.Parameter,
                                                             e.BelegungJeRaumzahl, prot, hinweise);
                    a.TagesbedarfVorschlag = Mengengeruest.Vorschlag(z, a.Art, a.Temperaturen, e.Parameter, e.BelegungJeRaumzahl, a.Menge);
                    a.Tagesbedarf = Schaetzhilfe.Tagesbedarf(z.TagesbedarfAuto, z.TagesbedarfManuellKwh, a.TagesbedarfVorschlag,
                                                             a.Art.Bezug);
                    a.Struktur = Formvektor.Bilden(z, a.Art, satz, e.Parameter, prot, hinweise);
                    a.Kaltwasserfaktor = Kaltwassergang.Monatsfaktoren(a.Temperaturen, a.Name);
                    a.Kalender = Zapfkalender.Bilden(e.WochentagJan1, e.We, Zapfkalender.FensterDerZone(z));
                    a.Messwert = Mengengeruest.MesswertAus(z, a.Temperaturen, prot);
                    a.InZ1 = z.Zirkulation && a.Art.Grenze == ZapfBilanzgrenze.Zapfstelle;
                    a.ZapfungKwh = a.Menge.JahresenergieKwh;
                }
                catch (ZapfprofilEingabeException ex)
                {
                    Ablehnen(a, ex, ablehnungen);
                }
                catch (ParametersatzException ex)
                {
                    Ablehnen(a, ZapfAblehnung.Aus(a.Name, ex), ablehnungen);
                }
            }

            // --- 2. Zirkulation aus den Mengen vor der Kalibrierung (S5, 4.3) ---------------
            var anteile = new List<Zonenanteil>();
            var anteilIndex = new List<Zonenarbeit>();
            foreach (Zonenarbeit a in arbeit)
            {
                if (a.Abgelehnt) continue;
                anteile.Add(new Zonenanteil(a.Name, a.ZapfungKwh, a.InZ1, a.Menge.FlaecheM2));
                anteilIndex.Add(a);
            }
            Zirkulationsansatz ansatz = null;
            Schaetzhilfe zirkulationshilfe = null;
            try
            {
                ansatz = Zirkulationskanal.Ansetzen(e.Projekt, anteile, e.Parameter, prot, hinweise);
                for (int i = 0; i < anteilIndex.Count; i++) anteilIndex[i].ZirkulationKwh = ansatz.AnteilJeZoneKwh[i];
                zirkulationshilfe = Schaetzhilfe.Zirkulation(e.Projekt.ZirkAuto, e.Projekt.ZirkManuellKw, ansatz.LeistungKw,
                    e.Projekt.ZirkAuto ? ansatz : Zirkulationskanal.Vorschlag(e.Projekt, anteile, e.Parameter));
            }
            catch (ZapfprofilEingabeException ex)
            {
                ablehnungen.Add(ZapfAblehnung.Aus("", ex));
            }
            catch (ParametersatzException ex)
            {
                ablehnungen.Add(ZapfAblehnung.Aus("", ex));
            }

            // --- 3. Kalibrierung, dann Zapfreihen (4.1, 4.2) ------------------------------
            // Die Schranke der stochastischen Jahresreihe gilt für das Projekt, vor jeder Ziehung.
            if (e.Projekt.JahresreiheStochastisch) EinheitentagePruefen(arbeit, e);
            double? rueckfrage = e.Parameter.Enthaelt(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE)
                                 ? e.Parameter.Wert(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE) : (double?)null;
            if (!rueckfrage.HasValue && arbeit.Exists(a => !a.Abgelehnt && a.Messwert != null))
                ZapfHinweis.Einmal(hinweise, ZapfHinweis.ParameterFehlt(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE,
                    ZapfSatz.Neu("FOLGE_MESSWERT_NICHT_GEPRUEFT")));
            foreach (Zonenarbeit a in arbeit)
            {
                if (a.Abgelehnt) continue;
                try
                {
                    if (a.Messwert != null)
                    {
                        Kalibrierergebnis k = Mengengeruest.Kalibrieren(a.Messwert, a.ZapfungKwh, a.ZirkulationKwh, a.Name);
                        a.Kalibrierfaktor = k.Faktor;
                        a.ZapfungKwh = k.ZapfungKwh;
                        a.ZirkulationKwh = k.ZirkulationKwh;
                        // Die Schätzhilfe nach der Kalibrierung: angesetzt ist der Tagesbedarf des Messwerts.
                        a.Tagesbedarf = Schaetzhilfe.Tagesbedarf(a.Stand.TagesbedarfAuto, a.Stand.TagesbedarfManuellKwh,
                            a.TagesbedarfVorschlag, a.Art.Bezug, a.ZapfungKwh / Zapfkalender.TAGE, k.Faktor);
                        string herkunft = "Messwert " + (a.Messwert.Quelle ?? "") + " " + (a.Messwert.Zeitraum ?? "")
                                          + ", Grenze " + (int)a.Messwert.Grenze;
                        prot.Vermerken(a.Name, ZapfFeld.KALIBRIERFAKTOR, k.Faktor, "-", Wertstatus.Kalibriert, null,
                                       herkunft.Trim());
                        prot.Vermerken(a.Name, ZapfFeld.JAHRESENERGIE, a.ZapfungKwh, "kWh/a", Wertstatus.Kalibriert, null,
                                       "Faktor " + k.Faktor.ToString("0.######", CultureInfo.InvariantCulture));
                        if (a.Messwert.Grenze != ZapfBilanzgrenze.Zapfstelle)
                            prot.Vermerken(a.Name, ZapfFeld.ZIRKULATION_ZONENANTEIL, a.ZirkulationKwh, "kWh/a",
                                           Wertstatus.Kalibriert, null, "Faktor der Zone");
                        if (a.Messwert.Grenze == ZapfBilanzgrenze.MitSpeicher)
                            hinweise.Add(new ZapfHinweis(a.Name, "MESSWERT_SPEICHERVERLUST",
                                ZapfSatz.Neu("HINWEIS_MESSWERT_SPEICHERVERLUST", a.Name,
                                             a.Messwert.SpeicherverlustKwhJeJahr ?? 0.0)));
                        if (rueckfrage.HasValue && Math.Abs(k.Faktor - 1.0) > rueckfrage.Value)
                            hinweise.Add(new ZapfHinweis(a.Name, "MESSWERT_ABWEICHUNG",
                                ZapfSatz.Neu("HINWEIS_MESSWERT_ABWEICHUNG", a.Name, k.Faktor, rueckfrage.Value)) { Warnung = true });
                    }
                    // DIE WEICHE DES JAHRESGANGS (4.2, 5.3; Stufe Z4b): Ohne eingespielte Typtage
                    // rechnet der Formvektor wie im Bestand; mit ihnen trägt der Typtagweg die
                    // Tagesmengen (Jahreszeit, Tagart, Bewölkung) und - wenn das Paket Tagesgänge
                    // führt - auch die Tagesform. Kein stiller Rückfall: Was der Typtagweg nicht
                    // rechnen kann, lehnt er benannt ab. Auch die STOCHASTISCHE Jahresreihe zieht
                    // dann über diese Tagesmengen (Stochastisch, Nachbesserung Gruppe 1) - der
                    // Typtagweg wird nicht überschrieben.
                    double[] tage;
                    double[] stunden = null;
                    if (e.Typtage != null)
                    {
                        a.Typtagjahr = Typtagzuordnung.Zuordnen(e.Typtage, e.WochentagJan1, e.We, a.Name,
                                                               Zapfkalender.FensterDerZone(a.Stand), hinweise);
                        tage = Typtagzuordnung.Tagesmengen(a.ZapfungKwh,
                                   Typtagzuordnung.Einheiten(a.Art, a.Menge, a.Name), a.Typtagjahr, a.Name, hinweise);
                        stunden = Typtagzuordnung.Stundenreihe(tage, a.Typtagjahr, e.Typtage.Daten,
                                                              e.Typtage.Gebaeudeart, a.Name, hinweise);
                    }
                    else
                    {
                        tage = Formvektor.Tagesmengen(a.ZapfungKwh, a.Struktur, a.Kalender, e.WochentagJan1,
                                                      a.Kaltwasserfaktor, a.Name);
                    }
                    a.Tagesmengen = tage;
                    a.Zapfreihe = new Bilanzreihe(stunden ?? Formvektor.Stundenreihe(tage, a.Struktur, a.Kalender));
                    if (e.Projekt.JahresreiheStochastisch) Stochastisch(a, e, hinweise, abbruch);
                }
                catch (ZapfprofilEingabeException ex)
                {
                    Ablehnen(a, ex, ablehnungen);
                }
                catch (ParametersatzException ex)
                {
                    Ablehnen(a, ZapfAblehnung.Aus(a.Name, ex), ablehnungen);
                }
            }
            if (e.Projekt.JahresreiheStochastisch && arbeit.Exists(a => !a.Abgelehnt))
                hinweise.Add(new ZapfHinweis("", HINWEIS_STOCHASTISCH,
                    ZapfSatz.Neu("HINWEIS_JAHRESREIHE_STOCHASTISCH", e.Projekt.Seed, e.Projekt.Realisierungen)));

            // --- 4. Laufzeitfenster und Zirkulationsreihen (4.3) --------------------------
            var z1Reihen = new List<IReadOnlyList<double>>();
            var alleReihen = new List<IReadOnlyList<double>>();
            foreach (Zonenarbeit a in arbeit)
            {
                if (a.Abgelehnt) continue;
                // Das Laufzeitfenster folgt dem Formvektor — auch auf dem stochastischen Weg (4.3, 4.4).
                Bilanzreihe form = a.Deterministisch ?? a.Zapfreihe;
                alleReihen.Add(form.StundenKwh);
                if (a.InZ1) z1Reihen.Add(form.StundenKwh);
            }
            double[] fenster = null;
            Bilanzreihe rest = null;
            if (ansatz != null)
            {
                double mitte = Zirkulationskanal.Tagesmitte(z1Reihen.Count > 0 ? z1Reihen : alleReihen);
                fenster = Zirkulationskanal.Laufzeitfenster(ansatz.LaufzeitH, mitte);
                if (ansatz.RestKwh != 0.0) rest = Zirkulationskanal.Reihe(ansatz.RestKwh, ansatz.LaufzeitH, fenster);
            }

            var jeZone = new List<ZonenErgebnis>(arbeit.Count);
            var zapfreihen = new List<Bilanzreihe>();
            var zirkreihen = new List<Bilanzreihe>();
            foreach (Zonenarbeit a in arbeit)
            {
                if (a.Abgelehnt)
                {
                    jeZone.Add(new ZonenErgebnis
                    {
                        IdZone = a.Stand?.Id ?? 0, Zone = a.Name, Abgelehnt = true,
                        Zapfung = Bilanzreihe.Null(), Zirkulation = Bilanzreihe.Null(),
                        Kalender = a.Kalender != null ? Array.AsReadOnly(a.Kalender) : null
                    });
                    continue;
                }
                Bilanzreihe zirk = fenster != null && a.ZirkulationKwh != 0.0
                                   ? Zirkulationskanal.Reihe(a.ZirkulationKwh, ansatz.LaufzeitH, fenster)
                                   : Bilanzreihe.Null();
                zapfreihen.Add(a.Zapfreihe);
                zirkreihen.Add(zirk);
                jeZone.Add(new ZonenErgebnis
                {
                    IdZone = a.Stand.Id,
                    Zone = a.Name,
                    Zapfung = a.Zapfreihe,
                    Zirkulation = zirk,
                    Bezugsmenge = a.Menge.Bezugsmenge,
                    JahresbedarfZapfungKwh = a.Zapfreihe.JahressummeKwh,
                    JahresverlustZirkulationKwh = zirk.JahressummeKwh,
                    SpezifischKwhJeEinheitJahr = a.Zapfreihe.JahressummeKwh / a.Menge.Bezugsmenge,
                    ZapfungLiterJeTag = Liter(a.Zapfreihe.JahressummeKwh, a.Temperaturen, e.AnzeigetemperaturC, a.Name, hinweise),
                    Temperaturfaktor = a.Menge.Temperaturfaktor,
                    Kalibrierfaktor = a.Kalibrierfaktor,
                    Kalender = Array.AsReadOnly(a.Kalender),
                    Konsistenz = a.Konsistenz,
                    SchaetzhilfeTagesbedarf = a.Tagesbedarf,
                    Auslastung = Zapfauswertung.Auslastung(a.Zapfreihe, e.WochentagJan1),
                    Auslastungsgang = Formvektor.Auslastungsgang(a.Stand, a.Art)
                });
            }
            if (rest != null) zirkreihen.Add(rest);

            Bilanzreihe zapfung = Bilanzreihe.Summe(zapfreihen);
            Bilanzreihe zirkulation = Bilanzreihe.Summe(zirkreihen);
            double? schwelle = Schwelle(e.SchwelleKw, hinweise);

            // --- 5. ZU5: Netzverluste und Zirkulation zugleich (Konzept 9, Risiko 8) ---------
            // Die Netzverlustverteilung bleibt unberührt; der Hinweis nennt nur, dass beide
            // gesetzt sind und derselbe Verlust doppelt zählen kann. Nicht blockierend.
            if (e.NetzverlusteProjekt > 0.0 && zirkulation.JahressummeKwh > 0.0
                && arbeit.Exists(a => !a.Abgelehnt && a.Stand.Zirkulation))
                hinweise.Add(new ZapfHinweis("", HINWEIS_NETZVERLUST,
                    ZapfSatz.Neu("HINWEIS_NETZVERLUST_UND_ZIRKULATION", zirkulation.JahressummeKwh)) { Warnung = true });

            // --- 6. Warnlogik (Z4): Zirkulation groß gegenüber der Zapfung ----------------
            // Lehre 3 des Mockups: In kleinen Mehrfamilienhäusern verliert die Zirkulation mehr, als
            // gezapft wird — das ist üblich, keine Warnung. Ein HINWEIS nennt es erst über dem
            // Verhältnis des Katalogs (Parameter Zapfprofil.Zirkulation.Hinweisverhaeltnis); ohne
            // gültigen Parameter kein Hinweis. Die Rechnung ändert er nie.
            double? verhaeltnis = Hinweisverhaeltnis(e.Parameter);
            if (verhaeltnis.HasValue && zapfung.JahressummeKwh > 0.0
                && zirkulation.JahressummeKwh > verhaeltnis.Value * zapfung.JahressummeKwh)
                hinweise.Add(new ZapfHinweis("", HINWEIS_ZIRKULATION_GROSS,
                    ZapfSatz.Neu("HINWEIS_ZIRKULATION_GROSS", zirkulation.JahressummeKwh,
                                 zirkulation.JahressummeKwh / zapfung.JahressummeKwh, zapfung.JahressummeKwh, verhaeltnis.Value)));

            return new ZapfprofilErgebnis
            {
                Stochastisch = e.Projekt.JahresreiheStochastisch,
                Zapfung = zapfung,
                Zirkulation = zirkulation,
                JeZone = jeZone.AsReadOnly(),
                Zirkulationsansatz = ansatz,
                Laufzeitfenster = fenster != null ? Array.AsReadOnly(fenster) : null,
                Kennzahlen = Kennzahlen(zapfung, zirkulation, jeZone, schwelle),
                Dauerlinie = Zapfauswertung.Dauerlinie(zapfung, zirkulation, schwelle),
                SchaetzhilfeZirkulation = zirkulationshilfe,
                Herkunft = prot.Abschrift(),
                Hinweise = hinweise.AsReadOnly(),
                Ablehnungen = ablehnungen.AsReadOnly()
            };
        }

        /// <summary>Die Kennzahlen der Bilanz (4.6) aus den Summenreihen und den Zonen.</summary>
        internal static Zapfkennzahlen Kennzahlen(Bilanzreihe zapfung, Bilanzreihe zirkulation,
                                                  IReadOnlyList<ZonenErgebnis> zonen, double? schwelleKw)
        {
            double zapf = zapfung.JahressummeKwh;
            double zirk = zirkulation.JahressummeKwh;
            double gesamt = zapf + zirk;
            var summe = new double[Bilanzreihe.STUNDEN];
            IReadOnlyList<double> a = zapfung.StundenKwh, b = zirkulation.StundenKwh;
            for (int h = 0; h < Bilanzreihe.STUNDEN; h++) summe[h] = a[h] + b[h];
            var gesamtreihe = new Bilanzreihe(summe);

            double? liter = null;
            bool alleMitLiter = zonen.Count > 0;
            double l = 0.0;
            foreach (ZonenErgebnis z in zonen)
            {
                if (z.Abgelehnt) continue;
                if (z.ZapfungLiterJeTag.HasValue) l += z.ZapfungLiterJeTag.Value;
                else alleMitLiter = false;
            }
            if (alleMitLiter) liter = l;

            double max = gesamtreihe.GroessterStundenwertKw;
            return new Zapfkennzahlen
            {
                JahresbedarfZapfungKwh = zapf,
                JahresverlustZirkulationKwh = zirk,
                JahresbedarfGesamtKwh = gesamt,
                Zirkulationsanteil = gesamt > 0 ? zirk / gesamt : 0.0,
                TagesmittelZapfungKwh = zapf / Zapfkalender.TAGE,
                ZapfungLiterJeTag = liter,
                GroessterStundenwertKw = max,
                VolllaststundenH = max > 0 ? gesamt / max : 0.0,
                StundenUeberSchwelle = schwelleKw.HasValue ? gesamtreihe.StundenUeber(schwelleKw.Value) : (int?)null,
                SchwelleKw = schwelleKw
            };
        }

        // =================================================================================
        // Rechenweg „stochastisch" (4.4)
        // =================================================================================

        /// <summary>Kennung: Die Jahresreihe ist stochastisch (Realisierungen und Seed als Werte).</summary>
        internal const string HINWEIS_STOCHASTISCH = "JAHRESREIHE_STOCHASTISCH";

        /// <summary>Kennung: Das Ensemblemittel lag außerhalb der Toleranz der Konsistenzprobe (4.4).</summary>
        internal const string HINWEIS_ENERGIEPROBE = "STOCHASTIK_ENERGIEPROBE";

        /// <summary>
        /// Kennung: Das Projekt trägt Netzverluste, und das Zapfprofil rechnet eine Zirkulation (ZU5,
        /// Konzept 9, Risiko 8) — ein Eintrag der Warnliste des Zapfprofils (N9 (g)).
        /// </summary>
        internal const string HINWEIS_NETZVERLUST = "NETZVERLUST_UND_ZIRKULATION";

        /// <summary>Kennung des Hinweises: θ_Anzeige liegt nicht über dem Kaltwassermittel einer Zone — keine Literanzeige.</summary>
        internal const string HINWEIS_ANZEIGETEMPERATUR = "ANZEIGETEMPERATUR_UNTER_KALTWASSER";

        /// <summary>Kennung des Hinweises: Die Schwelle der Stundenzählung ist negativ oder nicht endlich — keine Zählung.</summary>
        internal const string HINWEIS_STUNDENSCHWELLE = "STUNDENSCHWELLE_UNGUELTIG";

        /// <summary>
        /// Kennung des Hinweises: Die Zirkulation verliert im Jahr mehr als das Verhältnis des Katalogs
        /// mal die Zapfung (Warnlogik Z4, <see cref="ZapfParameter.ZIRKULATION_HINWEISVERHAELTNIS"/>).
        /// </summary>
        internal const string HINWEIS_ZIRKULATION_GROSS = "ZIRKULATION_GROSS";

        /// <summary>
        /// Das Hinweisverhältnis der Zirkulation aus dem Katalog; <c>null</c> ohne Parameter oder bei
        /// einem Wert, der nicht endlich und positiv ist.
        /// </summary>
        private static double? Hinweisverhaeltnis(Parametersatz ps)
        {
            if (ps == null || !ps.Enthaelt(ZapfParameter.ZIRKULATION_HINWEISVERHAELTNIS)) return null;
            double v = ps.Wert(ZapfParameter.ZIRKULATION_HINWEISVERHAELTNIS);
            return double.IsNaN(v) || double.IsInfinity(v) || v <= 0 ? (double?)null : v;
        }

        /// <summary>
        /// Kennung der Ablehnung „zu viele Einheitentage" der Jahresreihe
        /// (<see cref="ZapfEingabefehler.StochastikUngueltig"/>); Argumente: die Einheitentage
        /// R · Σ n_E · 365 und die Grenze <see cref="Zapfensemble.HOECHSTENS_EINHEITSTAGE"/>.
        /// </summary>
        internal const string KENNUNG_EINHEITSTAGE = "EINGABE_STOCHASTIK_EINHEITSTAGE";

        /// <summary>
        /// <b>Die Schranke der Jahresreihe</b> (4.4): Die R Jahre ziehen je Zone n_E Einheiten an 365
        /// Tagen; die Laufzeit wächst mit <c>R · Σ n_E · 365</c>. Liegt das über
        /// <see cref="Zapfensemble.HOECHSTENS_EINHEITSTAGE"/> — derselben Grenze wie beim
        /// Auslegungsensemble —, lehnt das Projekt benannt ab, bevor ein Jahr gezogen ist
        /// (<see cref="ZapfEingabefehler.StochastikUngueltig"/> mit <see cref="KENNUNG_EINHEITSTAGE"/>
        /// und den Werten). Eine Zone, deren Einheiten nicht bestimmbar sind, zählt nicht mit — sie
        /// lehnt im Rechenweg selbst benannt ab.
        /// </summary>
        private static void EinheitentagePruefen(List<Zonenarbeit> arbeit, Zapfprofileingang e)
        {
            long einheiten = 0;
            foreach (Zonenarbeit a in arbeit)
            {
                if (a.Abgelehnt) continue;
                try { einheiten += Zapfeinheiten.Anzahl(a.Stand, a.Art, a.Menge.Bezugsmenge, e.Parameter); }
                catch (ZapfprofilEingabeException) { /* die Zone lehnt im Rechenweg selbst ab */ }
                catch (ParametersatzException) { /* ebenso */ }
            }
            long tage = (long)Math.Max(0, e.Projekt.Realisierungen) * einheiten * Zapfkalender.TAGE;
            if (tage <= Zapfensemble.HOECHSTENS_EINHEITSTAGE) return;
            throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, "",
                ZapfSatz.Neu(KENNUNG_EINHEITSTAGE, tage, (long)Zapfensemble.HOECHSTENS_EINHEITSTAGE));
        }

        /// <summary>
        /// Ersetzt die Zapfreihe der Zone durch die Realisierung zum Seed des Jahresensembles, mit dem
        /// Faktor der Energieprobe auf die Jahresmenge gebracht (<see cref="Jahresensemble.Bilanz"/>);
        /// die R Jahre prüfen nur die Konsistenz. Die deterministische Reihe bleibt für das
        /// Laufzeitfenster und die Probe. Die Urlaube werden bei Kalenderart Wohnen je Einheit
        /// versetzt, wenn die Zone Ferien trägt (Parameter <see cref="ZapfStochastikParameter.URLAUBSVERSATZ"/>).
        ///
        /// <para><b>Auf dem Typtagweg zieht das Ensemble über die Tagesmengen der Typtage</b>
        /// (Stufe Z4b, Nachbesserung Gruppe 1): Der Zapfereignisgenerator bekommt je Tag die
        /// Tagesmenge des Typtagjahres statt der des Formvektors, und führt das Paket Tagesgänge,
        /// auch die Tagesform des Typtags. So rechnen Typtagweg und Stochastik zusammen: Die
        /// Energieprobe hält die gezogene Reihe gegen die Typtagreihe, nicht gegen den Formvektor.
        /// Die Entkopplung der Urlaube entfällt dort — auf dem Typtagweg wirkt kein Ferienfenster
        /// (<see cref="Typtagzuordnung.HINWEIS_FERIEN"/>).</para>
        /// </summary>
        private static void Stochastisch(Zonenarbeit a, Zapfprofileingang e, ICollection<ZapfHinweis> hinweise,
                                         CancellationToken abbruch)
        {
            IReadOnlyList<Ferienfenster> ferien = Zapfkalender.FensterDerZone(a.Stand);
            bool entkoppeln = a.Art.Kalender == ZapfKalenderart.Wohnen && ferien.Count > 0 && a.Typtagjahr == null;
            int versatz = 0;
            if (entkoppeln)
            {
                double v = e.Parameter.Wert(ZapfStochastikParameter.URLAUBSVERSATZ);
                if (double.IsNaN(v) || v < 0 || v >= Zapfkalender.TAGE || v != Math.Floor(v))
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, a.Name,
                        ZapfSatz.Neu("EINGABE_URLAUBSVERSATZ_UNGUELTIG"));
                versatz = (int)v;
            }
            double[] kaltwasser = Kaltwassergang.Monatswerte(a.Temperaturen.KaltwasserMittelC, a.Temperaturen.KaltwasserAmplitudeK,
                                                             a.Temperaturen.KaltwasserMonatMaximum);
            var spreizung = new double[Zapfkalender.MONATE];
            for (int m = 0; m < Zapfkalender.MONATE; m++) spreizung[m] = a.Temperaturen.ZapfC - kaltwasser[m];

            var zone = new Jahreszone
            {
                Index = a.Index, Zone = a.Name,
                Einheiten = Zapfeinheiten.Anzahl(a.Stand, a.Art, a.Menge.Bezugsmenge, e.Parameter),
                Kategorien = Zapfkategoriensatz.Aus(e.Zapfkategorien, a.Art, a.Name),
                JahresmengeKwh = a.ZapfungKwh, Struktur = a.Struktur, Kalender = a.Kalender, Ferien = ferien,
                Kaltwasserfaktor = a.Kaltwasserfaktor, SpreizungJeMonatK = spreizung,
                WochentagJan1 = e.WochentagJan1, We = e.We, Urlaubsentkopplung = entkoppeln, UrlaubsversatzTage = versatz,
                TyptagmengenKwh = a.Typtagjahr != null ? a.Tagesmengen : null,
                TyptagdichteJeTag = a.Typtagjahr != null
                                    ? Typtagzuordnung.Dichten(a.Typtagjahr, e.Typtage.Daten, e.Typtage.Gebaeudeart) : null
            };
            Jahresensemble ensemble = Jahresensemble.Ziehen(zone, e.Projekt.Seed, e.Projekt.Realisierungen, abbruch: abbruch);
            Jahreskonsistenz k = ensemble.Pruefen(a.Zapfreihe);
            Bilanzreihe bilanz = ensemble.Bilanz(k);
            if (!k.Erfuellt)
                hinweise.Add(new ZapfHinweis(a.Name, HINWEIS_ENERGIEPROBE,
                    ZapfSatz.Neu("HINWEIS_STOCHASTIK_ENERGIEPROBE", a.Name, k.MittelKwh, k.ToleranzKwh, k.DeterministischKwh))
                    { Warnung = true });
            a.Deterministisch = a.Zapfreihe;
            a.Konsistenz = k;
            a.Zapfreihe = bilanz;
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Die Literanzeige einer Zone bei θ_Anzeige (4.6); ohne θ_Anzeige keine. Liegt θ_Anzeige nicht
        /// über dem Kaltwassermittel der Zone, ist die Umrechnung nicht definiert: benannt abgewiesen
        /// (Hinweis <see cref="HINWEIS_ANZEIGETEMPERATUR"/>), die Zone zeigt keine Liter.
        /// </summary>
        private static double? Liter(double jahresKwh, Zonentemperaturen t, double? anzeigeC, string zone,
                                     ICollection<ZapfHinweis> hinweise)
        {
            if (!anzeigeC.HasValue) return null;
            double delta = anzeigeC.Value - t.KaltwasserMittelC;
            if (!(delta > 0))
            {
                hinweise.Add(new ZapfHinweis(zone, HINWEIS_ANZEIGETEMPERATUR,
                    ZapfSatz.Neu("HINWEIS_ANZEIGETEMPERATUR_UNTER_KALTWASSER", zone, anzeigeC.Value, t.KaltwasserMittelC)));
                return null;
            }
            return Mengengeruest.VolumenL(jahresKwh / Zapfkalender.TAGE, delta, zone);
        }

        /// <summary>
        /// Die Schwelle der Stundenzählung, geprüft: eine negative oder nicht endliche Schwelle ist
        /// benannt abgewiesen (Hinweis <see cref="HINWEIS_STUNDENSCHWELLE"/>) und zählt nicht.
        /// </summary>
        private static double? Schwelle(double? schwelleKw, ICollection<ZapfHinweis> hinweise)
        {
            if (!schwelleKw.HasValue) return null;
            double s = schwelleKw.Value;
            if (!double.IsNaN(s) && !double.IsInfinity(s) && s >= 0) return s;
            hinweise.Add(new ZapfHinweis("", HINWEIS_STUNDENSCHWELLE, ZapfSatz.Neu("HINWEIS_STUNDENSCHWELLE_UNGUELTIG", s)));
            return null;
        }

        /// <summary>Die Ablehnung aus der benannten Ausnahme — samt Kennung und Wert (etwa der Nutzungsart).</summary>
        private static void Ablehnen(Zonenarbeit a, ZapfprofilEingabeException ex, List<ZapfAblehnung> liste)
            => Ablehnen(a, ZapfAblehnung.Aus(a.Name, ex), liste);

        private static void Ablehnen(Zonenarbeit a, ZapfAblehnung ablehnung, List<ZapfAblehnung> liste)
        {
            a.Abgelehnt = true;
            a.ZapfungKwh = 0.0;
            a.ZirkulationKwh = 0.0;
            liste.Add(ablehnung);
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
