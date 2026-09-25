namespace Gebaeudevergleich
{
    /// <summary><c>variante</c> — folgt im nächsten Schritt.</summary>
    internal static class Variante
    {
        internal static int Ausfuehren(Argumente arg, Ausgabe aus)
        {
            aus.Fehler("Abbruch: 'variante' ist in diesem Stand noch nicht gebaut.");
            return Program.ABBRUCH;
        }
    }
}
