using System;
using System.Text;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Standpaar zur IFC-Probe <c>ifc4_zonen.ifc</c></b> (Standprüfung, Anwenderentscheid vom 08.10.2026) — ohne Kerntypen,
    /// deshalb auch in <c>EPOS.UI.Tests</c> verlinkt: Der Dialog liest die echte IFC über die echte Hülle, die Projektdatei trägt
    /// die Hüllflächen der Fassaden und des Dachs mit eigenem U. Abgeglichen wird über die dekodierte <c>GlobalId</c> (die Probe
    /// trägt keine HottCAD-Eigenschaft <c>GUID</c>).
    /// </summary>
    internal sealed partial class SqprojProbenErzeuger
    {
        /// <summary>Die Zeichen der komprimierten IFC-<c>GlobalId</c> (Basis 64).</summary>
        private const string GLOBALID_ZEICHEN = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$";

        /// <summary>Die Außenwände über Erdreich und das Dach der Probe <c>ifc4_zonen.ifc</c> (GlobalId, Elementtyp, Raum).</summary>
        private static readonly (string GlobalId, int Element, string Raum)[] STANDHAUS_FLAECHEN =
        {
            ("000000001008000000002G", 1, "R2"),   // Fassade Süd
            ("000000001008000000004W", 1, "R2"),   // EG Nord
            ("000000001008000000004b", 1, "R2"),   // EG Ost
            ("000000001008000000004f", 1, "R2"),   // EG West
            ("000000001008000000004p", 1, "R4"),   // OG Nord
            ("000000001008000000004u", 1, "R4"),   // OG Ost
            ("0000000010080000000052", 1, "R4"),   // OG West
            ("000000001008000000005v", 5, "R4"),   // Dach
        };

        /// <summary>Der Aufbau der Projektdatei im Standhaus.</summary>
        internal const string STANDHAUS_AUFBAU = "{e2000000-0000-0000-0000-0000000000a1}";

        /// <summary>Das U der Projektdatei im Standhaus [W/(m²K)] — weit über dem der IFC (0,28 bis 0,3).</summary>
        internal const double STANDHAUS_U = 1.4;

        /// <summary>
        /// <b>Das Zonenhaus mit abweichenden Aufbauten</b>: <see cref="Zonenhaus"/> samt Hüllflächen der Fassaden und des Dachs, je
        /// mit dem Aufbau „AW Altbau“ und <see cref="STANDHAUS_U"/>, Baujahr 1950 — zur HottCAD-Fassung der IFC-Probe
        /// (<see cref="HottcadZonenhaus"/>) schlägt die Standprüfung an.
        /// </summary>
        internal static SqprojProbenErzeuger StandZonenhaus()
        {
            SqprojProbenErzeuger e = Zonenhaus();
            e.Baujahr = "1950-01-01";
            e.Aufbau(STANDHAUS_AUFBAU, "AW Altbau", STANDHAUS_U)
             .Schicht("LS1", STANDHAUS_AUFBAU, 0, "Vollziegel", 0.36, 0.6, 1800.0, 1.0);
            for (int i = 0; i < STANDHAUS_FLAECHEN.Length; i++)
            {
                (string globalId, int element, string raum) = STANDHAUS_FLAECHEN[i];
                string h = "SH" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                e.Huellflaeche(h, GuidAusGlobalId(globalId), element, STANDHAUS_AUFBAU, STANDHAUS_U, 10.0)
                 .Bezug("SB" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), raum, h, element == 5 ? 9 : 1);
            }
            return e;
        }

        /// <summary>Die GUID (<c>{…}</c>, Kleinbuchstaben) einer komprimierten IFC-<c>GlobalId</c> — die Umkehrung des Abgleichs.</summary>
        internal static string GuidAusGlobalId(string globalId)
        {
            if (globalId is not { Length: 22 }) throw new ArgumentException("GlobalId mit 22 Zeichen erwartet.", nameof(globalId));
            var b = new byte[16];
            b[0] = (byte)Wert(globalId, 0, 2);
            for (int i = 1, k = 2; i < 16; i += 3, k += 4)
            {
                int w = Wert(globalId, k, 4);
                b[i] = (byte)(w >> 16);
                b[i + 1] = (byte)(w >> 8);
                b[i + 2] = (byte)w;
            }
            var s = new StringBuilder(38).Append('{');
            for (int i = 0; i < 16; i++)
            {
                if (i is 4 or 6 or 8 or 10) s.Append('-');
                s.Append(b[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }
            return s.Append('}').ToString();
        }

        private static int Wert(string text, int ab, int stellen)
        {
            int w = 0;
            for (int i = ab; i < ab + stellen; i++)
            {
                int z = GLOBALID_ZEICHEN.IndexOf(text[i], StringComparison.Ordinal);
                if (z < 0) throw new ArgumentException("Kein Zeichen der GlobalId: " + text[i]);
                w = w * 64 + z;
            }
            return w;
        }
    }
}
