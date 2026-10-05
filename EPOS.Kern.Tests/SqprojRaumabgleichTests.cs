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
    }
}
