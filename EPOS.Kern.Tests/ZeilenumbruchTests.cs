using System;
using Xunit;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Normalisierung der Zeilenumbrueche aus dem Ressourcenkatalog. Sie muss
    /// IDEMPOTENT sein: Der frueher uebliche <c>Replace("\n", Environment.NewLine)</c>
    /// machte aus einem CRLF ein CR+CRLF und damit je Umbruch eine Leerzeile zu viel.
    /// </summary>
    public class ZeilenumbruchTests
    {
        [Fact]
        public void CRLF_wird_nicht_verdoppelt()
        {
            string erwartet = "Zeile 1" + Environment.NewLine + "Zeile 2";
            Assert.Equal(erwartet, Zeilenumbruch.Normalisieren("Zeile 1\r\nZeile 2"));
        }
    }
}
