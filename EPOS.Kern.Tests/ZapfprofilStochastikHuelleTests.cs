using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hüllen der Stochastik</b> (Umsetzungskonzept Zapfprofilgenerator 4.4, 4.5 b, 5.3, 5.5;
    /// Stufe Z3, Gruppe 3): Rechenweg, Seed und Realisierungen der Jahresreihe gehen zwischen
    /// Arbeitsstand und DTO hin und zurück, die Pflichtprüfung hält ihre Grenzen, die Vorschau trägt
    /// nach einem stochastischen Lauf die Konsistenzprobe je Zone, und die Überlagerung „Auslegung"
    /// reicht „Stochastisch rechnen", Perzentil und Realisierungen des Bedarfstags an den Kern und
    /// baut aus dem Perzentil des Kerns die Zeilen der Karte (b): Wert, Streuband, Gleichzeitigkeit,
    /// Belastbarkeit und Konsistenzhinweis.
    ///
    /// <para><b>Auf der Arbeitskopie der Testdatenbank</b> (fiktiver Katalog TEST-1, Projekt 1007 als
    /// Träger): eine Zone mit abgeleiteter Nutzungsart rechnet stochastisch, eine Zone, deren
    /// Nutzungsart keine Zapfkategorien trägt, lehnt benannt mit der Nutzungsart ab; Rechenweg,
    /// Seed oder Realisierungen allein machen ein gespeichertes Ergebnis veraltet. Ohne
    /// Testdatenbank schweigen diese Fälle. Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilStochastikHuelleTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const string VERSION = "TEST-1";
        private const string ABGELEITET = "Wohnen groß (abgeleitet)";
        private const string TESTNUTZUNG = "Testnutzung A (fiktiv)";

        /// <summary>Ein Änderungsdatum, das jedes Speichern überbieten muss.</summary>
        private static readonly DateTime ALT = new DateTime(2000, 1, 1);

        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Ohne Datenbank: Abbildung und Prüfung
        // =================================================================================

        [Fact]
        public void Rechenweg_Seed_und_Realisierungen_gehen_in_die_Projektgroessen_und_zurueck()
        {
            var projekt = new ProjektStand { Id = 3, Weg = BrauchwasserWeg.Generator, Seed = 7, Realisierungen = 10, Perzentil = 99 };
            var zone = new ZonenStand { Id = 4, Name = "Zone Nord", IdNutzungsart = 11, Bezugsmenge = 20.0, Reihenfolge = 1 };
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, projekt);

            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);
            Assert.False(e.JahresreiheStochastisch);
            Assert.Equal(7, e.Seed);
            Assert.Equal(10, e.Realisierungen);

            // Unverändert bleibt dieselbe Projektzeile — nichts, was das Speichern anders schriebe.
            Assert.Same(projekt, ZapfprofilHuelle.AlsStand(e, stand).Projekt);

            // Rechenweg, Seed und Realisierungen gehen in die Projektgrößen; alles Übrige bleibt.
            e.JahresreiheStochastisch = true;
            e.Seed = 0;
            e.Realisierungen = 25;
            ProjektStand p = ZapfprofilHuelle.AlsStand(e, stand).Projekt;
            Assert.True(p.JahresreiheStochastisch);
            Assert.Equal(0, p.Seed);
            Assert.Equal(25, p.Realisierungen);
            Assert.Equal(3, p.Id);
            Assert.Equal(99, p.Perzentil);
            Assert.Equal(BrauchwasserWeg.Generator, p.Weg);

            // null heißt: der Wert der Basis bleibt; ohne Projektzeile und ohne Wunsch entsteht keine.
            ProjektStand nurWeg = ZapfprofilHuelle.MitStochastik(projekt, new ZapfprofilEingabeDaten { JahresreiheStochastisch = true });
            Assert.True(nurWeg.JahresreiheStochastisch);
            Assert.Equal(7, nurWeg.Seed);
            Assert.Equal(10, nurWeg.Realisierungen);
            Assert.Null(ZapfprofilHuelle.MitStochastik(null, new ZapfprofilEingabeDaten()));
            Assert.Same(projekt, ZapfprofilHuelle.MitStochastik(projekt, null));

            // Die Übernahme (OK) trägt sie mit; die Auslegung setzt daneben ihr Perzentil.
            e.Auslegung = new ZapfprofilAuslegungEingabeDaten { Perzentil = 95, RealisierungenAuslegung = 40 };
            ProjektStand mit = ZapfprofilHuelle.Uebernahme(new ZapfprofilErgebnisDaten(e), stand).Projekt;
            Assert.True(mit.JahresreiheStochastisch);
            Assert.Equal(25, mit.Realisierungen);
            Assert.Equal(95, mit.Perzentil);
            Assert.Equal(40, mit.RealisierungenAuslegung);
        }

        [Fact]
        public void Die_Pflichtpruefung_haelt_Seed_und_Realisierungen_in_ihren_Grenzen()
        {
            var e = new ZapfprofilEingabeDaten
            {
                Zonen = { new ZapfprofilZoneDaten { Name = "Zone A", IdNutzungsart = 1, Bezugsmenge = 5 } }
            };
            Assert.Empty(ZapfprofilHuelle.Pruefen(e));

            e.Seed = -1;
            e.Realisierungen = TwwSchema.RealisierungenMindestens - 1;
            IReadOnlyList<ZapfprofilMeldung> m = ZapfprofilHuelle.Pruefen(e);
            Assert.Equal(new[] { "ZPG_MSG_SEED_UNGUELTIG", "ZPG_MSG_REALISIERUNGEN_UNGUELTIG" }, m.Select(x => x.Kennung).ToArray());
            Assert.All(m, x => Assert.Equal(ZapfprofilMeldungsart.Fehler, x.Art));
            Assert.Equal("Der Seed muss eine ganze Zahl ab 0 sein.", m[0].Text);
            // Die Grenzen kommen aus Schema (unten) und Kern (oben), nicht aus der Hülle.
            Assert.Equal("Die Zahl der Realisierungen muss zwischen " + TwwSchema.RealisierungenMindestens + " und "
                         + Jahresensemble.HOECHSTENS + " liegen.", m[1].Text);

            e.Seed = 0;
            e.Realisierungen = Jahresensemble.HOECHSTENS;
            Assert.Empty(ZapfprofilHuelle.Pruefen(e));
            e.Realisierungen = Jahresensemble.HOECHSTENS + 1;
            Assert.Equal("ZPG_MSG_REALISIERUNGEN_UNGUELTIG", Assert.Single(ZapfprofilHuelle.Pruefen(e)).Kennung);
        }

        [Fact]
        public void Perzentil_und_Realisierungen_der_Auslegung_gehen_in_die_Projektgroessen_und_zurueck()
        {
            var basis = new ProjektStand { Id = 3, Seed = 1, Realisierungen = 10, Perzentil = 99, RealisierungenAuslegung = 150 };

            ZapfprofilAuslegungEingabeDaten a = ZapfprofilHuelle.AuslegungAusStand(
                new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], basis));
            Assert.Equal(99, a.Perzentil);
            Assert.Equal(150, a.RealisierungenAuslegung);
            Assert.False(a.Stochastisch);                                   // Laufangabe, keine Spalte

            a.Perzentil = 95;
            a.RealisierungenAuslegung = null;                               // leer = Vorgabe aus dem Parametersatz
            a.Stochastisch = true;
            ProjektStand p = ZapfprofilHuelle.MitAuslegung(basis, a);
            Assert.Equal(95, p.Perzentil);
            Assert.Null(p.RealisierungenAuslegung);
            Assert.Equal(1, p.Seed);

            // Ohne Perzentil bleibt das der Basis; ein Wert außerhalb der Wertemenge kommt nicht zurück.
            Assert.Equal(99, ZapfprofilHuelle.MitAuslegung(basis, new ZapfprofilAuslegungEingabeDaten()).Perzentil);
            Assert.Null(ZapfprofilHuelle.AuslegungAusStand(
                new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], new ProjektStand())).Perzentil);
        }

        /// <summary>
        /// Das Perzentil des Kerns wird Zeile für Zeile zum DTO: Speicher mit den Volumina beim Φ_N,
        /// Durchfluss mit der Minutenspitze, das Streuband P50 … P99 samt Spannweite, die
        /// Gleichzeitigkeit der Topologie, die Belastbarkeit und der Konsistenzhinweis in seinen drei
        /// Zuständen. Die Hülle rechnet nichts nach.
        /// </summary>
        [Fact]
        public void Das_Perzentil_des_Kerns_wird_Zeile_fuer_Zeile_zum_DTO()
        {
            var volumen = new Perzentilwerte(12, 300, 340, 360, 380, 280, double.PositiveInfinity);
            var minute = new Perzentilwerte(12, 20, 24, 26, 30, 18, 33);
            var stunde = new Perzentilwerte(12, 8, 9, 10, 11, 7, 12);
            Perzentilergebnis Ergebnis(ZapfTopologie t, bool belastbar) => new Perzentilergebnis
            {
                Topologie = t, Perzentil = 95, Seed = 4, Realisierungen = 12, Tag = 33, Belastbar = belastbar,
                MinutenspitzeKw = minute, StundenspitzeKw = stunde,
                VolumenL = t == ZapfTopologie.Speicher ? volumen : null, LeistungKw = t == ZapfTopologie.Speicher ? 25.0 : null,
                OhneNachweis = t == ZapfTopologie.Speicher ? 1 : 0,
                GleichzeitigkeitLeistung = 0.4, GleichzeitigkeitVolumen = t == ZapfTopologie.Speicher ? 0.6 : null,
                WurzelNSchaetzungKw = 27.5
            };

            var speicher = new Auslegungsgruppe { Topologie = ZapfTopologie.Speicher, Perzentil = Ergebnis(ZapfTopologie.Speicher, false) };
            ZapfprofilPerzentilDaten s = ZapfprofilHuelle.PerzentilDaten(speicher);
            Assert.True(s.Volumen);
            Assert.Equal(95, s.Perzentil);
            Assert.Equal(4, s.Seed);
            Assert.Equal(12, s.Realisierungen);
            Assert.Equal(33, s.Tag);
            Assert.Equal(20, s.Mindestzahl);
            Assert.False(s.Belastbar);
            Assert.Equal(25.0, s.LeistungKw);
            Assert.Equal(new[] { (50, 300.0), (90, 340.0), (95, 360.0), (99, 380.0) },
                         s.Streuband.Select(z => (z.Perzentil, z.Wert)).ToArray());
            Assert.Equal(360.0, s.Wert);
            Assert.Equal(280.0, s.Minimum);
            Assert.True(double.IsPositiveInfinity(s.Maximum));
            Assert.Equal(1, s.OhneNachweis);
            Assert.Equal(0.6, s.Gleichzeitigkeit);                            // GLF_V beim Speicher
            Assert.Equal(26.0, s.MinutenspitzeKw);
            Assert.Equal(10.0, s.StundenspitzeKw);
            Assert.Equal(27.5, s.WurzelNKw);
            Assert.True(s.KonsistenzGeprueft);
            Assert.False(s.KonsistenzAuffaellig);

            // Auffällig: der Satz des Kerns; ohne Schwelle im Parametersatz: nicht geprüft.
            ZapfprofilPerzentilDaten auffaellig = ZapfprofilHuelle.PerzentilDaten(speicher with
            {
                Hinweise = new[] { new Auslegungshinweis(ZapfprofilHuelle.HINWEIS_KONSISTENZ, "Satz des Kerns.", true) }
            });
            Assert.True(auffaellig.KonsistenzAuffaellig);
            Assert.Equal("Satz des Kerns.", auffaellig.KonsistenzText);
            ZapfprofilPerzentilDaten ohneSchwelle = ZapfprofilHuelle.PerzentilDaten(speicher with
            {
                Hinweise = new[] { Auslegungshinweis.ParameterFehlt(ZapfStochastikParameter.KONSISTENZSCHWELLE, "entfällt.") }
            });
            Assert.False(ohneSchwelle.KonsistenzGeprueft);

            var durchfluss = new Auslegungsgruppe { Topologie = ZapfTopologie.Durchfluss, Perzentil = Ergebnis(ZapfTopologie.Durchfluss, true) };
            ZapfprofilPerzentilDaten d = ZapfprofilHuelle.PerzentilDaten(durchfluss);
            Assert.False(d.Volumen);
            Assert.True(d.Belastbar);
            Assert.Null(d.LeistungKw);
            Assert.Equal(26.0, d.Wert);                                       // Minutenspitze
            Assert.Equal(18.0, d.Minimum);
            Assert.Equal(33.0, d.Maximum);
            Assert.Equal(0.4, d.Gleichzeitigkeit);                            // GLF_P am Durchfluss
            Assert.False(d.KonsistenzGeprueft);                              // nur Speicher

            Assert.Null(ZapfprofilHuelle.PerzentilDaten(new Auslegungsgruppe { Topologie = ZapfTopologie.Speicher }));
        }

        /// <summary>
        /// Die zwei Textbündel der Stochastik stehen in der Oberflächensprache: Jede Beschriftung,
        /// die ihren Schlüssel im Quelltext nennt, trägt unter en-US den englischen Text — die Hülle
        /// vergisst keine.
        /// </summary>
        [Fact]
        public void Die_Textbuendel_stehen_ganz_in_der_Oberflaechensprache()
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = EN;
                var funde = new List<string>();
                funde.AddRange(Abweichungen(ZapfprofilHuelle.Texte(), "ZapfprofilTexte.cs"));
                funde.AddRange(Abweichungen(ZapfprofilHuelle.AuslegungTexte(), "ZapfprofilAuslegungTexte.cs"));
                Assert.True(funde.Count == 0, string.Join("\n", funde));
            }
            finally { CultureInfo.CurrentUICulture = vorher; }
        }

        private static readonly Regex Eigenschaft = new(
            @"///\s*<summary><c>(?<schluessel>[A-Z0-9_]+)</c></summary>\s*\r?\n\s*public string (?<name>\w+) \{ get; set; \}",
            RegexOptions.Compiled);

        private static IEnumerable<string> Abweichungen(object buendel, string datei)
        {
            string quelle = File.ReadAllText(Pfad("EPOS.UI", "Dialoge", "Bedarf", datei));
            foreach (Match m in Eigenschaft.Matches(quelle))
            {
                string schluessel = m.Groups["schluessel"].Value;
                string name = m.Groups["name"].Value;
                string soll = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, EN) ?? "";
                string ist = (string)buendel.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(buendel)!;
                if (!string.Equals(soll, ist, StringComparison.Ordinal))
                    yield return datei + " " + name + " (" + schluessel + "): „" + ist + "“ statt „" + soll + "“";
            }
        }

        // =================================================================================
        // Auf der Testdatenbank: Jahresreihe
        // =================================================================================

        [Fact]
        public void Laden_bringt_Vorgaben_und_Grenzen_der_Stochastik()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilDaten d = ZapfprofilHuelle.Laden(PROJEKT, null);
            ProjektStand vorgabe = ZapfprofilCtrl.ProjektVorgabe();
            Assert.Equal(vorgabe.Seed, d.SeedVorgabe);
            Assert.Equal(vorgabe.Realisierungen, d.RealisierungenVorgabe);
            Assert.Equal(TwwSchema.RealisierungenMindestens, d.RealisierungenMindestens);
            Assert.Equal(Jahresensemble.HOECHSTENS, d.RealisierungenHoechstens);
            // Ohne Projektzeile trägt der Arbeitsstand keinen eigenen Wert — es gilt die Vorgabe.
            Assert.False(d.Eingabe.JahresreiheStochastisch);
            Assert.Null(d.Eingabe.Seed);
            Assert.Null(d.Eingabe.Realisierungen);
        }

        /// <summary>
        /// Rechenweg „stochastisch": Die Vorschau nimmt denselben Weg wie der Lauf — in der Bilanz
        /// das gezogene Jahr zum Seed, auf die Jahresmenge gebracht —, nennt Seed und Jahre im
        /// Status und trägt je Zone die Konsistenzprobe (an der Zone und in der Summe). Die
        /// Jahresmenge gleicht der deterministischen, die Stunden nicht.
        /// </summary>
        [Fact]
        public void Stochastisch_rechnet_die_Vorschau_das_Jahr_zum_Seed_samt_Konsistenzprobe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var eingabe = new ZapfprofilEingabeDaten
            {
                JahresreiheStochastisch = true, Seed = 3, Realisierungen = 4,
                Zonen = { new ZapfprofilZoneDaten { Name = "Wohnen", IdNutzungsart = Nutzungsart(ABGELEITET), Bezugsmenge = 20 } }
            };
            ZapfprofilVorschauDaten v = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));

            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, v.Zustand);
            Assert.DoesNotContain(v.Meldungen, x => x.Art != ZapfprofilMeldungsart.Hinweis);
            Assert.True(v.Stochastisch);
            Assert.Equal(3, v.Seed);
            Assert.Equal(4, v.Realisierungen);
            Assert.Equal("Vorschau aktuell · stochastisch · Seed 3 · 4 Jahre", v.Status);
            ZapfprofilKonsistenzDaten k = Assert.Single(v.Summe.Konsistenzen);
            Assert.Same(k, Assert.Single(v.Ansichten[1].Konsistenzen));
            Assert.Equal("Wohnen", k.Zone);
            Assert.Equal(0, k.Position);
            Assert.Equal(4, k.Realisierungen);
            Assert.True(k.ToleranzKwh > 0 && k.StandardabweichungKwh >= 0 && k.Faktor > 0);
            Assert.NotNull(k.Abweichung);

            eingabe.JahresreiheStochastisch = false;
            ZapfprofilVorschauDaten d = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));
            Assert.False(d.Stochastisch);
            Assert.Empty(d.Summe.Konsistenzen);
            Assert.StartsWith("Vorschau aktuell · deterministisch", d.Status);
            double det = d.Summe.Kennzahlen.JahresbedarfZapfungKwh;
            Assert.InRange(Math.Abs(v.Summe.Kennzahlen.JahresbedarfZapfungKwh / det - 1.0), 0.0, 1e-9);
            Assert.InRange(Math.Abs(k.DeterministischKwh / det - 1.0), 0.0, 1e-9);
            Assert.NotEqual(d.Summe.WocheZapfungKw, v.Summe.WocheZapfungKw);
        }

        /// <summary>
        /// Trägt die Nutzungsart einer stochastisch gerechneten Zone keine Zapfkategorien, lehnt die
        /// Zone benannt ab — mit der Nutzungsart im Satz, am Ort der Zone, wie jede Ablehnung —; die
        /// andere Zone rechnet weiter und trägt ihre Konsistenzprobe.
        /// </summary>
        [Fact]
        public void Ohne_Zapfkategorien_lehnt_die_Zone_benannt_mit_ihrer_Nutzungsart_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int ohne = Nutzungsart(TESTNUTZUNG);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart = ?", new DbParam("?", ohne));

            var eingabe = new ZapfprofilEingabeDaten
            {
                JahresreiheStochastisch = true, Realisierungen = 2,
                Zonen =
                {
                    new ZapfprofilZoneDaten { Name = "Wohnen", IdNutzungsart = Nutzungsart(ABGELEITET), Bezugsmenge = 20 },
                    new ZapfprofilZoneDaten { Name = "Probe", IdNutzungsart = ohne, Bezugsmenge = 8 }
                }
            };
            ZapfprofilVorschauDaten v = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));

            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, v.Zustand);
            ZapfprofilMeldung m = Assert.Single(v.Meldungen, x => x.Art == ZapfprofilMeldungsart.Ablehnung);
            Assert.Equal("ZPG_EINGABE_STOCHASTIK_KATEGORIEN_FEHLEN", m.Kennung);
            Assert.Equal("Probe", m.Zone);
            Assert.Equal(1, m.Position);
            Assert.Equal("Zone „Probe“ trägt 0: Für die Nutzungsart „" + TESTNUTZUNG + "“ (Katalogversion " + VERSION
                         + ") stehen keine Zapfkategorien im Katalog — die Zone rechnet nicht stochastisch.", m.Text);
            Assert.True(v.Zonen[1].Abgelehnt);
            Assert.False(v.Zonen[0].Abgelehnt);
            Assert.Equal("Wohnen", Assert.Single(v.Summe.Konsistenzen).Zone);
            Assert.Empty(v.Ansichten[2].Konsistenzen);

            // Deterministisch braucht die Zone keine Kategorien.
            eingabe.JahresreiheStochastisch = false;
            Assert.DoesNotContain(ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT)).Meldungen,
                                  x => x.Art == ZapfprofilMeldungsart.Ablehnung);
        }

        /// <summary>
        /// „Veraltet" (<c>MarkiereProjektGeaendert</c>): Schon der Wechsel des Rechenwegs, ein neuer
        /// Seed oder eine andere Zahl der Realisierungen — sonst nichts — geht mit dem OK des
        /// Zapfprofils in den Behälter, wird im Vorgang des Bedarfsprofil-Dialogs geschrieben und
        /// setzt <c>Tab_Projekt.Aenderungsdatum</c>.
        /// </summary>
        [Fact]
        public void Rechenweg_Seed_oder_Realisierungen_allein_machen_das_Ergebnis_veraltet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            ZapfprofilCtrl.Speichern(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { new ZonenStand { Name = "Zone Probe", IdNutzungsart = Nutzungsart(ABGELEITET), Bezugsmenge = 10 } }, null));

            var aenderungen = new Action<ZapfprofilEingabeDaten>[]
            {
                e => e.JahresreiheStochastisch = true,
                e => e.Seed = 42,
                e => e.Realisierungen = 3
            };
            foreach (Action<ZapfprofilEingabeDaten> aendern in aenderungen)
            {
                DatumZuruecksetzen();
                var behaelter = new ZapfprofilBehaelter(PROJEKT);
                ZapfprofilEinstieg einstieg = ZapfprofilHuelle.Einstieg(PROJEKT, behaelter.Wege());
                var daten = (ZapfprofilDaten)einstieg.Gaben()["Daten"];
                ZapfprofilEingabeDaten e = daten.Eingabe.Kopie();
                aendern(e);
                einstieg.Uebernommen(new ZapfprofilErgebnisDaten(e));
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    ZapfprofilSpeicherergebnis ok = behaelter.Schreiben(v);
                    Assert.True(ok.Erfolg, ok.Meldung?.Text);
                    v.Commit();
                }
                Assert.True(MerkmalUebernahmeCtrl.NachStandGeaendert(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT)));
            }

            ProjektStand p = ZapfprofilCtrl.Lies(PROJEKT).Projekt;
            Assert.True(p.JahresreiheStochastisch);
            Assert.Equal(42, p.Seed);
            Assert.Equal(3, p.Realisierungen);
            // Der nächste Öffnen-Stand trägt sie — und rechnet die Vorschau stochastisch.
            var wieder = ZapfprofilHuelle.Laden(PROJEKT, null);
            Assert.True(wieder.Eingabe.JahresreiheStochastisch);
            Assert.Equal(42, wieder.Eingabe.Seed);
            Assert.True(wieder.Vorschau.Stochastisch);
        }

        // =================================================================================
        // Auf der Testdatenbank: Auslegung („Stochastisch rechnen", Karte (b))
        // =================================================================================

        [Fact]
        public void Der_Stand_der_Auslegung_nennt_Perzentile_Grenzen_und_Vorgaben_der_Realisierungen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);

            ZapfprofilAuslegungStartDaten s = ZapfprofilHuelle.AuslegungStart(PROJEKT, Zonen(), null, ZapfprofilStufe.Einfach);
            Assert.Equal(TwwSchema.Perzentile, s.Perzentile);
            Assert.Equal(ZapfprofilCtrl.ProjektVorgabe().Perzentil, s.PerzentilVorgabe);
            Assert.Equal(TwwSchema.RealisierungenMindestens, s.RealisierungenMindestens);
            Assert.Equal(Zapfensemble.HOECHSTENS, s.RealisierungenHoechstens);
            Assert.Equal(20, s.Mindestzahl[95]);
            Assert.Equal(100, s.Mindestzahl[99]);
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            foreach (int p in TwwSchema.Perzentile)
                Assert.Equal(Zapfensemble.RealisierungenAuslegung(null, p, ps), s.RealisierungenVorgabe[p]);
            Assert.Equal("", s.RealisierungenVorgabeGrund);
            Assert.False(s.Eingabe.Stochastisch);
        }

        /// <summary>
        /// „Stochastisch rechnen" geht als Laufangabe an den Kern: Ohne ihn bleibt das Perzentil
        /// offen, mit ihm trägt die Speichergruppe Wert, Streuband P50 … P99, Spannweite,
        /// Gleichzeitigkeit GLF_V mit Σ n_E und den geprüften Konsistenzhinweis; der Status nennt
        /// Perzentil und Seed. Zu wenige Realisierungen sind nicht belastbar — bei P95 gegen
        /// dessen Mindestzahl.
        /// </summary>
        [Fact]
        public void Stochastisch_rechnen_fuellt_die_Karte_b_mit_Streuband_und_Gleichzeitigkeit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            ZapfprofilAuslegungEingabeDaten a = MitTag();

            ZapfprofilAuslegungDaten ohne = ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(), a, null, ZapfprofilStufe.Einfach);
            ZapfprofilAuslegungsgruppeDaten go = Assert.Single(ohne.Gruppen);
            Assert.True(go.Empfehlung.Rechenbar, go.Empfehlung.Grund);
            Assert.False(ohne.Stochastisch);
            Assert.Null(go.PerzentilErgebnis);
            Assert.Equal(ZapfprofilKartenstand.NichtGerechnet, go.Perzentil.Stand);
            Assert.Equal("Auslegung gerechnet · deterministisch · Perzentil erst mit „Stochastisch rechnen“", ohne.Status);

            a.Stochastisch = true;
            ZapfprofilAuslegungDaten d = ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(), a, null, ZapfprofilStufe.Einfach);
            ZapfprofilAuslegungsgruppeDaten g = Assert.Single(d.Gruppen);
            Assert.True(d.Stochastisch);
            Assert.True(g.Empfehlung.Rechenbar, g.Empfehlung.Grund);
            Assert.Equal(ZapfprofilKartenstand.Gerechnet, g.Perzentil.Stand);
            Assert.False(g.Perzentil.Empfohlen);                              // die Empfehlung bleibt die Summenlinie
            ZapfprofilPerzentilDaten p = g.PerzentilErgebnis;
            Assert.NotNull(p);
            ProjektStand vorgabe = ZapfprofilCtrl.ProjektVorgabe();
            Assert.Equal(vorgabe.Perzentil, p.Perzentil);
            Assert.Equal(vorgabe.Seed, p.Seed);
            Assert.Equal(Zapfensemble.RealisierungenAuslegung(null, p.Perzentil, ZapfprofilCtrl.Parameter()), p.Realisierungen);
            Assert.True(p.Belastbar);
            Assert.True(p.Volumen);
            Assert.Equal(g.Empfehlung.LeistungKw, p.LeistungKw);
            Assert.Equal(new[] { 50, 90, 95, 99 }, p.Streuband.Select(z => z.Perzentil).ToArray());
            for (int i = 1; i < p.Streuband.Count; i++) Assert.True(p.Streuband[i].Wert >= p.Streuband[i - 1].Wert);
            Assert.True(p.Minimum <= p.Streuband[0].Wert && p.Streuband[3].Wert <= p.Maximum);
            Assert.Equal(g.Perzentil.VolumenL, p.Wert);
            Assert.True(p.Gleichzeitigkeit > 0);
            Assert.True(p.Einheiten > 0);
            Assert.True(p.KonsistenzGeprueft);
            Assert.Equal("Auslegung gerechnet · stochastisch · Perzentil P" + p.Perzentil + " · Seed " + p.Seed, d.Status);

            // Zu wenige Realisierungen: nicht belastbar, mit dem Hinweis der Warnliste.
            a.RealisierungenAuslegung = 5;
            a.Perzentil = 95;
            ZapfprofilAuslegungsgruppeDaten k = Assert.Single(ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(), a, null, ZapfprofilStufe.Einfach).Gruppen);
            Assert.Equal(95, k.PerzentilErgebnis.Perzentil);
            Assert.Equal(5, k.PerzentilErgebnis.Realisierungen);
            Assert.Equal(20, k.PerzentilErgebnis.Mindestzahl);
            Assert.False(k.PerzentilErgebnis.Belastbar);
            Assert.Contains(k.Warnliste, w => w.Kennung == "ZPG_AUSHINW_PERZENTIL_NICHT_BELASTBAR");
        }

        /// <summary>
        /// Fehlen der Nutzungsart die Zapfkategorien, bleibt das Perzentil benannt nicht rechenbar —
        /// der Satz nennt die Nutzungsart, die Warnliste den Eintrag „Stochastik nicht rechenbar";
        /// Summenlinie und Empfehlung stehen weiter.
        /// </summary>
        [Fact]
        public void Ohne_Zapfkategorien_bleibt_das_Perzentil_benannt_nicht_rechenbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwZapfkategorie_STAMM WHERE ID_Nutzungsart = ?",
                                           new DbParam("?", Nutzungsart(TESTNUTZUNG)));
            ZapfprofilAuslegungEingabeDaten a = MitTag();
            a.Stochastisch = true;

            ZapfprofilAuslegungsgruppeDaten g = Assert.Single(ZapfprofilHuelle.Auslegung(PROJEKT, Zonen(), a, null, ZapfprofilStufe.Einfach).Gruppen);
            Assert.Null(g.PerzentilErgebnis);
            Assert.Equal(ZapfprofilKartenstand.NichtRechenbar, g.Perzentil.Stand);
            Assert.Contains("keine Zapfkategorien", g.Perzentil.Text);
            Assert.Contains(TESTNUTZUNG, g.Perzentil.Text);
            Assert.Contains(g.Warnliste, w => w.Kennung == "ZPG_AUSHINW_STOCHASTIK_NICHT_RECHENBAR");
            Assert.True(g.Empfehlung.Rechenbar, g.Empfehlung.Grund);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static ZapfprofilEingabeDaten Zonen() => new ZapfprofilEingabeDaten
        {
            Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = Nutzungsart(TESTNUTZUNG), Bezugsmenge = 8 } }
        };

        /// <summary>Die Eingaben mit einem konstruierten Tag, Werkstoff und Erzeugerart — dann hat die Speichergruppe ihren Punkt.</summary>
        private static ZapfprofilAuslegungEingabeDaten MitTag()
        {
            ZapfprofilBedarfstagDaten tag = ZapfprofilHuelle.BedarfstagKonstruieren(new[]
            {
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 7.5, VolumenL = 120, ZapftemperaturC = 45 },
                new ZapfprofilKonstruktorZeileDaten { BeginnH = 18.5, EndeH = 19.5, VolumenL = 200, ZapftemperaturC = 45 }
            }, "Probetag (fiktiv)").Tag;
            Assert.NotNull(tag);
            return new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = tag, Werkstoff = ZapfprofilWerkstoff.Stahl,
                Erzeugerart = ZapfprofilErzeugerart.Kessel                    // 1007 führt Kessel und Wärmepumpe
            };
        }

        private static int Nutzungsart(string name)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", name), new DbParam("@k", VERSION)));

        private static void DatumZuruecksetzen()
        {
            DataRepository.ExecuteSQL("UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?",
                new DbParam("@d", DbParamTyp.Date) { Wert = ALT },
                new DbParam("@id", PROJEKT));
            Assert.Equal(ALT, MerkmalUebernahmeCtrl.Aenderungsdatum(PROJEKT));
        }

        private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
