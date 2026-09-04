using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die <b>eine</b> Definition der Assistentenseiten (Paket P4).
    ///
    /// <para>
    /// <b>Vorher.</b> Dieselben dreizehn Zeilen standen wortgleich zweimal in
    /// <c>Controller\MenueCtrl.cs</c> — einmal in <c>ProjektNeu()</c>, einmal in
    /// <c>ProjektBearbeiten()</c>. Die beiden Methoden unterschieden sich ausschließlich
    /// im <c>SetWizardMode(...)</c>; jede neue Seite hätte an beiden Stellen gepflegt
    /// werden müssen.
    /// </para>
    /// <para>
    /// <b>Reihenfolge und Inhalt sind unverändert übernommen.</b> Der Index einer Zeile
    /// ist zugleich ihre Kennung aus <see cref="WizardItemClass"/>
    /// (<c>KOMPONENTEN_ITEM</c> = 0 … <c>BHKW_ITEM</c> = 12) — <c>WizardParent</c>
    /// spricht die Liste ausschließlich über diese Konstanten an, deshalb darf hier
    /// nichts umsortiert werden.
    /// </para>
    /// <para>
    /// Die Erzeuger stehen als <see cref="Func{TResult}"/> und nicht als
    /// <see cref="Type"/>-Liste da: So bleibt der Bauplan vom Übersetzer geprüft
    /// (ein umbenanntes oder entferntes Formular bricht den Build), statt erst zur
    /// Laufzeit über <c>Activator.CreateInstance</c> aufzufallen.
    /// </para>
    /// </summary>
    internal static class AssistentSeiten
    {
        private static readonly Func<Form>[] ERZEUGER =
        {
            /* 0  KOMPONENTEN_ITEM   */ () => new Wizard_Komponenten(),
            // iU9-W15a.6: Die Projektkopfseite ist eine Razor-Komponente; sie traegt
            // als einzige eine EINELEMENTIGE geteilte Liste (ProjektKopfDaten) statt
            // eines Get*-Rueckwegs (Befund W15a-B42, Weg (a)).
            /* 1  PROJEKT_ITEM       */ () => ProjektKopfHuelle.AssistentSeite(),
            // iU9-W9.2: Die Gebaeudeseite ist eine Razor-Komponente; die Huelle baut
            // ihre WebView erst in Bestuecken (siehe BlazorAssistentSeite).
            /* 2  GEBAEUDE_ITEM      */ () => GebaeudeHuelle.AssistentSeite(),
            // iU9-W9.4: dieselbe Bauart wie die Gebaeudeseite (W9.2).
            /* 3  WAERMEBEDARF_ITEM  */ () => WaermebedarfExternHuelle.AssistentSeite(),
            // iU9-W9.5: Die beiden Bedarfsseiten sind DIESELBE Razor-Komponente in
            // zwei Auspraegungen (BedarfsArt); die Huelle baut ihre WebView erst in
            // Bestuecken (siehe BlazorAssistentSeite).
            /* 4  PROZESS_ITEM       */ () => BedarfsProfileHuelle.AssistentSeiteProzess(),
            /* 5  STROMSTD_ITEM      */ () => BedarfsProfileHuelle.AssistentSeiteStrom(),
            /* 6  STROMLASTGANG_ITEM */ () => new Wizard_Stromlastgang(),
            // iU9-W7.5: Die WP-Seite ist eine Razor-Komponente; die Huelle baut ihre
            // WebView erst in Bestuecken (siehe BlazorAssistentSeite).
            /* 7  WP_ITEM            */ () => WaermepumpenHuelle.AssistentSeite(),
            // iU9-W7.7: dieselbe Bauart wie die WP-Seite (W7.5).
            /* 8  SOLAR_ITEM         */ () => SolarkollektorHuelle.AssistentSeite(),
            /* 9  PV_ITEM            */ () => PhotovoltaikHuelle.AssistentSeite(),
            /* 10 SP_ITEM            */ () => StromspeicherHuelle.AssistentSeite(),
            // iU9-W6.3: Die Kesselseite ist eine Razor-Komponente; die Huelle baut ihre
            // WebView erst in Bestuecken (siehe BlazorAssistentSeite).
            /* 11 KESSEL_ITEM        */ () => HeizkesselHuelle.AssistentSeite(),
            // iU9-W6.4: dieselbe Bauart wie die Kesselseite (W6.3).
            /* 12 BHKW_ITEM          */ () => BhkwHuelle.AssistentSeite()
        };

        private static readonly ReadOnlyCollection<Type> _typen = new ReadOnlyCollection<Type>(new[]
        {
            typeof(Wizard_Komponenten),
            typeof(BlazorAssistentSeite<EPOS.UI.Seiten.Assistent.ProjektKopfSeite, ProjektKopfDaten>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Bedarf.GebaeudeDialog, Z_ProjGebModel>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Bedarf.WaermebedarfExternDialog,
                                        Z_ProjWaermebedarfModel>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Bedarf.BedarfsProfileDialog,
                                        Z_ProjektProzesswaermeModel>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Bedarf.BedarfsProfileDialog,
                                        Z_ProjektStromverbraucherModel>),
            typeof(Wizard_Stromlastgang),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Waermepumpe.WaermepumpenDialog>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Solarthermie.SolarkollektorenDialog>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Erzeuger.PhotovoltaikDialog>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Erzeuger.StromspeicherDialog>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Erzeuger.HeizkesselDialog>),
            typeof(BlazorAssistentSeite<EPOS.UI.Dialoge.Erzeuger.BhkwDialog>)
        });

        /// <summary>Anzahl der Assistentenseiten (13).</summary>
        public static int Anzahl
        {
            get { return ERZEUGER.Length; }
        }

        /// <summary>
        /// Die Seitentypen in ihrer festen Reihenfolge — <b>eine</b> unveränderliche
        /// Sammlung, die beide Aufrufer in <c>MenueCtrl</c> teilen. Dient dem Nachweis,
        /// dass es die frühere Doppelpflege nicht mehr gibt.
        /// </summary>
        public static ReadOnlyCollection<Type> Seitentypen
        {
            get { return _typen; }
        }

        /// <summary>
        /// Baut die Seitenliste für einen Assistentenlauf: je Zeile ein frisches
        /// Formular. Beide Einstiege (Neu und Bearbeiten) rufen genau diese Methode;
        /// sie unterscheiden sich danach nur noch in <c>SetWizardMode(...)</c>.
        /// </summary>
        public static List<WizardSeite> Erzeugen()
        {
            List<WizardSeite> seiten = new List<WizardSeite>(ERZEUGER.Length);
            for (int i = 0; i < ERZEUGER.Length; i++)
                seiten.Add(new WizardSeite(ERZEUGER[i](), i));
            return seiten;
        }
    }
}
