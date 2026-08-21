using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Dialogdeklaration und Dialogkatalog (Fachkonzept 11.3): Gesteuert wird nur, was
    /// deklariert ist - und ein Loeschknopf laesst sich gar nicht erst deklarieren.
    /// </summary>
    public class KiDialogTests
    {
        // ------------------------------------------------------------------ Bausteine

        private static KiDialogFeld Feld(string name = "wartungskosten",
                                         string pfad = "gb_Kosten.tb_Wartung")
            => new KiDialogFeld(name, pfad, "Wartungskosten", KiParameterTyp.Zahl,
                                "Jährliche Wartungskosten des Kessels.", einheit: "€/a");

        private static KiDialogKnopf Knopf(string name = "speichern", string pfad = "btn_Speichern")
            => new KiDialogKnopf(name, pfad, "Speichern");

        private static KiDialog Maske(IReadOnlyList<KiDialogFeld>? felder = null,
                                      IReadOnlyList<KiDialogKnopf>? knoepfe = null)
            => new KiDialog("Form_Heizkessel_Bearbeiten", "Heizkessel bearbeiten",
                            felder ?? new[] { Feld() }, knoepfe ?? new[] { Knopf() });

        // ============================================================ Felddeklaration

        [Theory]
        [InlineData("wartungskosten")]
        [InlineData("feld_2")]
        [InlineData("a")]
        public void GueltigeFeldnamen_LassenSichDeklarieren(string name)
        {
            Assert.Equal(name, Feld(name).Name);
        }

        [Theory]
        [InlineData("Wartungskosten")]   // Grossbuchstabe
        [InlineData("wartungs-kosten")]  // Bindestrich
        [InlineData("2_felder")]         // beginnt mit Ziffer
        [InlineData("prüfen")]           // kein ASCII
        [InlineData("")]
        [InlineData(null)]
        public void FeldMitUnzulaessigemNamen_LaesstSichNichtDeklarieren(string? name)
        {
            Assert.Throws<ArgumentException>(() => Feld(name!));
        }

        [Fact]
        public void FeldOhneAnzeigenamen_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "wartungskosten", "tb_Wartung", "  ", KiParameterTyp.Zahl, "Erläuterung."));
        }

        [Fact]
        public void FeldOhneErlaeuterung_LaesstSichNichtDeklarieren()
        {
            // Ohne Erlaeuterung koennte dialog_parameter_erklaeren nur den Anzeigenamen
            // wiederholen - genau die Antwort, die der Anwender schon vor sich sieht.
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "wartungskosten", "tb_Wartung", "Wartungskosten", KiParameterTyp.Zahl, "  "));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("gb_Kosten..tb_Wartung")]   // leere Stufe
        [InlineData(".tb_Wartung")]
        [InlineData("gb_Kosten.")]
        [InlineData("tb Wartung")]              // Leerzeichen
        [InlineData(null)]
        public void FeldOhneBrauchbarenControlpfad_LaesstSichNichtDeklarieren(string? pfad)
        {
            Assert.Throws<ArgumentException>(() => Feld(pfad: pfad!));
            Assert.False(KiControlpfad.IstGueltig(pfad));
        }

        [Theory]
        [InlineData("tb_Wartung")]
        [InlineData("gb_Kessel.tb_Wirkungsgrad_Öl")]   // Bestand: nicht-ASCII-Controlnamen
        [InlineData("tabControl1.tabPage2.gb_Kosten.tb_Wartung")]
        public void ControlpfadeDesBestands_WerdenAngenommen(string pfad)
        {
            // Die Startmasken fuehren nicht-ASCII-Controlnamen (Bestandsanker B9). Wuerde
            // der Kern die ASCII-Regel der Aktionsnamen auch hier anlegen, waere genau das
            // Feld nicht deklarierbar, um das es haeufig geht.
            Assert.True(KiControlpfad.IstGueltig(pfad));
            Assert.Equal(pfad, Feld(pfad: pfad).Controlpfad);
        }

        [Fact]
        public void ZahlenlisteAlsFeld_LaesstSichNichtDeklarieren()
        {
            // Ein Maskenfeld traegt genau einen Wert; fuer eine Liste gibt es kein Control.
            Assert.Throws<ArgumentException>(() => new KiDialogFeld(
                "projekte", "tb_Projekte", "Projekte", KiParameterTyp.GanzzahlListe, "Liste."));
        }

        [Fact]
        public void FeldEigenschaften_StehenSoDaWieDeklariert()
        {
            var f = new KiDialogFeld("nutzungsdauer", "gb_Kosten.tb_Dauer", "Nutzungsdauer",
                                     KiParameterTyp.Ganzzahl, "Erwartete Nutzungsdauer.",
                                     einheit: "a", leerErlaubt: true, hilfeSlug: "nutzungsdauer");

            Assert.Equal("nutzungsdauer", f.Name);
            Assert.Equal("gb_Kosten.tb_Dauer", f.Controlpfad);
            Assert.Equal("Nutzungsdauer", f.Anzeigename);
            Assert.Equal(KiParameterTyp.Ganzzahl, f.Typ);
            Assert.Equal("a", f.Einheit);
            Assert.True(f.LeerErlaubt);
            Assert.True(f.HatHilfe);
            Assert.Equal("nutzungsdauer", f.HilfeSlug);
        }

        [Fact]
        public void OhneHilfeSlug_IstDasFeldOhneHilfe()
        {
            KiDialogFeld f = Feld();

            Assert.False(f.HatHilfe);
            Assert.Equal("", f.HilfeSlug);
            Assert.False(f.LeerErlaubt);
        }

        // ============================================================ Knopfdeklaration

        [Theory]
        [InlineData("btn_Loeschen")]
        [InlineData("btn_Löschen")]
        [InlineData("btn_LOESCHEN")]
        [InlineData("btn_Delete")]
        [InlineData("btn_DeleteAll")]
        [InlineData("btnVarianteLoeschenUnten")]
        public void LoeschknopfImControlpfad_LaesstSichNichtDeklarieren(string pfad)
        {
            // Zweite Linie zur Positivliste (Fachkonzept 11.3): Ein Loeschknopf ist nicht
            // „nicht deklariert", sondern nicht deklarierBAR.
            Assert.Throws<ArgumentException>(() => new KiDialogKnopf("speichern", pfad, "Speichern"));
        }

        [Theory]
        [InlineData("loeschen")]
        [InlineData("variante_loeschen")]
        [InlineData("delete_all")]
        public void LoeschknopfImNamen_LaesstSichNichtDeklarieren(string name)
        {
            Assert.Throws<ArgumentException>(() => new KiDialogKnopf(name, "btn_Weg", "Weg"));
        }

        [Theory]
        [InlineData("btn_Loeschen", true)]
        [InlineData("btn_Löschen", true)]
        [InlineData("BTN_DELETE", true)]
        [InlineData("btn_Speichern", false)]
        [InlineData("btn_Abbrechen", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void DieLoeschregel_SiehtNichtAufGrossUndKlein(string? text, bool erwartet)
        {
            Assert.Equal(erwartet, KiDialogKnopf.IstLoeschbezeichnung(text));
        }

        [Fact]
        public void DieErlaubtenKnoepfe_LassenSichDeklarieren()
        {
            foreach (string pfad in new[] { "btn_Speichern", "btn_Speichern_Unter",
                                            "btn_Ueberschreiben", "btn_Abbrechen" })
                Assert.Equal(pfad, new KiDialogKnopf("knopf", pfad, "Text").Controlpfad);
        }

        [Fact]
        public void KnopfOhnePflichttexte_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => new KiDialogKnopf("speichern", "btn_Speichern", " "));
            Assert.Throws<ArgumentException>(() => new KiDialogKnopf("Speichern", "btn_Speichern", "Speichern"));
            Assert.Throws<ArgumentException>(() => new KiDialogKnopf("speichern", "", "Speichern"));
        }

        // =========================================================== Maskendeklaration

        [Theory]
        [InlineData("Form_Heizkessel_Bearbeiten", true)]
        [InlineData("Form_PV", true)]
        [InlineData("Form_PufferSp_Bearbeiten", true)]
        [InlineData("_Form", true)]
        [InlineData("2Form", false)]
        [InlineData("Form PV", false)]
        [InlineData("Form.PV", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Maskenname_IstEinTypnameKeinAktionsname(string? name, bool erwartet)
        {
            Assert.Equal(erwartet, KiDialog.IstGueltigerMaskenname(name));
        }

        [Fact]
        public void MaskeOhneGueltigenNamen_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => new KiDialog("Form PV", "Photovoltaik"));
        }

        [Fact]
        public void MaskeOhneAnzeigenamen_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => new KiDialog("Form_PV", "  "));
        }

        [Fact]
        public void DoppelterFeldname_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => Maske(new[]
            {
                Feld("wartungskosten", "tb_Eins"),
                Feld("wartungskosten", "tb_Zwei")
            }));
        }

        [Fact]
        public void DoppelterKnopfname_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => Maske(knoepfe: new[]
            {
                Knopf("speichern", "btn_Eins"),
                Knopf("speichern", "btn_Zwei")
            }));
        }

        [Fact]
        public void DoppelterControlpfad_LaesstSichNichtDeklarieren()
        {
            Assert.Throws<ArgumentException>(() => Maske(new[]
            {
                Feld("wartungskosten", "gb_Kosten.tb_Wartung"),
                Feld("wartungskosten_2", "gb_Kosten.tb_Wartung")
            }));
        }

        [Fact]
        public void ControlpfadDoppelt_TrotzAndererSchreibweise_WirdAbgewiesen()
        {
            // Die Controlsuche des Bestands vergleicht ohne Ruecksicht auf Gross-/Klein-
            // schreibung; zwei solche Eintraege zeigten also auf dasselbe Control.
            Assert.Throws<ArgumentException>(() => Maske(new[]
            {
                Feld("eins", "gb_Kosten.tb_Wartung"),
                Feld("zwei", "GB_Kosten.TB_Wartung")
            }));
        }

        [Fact]
        public void DasselbeControl_AlsFeldUndAlsKnopf_WirdAbgewiesen()
        {
            Assert.Throws<ArgumentException>(() => Maske(
                new[] { Feld("speichern_feld", "btn_Speichern") },
                new[] { Knopf("speichern", "btn_Speichern") }));
        }

        [Fact]
        public void FelderUndKnoepfe_LassenSichUeberDenNamenFinden()
        {
            KiDialog m = Maske();

            Assert.True(m.KenntFeld("wartungskosten"));
            Assert.Equal("Wartungskosten", m.FindeFeld("wartungskosten")!.Anzeigename);
            Assert.True(m.KenntKnopf("speichern"));
            Assert.Equal("btn_Speichern", m.FindeKnopf("speichern")!.Controlpfad);

            Assert.False(m.KenntFeld("gibtesnicht"));
            Assert.Null(m.FindeFeld("gibtesnicht"));
            Assert.Null(m.FindeFeld(null));
            Assert.Null(m.FindeKnopf(null));
        }

        [Fact]
        public void DieNamenslistenKommenAlphabetisch()
        {
            // Sie stehen in der Klartext-Ablehnung: „das Feld gibt es nicht, bekannt sind …".
            KiDialog m = Maske(
                new[] { Feld("wartungskosten", "tb_Eins"), Feld("dauer", "tb_Zwei") },
                new[] { Knopf("speichern", "btn_Eins"), Knopf("abbrechen", "btn_Zwei") });

            Assert.Equal(new[] { "dauer", "wartungskosten" }, m.Feldnamen());
            Assert.Equal(new[] { "abbrechen", "speichern" }, m.Knopfnamen());
        }

        [Fact]
        public void OhneFelderUndKnoepfe_IstDieMaskeLeerAberGueltig()
        {
            var m = new KiDialog("Form_PV", "Photovoltaik");

            Assert.Empty(m.Felder);
            Assert.Empty(m.Knoepfe);
            Assert.False(m.HatKnopfposition);
            Assert.Null(m.Knopfposition);
        }

        [Fact]
        public void DieKnopfposition_IstJeMaskeDeklariertUndNieNegativ()
        {
            var m = new KiDialog("Form_PV", "Photovoltaik", null, null, new KiKnopfposition(12, 40));

            Assert.True(m.HatKnopfposition);
            Assert.Equal(12, m.Knopfposition!.AbstandRechts);
            Assert.Equal(40, m.Knopfposition!.AbstandOben);

            Assert.Throws<ArgumentOutOfRangeException>(() => new KiKnopfposition(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new KiKnopfposition(0, -1));
        }

        // ==================================================================== Katalog

        private static KiDialogKatalog Katalog()
            => new KiDialogKatalog(
                Maske(),
                new KiDialog("Form_PV", "Photovoltaik", new[] { Feld("leistung", "tb_Leistung") }),
                new KiDialog("Form_WP", "Wärmepumpe"),
                new KiDialog("Form_PufferSp_Bearbeiten", "Pufferspeicher bearbeiten"));

        [Fact]
        public void DerKatalogSchlaegtNachDemMaskennamenNach()
        {
            KiDialogKatalog k = Katalog();

            Assert.Equal(4, k.Anzahl);
            Assert.True(k.Kennt("Form_PV"));
            Assert.Equal("Photovoltaik", k.Finde("Form_PV")!.Anzeigename);
            Assert.False(k.Kennt("Form_Einstellungen"));
            Assert.Null(k.Finde("Form_Einstellungen"));
            Assert.Null(k.Finde(null));
        }

        [Fact]
        public void DerNachschlagSiehtNichtAufGrossUndKlein()
        {
            // Der Maskenname kommt als Parameterwert aus einem Modellaufruf; die
            // Schreibweise darf nicht ueber Treffer und Fehltreffer entscheiden.
            Assert.NotNull(Katalog().Finde("form_pv"));
            Assert.True(Katalog().Kennt("FORM_WP"));
        }

        [Fact]
        public void DerKatalogHaeltDieDeklarationsreihenfolge()
        {
            var namen = new List<string>();
            foreach (KiDialog d in Katalog()) namen.Add(d.Maskenname);

            Assert.Equal(new[] { "Form_Heizkessel_Bearbeiten", "Form_PV", "Form_WP",
                                 "Form_PufferSp_Bearbeiten" }, namen);
        }

        [Fact]
        public void DieMaskennamenKommenAlphabetisch()
        {
            Assert.Equal(new[] { "Form_Heizkessel_Bearbeiten", "Form_PV", "Form_PufferSp_Bearbeiten",
                                 "Form_WP" }, Katalog().Maskennamen());
        }

        [Fact]
        public void DieselbeMaskeZweimal_IstEinProgrammierfehler()
        {
            Assert.Throws<ArgumentException>(() => new KiDialogKatalog(Maske(), Maske()));
        }

        [Fact]
        public void DieselbeMaskeInAndererSchreibweise_WirdEbenfallsAbgewiesen()
        {
            Assert.Throws<ArgumentException>(() => new KiDialogKatalog(
                new KiDialog("Form_PV", "Photovoltaik"),
                new KiDialog("FORM_pv", "Photovoltaik, zweite")));
        }

        [Fact]
        public void EinLeererEintragKommtNichtInDenKatalog()
        {
            Assert.Throws<ArgumentException>(() => new KiDialogKatalog(new KiDialog[] { null! }));
            Assert.Throws<ArgumentNullException>(() => new KiDialogKatalog((IEnumerable<KiDialog>)null!));
        }

        [Fact]
        public void EinLeererKatalogIstMoeglich()
        {
            var k = new KiDialogKatalog();

            Assert.Equal(0, k.Anzahl);
            Assert.Empty(k.Alle);
            Assert.Empty(k.Maskennamen());
            Assert.False(k.Kennt("Form_PV"));
        }

        [Fact]
        public void EinLoeschknopfKommtGarNichtErstInEineMaske()
        {
            // Der Weg in den Katalog fuehrt ueber KiDialogKnopf - und dort endet er fuer
            // jeden Loeschknopf. Deshalb laesst sich der Katalogfall nicht einmal
            // hinschreiben: Die Maske mit Loeschknopf entsteht nie.
            Assert.Throws<ArgumentException>(() => Maske(knoepfe: new[]
            {
                Knopf("speichern", "btn_Speichern"),
                new KiDialogKnopf("loeschen", "btn_Loeschen", "Löschen")
            }));
        }
    }
}
