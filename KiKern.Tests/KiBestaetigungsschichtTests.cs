using System;
using System.Globalization;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die Bestaetigungsschicht der Etappe 3 (Fachkonzept 3.5, 4.1, 4.4).
    /// </summary>
    /// <remarks>
    /// Geprueft wird ausschliesslich der KERN: Vorschaupflicht, Riegelgrenzen und die
    /// Freigabe mit ihren vier Ausgaengen. Dass eine Schreibaktion ohne Klick
    /// TATSAECHLICH nichts in die Datenbank schreibt, weist der Aktionsharnisch nach - das
    /// braucht eine Datenbank und gehoert nicht hierher.
    /// </remarks>
    public class KiBestaetigungsschichtTests
    {
        private static readonly CultureInfo De = new CultureInfo("de-DE");

        private static KiAufruf Aufruf(string aktion, string argumente)
        {
            KiPruefErgebnis p = KiPruefung.PruefeJson(Registerabbild.MitHoeherenStufen(), aktion, argumente);
            Assert.True(p.Gueltig, p.FehlerText());
            return p.Aufruf!;
        }

        private static KiAufruf Schreibaufruf()
            => Aufruf("variante_anlegen", "{\"projekt_id\":7,\"bezeichner\":\"Variante B\"}");

        // ================================================== Vorschaupflicht (3.5 Punkt 1)

        [Fact]
        public void SchreibaktionOhneVorschauGibtEsNicht()
        {
            ArgumentException fehler = Assert.Throws<ArgumentException>(() => new KiAktion(
                "probe_schreiben", "Probe.", Schutzstufe.Schreiben, "keiner",
                ausfuehren: _ => KiErgebnis.Ok("")));

            Assert.Contains("Vorschau", fehler.Message);
        }

        [Fact]
        public void RechenaktionOhneVorschauGibtEsNicht()
        {
            Assert.Throws<ArgumentException>(() => new KiAktion(
                "probe_rechnen", "Probe.", Schutzstufe.Rechnen, "keiner",
                ausfuehren: _ => KiErgebnis.Ok("")));
        }

        [Fact]
        public void MitVorschauLaesstSichEineSchreibaktionDeklarieren()
        {
            var a = new KiAktion("probe_schreiben", "Probe.", Schutzstufe.Schreiben, "keiner",
                                 ausfuehren: _ => KiErgebnis.Ok(""),
                                 vorschau: _ => "Ich wuerde etwas anlegen.");

            Assert.NotNull(a.Vorschau);
        }

        [Fact]
        public void LeseaktionBrauchtKeineVorschau()
        {
            var a = new KiAktion("probe_lesen", "Probe.", Schutzstufe.Lesen, "keiner",
                                 ausfuehren: _ => KiErgebnis.Ok(""));

            Assert.Null(a.Vorschau);
        }

        [Fact]
        public void JedeAktionDesRegisterabbildsOberhalbStufeEinsFuehrtEineVorschau()
        {
            foreach (KiAktion a in Registerabbild.MitHoeherenStufen().Alle)
                if (a.Stufe != Schutzstufe.Lesen)
                    Assert.NotNull(a.Vorschau);
        }

        // ================================================== Riegel (4.1)

        [Fact]
        public void OhneBestaetigungBleibtBeiLesen()
        {
            // Die Grenze „was laeuft SOFORT" wird mit Etappe 3 ausdruecklich NICHT
            // angehoben - sonst liefe jede Schreibaktion ohne Rueckfrage durch.
            Assert.Equal(Schutzstufe.Lesen, KiRiegel.OhneBestaetigung);
        }

        [Fact]
        public void HoechsteStufeIstMitEtappeDreiSchreiben()
            => Assert.Equal(Schutzstufe.Schreiben, KiRiegel.HoechsteStufe);

        [Fact]
        public void PruefeStufeLaesstStufeZweiDurch()
            => Assert.Null(KiRiegel.PruefeStufe(Schreibaufruf()));

        [Fact]
        public void PruefeStufeHaeltStufeDreiAn()
        {
            string? grund = KiRiegel.PruefeStufe(Aufruf("simulation_rechnen", "{\"projekt_id\":7}"));

            Assert.NotNull(grund);
            Assert.Contains("simulation_rechnen", grund!);
            Assert.Contains(KiTexte.StufeRechnen, grund!);
        }

        [Fact]
        public void BrauchtBestaetigungHaengtAnDerStufeUndNichtAnEinerNamensliste()
        {
            foreach (KiAktion a in Registerabbild.MitHoeherenStufen().Alle)
                Assert.Equal(a.Stufe != Schutzstufe.Lesen, KiRiegel.BrauchtBestaetigung(a));
        }

        [Fact]
        public void OhneAktionIstNichtsZuBeanstanden()
        {
            Assert.Null(KiRiegel.PruefeStufe((KiAktion?)null));
            Assert.Null(KiRiegel.PruefeStufe((KiAufruf?)null));
            Assert.False(KiRiegel.BrauchtBestaetigung((KiAktion?)null));
        }

        // ================================================== Freigabe (3.5 Punkte 3-5)

        private sealed class Pruefuhr
        {
            internal DateTime Jetzt = new DateTime(2026, 8, 19, 10, 0, 0);
            internal DateTime Lies() => Jetzt;
            internal void Vor(TimeSpan wie) { Jetzt += wie; }
        }

        private static KiFreigabe Freigabe(Pruefuhr uhr, KiAufruf? aufruf = null, long laufmarke = 0)
            => KiFreigabe.Erzeuge(aufruf ?? Schreibaufruf(), "Bestaetigungstext", uhr.Lies,
                                  null, laufmarke);

        [Fact]
        public void DieFristIstEineMinute()
            => Assert.Equal(60, KiFreigabe.VerfallSekunden);

        [Fact]
        public void EineNeueFreigabeIstOffenUndBerechtigtNichts()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            Assert.Equal(KiEntscheidung.Offen, f.Stand);
            Assert.Equal(KiTexte.FreigabeOffen, f.Pruefe(0));
            Assert.False(f.Verbraucht);
        }

        [Fact]
        public void EineErteilteFreigabeBerechtigtGenauEinmal()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            Assert.True(f.Erteilen());
            Assert.Null(f.Pruefe(0));

            Assert.Null(f.Verbrauchen(0));                       // erster Zugriff: gilt
            Assert.Equal(KiTexte.FreigabeVerbraucht, f.Verbrauchen(0));   // zweiter: nicht mehr
            Assert.True(f.Verbraucht);
        }

        [Fact]
        public void EineAbgelehnteFreigabeBerechtigtNicht()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            Assert.True(f.Ablehnen());
            Assert.Equal(KiTexte.FreigabeAbgelehnt, f.Pruefe(0));
            Assert.Equal(KiTexte.FreigabeAbgelehnt, f.Verbrauchen(0));
            Assert.False(f.Verbraucht);
        }

        [Fact]
        public void EineAbgebrocheneFreigabeBerechtigtNicht()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            Assert.True(f.Abbrechen());
            Assert.Equal(KiTexte.FreigabeAbgebrochen, f.Pruefe(0));
        }

        [Fact]
        public void NachEinerMinuteIstDieVorschauVerfallen()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            uhr.Vor(TimeSpan.FromSeconds(KiFreigabe.VerfallSekunden));

            Assert.True(f.IstVerfallen());
            Assert.False(f.Erteilen());
            Assert.Equal(KiTexte.FreigabeVerfallen, f.Verbrauchen(0));
            Assert.Equal(KiEntscheidung.Verfallen, f.Stand);
        }

        [Fact]
        public void EineErteilteFreigabeVerfaelltEbenfalls()
        {
            // Der gefaehrlichste Fall: der Klick kam rechtzeitig, der Lauf kommt zu spaet.
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            Assert.True(f.Erteilen());
            uhr.Vor(TimeSpan.FromSeconds(KiFreigabe.VerfallSekunden + 1));

            Assert.Equal(KiTexte.FreigabeVerfallen, f.Verbrauchen(0));
            Assert.False(f.Verbraucht);
        }

        [Fact]
        public void EineAndereAktionDazwischenEntwertetDieFreigabe()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr, laufmarke: 12);
            f.Erteilen();

            Assert.Null(f.Pruefe(12));
            Assert.Equal(KiTexte.FreigabeUeberholt, f.Pruefe(13));
            Assert.Equal(KiTexte.FreigabeUeberholt, f.Verbrauchen(13));
        }

        [Fact]
        public void EineFreigabeGiltNurFuerIhrenEigenenAufruf()
        {
            var uhr = new Pruefuhr();
            KiAufruf einer = Schreibaufruf();
            KiAufruf anderer = Schreibaufruf();      // gleiche Aktion, anderer Vorgang
            KiFreigabe f = Freigabe(uhr, einer);

            Assert.True(f.GiltFuer(einer));
            Assert.False(f.GiltFuer(anderer));
            Assert.False(f.GiltFuer(null));
        }

        [Fact]
        public void EineEntscheidungLaesstSichNichtUeberschreiben()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            Assert.True(f.Ablehnen());
            Assert.False(f.Erteilen());
            Assert.Equal(KiEntscheidung.Abgelehnt, f.Stand);
        }

        [Fact]
        public void DieRestzeitWirdNieNegativ()
        {
            var uhr = new Pruefuhr();
            KiFreigabe f = Freigabe(uhr);

            uhr.Vor(TimeSpan.FromMinutes(10));

            Assert.Equal(TimeSpan.Zero, f.Restzeit());
        }

        [Fact]
        public void EineFreigabeOhneBestaetigungstextGibtEsNicht()
        {
            Assert.Throws<ArgumentException>(() => KiFreigabe.Erzeuge(Schreibaufruf(), "   "));
            Assert.Throws<ArgumentNullException>(() => KiFreigabe.Erzeuge(null!, "Text"));
        }

        [Fact]
        public void EineFristVonNullGibtEsNicht()
            => Assert.Throws<ArgumentOutOfRangeException>(
                   () => KiFreigabe.Erzeuge(Schreibaufruf(), "Text", null, TimeSpan.Zero));

        // ================================================== Bestaetigungstext (3.5, 4.4)

        [Fact]
        public void DerBestaetigungstextNenntRueckholbarkeitSicherungUndFrist()
        {
            KiAufruf a = Schreibaufruf();
            var bis = new DateTime(2026, 8, 19, 10, 1, 0);

            string text = KiBestaetigung.Erzeuge(a, "Ich wuerde eine Variante anlegen.", De,
                                                 @"C:\DB-Backup\Kenndaten_KI_2026-08-19_100000.accdb", bis);

            Assert.Contains(KiTexte.FeldVorschau, text);
            Assert.Contains("Ich wuerde eine Variante anlegen.", text);
            Assert.Contains(KiTexte.FeldRueckholbar, text);
            Assert.Contains(KiTexte.RueckholbarNein, text);        // variante_anlegen ist nicht umkehrbar
            Assert.Contains(KiTexte.FeldSicherung, text);
            Assert.Contains("Kenndaten_KI_2026-08-19_100000.accdb", text);
            Assert.Contains(KiTexte.FeldGueltigBis, text);
            Assert.Contains("10:01:00", text);
        }

        [Fact]
        public void EineLeseaktionBekommtKeineRueckholbarkeitszeile()
        {
            string text = KiBestaetigung.Erzeuge(Aufruf("projekt_lesen", "{\"projekt_id\":7}"), null, De);

            Assert.DoesNotContain(KiTexte.FeldRueckholbar, text);
        }

        [Fact]
        public void UmkehrbarkeitStehtInDerDeklarationUndNichtImText()
        {
            var umkehrbar = new KiAktion("probe_um", "Probe.", Schutzstufe.Schreiben, "keiner",
                                         ausfuehren: _ => KiErgebnis.Ok(""),
                                         vorschau: _ => "Vorschau.",
                                         umkehrbar: true);
            var nicht = new KiAktion("probe_nicht", "Probe.", Schutzstufe.Schreiben, "keiner",
                                     ausfuehren: _ => KiErgebnis.Ok(""),
                                     vorschau: _ => "Vorschau.");

            Assert.True(umkehrbar.Umkehrbar);
            Assert.False(nicht.Umkehrbar);

            KiPruefErgebnis pu = KiPruefung.Pruefe(umkehrbar, Beispielregister.Werte());
            KiPruefErgebnis pn = KiPruefung.Pruefe(nicht, Beispielregister.Werte());

            Assert.Contains(KiTexte.RueckholbarJa, KiBestaetigung.Erzeuge(pu.Aufruf!, null, De));
            Assert.Contains(KiTexte.RueckholbarNein, KiBestaetigung.Erzeuge(pn.Aufruf!, null, De));
        }
    }
}
