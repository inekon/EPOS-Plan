using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Zapfprofil-Dialogs</b> (<c>EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.cs</c>,
    /// Naht und Behälter in <c>Zapfprofilwege.cs</c>; Umsetzungskonzept Zapfprofilgenerator
    /// 5.2, 5.5; Stufe Z1, Gruppe 3).
    ///
    /// <para><b>Ohne Datenbank:</b> die Abbildung Arbeitsstand ↔ DTO (die Stufe behält die
    /// Größen der höheren Stufen, ein Duplikat erbt sie mit neuen Ids), die Pflichtprüfung des
    /// OK, der Einstieg samt benannter Ablehnung (Plattform, Projekt), die Meldungen des Kerns als
    /// Ressourcentexte — und als Wache, dass jede Ablehnung, jede Hinweiskennung und jeder
    /// <c>ZPG_</c>-Schlüssel in beiden Sprachen steht.</para>
    ///
    /// <para><b>Auf der Arbeitskopie der Testdatenbank</b> (fiktiver Katalog TEST-1, Projekt 1007
    /// als Träger): Laden, Vorschau über den Weg des Laufs, Speichern im Vorgang des Aufrufers
    /// mit Commit und mit benannter Ablehnung samt Rückrollen. Ohne Testdatenbank schweigen diese
    /// Fälle. Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilHuelleTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const string NUTZUNG = "Testnutzung A (fiktiv)";
        private const string VERSION = "TEST-1";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        // =================================================================================
        // Abbildung Arbeitsstand <-> DTO
        // =================================================================================

        private static ZonenStand Reiche(int id, string name) => new ZonenStand
        {
            Id = id,
            IdNutzungsart = 11,
            IdTagesgangsatz = 5,
            Name = name,
            Reihenfolge = 7,
            Bezugsmenge = 20.0,
            Niveau = ZapfNiveau.Hoch,
            PersonenJeWe = 2.5,
            Topologie = ZapfTopologie.Frischwasserstation,
            Zirkulation = false,
            Ferienbeginn = new int?[] { 200, null, null, null },
            Ferienende = new int?[] { 220, null, null, null },
            Jahresmesswert = 30000.0,
            JahresmesswertEinheit = ZapfMesswerteinheit.KwhJeJahr,
            JahresmesswertBilanzgrenze = ZapfBilanzgrenze.Zapfstelle,
            Auslastung = new double?[] { 1.1, null, null, null, null, null, null, 0.5, null, null, null, null },
            Wohnungen = new[] { new WohnungstypStand { Id = 31, Anzahl = 4, Raumzahl = 3, Reihenfolge = 1 } }
        };

        [Fact]
        public void Die_Stufe_Einfach_behaelt_die_Groessen_der_hoeheren_Stufen()
        {
            var projekt = new ProjektStand { Id = 3, Weg = BrauchwasserWeg.Generator, Seed = 7 };
            var stand = new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { Reiche(4, "Zone Nord"), Reiche(5, "Zone Süd") }, projekt);

            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);
            Assert.Equal(ZapfprofilWeg.Bestand, e.Weg);
            Assert.Equal(2, e.Zonen.Count);
            Assert.Equal(20.0, e.Zonen[0].Bezugsmenge);
            Assert.Equal(ZapfprofilNiveau.Hoch, e.Zonen[0].Niveau);
            // Tagesgangsatz, Personen, Topologie, Zirkulation, ein Ferienfenster, Messwert,
            // zwei Auslastungsmonate, Wohnungstabelle.
            Assert.Equal(9, e.Zonen[0].Ueberschrieben);

            // Der Dialog ändert nur die vier Felder der Stufe Einfach, vertauscht die Reihenfolge,
            // entfernt eine Zone und dupliziert die andere.
            e.Zonen.RemoveAt(1);
            e.Zonen[0].Name = "  Zone Nord neu ";
            e.Zonen[0].Niveau = ZapfprofilNiveau.Niedrig;
            e.Zonen[0].Bezugsmenge = 24.0;
            e.Zonen.Insert(0, new ZapfprofilZoneDaten { IdVorlage = 4, Name = "Zone Nord (Kopie)", IdNutzungsart = 11, Bezugsmenge = 8 });
            e.Zonen.Add(new ZapfprofilZoneDaten { Name = "Zone neu", IdNutzungsart = 12, Bezugsmenge = 3 });

            ZapfprofilStand zurueck = ZapfprofilHuelle.AlsStand(e, stand);
            Assert.Same(projekt, zurueck.Projekt);
            Assert.Equal(3, zurueck.Zonen.Count);

            // Das Duplikat: neue Ids, dieselben Größen der höheren Stufen, eigene Felder.
            ZonenStand kopie = zurueck.Zonen[0];
            Assert.Equal(0, kopie.Id);
            Assert.Equal(1, kopie.Reihenfolge);
            Assert.Equal("Zone Nord (Kopie)", kopie.Name);
            Assert.Equal(8.0, kopie.Bezugsmenge);
            Assert.Equal(ZapfNiveau.Mittel, kopie.Niveau);
            Assert.Equal(2.5, kopie.PersonenJeWe);
            Assert.Equal(200, kopie.Ferienbeginn[0]);
            Assert.All(kopie.Wohnungen, w => Assert.Equal(0, w.Id));
            Assert.NotSame(stand.Zonen[0].Ferienbeginn, kopie.Ferienbeginn);

            // Die bestehende Zone: Id und alles Übrige bleiben, die vier Felder und die
            // Reihenfolge sind neu, der Name ist getrimmt.
            ZonenStand nord = zurueck.Zonen[1];
            Assert.Equal(4, nord.Id);
            Assert.Equal(2, nord.Reihenfolge);
            Assert.Equal("Zone Nord neu", nord.Name);
            Assert.Equal(ZapfNiveau.Niedrig, nord.Niveau);
            Assert.Equal(24.0, nord.Bezugsmenge);
            Assert.Equal(5, nord.IdTagesgangsatz);
            Assert.Equal(ZapfTopologie.Frischwasserstation, nord.Topologie);
            Assert.False(nord.Zirkulation);
            Assert.Equal(30000.0, nord.Jahresmesswert);
            Assert.Equal(31, nord.Wohnungen[0].Id);
            Assert.Equal(0.5, nord.Auslastung[7]);

            // Die neue Zone: Vorgaben des Kerns.
            ZonenStand neu = zurueck.Zonen[2];
            Assert.Equal(0, neu.Id);
            Assert.Equal(ZapfTopologie.Speicher, neu.Topologie);
            Assert.True(neu.Zirkulation);
            Assert.Null(neu.PersonenJeWe);
            Assert.Equal(0, ZapfprofilHuelle.Ueberschrieben(neu));
        }

        [Fact]
        public void Eine_fehlende_Bezugsmenge_geht_als_null_hin_und_als_0_zurueck()
        {
            var stand = new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { new ZonenStand { Id = 2, Name = "A" } }, null);
            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);
            Assert.Null(e.Zonen[0].Bezugsmenge);
            Assert.Equal(0.0, ZapfprofilHuelle.AlsStand(e, stand).Zonen[0].Bezugsmenge);
            Assert.Null(ZapfprofilHuelle.AlsStand(e, stand).Projekt);
        }

        [Fact]
        public void Das_OK_stellt_die_Weiche_auf_den_Generator()
        {
            var stand = new ZapfprofilStand(BrauchwasserWeg.Bestand, new[] { Reiche(4, "Z") }, null);
            ZapfprofilEingabeDaten e = ZapfprofilHuelle.AlsEingabe(stand);
            Assert.Equal(ZapfprofilWeg.Bestand, e.Weg);

            ZapfprofilStand ok = ZapfprofilHuelle.Uebernahme(new ZapfprofilErgebnisDaten(e), stand);
            Assert.Equal(BrauchwasserWeg.Generator, ok.Weg);
            Assert.Equal(ZapfprofilWeg.Bestand, e.Weg);   // die Eingabe des Dialogs bleibt unberührt
        }

        [Fact]
        public void Die_Pflichtpruefung_nennt_jede_Luecke_mit_Kennung()
        {
            Assert.Equal("ZPG_MSG_KEINE_ZONE", Assert.Single(ZapfprofilHuelle.Pruefen(new ZapfprofilEingabeDaten())).Kennung);

            var e = new ZapfprofilEingabeDaten
            {
                Zonen =
                {
                    new ZapfprofilZoneDaten { Name = "", IdNutzungsart = 1, Bezugsmenge = 1 },
                    new ZapfprofilZoneDaten { Name = "Büro", IdNutzungsart = 0, Bezugsmenge = 0 },
                    new ZapfprofilZoneDaten { Name = "Wohnen", IdNutzungsart = 1, Bezugsmenge = 20 }
                }
            };
            IReadOnlyList<ZapfprofilMeldung> m = ZapfprofilHuelle.Pruefen(e);
            Assert.Equal(new[] { "ZPG_MSG_ZONE_OHNE_NAME", "ZPG_MSG_ZONE_OHNE_NUTZUNGSART", "ZPG_MSG_ZONE_OHNE_BEZUGSMENGE" },
                         m.Select(x => x.Kennung).ToArray());
            Assert.All(m, x => Assert.Equal(ZapfprofilMeldungsart.Fehler, x.Art));
            Assert.Equal("Zone „Büro“: Bitte eine Nutzungsart wählen.", m[1].Text);

            e.Zonen.RemoveRange(0, 2);
            Assert.Empty(ZapfprofilHuelle.Pruefen(e));

            // Befund 8: Ein doppelter Name (auch in anderer Schreibung) wird einmal benannt abgelehnt.
            e.Zonen.Add(new ZapfprofilZoneDaten { Name = "wohnen ", IdNutzungsart = 1, Bezugsmenge = 5 });
            e.Zonen.Add(new ZapfprofilZoneDaten { Name = "Wohnen", IdNutzungsart = 1, Bezugsmenge = 5 });
            ZapfprofilMeldung doppelt = Assert.Single(ZapfprofilHuelle.Pruefen(e));
            Assert.Equal("ZPG_MSG_ZONE_NAME_DOPPELT", doppelt.Kennung);
            Assert.Equal("Wohnen", doppelt.Zone);
            Assert.StartsWith("Zone „Wohnen“: Der Name ist mehrfach vergeben", doppelt.Text);
        }

        // =================================================================================
        // Einstieg und Naht
        // =================================================================================

        [Fact]
        public void Ohne_Naht_oder_ohne_Projekt_wird_der_Einstieg_benannt_abgelehnt()
        {
            ZapfprofilEinstieg ohne = ZapfprofilHuelle.Einstieg(PROJEKT, null);
            Assert.False(ohne.Angeboten);
            Assert.Null(ohne.Gaben);
            Assert.Null(ohne.Uebernommen);
            Assert.Equal("Der Zapfprofilgenerator ist auf dieser Plattform noch nicht erreichbar.", ohne.Grund);

            Assert.Equal("Grund der Schale", ZapfprofilHuelle.Einstieg(PROJEKT, Zapfprofilwege.Ohne("Grund der Schale")).Grund);

            var behaelter = new ZapfprofilBehaelter(0);
            ZapfprofilEinstieg ohneProjekt = ZapfprofilHuelle.Einstieg(0, behaelter.Wege());
            Assert.False(ohneProjekt.Angeboten);
            Assert.Equal("Das Zapfprofil braucht ein gespeichertes Projekt.", ohneProjekt.Grund);

            ZapfprofilEinstieg mit = ZapfprofilHuelle.Einstieg(PROJEKT, new ZapfprofilBehaelter(PROJEKT).Wege());
            Assert.True(mit.Angeboten);
            Assert.Equal("", mit.Grund);
            Assert.NotNull(mit.Gaben);
            Assert.NotNull(mit.Uebernommen);
        }

        // =================================================================================
        // Meldungen des Kerns als Ressourcentexte
        // =================================================================================

        [Fact]
        public void Jede_Bezugsart_hat_Groesse_und_Einheit_in_beiden_Sprachen()
        {
            var schluessel = new List<string>();
            foreach (ZapfBezugsart b in Enum.GetValues(typeof(ZapfBezugsart)))
            {
                schluessel.Add("ZPG_BEZUG_" + ZapfprofilHuelle.Gross(b.ToString()));
                schluessel.Add("ZPG_EINHEIT_" + ZapfprofilHuelle.Gross(b.ToString()));
            }
            Assert.Equal(14, schluessel.Count);

            string[] fehlend = schluessel.Where(k => string.IsNullOrEmpty(Text(k, DE)) || string.IsNullOrEmpty(Text(k, EN))).ToArray();
            Assert.True(fehlend.Length == 0, "Ohne Text in beiden Sprachen: " + string.Join(", ", fehlend));
        }

        /// <summary>
        /// Die Meldungen tragen den Satz des Kerns in der Oberflächensprache (N11 (k)): Kennung ist
        /// sein Ressourcenschlüssel, die Werte setzt die Oberfläche in IHRER Kultur ein, der
        /// Klartext bleibt der deutsche Satz in invarianter Kultur.
        /// </summary>
        [Fact]
        public void Die_Meldungen_nennen_Zone_und_den_Satz_des_Kerns_in_der_Oberflaechensprache()
        {
            ZapfprofilMeldung a = ZapfprofilHuelle.Meldung(new ZapfAblehnung("Büro", ZapfEingabefehler.BezugsmengeFehlt,
                ZapfSatz.Neu("EINGABE_BEZUGSMENGE_NICHT_POSITIV", "Büro")));
            Assert.Equal("ZPG_SATZ_EINGABE_BEZUGSMENGE_NICHT_POSITIV", a.Kennung);
            Assert.Equal(ZapfprofilMeldungsart.Ablehnung, a.Art);
            // Der Satz nennt seine Zone selbst: kein Vorsatz, kein „Nicht rechenbar" (das sagt der Banner).
            Assert.Equal("Die Zone „Büro“ hat keine positive Bezugsmenge.", a.Text);
            Assert.Equal("Die Zone „Büro“ hat keine positive Bezugsmenge.", a.Klartext);

            // Ein Satz ohne Zone bekommt den Vorsatz der Zone.
            ZapfprofilMeldung ohneZone = ZapfprofilHuelle.Meldung(new ZapfAblehnung("Büro", ZapfEingabefehler.RasterUngueltig,
                ZapfSatz.Neu("EINGABE_KALTWASSER_MONAT_UNGUELTIG")));
            Assert.Equal("Zone „Büro“ trägt 0: Der Monat des Kaltwassermaximums ist keine Monatszahl 1 … 12.", ohneZone.Text);

            ZapfprofilMeldung z = ZapfprofilHuelle.Meldung(new ZapfAblehnung("", ZapfEingabefehler.ZirkulationUngueltig,
                ZapfSatz.Neu("EINGABE_ZIRKULATION_METHODE")));
            Assert.Equal("Die Zirkulation trägt 0: Unbekannte Methode der Zirkulation.", z.Text);

            // Ein Begriff am Satzanfang beginnt groß — in beiden Sprachen; die Nutzungsart steht mit Namen, nie als Id.
            Assert.Equal("Die Speichertemperatur ist negativ.",
                         ZapfSatz.Neu("AUSLEGUNG_NEGATIV", ZapfSatz.Neu("BEGRIFF_SPEICHERTEMPERATUR")).Klartext);
            Assert.Equal("Die Nutzungsart der Zone „Büro“ steht nicht (mehr) im Katalog — bitte eine Nutzungsart wählen.",
                         ZapfprofilHuelle.Meldung(new ZapfAblehnung("Büro", ZapfEingabefehler.NutzungsartFehlt,
                             ZapfSatz.Neu("EINGABE_NUTZUNGSART_FEHLT", "Büro"))).Text);

            var hinweis = new ZapfHinweis("Wohnen", Mengengeruest.HINWEIS_BANDBREITE,
                ZapfSatz.Neu("HINWEIS_BEDARF_AUSSERHALB_BANDBREITE", "Wohnen", 1.5, 0.25, "–"));
            ZapfprofilMeldung h = ZapfprofilHuelle.Meldung(hinweis);
            Assert.Equal("ZPG_SATZ_HINWEIS_BEDARF_AUSSERHALB_BANDBREITE", h.Kennung);
            Assert.Equal(ZapfprofilMeldungsart.Hinweis, h.Art);
            Assert.Equal("Der spezifische Bedarf der Zone „Wohnen“ (1,5 kWh je Einheit und Tag) liegt außerhalb der "
                         + "Bandbreite des Niveaus (0,25 … –).", h.Text);
            Assert.Contains("(1.5 kWh je Einheit und Tag)", h.Klartext);

            CultureInfo.CurrentCulture = EN;
            CultureInfo.CurrentUICulture = EN;
            Assert.Equal("The specific demand of zone “Wohnen” (1.5 kWh per unit and day) lies outside the range of the level "
                         + "(0.25 … –).", ZapfprofilHuelle.Meldung(hinweis).Text);
            CultureInfo.CurrentCulture = DE;
            CultureInfo.CurrentUICulture = DE;

            // Eine Kennung ohne Muster: die Kennung mit ihren Werten — benannt statt still.
            ZapfprofilMeldung u = ZapfprofilHuelle.Meldung(new ZapfHinweis("", "UNBEKANNT_NEU", ZapfSatz.Neu("UNBEKANNT_NEU", 3)));
            Assert.Equal("UNBEKANNT_NEU (3)", u.Text);

            ZapfprofilMeldung s = ZapfprofilHuelle.Meldung(
                new ZapfprofilSpeicherException(ZapfSpeicherfehler.ZoneFremd, "Nord", ZapfSatz.Neu("SPEICHER_ZONE_FREMD", "Nord")));
            Assert.Equal("ZPG_SATZ_SPEICHER_ZONE_FREMD", s.Kennung);
            Assert.Equal("Das Zapfprofil wurde nicht gespeichert — die Zone „Nord“ gehört zu einem anderen Projekt.", s.Text);
            Assert.Equal(ZapfprofilMeldungsart.Fehler, s.Art);
        }

        /// <summary>
        /// Fehlen einer stochastisch gerechneten Zone die Zapfkategorien ihrer Nutzungsart, nennt die
        /// Meldung Nutzungsart, Katalogversion und Zone — in beiden Sprachen mit denselben Werten.
        /// </summary>
        [Fact]
        public void Fehlende_Zapfkategorien_nennen_die_Nutzungsart_in_beiden_Sprachen()
        {
            var ablehnung = new ZapfAblehnung("Nord", ZapfEingabefehler.StochastikUngueltig,
                ZapfSatz.Neu(Zapfkategoriensatz.KENNUNG_KATEGORIEN_FEHLEN, "Probe", "T1", "Nord"));
            ZapfprofilMeldung m = ZapfprofilHuelle.Meldung(ablehnung);
            Assert.Equal("ZPG_SATZ_EINGABE_STOCHASTIK_KATEGORIEN_FEHLEN", m.Kennung);
            Assert.Equal("Für die Nutzungsart „Probe“ (Katalogversion T1) der Zone „Nord“ "
                         + "stehen keine Zapfkategorien im Katalog.", m.Text);
            Assert.Equal(ablehnung.Klartext, m.Klartext);
            string en = Text(m.Kennung, EN);
            Assert.Contains("{0}", en);
            Assert.Contains("{1}", en);
            Assert.Contains("{2}", en);
            Assert.Contains("draw-off categories", en);
        }

        /// <summary>
        /// Jeder <c>ZPG_</c>- und neue <c>BPF_</c>-Schlüssel steht in BEIDEN Ressourcendateien, und die
        /// Platzhalter gleichen sich — sonst fiele in einer Sprache ein Wert weg.
        /// </summary>
        [Fact]
        public void Jeder_ZPG_Schluessel_steht_in_beiden_Sprachen_mit_denselben_Platzhaltern()
        {
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");
            string[] zpg = de.Keys.Where(k => k.StartsWith("ZPG_", StringComparison.Ordinal)).ToArray();
            Assert.True(zpg.Length >= 160, "Nur " + zpg.Length + " ZPG_-Schlüssel.");

            var funde = new List<string>();
            foreach (string k in zpg.Concat(en.Keys.Where(x => x.StartsWith("ZPG_", StringComparison.Ordinal))).Distinct())
            {
                if (!de.TryGetValue(k, out string d)) { funde.Add(k + ": fehlt deutsch"); continue; }
                if (!en.TryGetValue(k, out string e)) { funde.Add(k + ": fehlt englisch"); continue; }
                if (d.Trim().Length == 0 || e.Trim().Length == 0) funde.Add(k + ": leer");
                if (!Platzhalter(d).SequenceEqual(Platzhalter(e))) funde.Add(k + ": Platzhalter weichen ab");
            }
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        // =================================================================================
        // Auf der Testdatenbank: Laden, Vorschau, Speichern
        // =================================================================================

        [Fact]
        public void Laden_bringt_Katalog_Kontext_und_ohne_Zone_keine_Vorschau()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ZapfprofilDaten d = ZapfprofilHuelle.Laden(PROJEKT, null);
            Assert.True(d.Verfuegbar, d.Sperrgrund);
            Assert.Equal(PROJEKT, d.IdProjekt);
            Assert.Empty(d.Eingabe.Zonen);
            Assert.Equal(ZapfprofilWeg.Bestand, d.Eingabe.Weg);
            Assert.NotEqual("", d.Kontext.Projekt);
            Assert.StartsWith("Klimaregion ", d.Kontext.Klimaregion);
            Assert.StartsWith("Kalender: 1. Januar = ", d.Kontext.Kalender);

            ZapfprofilNutzungsartDaten a = Assert.Single(d.Katalog, n => n.Name == NUTZUNG && n.Katalogversion == VERSION);
            Assert.True(a.Waehlbar, a.Sperrgrund);
            Assert.Equal(3, a.BedarfJeNiveauKwhJeEinheitTag.Length);
            Assert.StartsWith("fiktiv (Testdaten)", a.Herkunft);
            Assert.NotEqual("", a.Einheit);
            Assert.NotEqual("", a.Status);

            Assert.NotNull(d.Vorschau);
            Assert.Equal(ZapfprofilVorschauZustand.NichtGerechnet, d.Vorschau.Zustand);
            Assert.Null(d.Vorschau.Summe);
            Assert.Equal("ZPG_MSG_KEINE_ZONE", Assert.Single(d.Vorschau.Meldungen).Kennung);
        }

        /// <summary>
        /// Die Vorschau rechnet dieselbe Reihe wie der Weg des Laufs (2.4): Die Summe der
        /// Monatswerte ist der Jahresbedarf, die mit ihren Tagen gewichteten Tagesgänge sind die
        /// Monatssumme, und eine Zone ohne Bezugsmenge trägt 0 — benannt.
        /// </summary>
        [Fact]
        public void Die_Vorschau_rechnet_den_Weg_des_Laufs_und_nennt_eine_Nullzone()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int nutzung = Nutzungsart();

            var eingabe = new ZapfprofilEingabeDaten
            {
                Weg = ZapfprofilWeg.Bestand,   // die Vorschau zeigt trotzdem den Generator
                Zonen =
                {
                    new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = nutzung, Bezugsmenge = 10 },
                    new ZapfprofilZoneDaten { Name = "Zone leer", IdNutzungsart = nutzung, Bezugsmenge = null }
                }
            };
            ZapfprofilVorschauDaten v = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));
            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, v.Zustand);
            Assert.Equal(3, v.Ansichten.Count);
            Assert.Equal("Summe aller Zonen", v.Summe.Titel);
            Assert.NotEqual("", v.Status);

            // Derselbe Aufruf wie der Lauf: BedarfsVorschauCtrl mit dem Arbeitsstand.
            ZapfprofilStand stand = ZapfprofilHuelle.AlsStand(eingabe, null) with { Weg = BrauchwasserWeg.Generator };
            BedarfsVorschau lauf = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Brauchwasser, PROJEKT, null, stand);
            Assert.True(lauf.Erfolgreich, lauf.Meldung);
            Assert.Equal(lauf.Waerme.Zapfprofil.Kennzahlen.JahresbedarfZapfungKwh, v.Summe.Kennzahlen.JahresbedarfZapfungKwh);

            ZapfprofilAnsichtDaten s = v.Summe;
            Assert.True(s.Kennzahlen.JahresbedarfZapfungKwh > 0);
            Assert.Equal(s.Kennzahlen.JahresbedarfZapfungKwh, s.MonateZapfungKwh.Sum(), 6);
            double tage = 0.0;
            double[][] gaenge = { s.WerktagKw, s.SamstagKw, s.SonnFeiertagKw };
            for (int t = 0; t < 3; t++) if (gaenge[t] != null) tage += gaenge[t].Sum() * s.TageJeTagtyp[t];
            Assert.Equal(s.MonateZapfungKwh[s.Monat - 1], tage, 6);
            Assert.Equal(168, s.WocheZapfungKw.Length);
            Assert.NotNull(s.TagesgangModell);
            Assert.NotNull(s.WochenprofilModell);
            Assert.NotNull(s.JahresgangModell);
            Assert.Contains("Summe aller Zonen", s.UnterschriftTagesgang);
            Assert.NotNull(s.Kennzahlen.GroessterStundenwertKw);
            Assert.Equal("Bilanzwert, keine Auslegungsgröße", s.Kennzahlen.VermerkGroessterStundenwert);

            // Die Zone ohne Bezugsmenge trägt 0, benannt - die andere rechnet.
            ZapfprofilZonenwertDaten leer = v.Zonen[1];
            Assert.True(leer.Abgelehnt);
            Assert.Equal(0.0, leer.JahresbedarfZapfungKwh);
            Assert.True(v.Ansichten[2].Abgelehnt);
            Assert.Null(v.Ansichten[2].Kennzahlen.SpezifischKwhJeEinheitJahr);
            Assert.False(v.Zonen[0].Abgelehnt);
            Assert.Equal(s.Kennzahlen.JahresbedarfZapfungKwh, v.Zonen[0].JahresbedarfZapfungKwh, 6);
            ZapfprofilMeldung m = Assert.Single(v.Meldungen, x => x.Art == ZapfprofilMeldungsart.Ablehnung);
            Assert.Equal("Zone leer", m.Zone);
            Assert.StartsWith(ZapfSatz.PRAEFIX + "EINGABE_", m.Kennung);
            Assert.Equal("Die Zone „Zone leer“ hat keine positive Bezugsmenge.", m.Text);
        }

        /// <summary>
        /// ZU5: Trägt das Projekt Netzverluste (<c>Tab_Einstellungen</c>) und rechnet eine Zone
        /// Zirkulation, nennt die Vorschau den Hinweis in der Oberflächensprache — nicht blockierend,
        /// die Vorschau ist gerechnet. Ohne Netzverluste steht er nicht da.
        /// </summary>
        [Fact]
        public void Netzverluste_und_Zirkulation_ergeben_in_der_Vorschau_den_Hinweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var eingabe = new ZapfprofilEingabeDaten
            {
                Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = Nutzungsart(), Bezugsmenge = 10 } }
            };
            const string KENNUNG = "ZPG_SATZ_HINWEIS_NETZVERLUST_UND_ZIRKULATION";

            ZapfprofilVorschauDaten ohne = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));
            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, ohne.Zustand);
            Assert.True(ohne.Summe.Kennzahlen.JahresverlustZirkulationKwh > 0);
            Assert.DoesNotContain(ohne.Meldungen, x => x.Kennung == KENNUNG);

            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Netzverluste = 5 WHERE ID_Projekt = ?",
                                           new DbParam("?", PROJEKT));
            ZapfprofilVorschauDaten mit = ZapfprofilHuelle.Vorschau(PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT));

            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, mit.Zustand);
            ZapfprofilMeldung h = Assert.Single(mit.Meldungen, x => x.Kennung == KENNUNG);
            Assert.Equal(ZapfprofilMeldungsart.Hinweis, h.Art);
            Assert.Equal("", h.Zone);
            Assert.StartsWith("Das Projekt trägt Netzverluste, und das Zapfprofil rechnet eine Zirkulation (", h.Text);
        }

        [Fact]
        public void Der_Behaelter_schreibt_im_Vorgang_des_Aufrufers_und_die_Optionsgruppe_behaelt_die_Zonen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int nutzung = Nutzungsart();

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            Assert.False(behaelter.Geaendert);
            Assert.Equal(ZapfprofilWeg.Bestand, behaelter.Weg);

            // Unverändert schreibt der Behälter nichts.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilSpeicherergebnis nichts = behaelter.Schreiben(v);
                Assert.True(nichts.Erfolg);
                Assert.Null(nichts.Stand);
                v.Commit();
            }
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.Null(ZapfprofilCtrl.Lies(PROJEKT).Projekt);

            // Das OK des Zapfprofils über den Einstieg: der Stand wandert in den Behälter.
            ZapfprofilEinstieg einstieg = ZapfprofilHuelle.Einstieg(PROJEKT, behaelter.Wege());
            var e = new ZapfprofilEingabeDaten { Weg = ZapfprofilWeg.Bestand };
            e.Zonen.Add(new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = nutzung, Bezugsmenge = 10 });
            einstieg.Uebernommen(new ZapfprofilErgebnisDaten(e));
            Assert.True(behaelter.Geaendert);
            Assert.Equal(ZapfprofilWeg.Generator, behaelter.Weg);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilSpeicherergebnis ok = behaelter.Schreiben(v);
                Assert.True(ok.Erfolg, ok.Meldung?.Text);
                Assert.True(ok.Stand.Zonen[0].Id > 0);
                v.Commit();
            }
            behaelter.Geschrieben();
            ZapfprofilStand gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(BrauchwasserWeg.Generator, gelesen.Weg);
            Assert.Equal("Zone Probe", Assert.Single(gelesen.Zonen).Name);

            // Der Parametersatz öffnet mit dem gespeicherten Stand.
            IReadOnlyDictionary<string, object> gaben = einstieg.Gaben();
            var daten = (ZapfprofilDaten)gaben["Daten"];
            Assert.Equal("Zone Probe", Assert.Single(daten.Eingabe.Zonen).Name);
            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, daten.Vorschau.Zustand);
            Assert.IsType<ZapfprofilTexte>(gaben["Texte"]);
            var vorschau = (Func<ZapfprofilEingabeDaten, ZapfprofilVorschauDaten>)gaben["Vorschau"];
            Assert.Equal(ZapfprofilVorschauZustand.Gerechnet, vorschau(daten.Eingabe).Zustand);
            Assert.Equal(ZapfprofilHuelle.HILFE_DIALOG, gaben["HilfeSchluessel"]);

            // Die Optionsgruppe schaltet zurück und behält die Zone.
            behaelter.WegSetzen(ZapfprofilWeg.Bestand);
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.True(behaelter.Schreiben(v).Erfolg);
                v.Commit();
            }
            gelesen = ZapfprofilCtrl.Lies(PROJEKT);
            Assert.Equal(BrauchwasserWeg.Bestand, gelesen.Weg);
            Assert.Single(gelesen.Zonen);
        }

        [Fact]
        public void Eine_Ablehnung_des_Schreibwegs_kommt_benannt_zurueck_und_der_Aufrufer_rollt_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            behaelter.Uebernehmen(new ZapfprofilStand(BrauchwasserWeg.Generator,
                new[] { new ZonenStand { Name = "Zone X", IdNutzungsart = 987654, Bezugsmenge = 5 } }, null));

            using (DbVorgang v = DataRepository.Vorgang())
            {
                ZapfprofilSpeicherergebnis e = behaelter.Schreiben(v);
                Assert.False(e.Erfolg);
                Assert.Null(e.Stand);
                Assert.Equal("ZPG_SATZ_SPEICHER_NUTZUNGSART_FEHLT", e.Meldung.Kennung);
                Assert.Equal(ZapfprofilMeldungsart.Fehler, e.Meldung.Art);
                Assert.StartsWith("Das Zapfprofil wurde nicht gespeichert — ", e.Meldung.Text);
                Assert.NotEqual("", e.Meldung.Klartext);
                v.Rollback();
            }
            Assert.Equal(BrauchwasserWeg.Bestand, ZapfprofilCtrl.Weg(PROJEKT));
            Assert.Empty(ZapfprofilCtrl.Lies(PROJEKT).Zonen);
            Assert.True(behaelter.Geaendert);   // der Dialog bleibt offen, der Stand bleibt
        }

        /// <summary>Ohne Tww-Tabellen (älterer Seed): leerer Katalog, Vorschau benannt abgebrochen.</summary>
        [Fact]
        public void Ohne_Tabellen_wird_die_Vorschau_benannt_abgebrochen()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);
            Assert.Empty(ZapfprofilHuelle.Katalog());

            var e = new ZapfprofilEingabeDaten();
            e.Zonen.Add(new ZapfprofilZoneDaten { Name = "Z", IdNutzungsart = 1, Bezugsmenge = 1 });
            ZapfprofilVorschauDaten v = ZapfprofilHuelle.Vorschau(1, e, null);
            Assert.Equal(ZapfprofilVorschauZustand.Abgebrochen, v.Zustand);
            Assert.StartsWith("Der Zapfprofilgenerator ist in dieser Datenbank nicht verfügbar — es fehlen: Katalog der Tagesgangsätze, ", v.Grund);
            Assert.Contains("Parameterkatalog des Zapfprofils", v.Grund);
            Assert.DoesNotContain("Tab_", v.Grund);
            Assert.Equal("ZPG_SATZ_VERFUEGBAR_TABELLEN_FEHLEN", Assert.Single(v.Meldungen).Kennung);
            Assert.Null(v.Summe);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static int Nutzungsart()
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", NUTZUNG), new DbParam("@k", VERSION)));

        private static string Text(string schluessel, CultureInfo kultur)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, kultur);

        private static string[] Platzhalter(string text)
            => Regex.Matches(text ?? "", @"\{\d+(:[^}]*)?\}").Select(m => m.Value).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

        private static Dictionary<string, string> Resx(string datei)
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.Kern", "MyResource", datei));
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                d[m.Groups["k"].Value] = System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value);
            return d;
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
