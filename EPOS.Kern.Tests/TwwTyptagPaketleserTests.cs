using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Größenschutz des Typtag-Paketlesers</b> (<see cref="TwwTyptagCtrl.PaketLesen"/>,
    /// Umsetzungskonzept Zapfprofilgenerator N19; Muster
    /// <c>TwwNutzungsartCtrl.PaketLesen</c> und <c>TwwKatalogimportTests</c>): Eintragszahl,
    /// entpackte Gesamtgröße und Größe je Datei gelten für Archiv, Ordner und Einzeldatei; ein
    /// Eintragsname, der aus dem Archiv herauszeigt, fällt benannt, ein Unterordner nicht.
    ///
    /// <para><b>Ohne Datenbank.</b> Der Leser liest nur Dateien; die Form prüft erst das
    /// Einspielen. Die Pakete sind erfunden (<see cref="Typtagpaketbauer"/>) und entstehen im
    /// Temp-Ordner — im Repositorium entsteht keine Datei.</para>
    /// </summary>
    public sealed class TwwTyptagPaketleserTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static string NeuerOrdner()
            => Path.Combine(Path.GetTempPath(), "epos-typtagschutz-" + Guid.NewGuid().ToString("N"));

        /// <summary>Schreibt das erfundene Paket als Einzeldateien in einen neuen Ordner.</summary>
        private static string Paketordner()
        {
            string ordner = NeuerOrdner();
            Directory.CreateDirectory(ordner);
            foreach (TwwPaketdatei d in Typtagpaketbauer.Erfunden().Dateien())
                File.WriteAllText(Path.Combine(ordner, d.Name), d.Inhalt, new UTF8Encoding(false));
            return ordner;
        }

        private static void Fort(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { }
        }

        // =================================================================================
        //  Der gerade Weg
        // =================================================================================

        /// <summary>
        /// Mit den Vorgaben liest der Leser das ganze Paket — aus dem Archiv, aus dem Ordner und
        /// über EINE Datei des Ordners. Ohne diesen Fall wäre jede Ablehnung unten wertlos.
        /// </summary>
        [Fact]
        public void Mit_den_Vorgaben_liest_der_Leser_das_ganze_Paket()
        {
            string ordner = Paketordner();
            try
            {
                int dateien = Typtagpaketbauer.Erfunden().Dateien().Count;

                IReadOnlyList<TwwPaketdatei> ausOrdner = TwwTyptagCtrl.PaketLesen(ordner, out ZapfSatz fo);
                Assert.Null(fo);
                Assert.Equal(dateien, ausOrdner.Count);

                IReadOnlyList<TwwPaketdatei> ausDatei = TwwTyptagCtrl.PaketLesen(
                    Path.Combine(ordner, Normformvektorleser.DATEI_TYPTAGE), out ZapfSatz fd);
                Assert.Null(fd);
                Assert.Equal(dateien, ausDatei.Count);

                string zip = Typtagpaketbauer.Erfunden().AlsZip(ordner);
                IReadOnlyList<TwwPaketdatei> ausZip = TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz fz);
                Assert.Null(fz);
                Assert.Equal(dateien, ausZip.Count);
            }
            finally { Fort(ordner); }
        }

        // =================================================================================
        //  Die drei Grenzen im Archiv
        // =================================================================================

        /// <summary>
        /// <b>Der Größenschutz des ZIP-Wegs</b> (N19): Eintragszahl und entpackte Gesamtgröße stehen
        /// im Zentralverzeichnis und werden geprüft, bevor ein Byte entpackt wird; eine zu große
        /// Einzeldatei fällt ebenso. Ein Eintragsname, der aus dem Archiv herauszeigt (<c>..</c>),
        /// wird benannt abgelehnt — ein Unterordner dagegen nicht (das prüft der Nachbartest).
        /// Jedes Mal eine leere Liste, kein Teilpaket.
        /// </summary>
        [Fact]
        public void Der_Groessenschutz_lehnt_ein_zu_grosses_Paket_und_einen_Pfad_nach_oben_ab()
        {
            string ordner = NeuerOrdner();
            Directory.CreateDirectory(ordner);
            string zip = Path.Combine(ordner, "schutz.zip");
            try
            {
                // (1) Zu viele Eintraege — der Inhalt spielt keine Rolle, gelesen wird nichts.
                using (ZipArchive a = ZipFile.Open(zip, ZipArchiveMode.Create))
                    for (int i = 0; i <= TwwTyptagCtrl.HOECHSTENS_EINTRAEGE; i++)
                    {
                        ZipArchiveEntry e = a.CreateEntry("datei" + i.ToString(CultureInfo.InvariantCulture) + ".csv");
                        using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
                        w.Write("ID\n");
                    }
                IReadOnlyList<TwwPaketdatei> zuViele = TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz f1);
                Assert.Empty(zuViele);
                Assert.Equal("TYPTAGIMPORT_ZU_GROSS", f1.Kennung);
                Assert.Equal(TwwTyptagCtrl.HOECHSTENS_EINTRAEGE + 1, f1.Werte[0]);
                Assert.Equal(TwwTyptagCtrl.HOECHSTENS_EINTRAEGE, f1.Werte[1]);
                // Der Satz nennt auch die gemessene und die erlaubte Gesamtgroesse: 201 Eintraege
                // mit je drei Byte bleiben weit unter der Grenze — abgelehnt ist die Eintragszahl.
                Assert.Equal(3L * (TwwTyptagCtrl.HOECHSTENS_EINTRAEGE + 1), f1.Werte[2]);
                Assert.Equal(TwwTyptagCtrl.HOECHSTENS_BYTE_ENTPACKT, f1.Werte[3]);
                File.Delete(zip);

                // (2) Ein Eintragsname, der aus dem Archiv herauszeigt.
                using (ZipArchive a = ZipFile.Open(zip, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry e = a.CreateEntry("../" + Normformvektorleser.DATEI_TYPTAGE);
                    using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
                    w.Write("code;jahreszeit;tagart;bewoelkung\n");
                }
                IReadOnlyList<TwwPaketdatei> nachOben = TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz f2);
                Assert.Empty(nachOben);
                Assert.Equal("TYPTAGIMPORT_PFAD_UNZULAESSIG", f2.Kennung);
                Assert.Equal("../" + Normformvektorleser.DATEI_TYPTAGE, f2.Werte[0]);
                File.Delete(zip);

                // (3) Eine zu grosse Einzeldatei. Gemessen an einer kleinen Grenze statt an 16 MB —
                // dafuer sind die Grenzen Parameter mit den Konstanten als Vorgabe.
                string gross = new string('x', 4000);
                using (ZipArchive a = ZipFile.Open(zip, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry e = a.CreateEntry(Normformvektorleser.DATEI_TYPTAGE);
                    using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
                    w.Write(gross);
                }
                IReadOnlyList<TwwPaketdatei> zuGross = TwwTyptagCtrl.PaketLesen(
                    zip, out ZapfSatz f3, grenzeJeDatei: 1000);
                Assert.Empty(zuGross);
                Assert.Equal("TYPTAGIMPORT_DATEI_ZU_GROSS", f3.Kennung);
                Assert.Equal(Normformvektorleser.DATEI_TYPTAGE, f3.Werte[0]);
                Assert.Equal(4000L, f3.Werte[1]);
                Assert.Equal(1000L, f3.Werte[2]);

                // Dieselbe Datei mit den Vorgaben: gelesen, nicht abgelehnt.
                Assert.Single(TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz ohne));
                Assert.Null(ohne);
            }
            finally { Fort(ordner); }
        }

        /// <summary>
        /// Ein Unterordner im Archiv bleibt erlaubt — ein ZIP, das aus einem Ordner entstanden ist,
        /// trägt dessen Namen, und der Leser nimmt ohnehin allein den Dateinamen. Ein
        /// Verzeichniseintrag fällt still, er trägt keinen Inhalt.
        /// </summary>
        [Fact]
        public void Ein_Unterordner_bleibt_erlaubt_und_ein_Verzeichniseintrag_faellt_still()
        {
            string ordner = NeuerOrdner();
            Directory.CreateDirectory(ordner);
            string zip = Path.Combine(ordner, "unterordner.zip");
            try
            {
                List<TwwPaketdatei> paket = Typtagpaketbauer.Erfunden().Dateien();
                using (ZipArchive a = ZipFile.Open(zip, ZipArchiveMode.Create))
                {
                    a.CreateEntry("paket/");                       // Verzeichniseintrag, Name leer
                    foreach (TwwPaketdatei d in paket)
                    {
                        ZipArchiveEntry e = a.CreateEntry("paket/" + d.Name);
                        using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
                        w.Write(d.Inhalt);
                    }
                }
                IReadOnlyList<TwwPaketdatei> gelesen = TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz fehler);
                Assert.Null(fehler);
                Assert.Equal(paket.Count, gelesen.Count);
                Assert.All(gelesen, d => Assert.DoesNotContain("/", d.Name));

                // Und der Leser versteht das Paket unverändert.
                Normformvektorsatz satz = Normformvektorleser.AusDateien(gelesen, out ZapfSatz f2);
                Assert.Null(f2);
                Assert.NotNull(satz);
            }
            finally { Fort(ordner); }
        }

        // =================================================================================
        //  Ordner und Einzeldatei
        // =================================================================================

        /// <summary>
        /// <b>Eintragszahl und Gesamtgröße gelten auch für Ordner und Einzeldatei</b> (N19): Beide
        /// Wege lesen dieselben Dateien wie das Archiv, also gilt dieselbe Grenze — aus dem
        /// Dateisystem statt aus dem Zentralverzeichnis. Gemessen mit einer kleinen Grenze.
        /// </summary>
        [Fact]
        public void Der_Groessenschutz_gilt_auch_fuer_Ordner_und_Einzeldatei()
        {
            string ordner = Paketordner();
            try
            {
                IReadOnlyList<TwwPaketdatei> ausOrdner = TwwTyptagCtrl.PaketLesen(
                    ordner, out ZapfSatz fo, grenzeGesamt: 100);
                Assert.Empty(ausOrdner);
                Assert.Equal("TYPTAGIMPORT_ZU_GROSS", fo.Kennung);
                Assert.Equal(100L, fo.Werte[3]);

                IReadOnlyList<TwwPaketdatei> ausDatei = TwwTyptagCtrl.PaketLesen(
                    Path.Combine(ordner, Normformvektorleser.DATEI_TYPTAGE), out ZapfSatz fd,
                    grenzeGesamt: 100);
                Assert.Empty(ausDatei);
                Assert.Equal("TYPTAGIMPORT_ZU_GROSS", fd.Kennung);
                Assert.Equal(100L, fd.Werte[3]);

                // Die Grenze je Datei greift auf beiden Wegen ebenso, mit dem Dateinamen im Satz.
                IReadOnlyList<TwwPaketdatei> jeDatei = TwwTyptagCtrl.PaketLesen(
                    ordner, out ZapfSatz fj, grenzeJeDatei: 10);
                Assert.Empty(jeDatei);
                Assert.Equal("TYPTAGIMPORT_DATEI_ZU_GROSS", fj.Kennung);
                Assert.Equal(10L, fj.Werte[2]);
            }
            finally { Fort(ordner); }
        }

        /// <summary>
        /// Die Summe der ausgewiesenen Größen bricht <b>an</b> der Grenze ab, statt an erfundenen
        /// Längen überzulaufen (200 Einträge mit je 2^62 Byte wären sonst eine kleine Zahl) — im
        /// Archiv wie im Dateisystem. Gemessen am Archiv über die gemeldete Gesamtgröße.
        /// </summary>
        [Fact]
        public void Die_Summe_der_Groessen_laeuft_nicht_ueber()
        {
            string ordner = NeuerOrdner();
            Directory.CreateDirectory(ordner);
            string zip = Path.Combine(ordner, "summe.zip");
            try
            {
                using (ZipArchive a = ZipFile.Open(zip, ZipArchiveMode.Create))
                    for (int i = 0; i < 5; i++)
                    {
                        ZipArchiveEntry e = a.CreateEntry("datei" + i.ToString(CultureInfo.InvariantCulture) + ".csv");
                        using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
                        w.Write(new string('x', 1000));
                    }
                IReadOnlyList<TwwPaketdatei> d = TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz fehler, grenzeGesamt: 2000);
                Assert.Empty(d);
                Assert.Equal("TYPTAGIMPORT_ZU_GROSS", fehler.Kennung);
                // Die Summe ist gesaettigt: Grenze + 1, nie eine ueberlaufene Zahl.
                Assert.Equal(2001L, fehler.Werte[2]);
                Assert.Equal(2000L, fehler.Werte[3]);
            }
            finally { Fort(ordner); }
        }

        // =================================================================================
        //  Das Zentralverzeichnis ist eine Behauptung
        // =================================================================================

        /// <summary>
        /// <b>Ein lügendes Zentralverzeichnis bläht das Paket nicht auf</b> (N19, Muster des
        /// Katalogimports): Die entpackte Größe eines Eintrags ist eine Behauptung der Datei
        /// (<see cref="Archivluege"/>). <see cref="ZipArchiveEntry.Open"/> begrenzt den Entpackstrom
        /// selbst auf die ausgewiesene Größe, ein zu klein ausgewiesener Eintrag kommt also
        /// <b>gekürzt</b> herein und nicht zu groß. Der Größenschutz des Lesers ist damit die zweite
        /// Wand und feuert hier nicht; das Paket bleibt trotzdem draußen, denn die Formprüfung des
        /// Einspielens nimmt keine gekürzte Datei. Genau das hält dieser Fall fest: keine unbegrenzte
        /// Leselast — und kein stiller Import halber Dateien.
        /// </summary>
        [Fact]
        public void Ein_luegendes_Zentralverzeichnis_blaeht_das_Paket_nicht_auf()
        {
            string ordner = NeuerOrdner();
            try
            {
                string zip = Typtagpaketbauer.Erfunden().AlsZip(ordner);
                int dateien = Typtagpaketbauer.Erfunden().Dateien().Count;

                // Ohne Luege: dasselbe Paket, vollstaendig gelesen und verstanden.
                IReadOnlyList<TwwPaketdatei> ganz = TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz ohne);
                Assert.Null(ohne);
                Assert.Equal(dateien, ganz.Count);
                Assert.NotNull(Normformvektorleser.AusDateien(ganz, out ZapfSatz fg));
                Assert.Null(fg);

                // Mit Luege: Das Verzeichnis weist ein Byte aus — so viel kommt herein, nicht mehr.
                Archivluege.EntpackteGroesseFaelschen(zip, 1);
                IReadOnlyList<TwwPaketdatei> gekuerzt = TwwTyptagCtrl.PaketLesen(zip, out ZapfSatz fehler);
                Assert.Null(fehler);
                Assert.Equal(dateien, gekuerzt.Count);
                Assert.All(gekuerzt, d => Assert.Equal(1, d.Inhalt.Length));

                // Und die halbe Datei kommt nicht durch: Die Formpruefung lehnt sie benannt ab.
                Assert.Null(Normformvektorleser.AusDateien(gekuerzt, out ZapfSatz f2));
                Assert.NotNull(f2);
            }
            finally { Fort(ordner); }
        }

        // =================================================================================
        //  Die benannte Ablehnung bleibt benannt
        // =================================================================================

        /// <summary>
        /// Jede Ablehnung des Lesers trägt ihr Muster in beiden Sprachen (Wache
        /// <c>ZapfSaetzeWacheTests</c>) — hier gemessen, dass der Satz sich bauen lässt und die
        /// Kennung nicht roh dasteht.
        /// </summary>
        [Fact]
        public void Jede_Ablehnung_des_Lesers_hat_einen_Satz()
        {
            foreach (string kennung in new[] { "TYPTAGIMPORT_ZU_GROSS", "TYPTAGIMPORT_DATEI_ZU_GROSS",
                                               "TYPTAGIMPORT_PFAD_UNZULAESSIG" })
            {
                string muster = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                    ZapfSatz.PRAEFIX + kennung, CultureInfo.GetCultureInfo("de-DE"));
                string englisch = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                    ZapfSatz.PRAEFIX + kennung, CultureInfo.GetCultureInfo("en-US"));
                Assert.False(string.IsNullOrWhiteSpace(muster), kennung + " fehlt auf Deutsch.");
                Assert.False(string.IsNullOrWhiteSpace(englisch), kennung + " fehlt auf Englisch.");
                Assert.NotEqual(muster, englisch);
            }
        }
    }
}
