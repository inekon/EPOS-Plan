using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schreibweg des Kalibriervorschlags</b> (Umsetzungskonzept Zapfprofilgenerator 4.8 und
    /// Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 2, Punkt 3):
    /// <see cref="TwwNutzungsartCtrl.VorschlagUebernehmen"/> legt aus einem
    /// <see cref="Nichtwohnvorschlag"/> eine Anwenderkopie der Nutzungsart an — Bedarf,
    /// Wochenfaktoren und die gemessenen Tagesgänge kalibriert, Jahresgang und fehlende Tagtypen von
    /// der Vorlage, Zapfkategorien mitkopiert, Status <c>EIGEN</c>, Katalogversion „…-E&lt;n&gt;“.
    ///
    /// <para>Der Nachweis am Ende ist der ganze Weg: Eine gemessene Reihe über 365 volle Tage wird
    /// zum Vorschlag, der Vorschlag zur Kopie, und die Rechnung auf dieser Kopie gibt die gemessene
    /// Jahresenergie <b>genau</b> wieder. Alle Werte erfunden (Kapitel 6 (a)).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwVorschlagUebernehmenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly DateTime BEGINN = new DateTime(2025, 1, 1);

        /// <summary>Ein erfundenes Tagesmuster: 24 Stundenwerte [kWh], Summe 12 — Vormittag und Abend.</summary>
        private static readonly double[] TAGESMUSTER =
        {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.5, 2.0, 1.0, 0.5, 0.5, 0.5,
            0.5, 0.5, 0.5, 0.5, 0.5, 1.0, 1.5, 0.5, 0.5, 0.0, 0.0, 0.0
        };

        /// <summary>Eine gemessene Stundenreihe über <paramref name="tage"/> Tage mit dem Muster.</summary>
        private static Messreihe Reihe(int tage, double[] muster = null)
        {
            double[] m = muster ?? TAGESMUSTER;
            var werte = new double[tage * Zapfkalender.STUNDEN_TAG];
            for (int d = 0; d < tage; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    werte[d * Zapfkalender.STUNDEN_TAG + h] = m[h];
            return new Messreihe("Waermemengenzaehler (erfunden)", ZapfMessgroesse.Energie, 60, BEGINN, werte,
                                 "Probe (erfunden)");
        }

        private static Nichtwohnvorschlag Vorschlag(double bezugsmenge, int tage = Zapfkalender.TAGE, double[] muster = null)
        {
            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(Reihe(tage, muster), 0.0, bezugsmenge, 30,
                                                                      out ZapfSatz fehler);
            Assert.Null(fehler);
            return v;
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        // =================================================================================
        //  Die Anwenderkopie
        // =================================================================================

        /// <summary>
        /// Der Vorschlag wird eine NEUE Nutzungsart (Status <c>EIGEN</c>, Katalogversion „T1-E1“,
        /// <c>ID_Vorlage</c> auf die alte) mit einem eigenen Tagesgangsatz; die Vorlage bleibt Wert
        /// für Wert unberührt — auch die freie. Bedarf und Wochengang tragen die Quelle
        /// „Kalibriert aus Messreihe …“ mit Herkunftsart <c>VERFAHREN</c>, der Jahresgang die
        /// Provenienz der Vorlage. Die Zapfkategorien kommen mit.
        /// </summary>
        [Fact]
        public void Der_Vorschlag_wird_eine_Anwenderkopie_mit_kalibrierten_Werten()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int vorlage = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(vorlage, "Kurz", 1, 2.0, 1, 0.6, 1.0);
            TwwTestdatenbank.KategorieAnlegen(vorlage, "Lang", 2, 8.0, 5, 0.4, 1.0);
            Nutzungsart alt = ZapfprofilCtrl.LiesNutzungsart(vorlage);

            Nichtwohnvorschlag v = Vorschlag(20.0);
            TwwKatalogErgebnis e = TwwNutzungsartCtrl.VorschlagUebernehmen(vorlage, v, "Messung 2025 (erfunden)",
                                                                          temperaturen: new Temperaturbezug(45.0, 10.0));
            Assert.Equal(TwwKatalogAusgang.Ausgefuehrt, e.Ausgang);
            Assert.NotEqual(vorlage, e.Id);

            Nutzungsart kopie = ZapfprofilCtrl.LiesNutzungsart(e.Id);
            Assert.Equal(alt.Name, kopie.Name);
            Assert.Equal("T1" + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "1", kopie.Katalogversion);
            Assert.Equal(ZapfKatalogstatus.Eigen, kopie.Status);
            Assert.False(kopie.ReadOnly);
            Assert.Equal(vorlage, Zahl("SELECT ID_Vorlage FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", e.Id)));

            // (a) Die kalibrierten Werte: Bedarf (mittleres Niveau), Wochenfaktoren, Tagesgänge.
            Assert.Equal(v.TagesbedarfJeEinheitKwh, kopie.BedarfJeNiveauKwhJeEinheitTag[1], 12);
            double verhaeltnisNiedrig = alt.BedarfJeNiveauKwhJeEinheitTag[0] / alt.BedarfJeNiveauKwhJeEinheitTag[1];
            Assert.Equal(v.TagesbedarfJeEinheitKwh * verhaeltnisNiedrig, kopie.BedarfJeNiveauKwhJeEinheitTag[0], 12);
            Assert.Equal(v.Wochenfaktoren.ToArray(), kopie.Wochenfaktoren);
            Assert.Equal(new Temperaturbezug(45.0, 10.0), kopie.Bezugstemperaturen);
            foreach (Tagesgangvorschlag g in v.Tagesgaenge)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    Assert.Equal(g.Anteile[h], kopie.Tagesgaenge.Anteile[(int)g.Tagtyp - 1, h], 12);
            // Der Ruhetag steht nicht in der Messung (kein Messtag): der Gang der Vorlage bleibt.
            Assert.DoesNotContain(v.Tagesgaenge, g => g.Tagtyp == ZapfTagtyp.Ruhetag);
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                Assert.Equal(alt.Tagesgaenge.Anteile[3, h], kopie.Tagesgaenge.Anteile[3, h], 12);

            // (b) Die Provenienz: Bedarf und Wochengang kalibriert, Jahresgang und Ruhetag wie die Vorlage.
            string quelle = string.Format(CultureInfo.InvariantCulture, TwwNutzungsartCtrl.QUELLE_KALIBRIERT,
                                          "Messung 2025 (erfunden)");
            foreach (Provenienz p in new[] { kopie.Herkunft.Bedarf, kopie.Herkunft.Wochengang, kopie.Herkunft.Tagesgang[0] })
            {
                Assert.Equal(Herkunftsart.Verfahren, p.Art);
                Assert.Equal(quelle, p.Quelle);
                Assert.Equal(kopie.Katalogversion, p.Version);
            }
            Assert.Equal(alt.Herkunft.Jahresgang, kopie.Herkunft.Jahresgang);
            Assert.Equal(alt.Herkunft.Tagesgang[3].Quelle, kopie.Herkunft.Tagesgang[3].Quelle);
            Assert.All(kopie.Herkunft.Bandbreite.Min, m => Assert.Null(m));
            Assert.All(kopie.Herkunft.Bandbreite.Max, m => Assert.Null(m));

            // (c) Die Zapfkategorien kommen mit, die Monatsfaktoren bleiben die der Vorlage.
            Assert.Equal(new[] { "Kurz", "Lang" },
                         TwwNutzungsartCtrl.KategorienLesen(e.Id).Kategorien.Select(k => k.Name).ToArray());
            Assert.Equal(alt.Monatsfaktoren, kopie.Monatsfaktoren);

            // (d) Die Vorlage ist unberührt — Werte, Provenienz und ihr Tagesgangsatz.
            Nutzungsart nachher = ZapfprofilCtrl.LiesNutzungsart(vorlage);
            Assert.Equal(alt.BedarfJeNiveauKwhJeEinheitTag, nachher.BedarfJeNiveauKwhJeEinheitTag);
            Assert.Equal(alt.Wochenfaktoren, nachher.Wochenfaktoren);
            Assert.Equal(alt.Herkunft.Bedarf, nachher.Herkunft.Bedarf);
            Assert.Equal(satz, nachher.Tagesgaenge.Id);
            Assert.NotEqual(satz, kopie.Tagesgaenge.Id);
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_TwwTagesgangsatz_STAMM"));
            Assert.Equal(8L, Zahl("SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM"));
        }

        /// <summary>
        /// Jede Ablehnung ist benannt und schreibt nichts: ohne Tabellen, ohne Vorschlag, ohne
        /// Bezeichnung, mit verletztem Raster (Wochenfaktoren Σ ≠ 1), für eine unbekannte
        /// Nutzungsart und mit vergebener Katalogversion.
        /// </summary>
        [Fact]
        public void Jede_Ablehnung_des_Schreibwegs_ist_benannt()
        {
            using (var leer = new TwwTestdatenbank(mitTwwSchema: false))
                Assert.Equal(TwwKatalogAusgang.TabellenFehlen,
                             TwwNutzungsartCtrl.VorschlagUebernehmen(1, Vorschlag(10.0), "Messung").Ausgang);

            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int vorlage = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            Nichtwohnvorschlag v = Vorschlag(10.0);

            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig,
                         TwwNutzungsartCtrl.VorschlagUebernehmen(vorlage, null, "Messung").Ausgang);
            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig,
                         TwwNutzungsartCtrl.VorschlagUebernehmen(vorlage, v, "  ").Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig,
                         TwwNutzungsartCtrl.VorschlagUebernehmen(vorlage, v with { Wochenfaktoren = new[] { 1.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0 } },
                                                                 "Messung").Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig,
                         TwwNutzungsartCtrl.VorschlagUebernehmen(vorlage, v with { TagesbedarfJeEinheitKwh = 0.0 }, "Messung").Ausgang);
            Assert.Equal(TwwKatalogAusgang.NichtGefunden,
                         TwwNutzungsartCtrl.VorschlagUebernehmen(9999, v, "Messung").Ausgang);

            // Nichts geschrieben: eine Nutzungsart, ein Satz.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_TwwNutzungsart_STAMM"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_TwwTagesgangsatz_STAMM"));

            // Die Katalogversion der Kopie ist vergeben (eine gleichnamige Zeile steht schon da).
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1" + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "9", satz);
            Assert.Equal(TwwKatalogAusgang.NameBelegt,
                         TwwNutzungsartCtrl.VorschlagUebernehmen(vorlage, v, "Messung",
                                                                 "T1" + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "9").Ausgang);
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_TwwNutzungsart_STAMM"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_TwwTagesgangsatz_STAMM"));
        }

        // =================================================================================
        //  Der ganze Weg: Messung → Vorschlag → Kopie → Rechnung
        // =================================================================================

        /// <summary>
        /// <b>Die Rechnung auf der Anwenderkopie gibt die Messenergie wieder</b> (4.8): Eine Reihe
        /// über 365 volle Tage ergibt den Tagesbedarf je Einheit; die Kopie trägt ihn als mittleres
        /// Niveau, und die Zone auf ihr rechnet <c>Bezugsmenge · q · 365 · f_θ</c> — mit
        /// Temperaturfaktor 1 (die Zone nennt die Bezugstemperaturen der Kopie) ist das genau die
        /// gemessene Jahresenergie. Gerechnet wird auf einer Arbeitskopie der Testdatenbank mit
        /// einer NICHTWOHN-Nutzungsart ihres Testkatalogs; ohne Testdatenbank schweigt der Fall.
        ///
        /// <para><b>Dazu die Formprobe je Tagtyp</b> (Nachbesserung Gruppe 2): Die Energie allein
        /// wäre auch dann gleich, wenn die Kopie eine falsche Form trüge — sie ist EINE Zahl.
        /// Geprüft wird deshalb zusätzlich, dass die gerechnete Reihe für JEDEN Tagtyp, den der
        /// Kalender führt, genau die gemessene Tagesform trägt: die 24 mittleren Stundenanteile
        /// der Tage dieses Tagtyps gegen das Messmuster, auf Summe 1 gebracht. Weil jeder Messtag
        /// dieselbe Form hat, ist der Sollwert für jeden Tagtyp derselbe.</para>
        /// </summary>
        [Fact]
        public void Die_Kopie_reproduziert_die_gemessene_Jahresenergie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int PROJEKT = 1007;
            int vorlage = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", "Testnutzung B (fiktiv)"), new DbParam("@k", "TEST-1")), CultureInfo.InvariantCulture);
            Nutzungsart alt = ZapfprofilCtrl.LiesNutzungsart(vorlage);

            const double BEZUGSMENGE = 20.0;
            Messreihe gemessen = Reihe(Zapfkalender.TAGE);
            Nichtwohnvorschlag v = Messkalibrierung.Nichtwohnparameter(gemessen, 0.0, BEZUGSMENGE, 30, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(Zapfkalender.TAGE, v.VolleTage);

            TwwKatalogErgebnis e = TwwNutzungsartCtrl.VorschlagUebernehmen(vorlage, v, gemessen.Bezeichnung);
            Assert.Equal(TwwKatalogAusgang.Ausgefuehrt, e.Ausgang);

            // Die Zone nennt die Bezugstemperaturen der Kopie: Temperaturfaktor genau 1.
            var zone = new ZonenStand
            {
                Name = "Zone Nichtwohnen",
                IdNutzungsart = e.Id,
                Bezugsmenge = BEZUGSMENGE,
                Niveau = ZapfNiveau.Mittel,
                ZapftemperaturC = alt.Bezugstemperaturen.ZapftemperaturC,
                KaltwasserMittelC = alt.Bezugstemperaturen.KaltwasserC,
                KaltwasserAmplitudeK = 0.0
            };
            ProjektStand p = ZapfprofilCtrl.ProjektVorgabe() with { Weg = BrauchwasserWeg.Generator };
            var stand = new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, p);
            ZapfprofilErgebnis erg = ZapfprofilCtrl.Rechnen(PROJEKT, stand, 0, new bool[Zapfkalender.TAGE]);

            Assert.True(erg.Vollstaendig, string.Join("; ", erg.Ablehnungen.Select(a => a.Klartext)));
            Assert.Equal(gemessen.Menge, erg.Zapfung.JahressummeKwh, 9);
            Assert.Equal(v.TagesbedarfKwh * Zapfkalender.TAGE, erg.Zapfung.JahressummeKwh, 9);

            // --- Die Form je Tagtyp: die gerechnete Reihe trägt das Messmuster ---------------
            double tagessumme = TAGESMUSTER.Sum();
            double[] soll = TAGESMUSTER.Select(w => w / tagessumme).ToArray();
            IReadOnlyList<ZapfTagtyp> kalender = Assert.Single(erg.JeZone).Kalender;
            var tagtypen = kalender.Distinct().OrderBy(t => (int)t).ToArray();
            Assert.NotEmpty(tagtypen);
            foreach (ZapfTagtyp t in tagtypen)
            {
                var mittel = new double[Zapfkalender.STUNDEN_TAG];
                int tage = 0;
                for (int d = 0; d < Zapfkalender.TAGE; d++)
                {
                    if (kalender[d] != t) continue;
                    tage++;
                    for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                        mittel[h] += erg.Zapfung.StundenKwh[d * Zapfkalender.STUNDEN_TAG + h];
                }
                Assert.True(tage > 0, "Tagtyp " + t + " ohne Tag im Kalender.");
                double summe = mittel.Sum();
                Assert.True(summe > 0.0, "Tagtyp " + t + " ohne Menge.");
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    Assert.Equal(soll[h], mittel[h] / summe, 9);
            }
        }

        /// <summary>
        /// <b>Die Quelle der kalibrierten Wertgruppen</b>: Sie nennt die Messreihe, bleibt in der
        /// Länge begrenzt (<see cref="TwwNutzungsartCtrl.QUELLE_LAENGE"/>) und wird sichtbar mit
        /// einem Auslassungszeichen geschlossen — nie still abgehackt. <b>Beschnitten wird an einer
        /// Zeichengrenze</b>: Ein Ersatzzeichenpaar bleibt ganz oder fällt ganz weg, eine halbe
        /// UTF-16-Einheit entsteht nicht. Ohne Datenbank.
        /// </summary>
        [Fact]
        public void Die_Kalibrierquelle_nennt_die_Reihe_und_kappt_an_einer_Zeichengrenze()
        {
            // (a) Kurz: unveraendert, mit getrimmter Bezeichnung; ohne Bezeichnung bleibt der Rumpf.
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, TwwNutzungsartCtrl.QUELLE_KALIBRIERT, "Messung A"),
                         TwwNutzungsartCtrl.Kalibrierquelle("  Messung A  "));
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, TwwNutzungsartCtrl.QUELLE_KALIBRIERT, ""),
                         TwwNutzungsartCtrl.Kalibrierquelle(null));

            string rumpf = string.Format(CultureInfo.InvariantCulture, TwwNutzungsartCtrl.QUELLE_KALIBRIERT, "");
            int platz = TwwNutzungsartCtrl.QUELLE_LAENGE - rumpf.Length;
            Assert.True(platz > 4, "Die Laengengrenze laesst keinen Platz fuer eine Bezeichnung.");

            // (b) Genau auf der Grenze: noch ungekappt.
            string voll = TwwNutzungsartCtrl.Kalibrierquelle(new string('A', platz));
            Assert.Equal(TwwNutzungsartCtrl.QUELLE_LAENGE, voll.Length);
            Assert.DoesNotContain("…", voll);

            // (c) Ein Zeichen zu lang: gekappt und sichtbar geschlossen.
            string lang = TwwNutzungsartCtrl.Kalibrierquelle(new string('A', platz + 1));
            Assert.Equal(TwwNutzungsartCtrl.QUELLE_LAENGE, lang.Length);
            Assert.EndsWith("…", lang);
            Assert.StartsWith(rumpf, lang);

            // (d) Ein Ersatzzeichenpaar auf der Schnittstelle: Es bleibt ganz oder faellt ganz weg.
            //     Ein Wassertropfen (U+1F4A7) ist EIN Zeichen aus ZWEI UTF-16-Einheiten; ein einzeln
            //     stehengebliebenes Surrogat ergibt beim Lesen das Ersatzzeichen.
            string paar = TwwNutzungsartCtrl.Kalibrierquelle(new string('A', platz - 2) + "\U0001F4A7\U0001F4A7");
            Assert.EndsWith("…", paar);
            Assert.True(paar.Length <= TwwNutzungsartCtrl.QUELLE_LAENGE);
            Assert.DoesNotContain(System.Text.Rune.ReplacementChar, paar.EnumerateRunes().ToArray());
        }
    }
}
