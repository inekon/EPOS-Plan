using System;
using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Einmalige, vor dem Hintergrundlauf ausgefuehrte Aufloesung der Datenquellen
    /// einer Speicherauslegung.
    /// </summary>
    public static partial class SpeicherAuslegungCtrl
    {
        private const int ViertelstundenNormaljahr = 35040;
        private const int ViertelstundenSchaltjahr = 35136;
        private static readonly TimeSpan Viertelstunde = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Friert Zeitreihen, Kosten und Speicherparameter fuer genau einen Lauf ein.
        /// Die Rueckgabe traegt den unabhaengigen Eingabesnapshot in
        /// <see cref="StromspeicherOptimierungVorbereitung.Eingaben"/>.
        /// </summary>
        public static StromspeicherOptimierungVorbereitung Vorbereiten(
            SimulationControl sim, int projektId, SpeicherOptimierungEingaben eingaben,
            KostenPflicht kostenPflicht = KostenPflicht.Studienlauf)
        {
            ArgumentNullException.ThrowIfNull(eingaben);
            if (projektId <= 0) throw new ArgumentOutOfRangeException(nameof(projektId));

            SpeicherOptimierungEingaben snapshot = eingaben.Kopie();
            if (snapshot.Auslegung == null)
                throw new ArgumentException("Die Quellen- und Kostenkonfiguration fehlt.", nameof(eingaben));
            PruefeQuellen(snapshot.Auslegung);

            bool brauchtEpos = snapshot.Auslegung.Lastquelle == SpeicherAuslegungQuelle.Epos ||
                snapshot.Auslegung.PvQuelle == SpeicherAuslegungQuelle.Epos ||
                snapshot.Auslegung.Preisquelle == SpeicherAuslegungQuelle.Epos;
            if (brauchtEpos && sim == null)
                throw new ArgumentNullException(nameof(sim),
                    "Eine EPOS-Zeitreihe setzt einen abgeschlossenen Simulationslauf voraus.");

            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            StromspeicherOptimierungVorbereitung epos = null;
            SpeicherParameter basis;
            StromspeicherLaufKontext kontext;
            bool hatFlotte = snapshot.Auslegung.Flotte?.Einheiten?.Count > 0;
            if (brauchtEpos)
            {
                epos = hatFlotte
                    ? ctrl.BereiteProjektflotteVor(sim, projektId, snapshot.Auslegung.Flotte)
                    : ctrl.BereiteOptimierungVor(sim, projektId);
                if (epos == null) return null;
                basis = epos.Basis;
                kontext = epos.Kontext;
            }
            else
            {
                if (hatFlotte)
                {
                    StromspeicherOptimierungVorbereitung flotte =
                        ctrl.BereiteProjektflotteVor(null, projektId, snapshot.Auslegung.Flotte);
                    basis = flotte.Basis;
                    kontext = flotte.Kontext;
                }
                else
                {
                    basis = ctrl.LeseParameter(projektId);
                    if (basis == null) return null;
                    kontext = ctrl.LetzterKontext;
                }
            }

            bool brauchtModulkosten =
                snapshot.Auslegung.Investitionsquelle == SpeicherKostenQuelle.Kostenmodul ||
                snapshot.Auslegung.Betriebsquelle == SpeicherKostenQuelle.Kostenmodul;
            SpeicherKostensaetze modulkosten = brauchtModulkosten
                ? (snapshot.Auslegung.FlotteImProjektAktiv
                    // Der übernommene Projektflottenstand trägt die bei der Freigabe
                    // eingefrorenen Modulkosten. Er darf weder eine inzwischen aktive
                    // Einzelanlage benötigen noch seine Kosten bei jedem Projektlauf
                    // unbemerkt aus deren Kostendialog neu lesen.
                    //
                    // BEFUND #185: Fehlen sie im Stand, ist das im STUDIENLAUF weiterhin
                    // ein Fehler. Im PROJEKTLAUF darf es keiner sein — dort entscheidet
                    // erst KostenAufloesen, ob die Sätze überhaupt gebraucht werden.
                    ? snapshot.Auslegung.VerwendeteKosten == null
                        ? (kostenPflicht == KostenPflicht.Projektlauf
                            ? new SpeicherKostensaetze()
                            : throw new InvalidOperationException(
                                "Im übernommenen Projektflottenstand fehlen die aufgelösten Kosten."))
                        : KopiereKosten(snapshot.Auslegung.VerwendeteKosten)
                    : Modulkosten(projektId, Anlage(projektId)))
                : null;

            double profilAufschlagCtKwh = 0.0;
            if (snapshot.Auslegung.Preisquelle == SpeicherAuslegungQuelle.Preisprofil)
            {
                StromAufschlagModel m = new StromAufschlagCtrl().ReadStrom(projektId);
                profilAufschlagCtKwh = StromAufschlagCtrl.AlsAufschlagssatz(m).WirksamCtKwh;
            }

            StromVerguetungsErgebnis projektVerguetung = null;
            int dateiIntervalle = snapshot.Auslegung.LastDatei?.Werte?.Length ?? 0;
            if (!brauchtEpos && dateiIntervalle > 0)
                projektVerguetung = new StromPreisCtrl().BaueVerguetungen(
                    projektId, dateiIntervalle);

            return AusQuellenVorbereiten(epos, basis, kontext, snapshot, modulkosten,
                profilAufschlagCtKwh, projektVerguetung?.PvCtKwh,
                projektVerguetung?.BhkwCtKwh, kostenPflicht);
        }

        /// <summary>
        /// Löst die Kostensätze EINES gespeicherten Standes auf, ohne eine einzige
        /// Zeitreihe zu beschaffen (Befund #185).
        /// </summary>
        /// <remarks>
        /// Gebraucht wird sie von <see cref="SpeicherFlottenProjektCtrl.Aktivieren"/> und
        /// von dessen Vorprüfung: Beide müssen wissen, ob der Stand vollständig ist —
        /// aber keiner von beiden rechnet dabei. Bereits aufgelöste und brauchbare Sätze
        /// bleiben unverändert eingefroren; nur ein LÜCKENHAFTER Stand wird aus
        /// Dialogsätzen bzw. Kostenmodul nachgezogen.
        /// </remarks>
        internal static SpeicherKostensaetze StandKosten(
            int projektId, SpeicherAuslegungKonfiguration a)
        {
            ArgumentNullException.ThrowIfNull(a);
            bool gebraucht = SpezifischeSaetzeGebraucht(a);
            SpeicherKostensaetze vorhanden = a.VerwendeteKosten;
            if (vorhanden != null && !vorhanden.NichtBewertbar &&
                (!gebraucht || (vorhanden.InvestVorhanden && vorhanden.BetriebVorhanden)))
                return KopiereKosten(vorhanden);

            bool brauchtModulkosten =
                a.Investitionsquelle == SpeicherKostenQuelle.Kostenmodul ||
                a.Betriebsquelle == SpeicherKostenQuelle.Kostenmodul;
            SpeicherKostensaetze modul = brauchtModulkosten
                ? Modulkosten(projektId, Anlage(projektId)) : null;
            return KostenAufloesen(a, modul, KostenPflicht.Projektlauf);
        }

        /// <summary>
        /// Braucht dieser Stand überhaupt SPEZIFISCHE Kostensätze (Befund #185)?
        /// </summary>
        /// <remarks>
        /// <para>Nein — und nur dann nein —, wenn eine Flotte vorliegt und JEDE ihrer
        /// Einheiten eigene Kosten trägt: <c>SpeicherFlottenStudieCtrl.Konfiguration</c>
        /// überschreibt genau die Einheiten OHNE <c>EigeneKosten</c> mit den
        /// aufgelösten Sätzen und lässt die übrigen unangetastet.</para>
        /// <para>Ohne Flotte gehen die Investitionssätze in <c>SpeicherParameter</c>
        /// (<c>CCapEurProKwh</c>/<c>CPowEurProKw</c>) der Einzelanlage ein; dann werden
        /// sie gebraucht. Die Vorlagen der Größenachsen zählen nur mit, wenn der
        /// Suchlauf überhaupt läuft.</para>
        /// </remarks>
        internal static bool SpezifischeSaetzeGebraucht(SpeicherAuslegungKonfiguration a)
        {
            SpeicherEngine.FlottenStudieKonfiguration f = a?.Flotte;
            if (f?.Einheiten == null || f.Einheiten.Count == 0) return true;
            foreach (SpeicherEngine.FlottenEinheit e in f.Einheiten)
                if (e != null && !e.EigeneKosten) return true;
            if (a.FlottenGroessenOptimieren && f.Auslegung?.Achsen != null)
                foreach (SpeicherEngine.FlottenAuslegungsAchse achse in f.Auslegung.Achsen)
                    if (achse?.Vorlage != null && !achse.Vorlage.EigeneKosten) return true;
            return false;
        }

        /// <summary>
        /// Reine Naht fuer die Tests: Alle Datenbankwerte sind bereits beschafft;
        /// ab hier werden nur noch unveraenderliche Laufdaten zusammengesetzt.
        /// </summary>
        internal static StromspeicherOptimierungVorbereitung AusQuellenVorbereiten(
            StromspeicherOptimierungVorbereitung epos,
            SpeicherParameter basis,
            StromspeicherLaufKontext kontext,
            SpeicherOptimierungEingaben eingaben,
            SpeicherKostensaetze modulkosten,
            double profilAufschlagCtKwh,
            double[] projektPvVerguetungCtKwh = null,
            double[] projektBhkwVerguetungCtKwh = null,
            KostenPflicht kostenPflicht = KostenPflicht.Studienlauf)
        {
            ArgumentNullException.ThrowIfNull(basis);
            ArgumentNullException.ThrowIfNull(eingaben);
            if (eingaben.Auslegung == null)
                throw new ArgumentException("Die Quellen- und Kostenkonfiguration fehlt.", nameof(eingaben));
            if (!double.IsFinite(profilAufschlagCtKwh))
                throw new ArgumentException("Der Tarifaufschlag muss endlich sein.", nameof(profilAufschlagCtKwh));

            SpeicherOptimierungEingaben snapshot = eingaben.Kopie();
            SpeicherAuslegungKonfiguration a = snapshot.Auslegung;
            PruefeQuellen(a);

            SpeicherKostensaetze kosten = KostenAufloesen(a, modulkosten, kostenPflicht);
            a.VerwendeteKosten = KopiereKosten(kosten);
            SpeicherParameter laufBasis = basis with
            {
                CCapEurProKwh = kosten.InvestEurProKwh,
                CPowEurProKw = kosten.InvestEurProKw,
                IFixEur = 0.0
            };

            SpeicherEingang eposEingang = epos?.Eingang;
            bool hatDatei = a.Lastquelle == SpeicherAuslegungQuelle.Datei ||
                a.PvQuelle == SpeicherAuslegungQuelle.Datei ||
                a.Preisquelle == SpeicherAuslegungQuelle.Datei;
            bool hatModellquelle = a.Lastquelle == SpeicherAuslegungQuelle.Epos ||
                a.PvQuelle == SpeicherAuslegungQuelle.Epos ||
                a.Preisquelle == SpeicherAuslegungQuelle.Epos ||
                a.Preisquelle == SpeicherAuslegungQuelle.Preisprofil;

            if (hatModellquelle && hatDatei && !a.EposModelljahrZuordnen)
                throw new InvalidOperationException(
                    "Dateireihen duerfen nur nach ausdruecklicher Wahl der EPOS-Modelljahrzuordnung mit Modelljahrreihen gemischt werden.");

            List<string> hinweise = new List<string>();
            DateTimeOffset[] echteZeit = null;
            SpeicherZeitreihe lastDatei = Datei(a.LastDatei, SpeicherZeitreihenRolle.Last,
                a.Lastquelle == SpeicherAuslegungQuelle.Datei, "Last");
            SpeicherZeitreihe pvDatei = Datei(a.PvDatei, SpeicherZeitreihenRolle.Pv,
                a.PvQuelle == SpeicherAuslegungQuelle.Datei, "PV");
            SpeicherZeitreihe preisDatei = Datei(a.PreisDatei, SpeicherZeitreihenRolle.Bezug,
                a.Preisquelle == SpeicherAuslegungQuelle.Datei, "Bezugspreis");
            SpeicherZeitreihe ersteDatei = null;

            if (hatDatei)
            {
                ersteDatei = lastDatei ?? pvDatei ?? preisDatei;
                echteZeit = UtcAchse(ersteDatei);
                PruefeGleicheAchse(ersteDatei, lastDatei);
                PruefeGleicheAchse(ersteDatei, pvDatei);
                PruefeGleicheAchse(ersteDatei, preisDatei);
            }

            double[] last = Reihe(a.Lastquelle, lastDatei, eposEingang?.LastKw,
                echteZeit, ersteDatei, hinweise, "Last");
            double[] pv;
            if (a.PvQuelle == SpeicherAuslegungQuelle.Keine)
                pv = new double[last.Length];
            else
                pv = Reihe(a.PvQuelle, pvDatei, eposEingang?.PvKw,
                    echteZeit, ersteDatei, hinweise, "PV");

            double[] preis;
            if (a.Preisquelle == SpeicherAuslegungQuelle.Preisprofil)
                preis = Profil(a.Strompreisprofil, profilAufschlagCtKwh,
                    echteZeit, ersteDatei, hinweise);
            else
            {
                preis = Reihe(a.Preisquelle, preisDatei, eposEingang?.PreisCtKwh,
                    echteZeit, ersteDatei, hinweise, "Bezugspreis");
                if (a.Preisquelle == SpeicherAuslegungQuelle.Datei)
                    preis = Multipliziere(preis, 100.0, "Bezugspreis");
            }

            PruefeReihe(last, "Last", false);
            PruefeReihe(pv, "PV", false);
            PruefeReihe(preis, "Bezugspreis", true);
            if (pv.Length != last.Length || preis.Length != last.Length)
                throw new InvalidOperationException("Last, PV und Bezugspreis muessen dieselbe Anzahl Viertelstunden enthalten.");
            if (!hatDatei && last.Length != ViertelstundenNormaljahr)
                throw new InvalidOperationException("Das EPOS-Modelljahr muss genau 35.040 Viertelstunden enthalten.");

            double[] bhkw = OptionaleEposReihe(eposEingang?.BhkwKw, echteZeit,
                ersteDatei, hinweise, "BHKW");
            double[] vPv = VerguetungAufZiel(eposEingang?.VerguetungPvCtKwh,
                projektPvVerguetungCtKwh, echteZeit, ersteDatei, hinweise,
                "PV-Verguetung");
            double[] vBhkw = VerguetungAufZiel(eposEingang?.VerguetungBhkwCtKwh,
                projektBhkwVerguetungCtKwh, echteZeit, ersteDatei, hinweise,
                "BHKW-Verguetung");
            SpeicherEingang effektiv = new SpeicherEingang(last, pv, preis, bhkw, vPv, vBhkw);
            StromspeicherLaufKontext laufKontext = KopiereKontext(kontext, laufBasis,
                effektiv, a, preis);

            return new StromspeicherOptimierungVorbereitung
            {
                Basis = laufBasis,
                Eingang = effektiv,
                Kontext = laufKontext,
                Eingaben = snapshot,
                ZeitstempelUtc = echteZeit == null ? null : (DateTimeOffset[])echteZeit.Clone(),
                ZeitachsenHinweis = string.Join(" ", hinweise)
            };
        }

        private static void PruefeQuellen(SpeicherAuslegungKonfiguration a)
        {
            if (!Enum.IsDefined(typeof(SpeicherKostenQuelle), a.Investitionsquelle) ||
                !Enum.IsDefined(typeof(SpeicherKostenQuelle), a.Betriebsquelle))
                throw new ArgumentException("Unbekannte Kostenquelle.", nameof(a));
            if (a.Lastquelle != SpeicherAuslegungQuelle.Epos &&
                a.Lastquelle != SpeicherAuslegungQuelle.Datei)
                throw new ArgumentException("Die Lastquelle muss EPOS oder Datei sein.", nameof(a));
            if (a.PvQuelle != SpeicherAuslegungQuelle.Epos &&
                a.PvQuelle != SpeicherAuslegungQuelle.Datei &&
                a.PvQuelle != SpeicherAuslegungQuelle.Keine)
                throw new ArgumentException("Die PV-Quelle muss EPOS, Datei oder Keine sein.", nameof(a));
            if (a.Preisquelle != SpeicherAuslegungQuelle.Epos &&
                a.Preisquelle != SpeicherAuslegungQuelle.Datei &&
                a.Preisquelle != SpeicherAuslegungQuelle.Preisprofil)
                throw new ArgumentException("Die Preisquelle muss EPOS, Datei oder Preisprofil sein.", nameof(a));
        }

        /// <summary>
        /// Die spezifischen Sätze eines Laufs — und die Frage, ob sie ÜBERHAUPT
        /// gebraucht werden (Befund #185).
        /// </summary>
        /// <remarks>
        /// Bis zu diesem Befund verlangte die Methode beide Kostengruppen bedingungslos.
        /// Das traf zwei Fälle, in denen sie niemand braucht: eine Flotte, deren Einheiten
        /// AUSNAHMSLOS eigene Kosten tragen (dann überschreibt
        /// <c>SpeicherFlottenStudieCtrl.Konfiguration</c> nichts mit ihnen), und den
        /// PROJEKTLAUF, dessen Betriebsergebnis — Netzleistung, SoC, Energie — keinen
        /// Kostensatz liest. Beide Fälle brachten einen vollständigen Projektlauf zu Fall.
        /// </remarks>
        private static SpeicherKostensaetze KostenAufloesen(
            SpeicherAuslegungKonfiguration a, SpeicherKostensaetze modul,
            KostenPflicht pflicht = KostenPflicht.Studienlauf)
        {
            SpeicherKostensaetze direkt = a.DirekteKosten ?? new SpeicherKostensaetze();
            SpeicherKostensaetze invest = a.Investitionsquelle == SpeicherKostenQuelle.Kostenmodul
                ? modul : direkt;
            SpeicherKostensaetze betrieb = a.Betriebsquelle == SpeicherKostenQuelle.Kostenmodul
                ? modul : direkt;
            bool gebraucht = SpezifischeSaetzeGebraucht(a);
            bool investFehlt = invest == null || !invest.InvestVorhanden;
            bool betriebFehlt = betrieb == null || !betrieb.BetriebVorhanden;

            if (investFehlt && gebraucht && pflicht == KostenPflicht.Studienlauf)
                throw new InvalidOperationException(a.Investitionsquelle == SpeicherKostenQuelle.Kostenmodul
                    ? MyResource.Resource.FLOTTE_MSG_KOSTEN_INVEST_MODUL
                    : MyResource.Resource.FLOTTE_MSG_KOSTEN_INVEST_DIALOG);
            if (betriebFehlt && gebraucht && pflicht == KostenPflicht.Studienlauf)
                throw new InvalidOperationException(a.Betriebsquelle == SpeicherKostenQuelle.Kostenmodul
                    ? MyResource.Resource.FLOTTE_MSG_KOSTEN_BETRIEB_MODUL
                    : MyResource.Resource.FLOTTE_MSG_KOSTEN_BETRIEB_DIALOG);

            if (!investFehlt)
            {
                PruefeKostenwert(invest.InvestEurProKw, "Investition EUR/kW");
                PruefeKostenwert(invest.InvestEurProKwh, "Investition EUR/kWh");
            }
            if (!betriebFehlt)
            {
                PruefeKostenwert(betrieb.BetriebEurProKwJahr, "Betrieb EUR/(kW*a)");
                PruefeKostenwert(betrieb.BetriebEurProKwhJahr, "Betrieb EUR/(kWh*a)");
                PruefeKostenwert(betrieb.BetriebEurProKwhEntladen, "Betrieb EUR/kWh entladen");
            }

            // Fehlende Sätze stehen auf 0 — und sagen im Herkunftstext, WARUM: weil die
            // Einheiten sie selbst mitbringen (dann bleibt die Rechnung vollständig) oder
            // weil dieser Projektlauf ohne sie auskommen muss (dann nicht).
            bool nichtBewertbar = (investFehlt || betriebFehlt) && gebraucht;
            return new SpeicherKostensaetze
            {
                InvestEurProKw = investFehlt ? 0.0 : invest.InvestEurProKw,
                InvestEurProKwh = investFehlt ? 0.0 : invest.InvestEurProKwh,
                BetriebEurProKwJahr = betriebFehlt ? 0.0 : betrieb.BetriebEurProKwJahr,
                BetriebEurProKwhJahr = betriebFehlt ? 0.0 : betrieb.BetriebEurProKwhJahr,
                BetriebEurProKwhEntladen = betriebFehlt ? 0.0 : betrieb.BetriebEurProKwhEntladen,
                InvestVorhanden = !investFehlt,
                BetriebVorhanden = !betriebFehlt,
                NichtBewertbar = nichtBewertbar,
                Herkunft = "Investition: " + Herkunftstext(invest, investFehlt, gebraucht) +
                    "; Betrieb: " + Herkunftstext(betrieb, betriebFehlt, gebraucht),
                AusgelassenePositionen = modul == null ||
                    (a.Investitionsquelle != SpeicherKostenQuelle.Kostenmodul &&
                     a.Betriebsquelle != SpeicherKostenQuelle.Kostenmodul)
                    ? new List<string>()
                    : new List<string>(modul.AusgelassenePositionen ?? new List<string>())
            };
        }

        private static string Herkunftstext(SpeicherKostensaetze quelle, bool fehlt, bool gebraucht)
        {
            if (!fehlt) return quelle.Herkunft ?? "";
            return gebraucht
                ? MyResource.Resource.FLOTTE_MSG_KOSTEN_NICHT_BEWERTBAR_KURZ
                : MyResource.Resource.FLOTTE_MSG_KOSTEN_JE_EINHEIT;
        }

        private static void PruefeKostenwert(double wert, string name)
        {
            if (!double.IsFinite(wert) || wert < 0.0)
                throw new InvalidOperationException(name + " muss endlich und nicht negativ sein.");
        }

        private static SpeicherKostensaetze KopiereKosten(SpeicherKostensaetze k)
            => new SpeicherKostensaetze
            {
                InvestEurProKw = k.InvestEurProKw,
                InvestEurProKwh = k.InvestEurProKwh,
                BetriebEurProKwJahr = k.BetriebEurProKwJahr,
                BetriebEurProKwhJahr = k.BetriebEurProKwhJahr,
                BetriebEurProKwhEntladen = k.BetriebEurProKwhEntladen,
                InvestVorhanden = k.InvestVorhanden,
                BetriebVorhanden = k.BetriebVorhanden,
                NichtBewertbar = k.NichtBewertbar,
                Herkunft = k.Herkunft ?? "",
                AusgelassenePositionen = new List<string>(k.AusgelassenePositionen ?? new List<string>())
            };

        private static SpeicherZeitreihe Datei(SpeicherZeitreihe reihe,
            SpeicherZeitreihenRolle rolle, bool erforderlich, string name)
        {
            if (!erforderlich) return null;
            if (reihe == null) throw new InvalidOperationException("Die gewaehlte " + name + "-Dateireihe fehlt.");
            if (reihe.Rolle != rolle)
                throw new InvalidOperationException("Die " + name + "-Dateireihe hat die falsche Rolle.");
            if (reihe.Werte == null || reihe.ZeitstempelUtc == null ||
                reihe.Werte.Length != reihe.ZeitstempelUtc.Length || reihe.Werte.Length == 0)
                throw new InvalidOperationException("Die " + name + "-Dateireihe hat keine vollstaendige Zeitachse.");
            PruefeReihe(reihe.Werte, name, rolle == SpeicherZeitreihenRolle.Bezug);
            PruefeJahresachse(reihe, name);
            return reihe;
        }

        private static void PruefeJahresachse(SpeicherZeitreihe reihe, string name)
        {
            DateTimeOffset[] zeit = reihe.ZeitstempelUtc;
            for (int i = 0; i < zeit.Length; i++)
            {
                if (zeit[i].ToUniversalTime().Ticks % Viertelstunde.Ticks != 0)
                    throw new InvalidOperationException(name + " enthaelt einen Zeitstempel ausserhalb des Viertelstundenrasters.");
                if (i > 0 && zeit[i].ToUniversalTime() - zeit[i - 1].ToUniversalTime() != Viertelstunde)
                    throw new InvalidOperationException(name + " enthaelt eine Luecke oder einen doppelten Zeitstempel.");
            }

            TimeZoneInfo zone = FindeZeitzone(reihe.Optionen?.ZeitzoneId);
            DateTime start = TimeZoneInfo.ConvertTime(zeit[0].ToUniversalTime(), zone).DateTime;
            DateTime ende = TimeZoneInfo.ConvertTime(zeit[^1].ToUniversalTime().Add(Viertelstunde), zone).DateTime;
            bool startOk = start.Month == 1 && start.Day == 1 && start.TimeOfDay == TimeSpan.Zero;
            bool endeOk = ende.Year == start.Year + 1 && ende.Month == 1 && ende.Day == 1 &&
                ende.TimeOfDay == TimeSpan.Zero;
            int erwartet = DateTime.IsLeapYear(start.Year)
                ? ViertelstundenSchaltjahr : ViertelstundenNormaljahr;
            if (!startOk || !endeOk || zeit.Length != erwartet)
                throw new InvalidOperationException(name +
                    " muss ein vollstaendiges Kalenderjahr vom 01.01. 00:00 bis zum folgenden 01.01. 00:00 enthalten.");
        }

        private static DateTimeOffset[] UtcAchse(SpeicherZeitreihe reihe)
        {
            DateTimeOffset[] ziel = new DateTimeOffset[reihe.ZeitstempelUtc.Length];
            for (int i = 0; i < ziel.Length; i++) ziel[i] = reihe.ZeitstempelUtc[i].ToUniversalTime();
            return ziel;
        }

        private static void PruefeGleicheAchse(SpeicherZeitreihe erste, SpeicherZeitreihe andere)
        {
            if (andere == null || ReferenceEquals(erste, andere)) return;
            if (erste.ZeitstempelUtc.Length != andere.ZeitstempelUtc.Length)
                throw new InvalidOperationException("Die Dateireihen haben unterschiedliche Zeitachsen.");
            for (int i = 0; i < erste.ZeitstempelUtc.Length; i++)
                if (erste.ZeitstempelUtc[i].ToUniversalTime() != andere.ZeitstempelUtc[i].ToUniversalTime())
                    throw new InvalidOperationException("Die Dateireihen haben bei Intervall " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " unterschiedliche UTC-Zeitstempel.");
        }

        private static double[] Reihe(SpeicherAuslegungQuelle quelle, SpeicherZeitreihe datei,
            double[] epos, DateTimeOffset[] echteZeit, SpeicherZeitreihe achsenQuelle,
            List<string> hinweise, string name)
        {
            if (quelle == SpeicherAuslegungQuelle.Epos)
            {
                if (epos == null) throw new InvalidOperationException("Die EPOS-" + name + "reihe fehlt.");
                if (echteZeit == null) return (double[])epos.Clone();
                return EposAufEchteZeit(epos, echteZeit, achsenQuelle, hinweise, name);
            }
            if (quelle != SpeicherAuslegungQuelle.Datei || datei == null)
                throw new InvalidOperationException("Unbrauchbare Quelle fuer " + name + ".");
            return (double[])datei.Werte.Clone();
        }

        private static double[] EposAufEchteZeit(double[] epos, DateTimeOffset[] zielZeit,
            SpeicherZeitreihe achsenQuelle, List<string> hinweise,
            string name)
        {
            if (epos.Length != ViertelstundenNormaljahr)
                throw new InvalidOperationException("Die EPOS-" + name +
                    "reihe muss fuer die Kalenderzuordnung genau 35.040 Viertelstunden enthalten.");
            TimeZoneInfo zone = FindeZeitzone(achsenQuelle?.Optionen?.ZeitzoneId);
            double[] ziel = new double[zielZeit.Length];
            bool schalttag = false;
            bool doppelstunde = false;
            for (int i = 0; i < ziel.Length; i++)
            {
                DateTime lokal = TimeZoneInfo.ConvertTime(zielZeit[i], zone).DateTime;
                int tag = lokal.Day;
                if (lokal.Month == 2 && tag == 29) { tag = 28; schalttag = true; }
                if (lokal.Second != 0 || lokal.Millisecond != 0 || lokal.Minute % 15 != 0)
                    throw new InvalidOperationException(name + " kann nicht auf das EPOS-Viertelstundenraster abgebildet werden.");
                DateTime modell = new DateTime(2023, lokal.Month, tag, lokal.Hour, lokal.Minute, 0);
                int index = (modell.DayOfYear - 1) * 96 + modell.Hour * 4 + modell.Minute / 15;
                ziel[i] = epos[index];
                if (zone.IsAmbiguousTime(DateTime.SpecifyKind(lokal, DateTimeKind.Unspecified)))
                    doppelstunde = true;
            }
            if (schalttag) FuegeHinweisHinzu(hinweise,
                "EPOS-Modelljahr auf UTC-Dateiachse: Am 29. Februar werden die EPOS-Werte des 28. Februar wiederverwendet; alle CSV-Werte bleiben unveraendert.");
            if (doppelstunde) FuegeHinweisHinzu(hinweise,
                "EPOS-Modelljahr auf UTC-Dateiachse: In der wiederholten Ortsstunde wird je gleicher Wandzeit derselbe EPOS-Wert verwendet; alle CSV-Werte bleiben unveraendert.");
            return ziel;
        }

        private static void FuegeHinweisHinzu(List<string> hinweise, string hinweis)
        {
            if (!hinweise.Contains(hinweis)) hinweise.Add(hinweis);
        }

        private static double[] Profil(KostenprofilModel profil, double aufschlagCtKwh,
            DateTimeOffset[] echteZeit, SpeicherZeitreihe achsenQuelle, List<string> hinweise)
        {
            if (profil == null) throw new InvalidOperationException("Das gewaehlte Strompreisprofil fehlt.");
            double[] monat = ZahlenStrikt(profil.Monatswerte, 12, "Monatswerte");
            double[] woche = string.IsNullOrWhiteSpace(profil.Wochenwerte)
                ? null : ZahlenStrikt(profil.Wochenwerte, 168, "Wochenwerte");
            double[] viertel;
            if (echteZeit == null)
            {
                viertel = PreisModell.ZuViertelstunden(
                    PreisModell.AusMonatsUndWochenwerten(monat, woche));
            }
            else
            {
                TimeZoneInfo zone = FindeZeitzone(achsenQuelle?.Optionen?.ZeitzoneId);
                viertel = new double[echteZeit.Length];
                bool doppelstunde = false;
                for (int i = 0; i < echteZeit.Length; i++)
                {
                    DateTime lokal = TimeZoneInfo.ConvertTime(echteZeit[i], zone).DateTime;
                    int wochentag = lokal.DayOfWeek == DayOfWeek.Sunday
                        ? 6 : (int)lokal.DayOfWeek - 1;
                    viertel[i] = monat[lokal.Month - 1] +
                        (woche == null ? 0.0 : woche[wochentag * 24 + lokal.Hour]);
                    if (zone.IsAmbiguousTime(DateTime.SpecifyKind(lokal, DateTimeKind.Unspecified)))
                        doppelstunde = true;
                }
                FuegeHinweisHinzu(hinweise,
                    "Das Strompreisprofil wurde mit den wirklichen Monats- und Wochentagen der UTC-Dateiachse aufgebaut.");
                if (doppelstunde) FuegeHinweisHinzu(hinweise,
                    "In der wiederholten Ortsstunde verwendet das Strompreisprofil fuer beide Intervalle denselben Wandzeitwert.");
            }
            return PreisModell.MitAufschlag(viertel, aufschlagCtKwh);
        }

        private static double[] ZahlenStrikt(string text, int erwartet, string name)
        {
            string[] teile = (text ?? "").Split(';');
            if (teile.Length != erwartet)
                throw new FormatException(name + " muessen genau " +
                    erwartet.ToString(CultureInfo.InvariantCulture) + " Werte enthalten.");
            double[] werte = new double[erwartet];
            for (int i = 0; i < erwartet; i++)
                if (!double.TryParse(teile[i], NumberStyles.Float, CultureInfo.InvariantCulture,
                        out werte[i]) || !double.IsFinite(werte[i]))
                    throw new FormatException(name + " enthalten an Position " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " keine endliche invariante Zahl.");
            return werte;
        }

        private static double[] Multipliziere(double[] quelle, double faktor, string name)
        {
            double[] ziel = new double[quelle.Length];
            for (int i = 0; i < ziel.Length; i++)
            {
                ziel[i] = quelle[i] * faktor;
                if (!double.IsFinite(ziel[i]))
                    throw new InvalidOperationException(name + " ist nach der Einheitenumrechnung nicht endlich.");
            }
            return ziel;
        }

        private static void PruefeReihe(double[] reihe, string name, bool negativErlaubt)
        {
            if (reihe == null || reihe.Length == 0)
                throw new InvalidOperationException(name + " darf nicht leer sein.");
            for (int i = 0; i < reihe.Length; i++)
            {
                if (!double.IsFinite(reihe[i]))
                    throw new InvalidOperationException(name + " enthaelt bei Intervall " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " keinen endlichen Wert.");
                if (!negativErlaubt && reihe[i] < 0.0)
                    throw new InvalidOperationException(name + " enthaelt bei Intervall " +
                        (i + 1).ToString(CultureInfo.InvariantCulture) + " einen negativen Wert.");
            }
        }

        private static double[] OptionaleEposReihe(double[] reihe, DateTimeOffset[] echteZeit,
            SpeicherZeitreihe achsenQuelle, List<string> hinweise, string name)
        {
            if (reihe == null) return null;
            if (echteZeit == null)
            {
                if (reihe.Length != ViertelstundenNormaljahr)
                    throw new InvalidOperationException(name +
                        " muss im EPOS-Modelljahr genau 35.040 Viertelstunden enthalten.");
                return (double[])reihe.Clone();
            }
            return EposAufEchteZeit(reihe, echteZeit, achsenQuelle, hinweise, name);
        }

        private static double[] VerguetungAufZiel(double[] eposReihe, double[] projektReihe,
            DateTimeOffset[] echteZeit, SpeicherZeitreihe achsenQuelle,
            List<string> hinweise, string name)
        {
            if (eposReihe != null)
                return OptionaleEposReihe(eposReihe, echteZeit, achsenQuelle, hinweise, name);
            if (projektReihe == null) return null;

            int erwartet = echteZeit?.Length ?? ViertelstundenNormaljahr;
            if (projektReihe.Length != erwartet)
                throw new InvalidOperationException(name + " muss genau " +
                    erwartet.ToString(CultureInfo.InvariantCulture) +
                    " Viertelstunden enthalten.");
            PruefeReihe(projektReihe, name, true);
            return (double[])projektReihe.Clone();
        }

        private static StromspeicherLaufKontext KopiereKontext(
            StromspeicherLaufKontext quelle, SpeicherParameter basis, SpeicherEingang eingang,
            SpeicherAuslegungKonfiguration a, double[] preis)
        {
            if (quelle == null) return null;
            bool eigenerPreis = a.Preisquelle != SpeicherAuslegungQuelle.Epos;
            return new StromspeicherLaufKontext
            {
                Parameter = basis,
                Eingang = eingang,
                Variante = SpeicherAuslegungKopie.Von(quelle.Variante),
                ID_Energieanlage = quelle.ID_Energieanlage,
                Bezeichner = quelle.Bezeichner,
                ZyklenZugesichert = quelle.ZyklenZugesichert,
                StandbyLeistungW = quelle.StandbyLeistungW,
                Preisversion = eigenerPreis
                    ? (a.Preisquelle == SpeicherAuslegungQuelle.Datei
                        ? a.PreisDatei?.QuelleName ?? "CSV"
                        : a.Strompreisprofil?.Bezeichner ?? "Preisprofil")
                    : quelle.Preisversion,
                NetzladepreisCtKwh = eigenerPreis ? (double[])preis.Clone()
                    : PassendeKopie(quelle.NetzladepreisCtKwh, eingang.Anzahl),
                ErloesCtKwh = PassendeKopie(quelle.ErloesCtKwh, eingang.Anzahl)
            };
        }

        private static double[] PassendeKopie(double[] reihe, int laenge)
            => reihe != null && reihe.Length == laenge ? (double[])reihe.Clone() : null;

        private static TimeZoneInfo FindeZeitzone(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Fuer die Kalenderpruefung fehlt die Zeitzone der Dateireihe.");
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) when (id == "Europe/Berlin")
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); }
                catch (TimeZoneNotFoundException) { }
            }
            catch (InvalidTimeZoneException ex)
            {
                throw new InvalidOperationException("Die Zeitzone '" + id + "' ist ungueltig.", ex);
            }
            throw new InvalidOperationException("Die Zeitzone '" + id + "' wurde nicht gefunden.");
        }
    }
}
