using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Feldkarte der Vorgabe-Matrix</b> (<see cref="KiKonditionierungsfelder"/>; Stufe KP2, Welle
    /// U1): EIN Profil für den Dialogkatalog und die Feldtafel der Sichtklasse.
    ///
    /// <para><b>Was geprüft wird:</b> die 41 Schlüssel nach ihrem Muster, eindeutig und gültig; die
    /// Bestandszellen genau die des Kerns (<see cref="Matrixzellenort"/>) unter ihren Katalognamen, und
    /// nur der Kühlsollwert der Nacht kommt neu dazu; die Zellen, die es gibt, dieselben wie im Reiter
    /// (<see cref="KonditionierungBearbeitung.Gibt"/>); Spalte und Zeile auf den Plätzen der Oberfläche;
    /// Typ, Einheit und Grenzen der Katalogfelder; je Größe die Vorlage als Wahl (Welle U2) und das
    /// Aktionswissen der Vorlagen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KiKonditionierungsfelderTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly Regex MUSTER = new(
            "^kond_(heizen|kuehlen|lueftung|geraete|personen)_((nennwert|tag|nacht|wochenende|ferien|saison)(_aus|_von|_bis|_dt)?|vorlage)$");

        [Fact]
        public void Die_Karte_fuehrt_41_Felder_nach_ihrem_Muster()
        {
            IReadOnlyList<KiKonditionierungsfelder.Feld> alle = KiKonditionierungsfelder.Alle;
            Assert.Equal(41, alle.Count);
            Assert.Equal(alle.Count, alle.Select(f => f.Schluessel).Distinct(StringComparer.Ordinal).Count());

            foreach (KiKonditionierungsfelder.Feld f in alle)
            {
                Assert.True(KiName.IstGueltig(f.Schluessel), f.Schluessel);
                Assert.Same(f, KiKonditionierungsfelder.Finde(f.Schluessel));
                if (f.Schluessel == "kuehl_sollwert_nacht") continue;
                Assert.Matches(MUSTER, f.Schluessel);
            }
            Assert.Null(KiKonditionierungsfelder.Finde("kond_personen_saison_von"));
            Assert.Null(KiKonditionierungsfelder.Finde("kond_heizen_nennwert"));
        }

        /// <summary>
        /// Die Bestandszellen sind die des Kerns — neun, je mit ihrem Katalognamen; acht stehen schon als
        /// Felder im Editor, die Karte führt nur den Kühlsollwert der Nacht neu.
        /// </summary>
        [Fact]
        public void Die_Bestandszellen_behalten_ihre_Namen()
        {
            int bestand = 0;
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                foreach (string z in DbWerte.KOND_ZEILEN)
                {
                    bool spalte = Matrixzellenort.Bestandsspalte(g, z) != null;
                    string name = KiKonditionierungsfelder.Bestandsname(g, z);
                    Assert.Equal(spalte, name != null);
                    if (!spalte) continue;
                    bestand++;
                    Assert.True(KiKonditionierungsfelder.BESTEHEND.Contains(name) || name == "kuehl_sollwert_nacht", name);
                }
            Assert.Equal(9, bestand);

            KiDialog editor = KiDialoge.Katalog.Finde(KiMaskennamen.GEBAEUDE_KATALOG)!;
            foreach (string name in KiKonditionierungsfelder.BESTEHEND)
                Assert.True(editor.KenntFeld(name), name);

            KiKonditionierungsfelder.Feld nacht = KiKonditionierungsfelder.Finde("kuehl_sollwert_nacht");
            Assert.NotNull(nacht);
            Assert.True(nacht.Bestandszelle);
            Assert.Equal(KiKonditionierungsfelder.Teil.Wert, nacht.Teil);
        }

        /// <summary>Die Zellen, die es gibt, sind die des Reiters — Spalte und Zeile auf den Plätzen der Oberfläche.</summary>
        [Fact]
        public void Spalte_und_Zeile_stehen_auf_den_Plaetzen_der_Oberflaeche()
        {
            foreach (KiKonditionierungsfelder.Feld f in KiKonditionierungsfelder.Alle)
            {
                if (f.Teil == KiKonditionierungsfelder.Teil.Vorlage) continue;   // eine Zeile der Karte, keine Zelle
                var g = (KonditionierungGroesse)f.Groessenplatz;
                var z = (KonditionierungZeile)f.Zeilenplatz;
                Assert.True(KonditionierungBearbeitung.Gibt(g, z), f.Schluessel);
                Assert.Equal(DbWerte.KOND_ZEILEN[(int)z], f.Zeile);
                if (f.Teil == KiKonditionierungsfelder.Teil.Aus)
                    Assert.True(KonditionierungBearbeitung.MitAus(g, z), f.Schluessel);
                if (f.Teil == KiKonditionierungsfelder.Teil.Wert)
                    Assert.Equal(f.Bestandszelle, KonditionierungDaten.IstBestandszelle(g, z));
            }
            // Jede Zelle des Reiters, die keine Bestandszelle im Editor ist, hat ihren Wert in der Karte.
            foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
                foreach (KonditionierungZeile z in KonditionierungBearbeitung.Zeilen)
                {
                    if (!KonditionierungBearbeitung.Gibt(g, z) || z == KonditionierungZeile.Saison) continue;
                    bool imEditor = KonditionierungDaten.IstBestandszelle(g, z)
                                    && KiKonditionierungsfelder.BESTEHEND.Contains(
                                        KiKonditionierungsfelder.Bestandsname(Konditionierungsgroessen.Alle[(int)g],
                                                                              DbWerte.KOND_ZEILEN[(int)z]));
                    bool inKarte = KiKonditionierungsfelder.Alle.Any(f => f.Teil == KiKonditionierungsfelder.Teil.Wert
                                                                          && f.Groessenplatz == (int)g && f.Zeilenplatz == (int)z);
                    Assert.True(imEditor ^ inKarte, g + " " + z);
                }
        }

        [Fact]
        public void Die_Katalogfelder_tragen_Typ_Einheit_und_Grenzen_der_Zelle()
        {
            Dictionary<string, KiDialogFeld> felder = KiKonditionierungsfelder.Dialogfelder()
                .ToDictionary(f => f.Name, StringComparer.Ordinal);
            Assert.Equal(41, felder.Count);

            KiDialogFeld geraete = felder["kond_geraete_tag"];
            Assert.Equal(KiParameterTyp.Zahl, geraete.Typ);
            Assert.Equal("%", geraete.Einheit);
            Assert.Equal(100.0, geraete.Max);
            Assert.Equal("Geräte · Tag", geraete.Anzeigename);
            Assert.Equal("GebaeudeKatalogKiSicht.kond_geraete_tag", geraete.Eigenschaftspfad);

            Assert.Equal("W", felder["kond_personen_nennwert"].Einheit);
            Assert.Equal(KiParameterTyp.Wahrheitswert, felder["kond_heizen_tag_aus"].Typ);
            Assert.Equal("Heizen · Tag · aus", felder["kond_heizen_tag_aus"].Anzeigename);

            KiDialogFeld saison = felder["kond_kuehlen_saison_von"];
            Assert.Equal(KiParameterTyp.Ganzzahl, saison.Typ);
            Assert.Equal(1.0, saison.Min);
            Assert.Equal(365.0, saison.Max);

            KiDialogFeld fenster = felder["kond_lueftung_nacht_bis"];
            Assert.Equal(KiParameterTyp.Ganzzahl, fenster.Typ);
            Assert.Equal(0.0, fenster.Min);
            Assert.Equal(23.0, fenster.Max);

            KiDialogFeld dt = felder["kond_lueftung_nacht_dt"];
            Assert.Equal("K", dt.Einheit);
            Assert.Equal(5.0, dt.Max);
            Assert.Equal("Lüftung · Nachtauskühlung", felder["kond_lueftung_nacht"].Anzeigename);

            Assert.Equal(15.0, felder["kuehl_sollwert_nacht"].Min);
            Assert.Equal(35.0, felder["kuehl_sollwert_nacht"].Max);
            Assert.All(felder.Values, f => Assert.False(f.NurLesen));
            Assert.All(felder.Values, f => Assert.False(string.IsNullOrWhiteSpace(f.Erlaeuterung)));
        }

        /// <summary>
        /// <b>Je Größe die Vorlage</b> (Welle U2; Entwurf KP2 D9): <c>kond_&lt;größe&gt;_vorlage</c> ist eine
        /// WAHL aus der Liste der Karte — Setzen trägt die Aktion des Knopfs „Übernehmen", Lesen nennt die
        /// Herkunft. Sie steht in der Zeile „Vorlage" über dem Nennwert, nicht an einer Zelle; leer lässt sie
        /// sich nicht setzen (zurück zur Matrix führt „Verwerfen").
        /// </summary>
        [Fact]
        public void Je_Groesse_ist_die_Vorlage_eine_Wahl_mit_der_Aktion_des_Knopfs()
        {
            string[] wort = { "heizen", "kuehlen", "lueftung", "geraete", "personen" };
            var vorlagen = KiKonditionierungsfelder.Alle.Where(f => f.Teil == KiKonditionierungsfelder.Teil.Vorlage).ToList();
            Assert.Equal(5, vorlagen.Count);
            for (int i = 0; i < wort.Length; i++)
            {
                KiKonditionierungsfelder.Feld f = KiKonditionierungsfelder.Finde("kond_" + wort[i] + "_vorlage");
                Assert.NotNull(f);
                Assert.Equal(i, f.Groessenplatz);
                Assert.Equal(-1, f.Zeilenplatz);
                Assert.Null(f.Zeile);
                Assert.False(f.Bestandszelle);
                // In der Reihenfolge der Matrix: die Zeile „Vorlage" eröffnet die Spalte.
                Assert.Same(f, KiKonditionierungsfelder.Alle.First(x => x.Groessenplatz == i));
            }

            KiDialogFeld heizen = KiKonditionierungsfelder.Dialogfelder().Single(f => f.Name == "kond_heizen_vorlage");
            Assert.Equal(KiParameterTyp.Wahl, heizen.Typ);
            Assert.True(heizen.IstWahl);
            Assert.False(heizen.LeerErlaubt);
            Assert.False(heizen.NurLesen);
            Assert.Equal("Heizen · Vorlage", heizen.Anzeigename);
            Assert.Equal("GebaeudeKatalogKiSicht.kond_heizen_vorlage", heizen.Eigenschaftspfad);
            Assert.Contains("„Übernehmen“", heizen.Erlaeuterung);
            Assert.Contains("Heizen", heizen.Erlaeuterung);
        }

        /// <summary>
        /// <b>Das Aktionswissen der Vorlagen</b> (Welle U2, Muster U1): „Vorlage übernehmen", „Als Vorlage
        /// speichern" und „Vorlagen verwalten" stehen im eingebauten Wissen des Bereichs Gebäude, mit
        /// deutschen und englischen Suchworten im Titel — die Suche findet sie in beiden Sprachen.
        /// </summary>
        [Fact]
        public void Das_Aktionswissen_kennt_die_drei_Handlungen_der_Vorlagen()
        {
            (string Frage, string Titel)[] faelle =
            {
                ("Vorlage übernehmen", "Konditionierung: Vorlage übernehmen (apply template)"),
                ("Als Vorlage speichern", "Konditionierung: Als Vorlage speichern (save as template)"),
                ("Vorlagen verwalten", "Konditionierung: Vorlagen verwalten (manage templates)"),
                ("apply template", "Konditionierung: Vorlage übernehmen (apply template)"),
                ("manage templates", "Konditionierung: Vorlagen verwalten (manage templates)"),
            };
            foreach ((string frage, string titel) in faelle)
            {
                WissensAbschnitt a = Assert.Single(HilfeWissen.Abschnitte, x => x.Titel == titel);
                Assert.Equal(KiChatKontext.B_GEBAEUDE, a.Bereich);
                Assert.Contains(HilfeWissen.Suchen(frage, KiChatKontext.B_GEBAEUDE, 4, ""), x => x.Titel == titel);
            }
        }
    }
}
