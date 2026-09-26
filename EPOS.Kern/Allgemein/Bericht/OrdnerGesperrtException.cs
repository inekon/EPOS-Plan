using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Ordner, in den EPOS-Plan nicht schreiben darf</b> — benannt statt der rohen Ausnahme. Windows meldet
    /// „Zugriff verweigert" (<see cref="UnauthorizedAccessException"/>) auch für einen vorhandenen Ordner, wenn der
    /// Überwachte Ordnerzugriff (Ransomware-Schutz) das Programm für „Dokumente" und ähnliche Ordner sperrt; die
    /// Meldung nennt den Ordner und den Weg zur Freigabe. Gilt für den Zielordner des Berichts
    /// (<see cref="BerichtCtrl"/>), den Vorlagenordner und seine Muster (<see cref="BerichtsvorlagenCtrl"/>).
    /// </summary>
    public sealed class OrdnerGesperrtException : UnauthorizedAccessException
    {
        /// <summary>Legt die Ausnahme mit der benannten Meldung an.</summary>
        /// <param name="ordner">Der Ordner, in den nicht geschrieben werden durfte.</param>
        /// <param name="meldung">Die benannte Meldung (<see cref="Zielordner"/> oder <see cref="Vorlagenordner"/>).</param>
        /// <param name="innen">Die Ausnahme des Dateisystems.</param>
        public OrdnerGesperrtException(string ordner, string meldung, Exception innen) : base(meldung, innen)
        {
            Ordner = ordner ?? "";
        }

        /// <summary>Der gesperrte Ordner.</summary>
        public string Ordner { get; }

        /// <summary>Ist <paramref name="ex"/> ein verweigerter Zugriff (und keine andere Ein-/Ausgabestörung)?</summary>
        public static bool IstGesperrt(Exception ex)
        {
            return ex is UnauthorizedAccessException;
        }

        /// <summary>Die Meldung für den Zielordner des Berichts (Einstellungen › Bericht, leer = Dokumente).</summary>
        public static string Zielordner(string ordner)
        {
            return string.Format(MyResource.Resource.BER_ZIELORDNER_GESPERRT, ordner ?? "");
        }

        /// <summary>Die Meldung für den Vorlagenordner und seinen Unterordner der Muster.</summary>
        public static string Vorlagenordner(string ordner)
        {
            return string.Format(MyResource.Resource.BV_VORLAGENORDNER_GESPERRT, ordner ?? "");
        }
    }
}
