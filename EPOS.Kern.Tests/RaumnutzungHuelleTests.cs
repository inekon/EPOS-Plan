using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des Blatts „Nutzungsprofile"</b> (Stufe NP3a; Konzept Nutzungsprofile 6.1) gegen die
    /// Testdatenbank: dass der Weg die gesäten Kategorien und Profile als DTO liefert, dass die Übersetzung in
    /// beide Richtungen verlustfrei ist (Kennwerte, Einheit der Außenluft, Zeilenbild, Stundenprofile), dass
    /// die Vorschau je Größe rechnet, ohne zu schreiben, und dass eine Ablehnung benannt zurückkommt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die vier gesäten Kategorien mit ihren Arten und dem Schloss (NP-F3, NP-F4).</summary>
        [Fact]
        public void Der_Weg_liefert_die_vier_gesaeten_Kategorien_mit_Art_und_Schloss()
        {
            RaumnutzungWeg w = RaumnutzungHuelle.Weg();
            Assert.True(w.MitKatalog);
            Assert.Null(w.Sperrgrund);

            IReadOnlyList<RaumnutzungKategorieDaten> kategorien = w.Kategorien();
            Assert.Equal(RaumnutzungSaat.Kategorien.Count, kategorien.Count);
            Assert.Equal(4, kategorien.Count);
            Assert.All(kategorien, k => Assert.True(k.Ausgeliefert, k.Bezeichner + " ist nicht ausgeliefert"));
            Assert.Equal(new[]
                {
                    RaumnutzungArt.EposMuster, RaumnutzungArt.Din18599,
                    RaumnutzungArt.Sia2024, RaumnutzungArt.Vdi2078,
                },
                kategorien.Select(k => k.Art).ToArray());
            Assert.All(kategorien.Where(k => k.IstNorm), k => Assert.NotEqual("", k.Quellenhinweis));
        }

        /// <summary>
        /// Alle gesäten Profile zusammen (33, NP1b) kommen als DTO; die DIN-Profile sind leer (NP-F6, NP-F13),
        /// die EPOS-Muster tragen Werte oder ein Zeilenbild (NP-F7).
        /// </summary>
        [Fact]
        public void Der_Weg_liefert_die_gesaeten_Profile_je_Kategorie()
        {
            RaumnutzungWeg w = RaumnutzungHuelle.Weg();
            List<RaumnutzungProfilDaten> alle = w.Kategorien()
                .SelectMany(k => w.Profile(k.Id)).ToList();

            Assert.Equal(33, alle.Count);
            Assert.All(alle, p => Assert.True(p.Ausgeliefert));

            RaumnutzungKategorieDaten muster = w.Kategorien().Single(k => k.Art == RaumnutzungArt.EposMuster);
            IReadOnlyList<RaumnutzungProfilDaten> musterprofile = w.Profile(muster.Id);
            Assert.NotEmpty(musterprofile);
            // Die gepflegten Muster tragen Werte oder ein Zeilenbild; das Sammelmuster bleibt bewusst leer.
            Assert.True(musterprofile.Count(p => !p.IstLeer) >= musterprofile.Count - 1,
                        "Mehr als ein EPOS-Muster ist leer.");
            Assert.Contains(musterprofile, p => p.Zeilenbild.Count > 0);

            RaumnutzungKategorieDaten din = w.Kategorien().Single(k => k.Art == RaumnutzungArt.Din18599);
            IReadOnlyList<RaumnutzungProfilDaten> dinprofile = w.Profile(din.Id);
            Assert.NotEmpty(dinprofile);
            Assert.All(dinprofile, p => Assert.True(p.IstLeer, p.Bezeichner + " trägt Werte"));
            // Die Nummer der Quelle ist Text und bleibt es (NP-F5).
            Assert.Contains(dinprofile, p => p.Nummer == "1");
        }

        /// <summary>Die gesäten Zuordnungszeilen kommen mit Art, Schlüssel und Schloss (NP-F12).</summary>
        [Fact]
        public void Der_Weg_liefert_die_gesaeten_Zuordnungszeilen()
        {
            IReadOnlyList<RaumnutzungZuordnungDaten> zeilen = RaumnutzungHuelle.Weg().Zuordnungen();

            Assert.NotEmpty(zeilen);
            Assert.All(zeilen, z => Assert.True(z.Ausgeliefert));
            Assert.Contains(zeilen, z => z.Art == RaumnutzungZuordnungsart.DinNummer);
            Assert.Contains(zeilen, z => z.Art == RaumnutzungZuordnungsart.IfcKlasse);
            Assert.All(zeilen, z => Assert.NotEqual("", z.Schluessel));
            // Jede gesaete Zeile zeigt auf ein Profil des Katalogs oder auf "keine" (NP-F12).
            RaumnutzungWeg w = RaumnutzungHuelle.Weg();
            var ids = new HashSet<long>(w.Kategorien().SelectMany(k => w.Profile(k.Id)).Select(p => p.Id));
            Assert.All(zeilen, z => Assert.True(z.IdProfil is null || ids.Contains(z.IdProfil.Value),
                                                z.Schluessel + " zeigt ins Leere"));
        }

        /// <summary>
        /// Die Übersetzung in beide Richtungen ist verlustfrei: Kennwerte, Einheit der Außenluft (NP-F10),
        /// Zeilenbild (NP-F7) und Stundenprofile (NP-F9) kommen unverändert zurück.
        /// </summary>
        [Fact]
        public void Die_Uebersetzung_in_beide_Richtungen_ist_verlustfrei()
        {
            var ctrl = new RaumnutzungCtrl();
            foreach (Raumnutzungsprofil kern in ctrl.Profile())
            {
                RaumnutzungProfilDaten d = RaumnutzungHuelle.Profil(kern);
                Raumnutzungsprofil zurueck = RaumnutzungHuelle.Kern(d);

                Assert.Equal(kern.Id, zurueck.Id);
                Assert.Equal(kern.IdKatalog, zurueck.IdKatalog);
                Assert.Equal(kern.Nummer, zurueck.Nummer);
                Assert.Equal(kern.Bezeichner, zurueck.Bezeichner);
                Assert.Equal(kern.Beschreibung, zurueck.Beschreibung);
                Assert.Equal(kern.Kennwerte(), zurueck.Kennwerte());
                Assert.Equal(kern.Aussenluft_Einheit, zurueck.Aussenluft_Einheit);
                Assert.Equal(kern.Zeilen.Count, zurueck.Zeilen.Count);
                Assert.Equal(kern.Stunden.Count, zurueck.Stunden.Count);
                Assert.Equal(kern.IstLeer, d.IstLeer);

                foreach ((Vorgabezeile a, Vorgabezeile b) in kern.Zeilen.Zip(zurueck.Zeilen))
                {
                    Assert.Equal(a.Groesse, b.Groesse);
                    Assert.Equal(a.Zeile, b.Zeile);
                    Assert.Equal(a.Wert, b.Wert);
                    Assert.Equal(a.Aus, b.Aus);
                    Assert.Equal(a.Von, b.Von);
                    Assert.Equal(a.Bis, b.Bis);
                }
            }
        }

        /// <summary>
        /// Die Vorschau rechnet je Größe über den Generator, nennt die nicht belegten Größen (NP-F6) und
        /// schreibt dabei nichts — der Katalog bleibt Zeile für Zeile, wie er war.
        /// </summary>
        [Fact]
        public void Die_Vorschau_rechnet_je_Groesse_und_schreibt_nichts()
        {
            RaumnutzungWeg w = RaumnutzungHuelle.Weg();
            RaumnutzungKategorieDaten muster = w.Kategorien().Single(k => k.Art == RaumnutzungArt.EposMuster);
            RaumnutzungProfilDaten profil = w.Profile(muster.Id).First(p => !p.IstLeer);
            int profileVorher = w.Kategorien().Sum(k => w.Profile(k.Id).Count);

            RaumnutzungVorschau v = w.Vorschau(profil, 120.0, 2.5);

            Assert.Equal(5, v.Groessen.Count);
            Assert.Contains(v.Groessen, g => g.Belegt && g.Zeilen.Count > 0);
            Assert.All(v.Groessen.Where(g => !g.Belegt), g => Assert.Empty(g.Zeilen));
            Assert.Equal(profil.ToString(), v.Profilname);

            // Nichts geschrieben: dieselbe Zahl der Profile, dasselbe Profil.
            Assert.Equal(profileVorher, w.Kategorien().Sum(k => w.Profile(k.Id).Count));
            RaumnutzungProfilDaten danach = w.Profile(muster.Id).Single(p => p.Id == profil.Id);
            Assert.Equal(profil.Kennwerte(), danach.Kennwerte());
        }

        /// <summary>
        /// Ein ausgeliefertes Profil lässt sich nicht ändern — die Ablehnung kommt als Text (NP-F19), und ein
        /// Duplikat in einer eigenen Kategorie geht; danach steht der Katalog wieder wie gesät.
        /// </summary>
        [Fact]
        public void Ein_ausgeliefertes_Profil_wird_benannt_abgelehnt_und_das_Duplikat_geht()
        {
            RaumnutzungWeg w = RaumnutzungHuelle.Weg();
            RaumnutzungKategorieDaten muster = w.Kategorien().Single(k => k.Art == RaumnutzungArt.EposMuster);
            RaumnutzungProfilDaten profil = w.Profile(muster.Id).First();

            RaumnutzungProfilDaten geaendert = profil.Kopie();
            geaendert.HeizSoll = 23;
            RaumnutzungErgebnis abgelehnt = w.ProfilAendern(geaendert);
            Assert.False(abgelehnt.Ok);
            Assert.NotEqual("", abgelehnt.Meldung);

            RaumnutzungErgebnis kategorie = w.KategorieAnlegen("Probe NP3a", "", "");
            Assert.True(kategorie.Ok, kategorie.Meldung);
            RaumnutzungErgebnis duplikat = w.ProfilDuplizieren(profil.Id, kategorie.Id, "Probe NP3a Profil");
            Assert.True(duplikat.Ok, duplikat.Meldung);

            RaumnutzungProfilDaten kopie = RaumnutzungHuelle.Weg().Profile(kategorie.Id)
                .Single(p => p.Id == duplikat.Id);
            Assert.False(kopie.Ausgeliefert);
            Assert.Equal(profil.Kennwerte(), kopie.Kennwerte());

            // Die Kopie ist änderbar — jetzt greift derselbe Weg.
            kopie.HeizSoll = 23;
            RaumnutzungErgebnis geschrieben = w.ProfilAendern(kopie);
            Assert.True(geschrieben.Ok, geschrieben.Meldung);
            Assert.Equal(23, RaumnutzungHuelle.Weg().Profile(kategorie.Id).Single(p => p.Id == kopie.Id).HeizSoll);

            // Aufräumen: die eigene Kategorie samt Profil fällt wieder.
            Assert.True(w.KategorieLoeschen(kategorie.Id).Ok);
            Assert.DoesNotContain(RaumnutzungHuelle.Weg().Kategorien(), k => k.Id == kategorie.Id);
        }

        /// <summary>
        /// Ein Kennwert außerhalb der Grenzen wird benannt abgelehnt (NP-F11) — die Prüfung steht im Kern, die
        /// Hülle reicht nur den Text weiter.
        /// </summary>
        [Fact]
        public void Ein_Kennwert_ausserhalb_der_Grenzen_wird_benannt_abgelehnt()
        {
            RaumnutzungWeg w = RaumnutzungHuelle.Weg();
            RaumnutzungErgebnis kategorie = w.KategorieAnlegen("Probe NP3a Grenzen", "", "");
            Assert.True(kategorie.Ok, kategorie.Meldung);
            try
            {
                var profil = new RaumnutzungProfilDaten
                {
                    IdKategorie = kategorie.Id,
                    Bezeichner = "Grenzprobe",
                    PersonenAnteil = Konditionierungsgroessen.ANTEIL_MAX + 0.5,
                };
                RaumnutzungErgebnis e = w.ProfilAnlegen(profil);
                Assert.False(e.Ok);
                Assert.NotEqual("", e.Meldung);
                Assert.Empty(RaumnutzungHuelle.Weg().Profile(kategorie.Id));
            }
            finally
            {
                w.KategorieLoeschen(kategorie.Id);
            }
        }

        /// <summary>
        /// Das Textbündel kommt vollständig aus den Ressourcen: kein Text leer, und die Fragen tragen ihre
        /// Platzhalter (die Oberfläche setzt Namen und Zahlen ein).
        /// </summary>
        [Fact]
        public void Das_Textbuendel_ist_vollstaendig_und_traegt_seine_Platzhalter()
        {
            RaumnutzungTexte t = RaumnutzungHuelle.Texte();

            Assert.All(typeof(RaumnutzungTexte).GetProperties(),
                       e => Assert.False(string.IsNullOrWhiteSpace((string)e.GetValue(t)),
                                         e.Name + " ist leer"));
            Assert.Contains("{0}", t.FrageProfilLoeschen);
            Assert.Contains("{0}", t.FrageProfilZuordnung);
            Assert.Contains("{0}", t.FrageKategorieLoeschen);
            Assert.Contains("{0}", t.FrageZuordnungLoeschen);
            Assert.Contains("{0}", t.TextTage);
            Assert.Contains("{1}", t.TextTage);
        }
    }
}
