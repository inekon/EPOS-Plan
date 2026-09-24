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
    /// <b>Die Hülle der Editoren der Stufe Experte und der Auslegungsfelder</b>
    /// (<c>EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Editoren.cs</c>, <c>…Auslegung.cs</c>; Umsetzungskonzept
    /// Zapfprofilgenerator 5.1, 5.3, 4.4, 4.7, N10 (i)/(j), N11 (d)/(j); Stufe Z4, Gruppe 2b): Tagesgang
    /// lesen, normieren und schreiben (an Ort und Stelle oder als neue Katalogversion), die Regeln der
    /// Zapfkategorien ohne Datenbank, der Katalog nach dem Schreibweg, der Bezug eines konstruierten
    /// Tags, die Zeilen des Konstruktors über das Schließen des Zapfprofils hinaus, der
    /// Ladeleistungs-Vorschlag und die Parametersätze.
    ///
    /// <para>Mit leerer Tww-Datenbank (<see cref="TwwTestdatenbank"/>) bzw. der Arbeitskopie der
    /// Testdatenbank (Projekt 1007, Katalog TEST-1 samt Auslegungsparametern); ohne Testdatenbank
    /// schweigen deren Fälle. Werte erfunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilHuelleEditorenTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const string VERSION = "TEST-1";
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        // =================================================================================
        // Wachen
        // =================================================================================

        /// <summary>Jeder Ausgang einer Pflegeaktion am Tagesgang hat seinen Grund in beiden Sprachen.</summary>
        [Fact]
        public void Jeder_Ausgang_des_Tagesgangs_hat_seinen_Grund_in_beiden_Sprachen()
        {
            string[] ohneGrund = Enum.GetValues(typeof(TwwKatalogAusgang)).Cast<TwwKatalogAusgang>()
                .Where(a => a != TwwKatalogAusgang.Ausgefuehrt).Select(ZapfprofilHuelle.TagesgangSchluessel)
                .Where(k => string.IsNullOrEmpty(Text(k, DE)) || string.IsNullOrEmpty(Text(k, EN))).ToArray();
            Assert.True(ohneGrund.Length == 0, "Ohne Grund: " + string.Join(", ", ohneGrund));
            Assert.Equal("ZPG_TGE_GRUND_READ_ONLY_GESPERRT", ZapfprofilHuelle.TagesgangSchluessel(TwwKatalogAusgang.ReadOnlyGesperrt));
        }

        [Fact]
        public void Die_Normierung_des_Kerns_teilt_durch_die_Summe_und_lehnt_ohne_Summe_ab()
        {
            double[] n = TwwNutzungsartCtrl.AnteileNormiert(new[] { 1.0, 1.0, 2.0 });
            Assert.Equal(new[] { 0.25, 0.25, 0.5 }, n);
            Assert.Null(TwwNutzungsartCtrl.AnteileNormiert(new[] { 0.0, 0.0 }));
            Assert.Null(TwwNutzungsartCtrl.AnteileNormiert(new[] { 1.0, -0.5 }));
            Assert.Null(TwwNutzungsartCtrl.AnteileNormiert(new[] { 1.0, double.NaN }));
            Assert.Null(TwwNutzungsartCtrl.AnteileNormiert(new double[0]));
        }

        // =================================================================================
        // Tagesgang
        // =================================================================================

        [Fact]
        public void Der_Tagesgang_einer_freien_Nutzungsart_schreibt_normiert_an_Ort_und_Stelle()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", "T1", satz);

            ZapfprofilTagesgangDaten d = ZapfprofilHuelle.TagesgangLaden(frei, null);
            Assert.True(d.Verfuegbar);
            Assert.False(d.Kopie);
            Assert.Equal("", d.Sperrgrund);
            Assert.Equal("", d.KatalogversionVorschlag);
            Assert.Equal("Nutzung frei · T1", d.Nutzungsart);
            Assert.Equal(satz, d.Satz.Id);
            Assert.Equal(0.5, d.Satz.Anteile[0][6]);
            Assert.Equal(0.5, d.Satz.Anteile[3][18]);
            Assert.All(d.Satz.Herkunft, h => Assert.NotEqual("", h));
            Assert.Equal(0.2, d.Wochenfaktoren[0], 12);
            Assert.Equal(0.0, d.Wochenfaktoren[6], 12);
            Assert.Contains(d.Vorlagen, v => v.Id == satz && v.Waehlbar);

            // Prozent je Reihe, nicht zu 100 % summiert: Der Kern normiert.
            var e = new ZapfprofilTagesgangEingabeDaten
            {
                IdNutzungsart = frei,
                TagesgaengeProzent = Enumerable.Range(0, 4).Select(_ => Enumerable.Repeat(1.0, 24).ToArray()).ToArray(),
                WochenfaktorenProzent = Enumerable.Repeat(10.0, 7).ToArray()
            };
            ZapfprofilTagesgangErgebnis erg = ZapfprofilHuelle.TagesgangSpeichern(e);
            Assert.True(erg.Ok, erg.Meldung?.Text);
            Assert.False(erg.NeueZeile);
            Assert.Equal(frei, erg.IdNutzungsart);
            Assert.Equal(satz, erg.IdTagesgangsatz);

            Nutzungsart n = TwwNutzungsartCtrl.Lies(frei);
            Assert.Equal(1.0 / 24.0, n.Tagesgaenge.Anteile[0, 0], 12);
            Assert.Equal(1.0 / 7.0, n.Wochenfaktoren[6], 12);
        }

        /// <summary>
        /// Z4, Gruppe 2b Punkt 1: Der Editor schickt alle Reihen in Prozent, die Hülle teilt durch
        /// 100 — ohne die Originalanteile trüge eine nicht runde, aber UNVERÄNDERTE Reihe (VDI-Werte)
        /// nach dem Umweg Prozent → Bruch → Normierung eine andere Bitfolge als die gespeicherte und
        /// gälte als geändert, würde also ohne Beleg als Eigenkonstruktion neu geschrieben. Öffnen →
        /// „OK" ohne jede Änderung darf das nicht tun.
        /// </summary>
        [Fact]
        public void Ein_nicht_runder_Tagesgangsatz_bleibt_ohne_Aenderung_mit_seiner_Herkunft_und_ohne_neuen_Satz()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz VDI", "T1");

            // Werktag (Tagtyp 1) auf nicht runde Anteile überschreiben — 23 × 0,0417 plus Rest,
            // damit die Summe (fast) exakt 1 bleibt — mit eigener Herkunft und Beleg.
            double[] anteile = Enumerable.Repeat(0.0417, 23).Append(1.0 - 23 * 0.0417).ToArray();
            string setze = string.Join(", ", Enumerable.Range(1, 24).Select(h => "\"Anteil_" + h.ToString("00") + "\" = ?"));
            var p = anteile.Select((a, i) => new DbParam("@a" + i.ToString(CultureInfo.InvariantCulture), a)).ToList();
            p.Add(new DbParam("@id", satz));
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_TwwTagesgang_STAMM\" SET " + setze +
                ", \"Quelle\" = 'VDI 6002', \"Ausgabe\" = '2019', \"Version\" = 'T1', \"Herkunftsart\" = 'VERFAHREN' " +
                "WHERE \"ID_Tagesgangsatz\" = ? AND \"Tagtyp\" = 1", p.ToArray());
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_TwwTagesgangsatz_STAMM\" SET \"Beleg\" = 'VDI-Beleg' WHERE \"ID\" = ?", new DbParam("@id", satz));

            int id = TwwTestdatenbank.NutzungsartAnlegen("Nutzung VDI", "T1", satz);
            TwwTestdatenbank.ZoneAnlegen(1, id, "Zone", 10.0);    // Satz benutzt: eine echte Änderung erzwänge einen neuen Satz

            ZapfprofilTagesgangDaten d = ZapfprofilHuelle.TagesgangLaden(id, null);
            Assert.Equal(anteile[0], d.Satz.Anteile[0][0], 15);
            Assert.Contains("VDI 6002", d.Satz.Herkunft[0]);

            // „OK" ohne jede Änderung: der Editor reicht die geladenen Werte unverändert durch.
            var e = new ZapfprofilTagesgangEingabeDaten
            {
                IdNutzungsart = id,
                TagesgaengeProzent = d.Satz.Anteile.Select(r => r.Select(a => a * 100.0).ToArray()).ToArray(),
                TagesgaengeOriginal = d.Satz.Anteile,
                WochenfaktorenProzent = d.Wochenfaktoren.Select(w => w * 100.0).ToArray(),
                WochenfaktorenOriginal = d.Wochenfaktoren
            };
            ZapfprofilTagesgangErgebnis erg = ZapfprofilHuelle.TagesgangSpeichern(e);
            Assert.True(erg.Ok, erg.Meldung?.Text);
            Assert.False(erg.NeueZeile);
            Assert.Equal(satz, erg.IdTagesgangsatz);              // kein neuer Satz

            Tagesgangsatz nach = ZapfprofilCtrl.Tagesgangsaetze().Single(s => s.Id == satz);
            Assert.Equal(new Provenienz("VDI 6002", "2019", "T1", Herkunftsart.Verfahren), nach.JeTagtyp[0]);
            Assert.Equal("VDI-Beleg", DataRepository.ExecuteScalar(
                "SELECT \"Beleg\" FROM \"Tab_TwwTagesgangsatz_STAMM\" WHERE \"ID\" = ?", new DbParam("@id", satz)));
            Assert.Equal(1, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"Tab_TwwTagesgangsatz_STAMM\"")));
        }

        [Fact]
        public void Eine_gesperrte_Nutzungsart_bekommt_eine_neue_Katalogversion_und_nennt_jede_Ablehnung()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz G", "T1");
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", "T1", satz,
                                                               status: TwwSchema.STATUS_AUSLIEFERUNG, readOnly: true);

            ZapfprofilTagesgangDaten d = ZapfprofilHuelle.TagesgangLaden(geliefert, null);
            Assert.True(d.Kopie);
            Assert.StartsWith("Die Nutzungsart oder ihr Tagesgangsatz gehört zur Auslieferung", d.Sperrgrund);
            Assert.Equal("T1" + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "1", d.KatalogversionVorschlag);

            var e = new ZapfprofilTagesgangEingabeDaten
            {
                IdNutzungsart = geliefert,
                TagesgaengeProzent = Enumerable.Range(0, 4).Select(_ => Enumerable.Repeat(100.0 / 24.0, 24).ToArray()).ToArray(),
                WochenfaktorenProzent = new[] { 20.0, 20.0, 20.0, 20.0, 20.0, 0.0, 0.0 }
            };

            // Ohne Katalogversion der Kopie: benannt abgelehnt, nichts geschrieben.
            ZapfprofilTagesgangErgebnis ohne = ZapfprofilHuelle.TagesgangSpeichern(e);
            Assert.False(ohne.Ok);
            Assert.Equal("ZPG_TGE_GRUND_ENTWURF_UNVOLLSTAENDIG", ohne.Meldung.Kennung);
            Assert.Equal("Der Tagesgang wurde nicht gespeichert — für die neue Katalogversion fehlt ihr Name.", ohne.Meldung.Text);

            // Eine Reihe ohne Summe lässt sich nicht normieren.
            var leer = new ZapfprofilTagesgangEingabeDaten
            {
                IdNutzungsart = geliefert,
                TagesgaengeProzent = Enumerable.Range(0, 4).Select(t => Enumerable.Repeat(t == 2 ? 0.0 : 1.0, 24).ToArray()).ToArray(),
                WochenfaktorenProzent = e.WochenfaktorenProzent,
                Katalogversion = d.KatalogversionVorschlag
            };
            Assert.Equal("ZPG_TGE_GRUND_RASTER_UNGUELTIG", ZapfprofilHuelle.TagesgangSpeichern(leer).Meldung.Kennung);

            // Mit Katalogversion: eine neue Nutzungsart (Status eigen), die gelieferte bleibt.
            e.Katalogversion = d.KatalogversionVorschlag;
            ZapfprofilTagesgangErgebnis neu = ZapfprofilHuelle.TagesgangSpeichern(e);
            Assert.True(neu.Ok, neu.Meldung?.Text);
            Assert.True(neu.NeueZeile);
            Assert.NotEqual(geliefert, neu.IdNutzungsart);
            Assert.NotEqual(satz, neu.IdTagesgangsatz);
            Nutzungsart kopie = TwwNutzungsartCtrl.Lies(neu.IdNutzungsart);
            Assert.Equal(d.KatalogversionVorschlag, kopie.Katalogversion);
            Assert.Equal(ZapfKatalogstatus.Eigen, kopie.Status);
            Assert.False(kopie.ReadOnly);
            Assert.True(TwwNutzungsartCtrl.IstReadOnly(geliefert));

            // Der Vorschlag der nächsten Kopie ist frei; der Katalog nach dem Schreibweg trägt die Kopie.
            Assert.Equal("T1" + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "2", TwwNutzungsartCtrl.FreieKopieversion(geliefert));
            ZapfprofilKatalogstandDaten stand = ZapfprofilHuelle.Katalogstand();
            Assert.Contains(stand.Katalog, k => k.Id == neu.IdNutzungsart && k.Katalogversion == d.KatalogversionVorschlag);
            Assert.Contains(stand.Tagesgangsaetze, s => s.Id == neu.IdTagesgangsatz);
        }

        [Fact]
        public void Ohne_Zeile_oder_Tabellen_ist_der_Editor_benannt_nicht_verfuegbar()
        {
            using (var db = new TwwTestdatenbank())
            {
                ZapfprofilTagesgangDaten d = ZapfprofilHuelle.TagesgangLaden(4711, null);
                Assert.False(d.Verfuegbar);
                Assert.Equal("die Nutzungsart steht nicht (mehr) im Katalog.", d.Grund);
            }
            using (var ohne = new TwwTestdatenbank(mitTwwSchema: false))
            {
                ZapfprofilTagesgangDaten d = ZapfprofilHuelle.TagesgangLaden(1, null);
                Assert.False(d.Verfuegbar);
                Assert.Equal("die Tabellen der Brauchwasser-Nutzungsarten fehlen in dieser Datenbank.", d.Grund);
                ZapfprofilKategorienEditorDaten k = ZapfprofilHuelle.KategorienLaden(1);
                Assert.False(k.Verfuegbar);
            }
        }

        [Fact]
        public void Die_Expertenwahl_der_Zone_oeffnet_ihren_Satz_und_eine_gemeinsame_Vorlage_sperrt()
        {
            using var db = new TwwTestdatenbank();
            int satzA = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int satzB = TwwTestdatenbank.TagesgangsatzAnlegen("Satz B", "T1", false, 1, 2);
            int eins = TwwTestdatenbank.NutzungsartAnlegen("Nutzung eins", "T1", satzA);
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung zwei", "T1", satzA);

            // Zwei Nutzungsarten teilen den Satz: ein geänderter Tagesgang braucht eine Kopie.
            ZapfprofilTagesgangDaten d = ZapfprofilHuelle.TagesgangLaden(eins, satzB);
            Assert.True(d.Kopie);
            Assert.StartsWith("Eine Zone oder eine andere Nutzungsart benutzt den Tagesgang", d.Sperrgrund);
            Assert.Equal(satzB, d.Satz.Id);
            Assert.Equal("", d.Satz.Herkunft[2]);
            ZapfprofilTagesgangsatzDaten halb = Assert.Single(d.Vorlagen, v => v.Id == satzB);
            Assert.False(halb.Waehlbar);
            Assert.Equal("Der Tagesgangsatz führt nicht alle vier Tagtypen.", halb.Sperrgrund);
        }

        /// <summary>
        /// Z4, Gruppe 2b Punkt 3: Zeigt der Editor den Satz X der Zone (Expertenwahl) und ist X
        /// ≠ Satz Y der Nutzungsart, erzwingt „OK" eine Kopie — selbst wenn Y für sich genommen
        /// FREI wäre. Ohne die Sperre schriebe „OK" die (an X orientierten) Werte in Y hinein und
        /// überschriebe ihn damit, obwohl der Anwender ihn nie gesehen hat.
        /// </summary>
        [Fact]
        public void Eine_Expertenwahl_ungleich_dem_Satz_der_Nutzungsart_erzwingt_eine_Kopie_obwohl_beide_frei_sind()
        {
            using var db = new TwwTestdatenbank();
            int satzY = TwwTestdatenbank.TagesgangsatzAnlegen("Satz Y", "T1");
            int satzX = TwwTestdatenbank.TagesgangsatzAnlegen("Satz X", "T1");
            // X unterscheidet sich inhaltlich von Y (sonst wäre "nichts geändert" — auch das ein gültiger Fall).
            string spalten = string.Join(", ", Enumerable.Range(1, 24).Select(h => "\"Anteil_" + h.ToString("00") + "\" = " + (h == 10 ? "1.0" : "0.0")));
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_TwwTagesgang_STAMM\" SET " + spalten +
                                           " WHERE \"ID_Tagesgangsatz\" = ? AND \"Tagtyp\" = 1", new DbParam("@id", satzX));
            int id = TwwTestdatenbank.NutzungsartAnlegen("Nutzung", "T1", satzY);       // Y frei: keine Zone, kein Teiler

            ZapfprofilTagesgangDaten d = ZapfprofilHuelle.TagesgangLaden(id, satzX);
            Assert.True(d.Kopie);
            Assert.Equal(satzX, d.Satz.Id);
            Assert.Equal("Die Zone rechnet einen anderen Tagesgangsatz als die Nutzungsart — ein geänderter Tagesgang entsteht als neue Katalogversion.",
                         d.Sperrgrund);
            Assert.Equal("T1" + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "1", d.KatalogversionVorschlag);

            // "OK": die (an X orientierten) Werte unverändert übernommen — Y darf dabei nie berührt werden.
            var e = new ZapfprofilTagesgangEingabeDaten
            {
                IdNutzungsart = id,
                TagesgaengeProzent = d.Satz.Anteile.Select(r => r.Select(a => a * 100.0).ToArray()).ToArray(),
                WochenfaktorenProzent = d.Wochenfaktoren.Select(w => w * 100.0).ToArray(),
                AngezeigterSatz = d.Satz.Id,
                Katalogversion = d.KatalogversionVorschlag
            };
            ZapfprofilTagesgangErgebnis erg = ZapfprofilHuelle.TagesgangSpeichern(e);
            Assert.True(erg.Ok, erg.Meldung?.Text);
            Assert.Equal(id, erg.IdNutzungsart);                    // die Nutzungsart selbst ist frei: keine neue Zeile nötig
            Assert.NotEqual(satzY, erg.IdTagesgangsatz);
            Assert.NotEqual(satzX, erg.IdTagesgangsatz);            // ein neuer Satz — weder Y noch X überschrieben

            Tagesgangsatz y = ZapfprofilCtrl.Tagesgangsaetze().Single(s => s.Id == satzY);
            Tagesgangsatz x = ZapfprofilCtrl.Tagesgangsaetze().Single(s => s.Id == satzX);
            Assert.Equal(0.5, y.Anteile[0, 6]);                      // Y unverändert
            Assert.Equal(1.0, x.Anteile[0, 9]);                      // X unverändert
        }

        // =================================================================================
        // Zapfkategorien
        // =================================================================================

        [Fact]
        public void Die_Pruefung_der_Kategorien_nimmt_die_Regeln_des_Kerns_ohne_Datenbank()
        {
            var null_ = new List<ZapfprofilKategorieDaten>
            {
                new() { Name = "A", VolumenstromLJeMin = 5, StreuungLJeMin = 1, DauerMin = 2, Anteil = 0 }
            };
            ZapfprofilKategorienPruefungDaten p = ZapfprofilHuelle.KategorienPruefen(null_);
            Assert.Equal("ZPG_SATZ_KATEGORIE_ANTEIL_NULL", p.Ablehnung.Kennung);
            Assert.Equal(0.0, p.SummeAnteil);

            var teil = new List<ZapfprofilKategorieDaten>
            {
                new() { Name = "A", VolumenstromLJeMin = 5, StreuungLJeMin = 1, DauerMin = 2, Anteil = 0.3 },
                new() { Name = "B", VolumenstromLJeMin = 8, StreuungLJeMin = 2, DauerMin = 5, Anteil = 0.3, KappungLJeMin = 12 }
            };
            ZapfprofilKategorienPruefungDaten q = ZapfprofilHuelle.KategorienPruefen(teil);
            Assert.Null(q.Ablehnung);
            Assert.Equal(0.6, q.SummeAnteil, 12);
            Assert.StartsWith("Die Anteile der Zapfkategorien summieren zu 0,6 statt 1", q.Hinweis);

            teil[1].VolumenstromLJeMin = null;
            Assert.Equal("ZPG_SATZ_KATEGORIE_VOLUMENSTROM", ZapfprofilHuelle.KategorienPruefen(teil).Ablehnung.Kennung);
            teil[1].VolumenstromLJeMin = 8;
            teil[1].Name = "A";
            Assert.Equal("ZPG_SATZ_KATEGORIE_NAME_DOPPELT", ZapfprofilHuelle.KategorienPruefen(teil).Ablehnung.Kennung);
            Assert.Equal("ZPG_SATZ_KATEGORIE_KEINE",
                         ZapfprofilHuelle.KategorienPruefen(new List<ZapfprofilKategorieDaten>()).Ablehnung.Kennung);
        }

        [Fact]
        public void Der_Stand_des_Kategorien_Editors_nennt_Nutzungsart_Sperre_und_Katalogversion_der_Kopie()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", "T1", satz);
            int geliefert = TwwTestdatenbank.NutzungsartAnlegen("Nutzung geliefert", "T1", satz);
            TwwTestdatenbank.KategorieAnlegen(frei, "Erste", 1, 4.0, 1, 1.0, 1.0);
            TwwTestdatenbank.KategorieAnlegen(geliefert, "Kurz", 1, 2.0, 1, 1.0, 1.0, readOnly: true, herkunftsart: TwwSchema.HERKUNFT_FREI);

            ZapfprofilKategorienEditorDaten f = ZapfprofilHuelle.KategorienLaden(frei);
            Assert.True(f.Verfuegbar);
            Assert.True(f.Stand.Frei);
            Assert.Equal("", f.KatalogversionVorschlag);
            Assert.Equal("Nutzung frei · T1", f.Nutzungsart);

            ZapfprofilKategorienEditorDaten g = ZapfprofilHuelle.KategorienLaden(geliefert);
            Assert.False(g.Stand.Frei);
            Assert.Equal("T1" + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "1", g.KatalogversionVorschlag);

            // Der Schreibweg der Gaben: gesperrt mit Katalogversion -> neue Nutzungsart, EIGEN.
            var speichern = (Func<IReadOnlyList<ZapfprofilKategorieDaten>, string, ZapfprofilKategorienErgebnis>)
                ZapfprofilHuelle.KategorienGaben(geliefert)["Speichern"];
            var neu = g.Stand.Kategorien.Select(k => k.Kopie()).ToList();
            neu[0].Anteil = 0.5;
            ZapfprofilKategorienErgebnis e = speichern(neu, g.KatalogversionVorschlag);
            Assert.True(e.Ok, e.Meldung?.Text);
            Assert.True(e.NeueZeile);
            Assert.NotEqual(geliefert, e.IdNutzungsart);
        }

        // =================================================================================
        // Parametersätze
        // =================================================================================

        /// <summary>
        /// Hausregel „Ein Parametersatz aus einer Hülle trifft nur [Parameter]": die Gaben der Editoren
        /// und die neuen Schlüssel des Zapfprofils, je mit passendem Typ.
        /// </summary>
        [Fact]
        public void Die_Parametersaetze_der_Editoren_treffen_nur_Parameter_der_Komponenten()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int frei = TwwTestdatenbank.NutzungsartAnlegen("Nutzung frei", "T1", satz);

            ParameterPruefen(typeof(TagesgangEditor), ZapfprofilHuelle.TagesgangGaben(frei, null));
            ParameterPruefen(typeof(ZapfkategorienEditor), ZapfprofilHuelle.KategorienGaben(frei));
            Assert.IsType<ZapfprofilTexte>(ZapfprofilHuelle.TagesgangGaben(frei, null)["Texte"]);
            Assert.Equal(ZapfprofilHuelle.HILFE_TAGESGANG, ZapfprofilHuelle.TagesgangGaben(frei, null)["HilfeSchluessel"]);
            Assert.Equal(ZapfprofilHuelle.HILFE_KATEGORIEN, ZapfprofilHuelle.KategorienGaben(frei)["HilfeSchluessel"]);

        }

        [Fact]
        public void Die_neuen_Schluessel_des_Zapfprofils_treffen_Parameter_des_Dialogs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            IReadOnlyDictionary<string, object> gaben = ZapfprofilHuelle.Gaben(PROJEKT, null);
            ParameterPruefen(typeof(ZapfprofilDialog), gaben);
            foreach (string k in new[] { "TagesgangGaben", "KategorienGaben", "Katalogstand", "Ladevorschlag" })
                Assert.True(gaben.ContainsKey(k), k);
            var tagesgang = (Func<int, int?, IReadOnlyDictionary<string, object>>)gaben["TagesgangGaben"];
            var d = (ZapfprofilTagesgangDaten)tagesgang(Nutzungsart("Testnutzung A (fiktiv)"), null)["Daten"];
            // Der Testsatz trägt drei Nutzungsarten: ein geänderter Tagesgang wird eine Kopie.
            Assert.True(d.Verfuegbar);
            Assert.True(d.Kopie);
            Assert.Equal(VERSION + TwwNutzungsartCtrl.KOPIEVERSION_ZUSATZ + "1", d.KatalogversionVorschlag);
            var katalog = (Func<ZapfprofilKatalogstandDaten>)gaben["Katalogstand"];
            Assert.NotEmpty(katalog().Katalog);
        }

        // =================================================================================
        // Konstruktor: Bezug des Tags und Zeilen über das Schließen hinaus
        // =================================================================================

        [Fact]
        public void Der_Konstruktor_legt_Bezugsart_und_Bezugsmenge_an_den_Tag_und_lehnt_halbe_Angaben_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            var zeilen = new[] { new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 8, VolumenL = 100, ZapftemperaturC = 45 } };

            ZapfprofilKonstruktorErgebnis halb = ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, "Tag mit Bezug", 10.0, null);
            Assert.Null(halb.Tag);
            Assert.Contains(halb.Meldungen, m => m.Kennung == "ZPG_AUS_KON_BEZUG_UNVOLLSTAENDIG");
            Assert.Contains(ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, "Tag mit Bezug", 0.0, 2).Meldungen,
                            m => m.Kennung == "ZPG_AUS_KON_BEZUG_UNVOLLSTAENDIG");
            Assert.Contains(ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, "Tag mit Bezug", 10.0, 99).Meldungen,
                            m => m.Kennung == "ZPG_AUS_KON_BEZUG_UNVOLLSTAENDIG");

            ZapfprofilBedarfstagDaten tag = ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, "Tag mit Bezug", 10.0, (int)ZapfBezugsart.Wohneinheiten).Tag;
            Assert.NotNull(tag);
            Assert.Equal(10.0, tag.Bezugsmenge);
            Assert.Equal((int)ZapfBezugsart.Wohneinheiten, tag.Bezugsart);
            Assert.Equal(10.0, tag.Kopie().Bezugsmenge);

            ZapfprofilBedarfstagDaten ohne = ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, "Tag ohne Bezug").Tag;
            Assert.Null(ohne.Bezugsmenge);
            Assert.Null(ohne.Bezugsart);

            // Der Entwurf trägt den Bezug in den Kern (Schritt 124).
            BedarfstagKatalogzeile k = ZapfprofilHuelle.EntwurfAus(new ZapfprofilAuslegungEingabeDaten
            {
                Quelle = ZapfprofilBedarfstagquelle.Konstruktor,
                Entwurf = tag
            });
            Assert.Equal(10.0, k.Bezugsmenge);
            Assert.Equal(ZapfBezugsart.Wohneinheiten, k.Bezugsart);

            // Die Wertemenge der Bezugsarten kommt aus dem Schema.
            ZapfprofilAuslegungStartDaten start = ZapfprofilHuelle.AuslegungStart(PROJEKT, Zonen(), null, ZapfprofilStufe.Einfach);
            Assert.Equal(TwwSchema.Werte(TwwSchema.BEZUGSART_WERTE), start.Bezugsarten.Select(b => b.Id).ToArray());
            Assert.Equal("Wohneinheiten", start.Bezugsarten.Single(b => b.Id == 2).Name);
            Assert.Equal(new[] { 1, 2, 3, 4 }, start.Fuellstandbezuege.Select(b => b.Id).ToArray());
        }

        /// <summary>
        /// N11 (j): Die Zeilen eines konstruierten Tags samt Bezug überdauern das Schließen des
        /// Zapfprofils — der Behälter des Bedarfsprofil-Dialogs hält den Arbeitsstand des Kerns, ein
        /// erneutes Öffnen beginnt den Konstruktor mit ihnen.
        /// </summary>
        [Fact]
        public void Die_Zeilen_des_Konstruktors_ueberdauern_das_Schliessen_des_Zapfprofils()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);
            var zeilen = new List<ZapfprofilKonstruktorZeileDaten>
            {
                new() { BeginnH = 7, EndeH = 8, VolumenL = 100, ZapftemperaturC = 45, Verbraucher = "Küche" },
                new() { BeginnH = 19, EndeH = 20, VolumenL = 60, ZapftemperaturC = 40 }
            };
            ZapfprofilBedarfstagDaten tag = ZapfprofilHuelle.BedarfstagKonstruieren(zeilen, "Tag über Schließen", 4.0,
                                                                                    (int)ZapfBezugsart.Wohneinheiten).Tag;
            Assert.NotNull(tag);

            var behaelter = new ZapfprofilBehaelter(PROJEKT);
            ZapfprofilEinstieg einstieg = ZapfprofilHuelle.Einstieg(PROJEKT, behaelter.Wege());
            ZapfprofilEingabeDaten eingabe = Zonen();
            eingabe.Auslegung = new ZapfprofilAuslegungEingabeDaten { Quelle = ZapfprofilBedarfstagquelle.Konstruktor, Entwurf = tag };
            einstieg.Uebernommen(new ZapfprofilErgebnisDaten(eingabe));
            Assert.Equal(2, behaelter.Arbeitsstand.Konstruktorzeilen.Count);

            // Erneut geöffnet: Der Parametersatz der Auslegung beginnt mit den Zeilen und dem Bezug.
            IReadOnlyDictionary<string, object> gaben = einstieg.Gaben();
            var daten = (ZapfprofilDaten)gaben["Daten"];
            var ausl = (Func<ZapfprofilEingabeDaten, bool, IReadOnlyDictionary<string, object>>)gaben["AuslegungGaben"];
            var start = (ZapfprofilAuslegungStartDaten)ausl(daten.Eingabe, false)["Daten"];
            Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, start.Eingabe.Quelle);
            Assert.NotNull(start.Eingabe.Entwurf);
            Assert.Equal(new[] { "Küche", "" }, start.Eingabe.Entwurf.Konstruktorzeilen.Select(z => z.Verbraucher).ToArray());
            Assert.Equal(60.0, start.Eingabe.Entwurf.Konstruktorzeilen[1].VolumenL);
            Assert.Equal(4.0, start.Eingabe.Entwurf.Bezugsmenge);
            Assert.Equal((int)ZapfBezugsart.Wohneinheiten, start.Eingabe.Entwurf.Bezugsart);

            // Ein OK des Zapfprofils ohne Auslegung behält Entwurf und Zeilen des Behälters.
            einstieg.Uebernommen(new ZapfprofilErgebnisDaten(daten.Eingabe));
            Assert.Equal(2, behaelter.Arbeitsstand.Konstruktorzeilen.Count);
            Assert.Equal(4.0, behaelter.Arbeitsstand.BedarfstagEntwurf.Bezugsmenge);
        }

        // =================================================================================
        // Ladeleistungs-Vorschlag des Zapfprofils
        // =================================================================================

        [Fact]
        public void Der_Vorschlag_der_Ladeleistung_kommt_aus_der_Auslegung_zum_Arbeitsstand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            AuslegungTestbau.ParameterEinspielen(VERSION);

            Assert.Null(ZapfprofilHuelle.Ladevorschlag(PROJEKT, new ZapfprofilEingabeDaten(), null));
            ZapfprofilSchaetzhilfeDaten h = ZapfprofilHuelle.Ladevorschlag(PROJEKT, Zonen(), ZapfprofilCtrl.Lies(PROJEKT));
            Assert.NotNull(h);
            Assert.Equal("kW", h.Einheit);
            Assert.True(h.Vorschlag > 0, "Kein Vorschlag");
            Assert.NotEqual("", h.Rechenweg);

            var gaben = ZapfprofilHuelle.Gaben(PROJEKT, null);
            var ladevorschlag = (Func<ZapfprofilEingabeDaten, ZapfprofilSchaetzhilfeDaten>)gaben["Ladevorschlag"];
            Assert.Equal(h.Vorschlag, ladevorschlag(Zonen()).Vorschlag);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static ZapfprofilEingabeDaten Zonen() => new ZapfprofilEingabeDaten
        {
            Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = Nutzungsart("Testnutzung A (fiktiv)"), Bezugsmenge = 8 } }
        };

        private static int Nutzungsart(string name)
        {
            object id = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", name), new DbParam("@k", VERSION));
            return id == null ? 0 : Convert.ToInt32(id, CultureInfo.InvariantCulture);
        }

        private static void ParameterPruefen(Type komponente, IReadOnlyDictionary<string, object> gaben)
        {
            foreach (KeyValuePair<string, object> g in gaben)
            {
                System.Reflection.PropertyInfo p = komponente.GetProperty(g.Key);
                Assert.True(p != null, komponente.Name + " kennt keinen Parameter „" + g.Key + "“.");
                Assert.True(p.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.ParameterAttribute), true).Length == 1,
                            komponente.Name + "." + g.Key + " ist kein [Parameter].");
                Assert.True(g.Value == null || p.PropertyType.IsInstanceOfType(g.Value),
                            komponente.Name + "." + g.Key + ": " + g.Value?.GetType().Name + " passt nicht zu " + p.PropertyType.Name);
            }
        }

        private static string Text(string schluessel, CultureInfo kultur)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, kultur);
    }
}
