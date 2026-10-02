using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache der 14 ausgelieferten Konditionierungsvorlagen</b> (KP-S1b, Entscheid E56 F1 (b);
    /// Konzept Konditionierungsprofile 3.5 und 5.7; Entwurf KP2 Abschnitt 4). Nummer und Saat bei
    /// <see cref="KonditionierungsvorlagenSaatSchema"/> und <see cref="KonditionierungsvorlagenSaattabelle"/>.
    ///
    /// <para><b>Geprüft wird:</b> die Nummer und das Register; die 14 Vorlagen Zelle für Zelle gegen
    /// die Tabelle des Entwurfs (zweite, unabhängige Niederschrift unten), dazu die Feiertagsregeln
    /// bei Büro und Schule; eindeutige Namen je Liste, keine Produktnamen, jede Zelle in ihren Grenzen,
    /// nur die eigene Größe, der E54-Filter (kein Nennwert, keine Saison) und gültige, ausdrückliche
    /// Nachtfenster; der Stand der Testdatenbank; dass der Schritt nur anlegt, was fehlt, eine eigene
    /// gleichnamige Vorlage stehen lässt, wiederholbar ist und aus dem Stand davor alle 14 anlegt;
    /// dass jede Vorlage, auf ein Probegebäude übernommen, einen gültigen Kalender ergibt; dass jede
    /// Vorlage mit Ziel sich „nach …" kopieren lässt und ihr Inhalt dabei nur in der Zielgröße entsteht; die
    /// Verdrahtung in Werkzeug, Migration, Testkopie und Repo-Datei.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben. Die Kultur ist auf de-DE
    /// gepinnt: Ein Fall vergleicht die Feiertagsnamen mit dem deutschen Text der Karte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungsvorlagenWacheTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungsvorlageCtrl _vorlagen = new KonditionierungsvorlageCtrl();
        private readonly KonditionierungCtrl _kond = new KonditionierungCtrl();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private bool Bereit()
            => _db.Vorhanden && KonditionierungSchema.Lesbar() && KonditionierungVorlagenSchema.Lesbar();

        // =============================================================================
        //  Die Tabelle des Entwurfs KP2, Abschnitt 4 - ZWEITE Niederschrift
        // =============================================================================
        //
        // Die Tabelle ist verbindlich (E56 F1 (b)). Sie steht hier ein zweites Mal, von Hand aus dem
        // Entwurf abgeschrieben - Anteile als Anteil 0 ... 1 statt Prozent, "sonst" als Nacht,
        // Wochenende und Ferien, jedes Nachtfenster ausdruecklich; "+F" heisst: die neun Feiertage
        // "wie Sonntag". Eine Aenderung der Saattabelle, die hier nicht mitgeht, ist rot.

        private static readonly string[] ENTWURF =
        {
            "HEIZSOLL/Wohnen: TAG 20 | NACHT 18 (22-6)",
            "HEIZSOLL/Büro: TAG 20 | NACHT 16 (18-7) | WOCHENENDE 16 | FERIEN 16 +F",
            "HEIZSOLL/Schule: TAG 20 | NACHT 16 (15-7) | WOCHENENDE 16 | FERIEN 16 +F",
            "KUEHLSOLL/Wohnen: TAG 26 | NACHT 28 (22-6)",
            "KUEHLSOLL/Büro: TAG 26 | NACHT aus (18-7) | WOCHENENDE aus | FERIEN aus +F",
            "KUEHLSOLL/Schule: TAG 26 | NACHT aus (15-7) | WOCHENENDE aus | FERIEN aus +F",
            "LUEFTUNG/Büro: NACHT 0.1 (18-7) | WOCHENENDE 0.1 | FERIEN 0.1 +F",
            "LUEFTUNG/Schule: NACHT 0.1 (15-7) | WOCHENENDE 0.1 | FERIEN 0.1 +F",
            "GERAETE/Wohnen: TAG 1",
            "GERAETE/Büro: TAG 1 | NACHT 0.1 (18-7) | WOCHENENDE 0.1 | FERIEN 0.1 +F",
            "GERAETE/Schule: TAG 1 | NACHT 0.1 (15-7) | WOCHENENDE 0.1 | FERIEN 0.1 +F",
            "PERSONEN/Wohnen: TAG 0.5 | NACHT 1 (17-7) | WOCHENENDE 1",
            "PERSONEN/Büro: TAG 1 | NACHT 0 (17-8) | WOCHENENDE 0 | FERIEN 0 +F",
            "PERSONEN/Schule: TAG 1 | NACHT 0 (14-8) | WOCHENENDE 0 | FERIEN 0 +F",
        };

        /// <summary>Eine Vorlage der Saat in der Schreibweise von <see cref="ENTWURF"/>.</summary>
        private static string Niederschrift(KonditionierungsvorlagenSaat s)
            => s + ": " + string.Join(" | ", s.Zeilen.Select(z =>
                   z.Zeile + " " + (z.Aus ? "aus" : Zahl(z.Wert.Value)) +
                   (z.Von.HasValue || z.Bis.HasValue ? " (" + z.Von + "-" + z.Bis + ")" : ""))) +
               (s.Feiertage ? " +F" : "");

        // =============================================================================
        //  Teil 1 - die Saattabelle (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf die Kesselkennlinie (156), der Zielstand reicht bis zu ihr, und das Register führt sie als Katalogschritt.</summary>
        [Fact]
        public void Die_Nummer_folgt_auf_die_Kesselkennlinie_und_ist_das_Ziel()
        {
            Assert.Equal(KesselKennlinieSchema.SCHRITT + 1, KonditionierungsvorlagenSaatSchema.SCHRITT);
            Assert.Equal(157, KonditionierungsvorlagenSaatSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KonditionierungsvorlagenSaatSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KonditionierungsvorlagenSaatSchema.SCHRITT + ".");
            Paketanhebung.Stufe stufe = Assert.Single(Paketanhebung.Stufen,
                                                      s => s.Nr == KonditionierungsvorlagenSaatSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, stufe.Wirkung);
        }

        /// <summary>
        /// <b>Die 14 Vorlagen gleich dem Entwurf</b> — Zelle für Zelle, 3 + 3 + 2 + 3 + 3 Vorlagen mit
        /// zusammen 46 Vorgabezeilen, Feiertage genau bei Büro und Schule.
        /// </summary>
        [Fact]
        public void Vierzehn_Vorlagen_gleich_der_Tabelle_des_Entwurfs()
        {
            IReadOnlyList<KonditionierungsvorlagenSaat> saat = KonditionierungsvorlagenSaatSchema.Saat;
            Assert.Equal(ENTWURF, saat.Select(Niederschrift).ToArray());

            Assert.Equal(KonditionierungsvorlagenSaattabelle.VORLAGEN, saat.Count);
            Assert.Equal(KonditionierungsvorlagenSaattabelle.VORGABEZEILEN, saat.Sum(s => s.Zeilen.Count));
            Assert.Equal(new[] { 3, 3, 2, 3, 3 },
                         Konditionierungsgroessen.Alle.Select(g => saat.Count(s => s.Groesse == g)).ToArray());
            Assert.Equal(new[] { 10, 10, 6, 9, 11 },
                         Konditionierungsgroessen.Alle.Select(g => saat.Where(s => s.Groesse == g)
                                                                        .Sum(s => s.Zeilen.Count)).ToArray());
            Assert.DoesNotContain(saat, s => s.Groesse == Konditionierungsgroesse.Lueftung &&
                                             s.Bezeichner == KonditionierungsvorlagenSaattabelle.WOHNEN);
            foreach (KonditionierungsvorlagenSaat s in saat)
                Assert.Equal(s.Bezeichner != KonditionierungsvorlagenSaattabelle.WOHNEN, s.Feiertage);
            Assert.Equal(10, saat.Count(s => s.Feiertage));
        }

        /// <summary>
        /// Eindeutige Namen je Liste (ohne Unterschied von Groß- und Kleinschreibung), die drei
        /// Nutzungsnamen samt passender Nutzung, dieselbe Beschreibung — alles in den Grenzen der Spalten.
        /// </summary>
        [Fact]
        public void Namen_eindeutig_je_Liste_Nutzung_und_Beschreibung_wie_festgelegt()
        {
            var nutzung = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [KonditionierungsvorlagenSaattabelle.WOHNEN] = DbWerte.KOND_NUTZUNG_WOHNEN,
                [KonditionierungsvorlagenSaattabelle.BUERO] = DbWerte.KOND_NUTZUNG_BUERO,
                [KonditionierungsvorlagenSaattabelle.SCHULE] = DbWerte.KOND_NUTZUNG_SCHULE,
            };
            Assert.Equal("Wohnen", KonditionierungsvorlagenSaattabelle.WOHNEN);
            Assert.Equal("Büro", KonditionierungsvorlagenSaattabelle.BUERO);
            Assert.Equal("Schule", KonditionierungsvorlagenSaattabelle.SCHULE);
            Assert.Equal("EPOS-Muster mit runden Werten, weder Norm- noch Messwerte",
                         KonditionierungsvorlagenSaattabelle.BESCHREIBUNG);

            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                List<string> namen = KonditionierungsvorlagenSaatSchema.Saat.Where(s => s.Groesse == g)
                                                                           .Select(s => s.Bezeichner).ToList();
                Assert.Equal(namen.Count, namen.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            }
            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
            {
                Assert.Equal(nutzung[s.Bezeichner], s.Nutzung);
                Assert.Contains(s.Nutzung, DbWerte.KOND_NUTZUNGEN);
                Assert.Equal(s.Bezeichner.Trim(), s.Bezeichner);
                Assert.InRange(s.Bezeichner.Length, 1, KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN);
                Assert.Equal(KonditionierungsvorlagenSaattabelle.BESCHREIBUNG, s.Beschreibung);
                Assert.True(s.Beschreibung.Length <= KonditionierungVorlagenSchema.BESCHREIBUNG_MAX_ZEICHEN);
            }
        }

        /// <summary>
        /// <b>Jede Zelle in ihren Grenzen, nur die Nutzungszeilen (E54), gültige Nachtfenster.</b> Eine
        /// Zahl liegt in den Grenzen ihrer Größe und läuft über den Text der Woche bitgleich rund; „aus"
        /// trägt keinen Wert; jede Nachtzeile trägt ihr Fenster ausdrücklich (keine Vorlage hängt am
        /// Fenster des Ziels, F19), andere Zeilen tragen keins; keine Zeile doppelt.
        /// </summary>
        [Fact]
        public void Jede_Zelle_in_ihren_Grenzen_nur_Nutzungszeilen_und_gueltige_Nachtfenster()
        {
            var nutzungszeilen = new[] { DbWerte.KOND_ZEILE_TAG, DbWerte.KOND_ZEILE_NACHT,
                                         DbWerte.KOND_ZEILE_WOCHENENDE, DbWerte.KOND_ZEILE_FERIEN };
            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
            {
                Assert.Equal(s.Zeilen.Count, s.Zeilen.Select(z => z.Zeile).Distinct(StringComparer.Ordinal).Count());
                foreach (KonditionierungsvorlagenSaatzeile z in s.Zeilen)
                {
                    string n = s + " " + z.Zeile + ": ";
                    Assert.True(nutzungszeilen.Contains(z.Zeile), n + "keine Nutzungszeile (E54: kein Nennwert, keine Saison).");
                    Assert.Equal(z.Aus, !z.Wert.HasValue);
                    if (!z.Aus)
                    {
                        Assert.True(Konditionierungsgroessen.ImBereich(s.Groesse, z.Wert.Value),
                                    n + Zahl(z.Wert.Value) + " liegt nicht in " + Konditionierungsgroessen.Bereichstext(s.Groesse));
                        Assert.True(Kalenderwoche.Rundlauf(z.Wert.Value), n + "läuft nicht rund.");
                    }
                    if (z.Zeile == DbWerte.KOND_ZEILE_NACHT)
                    {
                        Assert.True(z.Von.HasValue && z.Bis.HasValue, n + "das Nachtfenster steht nicht ausdrücklich.");
                        Assert.Equal(NachtzeitBefund.Gueltig, Nachtzeit.Pruefen(z.Von, z.Bis));
                    }
                    else
                    {
                        Assert.Null(z.Von);
                        Assert.Null(z.Bis);
                    }
                }
                // "aus" nur, wo der Entwurf es setzt: in der Kuehlspalte.
                if (s.Groesse != Konditionierungsgroesse.Kuehlsoll)
                    Assert.DoesNotContain(s.Zeilen, z => z.Aus);
                // Wer Feiertage traegt, traegt die Wochenendzeile, die "wie Sonntag" meint.
                if (s.Feiertage) Assert.NotNull(s.Kalendergrund);
            }
        }

        /// <summary>Die Feiertagsnamen der Saat sind die neun deutschen Namen, die das Werkzeug der Karte schreibt.</summary>
        [Fact]
        public void Die_Feiertagsnamen_gleichen_dem_deutschen_Text_der_Karte()
        {
            Assert.Equal(DbWerte.KOND_FEIERTAGE.Count, KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN.Count);
            Assert.Equal(Kalenderwerkzeuge.Feiertagsnamen(), KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN);
            Assert.Equal(7, KonditionierungsvorlagenSaattabelle.WIE_WOCHENTAG);
        }

        /// <summary>
        /// <b>Keine Hersteller- und Produktnamen</b> (Konzept 5.7, Wache <see cref="WikiProduktdatenWacheTests"/>):
        /// Namen, Beschreibung und Feiertagsnamen gegen die Katalognamen der Testdatenbank und die feste
        /// Liste — mit Gegenprobe.
        /// </summary>
        [Fact]
        public void Keine_Vorlage_nennt_Hersteller_oder_Produkt()
        {
            if (!_db.Vorhanden) return;
            var wache = new WikiProduktdatenWacheTests(_db);
            try
            {
                var funde = new List<string>();
                foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
                    funde.AddRange(wache.FundstellenIn("Vorlage " + s, s.Bezeichner + "\n" + s.Beschreibung));
                funde.AddRange(wache.FundstellenIn("Feiertage",
                                                   string.Join("\n", KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN)));
                Assert.True(funde.Count == 0, "Die Saat nennt Produktdaten:\n" + string.Join("\n", funde));

                Assert.NotEmpty(wache.FundstellenIn("Gegenprobe", "Ein Modul JKM400M liefert 400 W."));
            }
            finally { wache.Dispose(); }
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank
        // =============================================================================

        /// <summary>
        /// <b>Die Testdatenbank trägt die 14 Vorlagen gleich der Saattabelle</b>: je Größe und Name
        /// genau eine, gesperrt, mit Beschreibung und Nutzung; ihre Vorgabezeilen Feld für Feld; bei
        /// Büro und Schule ein Kalender ohne Woche und Nennwert mit den neun Feiertagsregeln „wie
        /// Sonntag"; Inhalt nur in der eigenen Größe; nichts von Nennwert und Saison (E54). Die Leser
        /// des Kerns (Liste, Vorgaben, Kalender) lesen sie ohne Meldung.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_traegt_die_vierzehn_Vorlagen_gleich_der_Saattabelle()
        {
            if (!Bereit()) return;

            Assert.True(KonditionierungsvorlagenSaatSchema.Vollstaendig());
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KonditionierungsvorlagenSaatSchema.SCHRITT);
            Assert.Equal(14L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\""));
            Assert.Equal(14L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"ReadOnly\" = 1"));
            Assert.Equal(46L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(10L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(90L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\" p JOIN \"Tab_Konditionierungskalender\" k " +
                                   "ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Vorlage\" IS NOT NULL"));

            // Nur die eigene Groesse, kein Nennwert, keine Saison, keine Matrixperiode (E54).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" t JOIN \"Tab_Konditionierungsvorlage_STAMM\" v " +
                                  "ON v.\"ID\" = t.\"ID_Vorlage\" WHERE t.\"Groesse\" <> v.\"Groesse\""));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" t JOIN \"Tab_Konditionierungsvorlage_STAMM\" v " +
                                  "ON v.\"ID\" = t.\"ID_Vorlage\" WHERE t.\"Groesse\" <> v.\"Groesse\""));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" IS NOT NULL AND " +
                                  "(\"Zeile\" IN ('NENNWERT', 'SAISON') OR \"Bedingt_K\" IS NOT NULL)"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Vorlage\" IS NOT NULL AND " +
                                  "(\"Nennwert\" IS NOT NULL OR \"Woche\" IS NOT NULL)"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\" p JOIN \"Tab_Konditionierungskalender\" k " +
                                  "ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Vorlage\" IS NOT NULL AND p.\"Art\" <> 'FEIERTAG'"));

            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
            {
                DataTable kopf = Tabelle("SELECT * FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"Groesse\" = ? AND \"Bezeichner\" = ?",
                                         new DbParam("@g", s.Kennwort), new DbParam("@b", s.Bezeichner));
                Assert.True(kopf.Rows.Count == 1, "Keine eindeutige Vorlage " + s);
                DataRow r = kopf.Rows[0];
                long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Beschreibung, Convert.ToString(r["Beschreibung"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Nutzung, Convert.ToString(r["Nutzung"], CultureInfo.InvariantCulture));

                // Die Vorgabezeilen ueber den Leser des Kerns, Feld fuer Feld.
                List<Vorgabezeile> zeilen = _kond.Vorgaben(KonditionierungCtrl.Eigner.Vorlage(id));
                Assert.Equal(s.Zeilen.Count, zeilen.Count);
                foreach (KonditionierungsvorlagenSaatzeile z in s.Zeilen)
                {
                    Vorgabezeile ist = Assert.Single(zeilen, x => x.Zeile == z.Zeile);
                    Assert.Equal(s.Kennwort, ist.Groesse);
                    Assert.Equal(z.Wert, ist.Wert);
                    Assert.Equal(z.Aus, ist.Aus);
                    Assert.Equal(z.Von, ist.Von);
                    Assert.Equal(z.Bis, ist.Bis);
                    Assert.Null(ist.BedingtK);
                }

                // Der Kalender - nur mit Feiertagen, ohne Woche, Grundangabe der Wochenendwert.
                Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender =
                    _kond.Kalender(KonditionierungCtrl.Eigner.Vorlage(id), out string meldung);
                Assert.Null(meldung);
                if (!s.Feiertage)
                {
                    Assert.Empty(kalender);
                    continue;
                }
                Konditionierungskalender k = Assert.Single(kalender).Value;
                Assert.Equal(s.Groesse, k.Groesse);
                Assert.Null(k.Standardwoche);
                Assert.Null(k.Nennwert);
                Assert.Equal(s.Kalendergrund.Aus ? Angabeart.Aus : Angabeart.Wert, k.Grundangabe.Art);
                if (!s.Kalendergrund.Aus) Assert.Equal(s.Kalendergrund.Wert.Value, k.Grundangabe.Wert);

                Assert.Equal(9, k.Perioden.Count);
                for (int i = 0; i < DbWerte.KOND_FEIERTAGE.Count; i++)
                {
                    Kalenderregel p = Assert.Single(k.Perioden, x => x.Feiertagsregel == DbWerte.KOND_FEIERTAGE[i]);
                    Assert.Equal(DbWerte.KOND_ART_FEIERTAG, p.Art);
                    Assert.Equal(Standardfahrplan.RANG_FEIERTAG + i, p.Rang);
                    Assert.Equal(KonditionierungsvorlagenSaattabelle.FEIERTAGSNAMEN[i], p.Bezeichner);
                    Assert.Equal(Angabeart.WieWochentag, p.Angabe.Art);
                    Assert.Equal(KonditionierungsvorlagenSaattabelle.WIE_WOCHENTAG, p.Angabe.WieWochentag);
                }
            }

            // Die Auswahllisten: je Groesse die Saat, ausgeliefert und vorn.
            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                List<KonditionierungsvorlageCtrl.Vorlage> liste = _vorlagen.Liste(g);
                List<string> erwartet = KonditionierungsvorlagenSaatSchema.Saat.Where(s => s.Groesse == g)
                                                                              .Select(s => s.Bezeichner)
                                                                              .OrderBy(n => n, StringComparer.Ordinal).ToList();
                Assert.Equal(erwartet, liste.Select(v => v.Bezeichner).OrderBy(n => n, StringComparer.Ordinal).ToList());
                Assert.All(liste, v => Assert.True(v.Ausgeliefert));
            }
        }

        /// <summary>
        /// Eine ausgelieferte Vorlage lässt sich duplizieren, und die Kopie trägt denselben Inhalt: Der
        /// Vorlagenfilter des Kopierers (E54) lässt jede Zeile der Saat durch — sie trägt nichts, was er
        /// zurückhielte.
        /// </summary>
        [Fact]
        public void Jede_Vorlage_laesst_sich_ohne_Verlust_duplizieren()
        {
            if (!Bereit()) return;
            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
            {
                long id = Vorlagenid(s);
                KonditionierungCtrl.Ergebnis e = _vorlagen.Duplizieren(id, null, out long kopie);
                Assert.True(e.Ok, s + ": " + e.Meldung);
                Assert.False(_vorlagen.Lesen(kopie).Ausgeliefert);
                Assert.Equal(Inhalt(id), Inhalt(kopie));
            }
        }

        /// <summary>
        /// <b>„Kopieren nach …" mit jeder ausgelieferten Vorlage, die ein Ziel hat</b> (Konzept 3.5, 7.4):
        /// Geräte ↔ Personen tragen denselben Inhalt in die andere Liste, Heizen → Kühlen Zeitstruktur und
        /// Nachtzeiten mit dem Komfortsollwert am Tag und dem Absenksollwert darunter; die Kopie ist eigen, die Quelle bleibt Zeichen
        /// für Zeichen. Danach hält der Datenbankfall wie oben: <b>Inhalt nur in der eigenen Größe</b>, kein
        /// Nennwert, keine Saison, keine Matrixperiode (E54).
        /// </summary>
        [Fact]
        public void Jede_Vorlage_mit_Ziel_laesst_sich_kopieren_und_ihr_Inhalt_bleibt_in_der_Zielgroesse()
        {
            if (!Bereit()) return;
            int kopien = 0;
            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
            {
                long id = Vorlagenid(s);
                string vorher = Inhalt(id);
                foreach (Konditionierungsgroesse ziel in Vorlagenkopierregel.Ziele(s.Groesse))
                {
                    string zielwort = Konditionierungsgroessen.Kennwort(ziel);
                    KonditionierungCtrl.Ergebnis e = _vorlagen.KopierenNach(id, ziel, s.Bezeichner + " (" + s.Kennwort + ")",
                                                                            Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE,
                                                                            Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE, out long kopie);
                    Assert.True(e.Ok, s + " -> " + zielwort + ": " + e.Meldung);
                    kopien++;
                    KonditionierungsvorlageCtrl.Vorlage kopf = _vorlagen.Lesen(kopie);
                    Assert.Equal((ziel, false, s.Nutzung), (kopf.Groesse, kopf.Ausgeliefert, kopf.Nutzung));

                    if (Vorlagenkopierregel.Weg(s.Groesse, ziel) == Vorlagenkopierweg.Direkt)
                    {
                        // Derselbe Inhalt, nur in der anderen Liste.
                        Assert.Equal(vorher.Replace(s.Kennwort + ";", zielwort + ";"), Inhalt(kopie));
                        continue;
                    }

                    // Heizen -> Kühlen: jede Zeile der Saat mit ihren Zeiten, die Tagzeile mit dem Komfortsollwert,
                    // jede Zeile darunter (Nacht, Wochenende, Ferien) mit dem Absenksollwert.
                    double tagwert = s.Zeile(DbWerte.KOND_ZEILE_TAG).Wert.Value;
                    List<Vorgabezeile> zeilen = _kond.Vorgaben(KonditionierungCtrl.Eigner.Vorlage(kopie));
                    Assert.Equal(s.Zeilen.Count, zeilen.Count);
                    foreach (KonditionierungsvorlagenSaatzeile z in s.Zeilen)
                    {
                        Vorgabezeile ist = Assert.Single(zeilen, x => x.Zeile == z.Zeile);
                        Assert.Equal(zielwort, ist.Groesse);
                        Assert.Equal(z.Aus, ist.Aus);
                        Assert.Equal(z.Aus ? (double?)null
                                     : z.Wert < tagwert ? Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE
                                     : Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE, ist.Wert);
                        Assert.Equal((z.Von, z.Bis), (ist.Von, ist.Bis));
                    }
                    Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender =
                        _kond.Kalender(KonditionierungCtrl.Eigner.Vorlage(kopie), out string meldung);
                    Assert.Null(meldung);
                    Assert.Equal(s.Feiertage ? 1 : 0, kalender.Count);
                    if (!s.Feiertage) continue;
                    Konditionierungskalender k = kalender[ziel];
                    Assert.Equal(Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE, k.Grundangabe.Wert);   // Grundangabe unter dem Tagwert
                    Assert.Equal(9, k.Perioden.Count);
                    Assert.All(k.Perioden, r => Assert.Equal(KonditionierungsvorlagenSaattabelle.WIE_WOCHENTAG, r.Angabe.WieWochentag));
                }
                Assert.Equal(vorher, Inhalt(id));
            }
            Assert.Equal(9, kopien);                                   // drei aus Heizen, je drei aus Geräten und Personen

            // Der Datenbankfall: nur die eigene Groesse, kein Nennwert, keine Saison, keine Matrixperiode (E54).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" t JOIN \"Tab_Konditionierungsvorlage_STAMM\" v " +
                                  "ON v.\"ID\" = t.\"ID_Vorlage\" WHERE t.\"Groesse\" <> v.\"Groesse\""));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" t JOIN \"Tab_Konditionierungsvorlage_STAMM\" v " +
                                  "ON v.\"ID\" = t.\"ID_Vorlage\" WHERE t.\"Groesse\" <> v.\"Groesse\""));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" IS NOT NULL AND " +
                                  "(\"Zeile\" IN ('NENNWERT', 'SAISON') OR \"Bedingt_K\" IS NOT NULL)"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Vorlage\" IS NOT NULL AND " +
                                  "\"Nennwert\" IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\" p JOIN \"Tab_Konditionierungskalender\" k " +
                                  "ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Vorlage\" IS NOT NULL AND p.\"Art\" <> 'FEIERTAG'"));
            Assert.Equal(KonditionierungsvorlagenSaattabelle.VORLAGEN + 9,
                         Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\""));
            Assert.Equal(KonditionierungsvorlagenSaattabelle.VORLAGEN,
                         Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"ReadOnly\" = 1"));
        }

        /// <summary>
        /// <b>NUR WAS FEHLT, NIE ÜBERSCHREIBEN, WIEDERHOLBAR:</b> Eine gelöschte Vorlage kommt wieder,
        /// eine geänderte bleibt geändert, und eine EIGENE gleichnamige Vorlage — „BÜRO" in der
        /// Personenliste, die der NOCASE-Index neben „Büro" dulden würde — bleibt stehen und steht im
        /// Bericht, statt die ausgelieferte daneben zu bekommen; ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_legt_nur_an_was_fehlt_laesst_Eigenes_stehen_und_ist_wiederholbar()
        {
            if (!Bereit()) return;
            KonditionierungsvorlagenSaat heizenBuero = Saat(Konditionierungsgroesse.Heizsoll, "Büro");
            KonditionierungsvorlagenSaat kuehlenWohnen = Saat(Konditionierungsgroesse.Kuehlsoll, "Wohnen");
            KonditionierungsvorlagenSaat personenBuero = Saat(Konditionierungsgroesse.Personen, "Büro");

            Loeschen(Vorlagenid(heizenBuero));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"Tab_Konditionierungsvorgabe\" SET \"Wert\" = 25 WHERE \"ID_Vorlage\" = ? AND \"Zeile\" = 'TAG'",
                new DbParam("@v", Vorlagenid(kuehlenWohnen))));
            Loeschen(Vorlagenid(personenBuero));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Konditionierungsvorlage_STAMM\" (\"Groesse\", \"Bezeichner\", \"ReadOnly\") VALUES (?, 'BÜRO', 0)",
                new DbParam("@g", personenBuero.Kennwort)));
            Assert.False(KonditionierungsvorlagenSaatSchema.Vollstaendig());
            long vorher = Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\"");

            var bericht = new List<string>();
            KonditionierungsvorlagenSaatSchema.Bericht b = KonditionierungsvorlagenSaatSchema.Ausfuehren(bericht);
            Assert.Equal(1, b.Gesaet);
            Assert.Equal(13, b.Vorhanden.Count);
            Assert.Equal(new[] { personenBuero.ToString() }, b.Eigene);
            Assert.Contains(bericht, z => z.Contains("\"Büro\"", StringComparison.Ordinal) &&
                                          z.Contains("PERSONEN", StringComparison.Ordinal) &&
                                          z.Contains("\"BÜRO\"", StringComparison.Ordinal));
            Assert.True(KonditionierungsvorlagenSaatSchema.Vollstaendig());
            Assert.Equal(vorher + 1, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\""));

            // Heizen/Buero ist wieder da, samt Inhalt; Kuehlen/Wohnen bleibt geaendert.
            Assert.Equal(heizenBuero.Zeilen.Count, (int)Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" = ?",
                                                            new DbParam("@v", Vorlagenid(heizenBuero))));
            Assert.Equal(25.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT \"Wert\" FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" = ? AND \"Zeile\" = 'TAG'",
                new DbParam("@v", Vorlagenid(kuehlenWohnen))), CultureInfo.InvariantCulture));

            // Die Personenliste fuehrt allein die eigene "BÜRO" - keine ausgelieferte daneben.
            List<KonditionierungsvorlageCtrl.Vorlage> personen = _vorlagen.Liste(Konditionierungsgroesse.Personen);
            KonditionierungsvorlageCtrl.Vorlage eigene = Assert.Single(personen,
                v => string.Equals(v.Bezeichner, "Büro", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("BÜRO", eigene.Bezeichner);
            Assert.False(eigene.Ausgeliefert);

            // Zweiter Lauf: nichts.
            KonditionierungsvorlagenSaatSchema.Bericht b2 = KonditionierungsvorlagenSaatSchema.Ausfuehren(null);
            Assert.Equal(0, b2.Gesaet);
            Assert.Equal(14, b2.Vorhanden.Count);
            Assert.Equal(vorher + 1, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\""));
        }

        /// <summary>Ohne eine einzige Vorlage legt der Schritt alle 14 an — die Lage einer Anwenderdatenbank vor dem Schritt.</summary>
        [Fact]
        public void Aus_dem_Stand_davor_legt_der_Schritt_alle_vierzehn_an()
        {
            if (!Bereit()) return;

            Assert.True(DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungsvorlage_STAMM\""));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\""));
            Assert.False(KonditionierungsvorlagenSaatSchema.Vollstaendig());

            KonditionierungsvorlagenSaatSchema.Bericht b = KonditionierungsvorlagenSaatSchema.Ausfuehren(null);
            Assert.Equal(14, b.Gesaet);
            Assert.Empty(b.Vorhanden);
            Assert.Empty(b.Eigene);
            Assert.True(KonditionierungsvorlagenSaatSchema.Vollstaendig());
            Assert.Equal(14L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"ReadOnly\" = 1"));
            Assert.Equal(46L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(10L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Vorlage\" IS NOT NULL"));
            Assert.Equal(90L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\""));
        }

        // =============================================================================
        //  Teil 3 - jede Vorlage auf ein Probegebaeude uebernommen
        // =============================================================================

        /// <summary>Das Referenzjahr der Probe und sein w₀: Der 1. Januar 2025 ist ein Mittwoch.</summary>
        private const int JAHR = 2025, W0 = 2;

        /// <summary>Stundenindex h = 24 · Tag + Stunde.</summary>
        private static int H(int tag0, int stunde) => 24 * tag0 + stunde;

        /// <summary>
        /// Der Bestand des Probegebäudes: Heizen 21/17 °C ohne wirksames Wochenende, Ferien 12 °C an Tag
        /// 200 … 210 mit Merker, Nacht 22–6 Uhr, Kühlen 25 °C, Luftwechsel GETRENNT (Infiltration 0,3,
        /// Nutzer 0,4 1/h — F15), innere Gewinne 500 W.
        /// </summary>
        private static Vorgabematrix Probematrix()
        {
            var b = new Matrixeingang
            {
                SollTag = 21.0,
                SollNacht = 17.0,
                SollFerien = 12.0,
                Ferienmerker = 1.0,
                NachtBeginn = 22,
                NachtEnde = 6,
                KuehlSollwert = 25.0,
                LuftwechselInfiltration = 0.3,
                LuftwechselNutzer = 0.4,
                InterneWaermegewinne = 500.0,
            };
            b.Ferienbeginn[0] = 200;
            b.Ferienende[0] = 210;
            return Vorgabematrix.Bilden(b, null);
        }

        /// <summary>
        /// <b>Jede Vorlage, auf ein Probegebäude übernommen, ergibt einen gültigen Kalender</b> — über
        /// den Kernweg <see cref="KonditionierungsvorlageCtrl.Uebernehmen"/>: angelegt, lesbar ohne
        /// Meldung, jede der 8 760 Stunden in den Grenzen der Größe oder „aus"; die Werte der Vorlage an
        /// einem Werktag (Tag und Nacht), einem Samstag und in den Ferien des Ziels; die Feiertage „wie
        /// Sonntag" genau bei Büro und Schule — am 1. Mai (ein Donnerstag) gilt dort der Sonntagswert.
        /// </summary>
        [Fact]
        public void Jede_Vorlage_auf_ein_Probegebaeude_uebernommen_ergibt_einen_gueltigen_Kalender()
        {
            if (!Bereit()) return;
            long g = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MIN(\"ID\") FROM \"" + Matrixzellenort.TAB_GEBAEUDE + "\""),
                                     CultureInfo.InvariantCulture);
            Assert.True(g > 0, "Die Testdatenbank führt kein Gebäude.");
            var ziel = KonditionierungCtrl.Eigner.Gebaeude(g);
            Vorgabematrix matrix = Probematrix();

            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
            {
                // Jede Probe beginnt ohne Kalender und Vorgabezeilen am Gebaeude.
                Assert.True(DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", g)));
                Assert.True(DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", g)));

                KonditionierungCtrl.Ergebnis e = _vorlagen.Uebernehmen(Vorlagenid(s), ziel, matrix);
                Assert.True(e.Ok, s + ": " + e.Meldung);

                Dictionary<Konditionierungsgroesse, Konditionierungskalender> alle = _kond.Kalender(ziel, out string meldung);
                Assert.Null(meldung);
                Assert.True(alle.TryGetValue(s.Groesse, out Konditionierungskalender k), s + ": kein Kalender angelegt.");
                Assert.Single(alle);

                double[] reihe = k.Auswerten(W0, JAHR);
                Assert.Equal(8760, reihe.Length);
                double aus = Konditionierungsgroessen.AusWert(s.Groesse);
                for (int h = 0; h < reihe.Length; h++)
                    Assert.True(Gleich(aus, reihe[h]) || Konditionierungsgroessen.ImBereich(s.Groesse, reihe[h]),
                                s + ": Stunde " + h + " = " + Zahl(reihe[h]));

                // Die Werte der Vorlage - fehlt eine Zeile, gilt die des Ziels.
                double tag = Erwartet(s, DbWerte.KOND_ZEILE_TAG, Zielwert(matrix, s.Groesse));
                double nacht = Erwartet(s, DbWerte.KOND_ZEILE_NACHT, tag);
                double wochenende = Erwartet(s, DbWerte.KOND_ZEILE_WOCHENENDE, tag);
                Pruefe(s, "Werktag 10 Uhr", tag, reihe[H(7, 10)]);           // Mittwoch, 8. Januar
                Pruefe(s, "Werktag 3 Uhr", nacht, reihe[H(7, 3)]);
                Pruefe(s, "Samstag 12 Uhr", wochenende, reihe[H(10, 12)]);   // Samstag, 11. Januar
                if (s.Zeile(DbWerte.KOND_ZEILE_FERIEN) != null)
                    Pruefe(s, "Ferien 10 Uhr", Erwartet(s, DbWerte.KOND_ZEILE_FERIEN, double.NaN), reihe[H(205, 10)]);

                // Die Feiertage: neun Regeln bei Buero und Schule, am 1. Mai (Donnerstag) der Sonntagswert.
                int feiertage = k.Perioden.Count(p => p.IstFeiertag);
                Assert.Equal(s.Feiertage ? 9 : 0, feiertage);
                Pruefe(s, "1. Mai 10 Uhr", s.Feiertage ? wochenende : tag, reihe[H(120, 10)]);

                // Die Herkunft steht als Text am Kalender des Ziels.
                string bemerkung = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT \"Bemerkung\" FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Groesse\" = ?",
                    new DbParam("@g", g), new DbParam("@gr", s.Kennwort)), CultureInfo.InvariantCulture);
                Assert.Contains(s.Bezeichner, bemerkung, StringComparison.Ordinal);
            }
        }

        /// <summary>Der Tagwert des Ziels, wo die Vorlage keinen trägt: die Lüftung nimmt die Nutzerlüftung, eine leere Anteilszelle heißt 100 %.</summary>
        private static double Zielwert(Vorgabematrix m, Konditionierungsgroesse g)
        {
            Matrixzelle tag = m.Spalte(g).Tag;
            if (tag.Belegt) return tag.Aus ? Konditionierungsgroessen.AusWert(g) : tag.Wert;
            return Konditionierungsgroessen.HatNennwert(g) ? Konditionierungsgroessen.ANTEIL_MAX : double.NaN;
        }

        private static double Erwartet(KonditionierungsvorlagenSaat s, string zeile, double sonst)
        {
            KonditionierungsvorlagenSaatzeile z = s.Zeile(zeile);
            if (z == null) return sonst;
            return z.Aus ? Konditionierungsgroessen.AusWert(s.Groesse) : z.Wert.Value;
        }

        private static void Pruefe(KonditionierungsvorlagenSaat s, string wo, double erwartet, double ist)
            => Assert.True(Gleich(erwartet, ist), s + ", " + wo + ": erwartet " + Zahl(erwartet) + ", ist " + Zahl(ist));

        private static bool Gleich(double a, double b)
            => (double.IsNaN(a) && double.IsNaN(b)) || a == b;

        // =============================================================================
        //  Teil 4 - Werkzeug, Migration, Testkopie und Repo-Datei
        // =============================================================================

        /// <summary>
        /// <b>Die Verdrahtung.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie
        /// führen den Schritt aus derselben Quelle NACH der Kesselkennlinie; die Repo-Datei trägt ihn (lesend
        /// geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_Testkopie_und_Repo_Datei_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wSaat = werkzeug.IndexOf("KonditionierungsvorlagenSaatSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wSaat > werkzeug.IndexOf("KesselKennlinieSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Die Saat steht im Werkzeug nicht hinter der Kesselkennlinie.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KONDITIONIERUNGSVORLAGEN_SAAT = KonditionierungsvorlagenSaatSchema.SCHRITT", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_KESSEL_KENNLINIE", StringComparison.Ordinal);
            int ortSaat = migration.IndexOf("new Schritt(SCHRITT_KONDITIONIERUNGSVORLAGEN_SAAT", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSaat > ortVorher, "Der Schritt steht nicht hinter 156.");
            Assert.Contains("KonditionierungsvorlagenSaatSchema.Ausfuehren(zeilen)", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vSaat = vorrichtung.IndexOf("KonditionierungsvorlagenSaatSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vSaat > vorrichtung.IndexOf("KesselKennlinieSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Die Saat steht in der Testkopie nicht hinter der Kesselkennlinie.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SchemaVersion FROM Tab_Applikation";
            Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) >= KonditionierungsvorlagenSaatSchema.SCHRITT);
            cmd.CommandText = "SELECT COUNT(*) FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"ReadOnly\" = 1 AND \"Groesse\" = $g AND \"Bezeichner\" = $b";
            SqliteParameter pg = cmd.Parameters.Add("$g", SqliteType.Text);
            SqliteParameter pb = cmd.Parameters.Add("$b", SqliteType.Text);
            foreach (KonditionierungsvorlagenSaat s in KonditionierungsvorlagenSaatSchema.Saat)
            {
                pg.Value = s.Kennwort;
                pb.Value = s.Bezeichner;
                Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) == 1,
                            "Die Repo-Datei trägt " + s + " nicht gesperrt.");
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static KonditionierungsvorlagenSaat Saat(Konditionierungsgroesse g, string name)
            => KonditionierungsvorlagenSaatSchema.Saat.Single(s => s.Groesse == g && s.Bezeichner == name);

        private static long Vorlagenid(KonditionierungsvorlagenSaat s)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT \"ID\" FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"Groesse\" = ? AND \"Bezeichner\" = ?",
                new DbParam("@g", s.Kennwort), new DbParam("@b", s.Bezeichner));
            Assert.True(o != null && o != DBNull.Value, "Die Vorlage " + s + " fehlt.");
            return Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static void Loeschen(long id)
            => Assert.True(DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungsvorlage_STAMM\" WHERE \"ID\" = ?",
                                                     new DbParam("@id", id)));

        /// <summary>Der Inhalt einer Vorlage als Text — Vorgabezeilen, Kalender und Perioden, ohne Ids.</summary>
        private static string Inhalt(long id)
        {
            var teile = new List<string>();
            foreach (DataRow r in Tabelle("SELECT \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\" " +
                                          "FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Vorlage\" = ? ORDER BY \"Zeile\"",
                                          new DbParam("@v", id)).Rows)
                teile.Add(string.Join(";", r.ItemArray.Select(Text)));
            foreach (DataRow r in Tabelle("SELECT \"Groesse\", \"Wert\", \"Aus\", \"Woche\", \"Nennwert\" " +
                                          "FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Vorlage\" = ?",
                                          new DbParam("@v", id)).Rows)
                teile.Add(string.Join(";", r.ItemArray.Select(Text)));
            foreach (DataRow r in Tabelle("SELECT p.\"Rang\", p.\"Art\", p.\"Bezeichner\", p.\"Feiertagsregel\", p.\"WieWochentag\" " +
                                          "FROM \"Tab_Konditionierungsperiode\" p JOIN \"Tab_Konditionierungskalender\" k " +
                                          "ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Vorlage\" = ? ORDER BY p.\"Rang\"",
                                          new DbParam("@v", id)).Rows)
                teile.Add(string.Join(";", r.ItemArray.Select(Text)));
            return string.Join("\n", teile);
        }

        private static string Text(object o)
            => o == null || o == DBNull.Value ? "NULL" : Convert.ToString(o, CultureInfo.InvariantCulture);

        private static DataTable Tabelle(string sql, params DbParam[] p)
        {
            DataTable t = DataRepository.GetDataTable(sql, p);
            Assert.NotNull(t);
            return t;
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        private static string Zahl(double w) => w.ToString("G6", CultureInfo.InvariantCulture);

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
