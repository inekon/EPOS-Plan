using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Normwerte der DIN einlesen</b> (Stufe NP7, <see cref="RaumnutzungNormwerte"/>; Entscheid E131): Zuordnung über die
    /// DIN-Nummer und den Namen der Wohnprofile, Vorschau mit Treffern, unbekannten Nummern und Abweichungen, Überschreiben
    /// nur auf Wunsch, Herkunft, Rücknahme, die CSV-Vorlage ohne Werte, die Projektdatei als Quelle und die Umrechnung der
    /// Außenluft je Person im Generator. <b>Alle Werte sind runde Phantasiewerte</b> — keine Normwerte.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RaumnutzungNormwerteTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static readonly DateTime TAG = new DateTime(2026, 10, 12);

        private static byte[] Csv(params string[] zeilen)
            => Encoding.UTF8.GetBytes(string.Join("\r\n", new[] { "Nummer;Bezeichner;Heiz_Soll;Aussenluft_Person" }.Concat(zeilen)) + "\r\n");

        private static Raumnutzungsprofil Din(string nummer)
            => RaumnutzungNormwerte.DinProfile().Single(p => p.Nummer == nummer);

        private static Raumnutzungsprofil Wohnen()
            => RaumnutzungNormwerte.DinProfile().Single(p => p.Bezeichner == RaumnutzungNormkategorienSchema.WOHNEN_EFH);

        [Fact]
        public void Die_Nummer_ist_ohne_fuehrende_Nullen_der_Schluessel()
        {
            Assert.Equal("1", RaumnutzungNormwerte.Nummernschluessel(" 01 "));
            Assert.Equal("12a", RaumnutzungNormwerte.Nummernschluessel("12a"));
            Assert.Null(RaumnutzungNormwerte.Nummernschluessel("  "));
        }

        [Fact]
        public void Die_Testdatenbank_traegt_die_DIN_Profile_ohne_Werte_und_ohne_Herkunft()
        {
            if (!_db.Vorhanden) return;
            List<Raumnutzungsprofil> din = RaumnutzungNormwerte.DinProfile();
            Assert.Equal(RaumnutzungSaat.Din.Count + RaumnutzungNormkategorienSchema.WOHNPROFILE.Count, din.Count);
            Assert.All(din, p => Assert.True(p.IstLeer && p.Werte_Herkunft == null && p.Ausgeliefert));
            Assert.Equal(0, RaumnutzungNormwerte.Eingelesene());
        }

        [Fact]
        public void Csv_ordnet_ueber_Nummer_und_Wohnnamen_zu_und_liest_mit_Herkunft_ein()
        {
            if (!_db.Vorhanden) return;
            RaumnutzungNormwerte.Vorschau v = RaumnutzungNormwerte.AusCsv(
                Csv("01;irgendein Name;21;30", "99;Phantasie;20;", ";" + RaumnutzungNormkategorienSchema.WOHNEN_EFH.ToUpperInvariant() + ";20;"),
                "/pfad/probe.csv");
            Assert.True(v.Ok, v.Meldung);
            Assert.Equal("probe.csv", v.Quelle);
            Assert.Equal(new[] { "1", null }, v.Treffer.Select(t => t.Nummer));
            Assert.Single(v.Unbekannt);
            Assert.Equal(0, v.MitWerten);

            RaumnutzungNormwerte.Bilanz b = RaumnutzungNormwerte.Einlesen(v, ueberschreiben: false, TAG);
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(2, b.Geschrieben);
            Assert.Equal(2, RaumnutzungNormwerte.Eingelesene());

            Raumnutzungsprofil p = Din("1");
            Assert.Equal(21.0, p.Heiz_Soll);
            Assert.Equal(30.0, p.Aussenluft_Person);
            Assert.True(p.Eingelesen);
            Assert.Equal("probe.csv", p.Werte_Quelle);
            Assert.Equal("2026-10-12", p.Werte_Datum);
            Assert.True(p.Ausgeliefert);
            Assert.NotEqual("irgendein Name", p.Bezeichner);       // die Struktur bleibt
            Assert.Equal(20.0, Wohnen().Heiz_Soll);
        }

        [Fact]
        public void Vorhandene_Werte_werden_nur_auf_Wunsch_ueberschrieben_und_die_Abweichung_benannt()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(1, RaumnutzungNormwerte.Einlesen(RaumnutzungNormwerte.AusCsv(Csv("1;a;21;30"), "eins.csv"), false, TAG).Geschrieben);

            RaumnutzungNormwerte.Vorschau zweite = RaumnutzungNormwerte.AusCsv(Csv("1;a;22;30"), "zwei.csv");
            Assert.Equal(1, zweite.MitWerten);
            Assert.Equal(1, zweite.MitAbweichung);
            Assert.Equal(new[] { "Heiz_Soll" }, zweite.Treffer.Single().Abweichungen);
            Assert.True(zweite.Treffer.Single().Eingelesen);

            RaumnutzungNormwerte.Bilanz nein = RaumnutzungNormwerte.Einlesen(zweite, false, TAG);
            Assert.Equal((0, 1), (nein.Geschrieben, nein.Uebersprungen));
            Assert.Equal(21.0, Din("1").Heiz_Soll);

            Assert.Equal(1, RaumnutzungNormwerte.Einlesen(zweite, true, TAG).Geschrieben);
            Assert.Equal(22.0, Din("1").Heiz_Soll);
            Assert.Equal("zwei.csv", Din("1").Werte_Quelle);
        }

        [Fact]
        public void Entfernen_nimmt_alle_eingelesenen_Werte_zurueck()
        {
            if (!_db.Vorhanden) return;
            RaumnutzungNormwerte.Einlesen(RaumnutzungNormwerte.AusCsv(Csv("1;a;21;30", "2;b;20;40"), "probe.csv"), false, TAG);
            Assert.Equal(2, RaumnutzungNormwerte.Eingelesene());

            RaumnutzungNormwerte.Bilanz b = RaumnutzungNormwerte.Entfernen();
            Assert.True(b.Ok, b.Meldung);
            Assert.Equal(2, b.Geschrieben);
            Assert.Equal(0, RaumnutzungNormwerte.Eingelesene());
            Assert.All(RaumnutzungNormwerte.DinProfile(), p => Assert.True(p.IstLeer && p.Werte_Herkunft == null &&
                                                                          p.Werte_Quelle == null && p.Werte_Datum == null));
        }

        /// <summary>
        /// NP7b: Die Vorschau führt doppelte Nummern und Profile ohne Wert eigens (zugleich in den Meldungen); die Hülle des
        /// Dialogs reicht sie getrennt weiter, die übrigen Meldungen ohne sie, und schreibt nur die zuletzt geladene Vorschau.
        /// </summary>
        [Fact]
        public void Doppelte_Nummern_und_Profile_ohne_Wert_stehen_eigens_und_die_Huelle_trennt_sie()
        {
            if (!_db.Vorhanden) return;
            RaumnutzungNormwerte.Vorschau v = RaumnutzungNormwerte.AusCsv(Csv("1;a;21;", "01;b;22;", "2;c;;", "99;x;20;"), "probe.csv");
            Assert.True(v.Ok, v.Meldung);
            Assert.Single(v.Treffer);
            Assert.Single(v.Doppelt);
            Assert.Single(v.OhneWerte);
            Assert.Single(v.Unbekannt);
            Assert.Equal(2, v.Meldungen.Count);
            Assert.Contains(v.Doppelt[0], v.Meldungen);
            Assert.Contains(v.OhneWerte[0], v.Meldungen);

            EPOS.UI.Dialoge.Bedarf.NormwerteVorschau d = RaumnutzungNormwerteHuelle.Vorschau(v);
            Assert.True(d.Ok);
            Assert.Equal("probe.csv", d.Datei);
            Assert.Equal("1", d.Treffer.Single().Nummer);
            Assert.Single(d.Doppelt);
            Assert.Single(d.OhneWerte);
            Assert.Single(d.Unbekannt);
            Assert.Empty(d.Sonstige);

            Assert.False(RaumnutzungNormwerteHuelle.Einlesen(null, false).Ok);
            Assert.NotNull(RaumnutzungNormwerteHuelle.Weg(ios: false));
            Assert.Equal(1, RaumnutzungNormwerteHuelle.Einlesen(v, false).Id);
            Assert.Equal(1, RaumnutzungNormwerte.Eingelesene());
        }

        [Fact]
        public void Eine_Quelle_ohne_Treffer_oder_ohne_Werte_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            RaumnutzungNormwerte.Vorschau v = RaumnutzungNormwerte.AusCsv(Csv("99;x;20;", "1;a;;"), "probe.csv");
            Assert.False(v.Ok);
            Assert.False(string.IsNullOrEmpty(v.Meldung));
            Assert.Single(v.Unbekannt);
            Assert.Single(v.Meldungen);   // Nr. 1 ohne Wert
            Assert.False(RaumnutzungNormwerte.Einlesen(v, true, TAG).Ok);
            Assert.Equal(0, RaumnutzungNormwerte.Eingelesene());
        }

        [Fact]
        public void Die_Projektdatei_ist_Quelle_ueber_dieselbe_Zuordnung()
        {
            if (!_db.Vorhanden) return;
            var satz = new Projektdateiprofile
            {
                Dateiname = "probe.sqproj",
                Profile = new[]
                {
                    new Projektdateiprofil { Profil = new Raumnutzungsprofil { Nummer = "2", Bezeichner = "Name der Datei", Heiz_Soll = 20.0, Aussenluft_Person = 25.0 } },
                    new Projektdateiprofil { Profil = new Raumnutzungsprofil { Nummer = "70", Bezeichner = "Wohnen der Datei", Heiz_Soll = 20.0 } },
                },
            };
            RaumnutzungNormwerte.Vorschau v = RaumnutzungNormwerte.AusProjektdatei(satz);
            Assert.True(v.Ok, v.Meldung);
            Assert.Equal("2", v.Treffer.Single().Nummer);
            Assert.Single(v.Unbekannt);
            Assert.Equal(1, RaumnutzungNormwerte.Einlesen(v, false, TAG).Geschrieben);
            Assert.Equal(25.0, Din("2").Aussenluft_Person);
            Assert.Equal("probe.sqproj", Din("2").Werte_Quelle);
        }

        [Fact]
        public void Die_Vorlage_fuehrt_Nummern_und_Namen_ohne_einen_Wert()
        {
            if (!_db.Vorhanden) return;
            RaumnutzungNormwerte.Einlesen(RaumnutzungNormwerte.AusCsv(Csv("1;a;21;30"), "probe.csv"), false, TAG);
            string text = Encoding.UTF8.GetString(RaumnutzungNormwerte.Vorlage()).TrimStart('﻿');
            string[] zeilen = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            Assert.Contains(RaumnutzungNormwerteSchema.SPALTE_AUSSENLUFT_PERSON, zeilen[0].Split(';'));
            Assert.Equal(RaumnutzungNormwerte.DinProfile().Count, zeilen.Length - 1);
            Assert.All(zeilen.Skip(1), z => Assert.All(z.Split(';').Skip(3), f => Assert.Equal("", f)));

            // Der Rundlauf: die Vorlage selbst trägt keine Werte - einlesen findet nichts.
            Assert.False(RaumnutzungNormwerte.AusCsv(RaumnutzungNormwerte.Vorlage(), "vorlage.csv").Ok);
        }

        [Fact]
        public void Der_Generator_rechnet_Aussenluft_je_Person_mit_der_Belegung_um_und_benennt_die_fehlende()
        {
            var p = new Raumnutzungsprofil { Bezeichner = "Probe", Aussenluft_Person = 30.0, Personen_Flaeche = 10.0 };
            Raumnutzungsgroesse mit = Raumnutzungsgenerator.Erzeugen(p, Konditionierungsgroesse.Lueftung, null, 3.0);
            Assert.Equal(Raumnutzungsweg.Kennwerte, mit.Weg);
            Assert.Equal(new double?[] { 1.0 }, mit.Vorlage.Inhalt.Vorgabezeilen().Select(z => z.Wert));   // 30 ÷ 10 ÷ 3 = 1/h

            p.Personen_Flaeche = null;
            Raumnutzungsgroesse ohne = Raumnutzungsgenerator.Erzeugen(p, Konditionierungsgroesse.Lueftung, null, 3.0);
            Assert.Equal(Raumnutzungsweg.Keiner, ohne.Weg);
            Assert.Equal(Raumnutzungshinweis.LueftungOhneBelegung, ohne.Hinweis);

            p.Personen_Flaeche = 10.0;
            Assert.Equal(Raumnutzungshinweis.LueftungOhneHoehe, Raumnutzungsgenerator.Erzeugen(p, Konditionierungsgroesse.Lueftung, null, null).Hinweis);

            // Die raumbezogene Außenluft geht vor.
            p.Aussenluft = 2.0;
            p.Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE;
            Assert.Equal(new double?[] { 2.0 },
                         Raumnutzungsgenerator.Erzeugen(p, Konditionierungsgroesse.Lueftung, null, 3.0).Vorlage.Inhalt.Vorgabezeilen().Select(z => z.Wert));
        }
    }
}
