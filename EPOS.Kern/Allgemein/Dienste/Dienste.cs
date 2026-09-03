namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der HALTER der neun Umgebungsdienste (Umsetzungskonzept iOS, Paket iU5).
    ///
    /// <para><b>Wozu.</b> Kern-Code — Zugriffsschicht, Controller, Modelle — braucht
    /// gelegentlich die Umgebung: eine Meldung, einen Ablageort, eine Einstellung, die
    /// eingestellte Sprache, eine Maske. Bis iU4 holte er sich das aus <c>Program</c>,
    /// also aus der Klasse mit dem WinForms-Einstiegspunkt. Damit hing der Rechenkern an
    /// der Oberfläche. Hier steht dasselbe hinter neun kleinen Schnittstellen, die unter
    /// Windows von <c>WindowsFormsApplication1\Dienste\*.cs</c> und auf jeder anderen
    /// Plattform von einer eigenen Fassung bedient werden.</para>
    ///
    /// <para><b>Warum ein statischer Halter und kein DI-Container.</b> Das ist das
    /// Hausmuster: <see cref="Meldung"/>, <c>KiTexte.Lieferant</c>,
    /// <c>KiEinwilligung.Nachfragen</c>, <c>KiAusfuehrer.Uhr</c>,
    /// <c>AnlagenEindeutigkeit.Frage</c>, <c>SimulationControl.Speicherlauf</c> — acht
    /// austauschbare Haken tragen den Bestand bereits. Ein Container müsste jedem der
    /// betroffenen Aufrufer eine Instanz reichen; viele davon sind rein statische
    /// Klassen (<c>AnlagenEindeutigkeit</c>, <c>BerichtTexte</c>,
    /// <c>DokuUebersetzung</c>). Der Umbau wäre größer als der Nutzen und würde die
    /// Ergebnisgleichheit stärker gefährden als eine Zuweisung in <c>Program.Main</c>.
    /// Siehe Entscheidungsregister § 2.6.</para>
    ///
    /// <para><b>Die Vorbelegungen schlucken nichts und tun nichts Schädliches.</b> Ohne
    /// Oberfläche — Referenzlauf, Prüfstand, Konsolenwerkzeug — bleibt die
    /// Standardfassung stehen: Dialoge gehen auf die Konsole, eine Dateiwahl liefert
    /// nichts, Einstellungen leben nur im Speicher, Navigation läuft leer. Ein
    /// vergessener Adapter fällt damit als fehlende Wirkung auf, nicht als Absturz.</para>
    ///
    /// <para><b>Wer belegt.</b> Genau eine Stelle: <c>Program.Main</c>, unmittelbar nach
    /// der Auswertung des Feldsicherungsschalters und <b>vor</b>
    /// <c>DataRepository.DatenbankVorhanden()</c> — sonst käme die erste Startmeldung auf
    /// die Konsole statt in einen Dialog.</para>
    ///
    /// <para><b>Prüfstände tauschen ein Feld und setzen es zurück</b> — genau wie bei
    /// <c>AnlagenEindeutigkeit.Frage</c>. Ein <c>null</c> ist dabei nicht vorgesehen; wer
    /// zurücksetzen will, legt die Standardfassung wieder ein.</para>
    /// </summary>
    public static class Dienste
    {
        /// <summary>Meldungen und Rückfragen — 47 Meldungen und 4 Rückfragen im Kernsatz.</summary>
        public static IDialogDienst Dialog { get; set; } = new StilleDialoge();

        /// <summary>Datei- und Ordnerwahl, Öffnen mit der Systemanwendung — 3 Fundstellen.</summary>
        public static IDateiDienst Datei { get; set; } = new KeineDateiwahl();

        /// <summary>Die Ablagewurzeln — 14 Fundstellen in 12 Dateien.</summary>
        public static IPfade Pfade { get; set; } = new StandardPfade();

        /// <summary>Schlüssel-Wert-Ablage — ersetzt Registry (10 Dateien) und die lesenden
        /// <c>Properties.Settings</c>-Zugriffe des Kerns.</summary>
        public static IEinstellungen Einstellungen { get; set; } = new FluechtigeEinstellungen();

        /// <summary>Geheimnisse (Lizenztoken, Zeitanker, KI-Schlüssel) — 10 Fundstellen in 2 Dateien.</summary>
        public static ILizenzAblage Lizenzablage { get; set; } = new KeineAblage();

        /// <summary>Geräte-Identität für die Lizenzbindung — 1 Datei.</summary>
        public static IGeraeteId GeraeteId { get; set; } = new KeineGeraeteId();

        /// <summary>Oberflächensprache — 5 Leser, 1 Setzstelle.</summary>
        public static ISprache Sprache { get; set; } = new StandardSprache();

        /// <summary>Maskenaufruf — 35 <c>Set*Control</c> und 45 <c>ShowDialog</c> im Kernsatz.</summary>
        public static INavigation Navigation { get; set; } = new KeineNavigation();

        /// <summary>Das gerade geöffnete Projekt — 13 Fundstellen.</summary>
        public static IProjektKontext Projekt { get; set; } = new LeererProjektKontext();
    }
}
