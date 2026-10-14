using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache: Die Klappliste der Wärmepumpe steht in der Kältemitteltabelle</b> (KM4-E1-b, Fachkonzept Kältemittel als
    /// Stammdatum 3.2 und 10): Jeder der zehn Codes von <see cref="Bivalenzvorgaben"/> (ohne <c>SONSTIGES</c>) ist ein Code
    /// der Testdatenbank und der Saat — sonst fände der Code-Abgleich eines Wärmepumpen-Kältemittels keine Zeile. Die
    /// Klappliste selbst bleibt unverändert.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemittelCodesWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static IReadOnlyList<string> Klappliste =>
            Bivalenzvorgaben.Kaeltemittelcodes.Where(c => c != Bivalenzvorgaben.KM_SONSTIGES).ToList();

        [Fact]
        public void Die_zehn_Codes_der_Klappliste_stehen_in_der_Saat()
        {
            Assert.Equal(10, Klappliste.Count);
            var saat = new HashSet<string>(KaeltemittelSchema.SAAT.Select(k => k.Code), StringComparer.Ordinal);
            foreach (string c in Klappliste)
                Assert.True(saat.Contains(c), "Code der Klappliste fehlt in der Saat: " + c);
        }

        [Fact]
        public void Die_zehn_Codes_der_Klappliste_stehen_in_der_Testdatenbank()
        {
            if (!_db.Vorhanden) return;
            DataTable dt = DataRepository.GetDataTable("SELECT \"" + KaeltemittelSchema.SPALTE_CODE + "\" FROM \"" +
                                                       KaeltemittelSchema.TAB_STAMM + "\"");
            var codes = new HashSet<string>(dt.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)),
                                            StringComparer.Ordinal);
            foreach (string c in Klappliste)
            {
                Assert.True(codes.Contains(c), "Code der Klappliste fehlt in Tab_Kaeltemittel_STAMM: " + c);
                Assert.NotNull(KaeltemittelCtrl.IdZumCode(c));
            }
        }
    }
}
