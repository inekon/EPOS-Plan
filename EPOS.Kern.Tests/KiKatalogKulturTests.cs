using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Dialogkatalog ist in jeder Anzeigesprache DERSELBE — nur seine Texte sind
    /// übersetzt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum es diesen Wächter gibt.</b> <see cref="KiDialoge.Katalog"/> wird je
    /// Kultur gebaut, weil seine Anzeigenamen in <c>MyResource.Resource</c> stehen. Auf
    /// dem Windows-Läufer (<c>en-US</c>) nannte die Absage für <c>bereitschaftsverlust</c>
    /// den Katalogeditor statt der Heizkesselverwaltung, auf Linux (invariante Kultur,
    /// also die neutralen deutschen Texte) und lokal (<c>de-DE</c>) die Verwaltung: Ob
    /// die Verwaltung das Feld führt, entschied ein toleranter Vergleich mit ihrer
    /// BESCHRIFTUNG („Betriebsbereitschaftsverluste" trifft den Namen, „Operational
    /// readiness losses" nicht). Das entscheiden jetzt Schlüssel
    /// (<see cref="KiDialoge.Zielfeldname"/>).
    /// </para>
    /// <para>
    /// <b>Was er hält.</b> Dieselben Masken, Felder und Knöpfe in derselben Reihenfolge
    /// mit denselben Schlüsseln, Pfaden und Schaltern in <c>de-DE</c>, <c>en-US</c> und
    /// der invarianten Kultur; für JEDEN Feldschlüssel des Katalogs dieselbe gemeinte
    /// Maske (<c>KiAktionenDialog.GemeinteMaske</c>) in allen drei Kulturen; und eine
    /// vollständige Zuordnung der vier Katalogeditoren zu ihren Verwaltungen.
    /// </para>
    /// <para>
    /// Die Kultur wird je Durchgang über die <see cref="Kulturvorrichtung"/> gesetzt und
    /// danach zurückgestellt; die Klasse hält keine deutschen Texte.
    /// </para>
    /// </remarks>
    public class KiKatalogKulturTests
    {
        /// <summary>Deutsch (lokal), Englisch (Windows-Läufer), invariant (Linux-Läufer).</summary>
        private static readonly string[] Kulturen = { "de-DE", "en-US", "" };

        /// <summary>Führt <paramref name="lauf"/> unter <paramref name="kultur"/> aus.</summary>
        private static T Unter<T>(string kultur, Func<T> lauf)
        {
            using var k = new Kulturvorrichtung(kultur);
            return lauf();
        }

        private static string Anzeige(string kultur) => kultur.Length == 0 ? "invariant" : kultur;

        // ===================================================== Die Struktur

        /// <summary>
        /// Der sprachneutrale Abdruck des Katalogs: je Maske ihr Schlüssel, ihre Felder
        /// (Schlüssel, Typ, Pfad, Schalter, Grenzen, Reihenlänge) und ihre Knöpfe — alles
        /// außer Anzeigename, Erläuterung und Einheit.
        /// </summary>
        private static List<string> Abdruck()
        {
            var zeilen = new List<string>();

            foreach (KiDialog d in KiDialoge.Katalog.Alle)
            {
                zeilen.Add("Maske " + d.Maskenname + " (" + d.Felder.Count + " Felder, " +
                           d.Knoepfe.Count + " Knöpfe)");

                foreach (KiDialogFeld f in d.Felder)
                    zeilen.Add(d.Maskenname + " | Feld " + f.Name + " | " + f.Typ + " | " +
                               f.Eigenschaftspfad + " | leer=" + f.LeerErlaubt +
                               " | nurLesen=" + f.NurLesen + " | satzwahl=" + f.Satzwahl +
                               " | zeile=" + f.Zeilenkennzeichen + " | hilfe=" + f.HilfeSlug +
                               " | min=" + Zahl(f.Min) + " | max=" + Zahl(f.Max) +
                               " | reihe=" + (f.Reihe?.Laenge.ToString(CultureInfo.InvariantCulture) ?? "-"));

                foreach (KiDialogKnopf b in d.Knoepfe)
                    zeilen.Add(d.Maskenname + " | Knopf " + b.Name + " | " + b.Controlpfad);
            }

            return zeilen;
        }

        private static string Zahl(double? wert)
            => wert.HasValue ? wert.Value.ToString("R", CultureInfo.InvariantCulture) : "-";

        /// <summary>
        /// Gleiche Maskenschlüssel, gleiche Feldschlüssel je Maske, gleiche Feld- und
        /// Knopfzahlen — in allen drei Kulturen.
        /// </summary>
        [Fact]
        public void Der_Katalog_ist_in_allen_Kulturen_strukturgleich()
        {
            List<string> deutsch = Unter(Kulturen[0], Abdruck);
            Assert.NotEmpty(deutsch);

            foreach (string kultur in Kulturen.Skip(1))
            {
                List<string> andere = Unter(kultur, Abdruck);

                var abweichungen = new List<string>();
                abweichungen.AddRange(deutsch.Except(andere).Select(z => "nur de-DE:  " + z));
                abweichungen.AddRange(andere.Except(deutsch).Select(z => "nur " + Anzeige(kultur) + ": " + z));
                if (abweichungen.Count == 0 && !deutsch.SequenceEqual(andere))
                    abweichungen.Add("Gleiche Einträge, andere Reihenfolge.");

                Assert.True(abweichungen.Count == 0,
                    "Der Katalog unter " + Anzeige(kultur) + " weicht von de-DE ab:\n" +
                    string.Join("\n", abweichungen.Take(40)));
            }
        }

        // ===================================================== Der Weg zur Maske

        /// <summary>Für jeden Feldschlüssel des Katalogs die gemeinte Maske — oder „(keine)".</summary>
        private static SortedDictionary<string, string> Wege()
        {
            var wege = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (KiDialog d in KiDialoge.Katalog.Alle)
                foreach (KiDialogFeld f in d.Felder)
                    if (!wege.ContainsKey(f.Name))
                        wege[f.Name] = KiAktionenDialog.GemeinteMaske("", new[] { f.Name })?.Maskenname
                                       ?? "(keine)";

            return wege;
        }

        /// <summary>
        /// <b>Die gemeinte Maske eines Feldschlüssels hängt nicht an der Sprache.</b>
        /// Ein Schlüssel ist sprachneutral; welche Maske die Absage „keine Maske offen"
        /// für ihn nennt, darf es deshalb auch nicht sein.
        /// </summary>
        /// <remarks>
        /// Solange die Zielmaske über ihre Beschriftungen gefunden wurde, war dieser Fall
        /// zweimal rot: <c>bereitschaftsverlust</c> (de-DE: Heizkesselverwaltung, en-US:
        /// Katalogeditor) und <c>entladeleistung</c> (de-DE: Ansicht „Simulation" über die
        /// Ladeleistung des BATTERIEspeichers, en-US: die Pufferspeichermaske des Projekts).
        /// </remarks>
        [Fact]
        public void Die_gemeinte_Maske_eines_Feldschluessels_ist_in_allen_Kulturen_dieselbe()
        {
            SortedDictionary<string, string> deutsch = Unter(Kulturen[0], Wege);
            Assert.NotEmpty(deutsch);

            foreach (string kultur in Kulturen.Skip(1))
            {
                SortedDictionary<string, string> andere = Unter(kultur, Wege);

                var abweichungen = new List<string>();
                foreach (KeyValuePair<string, string> w in deutsch)
                {
                    andere.TryGetValue(w.Key, out string dort);
                    if (!string.Equals(w.Value, dort, StringComparison.Ordinal))
                        abweichungen.Add(w.Key + ": de-DE → " + w.Value + ", " + Anzeige(kultur) + " → " + dort);
                }

                Assert.True(abweichungen.Count == 0,
                    "Die gemeinte Maske hängt an der Kultur (" + Anzeige(kultur) + "):\n" +
                    string.Join("\n", abweichungen));
            }
        }

        /// <summary>
        /// Der Befund selbst, als Einzelfall: <c>bereitschaftsverlust</c> meint in jeder
        /// Kultur die Heizkesselverwaltung.
        /// </summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        [InlineData("")]
        public void Bereitschaftsverlust_meint_in_jeder_Kultur_die_Heizkesselverwaltung(string kultur)
        {
            string gemeint = Unter(kultur,
                () => KiAktionenDialog.GemeinteMaske("", new[] { "bereitschaftsverlust" })?.Maskenname);

            Assert.Equal(KiMaskennamen.HEIZKESSEL_ADMIN, gemeint);
        }

        // ===================================================== Editor und Verwaltung

        /// <summary>
        /// <b>Jedes Feld der vier Katalogeditoren steht unter einem Schlüssel in seiner
        /// Verwaltung</b> — unter demselben oder unter dem erklärten
        /// (<see cref="KiDialoge.Zielfeldname"/>); und keine Erklärung zeigt ins Leere.
        /// </summary>
        /// <remarks>
        /// Die Verwaltung führt den VOLLEN Feldbestand des Katalogsatzes
        /// (<c>KatalogBrowserProfil</c>). Ein Editorfeld ohne Gegenstück hieße: Die Absage
        /// nennt für dieses Feld den Editor statt der Verwaltung — genau der Unterschied,
        /// der sonst nur unter einer anderen Sprache auffiele.
        /// </remarks>
        [Fact]
        public void Jedes_Feld_eines_Katalogeditors_steht_unter_einem_Schluessel_in_seiner_Verwaltung()
        {
            using var k = new Kulturvorrichtung("en-US");

            Assert.Equal(4, KiDialoge.Katalogeditoren.Count);

            var fehlend = new List<string>();
            foreach (string maske in KiDialoge.Katalogeditoren)
            {
                KiDialog editor = KiDialoge.Katalog.Finde(maske);
                Assert.True(editor != null, "Der Katalogeditor " + maske + " fehlt im Katalog.");

                KiDialog verwaltung = KiDialoge.Katalog.Finde(KiMaskenziele.Ziel(maske));
                Assert.True(verwaltung != null, "Das Ziel des Katalogeditors " + maske + " ist keine Katalogmaske.");

                foreach (KiDialogFeld f in editor.Felder)
                {
                    string schluessel = KiDialoge.Zielfeldname(maske, f.Name);
                    if (!verwaltung.KenntFeld(schluessel))
                        fehlend.Add(maske + "." + f.Name + " → " + verwaltung.Maskenname + "." + schluessel);
                }
            }

            Assert.True(fehlend.Count == 0,
                "Diese Felder der Katalogeditoren stehen nicht in ihrer Verwaltung:\n" +
                string.Join("\n", fehlend));
        }

        /// <summary>
        /// Die Gegenprobe: Außerhalb der Erklärung — ein Feld unter demselben Schlüssel,
        /// eine andere Maske, ein unbekanntes Feld — bleibt der Schlüssel, wie er ist.
        /// </summary>
        [Fact]
        public void Ohne_Erklaerung_bleibt_der_Schluessel()
        {
            Assert.Equal("bbverlust", KiDialoge.Zielfeldname(KiMaskennamen.HEIZKESSEL, "bereitschaftsverlust"));
            Assert.Equal("vorlauf", KiDialoge.Zielfeldname(KiMaskennamen.HEIZKESSEL, "vorlauf"));
            Assert.Equal("bereitschaftsverlust",
                         KiDialoge.Zielfeldname(KiMaskennamen.HEIZKESSEL_ADMIN, "bereitschaftsverlust"));
            Assert.Equal("gibtesnicht", KiDialoge.Zielfeldname(KiMaskennamen.PUFFERSPEICHER, "gibtesnicht"));
        }
    }
}
