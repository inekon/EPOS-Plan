using System.Globalization;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Bestaetigungstext (Fachkonzept 3.2 Verwendung c, 3.5): Er entsteht aus der
    /// Deklaration, nicht aus Modelltext, und nennt Aktion, Angaben und Wirkung.
    /// </summary>
    public class KiBestaetigungTests
    {
        private static readonly CultureInfo De = new CultureInfo("de-DE");
        private static readonly CultureInfo En = new CultureInfo("en-US");

        private static KiAufruf Aufruf(KiAktion aktion, params object?[] paare)
        {
            KiPruefErgebnis p = KiPruefung.Pruefe(aktion, Beispielregister.Werte(paare));
            Assert.True(p.Gueltig, p.FehlerText());
            return p.Aufruf!;
        }

        [Fact]
        public void Text_NenntAktion_Stufe_Zweck_UndAngaben()
        {
            string text = KiBestaetigung.Erzeuge(Aufruf(Beispielregister.MitId(), "projekt_id", 1007),
                                                 null, De);

            Assert.Contains("projekt_lesen", text);
            Assert.Contains(KiTexte.StufeLesen, text);
            Assert.Contains("Liest die Kopfdaten eines Projekts.", text);
            Assert.Contains("Projekt (ID): 1007", text);
        }

        [Fact]
        public void Leseaktion_SagtAusdruecklichDassSieNichtsAendert()
        {
            string text = KiBestaetigung.Erzeuge(Aufruf(Beispielregister.MitId(), "projekt_id", 1007),
                                                 null, De);

            Assert.Contains(KiTexte.WirkungLesen, text);
        }

        [Fact]
        public void AktionOhneParameter_SagtDasStattEineLeereListeZuZeigen()
        {
            string text = KiBestaetigung.Erzeuge(Aufruf(Beispielregister.OhneParameter()), null, De);

            Assert.Contains(KiTexte.FeldAngaben + ": " + KiTexte.KeineAngaben, text);
        }

        [Fact]
        public void Einheit_StehtHinterDemWert()
        {
            string text = KiBestaetigung.Erzeuge(
                Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1, "schwelle_kw", 565.76), null, De);

            Assert.Contains("Zielschwelle: 565,76 kW", text);
        }

        [Fact]
        public void Zahlen_FolgenDerUebergebenenKultur()
        {
            KiAufruf a = Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1, "schwelle_kw", 565.76);

            Assert.Contains("565,76", KiBestaetigung.Erzeuge(a, null, De));
            Assert.Contains("565.76", KiBestaetigung.Erzeuge(a, null, En));
        }

        [Fact]
        public void IdsBekommenKeinenTausenderpunkt()
        {
            // „Projekt 1.007" waere irrefuehrend - eine ID ist keine Menge.
            string text = KiBestaetigung.Erzeuge(Aufruf(Beispielregister.MitId(), "projekt_id", 1007),
                                                 null, De);

            Assert.Contains("1007", text);
            Assert.DoesNotContain("1.007", text);
        }

        [Fact]
        public void Wahrheitswerte_ErscheinenAlsJaUndNein()
        {
            string text = KiBestaetigung.Erzeuge(
                Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1, "speichern", true), null, De);

            Assert.Contains("Speichern: ja", text);
        }

        [Fact]
        public void Vorschautext_WirdEingerueckt_UndNurWennVorhanden()
        {
            KiAufruf a = Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1);

            Assert.DoesNotContain(KiTexte.FeldVorschau, KiBestaetigung.Erzeuge(a, null, De));

            string mit = KiBestaetigung.Erzeuge(a, "Ich würde rechnen.\nDas dauert.", De);
            Assert.Contains(KiTexte.FeldVorschau + ":", mit);
            Assert.Contains(KiBestaetigung.Punkt + "Ich würde rechnen.", mit);
            Assert.Contains(KiBestaetigung.Punkt + "Das dauert.", mit);
        }

        [Fact]
        public void RechenaktionNenntIhreEigeneWirkung()
        {
            string text = KiBestaetigung.Erzeuge(
                Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1), null, De);

            Assert.Contains(KiTexte.StufeRechnen, text);
            Assert.Contains("Rechnet und speichert das Ergebnis.", text);
            Assert.DoesNotContain(KiTexte.WirkungLesen, text);
        }

        [Fact]
        public void OptionaleAngaben_ErscheinenNurWennGesetzt()
        {
            string ohne = KiBestaetigung.Erzeuge(
                Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1), null, De);

            Assert.DoesNotContain("Gewerk:", ohne);

            string mit = KiBestaetigung.Erzeuge(
                Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1,
                       "gewerk", Beispielregister.Gewerk1), null, De);

            Assert.Contains("Gewerk: " + Beispielregister.Gewerk1, mit);
        }

        [Fact]
        public void Kurzfassung_IstEineZeileMitAllenAngaben()
        {
            string kurz = KiBestaetigung.Kurzfassung(
                Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1007, "speichern", false), De);

            Assert.Equal("vielerlei (Projekt (ID): 1007; Speichern: nein)", kurz);
            Assert.DoesNotContain("\n", kurz);
        }

        [Fact]
        public void Kurzfassung_OhneParameter_IstNurDerName()
        {
            Assert.Equal("projekte_auflisten",
                         KiBestaetigung.Kurzfassung(Aufruf(Beispielregister.OhneParameter()), De));
        }

        [Fact]
        public void Werkzeugliste_FuehrtJedeAktionUndJedenParameter()
        {
            string liste = KiBestaetigung.Werkzeugliste(Beispielregister.Erzeuge());

            Assert.Contains("projekte_auflisten", liste);
            Assert.Contains("projekt_lesen", liste);
            Assert.Contains("vielerlei", liste);
            Assert.Contains("schwelle_kw (optional)", liste);
        }

        [Fact]
        public void Beschreibung_NenntDenAndockpunkt()
        {
            string text = KiBestaetigung.Beschreibe(Beispielregister.MitId());

            Assert.Contains(KiTexte.FeldAndockpunkt + ": ProjektCtrl.ReadSingle(int)", text);
        }

        [Fact]
        public void Zahlenliste_ErscheintKommagetrennt()
        {
            string text = KiBestaetigung.Erzeuge(
                Aufruf(Beispielregister.MitAllenTypen(), "projekt_id", 1,
                       "projekt_ids", new object[] { 1007, 1009 }), null, De);

            Assert.Contains("Projekte: 1007, 1009", text);
        }
    }
}
