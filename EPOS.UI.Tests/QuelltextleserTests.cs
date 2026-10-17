using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Wache des <see cref="Quelltextleser"/>: Derselbe Text liefert mit LF, CRLF und einzelnem CR
/// dasselbe — so sehen die Stilblatt-Tests im Windows-Klon (<c>core.autocrlf</c>) dasselbe wie
/// auf Linux.
/// </summary>
public class QuelltextleserTests
{
    private const string MitLf = "@media (forced-colors: active) {\n    .epos-zeilenstift { outline: 1px solid; }\n}\n";

    [Fact]
    public void Crlf_und_Cr_werden_wie_Lf_geliefert()
    {
        Assert.Equal(MitLf, Quelltextleser.Normalisiert(MitLf.Replace("\n", "\r\n", StringComparison.Ordinal)));
        Assert.Equal(MitLf, Quelltextleser.Normalisiert(MitLf.Replace('\n', '\r')));
        Assert.Equal(MitLf, Quelltextleser.Normalisiert(MitLf));
    }

    [Fact]
    public void Eine_Crlf_Datei_wird_wie_die_Lf_Datei_gelesen()
    {
        string ordner = Path.Combine(Path.GetTempPath(), "Quelltextleser_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ordner);
        try
        {
            string lf = Path.Combine(ordner, "lf.css");
            string crlf = Path.Combine(ordner, "crlf.css");
            File.WriteAllText(lf, MitLf);
            File.WriteAllText(crlf, MitLf.Replace("\n", "\r\n", StringComparison.Ordinal));

            Assert.Contains("\r\n", File.ReadAllText(crlf), StringComparison.Ordinal);   // Gegenprobe: roh steht CRLF
            Assert.Equal(Quelltextleser.Lesen(lf), Quelltextleser.Lesen(crlf));
            Assert.Contains("{\n    .epos-zeilenstift", Quelltextleser.Lesen(crlf), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(ordner, recursive: true);
        }
    }

    [Fact]
    public void Das_Hausblatt_kommt_ohne_Wagenruecklauf()
    {
        string css = Quelltextleser.Stilblatt();
        Assert.False(string.IsNullOrEmpty(css));
        Assert.DoesNotContain("\r", css, StringComparison.Ordinal);
    }
}
