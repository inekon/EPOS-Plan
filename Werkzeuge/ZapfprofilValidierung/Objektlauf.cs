using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Der Lauf eines Messobjekts</b> — Rechnung, Vergleich, Kalibrierung, Vorschlag, Ampel.
    ///
    /// <para><b>Ein Rechenweg, kein zweiter.</b> Gerechnet wird mit
    /// <see cref="ZapfprofilRechner.Rechnen"/> auf einem <see cref="Zapfprofileingang"/> mit
    /// <b>einer</b> Zone — genau wie der Lauf des Programms; der Katalog kommt als Modell herein
    /// (<see cref="Katalog"/>), nicht über einen Controller, und keine Datenbank wird geöffnet oder
    /// kopiert. Verglichen wird mit <see cref="Messvergleich.Vergleichen"/>, kalibriert mit
    /// <see cref="Messkalibrierung"/>. Das Werkzeug rechnet selbst nichts Fachliches; es stellt den
    /// Eingang zusammen und schreibt Verhältniszahlen heraus.</para>
    ///
    /// <para><b>Die verglichene Reihe</b> folgt der <b>Bilanzgrenze des Zählers</b> aus
    /// <c>objekt.json</c>: an der Zapfstelle die Zapfung, mit Verteilung oder Speicher Zapfung und
    /// Zirkulation (<see cref="Teile"/>). Dieselbe Grenze rechnet die Kalibrierung.</para>
    ///
    /// <para><b>Verglichen wird gegen die KALIBRIERTE Reihe</b> — erst wird das Niveau auf den
    /// Jahresmesswert gebracht (4.1), dann Band, Form und Monatsanteile geprüft. Das ist der
    /// Unterschied zum Dialogweg, und er hat einen Grund: Die gerechnete Stundenleistung ist der
    /// Bezugsmenge proportional. Verglich man die <i>rohe</i> Reihe, dann verschöbe eine um 20 %
    /// falsch geschätzte Personenzahl das Spitzenverhältnis um 20 % — das Band der Dauerlinie prüfte
    /// die Schätzung der Bezugsmenge und nicht die Gestalt der Reihe. Bei einem Messobjekt ist die
    /// Bezugsmenge oft nur ungefähr bekannt; die Reihe selbst ist es nicht. Nach der Kalibrierung
    /// steht der Niveaufehler <b>allein</b> im Kalibrierfaktor (er ist die Verhältniszahl
    /// „Messwert je Rechnung"), und die drei Kriterien je Objekt sagen, was sie sagen sollen:
    /// Gleichzeitigkeit (Band), Tagesgestalt (Form) und Genauigkeit der Kalibrierung selbst.
    /// Im Dialog ist beides dasselbe, weil dort die Bezugsmenge des Projekts gepflegt ist.</para>
    ///
    /// <para><b>Die Spitzenstreuung</b> kommt aus den Stundenspitzen des Ensembles der einen Zone
    /// (<c>ZonenErgebnis.StundenspitzenKw</c>). Ohne Ensemble bleibt sie leer und wird benannt.
    /// <b>Die Spitzen gehen auf dieselbe Stufe wie die verglichene Reihe</b> (Folge V7): Läuft der
    /// Vergleich gegen die kalibrierte Reihe, werden die Realisierungsspitzen mit demselben
    /// Streckfaktor der Zapfung skaliert, mit dem die Kalibrierung die Zapfreihe streckt — sonst
    /// bezöge die Streuung ungekalibrierte Spitzen auf eine kalibrierte Spitze und wäre um den
    /// Kalibrierfaktor verschoben. Der Dialogweg (<c>ZapfprofilHuelle.Vergleichsbericht</c>) hält
    /// beide Seiten ungekalibriert; beide Wege beziehen damit Gleiches auf Gleiches.</para>
    /// </summary>
    internal static class Objektlauf
    {
        /// <summary>Der Name der Zone im Eingang — er steht in Sätzen, nie im Bericht.</summary>
        private const string ZONE = "Messobjekt";

        /// <summary>
        /// Liest die Messreihe und rechnet das Objekt durch. Ein Fehler wird zum
        /// <see cref="Objektbefund.Abbruch"/>, nie zu einer Ausnahme.
        /// </summary>
        internal static Objektbefund Rechnen(string objektordner, Objektbeschreibung o, Katalog katalog,
                                             int? realisierungen, int? seed)
        {
            var b = new Objektbefund { Kennung = o.Kennung, Ordner = Path.GetFileName(objektordner) };
            var saetze = new List<ZapfSatz>();

            Nutzungsart art = katalog.Suchen(o.Nutzungsart);
            if (art == null)
            {
                b.Abbruch = "Der Katalog fuehrt keine Nutzungsart \"" + o.Nutzungsart + "\". Vorhanden sind: "
                            + string.Join(", ", katalog.Arten.Select(a => a.Name).OrderBy(n => n, StringComparer.Ordinal));
                return b;
            }
            b.Nutzungsart = art.Name;
            b.Bezugsart = art.Bezug;
            b.Kalenderart = art.Kalender;
            b.Bezugsmenge = o.Bezugsmenge;
            b.Herkunft = o.HerkunftWert;
            b.Grenze = o.GrenzeWert ?? art.Grenze;

            Messreihe gemessen = Messreihenlesen(objektordner, o, katalog, saetze, out string lesefehler);
            if (gemessen == null) { b.Abbruch = lesefehler; Saetze(b, saetze); return b; }
            b.AufloesungMin = gemessen.AufloesungMin;
            b.MesstageGesamt = gemessen.Tage;
            b.Lueckenanteil = gemessen.Lueckenanteil;
            b.Schalttage = gemessen.Schalttage;
            b.Verbotene.AddRange(Berichtswache.Verbotene(gemessen, Spreizung(o, art, katalog)));

            int real = realisierungen ?? o.Stochastik?.Realisierungen ?? 0;
            bool stochastisch = real >= 2 && (o.Stochastik?.JahresreiheStochastisch ?? true);
            b.Realisierungen = real;
            b.Seed = seed ?? o.Stochastik?.Seed ?? 1;
            b.Einheiten = Einheiten(o.Bezugsmenge);

            ZapfprofilErgebnis e;
            try
            {
                e = ZapfprofilRechner.Rechnen(Eingang(o, katalog, real, b.Seed, stochastisch), katalog.Arten);
            }
            catch (ZapfprofilEingabeException ex)
            {
                b.Abbruch = "Die Rechnung lehnt den Eingang ab: " + (ex.Satz?.Klartext ?? ex.Message);
                Saetze(b, saetze); return b;
            }
            catch (ParametersatzException ex)
            {
                b.Abbruch = "Der Parametersatz des Katalogs genuegt nicht: " + (ex.Satz?.Klartext ?? ex.Message);
                Saetze(b, saetze); return b;
            }
            if (!e.Vollstaendig)
            {
                b.Abbruch = "Die Rechnung lehnt ab: "
                            + string.Join(" | ", e.Ablehnungen.Select(a => a.Klartext));
                Saetze(b, saetze); return b;
            }

            b.Stochastisch = e.Stochastisch;
            foreach (ZapfHinweis h in e.Hinweise) saetze.Add(h.Satz);

            Bilanzreihe gerechnet = Bilanzreihe.Summe(Teile(e, b.Grenze));
            b.Zirkulationsanteil = e.Kennzahlen?.Zirkulationsanteil;
            b.Jahresanteil = Messkalibrierung.Jahresanteil(gemessen, gerechnet);

            double spreizung = Spreizung(o, art, katalog);

            // ERST kalibrieren, DANN vergleichen (Begründung im Klassenkopf): Der Vergleich hält die
            // Messung gegen die auf ihren Jahreswert gebrachte Rechnung. Das Niveau steckt allein im
            // Kalibrierfaktor; Band, Form und Monatsanteile sagen dann etwas über Gestalt und
            // Gleichzeitigkeit und nicht über eine geschätzte Bezugsmenge.
            Bilanzreihe kalibriert = Kalibrieren(b, gemessen, gerechnet, spreizung, o, e, katalog, saetze,
                                                 out double zapfstreckung);
            IReadOnlyList<double> spitzen = Spitzen(e, kalibriert != null ? zapfstreckung : 1.0);
            Vergleichen(b, gemessen, kalibriert ?? gerechnet, spreizung, o, e, katalog, saetze, spitzen);
            b.GegenKalibrierteReihe = kalibriert != null;
            Vorschlagen(b, gemessen, spreizung, o, art, e, katalog, saetze);

            Saetze(b, saetze);
            b.KriterienBilden();
            return b;
        }

        // =============================================================================
        //  Die Messreihe
        // =============================================================================

        private static Messreihe Messreihenlesen(string ordner, Objektbeschreibung o, Katalog katalog,
                                                 List<ZapfSatz> saetze, out string fehler)
        {
            fehler = null;
            string pfad = Path.Combine(ordner, o.Messdatei);
            if (!File.Exists(pfad)) { fehler = "Die Messreihe \"" + o.Messdatei + "\" fehlt im Objektordner."; return null; }
            var optionen = new Messreihenoptionen
            {
                Bezeichnung = o.Kennung,
                Quelle = o.Messung?.Quelle ?? o.Kennung,
                Groesse = o.GroesseWert,
                Zeitstempel = o.ZeitstempelWert,
                LueckenanteilHoechstens = o.Messung?.LueckenanteilHoechstens ?? Lueckenschwelle(katalog)
            };
            try
            {
                using FileStream s = File.OpenRead(pfad);
                Messreihe r = Messreihenleser.AusStrom(s, o.Messdatei, optionen, out ZapfSatz f, saetze);
                if (r == null) fehler = "Die Messreihe ist nicht lesbar: " + (f?.Klartext ?? "ohne Grund");
                return r;
            }
            catch (IOException ex) { fehler = "Die Messreihe ist nicht lesbar: " + ex.Message; return null; }
        }

        /// <summary>Die Lückenschwelle aus dem Parametersatz; ohne Parameter die Vorgabe des Lesers.</summary>
        private static double Lueckenschwelle(Katalog katalog)
        {
            Parametersatz p = katalog?.Parameter;
            if (p != null && p.Enthaelt(ZapfParameter.VALIDIERUNG_LUECKENANTEIL))
                return p.Wert(ZapfParameter.VALIDIERUNG_LUECKENANTEIL);
            return new Messreihenoptionen().LueckenanteilHoechstens;
        }

        // =============================================================================
        //  Der Eingang der Rechnung
        // =============================================================================

        private static Zapfprofileingang Eingang(Objektbeschreibung o, Katalog katalog, int realisierungen,
                                                 int seed, bool stochastisch)
        {
            var zone = new ZonenStand
            {
                Id = 1,
                Name = ZONE,
                IdNutzungsart = katalog.Suchen(o.Nutzungsart).Id,
                Bezugsmenge = o.Bezugsmenge,
                Niveau = o.NiveauWert,
                Reihenfolge = 1,
                Zirkulation = o.Zirkulation,
                ZapftemperaturC = o.ZapftemperaturC,
                KaltwasserMittelC = o.KaltwasserMittelC,
                KaltwasserAmplitudeK = o.KaltwasserAmplitudeK,
                SpeicherverlustKwhJeJahr = o.SpeicherverlustKwhJeJahr,
                Ferienbeginn = Fenster(o, true),
                Ferienende = Fenster(o, false)
            };
            var projekt = new ProjektStand
            {
                Id = 1,
                Weg = BrauchwasserWeg.Generator,
                // Die Zirkulation rechnet über den ANTEIL aus dem Parametersatz: der Weg, den jede
                // Katalogquelle tragen kann. Leitungslänge und Flächenkennwert brauchen Angaben, die
                // ein Messobjekt in der Regel nicht mitliefert (4.3).
                ZirkAuto = true,
                ZirkMethode = ZapfZirkulationsmethode.Anteil,
                Speicherart = ZapfSpeicherart.Ladespeicher,
                LadeAuto = true,
                Perzentil = 99,
                Seed = seed,
                Realisierungen = realisierungen,
                JahresreiheStochastisch = stochastisch
            };
            return new Zapfprofileingang
            {
                Zonen = new[] { zone },
                Projekt = projekt,
                WochentagJan1 = o.Kalender.WochentagJan1,
                We = o.WeBilden(),
                Parameter = katalog.Parameter,
                Tagesgangsaetze = katalog.Saetze,
                Zapfkategorien = katalog.Kategorien
            };
        }

        /// <summary>Die vier Ferienspalten der Zone aus den Fenstern der Beschreibung.</summary>
        private static int?[] Fenster(Objektbeschreibung o, bool beginn)
        {
            var a = new int?[4];
            IReadOnlyList<Ferienfenster> f = o.FerienBilden();
            for (int i = 0; i < f.Count && i < 4; i++) a[i] = beginn ? f[i].Beginn : f[i].Ende;
            return a;
        }

        /// <summary>N der √N-Skalierung: die kaufmännisch gerundete Bezugsmenge (wie in der Hülle).</summary>
        private static int Einheiten(double bezugsmenge)
            => bezugsmenge > 0.0 && bezugsmenge <= int.MaxValue
               ? (int)Math.Round(bezugsmenge, MidpointRounding.AwayFromZero) : 0;

        /// <summary>
        /// Die Spreizung θ_Zapf − θ̄_KW [K] der einen Zone; sie rechnet nur bei einer Volumenreihe.
        /// Dieselbe Rangfolge wie im Mengengerüst: Angabe der Zone, sonst der Katalog.
        /// </summary>
        private static double Spreizung(Objektbeschreibung o, Nutzungsart art, Katalog katalog)
        {
            double zapf = o.ZapftemperaturC ?? art.Bezugstemperaturen.ZapftemperaturC;
            double kalt = o.KaltwasserMittelC
                          ?? (katalog.Parameter != null && katalog.Parameter.Enthaelt(ZapfParameter.KALTWASSER_MITTEL)
                              ? katalog.Parameter.Wert(ZapfParameter.KALTWASSER_MITTEL)
                              : art.Bezugstemperaturen.KaltwasserC);
            return zapf - kalt;
        }

        // =============================================================================
        //  Vergleich, Kalibrierung, Vorschlag
        // =============================================================================

        private static void Vergleichen(Objektbefund b, Messreihe gemessen, Bilanzreihe gerechnet, double spreizung,
                                        Objektbeschreibung o, ZapfprofilErgebnis e, Katalog katalog,
                                        List<ZapfSatz> saetze, IReadOnlyList<double> spitzen)
        {
            var eingang = new Messvergleichseingang
            {
                Reihe = gemessen,
                SpreizungK = spreizung,
                Gerechnet = gerechnet,
                Kalender = Zapfkalender.Bilden(o.Kalender.WochentagJan1, o.WeBilden(), null),
                SynthetischeStundenspitzenKw = spitzen,
                Einheiten = b.Einheiten,
                // Die Feiertage des Messjahrs aus der Beschreibung: Sie ordnen die gemessenen Tage
                // denselben Tagtypen zu wie die Rechnung (V2). Ohne Angabe bleibt die Regel des Kerns.
                MessFeiertage = (o.Kalender.Feiertage?.Length ?? 0) > 0 ? o.Kalender.Feiertage : null
            };
            eingang = Messvergleich.AusParametern(eingang, katalog.Parameter);
            b.Formschwelle = eingang.Formschwelle;

            Messvergleichsergebnis v = Messvergleich.Vergleichen(eingang);
            saetze.AddRange(v.Hinweise);
            if (!v.Ok)
            {
                b.Abbruch = "Der Vergleich lehnt ab: " + (v.Abbruch?.Klartext ?? "ohne Grund");
                return;
            }
            if (v.Energie is { } a) { b.EnergieVerhaeltnis = a.Verhaeltnis; b.EnergieAbweichung = a.Abweichung; }
            if (v.Band is { } bd)
            {
                b.Spitzenverhaeltnis = bd.Spitzenverhaeltnis;
                b.BandUnten = bd.BandUnten;
                b.BandOben = bd.BandOben;
                b.PerzentilUnten = bd.PerzentilUnten;
                b.PerzentilOben = bd.PerzentilOben;
                b.Dauerlinienwerte = bd.Dauerlinienwerte;
                b.Lage = bd.Lage;
                Bandanalyse.Objekt(b, gerechnet, spitzen);
            }
            if (v.Streuung is { } s)
            {
                b.StreuungUnten = s.Unten; b.StreuungOben = s.Oben;
                b.Streubreite = s.Streubreite; b.StreuungRealisierungen = s.Realisierungen;
            }
            if (v.WurzelN is { } w)
            {
                b.Einheiten = w.Einheiten;
                b.WurzelNVerhaeltnis = w.WurzelNVerhaeltnis;
                b.Skalierungsmass = w.Skalierungsmass;
            }
            if (v.Form is { } f)
            {
                b.Formmass = f.Formmass;
                b.Formschwelle = f.Schwelle;
                foreach (Tagesgangabweichung t in f.JeTagtyp)
                    b.Form.Add(new Formzeile(t.Tagtyp.ToString(), t.TageGemessen, t.TageGerechnet,
                                             t.MittlereAbweichung, t.VerschobenerAnteil,
                                             t.MittlereAbweichung <= f.Schwelle));
            }
            if (v.Monate is { } m)
            {
                b.MonateGroessteAbweichung = m.GroessteAbweichung;
                b.MonateGroessterMonat = m.GroessterMonat;
                b.MonatsanteileGemessen = m.AnteileGemessen;
                b.MonatsanteileGerechnet = m.AnteileGerechnet;
            }
        }

        /// <summary>
        /// Die Stundenspitzen des Ensembles der einen Zone, mal <paramref name="faktor"/> — dem
        /// Streckfaktor der Zapfung, wenn gegen die kalibrierte Reihe verglichen wird (Folge V7),
        /// sonst 1. Leer = kein Ensemble.
        /// </summary>
        internal static IReadOnlyList<double> Spitzen(ZapfprofilErgebnis e, double faktor)
        {
            IReadOnlyList<double> roh = (e?.JeZone ?? new ZonenErgebnis[0])
                .Where(z => !z.Abgelehnt && z.StundenspitzenKw != null && z.StundenspitzenKw.Count > 0)
                .Select(z => z.StundenspitzenKw).FirstOrDefault() ?? new double[0];
            return faktor == 1.0 ? roh : roh.Select(p => p * faktor).ToArray();
        }

        /// <summary>
        /// <b>Die Kalibrierung</b> (4.1, 4.8) und daraus die <b>kalibrierte Jahresreihe</b>, gegen die
        /// der Vergleich läuft: Zapfung und Zirkulation werden getrennt mit ihrem eigenen Faktor
        /// gestreckt — genau wie <c>Mengengeruest.Kalibrieren</c> sie streckt — und dann summiert.
        /// <c>null</c> heißt: Die Kalibrierung ist nicht gelaufen (Grund als Satz); dann vergleicht
        /// der Aufrufer gegen die rohe Reihe und der Bericht nennt es. <paramref name="zapfstreckung"/>
        /// trägt den Streckfaktor der Zapfung (1 ohne Kalibrierung) — mit ihm gehen die
        /// Realisierungsspitzen auf die Stufe der kalibrierten Reihe (Folge V7).
        /// </summary>
        private static Bilanzreihe Kalibrieren(Objektbefund b, Messreihe gemessen, Bilanzreihe gerechnet,
                                               double spreizung, Objektbeschreibung o, ZapfprofilErgebnis e,
                                               Katalog katalog, List<ZapfSatz> saetze, out double zapfstreckung)
        {
            zapfstreckung = 1.0;
            double zapfung = e.Kennzahlen?.JahresbedarfZapfungKwh ?? 0.0;
            double zirkulation = e.Kennzahlen?.JahresverlustZirkulationKwh ?? 0.0;
            Kalibrierergebnis k = Messkalibrierung.Kalibrieren(gemessen, spreizung, b.Grenze,
                o.SpeicherverlustKwhJeJahr, zapfung, zirkulation, Messkalibrierung.Mindesttage(katalog.Parameter),
                ZONE, out ZapfSatz fehler, saetze, gerechnet);
            if (k == null)
            {
                saetze.Add(fehler ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_MENGE"));
                return null;
            }
            b.Kalibrierfaktor = k.Faktor;
            double netto = b.Grenze == ZapfBilanzgrenze.Zapfstelle ? k.ZapfungKwh : k.ZapfungKwh + k.ZirkulationKwh;
            b.EnergieResiduum = k.MesswertNettoKwh != 0.0
                ? Math.Abs(netto - k.MesswertNettoKwh) / Math.Abs(k.MesswertNettoKwh)
                : Math.Abs(netto);

            var teile = new List<Bilanzreihe>(2);
            zapfstreckung = Streckung(zapfung, k.ZapfungKwh);
            if (e.Zapfung != null) teile.Add(e.Zapfung.Mal(zapfstreckung));
            if (e.Zirkulation != null && b.Grenze != ZapfBilanzgrenze.Zapfstelle)
                teile.Add(e.Zirkulation.Mal(Streckung(zirkulation, k.ZirkulationKwh)));
            return teile.Count == 0 ? null : Bilanzreihe.Summe(teile);
        }

        /// <summary>
        /// <b>Die Teilreihen, die der Zähler gemessen hat</b>: an der Zapfstelle (Grenze 1) allein die
        /// Zapfung, mit Verteilung oder Speicher (Grenze 2, 3) Zapfung und Zirkulation.
        ///
        /// <para>Das ist der zweite benannte Unterschied zum Dialogweg: <c>ZapfprofilHuelle</c> summiert
        /// immer beide Teile. Im Dialog ist das vertretbar, weil dort ein Projekt mit gepflegter
        /// Messwertgrenze steht; hier <b>sagt</b> die <c>objekt.json</c> die Grenze des Zählers, und die
        /// Kalibrierung rechnet ohnehin mit ihr (<c>Messkalibrierung.Jahresmesswert</c>). Beide Seiten
        /// dieselbe Grenze zu geben ist die einzige Lesart, in der das Energieverhältnis eine Aussage
        /// ist.</para>
        /// </summary>
        private static IEnumerable<Bilanzreihe> Teile(ZapfprofilErgebnis e, ZapfBilanzgrenze grenze)
        {
            if (e.Zapfung != null) yield return e.Zapfung;
            if (e.Zirkulation != null && grenze != ZapfBilanzgrenze.Zapfstelle) yield return e.Zirkulation;
        }

        /// <summary>Der Streckfaktor eines Teils; ohne Menge bleibt er 1 (nichts zu strecken).</summary>
        private static double Streckung(double vorher, double nachher)
            => vorher > 0.0 ? nachher / vorher : 1.0;

        private static void Vorschlagen(Objektbefund b, Messreihe gemessen, double spreizung, Objektbeschreibung o,
                                        Nutzungsart art, ZapfprofilErgebnis e, Katalog katalog, List<ZapfSatz> saetze)
        {
            if (art.Kalender == ZapfKalenderart.Wohnen) return;   // der Vorschlag gilt Nichtwohn-Zonen (4.8)
            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(gemessen, spreizung, o.Bezugsmenge,
                Messkalibrierung.Mindesttage(katalog.Parameter), out ZapfSatz fehler, saetze);
            if (v == null) { saetze.Add(fehler ?? ZapfSatz.Neu("MESSKALIBRIERUNG_OHNE_TAGESGANG")); return; }

            // Der Vorschlag traegt absolute kWh - Parameter der Katalogkopie des Anwenders, die nie in
            // einen Bericht gehoeren (K5). In den Bericht kommt deshalb das VERHAELTNIS zum gerechneten
            // Tagesbedarf je Einheit; die absolute Zahl bleibt beim Anwender und folgt daraus.
            ZonenErgebnis z = e.JeZone.FirstOrDefault();
            double gerechnetJeEinheitTag = z != null && z.SpezifischKwhJeEinheitJahr > 0.0
                ? z.SpezifischKwhJeEinheitJahr / Zapfkalender.TAGE : 0.0;
            double? verhaeltnis = gerechnetJeEinheitTag > 0.0
                ? v.TagesbedarfJeEinheitKwh / gerechnetJeEinheitTag : (double?)null;

            var gaenge = new List<Formzeile>();
            foreach (Tagesgangvorschlag t in v.Tagesgaenge)
            {
                Formzeile bestand = b.Form.FirstOrDefault(f => f.Tagtyp == t.Tagtyp.ToString());
                gaenge.Add(new Formzeile(t.Tagtyp.ToString(), t.Tage, bestand?.TageGerechnet ?? 0,
                                         bestand?.MittlereAbweichung ?? 0.0, bestand?.VerschobenerAnteil ?? 0.0,
                                         bestand?.ImRahmen ?? false));
            }
            b.Vorschlag = new Vorschlagszeilen(verhaeltnis, v.VolleTage, v.Wochenfaktoren, gaenge);
        }

        private static void Saetze(Objektbefund b, List<ZapfSatz> saetze)
        {
            foreach (ZapfSatz s in saetze)
            {
                if (s == null) continue;
                string text = "[" + s.Kennung + "] " + s.Klartext;
                if (!b.Hinweise.Contains(text)) b.Hinweise.Add(text);
            }
        }
    }
}
