using System;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennung einer Quellentität auf höchstens 64 Zeichen</b> — die Länge von
    /// <c>Tab_Importzuordnung.Quellkennung</c> und <c>Tab_Zone/Tab_Bauteil.Quellkennung</c>
    /// (Datenaustauschkonzept 7.2, 7.3). Eine IFC-<c>GlobalId</c> hat 22 Zeichen; eine gbXML-<c>id</c>
    /// ist ein <c>xsd:ID</c> ohne Längengrenze.
    ///
    /// <para><b>Die Regel</b> (3.8, Probe 24): Bis 64 Zeichen bleibt die Kennung unverändert; eine
    /// längere wird auf ihre ersten 56 Zeichen gekürzt und um die ersten acht Hexadezimalzeichen des
    /// SHA-256 der VOLLEN Kennung (UTF-8) ergänzt. Das Ergebnis ist deterministisch, und zwei lange
    /// Kennungen mit gleichem Anfang bleiben unterscheidbar. Den Hinweis im Protokoll legt der Leser
    /// (<c>IMP_GBXML_PROT_KENNUNG_GEKUERZT</c>).</para>
    /// </summary>
    internal static class Quellkennung
    {
        /// <summary>Größte Länge einer gespeicherten Kennung.</summary>
        public const int MAX_LAENGE = 64;

        /// <summary>Länge des behaltenen Anfangs einer gekürzten Kennung.</summary>
        public const int ANFANG = 56;

        /// <summary>Länge des angehängten Hash-Präfixes.</summary>
        public const int HASHPRAEFIX = 8;

        /// <summary>Die Kennung auf höchstens <see cref="MAX_LAENGE"/> Zeichen.</summary>
        public static string Kuerzen(string kennung) => Kuerzen(kennung, out _);

        /// <summary>Die Kennung auf höchstens <see cref="MAX_LAENGE"/> Zeichen; <paramref name="gekuerzt"/> sagt, ob gekürzt wurde.</summary>
        public static string Kuerzen(string kennung, out bool gekuerzt)
        {
            gekuerzt = false;
            if (kennung == null) return "";
            if (kennung.Length <= MAX_LAENGE) return kennung;

            gekuerzt = true;
            // Ein Ersatzzeichenpaar wird nicht zerschnitten: Endet der Anfang auf ein hohes
            // Ersatzzeichen, bleibt er ein Zeichen kürzer (63 statt 64 Zeichen).
            int anfang = char.IsHighSurrogate(kennung[ANFANG - 1]) ? ANFANG - 1 : ANFANG;
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(kennung));
            return kennung.Substring(0, anfang) + Convert.ToHexStringLower(hash).Substring(0, HASHPRAEFIX);
        }
    }
}
