using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Editor „Zeitverlauf je Größe"</b> (Stufe NP4c; Konzept Nutzungsprofile 6.1, 4.3, NP-F7, NP-F9) gegen den Kern
    /// und die Testdatenbank:
    /// <list type="bullet">
    /// <item><b>Bitgleichheit</b> — ein Zeilenbild, das der Editor aus der Vorgabe-Matrix der ausgelieferten Vorlagen Wohnen,
    /// Büro und Schule aufbaut, ergibt im Generator dieselben Vorgabezeilen und an einem leeren Ziel dieselbe Reihe von 8 760
    /// Werten wie „Vorlage übernehmen" mit derselben Vorlage.</item>
    /// <item><b>Rundlauf</b> — Zeilenbild und Stundenprofile, die der Editor setzt, kommen über „Speichern" (Hülle,
    /// <see cref="RaumnutzungCtrl.ProfilAnlegen"/>/<see cref="RaumnutzungCtrl.ProfilAendern"/>) unverändert zurück.</item>
    /// <item><b>Vorschau und Vorschläge</b> — Woche und Teppichbild über dem Entwurf, die Grenzen je Größe, und dass die
    /// Vorschläge beim Umschalten das Ergebnis des bisherigen Wegs nicht ändern.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungBildTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Die Zeilen des Editors sind die des Schemas, ihre Kennungen die des Kerns.</summary>
        [Fact]
        public void Die_Zeilen_des_Editors_sind_die_des_Schemas()
        {
            Assert.Equal(RaumnutzungSchema.ZEILEN.OrderBy(z => z, StringComparer.Ordinal),
                         RaumnutzungBild.Zeilen.Select(RaumnutzungBild.Kennung).OrderBy(z => z, StringComparer.Ordinal));
            Assert.Equal(DbWerte.KOND_ZEILE_TAG, RaumnutzungBild.Kennung(KonditionierungZeile.Tag));
            Assert.Equal(DbWerte.KOND_ZEILE_NACHT, RaumnutzungBild.Kennung(KonditionierungZeile.Nacht));
            Assert.Equal(DbWerte.KOND_ZEILE_WOCHENENDE, RaumnutzungBild.Kennung(KonditionierungZeile.Wochenende));
            Assert.Equal(DbWerte.KOND_ZEILE_FERIEN, RaumnutzungBild.Kennung(KonditionierungZeile.Ferien));
            Assert.Equal(DbWerte.KOND_ZEILE_NENNWERT, RaumnutzungBild.Kennung(KonditionierungZeile.Nennwert));
            Assert.Equal(DbWerte.KOND_ZEILE_SAISON, RaumnutzungBild.Kennung(KonditionierungZeile.Saison));
            Assert.Equal(Enum.GetValues<RaumnutzungTagesart>().Length, RaumnutzungBild.Tagesarten.Count);
        }

        /// <summary>Einheit und Grenzen je Größe kommen aus dem Kern; Anteile in Prozent.</summary>
        [Fact]
        public void Die_Grenzen_je_Groesse_sind_die_des_Kerns()
        {
            foreach (KonditionierungGroesse g in RaumnutzungBild.Groessen)
            {
                RaumnutzungBildgrenzen b = RaumnutzungBildHuelle.Grenzen(g);
                Konditionierungsgroesse k = Konditionierungsgroessen.Alle[(int)g];
                Assert.Equal(Kalenderteppich.Einheit(k), b.Einheit);
                Assert.Equal(Konditionierungsgroessen.Min(k) * b.Faktor, b.Min);
                Assert.Equal(Konditionierungsgroessen.Max(k) * b.Faktor, b.Max);
                Assert.Equal(g is KonditionierungGroesse.Heizen or KonditionierungGroesse.Kuehlen, b.MitAus);
            }
            Assert.Equal(100.0, RaumnutzungBildHuelle.Grenzen(KonditionierungGroesse.Personen).Faktor);
            Assert.Equal("%", RaumnutzungBildHuelle.Grenzen(KonditionierungGroesse.Geraete).Einheit);
        }

        // =================================================================
        //  Bitgleichheit: Zeilenbild aus der Vorgabe-Matrix = Vorlage
        // =================================================================

        private static string Abdruck(Vorgabezeile z)
            => string.Join("|", z.Groesse, z.Zeile, z.Wert?.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? "∅",
                           z.Aus ? "1" : "0", z.Von?.ToString() ?? "∅", z.Bis?.ToString() ?? "∅",
                           z.BedingtK?.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? "∅");

        private static void Bitgleich(double[] a, double[] b, string wo)
        {
            Assert.Equal(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[i]) == BitConverter.DoubleToInt64Bits(b[i]),
                            wo + ": Stunde " + i + " " + a[i] + " ≠ " + b[i]);
        }

        [Theory]
        [InlineData(RaumnutzungSaat.WOHNEN, DbWerte.KOND_NUTZUNG_WOHNEN)]
        [InlineData(RaumnutzungSaat.BUERO, DbWerte.KOND_NUTZUNG_BUERO)]
        [InlineData(RaumnutzungSaat.SCHULE, DbWerte.KOND_NUTZUNG_SCHULE)]
        public void Ein_Zeilenbild_aus_der_Vorgabe_Matrix_eines_Musters_ist_bitgleich_zur_Vorlage(string muster, string kennung)
        {
            if (!_db.Vorhanden) return;
            var ctrl = new RaumnutzungCtrl();
            long kat = ctrl.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER).Id;
            Raumnutzungsprofil musterprofil = ctrl.Profile(kat).Single(p => p.Bezeichner == muster);
            var vorlagen = new KonditionierungsvorlageCtrl();
            int geprueft = 0;

            foreach (KonditionierungGroesse g in RaumnutzungBild.Groessen)
            {
                Konditionierungsgroesse kg = Konditionierungsgroessen.Alle[(int)g];
                KonditionierungsvorlageCtrl.Vorlage kopf = vorlagen.Liste(kg).FirstOrDefault(x => x.Ausgeliefert && x.Nutzung == kennung);
                if (kopf == null) continue;
                Konditionierungsvorlage vorlage = vorlagen.Inhalt(kopf.Id, out string m);
                Assert.True(vorlage != null, m);
                List<Vorgabezeile> matrix = vorlage.Inhalt.Vorgabezeilen();
                Assert.All(matrix, z => Assert.Contains(z.Zeile, RaumnutzungSchema.ZEILEN));

                // Der Editor baut das Zeilenbild Zeile für Zeile auf — wie ein Anwender, der die Matrix abschreibt.
                var entwurf = new RaumnutzungProfilDaten
                {
                    IdKategorie = kat, Bezeichner = muster + " (Zeilenbild)", FeiertageWieSonntag = musterprofil.Feiertage_Wie_Sonntag,
                };
                foreach (Vorgabezeile z in matrix)
                    RaumnutzungBild.ZeileSetzen(entwurf, g, RaumnutzungBild.Zeile(z.Zeile)!.Value, z.Wert, z.Aus, z.Von, z.Bis, z.BedingtK);
                Assert.Equal(RaumnutzungBildweg.Zeilenbild, RaumnutzungBild.Weg(entwurf, g));
                Raumnutzungsprofil profil = RaumnutzungHuelle.Kern(entwurf);
                Assert.Null(RaumnutzungCtrl.Profilpruefung(profil));

                // Dieselben Vorgabezeilen im Generator …
                Raumnutzungsgroesse r = Raumnutzungsgenerator.Erzeugen(profil, kg, null);
                Assert.Equal(Raumnutzungsweg.Zeilenbild, r.Weg);
                Assert.Equal(matrix.Select(Abdruck).OrderBy(x => x, StringComparer.Ordinal),
                             r.Vorlage.Inhalt.Vorgabezeilen().Select(Abdruck).OrderBy(x => x, StringComparer.Ordinal));

                // … und dieselbe Reihe am leeren Ziel wie „Vorlage übernehmen".
                var leer = new Konditionierungsarbeitsstand(
                    Konditionierungsstand.Leer(Kalendereigentuemer.Gebaeude, RaumnutzungCtrl.Vorschaubestand()), null);
                var ort = new Konditionierungsort(kg, null);
                Konditionierungsschritt s = Konditionierungsarbeit.VorlageUebernehmen(leer, ort, vorlage);
                if (s.Rueckfrage) s = Konditionierungsarbeit.VorlageUebernehmen(Konditionierungsarbeit.LuftwechselAufteilen(leer).Stand, ort, vorlage);
                Assert.True(s.Ok && !s.Rueckfrage, s.Meldung);
                double[] ausVorlage = s.Stand.Ansichtskalender(kg, null).Auswerten(leer.W0, leer.Referenzjahr);

                RaumnutzungCtrl.Profilvorschau v = RaumnutzungCtrl.Vorschau(profil, kg);
                Assert.Equal(Raumnutzungsweg.Zeilenbild, v.Weg);
                Bitgleich(ausVorlage, v.Kalender.Auswerten(leer.W0, leer.Referenzjahr), muster + " " + g);

                // Das ausgelieferte Muster selbst sieht in der Vorschau dieselbe Reihe (NP1: Profil = Vorlage).
                RaumnutzungCtrl.Profilvorschau vm = RaumnutzungCtrl.Vorschau(musterprofil, kg);
                Assert.True(vm.Kalender != null, muster + " " + g + ": Muster ohne Kalender, Weg " + vm.Weg + ", Hinweis " + vm.Hinweis + ", " + vm.Meldung);
                Bitgleich(ausVorlage, vm.Kalender.Auswerten(leer.W0, leer.Referenzjahr), muster + " " + g + " (Muster)");
                geprueft++;
            }
            Assert.True(geprueft >= 3, muster + ": nur " + geprueft + " Größen mit ausgelieferter Vorlage");
        }

        // =================================================================
        //  Rundlauf Speichern/Laden
        // =================================================================

        [Fact]
        public void Zeilenbild_und_Stundenprofil_kommen_ueber_Speichern_unveraendert_zurueck()
        {
            if (!_db.Vorhanden) return;
            RaumnutzungWeg w = RaumnutzungHuelle.Weg();
            Assert.NotNull(w.Bild);
            RaumnutzungErgebnis k = w.KategorieAnlegen("NP4c Rundlauf", "", "");
            Assert.True(k.Ok, k.Meldung);

            var d = new RaumnutzungProfilDaten { IdKategorie = k.Id, Bezeichner = "Rundlauf", NutzungstageWoche = "1111100" };
            RaumnutzungBild.ZeileSetzen(d, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag, 21, false, null, null, null);
            RaumnutzungBild.ZeileSetzen(d, KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht, null, true, 22, 6, null);
            RaumnutzungBild.ZeileSetzen(d, KonditionierungGroesse.Heizen, KonditionierungZeile.Wochenende, 16, false, null, null, null);
            RaumnutzungBild.ZeileSetzen(d, KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht, 1.5, false, 22, 6, 2);
            double[] werktag = Enumerable.Range(0, 24).Select(h => h is >= 8 and < 18 ? 0.8 : 0.05).ToArray();
            double[] frei = Enumerable.Repeat(0.05, 24).ToArray();
            RaumnutzungBild.StundenSetzen(d, KonditionierungGroesse.Personen, RaumnutzungTagesart.Werktag, werktag);
            RaumnutzungBild.StundenSetzen(d, KonditionierungGroesse.Personen, RaumnutzungTagesart.Frei, frei);
            double[] kuehl = Enumerable.Range(0, 24).Select(h => h is >= 8 and < 18 ? 26.0 : double.NaN).ToArray();
            RaumnutzungBild.StundenSetzen(d, KonditionierungGroesse.Kuehlen, RaumnutzungTagesart.Werktag, kuehl);

            RaumnutzungErgebnis e = w.ProfilAnlegen(d);
            Assert.True(e.Ok, e.Meldung);
            RaumnutzungProfilDaten zurueck = w.Profile(k.Id).Single(p => p.Id == e.Id);
            Gleich(d, zurueck);
            Assert.Equal(RaumnutzungBildweg.Zeilenbild, RaumnutzungBild.Weg(zurueck, KonditionierungGroesse.Heizen));
            Assert.Equal(RaumnutzungBildweg.Stundenprofil, RaumnutzungBild.Weg(zurueck, KonditionierungGroesse.Personen));
            Assert.Equal(RaumnutzungBildweg.Kennwerte, RaumnutzungBild.Weg(zurueck, KonditionierungGroesse.Geraete));
            Assert.True(double.IsNaN(RaumnutzungBild.Stunden(zurueck, KonditionierungGroesse.Kuehlen, RaumnutzungTagesart.Frei)![0]));

            // Umschalten im Entwurf (Heizen auf Kennwerte, Personen auf ein Zeilenbild), dann „Speichern" = ProfilAendern.
            RaumnutzungBild.Entfernen(zurueck, KonditionierungGroesse.Heizen, zeilenbild: true, stunden: true);
            RaumnutzungBild.Entfernen(zurueck, KonditionierungGroesse.Personen, zeilenbild: false, stunden: true);
            RaumnutzungBild.ZeileSetzen(zurueck, KonditionierungGroesse.Personen, KonditionierungZeile.Tag, 0.5, false, null, null, null);
            e = w.ProfilAendern(zurueck);
            Assert.True(e.Ok, e.Meldung);
            RaumnutzungProfilDaten wieder = w.Profile(k.Id).Single(p => p.Id == zurueck.Id);
            Gleich(zurueck, wieder);
            Assert.Equal(RaumnutzungBildweg.Kennwerte, RaumnutzungBild.Weg(wieder, KonditionierungGroesse.Heizen));
            Assert.Equal(RaumnutzungBildweg.Zeilenbild, RaumnutzungBild.Weg(wieder, KonditionierungGroesse.Personen));

            // Ein Wert außerhalb der Grenzen wird beim Speichern benannt abgelehnt (NP-F11).
            RaumnutzungBild.StundeSetzen(wieder, KonditionierungGroesse.Geraete, RaumnutzungTagesart.Werktag, 3, 1.5);
            e = w.ProfilAendern(wieder);
            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
        }

        private static void Gleich(RaumnutzungProfilDaten a, RaumnutzungProfilDaten b)
        {
            Assert.Equal(a.Zeilenbild.OrderBy(z => z.Groesse).ThenBy(z => z.Zeile, StringComparer.Ordinal),
                         b.Zeilenbild.OrderBy(z => z.Groesse).ThenBy(z => z.Zeile, StringComparer.Ordinal));
            Assert.Equal(a.Stunden.Count, b.Stunden.Count);
            foreach (RaumnutzungStundenDaten s in a.Stunden)
            {
                RaumnutzungStundenDaten t = b.Stunden.Single(x => x.Groesse == s.Groesse && x.Tagesart == s.Tagesart);
                Assert.Equal(s.Werte.Select(v => double.IsNaN(v) ? "aus" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture)),
                             t.Werte.Select(v => double.IsNaN(v) ? "aus" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
            }
        }

        // =================================================================
        //  Vorschau und Vorschläge
        // =================================================================

        private static RaumnutzungProfilDaten Kennwertprofil() => new()
        {
            Bezeichner = "Kennwerte", NutzungVon = 8, NutzungBis = 18, NutzungstageWoche = "1111100", FeiertageWieSonntag = true,
            HeizSoll = 21, HeizSollAusserhalb = 17, PersonenAnteil = 1, PersonenAnteilAusserhalb = 0,
            Aussenluft = 2, AussenluftAusserhalb = 0.5, LuftEinheit = RaumnutzungLuftEinheit.JeStunde,
        };

        [Fact]
        public void Die_Vorschau_liefert_Woche_und_Teppichbild_und_benennt_eine_unbelegte_Groesse()
        {
            RaumnutzungBildTexte t = RaumnutzungBildHuelle.Texte();
            RaumnutzungProfilDaten d = Kennwertprofil();

            RaumnutzungBildvorschau heizen = RaumnutzungBildHuelle.Vorschau(d, KonditionierungGroesse.Heizen, t);
            Assert.True(heizen.Belegt);
            Assert.Equal(RaumnutzungBildweg.Kennwerte, heizen.Weg);
            Assert.Equal(168, heizen.Woche!.Length);
            Assert.Equal(21.0, heizen.Woche[10]);           // Montag 10 Uhr
            Assert.Equal(17.0, heizen.Woche[3]);            // Montag 3 Uhr
            Assert.Equal(17.0, heizen.Woche[6 * 24 + 10]);  // Sonntag
            Assert.NotNull(heizen.Teppich);
            Assert.Contains("2025", heizen.Bezug);

            RaumnutzungBildvorschau personen = RaumnutzungBildHuelle.Vorschau(d, KonditionierungGroesse.Personen, t);
            Assert.Equal(100.0, personen.Woche![10]);         // Anteil in Prozent

            RaumnutzungBildvorschau geraete = RaumnutzungBildHuelle.Vorschau(d, KonditionierungGroesse.Geraete, t);
            Assert.False(geraete.Belegt);
            Assert.Null(geraete.Woche);
            Assert.Null(geraete.Teppich);
        }

        [Fact]
        public void Die_Vorschlaege_beim_Umschalten_aendern_das_Ergebnis_nicht()
        {
            RaumnutzungProfilDaten d = Kennwertprofil();
            // Auch das Stundenprofil der Personen: es bringt seine Woche selbst mit und braucht keine Personenzeile des
            // neutralen Ziels (siehe Die_Vorschau_zeigt_das_Stundenprofil_der_Personen_an_einem_Ziel_ohne_Personenzeilen).
            foreach (KonditionierungGroesse g in new[] { KonditionierungGroesse.Heizen, KonditionierungGroesse.Personen, KonditionierungGroesse.Lueftung })
            {
                Konditionierungsgroesse kg = Konditionierungsgroessen.Alle[(int)g];
                double[] vorher = RaumnutzungCtrl.Vorschau(RaumnutzungHuelle.Kern(d), kg).Woche;

                // Zeilenbild aus den Kennwerten: dieselbe Woche.
                RaumnutzungProfilDaten z = d.Kopie();
                RaumnutzungBild.ZeilenbildSetzen(z, g, RaumnutzungBildHuelle.Zeilenbildvorschlag(d, g));
                Assert.Equal(RaumnutzungBildweg.Zeilenbild, RaumnutzungBild.Weg(z, g));
                Bitgleich(vorher, RaumnutzungCtrl.Vorschau(RaumnutzungHuelle.Kern(z), kg).Woche, g + " Zeilenbild");

                // Stundenprofil aus dem bisherigen Weg: dieselbe Woche (an der Lüftung als Anteile der Außenluft).
                RaumnutzungProfilDaten s = d.Kopie();
                RaumnutzungBild.StundenprofilSetzen(s, g, RaumnutzungBildHuelle.Stundenvorschlag(d, g));
                Assert.Equal(RaumnutzungBildweg.Stundenprofil, RaumnutzungBild.Weg(s, g));
                RaumnutzungCtrl.Profilvorschau vs = RaumnutzungCtrl.Vorschau(RaumnutzungHuelle.Kern(s), kg);
                Assert.True(vs.Woche != null, g + ": Stundenprofil ohne Woche, Weg " + vs.Weg + ", Hinweis " + vs.Hinweis + ", " + vs.Meldung);
                Bitgleich(vorher, vs.Woche, g + " Stundenprofil");
            }
            Assert.Empty(RaumnutzungBildHuelle.Zeilenbildvorschlag(d, KonditionierungGroesse.Geraete));
            Assert.Empty(RaumnutzungBildHuelle.Stundenvorschlag(d, KonditionierungGroesse.Geraete));
        }

        /// <summary>
        /// Lehnt der Übernahme        /// <summary>
        /// Ein Stundenprofil der Personen an einem Ziel ohne Personenzeilen (das neutrale Vorschauziel trägt keine): Der
        /// Übernahmeschritt bildet den Fahrplan aus der Woche des Profils und braucht keine Zeile des Ziels — die Vorschau
        /// ist belegt und zeigt das Stundenprofil, ohne Meldung (Befund NP4c, Auftrag NP4d).
        /// </summary>
        [Fact]
        public void Die_Vorschau_zeigt_das_Stundenprofil_der_Personen_an_einem_Ziel_ohne_Personenzeilen()
        {
            RaumnutzungProfilDaten d = Kennwertprofil();
            double[] tag = Enumerable.Range(0, 24).Select(h => h is >= 8 and < 18 ? 1.0 : 0.0).ToArray();
            RaumnutzungBild.StundenSetzen(d, KonditionierungGroesse.Personen, RaumnutzungTagesart.Werktag, tag);

            RaumnutzungCtrl.Profilvorschau k = RaumnutzungCtrl.Vorschau(RaumnutzungHuelle.Kern(d), Konditionierungsgroesse.Personen);
            Assert.Equal(Raumnutzungsweg.Stundenprofil, k.Weg);
            Assert.True(k.Woche != null, "ohne Woche: " + k.Meldung);
            Assert.Null(k.Meldung);
            // Ohne freien Tag gilt der Werktag an allen Tagen; die typische Woche liegt ohne Ferien und Feiertage.
            Bitgleich(Enumerable.Range(0, 168).Select(i => tag[i % 24]).ToArray(), k.Woche, "Personen");

            RaumnutzungBildvorschau v = RaumnutzungBildHuelle.Vorschau(d, KonditionierungGroesse.Personen, RaumnutzungBildHuelle.Texte());
            Assert.Equal(RaumnutzungBildweg.Stundenprofil, v.Weg);
            Assert.True(v.Belegt, v.Hinweis);
            Assert.Equal(100.0, v.Woche![10]);               // Montag 10 Uhr, Anteil in Prozent
            Assert.Equal(0.0, v.Woche[3]);                   // Montag 3 Uhr
            Assert.NotNull(v.Teppich);
            Assert.DoesNotContain("PERSONEN", v.Hinweis ?? "");
        }
    }
}
