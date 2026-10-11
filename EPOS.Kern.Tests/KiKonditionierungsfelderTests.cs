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
    /// <para><b>Was geprüft wird:</b> die 47 Schlüssel nach ihrem Muster, eindeutig und gültig; die
    /// Bestandszellen genau die des Kerns (<see cref="Matrixzellenort"/>) unter ihren Katalognamen, und
    /// nur der Kühlsollwert der Nacht kommt neu dazu; die Zellen, die es gibt, dieselben wie im Reiter
    /// (<see cref="KonditionierungBearbeitung.Gibt"/>); Spalte und Zeile auf den Plätzen der Oberfläche;
    /// Typ, Einheit und Grenzen der Katalogfelder; je Größe die Vorlage als Wahl (Welle U2), vor den Spalten
    /// die Abkürzung „alle Größen" (E57, Welle U5) und das Aktionswissen der Vorlagen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KiKonditionierungsfelderTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly Regex MUSTER = new(
            "^kond_((heizen|kuehlen|lueftung|geraete|personen)_((nennwert|tag|nacht|wochenende|ferien|saison)(_aus|_von|_bis|_dt)?|vorlage|woche)|vorlage_alle)$");

        [Fact]
        public void Die_Karte_fuehrt_47_Felder_nach_ihrem_Muster()
        {
            // 41 Felder der Matrix und der Vorlagen (Wellen U1, U2), dazu je Größe die Woche (Welle U3) und vor
            // den Spalten die Abkürzung „alle Größen" (E57, Welle U5).
            IReadOnlyList<KiKonditionierungsfelder.Feld> alle = KiKonditionierungsfelder.Alle;
            Assert.Equal(47, alle.Count);
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
                if (f.Teil is KiKonditionierungsfelder.Teil.Vorlage or KiKonditionierungsfelder.Teil.Woche
                    or KiKonditionierungsfelder.Teil.VorlageAlle) continue;   // keine Zelle
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
            Assert.Equal(47, felder.Count);

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
        /// <b>Die Zonenkarte</b> (Stufe KP2, Welle U4; Teilkonzept 3.4, 7.3): dieselben Zellen ohne die
        /// Kühlspalte (an der Zone gesperrt bis KU3), dazu das Nachtfenster der Heizspalte (an der Zone steht
        /// es in der Zelle), ohne Bestandszelle (die Zone führt sie unter eigenen Namen); die Felder zeigen
        /// auf die Sichtklasse des Zonendialogs und sagen „wie Gebäude".
        /// </summary>
        [Fact]
        public void Die_Zonenkarte_fuehrt_die_Matrix_ohne_Kuehlspalte_mit_dem_Heiz_Nachtfenster()
        {
            IReadOnlyList<KiKonditionierungsfelder.Feld> zone = KiKonditionierungsfelder.Zonenfelder;
            // Ohne Kühlspalte, ohne die Vorlagen, die Abkürzung „alle Größen" und die Wochen (die Karten der Zone
            // bieten keine), dazu das Heiz-Nachtfenster.
            int ohneKuehlen = KiKonditionierungsfelder.Alle.Count(f => f.Groesse != Konditionierungsgroesse.Kuehlsoll
                                                                        && f.Teil != KiKonditionierungsfelder.Teil.Vorlage
                                                                        && f.Teil != KiKonditionierungsfelder.Teil.VorlageAlle
                                                                        && f.Teil != KiKonditionierungsfelder.Teil.Woche);
            Assert.Equal(ohneKuehlen + 2, zone.Count);
            Assert.Equal(27, zone.Count);
            Assert.DoesNotContain(zone, f => f.Teil == KiKonditionierungsfelder.Teil.Vorlage);
            Assert.DoesNotContain(zone, f => f.Teil == KiKonditionierungsfelder.Teil.VorlageAlle);
            Assert.Null(KiKonditionierungsfelder.FindeZone("kond_heizen_vorlage"));
            Assert.Null(KiKonditionierungsfelder.FindeZone(KiKonditionierungsfelder.VORLAGE_ALLE));
            Assert.DoesNotContain(zone, f => f.Groesse == Konditionierungsgroesse.Kuehlsoll);
            Assert.DoesNotContain(zone, f => f.Bestandszelle && f.Teil == KiKonditionierungsfelder.Teil.Wert);

            foreach (KiKonditionierungsfelder.Feld f in zone)
            {
                Assert.Same(f, KiKonditionierungsfelder.FindeZone(f.Schluessel));
                Assert.Matches(MUSTER, f.Schluessel);
            }
            Assert.NotNull(KiKonditionierungsfelder.FindeZone("kond_heizen_nacht_von"));
            Assert.NotNull(KiKonditionierungsfelder.FindeZone("kond_heizen_nacht_bis"));
            Assert.Null(KiKonditionierungsfelder.Finde("kond_heizen_nacht_von"));
            Assert.Null(KiKonditionierungsfelder.FindeZone("kuehl_sollwert_nacht"));
            Assert.Null(KiKonditionierungsfelder.FindeZone("kond_kuehlen_tag_aus"));

            var felder = KiKonditionierungsfelder.ZonenDialogfelder().ToDictionary(f => f.Name, StringComparer.Ordinal);
            Assert.Equal(zone.Count, felder.Count);
            Assert.All(felder.Values, f => Assert.StartsWith("ZonenKiSicht.", f.Eigenschaftspfad, StringComparison.Ordinal));
            Assert.Contains("wie Gebäude", felder["kond_personen_nennwert"].Erlaeuterung, StringComparison.Ordinal);
            Assert.Contains("wie Gebäude", felder["kond_heizen_nacht_von"].Erlaeuterung, StringComparison.Ordinal);
            Assert.Contains("wie Gebäude", felder["kond_heizen_saison_bis"].Erlaeuterung, StringComparison.Ordinal);
            Assert.Equal("Heizen · Nachtfenster von", felder["kond_heizen_nacht_von"].Anzeigename);
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
        /// <b>Die Abkürzung „alle Größen"</b> (E57; Stufe KP2, Welle U5): <c>kond_vorlage_alle</c> steht VOR den
        /// Spalten — die Liste in der Kopfzelle der Zeile „Vorlage", keine Zelle und keine Größe (Platz −1). Eine
        /// WAHL mit der Aktion der einen Rückfrage für alle Größen, nicht leer setzbar; die Zonenkarte führt sie
        /// nicht (die Karten der Zone bieten keine Vorlagen).
        /// </summary>
        [Fact]
        public void Die_Abkuerzung_alle_Groessen_ist_eine_Wahl_vor_den_Spalten()
        {
            KiKonditionierungsfelder.Feld f = KiKonditionierungsfelder.Finde(KiKonditionierungsfelder.VORLAGE_ALLE);
            Assert.NotNull(f);
            Assert.Equal("kond_vorlage_alle", f.Schluessel);
            Assert.Equal(KiKonditionierungsfelder.Teil.VorlageAlle, f.Teil);
            Assert.Equal(-1, f.Groessenplatz);
            Assert.Equal(-1, f.Zeilenplatz);
            Assert.Null(f.Zeile);
            Assert.False(f.Bestandszelle);
            Assert.Same(f, KiKonditionierungsfelder.Alle[0]);
            Assert.Single(KiKonditionierungsfelder.Alle, x => x.Teil == KiKonditionierungsfelder.Teil.VorlageAlle);
            Assert.Null(KiKonditionierungsfelder.FindeZone(f.Schluessel));

            KiDialogFeld feld = KiKonditionierungsfelder.Dialogfelder().Single(x => x.Name == "kond_vorlage_alle");
            Assert.Equal(KiParameterTyp.Wahl, feld.Typ);
            Assert.True(feld.IstWahl);
            Assert.False(feld.LeerErlaubt);
            Assert.False(feld.NurLesen);
            Assert.Equal("Vorlage · alle Größen", feld.Anzeigename);
            Assert.Equal("GebaeudeKatalogKiSicht.kond_vorlage_alle", feld.Eigenschaftspfad);
            Assert.Contains("EINE Rückfrage", feld.Erlaeuterung);
            Assert.Contains("„Zurücknehmen“", feld.Erlaeuterung);
            Assert.Contains("gleichnamige Vorlage", feld.Erlaeuterung);
            using (new Kulturvorrichtung("en-US"))
            {
                KiDialogFeld en = KiKonditionierungsfelder.Dialogfelder().Single(x => x.Name == "kond_vorlage_alle");
                Assert.Equal("Template · all quantities", en.Anzeigename);
                Assert.Contains("ONE question", en.Erlaeuterung);
            }
        }

        /// <summary>
        /// <b>Das Aktionswissen der Vorlagen</b> (Welle U2, Muster U1): „Vorlage übernehmen", „Als Vorlage
        /// speichern" und „Vorlagen verwalten" stehen im eingebauten Wissen des Bereichs Gebäude, mit
        /// deutschen und englischen Suchworten im Titel — die Suche findet sie in beiden Sprachen.
        /// </summary>
        /// <summary>
        /// <b>Die Woche je Größe</b> (Welle U3): <c>kond_&lt;größe&gt;_woche</c> schließt die Spalte ab, ein Textfeld
        /// ohne Zelle, das die Erläuterung mit Größe und Einheit nennt; die Zonenkarte führt sie nicht.
        /// </summary>
        [Fact]
        public void Je_Groesse_ist_die_Woche_ein_Textfeld_am_Ende_der_Spalte()
        {
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                List<KiKonditionierungsfelder.Feld> spalte = KiKonditionierungsfelder.Alle.Where(f => f.Groesse == g).ToList();
                KiKonditionierungsfelder.Feld woche = spalte.Last();
                Assert.Equal(KiKonditionierungsfelder.Teil.Woche, woche.Teil);
                Assert.EndsWith("_woche", woche.Schluessel);
                Assert.Equal(-1, woche.Zeilenplatz);
                Assert.Null(KiKonditionierungsfelder.FindeZone(woche.Schluessel));
            }
            KiDialogFeld heizen = KiKonditionierungsfelder.Dialogfelder().Single(f => f.Name == "kond_heizen_woche");
            Assert.Equal(KiParameterTyp.Text, heizen.Typ);
            Assert.Contains("168", heizen.Erlaeuterung);
            Assert.Contains("Heizen", heizen.Erlaeuterung);
            Assert.Contains("°C", heizen.Erlaeuterung);
        }

        /// <summary>
        /// <b>Das Aktionswissen der Karte im Einzelnen</b> (Welle U3): Zeitfenster, Periodenliste, Feiertage und
        /// Teppichbild — gefunden mit deutschen und englischen Suchworten im Bereich Gebäude.
        /// </summary>
        [Fact]
        public void Das_Aktionswissen_kennt_Zeitfenster_Periodenliste_Feiertage_und_Teppichbild()
        {
            const string KARTE = "Konditionierung: Kalender im Einzelnen, Grundangabe, Standardwoche und Zeitfenster (calendar details, base value, standard week, time window)";
            const string PERIODEN = "Konditionierung: Periodenliste und Rang (period list, rank, date range, public holiday)";
            const string FEIERTAGE = "Konditionierung: Feiertage als Regel und Zeitstruktur übernehmen (public holidays, apply time structure)";
            const string TEPPICH = "Konditionierung: Teppichbild des Kalenders (carpet plot, annual view)";
            (string Frage, string Titel)[] faelle =
            {
                ("Zeitfenster", KARTE), ("time window", KARTE), ("Standardwoche", KARTE),
                ("Periodenliste", PERIODEN), ("period list", PERIODEN),
                ("Feiertage als Regel", FEIERTAGE), ("apply time structure", FEIERTAGE),
                ("Teppichbild", TEPPICH), ("carpet plot", TEPPICH),
            };
            foreach ((string frage, string titel) in faelle)
            {
                WissensAbschnitt a = Assert.Single(HilfeWissen.Abschnitte, x => x.Titel == titel);
                Assert.Equal(KiChatKontext.B_GEBAEUDE, a.Bereich);
                Assert.Contains(HilfeWissen.Suchen(frage, KiChatKontext.B_GEBAEUDE, 4, ""), x => x.Titel == titel);
            }
        }

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
                // Die Abkürzung nach E57 (Welle U5) steht im Abschnitt „Vorlage übernehmen".
                ("gleichnamige Vorlage in allen Größen", "Konditionierung: Vorlage übernehmen (apply template)"),
            };
            foreach ((string frage, string titel) in faelle)
            {
                WissensAbschnitt a = Assert.Single(HilfeWissen.Abschnitte, x => x.Titel == titel);
                Assert.Equal(KiChatKontext.B_GEBAEUDE, a.Bereich);
                Assert.Contains(HilfeWissen.Suchen(frage, KiChatKontext.B_GEBAEUDE, 4, ""), x => x.Titel == titel);
            }

            // Die Abkürzung im Wortlaut: die Zeile „Vorlage", EINE Rückfrage, Größen ohne gleichnamige Vorlage bleiben.
            WissensAbschnitt uebernehmen = Assert.Single(HilfeWissen.Abschnitte,
                                                         x => x.Titel == "Konditionierung: Vorlage übernehmen (apply template)");
            Assert.Contains("'Gleichnamige Vorlage in allen Größen übernehmen'", uebernehmen.Inhalt);
            Assert.Contains("Zeile 'Vorlage'", uebernehmen.Inhalt);
            Assert.Contains("EINE Rückfrage", uebernehmen.Inhalt);
            Assert.Contains("Größen ohne gleichnamige Vorlage bleiben", uebernehmen.Inhalt);
        }
    }
}
