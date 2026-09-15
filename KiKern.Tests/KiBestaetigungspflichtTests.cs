using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die EINE Stelle, an der ueber die Bestaetigungspflicht entschieden wird
    /// (<see cref="KiBestaetigungspflicht"/>, Fachkonzept 11.5, Paket F4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Seit dem 14.09.2026 gibt es hier nichts mehr abzuschalten.</b> Bis dahin trug
    /// die Feldsicherung einen prozessweiten, nicht zuruecksetzbaren Zustand
    /// (Befehlszeilenschalter <c>/ki-feldsicherung-aus</c>), und diese Faelle mussten
    /// darauf Ruecksicht nehmen: Sie durften nur Zusagen pruefen, die in BEIDEN
    /// Zustaenden gelten. Der Schalter ist entfallen - die Antwort dieser Klasse ist
    /// seither in jedem Lauf dieselbe.
    /// </para>
    /// </remarks>
    public class KiBestaetigungspflichtTests
    {
        private static KiAktion Leseaktion()
            => new KiAktion("projekt_lesen", "Liest ein Projekt.", Schutzstufe.Lesen,
                            "ProjektCtrl.Read");

        private static KiAktion Schreibaktion(string name = "kostenposition_setzen")
            => new KiAktion(name, "Setzt eine Kostenposition.", Schutzstufe.Schreiben,
                            "KostenCtrl.Update",
                            vorschau: _ => "Ich wuerde eine Kostenposition setzen.");

        private static KiAktion Formularaktion(string name = "feld_setzen",
                                               bool datenbankwirksam = false)
            => new KiAktion(name, "Traegt einen Wert ein.", Schutzstufe.Schreiben,
                            "KiDialogZugriff.Setze",
                            vorschau: _ => "Wartungskosten · 850 → 1200",
                            formularaktion: true,
                            datenbankwirksam: datenbankwirksam);

        [Fact]
        public void OhneAktionGibtEsNichtsZuBestaetigen()
        {
            Assert.False(KiBestaetigungspflicht.Gilt((KiAktion?)null));
            Assert.False(KiBestaetigungspflicht.Gilt((KiAufruf?)null));
        }

        [Fact]
        public void ReinesLesenBrauchtNieEineBestaetigung()
        {
            Assert.False(KiBestaetigungspflicht.Gilt(Leseaktion()));
        }

        [Fact]
        public void EineGewoehnlicheSchreibaktionBrauchtIMMERDieBestaetigung()
        {
            // Der Kern der Zusage aus Fachkonzept 11.5: Der Schalter erreicht die Stufe 2
            // nicht. Der Fall gilt unabhaengig davon, ob die Feldsicherung in diesem
            // Testlauf bereits abgeschaltet wurde - genau das ist sein Zweck.
            foreach (string name in new[] { "kostenposition_setzen", "variante_anlegen" })
            {
                KiAktion a = Schreibaktion(name);
                Assert.True(KiBestaetigungspflicht.Gilt(a),
                            name + " muss in JEDEM Fall bestaetigungspflichtig bleiben.");
                Assert.Equal(KiRiegel.BrauchtBestaetigung(a), KiBestaetigungspflicht.Gilt(a));
            }
        }

        /// <summary>
        /// <b>Auch eine Formularaktion wird IMMER bestaetigt</b> (Auftraggeber,
        /// 14.09.2026: „eine Bestaetigung was gesetzt wird sollte immer erscheinen").
        /// Bis dahin hing genau dieser Fall am Befehlszeilenschalter
        /// <c>/ki-feldsicherung-aus</c>; den gibt es nicht mehr.
        /// </summary>
        [Fact]
        public void EineFormularaktionWirdIMMERBestaetigt()
        {
            KiAktion a = Formularaktion();

            Assert.True(KiBestaetigungspflicht.Gilt(a));
            Assert.Equal(KiRiegel.BrauchtBestaetigung(a), KiBestaetigungspflicht.Gilt(a));
        }

        [Fact]
        public void DieAntwortIstGENAUDieDesRiegels()
        {
            // Diese Klasse darf nichts hinzufuegen und nichts wegnehmen - sie ist seit
            // dem Wegfall der Feldsicherung eine reine Weiterleitung, und genau das
            // haelt dieser Fall fest.
            var alle = new List<KiAktion>
            {
                Leseaktion(), Schreibaktion(), Formularaktion(),
                Formularaktion("dialog_aktion_ausfuehren", datenbankwirksam: true)
            };

            foreach (KiAktion a in alle)
                Assert.Equal(KiRiegel.BrauchtBestaetigung(a), KiBestaetigungspflicht.Gilt(a));
        }

        [Fact]
        public void DerAufrufwegLiefertDasselbeWieDerAktionsweg()
        {
            // Beide Ueberladungen muessen dieselbe Antwort geben - sonst haengt das
            // Ergebnis davon ab, welche Aufrufstelle gerade fragt.
            KiAktion a = Schreibaktion();
            KiPruefErgebnis p = KiPruefung.Pruefe(a, new Dictionary<string, object?>());
            Assert.True(p.Gueltig, p.FehlerText());

            Assert.Equal(KiBestaetigungspflicht.Gilt(a), KiBestaetigungspflicht.Gilt(p.Aufruf));
        }

        // =====================================================================
        // Sicherungspunkt-Regel (Festlegung Paket F4)
        // =====================================================================

        [Fact]
        public void OhneAngabeIstEineAktionDatenbankwirksam()
        {
            // Die Vorgabe zeigt in die unschaedliche Richtung: eine vergessene Angabe
            // kostet eine ueberfluessige Kopie, nicht den Rueckweg.
            Assert.True(Schreibaktion().Datenbankwirksam);
            Assert.True(Schreibaktion().BrauchtSicherungspunkt);
            Assert.True(Leseaktion().Datenbankwirksam);
        }

        [Fact]
        public void ReinesLesenBrauchtKeinenSicherungspunkt()
        {
            Assert.False(Leseaktion().BrauchtSicherungspunkt);
        }

        [Fact]
        public void EinReinerOberflaechenEintragBrauchtKeinenSicherungspunkt()
        {
            // feld_setzen / formular_ausfuellen: Text in ein Eingabefeld, Datenbank
            // unberuehrt. Die Bestaetigungspflicht bleibt davon voellig unberuehrt.
            KiAktion a = Formularaktion();

            Assert.False(a.Datenbankwirksam);
            Assert.False(a.BrauchtSicherungspunkt);
            Assert.Equal(Schutzstufe.Schreiben, a.Stufe);
        }

        [Fact]
        public void DialogAktionAusfuehrenBehaeltIhrenSicherungspunkt()
        {
            // Der ausgeloeste Knopf schreibt ueber den Bestand in die Datenbank - deshalb
            // wird die Frage je Aktion entschieden und nicht pauschal fuer alle
            // Formularaktionen.
            KiAktion a = Formularaktion("dialog_aktion_ausfuehren", datenbankwirksam: true);

            Assert.True(a.Formularaktion);
            Assert.True(a.BrauchtSicherungspunkt);
        }

        [Fact]
        public void NurEineFormularaktionDarfSichVomSicherungspunktFreistellen()
        {
            // Sonst entstuende genau der Fall, den Fachkonzept 4.4 ausschliesst: eine
            // Aenderung am Datenbestand ohne Rueckweg.
            Assert.Throws<ArgumentException>(() => new KiAktion(
                "kostenposition_setzen", "Setzt eine Kostenposition.", Schutzstufe.Schreiben,
                "KostenCtrl.Update",
                vorschau: _ => "Vorschau",
                datenbankwirksam: false));
        }
    }
}
