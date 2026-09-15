using System;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WIRKSAME Faktorsatz eines Erzeugers samt Herkunft — das Ergebnis von
    /// <see cref="Emissionsquelle"/> (Anwenderentscheid <b>W14a-E-8-B1</b> vom
    /// 07.09.2026).
    ///
    /// <para>Anders als <see cref="EmissionsFaktorSatz"/>, der die Lesekette EINES
    /// Trägers roh abbildet, ist das hier die Zahl, mit der ein Rechenweg
    /// multipliziert: der CO₂-Faktor bereits nach dem Berechnungsmodus aufgelöst
    /// (F7), fehlende Werte als 0 statt <c>null</c>, und daneben der Klartext, aus
    /// welcher Ebene die Zahl stammt.</para>
    /// </summary>
    public sealed class Emissionsfaktoren
    {
        /// <summary>Der Energieträger, aus dem die Faktoren stammen; 0 = keiner
        /// zugeordnet (dann greift der Brennstoff-Rückfall).</summary>
        public int CarrierId;

        /// <summary>Der Berechnungsmodus des Laufs (<c>CO2</c> oder <c>CO2E</c>, F7).</summary>
        public string Modus = DbWerte.EMISSION_MODUS_CO2;

        /// <summary>Der im Modus wirksame CO₂-Faktor [g/kWh]; 0 = nicht gepflegt.</summary>
        public double Co2GKwh;

        /// <summary>Schwefeldioxid [mg/kWh]; 0 = nicht gepflegt.</summary>
        public double So2MgKwh;

        /// <summary>Stickoxide [mg/kWh]; 0 = nicht gepflegt.</summary>
        public double NoxMgKwh;

        /// <summary>Gesamtstaub [mg/kWh]; 0 = nicht gepflegt.</summary>
        public double StaubMgKwh;

        /// <summary>
        /// Kohlenmonoxid [mg/kWh] — <b>heute immer 0</b>. Der Emissionsarten-Katalog
        /// führt keine Art <c>CO</c> (sieben Arten: CO₂, SO₂, NOx, CH₄ fossil, CH₄
        /// biogen, N₂O, Staub), und <c>Tab_Brennstoff_Stamm</c> hat keine CO-Spalte.
        /// Das Feld steht hier, damit der Rechenweg unverändert bleibt, sobald jemand
        /// die Art anlegt — nicht als Platzhalter für eine Zahl, die es gäbe.
        /// </summary>
        public double CoMgKwh;

        /// <summary>true, wenn wenigstens ein CO₂-Faktor gefunden wurde.</summary>
        public bool Co2Gepflegt;

        /// <summary>Ebene, aus der der CO₂-Wert stammt: <c>PROJEKT</c>, <c>KATALOG</c>,
        /// <c>STAMM</c>, <c>CARRIER</c>, <c>BRENNSTOFF</c> oder <c>-</c>.</summary>
        public string Ebene = "-";

        /// <summary>Die Herkunft in Worten — für Laufprotokoll und Bericht.</summary>
        public string Herkunft = "";

        /// <summary>
        /// <b>Der Faktor ist GELIEHEN</b>: <c>energy_carrier.id</c> des Trägers, der
        /// eingesprungen ist, weil dem Projekt keiner zugeordnet war; <c>0</c> = kein
        /// Rückfall, die Zahl steht auf eigenen Füßen.
        ///
        /// <para>Gesetzt wird das Feld allein von <see cref="Emissionsquelle.Netzstrom(int,int,string)"/>
        /// (Anwenderentscheid 15.09.2026). Es trägt die HERLEITUNG: Der Anwender soll
        /// einen geliehenen Wert von einem gepflegten unterscheiden können, und
        /// <see cref="Herkunft"/> sagt es in Worten.</para>
        /// </summary>
        public int RueckfallTraegerId;
    }

    /// <summary>
    /// <b>DIE EINE Emissionsquelle aller Erzeuger</b> (Anwenderentscheid
    /// <b>W14a-E-8-B1</b> vom 07.09.2026).
    ///
    /// <para><b>Der Entscheid im Wortlaut:</b> „Da CO₂, SO₂, NOₓ, CO und Staub in
    /// g/MWh kein CO₂-Äquivalent haben, sind diese Zahlen informativ. […] Es soll der
    /// gepflegte CO₂-Wert herangezogen werden — der an dem Energieträger hängt (gilt
    /// generell für alle Erzeuger!). Es sollte dazu eine Emissionsdatenbank geben
    /// (siehe Konzept)."</para>
    ///
    /// <para><b>Was vorher war.</b> Drei Rechenwege, drei Quellen: Die
    /// Wirtschaftlichkeit (<see cref="EmissionsBilanzRechner"/>,
    /// <see cref="KostenEmissionRechner"/>) las den Emissionskatalog über
    /// <see cref="EmissionsFaktorLader"/>; die Simulation las beim <b>Kessel</b> die
    /// alte Brennstofftabelle (<c>SimulationSPK.Kesseldaten_Einlesen</c>) und beim
    /// <b>BHKW</b> die fünf Gerätespalten des Katalogs
    /// (<c>SimulationBHKW.Moduldaten_Einlesen</c>). Dieselbe Anlage konnte damit im
    /// Simulationsprotokoll und in der Emissionsbilanz zwei verschiedene Zahlen
    /// tragen. Seit B1 gibt es EINE Quelle, und die fünf Gerätespalten von Kessel und
    /// BHKW sind <c>Verwendung.Dialog</c> („nur Anzeige",
    /// <see cref="ParameterVerwendung"/>).</para>
    ///
    /// <para><b>Die Lesekette</b> ist die des Konzepts
    /// (<c>Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md</c> § 3), unverändert
    /// in <see cref="EmissionsFaktorLader"/>: Projektwert →  aktive
    /// <c>emissionswert</c>-Zeile →  <c>Tab_Brennstoff_Stamm</c> →  Altspalte
    /// <c>energy_carrier</c>. Sie setzt einen ZUGEORDNETEN Energieträger voraus.</para>
    ///
    /// <para><b>Das fünfte Glied — der Brennstoff-Rückfall.</b> Eine Anlage OHNE
    /// <c>Tab_Energieanlagen.ID_Carrier</c> hat keinen Träger, über den sich der
    /// Katalog erreichen ließe; im Bestand ist das der Regelfall älterer Projekte
    /// (in der Testdatenbank fünf von zwölf). Statt einer stillen 0 gilt dann der
    /// Brennstoff des GERÄTS gegen dieselbe Tabelle, in der die Kette ohnehin endet:
    /// <c>Tab_Brennstoff_Stamm</c>. Das ist keine zweite Wahrheit — es ist dieselbe
    /// Zeile, nur über einen anderen Schlüssel erreicht —, und es ist genau das
    /// Verhalten, das der Kessel bis hierher hatte. Der Lauf sagt es im Protokoll an,
    /// damit die fehlende Zuordnung sichtbar bleibt statt bequem zu sein.</para>
    ///
    /// <para><b>Kein Zwischenspeicher</b> — aus demselben Grund wie in
    /// <see cref="EmissionsFaktorLader"/>: Je Lauf gibt es eine Handvoll Erzeuger,
    /// und ein prozessweiter Cache über im Katalog pflegbare Zahlen wäre nach der
    /// ersten Änderung falsch.</para>
    /// </summary>
    public static class Emissionsquelle
    {
        /// <summary>Herkunftsebene: Brennstoff des Geräts gegen
        /// <c>Tab_Brennstoff_Stamm</c> (Anlage ohne Energieträger).</summary>
        public const string EBENE_BRENNSTOFF = "BRENNSTOFF";

        /// <summary>
        /// CO₂-Faktor des Netzstroms [g/kWh], wenn dem Projekt KEIN Stromträger
        /// zugeordnet ist — BAFA, Informationsblatt CO₂-Faktoren EEW, Zeile „El. Strom
        /// (Effizienzmaßnahme)": 0,435 tCO₂/MWh (Nutzerentscheid 29.08.2026).
        ///
        /// <para>Er ist der Rückfall von <see cref="KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH"/>
        /// UND — seit W11a-O-2 — der Autarkie-Kachel: Bis dahin rechnete die Kachel mit
        /// einem eigenen Literal 0,42 kg/kWh, das mit keiner anderen Zahl des Hauses
        /// abgestimmt war.</para>
        /// </summary>
        public const double NETZSTROM_RUECKFALL_G_JE_KWH = 435.0;

        /// <summary>
        /// CO₂-Faktor der verdrängten WÄRME [g/kWh], wenn das Projekt keinen
        /// Wärmeerzeuger mit Energieträger führt. 200 g/kWh ist der bisherige
        /// Kachel-Literalwert (0,20 kg/kWh aus <c>DashboardForm.cs:355</c>) und liegt
        /// beim BAFA-Erdgasfaktor 201 g/kWh — er bleibt als Rückfall stehen, damit ein
        /// Projekt ohne Wärmeerzeuger dieselbe Zahl zeigt wie bisher.
        /// </summary>
        public const double WAERME_RUECKFALL_G_JE_KWH = 200.0;

        // =====================================================================
        //  Modus
        // =====================================================================

        /// <summary>
        /// Der Berechnungsmodus des Projekts (F7) — EINMAL je Rechenlauf lesen und
        /// weiterreichen, nicht je Erzeuger.
        /// </summary>
        public static string Modus(int idProjekt)
        {
            try { return EmissionenCtrl.ModusFuerRechenlauf(idProjekt); }
            catch { return DbWerte.EMISSION_MODUS_CO2; }
        }

        // =====================================================================
        //  Der Faktorsatz eines Erzeugers
        // =====================================================================

        /// <summary>
        /// Die Faktoren EINES Erzeugers. <paramref name="carrierId"/> ist der
        /// zugeordnete Energieträger (<c>Tab_Energieanlagen.ID_Carrier</c>),
        /// <paramref name="idBrennstoff"/> der Brennstoff des Geräts als Rückfall,
        /// wenn kein Träger zugeordnet ist.
        /// </summary>
        public static Emissionsfaktoren Fuer(int idProjekt, int carrierId,
                                             int idBrennstoff, string modus)
        {
            var f = new Emissionsfaktoren { CarrierId = carrierId };
            f.Modus = string.IsNullOrWhiteSpace(modus) ? DbWerte.EMISSION_MODUS_CO2 : modus;

            if (carrierId > 0)
            {
                EmissionsFaktorSatz satz = EmissionsFaktorLader.Lade(idProjekt, carrierId);
                double? co2 = satz.Wirksam(f.Modus);

                f.Co2GKwh = co2 ?? 0.0;
                f.Co2Gepflegt = co2.HasValue;
                f.So2MgKwh = satz.So2 ?? 0.0;
                f.NoxMgKwh = satz.Nox ?? 0.0;
                f.StaubMgKwh = satz.Staub ?? 0.0;
                f.Ebene = satz.Co2Ebene;
                f.Herkunft = HerkunftstextTraeger(carrierId, satz.Co2Ebene);

                // Ein zugeordneter Träger OHNE jeden CO2-Wert ist eine Datenlücke, keine
                // Aufforderung zum Rückfall: Die Kette hat vier Ebenen durchsucht,
                // Tab_Brennstoff_Stamm eingeschlossen. Ein zweiter Anlauf über den
                // Gerätebrennstoff verdeckte die Lücke nur.
                return f;
            }

            // Kein Energieträger zugeordnet - der Brennstoff des Geräts gegen dieselbe
            // Tabelle, in der die Kette ohnehin endet.
            Brennstoffwerte b = Brennstoff(idBrennstoff);
            f.Co2GKwh = b.Co2 ?? 0.0;
            f.Co2Gepflegt = b.Co2.HasValue;
            f.So2MgKwh = b.So2 ?? 0.0;
            f.NoxMgKwh = b.Nox ?? 0.0;
            f.StaubMgKwh = b.Staub ?? 0.0;
            f.Ebene = b.Co2.HasValue ? EBENE_BRENNSTOFF : "-";
            f.Herkunft = b.Co2.HasValue
                ? "Brennstoffkatalog „" + b.Name + "“ (kein Energieträger zugeordnet)"
                : "kein Emissionsfaktor gepflegt";
            return f;
        }

        // =====================================================================
        //  Netzstrom und Wärme - die zwei Bezugsgrößen der Autarkie-Kachel
        // =====================================================================

        /// <summary>
        /// Der CO₂-Faktor des NETZSTROMS eines Projekts [g/kWh] — siehe die
        /// Stufenfolge in <see cref="Netzstrom(int,int,string)"/>.
        /// </summary>
        public static Emissionsfaktoren Netzstrom(int idProjekt, string modus)
        {
            return Netzstrom(idProjekt, StromTraeger(idProjekt), modus);
        }

        /// <summary>
        /// Der CO₂-Faktor des NETZSTROMS [g/kWh] mit bereits bestimmtem Stromträger —
        /// für Aufrufer, die ihn ohnehin schon gelesen haben (spart die zweite
        /// Abfrage).
        ///
        /// <para><b>Die Stufen, in dieser Reihenfolge:</b></para>
        /// <list type="number">
        ///   <item><description>der Faktor des ZUGEORDNETEN Stromträgers
        ///     (<c>pricing_model = 'ELECTRICITY'</c>) über die Lesekette;</description></item>
        ///   <item><description>sonst — und nur, wenn gar keiner zugeordnet ist — der
        ///     des Auslieferungsträgers des Katalogs
        ///     (<see cref="KatalogStromTraeger"/>);</description></item>
        ///   <item><description>sonst <see cref="NETZSTROM_RUECKFALL_G_JE_KWH"/>.</description></item>
        /// </list>
        ///
        /// <para><b>Stufe 2 FÜLLT NUR LÜCKEN</b> (Anwenderentscheid 15.09.2026:
        /// „bereits zugewiesene CO₂-Zahlen nicht überschreiben"). Sie greift allein
        /// dort, wo <b>kein</b> Stromträger zugeordnet ist — wo also gar keine Zahl aus
        /// den Projektdaten steht, sondern der anonyme Vorgabewert. Ein ZUGEORDNETER
        /// Träger behält seinen Faktor, auch wenn er keinen trägt: Dann bleibt es beim
        /// Vorgabewert wie bisher, denn seine Lesekette hat vier Ebenen durchsucht, und
        /// ein zweiter Anlauf über einen fremden Träger verdeckte die Lücke, statt sie
        /// zu füllen — dieselbe Regel, die <see cref="Fuer"/> für den
        /// Brennstoff-Rückfall trägt.</para>
        ///
        /// <para><b>Derselbe Träger wie auf der KOSTENseite:</b>
        /// <see cref="KostenEmissionRechner"/> bepreist den Netzbezug seit Auftrag #267
        /// mit genau dieser Wahl. Bis hierher fragten Kosten und Emissionen
        /// verschieden — dieselbe Lage, zwei Antworten.</para>
        /// </summary>
        public static Emissionsfaktoren Netzstrom(int idProjekt, int stromTraeger, string modus)
        {
            Emissionsfaktoren f = Fuer(idProjekt, stromTraeger, 0, modus);
            if (f.Co2GKwh > 0) return f;

            if (stromTraeger <= 0)
            {
                int rueckfall = KatalogStromTraeger(idProjekt);
                if (rueckfall > 0)
                {
                    Emissionsfaktoren r = Fuer(idProjekt, rueckfall, 0, modus);
                    if (r.Co2GKwh > 0)
                    {
                        r.RueckfallTraegerId = rueckfall;
                        r.Herkunft = HerkunftstextTraeger(rueckfall, r.Ebene) +
                                     " — dem Projekt ist kein Stromträger zugeordnet";
                        return r;
                    }
                }
            }

            f.Co2GKwh = NETZSTROM_RUECKFALL_G_JE_KWH;
            f.Co2Gepflegt = false;
            f.Ebene = "-";
            f.Herkunft = "Vorgabewert Strommix (BAFA EEW, El. Strom Effizienzmaßnahme)";
            return f;
        }

        /// <summary>
        /// Der CO₂-Faktor der verdrängten WÄRME eines Projekts [g/kWh]: der Faktor des
        /// Energieträgers seines ersten Wärmeerzeugers (Kessel vor BHKW), sonst
        /// <see cref="WAERME_RUECKFALL_G_JE_KWH"/>.
        ///
        /// <para>Bewusst OHNE Kesselwirkungsgrad: Die Kachel rechnete bisher
        /// <c>genutzte Solarwärme × 0,20</c>, also 1 kWh Wärme gegen 1 kWh Brennstoff.
        /// Ein Wirkungsgrad im Nenner wäre fachlich richtiger, änderte aber die
        /// Kennzahl über den Faktortausch hinaus — das ist eine eigene Entscheidung
        /// und keine Beifracht von W11a-O-2.</para>
        /// </summary>
        public static Emissionsfaktoren Waerme(int idProjekt, string modus)
        {
            int carrier = WaermeTraeger(idProjekt);
            Emissionsfaktoren f = Fuer(idProjekt, carrier, 0, modus);
            if (f.Co2GKwh > 0) return f;

            f.Co2GKwh = WAERME_RUECKFALL_G_JE_KWH;
            f.Co2Gepflegt = false;
            f.Ebene = "-";
            f.Herkunft = "Vorgabewert Wärme (kein Wärmeerzeuger mit Energieträger)";
            return f;
        }

        /// <summary>
        /// Der Stromträger des Projekts (<c>pricing_model = 'ELECTRICITY'</c>); 0 =
        /// keiner zugeordnet. Eine Fassung für <see cref="KostenEmissionRechner"/>
        /// und die Autarkie-Kachel.
        /// </summary>
        public static int StromTraeger(int idProjekt)
        {
            if (idProjekt <= 0) return 0;

            // ET-5 (08.09.2026): dieselbe Wahl wie StromAufschlagCtrl.StromCarrierId - der an
            // der Anlage gewaehlte, dem Projekt zugeordnete Stromtraeger. Ohne Anlagenwahl
            // (aller Bestand vor ET-5) bleibt der Weg darunter unveraendert.
            try
            {
                int gewaehlt = ProjektEnergietraegerCtrl.StromTraegerDerAnlagen(idProjekt);
                if (gewaehlt > 0) return gewaehlt;
            }
            catch { }

            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ec.id FROM energy_project_settings AS s " +
                    "INNER JOIN energy_carrier AS ec ON s.[ID_Energieträger] = ec.id " +
                    "WHERE s.ID_Projekt = ? AND ec.pricing_model = 'ELECTRICITY' LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            catch { }
            return 0;
        }

        /// <summary>
        /// <b>Der Stromträger, der einspringt, wenn dem Projekt keiner zugeordnet
        /// ist</b> — der Auslieferungsträger des Katalogs
        /// (<see cref="ProjektEnergietraegerCtrl.StandardStromTraeger"/>), also genau
        /// die Fassung, die die Kostenseite ANZEIGT und der Assistent ZUORDNET.
        /// <c>0</c> = das Projekt führt keine elektrische Erzeugung, oder der Katalog
        /// führt keinen Stromträger.
        ///
        /// <para><b>Nur für Projekte mit elektrischer Welt.</b> Ein reines
        /// Kesselprojekt ohne Wärmepumpe, PV, Stromspeicher oder Heizstab bekommt
        /// keinen Rückfall: Sein Netzbezug ist Haushaltsstrom der Bedarfsseite und
        /// keine Anlagengröße, und ein Träger, den niemand zugeordnet hat, wäre dort
        /// eine Erfindung.</para>
        ///
        /// <para><b>EINE Fassung für Kosten und Emissionen.</b>
        /// <see cref="KostenEmissionRechner"/> bepreist den Netzbezug seit Auftrag #267
        /// mit demselben Träger und liest ihn hier; <see cref="Netzstrom(int,int,string)"/>
        /// holt sich von hier den Faktor. Zwei Fassungen wären zwei Antworten auf
        /// dieselbe Frage.</para>
        /// </summary>
        internal static int KatalogStromTraeger(int idProjekt)
        {
            if (idProjekt <= 0) return 0;
            try
            {
                if (!ProjektEnergietraegerCtrl.BrauchtStromTraeger(idProjekt)) return 0;
                return ProjektEnergietraegerCtrl.StandardStromTraeger(idProjekt);
            }
            catch { return 0; }
        }

        /// <summary>
        /// Der Energieträger des ersten WÄRMEERZEUGERS des Projekts — Kessel
        /// (<c>ID_Type</c> 10) vor BHKW (11), weil die Solarwärme in aller Regel
        /// Kesselwärme verdrängt. 0 = keiner zugeordnet.
        /// </summary>
        public static int WaermeTraeger(int idProjekt)
        {
            if (idProjekt <= 0) return 0;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ID_Carrier FROM Tab_Energieanlagen " +
                    "WHERE ID_Projekt = ? AND ID_Type IN (?, ?) AND ID_Carrier > 0 " +
                    "ORDER BY ID_Type, ID LIMIT 1",
                    new DbParam("@p", idProjekt),
                    new DbParam("@t1", WizardItemClass.KESSEL_TYP),
                    new DbParam("@t2", WizardItemClass.BHKW_TYP));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            catch { }
            return 0;
        }

        // =====================================================================
        //  Herkunft in Worten
        // =====================================================================

        /// <summary>Der Klartext zur Herkunftsebene eines Trägerfaktors.</summary>
        private static string HerkunftstextTraeger(int carrierId, string ebene)
        {
            string name = TraegerName(carrierId);
            string quelle;
            switch (ebene ?? "")
            {
                case EmissionsFaktorLader.EBENE_PROJEKT: quelle = "Projektwert"; break;
                case EmissionsFaktorLader.EBENE_KATALOG: quelle = "Emissionskatalog"; break;
                case EmissionsFaktorLader.EBENE_STAMM: quelle = "Brennstoffkatalog"; break;
                case EmissionsFaktorLader.EBENE_CARRIER: quelle = "Energieträgerspalte"; break;
                default: quelle = "kein Emissionsfaktor gepflegt"; break;
            }
            return "Energieträger „" + name + "“ (" + quelle + ")";
        }

        /// <summary>Der Anzeigename eines Energieträgers (<c>energy_carrier.name</c>);
        /// „—" ohne Träger, „ID n" wenn die Katalogzeile fehlt.
        /// <para>Seit Auftrag #267 <c>internal</c>: Der
        /// <see cref="KostenEmissionRechner"/> nennt seine Fehlgründe beim
        /// Trägernamen und braucht dieselbe Auskunft — eine zweite Abfrage wäre eine
        /// zweite Schreibweise desselben Namens.</para></summary>
        internal static string TraegerName(int carrierId)
        {
            if (carrierId <= 0) return "—";
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT [name] FROM energy_carrier WHERE id = ?",
                    new DbParam("@c", carrierId));
                if (o != null && o != DBNull.Value) return Convert.ToString(o);
            }
            catch { }
            return "ID " + carrierId;
        }

        // =====================================================================
        //  Brennstoff-Rückfall
        // =====================================================================

        private sealed class Brennstoffwerte
        {
            public string Name = "—";
            public double? Co2;     // g/kWh
            public double? So2;     // mg/kWh
            public double? Nox;     // mg/kWh
            public double? Staub;   // mg/kWh
        }

        /// <summary>Die Faktoren eines Brennstoffs aus <c>Tab_Brennstoff_Stamm</c> —
        /// dieselbe Tabelle, die auch die Ebene <c>STAMM</c> der Lesekette liest,
        /// nur über die Brennstoff-ID des Geräts statt über den Träger.</summary>
        private static Brennstoffwerte Brennstoff(int idBrennstoff)
        {
            var b = new Brennstoffwerte();
            if (idBrennstoff <= 0) return b;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT Bezeichner, CO2, SO2, NOx, Staub FROM Tab_Brennstoff_Stamm WHERE ID = ?",
                    new DbParam("@id", idBrennstoff));
                if (dt == null || dt.Rows.Count == 0) return b;

                DataRow r = dt.Rows[0];
                if (r.Table.Columns.Contains("Bezeichner") && r["Bezeichner"] != DBNull.Value)
                    b.Name = Convert.ToString(r["Bezeichner"]);
                b.Co2 = Gepflegt(r, "CO2");
                b.So2 = Gepflegt(r, "SO2");
                b.Nox = Gepflegt(r, "NOx");
                b.Staub = Gepflegt(r, "Staub");
            }
            catch { }
            return b;
        }

        /// <summary>„Gepflegt" heißt größer als 0 — dieselbe Regel wie in
        /// <see cref="EmissionsFaktorLader"/>.</summary>
        private static double? Gepflegt(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try
            {
                double w = Convert.ToDouble(r[spalte]);
                return w > 0 ? (double?)w : null;
            }
            catch { return null; }
        }
    }
}
