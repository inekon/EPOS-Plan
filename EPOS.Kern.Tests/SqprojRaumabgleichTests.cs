using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe 34 — Raumabgleich</b> (Datenaustauschkonzept 16.7, Befund Kapitel 6): Räume treffen über <c>GId</c> ↔
    /// <c>GlobalId</c> (umkodiert), dann über den Namen je Geschoss, und bleiben benannt unzugeordnet, wenn beides fehlt;
    /// die Raumart ist Beleg, kein Schlüssel.
    /// </summary>
    public sealed class SqprojRaumabgleichTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _pfade = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string p in _pfade)
                try { if (File.Exists(p)) File.Delete(p); } catch (IOException) { }
        }

        private SqprojAbbild Lesen(SqprojProbenErzeuger e)
        {
            string p = SqprojProbenErzeuger.TempPfad("abgleich");
            _pfade.Add(p);
            return SqprojLeser.Lesen(e.Schreiben(p));
        }

        [Fact]
        public void Kennung_dann_Name_je_Geschoss_sonst_benannt_ohne_Treffer()
        {
            SqprojAbbild projekt = Lesen(SqprojProbenErzeuger.Standard());
            GebaeudeAbbild ifc = SqprojProbenErzeuger.IfcAbbild();
            SqprojRaumabgleich a = SqprojRaumabgleich.Bilden(projekt, ifc.Gebaeude[0]);
            Assert.Equal(3, a.UeberName);
            Assert.Equal(1, a.UeberKennung);
            Assert.Equal(4, a.Abgeglichen);
            Assert.Equal("0000000000000000000R0B", a.IfcRaum("R2"));   // Groß-/Kleinschreibung und Rand zählen nicht
            Assert.Equal(SqprojRaumabgleich.IfcKennung(SqprojProbenErzeuger.GID_D), a.IfcRaum("R4"));
            Assert.Equal(new[] { "Raum C" }, a.RaumartAbweichend.Select(r => r.Name));   // Beleg, kein Schlüssel: C bleibt gepaart
            Assert.Equal("0000000000000000000R0C", a.IfcRaum("R3"));
            Assert.Equal(new[] { "Raum E" }, a.OhneTreffer.Select(r => r.Name));
            Assert.Equal(new[] { "Raum Y" }, a.IfcOhneGegenstueck.Select(r => r.Name));
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojProtokoll.RAUM_OHNE_TREFFER && m.Werte[0] == "1" && m.Werte[1] == "Raum E");
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojProtokoll.IFC_RAUM_OHNE_GEGENSTUECK && m.Werte[1] == "Raum Y");
        }

        [Fact]
        public void Die_Kennung_schlaegt_den_Namen()
        {
            // Raum „Raum A“ trägt die GId des IFC-Raums X: die Kennung entscheidet, der Name A bleibt frei.
            SqprojAbbild projekt = Lesen(new SqprojProbenErzeuger().Geschoss("F1", "EG").Raum("R1", "Raum A", "F1", SqprojProbenErzeuger.GID_D, 10.0));
            SqprojRaumabgleich a = SqprojRaumabgleich.Bilden(projekt, SqprojProbenErzeuger.IfcAbbild().Gebaeude[0]);
            Assert.Equal(1, a.UeberKennung);
            Assert.Equal(0, a.UeberName);
            Assert.Contains(a.IfcOhneGegenstueck, r => r.Name == "Raum A");
        }

        [Fact]
        public void Ein_doppelter_Name_im_Geschoss_trifft_nicht()
        {
            SqprojAbbild projekt = Lesen(new SqprojProbenErzeuger().Geschoss("F1", "EG")
                .Raum("R1", "Raum A", "F1", null, 10.0).Raum("R2", "Raum A", "F1", null, 10.0));
            SqprojRaumabgleich a = SqprojRaumabgleich.Bilden(projekt, SqprojProbenErzeuger.IfcAbbild().Gebaeude[0]);
            Assert.Equal(0, a.Abgeglichen);
            Assert.Equal(2, a.OhneTreffer.Count);
        }

        [Fact]
        public void Ein_Name_in_einem_anderen_Geschoss_trifft_nicht()
        {
            SqprojAbbild projekt = Lesen(new SqprojProbenErzeuger().Geschoss("F2", "OG").Raum("R1", "Raum A", "F2", null, 10.0));
            Assert.Equal(0, SqprojRaumabgleich.Bilden(projekt, SqprojProbenErzeuger.IfcAbbild().Gebaeude[0]).Abgeglichen);
        }

        [Theory]
        [InlineData("{00000000-0000-0000-0000-000000000000}", "0000000000000000000000")]
        [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", "3$$$$$$$$$$$$$$$$$$$$$")]
        public void Die_GUID_wird_in_die_IFC_Form_umkodiert(string guid, string ifc)
            => Assert.Equal(ifc, SqprojRaumabgleich.IfcKennung(guid));

        [Theory]
        [InlineData("{0A1B2C3D-4E5F-6071-8293-A4B5C6D7E8F9}")]
        [InlineData("12345678-9abc-def0-1234-56789abcdef0")]
        [InlineData("{FEDCBA98-7654-3210-0F1E-2D3C4B5A6978}")]
        public void Die_Umkodierung_ist_umkehrbar(string guid)
        {
            string ifc = SqprojRaumabgleich.IfcKennung(guid);
            Assert.Equal(22, ifc.Length);
            const string zeichen = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";
            BigInteger n = ifc.Aggregate(BigInteger.Zero, (s, c) => s * 64 + zeichen.IndexOf(c));
            string hex = n.ToString("x").TrimStart('0').PadLeft(32, '0');
            string erwartet = new string(guid.Where(Uri.IsHexDigit).ToArray()).ToLowerInvariant();
            Assert.Equal(erwartet, hex);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("{0A1B2C3D-4E5F}")]
        [InlineData("{0A1B2C3D-4E5F-6071-8293-A4B5C6D7E8FX}")]
        public void Keine_GUID_hat_keine_IFC_Form(string guid) => Assert.Null(SqprojRaumabgleich.IfcKennung(guid));

        // ------------------------------------------------------------------
        //  HottCAD-Kennung GUID (HSETU_BauteilAllgemein.GUID): vor der GlobalId
        // ------------------------------------------------------------------

        private const string GID_G = "{3B6425E5-4435-40C6-8821-6F4E16E47855}";
        private const string GID_H = "{ADEF8666-4B52-4F84-B45F-6FC96A684D24}";

        /// <summary>Ein IFC-Gebäude, dessen Räume eine neu vergebene GlobalId tragen; die HottCAD-Kennung je Raum frei.</summary>
        private static AbbildGebaeude NeuVergeben(params (string Kennung, string Name, string Guid)[] raeume)
        {
            var g = new AbbildGebaeude { Kennung = "0000000000000000000GEB", Name = "Probegebäude", Art = "TModelBuilding" };
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "0000000000000000000G01", Name = "EG" });
            foreach (var r in raeume)
                g.Raeume.Add(new AbbildRaum
                {
                    Kennung = r.Kennung, Name = r.Name, GeschossKennung = "0000000000000000000G01", FlaecheM2 = 10.0, VolumenM3 = 30.0,
                    HottcadGuid = IfcAbbildBauer.GuidNormalform(r.Guid),
                });
            return g;
        }

        [Fact]
        public void Die_GUID_trifft_wo_die_GlobalId_neu_vergeben_ist()
        {
            SqprojAbbild projekt = Lesen(new SqprojProbenErzeuger().Geschoss("F1", "EG")
                .Raum("R1", "Raum G", "F1", GID_G, 10.0).Raum("R2", "Raum H", "F1", GID_H, 10.0));
            // Namen verschieden, GlobalId neu vergeben: nur die GUID trifft — auch in anderer Schreibweise.
            SqprojRaumabgleich a = SqprojRaumabgleich.Bilden(projekt, NeuVergeben(
                ("1111111111111111111111", "anders 1", "3b6425e5-4435-40c6-8821-6f4e16e47855"),
                ("2222222222222222222222", "anders 2", "ADEF86664B524F84B45F6FC96A684D24")));
            Assert.Equal(2, a.UeberGuid);
            Assert.Equal(0, a.UeberKennung);
            Assert.Equal(0, a.UeberName);
            Assert.Equal("1111111111111111111111", a.IfcRaum("R1"));
            Assert.Equal("2222222222222222222222", a.IfcRaum("R2"));
            Assert.Equal(SqprojRaumabgleich.Herkunft.Guid, a.HerkunftVon("R1"));
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojProtokoll.RAUM_HERKUNFT && m.Werte[0] == "2" && m.Werte[1] == "0" && m.Werte[2] == "0");
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel == SqprojProtokoll.RAUM_OHNE_TREFFER);
        }

        [Fact]
        public void Die_GUID_schlaegt_GlobalId_und_Namen()
        {
            // R1 trägt GID_G; der IFC-Raum mit der dekodierten GlobalId heißt wie R1, die GUID zeigt auf den anderen.
            SqprojAbbild projekt = Lesen(new SqprojProbenErzeuger().Geschoss("F1", "EG").Raum("R1", "Raum G", "F1", GID_G, 10.0));
            SqprojRaumabgleich a = SqprojRaumabgleich.Bilden(projekt, NeuVergeben(
                (SqprojRaumabgleich.IfcKennung(GID_G), "Raum G", null),
                ("3333333333333333333333", "anders", GID_G)));
            Assert.Equal("3333333333333333333333", a.IfcRaum("R1"));
            Assert.Equal(1, a.UeberGuid);
            Assert.Equal(new[] { "Raum G" }, a.IfcOhneGegenstueck.Select(r => r.Name));
        }

        [Fact]
        public void Ohne_GUID_bleibt_der_Abgleich_wie_er_war()
        {
            SqprojAbbild projekt = Lesen(SqprojProbenErzeuger.Standard());
            SqprojRaumabgleich a = SqprojRaumabgleich.Bilden(projekt, SqprojProbenErzeuger.IfcAbbild().Gebaeude[0]);
            Assert.Equal((0, 1, 3), (a.UeberGuid, a.UeberKennung, a.UeberName));
            Assert.Equal(SqprojRaumabgleich.Herkunft.Kennung, a.HerkunftVon("R4"));
            Assert.Equal(SqprojRaumabgleich.Herkunft.Name, a.HerkunftVon("R1"));
            Assert.Null(a.HerkunftVon("R5"));
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel == SqprojProtokoll.RAUM_HERKUNFT || m.Schluessel == SqprojProtokoll.GUID_MEHRDEUTIG);

            // GUID und GlobalId zeigen auf denselben Raum: dasselbe Paar, die Herkunft ist die GUID.
            GebaeudeAbbild ifc = SqprojProbenErzeuger.IfcAbbild();
            ifc.Gebaeude[0].Raeume.Single(r => r.Name == "Raum X").HottcadGuid = IfcAbbildBauer.GuidNormalform(SqprojProbenErzeuger.GID_D);
            SqprojRaumabgleich b = SqprojRaumabgleich.Bilden(projekt, ifc.Gebaeude[0]);
            Assert.Equal(a.Paarungen.OrderBy(p => p.Key), b.Paarungen.OrderBy(p => p.Key));
            Assert.Equal((1, 0, 3), (b.UeberGuid, b.UeberKennung, b.UeberName));
        }

        [Fact]
        public void Eine_doppelte_GUID_wird_nicht_geraten()
        {
            // IFC-Seite: zwei Räume mit derselben GUID — R1 fällt auf den Namen zurück; R2 hat keinen Namensgegenpart.
            SqprojAbbild projekt = Lesen(new SqprojProbenErzeuger().Geschoss("F1", "EG")
                .Raum("R1", "Raum G", "F1", GID_G, 10.0).Raum("R2", "Raum H", "F1", GID_H, 10.0).Raum("R3", "Raum H2", "F1", GID_H, 10.0));
            SqprojRaumabgleich a = SqprojRaumabgleich.Bilden(projekt, NeuVergeben(
                ("1111111111111111111111", "Raum G", GID_G),
                ("2222222222222222222222", "anders", GID_G),
                ("3333333333333333333333", "anders 3", GID_H)));
            Assert.Equal(0, a.UeberGuid);
            Assert.Equal(1, a.UeberName);
            Assert.Equal(SqprojRaumabgleich.Herkunft.Name, a.HerkunftVon("R1"));
            Assert.Equal("1111111111111111111111", a.IfcRaum("R1"));
            // Projektseite: R2 und R3 tragen dieselbe GUID — keiner trifft über sie.
            Assert.Null(a.IfcRaum("R2"));
            Assert.Null(a.IfcRaum("R3"));
            Assert.Equal(new[] { "Raum G", "Raum H", "Raum H2" }, a.GuidMehrdeutig.Select(r => r.Name));
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojProtokoll.GUID_MEHRDEUTIG && m.Werte[0] == "3");
        }

        [Theory]
        [InlineData("{3B6425E5-4435-40C6-8821-6F4E16E47855}", "3b6425e5-4435-40c6-8821-6f4e16e47855")]
        [InlineData(" 3b6425e5-4435-40c6-8821-6f4e16e47855 ", "3b6425e5-4435-40c6-8821-6f4e16e47855")]
        [InlineData("3B6425E5443540C688216F4E16E47855", "3b6425e5-4435-40c6-8821-6f4e16e47855")]
        [InlineData("2wPYNb5$J8bPVhH6jkTX1L", null)]
        [InlineData("{3B6425E5-4435}", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Die_Normalform_der_GUID(string text, string erwartet) => Assert.Equal(erwartet, IfcAbbildBauer.GuidNormalform(text));
    }
}
