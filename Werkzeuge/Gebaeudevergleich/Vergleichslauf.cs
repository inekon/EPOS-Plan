namespace Gebaeudevergleich
{
    /// <summary><c>vergleich</c> — folgt im nächsten Schritt.</summary>
    internal static class Vergleichslauf
    {
        internal static int Ausfuehren(Argumente arg, Ausgabe aus)
        {
            aus.Fehler("Abbruch: 'vergleich' ist in diesem Stand noch nicht gebaut.");
            return Program.ABBRUCH;
        }
    }
}
