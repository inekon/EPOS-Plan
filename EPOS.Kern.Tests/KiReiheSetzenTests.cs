using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Setzweg der ZAHLENREIHEN (Welle #458 Stufe 3b): <c>reihe_setzen</c> ueber die
    /// Maskenbruecke — ganz, ab einer Stelle, je Stelle; Laenge, Grenzen, Schreibschutz
    /// und Lesemodus abgelehnt; die gekuerzte Bestaetigung und die Gegenproben an
    /// <c>feld_setzen</c>.
    /// </summary>
    /// <remarks>
    /// Die Masken stehen hier ohne Oberflaeche: Ein Pruefdialog meldet die Reihe seiner
    /// Katalogmaske mit denselben Zugaengen an, die <c>KiMaskenanmeldung</c> baut. Den
    /// Weg durch die echten Dialoge halten deren bunit-Faelle.
    /// <para><b>In der seriellen Sammlung</b>, denn Maskenbruecke und Schreibnaht sind
    /// prozessweiter Zustand — dieselbe Lage wie bei <c>KiRegisterS3Tests</c>.</para>
    /// </remarks>
    [Collection("Testdatenbank")]
    public sealed class KiReiheSetzenTests : IDisposable
    {
        private readonly Func<bool> _schreibrechtVorher = Schreibnaht.Schreibrecht;

        // Deutsche Texte werden geprueft - alle vier Kulturwerte stehen fest (der
        // CI-Laeufer laeuft unter en-US).
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public KiReiheSetzenTests()
        {
            Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt;
            KiMaskenbruecke.Leeren();
        }

        public void Dispose()
        {
            KiMaskenbruecke.Leeren();
            Schreibnaht.Schreibrecht = _schreibrechtVorher;
            _kultur.Dispose();
        }

        // ==================================================================
        //  Setzen: ganz, ab einer Stelle, je Stelle
        // ==================================================================

        [Fact]
        public async Task Die_ganze_Reihe_wird_in_einem_Aufruf_gesetzt()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte");
            double[] werte = Enumerable.Range(1, 12).Select(i => i * 1.5).ToArray();

            KiErgebnis ergebnis = await Mit("reihe_setzen", Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", werte));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Equal(werte.Select(w => (double?)w), dialog.Werte);
            Assert.True(dialog.Aufgefrischt, "Die Maske wurde nicht aufgefrischt.");
            Assert.Contains("12 von 12", ergebnis.Text, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Ein_Tag_der_Wochenreihe_ist_ein_Ausschnitt_ab_seiner_ersten_Stunde()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPPROFIL, "wochenwerte", vorbelegung: 1.0);
            double[] dienstag = Enumerable.Repeat(2.5, 24).ToArray();

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.TYPPROFIL, "wochenwerte", dienstag, ab: 25));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            for (int i = 0; i < 168; i++)
                Assert.Equal(i >= 24 && i < 48 ? 2.5 : 1.0, dialog.Werte[i]);

            // Die Rueckmeldung nennt die Stellen beim Namen, wie die Maske sie zeigt.
            Assert.Contains("Dienstag, Stunde 1", ergebnis.Text, StringComparison.Ordinal);
            Assert.Contains("Dienstag, Stunde 24", ergebnis.Text, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Eine_einzelne_Stelle_wird_mit_ab_gesetzt_der_Rest_bleibt()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte", vorbelegung: 5.0);

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", new[] { 7.25 }, ab: 3));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Equal(new double?[] { 5, 5, 7.25, 5, 5, 5, 5, 5, 5, 5, 5, 5 }, dialog.Werte);
        }

        // ==================================================================
        //  Ablehnungen: Laenge, Grenzen, Schutz, Lesemodus, falscher Weg
        // ==================================================================

        [Fact]
        public async Task Ohne_ab_muss_die_Liste_die_ganze_Reihe_tragen()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte", vorbelegung: 5.0);

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", new double[11]));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("12", ergebnis.Text, StringComparison.Ordinal);
            Assert.Contains("11", ergebnis.Text, StringComparison.Ordinal);
            Assert.All(dialog.Werte, w => Assert.Equal(5.0, w));
        }

        [Fact]
        public async Task Ein_Ausschnitt_der_ueber_das_Ende_ragt_wird_abgelehnt()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPPROFIL, "wochenwerte", vorbelegung: 1.0);

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                Aufruf(KiMaskennamen.TYPPROFIL, "wochenwerte", Enumerable.Repeat(3.0, 24).ToArray(), ab: 160));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("160", ergebnis.Text, StringComparison.Ordinal);
            Assert.All(dialog.Werte, w => Assert.Equal(1.0, w));
        }

        [Fact]
        public async Task Ein_Wert_ausserhalb_der_Grenzen_des_Eingabefeldes_wird_mit_Stelle_abgelehnt()
        {
            var dialog = new Pruefdialog(KiMaskennamen.LEISTUNGSPREISREIHE, "monatssaetze", vorbelegung: 10.0);
            double[] werte = Enumerable.Repeat(10.0, 12).ToArray();
            werte[4] = -1;

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.LEISTUNGSPREISREIHE, "monatssaetze", werte));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("Mai", ergebnis.Text, StringComparison.Ordinal);
            Assert.Contains("-1", ergebnis.Text, StringComparison.Ordinal);
            Assert.All(dialog.Werte, w => Assert.Equal(10.0, w));
        }

        [Fact]
        public async Task Ein_geschuetzter_Satz_wird_benannt_abgelehnt()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte", vorbelegung: 5.0) { Geschuetzt = true };

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", new[] { 1.0 }, ab: 1));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.All(dialog.Werte, w => Assert.Equal(5.0, w));
        }

        [Fact]
        public async Task Im_Lesemodus_wird_benannt_abgelehnt()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte", vorbelegung: 5.0);

            Schreibnaht.Schreibrecht = () => false;
            try
            {
                KiErgebnis ergebnis = await Mit("reihe_setzen",
                                                Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", new[] { 1.0 }, ab: 1));

                Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
                Assert.All(dialog.Werte, w => Assert.Equal(5.0, w));
            }
            finally { Schreibnaht.Schreibrecht = Schreibnaht.ImmerErlaubt; }
        }

        [Fact]
        public async Task Ohne_Aenderung_gibt_es_nichts_zu_bestaetigen()
        {
            new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte", vorbelegung: 5.0);

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", new[] { 5.0 }, ab: 7));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
        }

        [Fact]
        public async Task Reihe_setzen_auf_ein_Einzelfeld_nennt_feld_setzen()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte");

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.TYPSTAMM, "beschreibung", new[] { 1.0 }));

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("feld_setzen", ergebnis.Text, StringComparison.Ordinal);
            Assert.Equal("", dialog.Beschreibung);
        }

        [Fact]
        public async Task Feld_setzen_auf_eine_Zahlenreihe_nennt_reihe_setzen()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte", vorbelegung: 5.0);

            KiErgebnis ergebnis = await Mit("feld_setzen", new Dictionary<string, object>
            {
                ["maske"] = KiMaskennamen.TYPSTAMM,
                ["feld"] = "monatswerte",
                ["wert"] = "1; 2; 3"
            });

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("reihe_setzen", ergebnis.Text, StringComparison.Ordinal);
            Assert.All(dialog.Werte, w => Assert.Equal(5.0, w));
        }

        // ==================================================================
        //  Bestaetigung, Befund, Lesen, Erklaeren, Protokoll
        // ==================================================================

        /// <summary>
        /// <b>Gekuerzt, aber nie verschwiegen:</b> Die Bestaetigung einer ganzen Woche nennt
        /// die Zahl der geaenderten Stellen und die ersten davon — nicht 168 Zeilen.
        /// </summary>
        [Fact]
        public async Task Die_Bestaetigung_nennt_alt_und_neu_gekuerzt()
        {
            new Pruefdialog(KiMaskennamen.TYPPROFIL, "wochenwerte", vorbelegung: 1.0);

            KiAusfuehrung schicht = Frisch();
            KiAufruf aufruf = Geprueft(schicht, "reihe_setzen",
                Aufruf(KiMaskennamen.TYPPROFIL, "wochenwerte", Enumerable.Repeat(4.5, 168).ToArray()));

            KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(aufruf, CancellationToken.None);

            Assert.True(vorbereitung.Freigabe != null, vorbereitung.Ablehnung?.Text);
            string text = vorbereitung.Freigabe.Text;
            Assert.Contains("168 von 168", text, StringComparison.Ordinal);
            Assert.Contains("Montag, Stunde 1) · 1 → 4,5", text, StringComparison.Ordinal);
            Assert.Contains("weitere", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Sonntag, Stunde 24", text, StringComparison.Ordinal);
            Assert.Contains("(168 Werte)", text, StringComparison.Ordinal);    // die Angaben gekuerzt
        }

        [Fact]
        public async Task Der_Befund_der_Dialogpruefung_steht_im_Ergebnis()
        {
            new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte") { Befund = "Bitte Monatswert März als Zahl eingeben." };

            KiErgebnis ergebnis = await Mit("reihe_setzen",
                                            Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", new[] { 1.0 }, ab: 1));

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Contains(ergebnis.Meldungen, m => m.Contains("März", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Dialog_lesen_zeigt_die_Reihe_als_Liste_und_nennt_den_Weg()
        {
            var dialog = new Pruefdialog(KiMaskennamen.TYPSTAMM, "monatswerte", vorbelegung: 2.5);

            KiErgebnis ergebnis = await Frisch().AusfuehrenAsync(
                "dialog_lesen", new Dictionary<string, object> { ["maske"] = KiMaskennamen.TYPSTAMM });

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            var zeile = ergebnis.Zeilen.First(z => Equals(z["name"], "monatswerte"));
            Assert.Equal("ZahlListe", zeile["typ"]);
            Assert.Equal(string.Join("; ", Enumerable.Repeat("2,5", 12)), zeile["wert"]);
            Assert.Contains("reihe_setzen", (string)zeile["hinweis"], StringComparison.Ordinal);
            Assert.Contains("12 Werte", (string)zeile["hinweis"], StringComparison.Ordinal);
        }

        [Fact]
        public async Task Dialog_parameter_erklaeren_nennt_Umfang_und_Bereich()
        {
            new Pruefdialog(KiMaskennamen.LEISTUNGSPREISREIHE, "monatssaetze");

            KiErgebnis ergebnis = await Frisch().AusfuehrenAsync(
                "dialog_parameter_erklaeren",
                new Dictionary<string, object>
                { ["maske"] = KiMaskennamen.LEISTUNGSPREISREIHE, ["feld"] = "monatssaetze" });

            Assert.Equal(KiStatus.Ausgefuehrt, ergebnis.Status);
            Assert.Contains("12 Werte, Januar bis Dezember", ergebnis.Text, StringComparison.Ordinal);
            Assert.Contains("0 bis 100000", ergebnis.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die GEGENPROBE zur Vorbedingung von <c>dialog_parameter_erklaeren</c>: Ein Feld,
        /// das die Maske nicht fuehrt, wird weiter benannt abgelehnt — erklaert wird nur,
        /// was es gibt.
        /// </summary>
        [Fact]
        public async Task Dialog_parameter_erklaeren_lehnt_ein_unbekanntes_Feld_ab()
        {
            new Pruefdialog(KiMaskennamen.LEISTUNGSPREISREIHE, "monatssaetze");

            KiErgebnis ergebnis = await Frisch().AusfuehrenAsync(
                "dialog_parameter_erklaeren",
                new Dictionary<string, object>
                { ["maske"] = KiMaskennamen.LEISTUNGSPREISREIHE, ["feld"] = "gibtsnicht" });

            Assert.Equal(KiStatus.Abgelehnt, ergebnis.Status);
            Assert.Contains("gibtsnicht", ergebnis.Text, StringComparison.Ordinal);
        }

        [Fact]
        public void Das_Protokoll_traegt_jeden_Wert()
        {
            KiAufruf aufruf = Geprueft(Frisch(), "reihe_setzen",
                Aufruf(KiMaskennamen.TYPSTAMM, "monatswerte", Enumerable.Range(1, 12).Select(i => i + 0.5).ToArray()));

            string json = aufruf.AlsJson();
            Assert.Contains("[1.5,2.5,3.5,4.5,5.5,6.5,7.5,8.5,9.5,10.5,11.5,12.5]", json, StringComparison.Ordinal);
        }

        [Fact]
        public void Die_Werkzeugliste_zerlegt_eine_Reihe_ohne_das_Dezimalkomma_zu_brechen()
        {
            KiAktion aktion = Frisch().Register.Finde("reihe_setzen");
            IReadOnlyDictionary<string, object> werte = KiWerkzeugWerte.Sammeln(aktion,
                new Dictionary<string, string> { ["feld"] = "monatswerte", ["werte"] = "1,5; 2  3,25" });

            KiPruefErgebnis p = KiPruefung.Pruefe(aktion, werte);
            Assert.True(p.Gueltig, p.FehlerText());
            Assert.Equal(new[] { 1.5, 2.0, 3.25 }, p.Aufruf.ZahlListe("werte"));
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static KiAusfuehrung Frisch() => new KiAusfuehrung { Schreibrecht = () => true };

        private static Dictionary<string, object> Aufruf(string maske, string feld, double[] werte, int ab = 0)
        {
            var d = new Dictionary<string, object>
            {
                ["maske"] = maske,
                ["feld"] = feld,
                ["werte"] = werte.Cast<object>().ToList()
            };
            if (ab > 0) d["ab"] = (long)ab;
            return d;
        }

        private static KiAufruf Geprueft(KiAusfuehrung schicht, string name, IReadOnlyDictionary<string, object> werte)
        {
            KiPruefErgebnis geprueft = KiPruefung.Pruefe(schicht.Register, name, werte);
            Assert.True(geprueft.Gueltig, geprueft.FehlerText());
            return geprueft.Aufruf;
        }

        /// <summary>Vorbereiten, freigeben, ausfuehren — der ganze Weg einer Formularaktion.</summary>
        private static async Task<KiErgebnis> Mit(string name, IReadOnlyDictionary<string, object> werte)
        {
            KiAusfuehrung schicht = Frisch();
            KiAufruf aufruf = Geprueft(schicht, name, werte);
            KiVorbereitung vorbereitung = await schicht.VorbereitenAsync(aufruf, CancellationToken.None);
            if (vorbereitung.Freigabe == null) return vorbereitung.Ablehnung;

            vorbereitung.Freigabe.Erteilen();
            return await schicht.AusfuehrenAsync(aufruf, vorbereitung.Freigabe, CancellationToken.None);
        }

        /// <summary>
        /// Eine Katalogmaske ohne Oberflaeche: ihre Zahlenreihe und ihre Beschreibung,
        /// angemeldet mit Auffrischen, Pruefen und Schreibschutz.
        /// </summary>
        private sealed class Pruefdialog
        {
            internal double?[] Werte { get; private set; }
            internal string Beschreibung { get; private set; } = "";
            internal bool Aufgefrischt { get; private set; }
            internal bool Geschuetzt { get; set; }
            internal string Befund { get; set; } = "";

            internal Pruefdialog(string maske, string reihe, double? vorbelegung = null)
            {
                KiDialog eintrag = KiDialoge.Katalog.Finde(maske);
                KiDialogFeld feld = eintrag.FindeFeld(reihe);
                Assert.True(feld.IstReihe, reihe + " ist keine Zahlenreihe");

                Werte = Enumerable.Repeat(vorbelegung, feld.Reihe.Laenge).ToArray();

                var zugaenge = new List<KiFeldzugang>
                {
                    new KiFeldzugang(feld, () => (double?[])Werte.Clone(),
                                     w => Werte = (double?[])w, typeof(double?[]))
                };

                KiDialogFeld beschreibung = eintrag.FindeFeld("beschreibung");
                if (beschreibung != null)
                    zugaenge.Add(new KiFeldzugang(beschreibung, () => Beschreibung,
                                                  w => Beschreibung = (string)w, typeof(string)));

                var haken = new KiMaskenhaken
                {
                    Auffrischen = () => Aufgefrischt = true,
                    Pruefen = () => Befund,
                    Schreibgeschuetzt = () => Geschuetzt
                };

                KiMaskenbruecke.Anmelden(eintrag.Maskenname, eintrag, zugaenge, haken);
            }
        }
    }
}
