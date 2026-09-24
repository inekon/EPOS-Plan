using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die WAHL des Typtagwegs je Projekt</b> (Umsetzungskonzept Zapfprofilgenerator 4.2,
    /// N14 (i); Schemaschritt T3 „Typtage", Schritt 130, Stufe Z4b, Gruppe 2): Sie steht in
    /// <c>Tab_TwwProjekt</c> (<c>Typtage_Aktiv</c>, <c>Typtage_Klimazone</c>,
    /// <c>Typtage_Gebaeudeart</c>), der Schreibweg trägt sie, der Eingang macht daraus die Weiche
    /// <c>Zapfprofileingang.Typtage</c>, und der Rechenweg rechnet damit den Jahresgang — mit
    /// erhaltener Jahresenergie. Ohne eingespielte Typtage lehnt er die Zone BENANNT ab; eine
    /// Datenbank vor 130 lehnt eine gesetzte Wahl benannt ab, läuft aber ohne Wahl durch.
    ///
    /// <para><b>Kein Wert einer Richtlinie:</b> Die Typtage kommen aus einem ERFUNDENEN Paket
    /// (<see cref="Typtagpaketbauer"/>) und entstehen nur in der Arbeitskopie; die Testdatenbank
    /// bleibt ohne Typtage (Konzept Kapitel 6). Gerechnet wird auf Projekt 1006 — keines der fünf
    /// Projekte der CI.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilTyptagwahlTests
    {
        /// <summary>Projekt 1006 trägt eine Klimaregion mit 365 Tageszeilen.</summary>
        private const int PROJEKT = 1006;

        private const int ZONE = 3;
        private const string ART = "probehaus";

        /// <summary>Die erste Nutzungsart des fiktiven Testkatalogs mit Bezug auf Personen.</summary>
        private static Nutzungsart Wohnnutzung()
            => ZapfprofilCtrl.Katalog().First(n => n.Bezug == ZapfBezugsart.Personen);

        /// <summary>Ein Arbeitsstand mit EINER Zone auf einer Wohnnutzung (erfundene, runde Werte).</summary>
        private static ZapfprofilStand Stand(bool typtage, int? klimazone = ZONE, string art = ART)
        {
            Nutzungsart n = Wohnnutzung();
            var zone = new ZonenStand
            {
                Name = "Zone Typtage",
                IdNutzungsart = n.Id,
                Bezugsmenge = 10,
                Niveau = ZapfNiveau.Mittel,
                Topologie = ZapfTopologie.Speicher
            };
            ProjektStand p = ZapfprofilCtrl.ProjektVorgabe() with
            {
                TyptageAktiv = typtage,
                TyptageKlimazone = klimazone,
                TyptageGebaeudeart = art
            };
            return new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, p);
        }

        /// <summary>Spielt das erfundene Paket ein und gibt seine Zeilenzahl zurück.</summary>
        private static int Einspielen(bool mitGaengen = false)
        {
            Typtagpaketbauer b = Typtagpaketbauer.Erfunden(ZONE, ART);
            if (mitGaengen)
                foreach (var k in b.Kategorien) b.Gaenge.Add((ART, k.Code, 720, new[] { 0.25, 0.75 }));
            TwwTyptagimportBericht bericht = TwwTyptagCtrl.Importieren(b.Dateien(), "2026-09-24");
            Assert.Null(bericht.Abbruch);
            return bericht.Zeilen;
        }

        // =================================================================================
        //  Der Schreibweg trägt die Wahl
        // =================================================================================

        [Fact]
        public void Die_Wahl_wird_gespeichert_und_gelesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(TwwSchema.T3TyptageVollstaendig(), "Die Kopie steht nicht auf Schritt 130.");

            ZapfprofilCtrl.Speichern(PROJEKT, Stand(typtage: true));
            ProjektStand p = ZapfprofilCtrl.Lies(PROJEKT).Projekt;
            Assert.True(p.TyptageAktiv);
            Assert.Equal(ZONE, p.TyptageKlimazone);
            Assert.Equal(ART, p.TyptageGebaeudeart);
            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT Typtage_Aktiv FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT))));

            // Zurück auf „keine Wahl": aus, beide Angaben NULL — der Bestandsweg.
            ZapfprofilCtrl.Speichern(PROJEKT, Stand(typtage: false, klimazone: null, art: "  "));
            ProjektStand leer = ZapfprofilCtrl.Lies(PROJEKT).Projekt;
            Assert.False(leer.TyptageAktiv);
            Assert.Null(leer.TyptageKlimazone);
            Assert.Null(leer.TyptageGebaeudeart);
            Assert.Null(DataRepository.ExecuteScalar(
                "SELECT Typtage_Gebaeudeart FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)));
        }

        [Fact]
        public void Eine_Klimazone_unter_eins_wird_benannt_abgelehnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ex = Assert.Throws<ZapfprofilSpeicherException>(() =>
                ZapfprofilCtrl.Speichern(PROJEKT, Stand(typtage: true, klimazone: 0)));
            Assert.Equal("SPEICHER_WERTEMENGE", ex.Grund.Kennung);
            Assert.Equal("BEGRIFF_TYPTAGE_KLIMAZONE", Assert.IsType<ZapfSatz>(ex.Grund.Werte[0]).Kennung);
        }

        [Fact]
        public void Vor_Schritt_130_laeuft_das_Speichern_ohne_Wahl_durch_und_lehnt_eine_Wahl_ab()
        {
            using var db = new TwwTestdatenbank();
            Assert.False(TwwSchema.T3TyptageVollstaendig());
            Assert.False(TwwTyptagCtrl.TabelleVorhanden());

            // OHNE Wahl: Das Speichern läuft durch wie vor dem Schritt (Muster 124).
            ZapfprofilCtrl.Speichern(1, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0],
                                                           ZapfprofilCtrl.ProjektVorgabe()));
            ProjektStand gelesen = ZapfprofilCtrl.Lies(1).Projekt;
            Assert.False(gelesen.TyptageAktiv);
            Assert.Null(gelesen.TyptageKlimazone);

            // MIT Wahl: benannt abgelehnt, nicht still fallen gelassen.
            foreach (ProjektStand p in new[]
                     {
                         ZapfprofilCtrl.ProjektVorgabe() with { TyptageAktiv = true },
                         ZapfprofilCtrl.ProjektVorgabe() with { TyptageKlimazone = ZONE },
                         ZapfprofilCtrl.ProjektVorgabe() with { TyptageGebaeudeart = ART }
                     })
            {
                var ex = Assert.Throws<ZapfprofilSpeicherException>(() =>
                    ZapfprofilCtrl.Speichern(1, new ZapfprofilStand(BrauchwasserWeg.Generator, new ZonenStand[0], p)));
                Assert.Equal(ZapfSpeicherfehler.TabellenFehlen, ex.Fehler);
                Assert.Equal("SPEICHER_SPALTE_FEHLT", ex.Grund.Kennung);
                Assert.Contains("Typtage_Aktiv", Convert.ToString(ex.Grund.Werte[0]));
            }
        }

        // =================================================================================
        //  Der Eingang: die Weiche entsteht nur bei gesetzter Wahl
        // =================================================================================

        [Fact]
        public void Der_Eingang_setzt_die_Weiche_nur_bei_gesetzter_Wahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(ZapfprofilCtrl.KalenderLesen(PROJEKT, out int jan1, out bool[] we));

            // Ohne Wahl: null — der Jahresgang rechnet über den Formvektor wie im Bestand.
            Assert.Null(ZapfprofilCtrl.Eingang(PROJEKT, Stand(typtage: false), jan1, we).Typtage);

            // Mit Wahl, aber ohne eingespielte Typtage: die Anbindung steht DA (mit leeren Daten),
            // damit der Rechenweg benannt ablehnt — kein stiller Rückfall.
            Typtaganbindung ohne = ZapfprofilCtrl.Eingang(PROJEKT, Stand(typtage: true), jan1, we).Typtage;
            Assert.NotNull(ohne);
            Assert.Null(ohne.Daten);
            Assert.Equal(ZONE, ohne.Klimazone);
            Assert.Equal(ART, ohne.Gebaeudeart);

            // Das Wetter des Projekts kommt aus der Klimaregion: 365 Tagesmittel.
            Assert.Equal(Zapfkalender.TAGE, ohne.TagesmittelC.Length);
            Assert.All(ohne.TagesmittelC, t => Assert.True(t > -50 && t < 60, "Tagesmittel " + t));
            // Tab_Solar führt in der Testdatenbank keinen Bedeckungsgrad (NULL heißt „nicht verfügbar").
            Assert.Null(ohne.BedeckungAchtel);

            // Nach dem Einspielen trägt die Anbindung den Satz.
            Einspielen();
            Typtaganbindung mit = ZapfprofilCtrl.Eingang(PROJEKT, Stand(typtage: true), jan1, we).Typtage;
            Assert.NotNull(mit.Daten);
            Assert.Equal(new[] { ZONE }, mit.Daten.Klimazonen);
            Assert.Equal(new[] { ART }, mit.Daten.Gebaeudearten);
        }

        // =================================================================================
        //  Der Rechenweg: Jahresenergie erhalten, Tagesform aus dem Paket
        // =================================================================================

        [Fact]
        public void Der_Typtagweg_rechnet_und_erhaelt_die_Jahresenergie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(ZapfprofilCtrl.KalenderLesen(PROJEKT, out int jan1, out bool[] we));
            Einspielen();

            // Derselbe Arbeitsstand einmal über den Formvektor, einmal über die Typtage.
            ZapfprofilErgebnis bestand = ZapfprofilCtrl.Rechnen(PROJEKT, Stand(typtage: false), jan1, we);
            ZapfprofilErgebnis typtage = ZapfprofilCtrl.Rechnen(PROJEKT, Stand(typtage: true), jan1, we);

            ZonenErgebnis zb = Assert.Single(bestand.JeZone);
            ZonenErgebnis zt = Assert.Single(typtage.JeZone);
            Assert.False(zb.Abgelehnt, Gruende(bestand));
            Assert.False(zt.Abgelehnt, Gruende(typtage));

            // DIE JAHRESENERGIE BLEIBT: dieselbe Jahresmenge, anders über das Jahr verteilt.
            Assert.Equal(zb.Zapfung.JahressummeKwh, zt.Zapfung.JahressummeKwh, 6);
            Assert.True(zt.Zapfung.JahressummeKwh > 0);
            double[] tageBestand = Tagesmengen(zb);
            double[] tageTyptage = Tagesmengen(zt);
            Assert.Equal(Zapfkalender.TAGE, tageTyptage.Length);
            Assert.True(tageTyptage.All(t => t >= 0), "Eine Tagesmenge ist negativ.");
            Assert.True(tageBestand.Zip(tageTyptage, (a, b) => Math.Abs(a - b)).Max() > 1e-6,
                        "Der Typtagweg verteilt wie der Formvektor — die Weiche greift nicht.");

            // Der Lauf ist WIEDERHOLBAR (deterministisch): zweimal dieselbe Reihe.
            ZapfprofilErgebnis nochmal = ZapfprofilCtrl.Rechnen(PROJEKT, Stand(typtage: true), jan1, we);
            Assert.Equal(tageTyptage, Tagesmengen(Assert.Single(nochmal.JeZone)));
        }

        [Fact]
        public void Ohne_eingespielte_Typtage_lehnt_der_Typtagweg_die_Zone_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(ZapfprofilCtrl.KalenderLesen(PROJEKT, out int jan1, out bool[] we));

            ZapfprofilErgebnis ohne = ZapfprofilCtrl.Rechnen(PROJEKT, Stand(typtage: true), jan1, we);
            Assert.True(Assert.Single(ohne.JeZone).Abgelehnt);
            ZapfAblehnung a = Assert.Single(ohne.Ablehnungen);
            Assert.Equal(ZapfEingabefehler.TyptageUngueltig, a.Grund);
            Assert.Equal("EINGABE_TYPTAGE_NICHT_VERFUEGBAR", a.Satz.Kennung);

            // Eine Zone, die das Paket nicht führt, wird ebenso benannt abgelehnt.
            Einspielen();
            ZapfprofilErgebnis fremd = ZapfprofilCtrl.Rechnen(PROJEKT, Stand(typtage: true, klimazone: ZONE + 1), jan1, we);
            Assert.True(Assert.Single(fremd.JeZone).Abgelehnt);
            ZapfAblehnung b2 = Assert.Single(fremd.Ablehnungen);
            Assert.Equal(ZapfEingabefehler.TyptageUngueltig, b2.Grund);
            Assert.Equal("EINGABE_TYPTAGE_ZONE_FEHLT", b2.Satz.Kennung);
        }

        /// <summary>Die Gründe aller Ablehnungen eines Laufs — für die Meldung einer roten Probe.</summary>
        private static string Gruende(ZapfprofilErgebnis e)
            => string.Join(" | ", e.Ablehnungen.Select(a => a.Zone + ": " + a.Satz.Klartext));

        /// <summary>Die 365 Tagesmengen einer Zone aus ihrer Stundenreihe [kWh].</summary>
        private static double[] Tagesmengen(ZonenErgebnis z)
        {
            double[] stunden = z.Zapfung.KopieStundenKwh();
            var tage = new double[Zapfkalender.TAGE];
            for (int d = 0; d < tage.Length; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    tage[d] += stunden[d * Zapfkalender.STUNDEN_TAG + h];
            return tage;
        }
    }
}
