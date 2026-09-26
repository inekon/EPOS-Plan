using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache „erklären lassen“ der Berichtsvorlagen</b> (Etappe BV-E1, Teil B1a): Jede Kennung,
    /// die der Vorlagenprüfer oder der Vorlagen-Controller in eine <see cref="Pruefmeldung"/> schreibt
    /// und die der <see cref="BerichtCtrl"/> einer <see cref="Berichtsmeldung"/> gibt, steht in
    /// <see cref="KiMeldungskennung.Berichtsvorlagen"/> — und damit unter der Wache
    /// <c>KiDialogaufrufTests</c> (Wissensabschnitt, Frage in beiden Sprachen). Umgekehrt steht dort
    /// keine Kennung, die niemand mehr meldet.
    ///
    /// <para><b>Gelesen wird der Quelltext.</b> Eine Meldung entsteht nur an wenigen Formen:
    /// <c>Melde(Befundstufe.…, nameof(R.X), …)</c>, <c>new Pruefmeldung(Befundstufe.…, nameof(R.X), …)</c>
    /// oder über die Variable <c>kennung</c>, deren Werte an <c>Groesse(nameof(R.X), …)</c>, an der
    /// Zuweisung <c>string kennung = …</c> und an <c>Unlesbar(…, nameof(R.X));</c> stehen. Eine neue
    /// Form fällt als „unbekanntes Muster“ auf, statt still übersehen zu werden.</para>
    /// </summary>
    public class BerichtKiKennungenTests
    {
        private static readonly Regex Meldestelle = new Regex(
            @"(?:\bMelde|new Pruefmeldung)\(\s*Befundstufe\.\w+,\s*(?<arg>[^,]+),", RegexOptions.CultureInvariant);

        private static readonly Regex Nameof = new Regex(@"nameof\(R\.(?<k>\w+)\)", RegexOptions.CultureInvariant);

        private static readonly Regex Groesse = new Regex(@"\bGroesse\(\s*nameof\(R\.(?<k>\w+)\)", RegexOptions.CultureInvariant);

        private static readonly Regex Zuweisung = new Regex(@"string kennung = (?<ausdruck>[^;]+);", RegexOptions.CultureInvariant);

        private static readonly Regex Unlesbar = new Regex(@"\bUnlesbar\(stufe, kontext,[\s\S]*?nameof\(R\.(?<k>\w+)\)\);",
                                                           RegexOptions.CultureInvariant);

        private static readonly Regex Laufkennung = new Regex(@"KiMeldungskennung\.(?<k>BV_(?:LAUF|START)_\w+)", RegexOptions.CultureInvariant);

        [Fact]
        public void Jede_Kennung_des_Pruefers_und_des_Vorlagen_Controllers_ist_erklaerbar()
        {
            string pruefer = Quelle("EPOS.Kern/Allgemein/Bericht/Vorlagen/Vorlagenpruefer.cs") +
                             Quelle("EPOS.Kern/Allgemein/Bericht/Vorlagen/VorlagenprueferBloecke.cs") +
                             Quelle("EPOS.Kern/Allgemein/Bericht/Vorlagen/VorlagenprueferTabellen.cs");
            string controller = Quelle("EPOS.Kern/Controller/BerichtsvorlagenCtrl.cs");
            if (pruefer == null || controller == null) return;

            SortedSet<string> ausPruefer = Kennungen(pruefer);
            SortedSet<string> ausController = Kennungen(controller);

            // Plausibel gelesen: der Prüfer hat drei Dutzend Regeln, der Controller drei eigene Kennungen.
            Assert.True(ausPruefer.Count >= 30, "Prüfer: nur " + ausPruefer.Count + " Kennungen gelesen");
            Assert.Contains(nameof(WindowsFormsApplication1.MyResource.Resource.BV_VORLAGEN_IN_WORD), ausController);

            var bekannt = new HashSet<string>(KiMeldungskennung.Berichtsvorlagen, StringComparer.Ordinal);
            List<string> fehlen = ausPruefer.Concat(ausController).Where(k => !bekannt.Contains(k)).Distinct().ToList();
            Assert.True(fehlen.Count == 0, "Nicht erklärbar (KiMeldungskennung.Berichtsvorlagen): " + string.Join(", ", fehlen));

            List<string> verwaist = KiMeldungskennung.Berichtsvorlagen
                .Where(k => k.StartsWith("VF_PRUEF_", StringComparison.Ordinal) || k.StartsWith("BV_VORLAGEN_", StringComparison.Ordinal))
                .Where(k => !ausPruefer.Contains(k) && !ausController.Contains(k)).ToList();
            Assert.True(verwaist.Count == 0, "Gemeldet von niemandem: " + string.Join(", ", verwaist));
        }

        [Fact]
        public void Jede_Kennung_der_Laufmeldung_und_der_Rueckfrage_ist_erklaerbar()
        {
            string ctrl = Quelle("EPOS.Kern/Controller/BerichtCtrl.cs");
            if (ctrl == null) return;

            var benutzt = new SortedSet<string>(Laufkennung.Matches(ctrl).Select(m => m.Groups["k"].Value), StringComparer.Ordinal);
            var bekannt = KiMeldungskennung.Berichtsvorlagen
                .Where(k => k.StartsWith("BV_LAUF_", StringComparison.Ordinal) || k.StartsWith("BV_START_", StringComparison.Ordinal))
                .ToList();

            Assert.Equal(bekannt.OrderBy(k => k, StringComparer.Ordinal), benutzt);
        }

        /// <summary>Das vorbereitete KI-Feld „vorlage“ und seine Wahleinträge aus der Vorlagenliste.</summary>
        [Fact]
        public void Das_KI_Feld_vorlage_waehlt_ueber_die_Kennungen_der_Vorlagenliste()
        {
            using var kultur = new Kulturvorrichtung();
            KiDialogFeld feld = KiDialoge.BerichtVorlagenfeld();
            Assert.Equal("vorlage", feld.Name);
            Assert.Equal("BerichtSeiteKiSicht.Vorlage", feld.Eigenschaftspfad);
            Assert.Equal(KiParameterTyp.Wahl, feld.Typ);
            Assert.Equal("Word-Vorlage", feld.Anzeigename);

            string wurzel = Probevorlagen.TempOrdner("epos-bv-b1a-ki");
            try
            {
                string app = Directory.CreateDirectory(Path.Combine(wurzel, "Vorlagen")).FullName;
                string buero = Directory.CreateDirectory(Path.Combine(wurzel, "Buero")).FullName;
                var einstellungen = new FluechtigeEinstellungen();
                var vorlagen = new BerichtsvorlagenCtrl(new Pfade(wurzel, app), einstellungen, () => null);
                Assert.True(vorlagen.SetzeVorlagenordner(buero).Erfolg);
                File.WriteAllBytes(Path.Combine(buero, "Angebot.docx"), Probevorlagen.AusAbsaetzen("{{projekt.name}}"));

                IReadOnlyList<KiWahleintrag> wahl = KiDialoge.BerichtVorlagenwahl(vorlagen.Liste());
                Assert.Equal(new[] { BerichtsvorlagenCtrl.ID_STANDARD, "eigen:Angebot.docx" }, wahl.Select(w => w.Schluessel));
                Assert.Equal(new[] { "Standard (EPOS-Plan)", "Angebot" }, wahl.Select(w => w.Text));
            }
            finally
            {
                Probevorlagen.Aufraeumen(wurzel);
            }
        }

        private sealed class Pfade : StandardPfade
        {
            private readonly string _dokumente;
            private readonly string _vorlagen;

            public Pfade(string dokumente, string vorlagen)
            {
                _dokumente = dokumente;
                _vorlagen = vorlagen;
            }

            public override string Dokumente { get { return _dokumente; } }

            public override string Berichtsvorlagen { get { return _vorlagen; } }
        }

        /// <summary>Die Kennungen einer Quelldatei; eine Meldestelle unbekannter Form lässt den Test scheitern.</summary>
        private static SortedSet<string> Kennungen(string quelle)
        {
            var gefunden = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Match m in Meldestelle.Matches(quelle))
            {
                string arg = m.Groups["arg"].Value.Trim();
                Match name = Nameof.Match(arg);
                if (name.Success && name.Value == arg) gefunden.Add(name.Groups["k"].Value);
                else Assert.True(arg == "kennung", "Unbekanntes Muster einer Meldestelle: " + m.Value);
            }
            foreach (Match m in Groesse.Matches(quelle)) gefunden.Add(m.Groups["k"].Value);
            foreach (Match m in Zuweisung.Matches(quelle))
                foreach (Match n in Nameof.Matches(m.Groups["ausdruck"].Value)) gefunden.Add(n.Groups["k"].Value);
            foreach (Match m in Unlesbar.Matches(quelle)) gefunden.Add(m.Groups["k"].Value);
            return gefunden;
        }

        private static string Quelle(string relativ)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, relativ.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(pfad), "Quelle fehlt: " + relativ);
            return File.ReadAllText(pfad);
        }
    }
}
