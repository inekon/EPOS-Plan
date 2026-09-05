using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Entscheidungsregeln des gemeinsamen Konfliktdialogs
    /// (<see cref="ImportKonfliktModell"/>, iU9-W12.0b).
    ///
    /// <para><b>Warum sie hier stehen.</b> Bis zur Welle 12 lagen sie in
    /// <c>Views/Import/Form_ImportKonflikte.cs</c> — einer WinForms-Datei, also an
    /// einem Ort, den kein Test dieses Projekts erreicht. Fuenf Importmasken haengen
    /// an ihnen; die Regeltabelle des Konzepts 3.3 ist damit erstmals belegt.</para>
    ///
    /// <para>Ohne Datenbank, ohne Oberflaeche. Wo ein TEXT geprueft wird, ist die
    /// Oberflaechensprache festgelegt (Regel seit iU9-W8).</para>
    /// </summary>
    public class ImportKonfliktModellTests
    {
        // ------------------------------------------------------------ Hilfsmittel

        private static ImportPruefung Pruefung(ImportBefund befund, string name = "Lastgang",
                                               bool nameMehrfachInDb = false,
                                               bool nameDoppeltInAuswahl = false,
                                               KatalogSatz vorhanden = null,
                                               params string[] abweichend)
        {
            return new ImportPruefung
            {
                Kandidat = new ImportKandidat { Name = name },
                Befund = befund,
                Vorhanden = vorhanden,
                NameMehrfachInDb = nameMehrfachInDb,
                NameDoppeltInAuswahl = nameDoppeltInAuswahl,
                AbweichendeSpalten = new List<string>(abweichend)
            };
        }

        private static KonfliktEntscheidung Zeile(ImportPruefung p, KonfliktAktion a, string neuerName = null)
            => new KonfliktEntscheidung { Pruefung = p, Aktion = a, NeuerName = neuerName };

        // ============================================================ ErlaubteAktionen
        // Die Regeltabelle des Konzepts 3.3, Zeile fuer Zeile.

        [Fact]
        public void Neu_erlaubt_Importieren_und_Auslassen_und_belegt_Importieren_vor()
        {
            KonfliktAktion vorbelegung;
            List<KonfliktAktion> erlaubt =
                ImportKonfliktModell.ErlaubteAktionen(Pruefung(ImportBefund.Neu), out vorbelegung);

            Assert.Equal(new[] { KonfliktAktion.Importieren, KonfliktAktion.Auslassen }, erlaubt);
            Assert.Equal(KonfliktAktion.Importieren, vorbelegung);
        }

        [Fact]
        public void Neu_mit_doppeltem_Namen_in_der_Auswahl_erlaubt_nur_Auslassen_und_Umbenennen()
        {
            KonfliktAktion vorbelegung;
            List<KonfliktAktion> erlaubt = ImportKonfliktModell.ErlaubteAktionen(
                Pruefung(ImportBefund.Neu, nameDoppeltInAuswahl: true), out vorbelegung);

            // Zwei markierte Eintraege mit demselben Namen: nur einer darf ihn tragen.
            Assert.Equal(new[] { KonfliktAktion.Auslassen, KonfliktAktion.Umbenennen }, erlaubt);
            Assert.Equal(KonfliktAktion.Auslassen, vorbelegung);
        }

        [Fact]
        public void Inhaltsgleich_belegt_Importieren_vor_weil_gewollte_Varianten_der_Regelfall_sind()
        {
            KonfliktAktion vorbelegung;
            List<KonfliktAktion> erlaubt = ImportKonfliktModell.ErlaubteAktionen(
                Pruefung(ImportBefund.InhaltsGleich), out vorbelegung);

            Assert.Equal(new[] { KonfliktAktion.Importieren, KonfliktAktion.Auslassen }, erlaubt);
            Assert.Equal(KonfliktAktion.Importieren, vorbelegung);
        }

        [Fact]
        public void Identisch_erlaubt_Auslassen_Ueberschreiben_und_Umbenennen()
        {
            KonfliktAktion vorbelegung;
            List<KonfliktAktion> erlaubt = ImportKonfliktModell.ErlaubteAktionen(
                Pruefung(ImportBefund.Identisch), out vorbelegung);

            Assert.Equal(new[] { KonfliktAktion.Auslassen, KonfliktAktion.Ueberschreiben,
                                 KonfliktAktion.Umbenennen }, erlaubt);
            Assert.Equal(KonfliktAktion.Auslassen, vorbelegung);
        }

        [Fact]
        public void Name_mehrfach_im_Katalog_nimmt_das_Ueberschreiben_aus_der_Liste()
        {
            KonfliktAktion vorbelegung;
            List<KonfliktAktion> erlaubt = ImportKonfliktModell.ErlaubteAktionen(
                Pruefung(ImportBefund.NameVorhanden, nameMehrfachInDb: true), out vorbelegung);

            Assert.Equal(new[] { KonfliktAktion.Auslassen, KonfliktAktion.Umbenennen }, erlaubt);
            Assert.DoesNotContain(KonfliktAktion.Ueberschreiben, erlaubt);
            Assert.Equal(KonfliktAktion.Auslassen, vorbelegung);
        }

        /// <summary>
        /// Die Sonderregel „doppelt in der Auswahl" gilt NUR fuer <c>Neu</c>. Ein
        /// Namenstreffer im Katalog bleibt ein Namenstreffer, auch wenn der Name
        /// zusaetzlich zweimal markiert ist.
        /// </summary>
        [Fact]
        public void Doppelt_in_der_Auswahl_greift_nur_bei_Neu()
        {
            KonfliktAktion vorbelegung;
            List<KonfliktAktion> erlaubt = ImportKonfliktModell.ErlaubteAktionen(
                Pruefung(ImportBefund.NameVorhanden, nameDoppeltInAuswahl: true), out vorbelegung);

            Assert.Contains(KonfliktAktion.Ueberschreiben, erlaubt);
        }

        // ============================================================ BefundText

        [Fact]
        public void BefundText_nennt_die_abweichenden_Spalten()
        {
            using var _ = new DeutscheOberflaeche();

            string text = ImportKonfliktModell.BefundText(
                Pruefung(ImportBefund.NameVorhanden, abweichend: new[] { "Leistung", "Zeitinterval" }));

            Assert.Equal("Name bereits vorhanden – abweichend: Leistung, Zeitinterval", text);
        }

        [Fact]
        public void BefundText_nennt_bei_Inhaltsgleichheit_den_vorhandenen_Satz()
        {
            using var _ = new DeutscheOberflaeche();

            string text = ImportKonfliktModell.BefundText(Pruefung(ImportBefund.InhaltsGleich,
                vorhanden: new KatalogSatz { Name = "Werk Nord 2024" }));

            Assert.Equal("inhaltsgleich mit \"Werk Nord 2024\"", text);
        }

        [Fact]
        public void BefundText_haengt_die_drei_Zusatzzeilen_an()
        {
            using var _ = new DeutscheOberflaeche();

            ImportPruefung p = Pruefung(ImportBefund.Identisch,
                nameMehrfachInDb: true, nameDoppeltInAuswahl: true,
                vorhanden: new KatalogSatz { Name = "Auslieferung", ReadOnly = true });

            string[] zeilen = ImportKonfliktModell.BefundText(p)
                                                  .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            Assert.Equal(4, zeilen.Length);
            Assert.Equal("bereits vorhanden (identisch)", zeilen[0]);
            Assert.Equal("Name kommt in der Auswahl mehrfach vor", zeilen[1]);
            Assert.StartsWith("Name im Katalog mehrfach vergeben", zeilen[2]);
            Assert.StartsWith("Auslieferungssatz:", zeilen[3]);
        }

        /// <summary>
        /// Der ReadOnly-Hinweis haengt an den beiden Namenstreffern — bei
        /// <c>InhaltsGleich</c> (Name neu) steht er nicht, auch wenn der getroffene
        /// Bestandssatz zur Auslieferung gehoert.
        /// </summary>
        [Fact]
        public void ReadOnly_Hinweis_steht_nur_bei_Identisch_und_NameVorhanden()
        {
            using var _ = new DeutscheOberflaeche();

            string text = ImportKonfliktModell.BefundText(Pruefung(ImportBefund.InhaltsGleich,
                vorhanden: new KatalogSatz { Name = "Auslieferung", ReadOnly = true }));

            Assert.DoesNotContain("Auslieferungssatz", text);
        }

        // ============================================================ Konflikte / Kopf

        [Fact]
        public void Konflikte_zaehlt_alles_ausser_sauberen_Neuzugaengen()
        {
            List<ImportPruefung> liste = new List<ImportPruefung>
            {
                Pruefung(ImportBefund.Neu),                                   // kein Konflikt
                Pruefung(ImportBefund.Neu, nameDoppeltInAuswahl: true),       // Konflikt
                Pruefung(ImportBefund.Identisch),                             // Konflikt
                Pruefung(ImportBefund.InhaltsGleich),                         // Konflikt
                Pruefung(ImportBefund.NameVorhanden)                          // Konflikt
            };

            Assert.Equal(4, ImportKonfliktModell.Konflikte(liste));
            Assert.False(ImportKonfliktModell.IstKonflikt(liste[0]));
            Assert.True(ImportKonfliktModell.IstKonflikt(liste[1]));
            Assert.Equal(0, ImportKonfliktModell.Konflikte(null));
        }

        [Fact]
        public void KopfText_nennt_Gesamtzahl_und_Konflikte()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal("5 Einträge, davon 4 mit Konflikt. Bitte je Zeile die Aktion wählen.",
                         ImportKonfliktModell.KopfText(5, 4));
        }

        // ============================================================ NamensVorschlag

        [Fact]
        public void NamensVorschlag_nimmt_die_erste_freie_Nummer()
        {
            HashSet<string> vergeben = new HashSet<string>(StringComparer.Ordinal)
            {
                DublettenPruefung.NormalisiereName("Werk Nord (2)"),
                DublettenPruefung.NormalisiereName("Werk Nord (3)")
            };

            Assert.Equal("Werk Nord (4)", ImportKonfliktModell.NamensVorschlag("Werk Nord", vergeben));
            Assert.Equal("Werk Nord (2)", ImportKonfliktModell.NamensVorschlag("Werk Nord", null));
        }

        [Fact]
        public void NamensVorschlag_faellt_nach_98_Versuchen_auf_neu_zurueck()
        {
            HashSet<string> vergeben = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 2; i < 100; i++)
                vergeben.Add(DublettenPruefung.NormalisiereName("Werk Nord (" + i + ")"));

            Assert.Equal("Werk Nord (neu)", ImportKonfliktModell.NamensVorschlag("Werk Nord", vergeben));
        }

        // ============================================================ Pruefe (Konzept 4.3)

        [Fact]
        public void Pruefe_laesst_eine_saubere_Auswahl_durch()
        {
            List<KonfliktEntscheidung> zeilen = new List<KonfliktEntscheidung>
            {
                Zeile(Pruefung(ImportBefund.Neu, "A"), KonfliktAktion.Importieren),
                Zeile(Pruefung(ImportBefund.Identisch, "B"), KonfliktAktion.Ueberschreiben),
                Zeile(Pruefung(ImportBefund.NameVorhanden, "C"), KonfliktAktion.Umbenennen, "C (2)"),
                Zeile(Pruefung(ImportBefund.Neu, "D"), KonfliktAktion.Auslassen)
            };

            Assert.Null(ImportKonfliktModell.Pruefe(zeilen, new HashSet<string>(StringComparer.Ordinal)));
        }

        [Fact]
        public void Pruefe_beanstandet_einen_leeren_Umbenennungsnamen()
        {
            List<KonfliktEntscheidung> zeilen = new List<KonfliktEntscheidung>
            {
                Zeile(Pruefung(ImportBefund.Neu, "A"), KonfliktAktion.Importieren),
                Zeile(Pruefung(ImportBefund.NameVorhanden, "B"), KonfliktAktion.Umbenennen, "   ")
            };

            ImportKonfliktModell.Beanstandung b =
                ImportKonfliktModell.Pruefe(zeilen, new HashSet<string>(StringComparer.Ordinal));

            Assert.NotNull(b);
            Assert.Equal(1, b.Zeile);
            Assert.Equal("", b.Name);
        }

        [Fact]
        public void Pruefe_beanstandet_einen_schon_vergebenen_Umbenennungsnamen()
        {
            HashSet<string> vergeben = new HashSet<string>(StringComparer.Ordinal)
            {
                DublettenPruefung.NormalisiereName("Werk Nord")
            };
            List<KonfliktEntscheidung> zeilen = new List<KonfliktEntscheidung>
            {
                Zeile(Pruefung(ImportBefund.NameVorhanden, "B"), KonfliktAktion.Umbenennen, "  Werk   Nord ")
            };

            ImportKonfliktModell.Beanstandung b = ImportKonfliktModell.Pruefe(zeilen, vergeben);

            Assert.NotNull(b);
            Assert.Equal(0, b.Zeile);
            Assert.Equal("Werk   Nord", b.Name);   // getrimmt, sonst woertlich
        }

        [Fact]
        public void Pruefe_beanstandet_zwei_Zeilen_mit_demselben_Zielnamen()
        {
            List<KonfliktEntscheidung> zeilen = new List<KonfliktEntscheidung>
            {
                Zeile(Pruefung(ImportBefund.Neu, "Gleich"), KonfliktAktion.Importieren),
                Zeile(Pruefung(ImportBefund.Neu, "Gleich"), KonfliktAktion.Importieren)
            };

            ImportKonfliktModell.Beanstandung b =
                ImportKonfliktModell.Pruefe(zeilen, new HashSet<string>(StringComparer.Ordinal));

            Assert.NotNull(b);
            Assert.Equal(1, b.Zeile);
        }

        /// <summary>
        /// Ueberschreiben faellt aus der Eindeutigkeitspruefung heraus — es legt keinen
        /// neuen Namen an, sondern ersetzt einen vorhandenen Satz.
        /// </summary>
        [Fact]
        public void Pruefe_nimmt_Ueberschreiben_aus_der_Eindeutigkeit_heraus()
        {
            List<KonfliktEntscheidung> zeilen = new List<KonfliktEntscheidung>
            {
                Zeile(Pruefung(ImportBefund.Identisch, "Gleich"), KonfliktAktion.Ueberschreiben),
                Zeile(Pruefung(ImportBefund.Neu, "Gleich"), KonfliktAktion.Importieren)
            };

            Assert.Null(ImportKonfliktModell.Pruefe(zeilen, new HashSet<string>(StringComparer.Ordinal)));
        }

        [Fact]
        public void Pruefe_uebergeht_ausgelassene_Zeilen()
        {
            List<KonfliktEntscheidung> zeilen = new List<KonfliktEntscheidung>
            {
                Zeile(Pruefung(ImportBefund.Neu, "Gleich"), KonfliktAktion.Auslassen),
                Zeile(Pruefung(ImportBefund.Neu, "Gleich"), KonfliktAktion.Auslassen),
                Zeile(Pruefung(ImportBefund.Neu, "Gleich"), KonfliktAktion.Importieren)
            };

            Assert.Null(ImportKonfliktModell.Pruefe(zeilen, new HashSet<string>(StringComparer.Ordinal)));
        }

        [Fact]
        public void BeanstandungsText_setzt_den_Namen_in_die_Meldung()
        {
            using var _ = new DeutscheOberflaeche();

            string text = ImportKonfliktModell.BeanstandungsText(
                new ImportKonfliktModell.Beanstandung(0, "Werk Nord"));

            Assert.Equal("Der Name \"Werk Nord\" ist leer oder bereits vergeben. " +
                         "Bitte einen eindeutigen Namen eintragen.", text);
            Assert.Equal("", ImportKonfliktModell.BeanstandungsText(null));
        }

        // ============================================================ AktionText

        [Fact]
        public void AktionText_liefert_alle_vier_Beschriftungen()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal("Importieren", ImportKonfliktModell.AktionText(KonfliktAktion.Importieren));
            Assert.Equal("Auslassen", ImportKonfliktModell.AktionText(KonfliktAktion.Auslassen));
            Assert.Equal("Überschreiben", ImportKonfliktModell.AktionText(KonfliktAktion.Ueberschreiben));
            Assert.Equal("Umbenennen", ImportKonfliktModell.AktionText(KonfliktAktion.Umbenennen));
        }

        /// <summary>
        /// Stellt de-DE ein und beim Verlassen die vorherige Sprache wieder her — die
        /// Regel seit iU9-W8: Der Windows-Laeufer der CI laeuft mit en-US.
        /// </summary>
        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly System.Globalization.CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo("de-DE");
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorher;
            }
        }
    }
}
