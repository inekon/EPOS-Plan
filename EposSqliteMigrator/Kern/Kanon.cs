using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace EposSqliteMigrator.Kern;

/// <summary>
/// Kanonisierung fuer den Datenbeweis. Quelle (OleDb/ACE) und Ziel (SQLite) werden
/// mit denselben Regeln in Text ueberfuehrt; nur so ist der Vergleich der Pruefsummen
/// ein Beweis und keine Zufallsuebereinstimmung.
/// Die Sonderzeichen stehen bewusst als Fluchtform da, damit die Datei unabhaengig
/// von ihrer Codierung dasselbe bedeutet.
/// </summary>
public static class Kanon
{
    /// <summary>Markierung fuer NULL: Paragraphenzeichen U+00A7 gefolgt von der Ziffer 0.</summary>
    public const string NullMarke = "§" + "0";

    /// <summary>Feldtrenner U+001F (UNIT SEPARATOR) - in Nutzdaten nicht zu erwarten.</summary>
    public const char Trenner = '';

    /// <summary>Datumsformat fuer Quelle und Ziel (ISO-nah, sekundengenau).</summary>
    public const string DatumFormat = "yyyy-MM-dd HH:mm:ss";

    public static string Ganzzahl(long v) => v.ToString(CultureInfo.InvariantCulture);

    public static string Real(double v) => v.ToString("R", CultureInfo.InvariantCulture);

    public static string Datum(DateTime v) => v.ToString(DatumFormat, CultureInfo.InvariantCulture);
}

/// <summary>
/// Reihenfolgeunabhaengige Inhaltspruefsumme einer Tabelle.
/// Je Zeile SHA-256 ueber die mit U+001F verbundenen kanonischen Spaltenwerte;
/// von jedem Zeilen-Hash werden die unteren (letzten) 16 Bytes als vorzeichenlose
/// 128-Bit-Zahl (Big-Endian) gelesen und modulo 2^128 aufaddiert. Addition ist
/// kommutativ - die Zeilenreihenfolge spielt damit keine Rolle.
/// </summary>
public sealed class PruefsummeAggregat
{
    private static readonly BigInteger Modul = BigInteger.One << 128;

    private BigInteger _summe = BigInteger.Zero;
    private long _zeilen;

    private readonly StringBuilder _puffer = new(512);
    private byte[] _bytes = new byte[1024];

    public long Zeilen => _zeilen;

    /// <summary>Beginnt eine neue Zeile.</summary>
    public void ZeileBeginnen() => _puffer.Clear();

    /// <summary>Haengt einen bereits kanonisierten Feldwert an.</summary>
    public void Feld(string kanonisch)
    {
        if (_puffer.Length > 0) _puffer.Append(Kanon.Trenner);
        _puffer.Append(kanonisch);
    }

    /// <summary>Schliesst die Zeile ab und verrechnet ihren Hash.</summary>
    public void ZeileAbschliessen()
    {
        int max = Encoding.UTF8.GetMaxByteCount(_puffer.Length) + 4;
        if (_bytes.Length < max) _bytes = new byte[Math.Max(max, _bytes.Length * 2)];

        int laenge = 0;
        foreach (var stueck in _puffer.GetChunks())
            laenge += Encoding.UTF8.GetBytes(stueck.Span, _bytes.AsSpan(laenge));

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(_bytes.AsSpan(0, laenge), hash);

        var teil = new BigInteger(hash[16..32], isUnsigned: true, isBigEndian: true);
        _summe = (_summe + teil) % Modul;
        _zeilen++;
    }

    /// <summary>Pruefsumme als 32 Hexziffern (Grossbuchstaben).</summary>
    public string Hex()
    {
        var roh = _summe.ToByteArray(isUnsigned: true, isBigEndian: true);
        Span<byte> feld = stackalloc byte[16];
        feld.Clear();
        int quelle = Math.Max(0, roh.Length - 16);
        int ziel = 16 - (roh.Length - quelle);
        roh.AsSpan(quelle).CopyTo(feld[ziel..]);
        return Convert.ToHexString(feld);
    }
}
