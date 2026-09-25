using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle der Messdaten, des Vergleichs und der Kalibrierung</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5; Stufe Z5, Gruppe 3):
    ///
    /// <list type="bullet">
    /// <item>die Wache über <see cref="ZapfprofilHuelle.VALIDIERUNGSHINWEISE"/> — jede Kennung der
    /// beiden Kern-Dateien steht darin und hat ihren Titel in beiden Sprachen;</item>
    /// <item>der <b>ganze Weg</b> auf einer Arbeitskopie der Testdatenbank (Projekt 1007): eine
    /// erfundene Reihe einspielen, sie in der Liste finden, den Vergleich rechnen, den
    /// Jahresmesswert daraus setzen, den Kalibriervorschlag sehen und ihn in eine Anwenderkopie
    /// übernehmen;</item>
    /// <item>die benannten Ablehnungen: ohne Reihe, ohne Projekt, mit einer Reihe, die es nicht
    /// gibt.</item>
    /// </list>
    ///
    /// <para>Ohne Testdatenbank schweigen die Fälle mit Datenbank. <b>Alle Werte erfunden</b> — die
    /// Reihe entsteht aus einem runden Tagesmuster, kein gemessener Wert eines Objekts steht in
    /// dieser Datei.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilHuelleMessreihenTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const string REIHE = "Probenzaehler (erfunden)";

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        /// <summary>Ein erfundenes Tagesmuster: 24 Stundenwerte [kWh], Summe 12 — Vormittag und Abend.</summary>
        private static readonly double[] TAGESMUSTER =
        {
            0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.5, 2.0, 1.0, 0.5, 0.5, 0.5,
            0.5, 0.5, 0.5, 0.5, 0.5, 1.0, 1.5, 0.5, 0.5, 0.0, 0.0, 0.0
        };

        // =================================================================================
        // Wache über die Hinweistitel
        // =================================================================================

        /// <summary>
        /// Jede Kennung, die <c>Messvergleich.cs</c> und <c>Messkalibrierung.cs</c> als
        /// <c>ZapfSatz</c> erzeugen, steht in <see cref="ZapfprofilHuelle.VALIDIERUNGSHINWEISE"/>
        /// und hat ihren Titel <c>ZPG_WARN_…</c> in <b>beiden</b> Sprachen — sonst stünde in der
        /// Warnliste der Sammeltitel „Hinweis“ statt des Gegenstands. Die Liste trägt keine
        /// Kennung, die es nicht gibt, und keine zweimal.
        /// </summary>
        [Fact]
        public void Jede_Kennung_des_Vergleichs_und_der_Kalibrierung_hat_ihren_Titel()
        {
            var muster = new Regex("ZapfSatz[.]Neu[(]\"(?<k>MESS(?:VERGLEICH|KALIBRIERUNG)_[A-Z0-9_]+)\"");
            var kennungen = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string datei in new[] { "Messvergleich.cs", "Messkalibrierung.cs" })
                foreach (Match m in muster.Matches(File.ReadAllText(
                             Pfad("EPOS.Kern", "Allgemein", "Zapfprofil", datei))))
                    kennungen.Add(m.Groups["k"].Value);
            Assert.True(kennungen.Count >= 25, "Nur " + kennungen.Count + " Kennungen gefunden.");

            string[] ungelistet = kennungen.Where(k => !ZapfprofilHuelle.VALIDIERUNGSHINWEISE.Contains(k)).ToArray();
            Assert.True(ungelistet.Length == 0, "Nicht in VALIDIERUNGSHINWEISE: " + string.Join(", ", ungelistet));
            string[] erfunden = ZapfprofilHuelle.VALIDIERUNGSHINWEISE.Where(k => !kennungen.Contains(k)).ToArray();
            Assert.True(erfunden.Length == 0, "Kennung ohne Stelle im Kern: " + string.Join(", ", erfunden));
            Assert.Equal(ZapfprofilHuelle.VALIDIERUNGSHINWEISE.Length,
                         ZapfprofilHuelle.VALIDIERUNGSHINWEISE.Distinct().Count());

            string[] ohneTitel = ZapfprofilHuelle.VALIDIERUNGSHINWEISE.Select(k => "ZPG_WARN_" + k)
                .Where(k => string.IsNullOrEmpty(Text(k, DE)) || string.IsNullOrEmpty(Text(k, EN))).ToArray();
            Assert.True(ohneTitel.Length == 0, "Ohne Titel: " + string.Join(", ", ohneTitel));

            // Eine Warnzeile traegt den Titel ihrer Kennung, nie den Sammeltitel.
            ZapfprofilWarnDaten w = ZapfprofilHuelle.Validierungswarnung(ZapfSatz.Neu("MESSVERGLEICH_TEILJAHR", 90, 365));
            Assert.Equal("ZPG_WARN_MESSVERGLEICH_TEILJAHR", w.Kennung);
            Assert.Equal("Teiljahr der Messung", w.Titel);
            Assert.Equal(ZapfprofilWarnstufe.Hinweis, w.Stufe);
            Assert.NotEqual("", w.Text);
        }

        // =================================================================================
        // Der ganze Weg auf der Testdatenbank
        // =================================================================================

        /// <summary>
        /// <b>Einspielen → Liste → Vergleich → Kalibrierung → Vorschlag → Kopie</b> auf einer
        /// Arbeitskopie der Testdatenbank. Die Reihe deckt ein ganzes Jahr mit demselben
        /// Tagesmuster ab; die Zone rechnet mit einer Nichtwohn-Nutzungsart des Testkatalogs.
        ///
        /// <para>Der Vergleich gibt nur Verhältniszahlen: Das Energieverhältnis ist positiv, die
        /// Bandgrenzen liegen unter 1 (ein hohes Quantil der Dauerlinie, nicht ihr Maximum), und
        /// die Lage der Messspitze ist entschieden. Nach der Kalibrierung trägt das Feld
        /// Jahresmesswert die Energie der Reihe in kWh/a samt Quelle und Zeitraum.</para>
        /// </summary>
        [Fact]
        public void Der_ganze_Weg_von_der_Datei_bis_zur_Anwenderkopie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // --- Einspielen: der Weg der Huelle, aus einer erfundenen Datei -------------------
            string datei = Path.Combine(Path.GetTempPath(), "epos_messreihe_probe.csv");
            File.WriteAllText(datei, Csv(Zapfkalender.TAGE), new UTF8Encoding(false));
            try
            {
                var eingaben = new TwwMessreiheneingabeDaten { Bezeichnung = REIHE, Quelle = "Probe (erfunden)" };
                TwwMessreihenpruefungDaten pruefung = ZapfprofilHuelle.MessreihePruefen(PROJEKT, datei, eingaben);
                Assert.False(pruefung.Abgebrochen, pruefung.Abbruch);
                Assert.Equal(REIHE, pruefung.Bezeichnung);
                Assert.False(pruefung.ErsetztVorhandene);
                Assert.NotEmpty(pruefung.Angaben);

                TwwMessreihenergebnisDaten eingespielt = ZapfprofilHuelle.MessreiheEinspielen(PROJEKT, datei, eingaben);
                Assert.True(eingespielt.Ok, eingespielt.Meldung);
                Assert.Equal(REIHE, eingespielt.Bezeichnung);

                // --- Die Liste: eine Zeile mit den Kopfangaben --------------------------------
                TwwMessreihenstandDaten stand = ZapfprofilHuelle.Messreihenstand(PROJEKT);
                Assert.Equal("", stand.Grund);
                TwwMessreiheDaten zeile = Assert.Single(stand.Reihen, r => r.Bezeichnung == REIHE);
                Assert.Equal(60, zeile.AufloesungMin);
                Assert.Equal((double)Zapfkalender.TAGE, zeile.Tage, 6);
                Assert.Equal(Zapfkalender.STUNDEN_JAHR, zeile.Schritte);
                // Das Muster traegt acht Nullstunden je Tag (0 bis 4 und 21 bis 23) - die Liste
                // zaehlt sie, ohne zu behaupten, es seien Luecken.
                int nullstunden = TAGESMUSTER.Count(w => w == 0.0);
                Assert.Equal(8, nullstunden);
                Assert.Equal(nullstunden * Zapfkalender.TAGE, zeile.Nulllaeufe);
                Assert.Equal((double)nullstunden / Zapfkalender.STUNDEN_TAG, zeile.Nullanteil, 9);
                Assert.True(stand.MindesttageVorschlag >= 1);

                // --- Der Vergleich ------------------------------------------------------------
                int art = Nichtwohnart();
                var eingabe = new ZapfprofilEingabeDaten
                {
                    Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = art, Bezugsmenge = 20 } }
                };
                ZapfprofilStand basis = ZapfprofilCtrl.Lies(PROJEKT);
                ZapfprofilMessvergleichDaten v = ZapfprofilHuelle.Vergleichsbericht(
                    PROJEKT, eingabe, basis, REIHE, CancellationToken.None);
                Assert.True(v.Ok, v.Abbruch);
                Assert.Equal(REIHE, v.Reihe);
                Assert.True(v.EnergieVerhaeltnis > 0.0);
                Assert.Equal((v.EnergieVerhaeltnis ?? 0.0) - 1.0, v.EnergieAbweichung ?? 0.0, 9);
                Assert.NotNull(v.Spitzenverhaeltnis);
                Assert.NotNull(v.BandUnten);
                Assert.NotNull(v.BandOben);
                Assert.True(v.BandUnten <= v.BandOben);
                Assert.True(v.BandOben <= 1.0, "Das Band ist ein Quantil der Dauerlinie, nicht ihr Maximum.");
                Assert.Equal(Zapfkalender.STUNDEN_JAHR, v.Dauerlinienwerte);
                Assert.NotEqual(ZapfprofilSpitzenlage.Unbestimmt, v.Lage);
                // Ohne Ensemble bleibt die Streuung offen - und wird benannt.
                Assert.Null(v.Streubreite);
                Assert.Contains(v.Hinweise, h => h.Kennung == "ZPG_WARN_MESSVERGLEICH_OHNE_ENSEMBLE");
                // Die Form: je Tagtyp eine Zeile, jede mit Tagen beider Seiten.
                Assert.NotEmpty(v.Form);
                Assert.All(v.Form, f => Assert.True(f.TageGemessen > 0 && f.TageGerechnet > 0));
                Assert.NotNull(v.Formmass);
                Assert.True(v.Formschwelle > 0.0);
                Assert.NotNull(v.MonateGroessteAbweichung);
                Assert.InRange(v.MonateGroessterMonat, 1, 12);
                // Die Einheitenzahl ist die Summe der Bezugsmengen.
                Assert.Equal(20, v.Einheiten);
                Assert.NotNull(v.Skalierungsmass);

                // --- „Aus Messreihe kalibrieren" ----------------------------------------------
                ZapfprofilMesskalibrierungDaten k = ZapfprofilHuelle.MesswertAusReihe(PROJEKT, eingabe, basis, REIHE, 0, CancellationToken.None);
                Assert.True(k.Ok, k.Abbruch);
                Assert.Equal((int)ZapfprofilMesswerteinheit.KwhJeJahr, k.EinheitId);
                Assert.Equal(TAGESMUSTER.Sum() * Zapfkalender.TAGE, k.Wert ?? 0.0, 6);
                Assert.False(k.Hochgerechnet, "Eine Reihe über ein ganzes Jahr wird nicht hochgerechnet.");
                Assert.Contains("2025-01-01", k.Zeitraum);
                // Die Quelle des Messwerts nennt die REIHE - das Feld sagt, woher der Wert kommt.
                Assert.Contains(REIHE, k.Quelle);
                Assert.NotNull(k.BilanzgrenzeId);

                // --- Der Vorschlag als Vorschau ----------------------------------------------
                ZapfprofilVorschlagDaten vs = ZapfprofilHuelle.Kalibriervorschlag(PROJEKT, eingabe, basis, REIHE, 0);
                Assert.True(vs.Ok, vs.Abbruch);
                Assert.Equal(Zapfkalender.TAGE, vs.VolleTage);
                Assert.Equal(TAGESMUSTER.Sum(), vs.TagesbedarfKwh, 9);
                Assert.Equal(TAGESMUSTER.Sum() / 20.0, vs.TagesbedarfJeEinheitKwh, 9);
                Assert.Equal(7, vs.Wochenfaktoren.Count);
                Assert.Equal(1.0, vs.Wochenfaktoren.Sum(), 9);
                Assert.NotEmpty(vs.Tagesgaenge);
                Assert.All(vs.Tagesgaenge, g =>
                {
                    Assert.Equal(24, g.Anteile.Count);
                    Assert.Equal(1.0, g.Anteile.Sum(), 9);
                });
                Assert.NotEqual("", vs.Kopie);
                Assert.NotEqual(vs.Vorlage, vs.Kopie);

                // --- Die Uebernahme: eine Anwenderkopie, die Vorlage unberuehrt ---------------
                Nutzungsart vorher = ZapfprofilCtrl.LiesNutzungsart(art);
                ZapfprofilVorschlagErgebnisDaten e = ZapfprofilHuelle.VorschlagUebernehmen(
                    PROJEKT, eingabe, basis, REIHE, 0);
                Assert.True(e.Ok, e.Abbruch);
                Assert.True(e.IdNutzungsart > 0);
                Assert.NotEqual(art, e.IdNutzungsart);
                Assert.Contains(vs.Kopie.Split(' ')[0], e.Kopie);
                Assert.NotEqual("", e.Meldung);

                Nutzungsart kopie = ZapfprofilCtrl.LiesNutzungsart(e.IdNutzungsart);
                Assert.Equal(ZapfKatalogstatus.Eigen, kopie.Status);
                Assert.Equal(vs.TagesbedarfJeEinheitKwh, kopie.BedarfJeNiveauKwhJeEinheitTag[1], 9);
                // Die Vorlage bleibt Wert fuer Wert, wie sie war (K7).
                Nutzungsart nachher = ZapfprofilCtrl.LiesNutzungsart(art);
                Assert.Equal(vorher.BedarfJeNiveauKwhJeEinheitTag, nachher.BedarfJeNiveauKwhJeEinheitTag);
                Assert.Equal(vorher.Wochenfaktoren, nachher.Wochenfaktoren);
            }
            finally
            {
                try { File.Delete(datei); } catch { }
            }
        }

        /// <summary>
        /// Das Löschen nimmt die Reihe weg; danach ist der Vergleich benannt nicht rechenbar — nie
        /// ein stilles Ergebnis zu einer Reihe, die es nicht gibt.
        /// </summary>
        [Fact]
        public void Nach_dem_Loeschen_ist_der_Vergleich_benannt_nicht_rechenbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Messreihe reihe = Erfunden(40);
            Assert.True(TwwMessreihenCtrl.Importieren(PROJEKT, reihe).Ok);
            Assert.Single(ZapfprofilHuelle.Messreihenstand(PROJEKT).Reihen, r => r.Bezeichnung == REIHE);

            TwwMessreihenergebnisDaten weg = ZapfprofilHuelle.MessreiheLoeschen(PROJEKT, REIHE);
            Assert.True(weg.Ok, weg.Meldung);
            Assert.Empty(ZapfprofilHuelle.Messreihenstand(PROJEKT).Reihen.Where(r => r.Bezeichnung == REIHE));

            var eingabe = new ZapfprofilEingabeDaten
            {
                Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = Nichtwohnart(), Bezugsmenge = 20 } }
            };
            ZapfprofilMessvergleichDaten v = ZapfprofilHuelle.Vergleichsbericht(
                PROJEKT, eingabe, ZapfprofilCtrl.Lies(PROJEKT), REIHE, CancellationToken.None);
            Assert.False(v.Ok);
            Assert.NotEqual("", v.Abbruch);
            Assert.StartsWith("ZPG_SATZ_", v.Kennung);

            // Ein zweites Loeschen ist folgenlos und sagt es.
            TwwMessreihenergebnisDaten nochmal = ZapfprofilHuelle.MessreiheLoeschen(PROJEKT, REIHE);
            Assert.False(nochmal.Ok);
            Assert.Contains("nichts zu löschen", nochmal.Meldung);
        }

        /// <summary>
        /// <b>Ein Teiljahr wird hochgerechnet und das benannt</b> (4.8): Der Jahresmesswert einer
        /// Reihe über vierzig Tage ist größer als ihre Energie, und der Hinweis nennt die
        /// Hochrechnung. Ohne Messreihe und ohne Bezugsmenge kommt je eine benannte Ablehnung.
        /// </summary>
        [Fact]
        public void Ein_Teiljahr_wird_benannt_hochgerechnet_und_jede_Ablehnung_ist_benannt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Messreihe reihe = Erfunden(40);
            Assert.True(TwwMessreihenCtrl.Importieren(PROJEKT, reihe).Ok);
            var eingabe = new ZapfprofilEingabeDaten
            {
                Zonen = { new ZapfprofilZoneDaten { Name = "Zone Probe", IdNutzungsart = Nichtwohnart(), Bezugsmenge = 20 } }
            };
            ZapfprofilStand basis = ZapfprofilCtrl.Lies(PROJEKT);

            ZapfprofilMesskalibrierungDaten k = ZapfprofilHuelle.MesswertAusReihe(PROJEKT, eingabe, basis, REIHE, 0, CancellationToken.None);
            Assert.True(k.Ok, k.Abbruch);
            Assert.True(k.Hochgerechnet);
            Assert.True(k.Wert > reihe.Menge, "Vierzig Tage ergeben mehr als vierzig Tage Energie.");
            Assert.Contains(k.Hinweise, h => h.Kennung.StartsWith("ZPG_WARN_MESSKALIBRIERUNG_HOCHGERECHNET",
                                                                  StringComparison.Ordinal));

            // Ohne Bezeichnung: benannt abgelehnt, nichts gerechnet.
            ZapfprofilMesskalibrierungDaten ohne = ZapfprofilHuelle.MesswertAusReihe(PROJEKT, eingabe, basis, "", 0, CancellationToken.None);
            Assert.False(ohne.Ok);
            Assert.Equal("ZPG_SATZ_MESSKALIBRIERUNG_OHNE_MESSREIHE", ohne.Kennung);

            // Eine Zone ausserhalb der Liste: benannt abgelehnt.
            ZapfprofilVorschlagDaten vs = ZapfprofilHuelle.Kalibriervorschlag(PROJEKT, eingabe, basis, REIHE, 7);
            Assert.False(vs.Ok);
            Assert.NotEqual("", vs.Abbruch);
        }

        /// <summary>
        /// <b>Ohne gespeichertes Projekt</b> gibt es keine Messreihen und keinen Schreibweg: Der
        /// Stand nennt den Grund, die Gaben führen weder <c>Einspielen</c> noch <c>Loeschen</c>.
        /// </summary>
        [Fact]
        public void Ohne_Projekt_nennt_der_Stand_den_Grund_und_die_Gaben_keinen_Schreibweg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            TwwMessreihenstandDaten stand = ZapfprofilHuelle.Messreihenstand(0);
            Assert.Empty(stand.Reihen);
            Assert.Contains("gespeichertes Projekt", stand.Grund);

            IReadOnlyDictionary<string, object> gaben = ZapfprofilHuelle.MessreihenGaben(0);
            Assert.True(gaben.ContainsKey("Stand"));
            Assert.True(gaben.ContainsKey("Pruefen"));
            Assert.False(gaben.ContainsKey("Einspielen"));
            Assert.False(gaben.ContainsKey("Loeschen"));

            IReadOnlyDictionary<string, object> mit = ZapfprofilHuelle.MessreihenGaben(PROJEKT);
            Assert.True(mit.ContainsKey("Einspielen"));
            Assert.True(mit.ContainsKey("Loeschen"));
        }

        /// <summary>
        /// Die Kennungen der beiden Wahlen des Messdaten-Dialogs gleichen den Zahlen der Ablage
        /// bzw. des Lesers — sonst spielte die Maske eine andere Größe ein, als sie zeigt.
        /// </summary>
        [Fact]
        public void Die_Kennungen_der_Wahlen_gleichen_denen_des_Kerns()
        {
            Assert.Equal((int)ZapfMessgroesse.Energie, TwwMessreihenwahl.GroesseEnergie);
            Assert.Equal((int)ZapfMessgroesse.Volumen, TwwMessreihenwahl.GroesseVolumen);
            Assert.Equal((int)ZapfMessgroesse.Leistung, TwwMessreihenwahl.GroesseLeistung);
            Assert.Equal((int)Messzeitstempel.Ortszeit, TwwMessreihenwahl.ZeitOrtszeit);
            Assert.Equal((int)Messzeitstempel.Normalzeit, TwwMessreihenwahl.ZeitNormalzeit);
            // Die Kennung „aus der Kopfzeile" ist KEIN Wert der Ablage.
            Assert.False(Enum.IsDefined(typeof(ZapfMessgroesse), TwwMessreihenwahl.GroesseAusKopf));

            // Die Optionen des Lesers folgen den Eingaben der Maske.
            Messreihenoptionen o = ZapfprofilHuelle.AlsOptionen(new TwwMessreiheneingabeDaten
            {
                Bezeichnung = " Probe ",
                Quelle = " Zaehler ",
                GroesseId = TwwMessreihenwahl.GroesseVolumen,
                LueckenschwelleAnteil = 0.2,
                ZeitstempelId = TwwMessreihenwahl.ZeitNormalzeit
            });
            Assert.Equal("Probe", o.Bezeichnung);
            Assert.Equal("Zaehler", o.Quelle);
            Assert.Equal(ZapfMessgroesse.Volumen, o.Groesse);
            Assert.Equal(0.2, o.LueckenanteilHoechstens);
            Assert.Equal(Messzeitstempel.Normalzeit, o.Zeitstempel);

            // Leer heisst „aus der Kopfzeile" und „die Vorgabe des Parametersatzes".
            Messreihenoptionen leer = ZapfprofilHuelle.AlsOptionen(new TwwMessreiheneingabeDaten());
            Assert.Null(leer.Groesse);
            Assert.True(leer.LueckenanteilHoechstens > 0.0);
            Assert.Equal(Messzeitstempel.Ortszeit, leer.Zeitstempel);
        }

        // =================================================================================
        // Kleinkram
        // =================================================================================

        /// <summary>Die Id einer NICHTWOHN-Nutzungsart des Testkatalogs (Kalenderart nicht Wohnen).</summary>
        private static int Nichtwohnart()
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
                new DbParam("@b", "Testnutzung B (fiktiv)"), new DbParam("@k", "TEST-1")), CultureInfo.InvariantCulture);

        /// <summary>Eine erfundene Stundenreihe über <paramref name="tage"/> Tage mit dem Muster.</summary>
        private static Messreihe Erfunden(int tage)
        {
            var werte = new double[tage * Zapfkalender.STUNDEN_TAG];
            for (int d = 0; d < tage; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    werte[d * Zapfkalender.STUNDEN_TAG + h] = TAGESMUSTER[h];
            return new Messreihe(REIHE, ZapfMessgroesse.Energie, 60, new DateTime(2025, 1, 1), werte,
                                 "Probe (erfunden)");
        }

        /// <summary>Dieselbe Reihe als CSV-Datei — der Weg, den die Hülle geht (Datei, Strom, Leser).</summary>
        private static string Csv(int tage)
        {
            var b = new StringBuilder();
            b.Append("Zeitstempel;Wert [kWh]\r\n");
            var t = new DateTime(2025, 1, 1);
            for (int d = 0; d < tage; d++)
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                {
                    b.Append(t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                    b.Append(';');
                    b.Append(TAGESMUSTER[h].ToString(CultureInfo.InvariantCulture));
                    b.Append("\r\n");
                    t = t.AddHours(1);
                }
            return b.ToString();
        }

        private static string Text(string schluessel, CultureInfo kultur)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, kultur);

        private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
            => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(eigeneDatei)!, ".."));
    }
}
