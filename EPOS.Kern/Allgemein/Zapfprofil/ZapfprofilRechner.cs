using System;
using System.Collections.Generic;
using System.Globalization;

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
        }

        /// <summary>Rechnet die Bilanz. Kalender, Projektgrößen und Parametersatz sind Pflicht.</summary>
        internal static ZapfprofilErgebnis Rechnen(Zapfprofileingang e, IReadOnlyList<Nutzungsart> katalog)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            Zapfkalender.Pruefen(e.WochentagJan1, e.We);
            if (e.Projekt == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ProjektFehlt, "",
                    "Nicht rechenbar — die Projektgrößen des Zapfprofils fehlen.");
            if (e.Parameter == null)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.ParameterFehlt, "",
                    "Nicht rechenbar — der Parametersatz des Zapfprofils fehlt.");

            var prot = new Herkunftsprotokoll();
            var hinweise = new List<ZapfHinweis>();
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
                    ablehnungen.Add(new ZapfAblehnung("", ZapfEingabefehler.BezugsmengeFehlt, "Eine Zone ohne Angaben."));
                    continue;
                }
                try
                {
                    a.Art = Suchen(katalog, z.IdNutzungsart)
                            ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.NutzungsartFehlt, a.Name,
                                   "Nicht rechenbar — die Nutzungsart " + z.IdNutzungsart + " der Zone „" + a.Name
                                   + "“ steht nicht im Katalog.");
                    Tagesgangsatz satz = a.Art.Tagesgaenge;
                    if (z.IdTagesgangsatz.HasValue)
                        satz = SuchenSatz(e.Tagesgangsaetze, z.IdTagesgangsatz.Value)
                               ?? throw new ZapfprofilEingabeException(ZapfEingabefehler.TagesgangsatzFehlt, a.Name,
                                      "Nicht rechenbar — der Tagesgangsatz " + z.IdTagesgangsatz.Value + " der Zone „"
                                      + a.Name + "“ fehlt.");
                    a.Temperaturen = Mengengeruest.Temperaturen(z, a.Art, e.Parameter, prot);
                    a.Menge = Mengengeruest.JahresenergieKwh(z, a.Art, a.Temperaturen, e.Parameter,
                                                             e.BelegungJeRaumzahl, prot, hinweise);
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
                    Ablehnen(a, ZapfEingabefehler.ParameterFehlt, ex.Message, ablehnungen);
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
            try
            {
                ansatz = Zirkulationskanal.Ansetzen(e.Projekt, anteile, e.Parameter, prot, hinweise);
                for (int i = 0; i < anteilIndex.Count; i++) anteilIndex[i].ZirkulationKwh = ansatz.AnteilJeZoneKwh[i];
            }
            catch (ZapfprofilEingabeException ex)
            {
                ablehnungen.Add(new ZapfAblehnung("", ex.Fehler, ex.Message));
            }
            catch (ParametersatzException ex)
            {
                ablehnungen.Add(new ZapfAblehnung("", ZapfEingabefehler.ParameterFehlt, ex.Message));
            }

            // --- 3. Kalibrierung, dann Zapfreihen (4.1, 4.2) ------------------------------
            double? rueckfrage = e.Parameter.Enthaelt(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE)
                                 ? e.Parameter.Wert(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE) : (double?)null;
            if (!rueckfrage.HasValue && arbeit.Exists(a => !a.Abgelehnt && a.Messwert != null))
                ZapfHinweis.Einmal(hinweise, ZapfHinweis.ParameterFehlt(ZapfParameter.MESSWERT_RUECKFRAGESCHWELLE,
                    "Die Abweichung der Messwerte vom Katalogwert wird nicht geprüft."));
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
                                "Vom Messwert der Zone „" + a.Name + "“ ist der Speicherverlust abgezogen."));
                        if (rueckfrage.HasValue && Math.Abs(k.Faktor - 1.0) > rueckfrage.Value)
                            hinweise.Add(new ZapfHinweis(a.Name, "MESSWERT_ABWEICHUNG",
                                "Der Messwert der Zone „" + a.Name + "“ weicht um mehr als die Rückfrageschwelle vom Katalogwert ab."));
                    }
                    double[] tage = Formvektor.Tagesmengen(a.ZapfungKwh, a.Struktur, a.Kalender, e.WochentagJan1,
                                                           a.Kaltwasserfaktor, a.Name);
                    a.Zapfreihe = new Bilanzreihe(Formvektor.Stundenreihe(tage, a.Struktur, a.Kalender));
                    if (e.Projekt.JahresreiheStochastisch) Stochastisch(a, e, hinweise);
                }
                catch (ZapfprofilEingabeException ex)
                {
                    Ablehnen(a, ex, ablehnungen);
                }
                catch (ParametersatzException ex)
                {
                    Ablehnen(a, ZapfEingabefehler.ParameterFehlt, ex.Message, ablehnungen);
                }
            }
            if (e.Projekt.JahresreiheStochastisch && arbeit.Exists(a => !a.Abgelehnt))
                hinweise.Add(new ZapfHinweis("", "JAHRESREIHE_STOCHASTISCH",
                    "Die Jahresreihe der Zapfung ist stochastisch: je Zone das gezogene Jahr zum Seed "
                    + e.Projekt.Seed.ToString(CultureInfo.InvariantCulture) + ", auf die Jahresmenge des Mengengerüsts gebracht; "
                    + "die Konsistenzprobe prüft es an " + e.Projekt.Realisierungen.ToString(CultureInfo.InvariantCulture)
                    + " gezogenen Jahren."));

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
                    ZapfungLiterJeTag = Liter(a.Zapfreihe.JahressummeKwh, a.Temperaturen, e.AnzeigetemperaturC, a.Name),
                    Temperaturfaktor = a.Menge.Temperaturfaktor,
                    Kalibrierfaktor = a.Kalibrierfaktor,
                    Kalender = Array.AsReadOnly(a.Kalender),
                    Konsistenz = a.Konsistenz
                });
            }
            if (rest != null) zirkreihen.Add(rest);

            Bilanzreihe zapfung = Bilanzreihe.Summe(zapfreihen);
            Bilanzreihe zirkulation = Bilanzreihe.Summe(zirkreihen);

            // --- 5. ZU5: Netzverluste und Zirkulation zugleich (Konzept 9, Risiko 8) ---------
            // Die Netzverlustverteilung bleibt unberührt; der Hinweis nennt nur, dass beide
            // gesetzt sind und derselbe Verlust doppelt zählen kann. Nicht blockierend.
            if (e.NetzverlusteProjekt > 0.0 && zirkulation.JahressummeKwh > 0.0
                && arbeit.Exists(a => !a.Abgelehnt && a.Stand.Zirkulation))
                hinweise.Add(new ZapfHinweis("", "NETZVERLUST_UND_ZIRKULATION",
                    "Das Projekt trägt Netzverluste, und das Zapfprofil rechnet eine Zirkulation — " +
                    "sind die Verluste der Zirkulation in den Netzverlusten enthalten, zählen sie doppelt."));

            return new ZapfprofilErgebnis
            {
                Stochastisch = e.Projekt.JahresreiheStochastisch,
                Zapfung = zapfung,
                Zirkulation = zirkulation,
                JeZone = jeZone.AsReadOnly(),
                Zirkulationsansatz = ansatz,
                Laufzeitfenster = fenster != null ? Array.AsReadOnly(fenster) : null,
                Kennzahlen = Kennzahlen(zapfung, zirkulation, jeZone, e.SchwelleKw),
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

        /// <summary>Kennung: Die Jahresreihe ist stochastisch (Realisierungen und Seed im Text).</summary>
        internal const string HINWEIS_STOCHASTISCH = "JAHRESREIHE_STOCHASTISCH";

        /// <summary>Kennung: Das Ensemblemittel lag außerhalb der Toleranz der Konsistenzprobe (4.4).</summary>
        internal const string HINWEIS_ENERGIEPROBE = "STOCHASTIK_ENERGIEPROBE";

        /// <summary>
        /// Ersetzt die Zapfreihe der Zone durch die Realisierung zum Seed des Jahresensembles, mit dem
        /// Faktor der Energieprobe auf die Jahresmenge gebracht (<see cref="Jahresensemble.Bilanz"/>);
        /// die R Jahre prüfen nur die Konsistenz. Die deterministische Reihe bleibt für das
        /// Laufzeitfenster und die Probe. Die Urlaube werden bei Kalenderart Wohnen je Einheit
        /// versetzt, wenn die Zone Ferien trägt (Parameter <see cref="ZapfStochastikParameter.URLAUBSVERSATZ"/>).
        /// </summary>
        private static void Stochastisch(Zonenarbeit a, Zapfprofileingang e, ICollection<ZapfHinweis> hinweise)
        {
            IReadOnlyList<Ferienfenster> ferien = Zapfkalender.FensterDerZone(a.Stand);
            bool entkoppeln = a.Art.Kalender == ZapfKalenderart.Wohnen && ferien.Count > 0;
            int versatz = 0;
            if (entkoppeln)
            {
                double v = e.Parameter.Wert(ZapfStochastikParameter.URLAUBSVERSATZ);
                if (double.IsNaN(v) || v < 0 || v >= Zapfkalender.TAGE || v != Math.Floor(v))
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, a.Name,
                        "Nicht rechenbar — der Urlaubsversatz ist keine ganze Zahl von 0 bis 364 Tagen.");
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
                WochentagJan1 = e.WochentagJan1, We = e.We, Urlaubsentkopplung = entkoppeln, UrlaubsversatzTage = versatz
            };
            Jahresensemble ensemble = Jahresensemble.Ziehen(zone, e.Projekt.Seed, e.Projekt.Realisierungen);
            Jahreskonsistenz k = ensemble.Pruefen(a.Zapfreihe);
            Bilanzreihe bilanz = ensemble.Bilanz(k);
            if (!k.Erfuellt)
                hinweise.Add(new ZapfHinweis(a.Name, "STOCHASTIK_ENERGIEPROBE",
                    "Zone „" + a.Name + "“: Das Mittel der gezogenen Jahre (" + k.MittelKwh.ToString("0.#", CultureInfo.InvariantCulture)
                    + " kWh) weicht um mehr als die Toleranz " + k.ToleranzKwh.ToString("0.#", CultureInfo.InvariantCulture)
                    + " kWh von der Jahresmenge " + k.DeterministischKwh.ToString("0.#", CultureInfo.InvariantCulture)
                    + " kWh ab; das Jahr zum Seed ist auf die Jahresmenge gebracht."));
            a.Deterministisch = a.Zapfreihe;
            a.Konsistenz = k;
            a.Zapfreihe = bilanz;
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static double? Liter(double jahresKwh, Zonentemperaturen t, double? anzeigeC, string zone)
        {
            if (!anzeigeC.HasValue) return null;
            double delta = anzeigeC.Value - t.KaltwasserMittelC;
            if (!(delta > 0)) return null;
            return Mengengeruest.VolumenL(jahresKwh / Zapfkalender.TAGE, delta, zone);
        }

        private static void Ablehnen(Zonenarbeit a, ZapfEingabefehler grund, string text, List<ZapfAblehnung> liste)
            => Ablehnen(a, new ZapfAblehnung(a.Name, grund, text), liste);

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
