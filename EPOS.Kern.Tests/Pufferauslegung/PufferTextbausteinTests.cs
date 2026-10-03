using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// <b>Herkunft und Rechenweg der Pufferspeicher-Auslegung als Ressourcenschlüssel</b> (Stufe P4a, Punkt 3):
    /// Der Kern liefert je Kriterium, Warnung, Nutzungsprofil und Zeile der Herkunftsliste einen
    /// <see cref="Textbaustein"/> (Schlüssel <c>PAUS_HERK_*</c>/<c>PAUS_WEG_*</c> mit Argumenten). Geprüft wird:
    /// Jedes Kriterium trägt einen Schlüssel, jeder Schlüssel ist in beiden Sprachen auflösbar, die deutsche
    /// Auflösung gleicht dem Klartext des Kerns, die englische enthält keine der bekannten deutschen Marken,
    /// Zahlen stehen im Format der Kultur, ein fehlender Schlüssel fällt auf den Klartext zurück. Synthetische
    /// Eingänge ohne Datenbank, dazu die Herkunftsliste der Vorbelegung auf der Testdatenbank (nur lesend).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PufferTextbausteinTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        /// <summary>Deutsche Marken und Wörter, die in einer englischen Auflösung nicht stehen dürfen.</summary>
        private static readonly string[] DEUTSCH =
        {
            "Gleichung", "Tabelle", "Anhang", "Wärmespeicher", "Sekundärquelle", "Whitepaper", "Rechenkern", "Vorgabe",
            "Projekt", "Zapfprofil", "Zapf-", "kleinstes", "Nenninhalt", "Zirkulation", "Brennstoff", "Kollektor",
            "leistungsgeregelt", "Nennleistung", "Zweiterzeuger", "Kaskade", "Rang 1", "Gebäude", "Heizreihe",
            "Bedarfsreihen", "Puffers", "Recherche", "Durchfluss", "erreicht", "Starts je Tag", "Schwellen", "Wärmepumpe",
            "Heizkessel", "Solarthermie", "Prüfwert", "Katalogsatz", "Nenner", "Mindestleistung", "Ersatzwert",
            "rollierend", "Modulation", "Ein/Aus", " und ", " mit ", " ohne ", " über "
        };

        private static double[] Jahresreihe(double spitzeKw)
        {
            var r = new double[8760];
            for (int i = 0; i < 8760; i++)
            {
                double tag = i / 24;
                double aussen = 8 - 10 * Math.Cos(2 * Math.PI * (tag - 15) / 365.0) + 3 * Math.Sin(2 * Math.PI * (i % 24 - 9) / 24.0);
                r[i] = Math.Max(0, (15 - aussen) / 27.0 * spitzeKw);
            }
            return r;
        }

        private static PufferAuslegungEingang Waermepumpe() => new PufferAuslegungEingang
        {
            KlasseHeizung = true, Vorlage = PufferVorlage.WP_MONO,
            Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = false },
            VorlaufC = 50, RuecklaufC = 40, Uebergabeart = "FLAECHE", HeizgrenzeC = 15,
            ReiheHeizung = Jahresreihe(40), Sperrfenster = PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI")
        };

        /// <summary>Eingänge, die zusammen jedes Kriterium und jeden Brauchwasserweg rechnen.</summary>
        private static IEnumerable<PufferAuslegungEingang> Eingaenge()
        {
            yield return Waermepumpe() with { Einzelraumregelung = true };
            yield return Waermepumpe() with
            {
                Vorlage = PufferVorlage.WP_BIVALENT, SperrzeitExpertenweg = true, Trinkwasservorrang = true,
                Erzeuger = new PufferErzeuger { NennleistungKw = 40, IstWaermepumpe = true, Geregelt = true, ZweiterzeugerKw = 20 },
                KlasseBrauchwasser = true, ZirkulationWeg = PufferZirkulationWeg.ANTEIL,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Frischwasser, DmaxKwh = 20, TagesbedarfL = 2000, Personen = 50 }
            };
            yield return Waermepumpe() with
            {
                KlasseBrauchwasser = true,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Speicher, NenninhaltL = 500, DmaxKwh = 10 }
            };
            yield return Waermepumpe() with
            {
                KlasseBrauchwasser = true,
                Zapfprofil = new PufferZapfprofil { Topologie = PufferBwTopologie.Durchfluss, DmaxKwh = 10 }
            };
            yield return Waermepumpe() with
            {
                Vorlage = PufferVorlage.BHKW, Sperrfenster = Array.Empty<PufferSperrfenster>(),
                Erzeuger = new PufferErzeuger { NennleistungKw = 50, ZweiterzeugerKw = 100 }
            };
            yield return Waermepumpe() with
            {
                Vorlage = PufferVorlage.FESTBRENNSTOFF, Sperrfenster = Array.Empty<PufferSperrfenster>(),
                Erzeuger = new PufferErzeuger { NennleistungKw = 30, MindestleistungKw = 9, Brennstoff = PufferBrennstoff.Scheitholz }
            };
            yield return Waermepumpe() with
            {
                Vorlage = PufferVorlage.SOLAR, Sperrfenster = Array.Empty<PufferSperrfenster>(),
                Erzeuger = new PufferErzeuger { KollektorflaecheM2 = 25, Kollektorart = PufferKollektorart.Roehre, ZweiterzeugerKw = 30 }
            };
            yield return Waermepumpe() with
            {
                KlasseHeizung = false, KlasseProzess = true, Vorlage = PufferVorlage.PROZESS,
                ReiheProzess = Jahresreihe(30), Erzeuger = new PufferErzeuger { NennleistungKw = 30 }
            };
        }

        private static IEnumerable<PufferKriterium> Kriterien(PufferAuslegungErgebnis r) => r.Zonen.SelectMany(z => z.Kriterien);

        private static void SchluesselAufloesbar(Textbaustein t, string wo)
        {
            foreach (string s in t.AlleSchluessel())
            {
                Assert.True(Textbaustein.Aufloesbar(s, DE), wo + ": " + s + " fehlt in Deutsch");
                Assert.True(Textbaustein.Aufloesbar(s, EN), wo + ": " + s + " fehlt in Englisch");
            }
        }

        /// <summary>Technische Bezeichner (Tabellen-, Spalten- und Vorgabeschlüssel) sind sprachneutral und fallen aus der Prüfung.</summary>
        private static readonly Regex BEZEICHNER = new Regex(@"\bTab_\w+|[\w<>]+(\.[\w<>*]+)+", RegexOptions.CultureInvariant);

        private static void OhneDeutsch(string englisch, string wo)
        {
            englisch = BEZEICHNER.Replace(englisch, "");
            foreach (string d in DEUTSCH)
                Assert.False(englisch.Contains(d, StringComparison.Ordinal), wo + ": „" + d + "“ in „" + englisch + "“");
        }

        // =============================================================================
        //  Kriterien und Warnungen
        // =============================================================================

        [Fact]
        public void Jedes_Kriterium_traegt_Schluessel_fuer_Herkunft_und_Rechenweg_in_beiden_Sprachen()
        {
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (PufferAuslegungEingang e in Eingaenge())
            {
                PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(e);
                foreach (PufferKriterium k in Kriterien(r))
                {
                    gesehen.Add(k.Kennung);
                    Assert.StartsWith("PAUS_HERK_", k.HerkunftBaustein.Schluessel ?? "");
                    Assert.StartsWith("PAUS_WEG_", k.RechenwegBaustein.Schluessel ?? "");
                    SchluesselAufloesbar(k.HerkunftBaustein, k.Kennung);
                    SchluesselAufloesbar(k.RechenwegBaustein, k.Kennung);

                    // Deutsch: die Ressource gleicht dem Klartext des Kerns (eine Quelle, kein Auseinanderlaufen).
                    Assert.Equal(k.Herkunft, k.HerkunftBaustein.Aufloesen(DE));
                    Assert.Equal(k.Rechenweg, k.RechenwegBaustein.Aufloesen(DE));

                    // Englisch: aufgelöst, nicht leer, ohne deutsche Marke.
                    string h = k.HerkunftBaustein.Aufloesen(EN), w = k.RechenwegBaustein.Aufloesen(EN);
                    Assert.False(string.IsNullOrWhiteSpace(h));
                    Assert.False(string.IsNullOrWhiteSpace(w));
                    OhneDeutsch(h, k.Kennung + " Herkunft");
                    OhneDeutsch(w, k.Kennung + " Rechenweg");
                }
                foreach (PufferWarnung x in r.Warnungen)
                {
                    Assert.NotNull(x.HerkunftBaustein.Schluessel);
                    SchluesselAufloesbar(x.HerkunftBaustein, x.Code);
                    OhneDeutsch(x.HerkunftBaustein.Aufloesen(EN), x.Code);
                }
                if (r.Kennzahlen.Verlust != null)
                {
                    SchluesselAufloesbar(r.Kennzahlen.Verlust.HerkunftBaustein, "K11");
                    OhneDeutsch(r.Kennzahlen.Verlust.HerkunftBaustein.Aufloesen(EN), "K11");
                }
            }
            foreach (string kennung in new[]
                     {
                         PufferKriteriumKennung.K1, PufferKriteriumKennung.K2, PufferKriteriumKennung.K3, PufferKriteriumKennung.K4,
                         PufferKriteriumKennung.K4E, PufferKriteriumKennung.D1, PufferKriteriumKennung.D2, PufferKriteriumKennung.K9,
                         PufferKriteriumKennung.K9E, PufferKriteriumKennung.K10, PufferKriteriumKennung.KV,
                         PufferKriteriumKennung.B_SPEICHER, PufferKriteriumKennung.B_FRISCHWASSER
                     })
                Assert.Contains(kennung, gesehen);
        }

        [Fact]
        public void Zahlen_im_Rechenweg_stehen_im_Format_der_Kultur()
        {
            PufferAuslegungErgebnis r = PufferAuslegung.Rechnen(Waermepumpe() with
            {
                Erzeuger = new PufferErzeuger { NennleistungKw = 1234.5, IstWaermepumpe = true }
            });
            PufferKriterium k2 = r.Zone(PufferZone.Heizung).Kriterium(PufferKriteriumKennung.K2);
            Assert.Contains("1.234,5 kW", k2.RechenwegBaustein.Aufloesen(DE));
            Assert.Contains("1,234.5 kW", k2.RechenwegBaustein.Aufloesen(EN));
            Assert.Contains("fixed speed", k2.RechenwegBaustein.Aufloesen(EN));
            Assert.Contains("Fixed-Speed", k2.Rechenweg);
        }

        [Fact]
        public void Ohne_Schluessel_gilt_der_Klartext_und_ein_Klartext_wird_nie_formatiert()
        {
            var t = Textbaustein.T("PAUS_GIBT_ES_NICHT", "Wert {0} l", 1500.25);
            Assert.Equal("Wert 1.500,25 l", t.Klartext);
            Assert.Equal("Wert 1,500.25 l", t.Aufloesen(EN));
            Assert.False(Textbaustein.Aufloesbar("PAUS_GIBT_ES_NICHT", EN));

            var klar = Textbaustein.Klar("Tabelle {0} fehlt");
            Assert.Null(klar.Schluessel);
            Assert.Equal("Tabelle {0} fehlt", klar.Aufloesen(EN));
            Assert.Equal("", Textbaustein.Aufloesen(null));
            Assert.True(Textbaustein.Leer.IstLeer);

            // Gleichheit über Schlüssel und Klartext (Records des Ergebnisses vergleichen sich weiter).
            Assert.Equal(Textbaustein.T("PAUS_WEG_K2", "{0} l/kW ({1}) · {2} kW", 20.0, "x", 40.0),
                         Textbaustein.T("PAUS_WEG_K2", "{0} l/kW ({1}) · {2} kW", 20.0, "x", 40.0));
        }

        [Fact]
        public void Quellen_der_Vorgabeliste_tragen_Schluessel_fremde_bleiben_Klartext()
        {
            PufferAuslegungParameter p = PufferAuslegungParameter.Vorgabe();
            string quelle = p.Quelle(PufferAuslegungVorgaben.WARNSCHWELLE);
            Textbaustein t = PufferAuslegungVorgaben.Quellentext(quelle);
            Assert.StartsWith("PAUS_HERK_Q_", t.Schluessel);
            Assert.Equal(quelle, t.Klartext);
            Assert.Equal(quelle, t.Aufloesen(DE));
            OhneDeutsch(t.Aufloesen(EN), "Quelle");
            Assert.Null(PufferAuslegungVorgaben.Quellentext("eigene Quelle").Schluessel);
            Assert.True(PufferAuslegungVorgaben.Quellentext(null).IstLeer);
        }

        [Fact]
        public void Nutzungsprofil_traegt_seine_Herkunft_als_Schluessel()
        {
            PufferNutzungsprofilAbleitung a = Nutzungsprofil.Ableiten(new[] { "Mehrfamilienhaus" }, false, null);
            Assert.Equal("PAUS_HERK_NP_ZAPF", a.HerkunftBaustein.Schluessel);
            Assert.Equal("Zapf-Nutzungsart „Mehrfamilienhaus“", a.Herkunft);
            Assert.Equal("Draw-off use type “Mehrfamilienhaus”", a.HerkunftBaustein.Aufloesen(EN));
            PufferNutzungsprofilAbleitung v = Nutzungsprofil.Ableiten(null, false, null);
            Assert.Equal("Default", v.HerkunftBaustein.Aufloesen(EN));
            Assert.Equal("Vorgabe", v.HerkunftBaustein.Aufloesen(DE));
            Assert.Equal("Process heat in the project", Nutzungsprofil.Ableiten(null, true, null).HerkunftBaustein.Aufloesen(EN));
        }

        // =============================================================================
        //  Herkunftsliste der Vorbelegung (Testdatenbank, nur lesend)
        // =============================================================================

        [Fact]
        public void Herkunftsliste_der_Vorbelegung_loest_in_beiden_Sprachen_auf()
        {
            if (!_db.Vorhanden) return;
            foreach ((int projekt, int? puffer) in new (int, int?)[] { (1045, 1054210), (1045, null), (1030, 1054170), (1041, null) })
            {
                PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(projekt, puffer, PufferAuslegungCtrl.Reihen(projekt));
                Assert.NotEmpty(v.Herkunft);
                foreach (PufferAuslegungHerkunft h in v.Herkunft)
                {
                    Assert.NotNull(h.Baustein);
                    SchluesselAufloesbar(h.Baustein, projekt + "/" + h.Feld);
                    if (h.Baustein.Schluessel == null) continue;   // Tabellen- und Spaltennamen bleiben Klartext
                    Assert.Equal(h.Text, h.Baustein.Aufloesen(DE));
                    OhneDeutsch(h.Baustein.Aufloesen(EN), projekt + "/" + h.Feld);
                }
                Assert.Contains(v.Herkunft, h => h.Feld == nameof(PufferAuslegungEingang.Erzeuger) && h.Baustein.Schluessel != null);
            }
        }

        [Fact]
        public void Nachgerechnete_Kriterien_und_Warnungen_eines_Projekts_loesen_englisch_auf()
        {
            if (!_db.Vorhanden) return;
            PufferAuslegungErgebnis r = PufferAuslegungCtrl.Durchrechnen(1045, 1054210, out _, out string fehler);
            Assert.True(r != null, fehler);
            foreach (PufferKriterium k in Kriterien(r))
            {
                SchluesselAufloesbar(k.HerkunftBaustein, k.Kennung);
                SchluesselAufloesbar(k.RechenwegBaustein, k.Kennung);
                OhneDeutsch(k.HerkunftBaustein.Aufloesen(EN), k.Kennung);
                OhneDeutsch(k.RechenwegBaustein.Aufloesen(EN), k.Kennung);
            }
            foreach (PufferWarnung w in r.Warnungen)
                if (w.HerkunftBaustein.Schluessel != null) OhneDeutsch(w.HerkunftBaustein.Aufloesen(EN), w.Code);
        }
    }
}
