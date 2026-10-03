using System;
using System.Globalization;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Pufferspeicher;

/// <summary>
/// Die Anzeigetexte der PUFFERSPEICHER-AUSLEGUNG — ein Bündel statt hundertfünfzig einzelner
/// Parameter (Hausregel „Texte ab zehn als Bündel"). Es füllt sich selbst aus <c>MyResource</c>
/// in der Oberflächensprache; jede Eigenschaft trägt ihren Ressourcenschlüssel im Kommentar.
/// Texte, die je Wert wechseln (Vorlagen, Kriterien, Zonen, Übergabearten, Sperrprofile),
/// liefert <see cref="Nach"/> über ihren Schlüssel — mit dem Wert selbst als Rückfall.
/// </summary>
public sealed class PufferAuslegungTexte
{
    /// <summary>„Pufferspeicher-Auslegung" (<c>PAUS_TITEL</c>).</summary>
    public string Titel { get; set; } = Resource.PAUS_TITEL;

    /// <summary>„← zurück" (<c>PAUS_ZURUECK</c>).</summary>
    public string Zurueck { get; set; } = Resource.PAUS_ZURUECK;

    /// <summary>„Ablauf der Auslegung" (<c>PAUS_ABLAUF</c>).</summary>
    public string Ablauf { get; set; } = Resource.PAUS_ABLAUF;

    /// <summary>„Klasse und Erzeuger" (<c>PAUS_SCHRITT_ANLAGE</c>).</summary>
    public string SchrittAnlage { get; set; } = Resource.PAUS_SCHRITT_ANLAGE;

    /// <summary>„Bedarf und Randbedingungen" (<c>PAUS_SCHRITT_BEDARF</c>).</summary>
    public string SchrittBedarf { get; set; } = Resource.PAUS_SCHRITT_BEDARF;

    /// <summary>„Kriterien" (<c>PAUS_SCHRITT_KRITERIEN</c>).</summary>
    public string SchrittKriterien { get; set; } = Resource.PAUS_SCHRITT_KRITERIEN;

    /// <summary>„Ergebnis und Übernahme" (<c>PAUS_SCHRITT_ERGEBNIS</c>).</summary>
    public string SchrittErgebnis { get; set; } = Resource.PAUS_SCHRITT_ERGEBNIS;

    /// <summary>„veraltet" (<c>PAUS_VERALTET</c>).</summary>
    public string Veraltet { get; set; } = Resource.PAUS_VERALTET;

    /// <summary>„Tiefe" (<c>PAUS_STUFE</c>).</summary>
    public string Stufe { get; set; } = Resource.PAUS_STUFE;

    /// <summary>„Schnell" (<c>PAUS_STUFE_SCHNELL</c>).</summary>
    public string StufeSchnell { get; set; } = Resource.PAUS_STUFE_SCHNELL;

    /// <summary>„Standard" (<c>PAUS_STUFE_STANDARD</c>).</summary>
    public string StufeStandard { get; set; } = Resource.PAUS_STUFE_STANDARD;

    /// <summary>„Experte" (<c>PAUS_STUFE_EXPERTE</c>).</summary>
    public string StufeExperte { get; set; } = Resource.PAUS_STUFE_EXPERTE;

    /// <summary>„Projekt „{0}“ · Speicher „{1}“" (<c>PAUS_KONTEXT_PUFFER</c>).</summary>
    public string KontextPuffer { get; set; } = Resource.PAUS_KONTEXT_PUFFER;

    /// <summary>„Projekt „{0}“ · neuer Pufferspeicher" (<c>PAUS_KONTEXT_NEU</c>).</summary>
    public string KontextNeu { get; set; } = Resource.PAUS_KONTEXT_NEU;

    /// <summary>„Die gespeicherte Auslegung dieses Speichers ist geladen." (<c>PAUS_GESPEICHERTE_AUSLEGUNG</c>).</summary>
    public string GespeicherteAuslegung { get; set; } = Resource.PAUS_GESPEICHERTE_AUSLEGUNG;

    /// <summary>„Weiter ›" (<c>PAUS_WEITER</c>).</summary>
    public string Weiter { get; set; } = Resource.PAUS_WEITER;

    /// <summary>„‹ Zurück" (<c>PAUS_ZURUECK_SCHRITT</c>).</summary>
    public string ZurueckSchritt { get; set; } = Resource.PAUS_ZURUECK_SCHRITT;

    /// <summary>„Auslegen und Ergebnis zeigen" (<c>PAUS_RECHNEN</c>).</summary>
    public string Rechnen { get; set; } = Resource.PAUS_RECHNEN;

    /// <summary>„Neu rechnen" (<c>PAUS_NEU_RECHNEN</c>).</summary>
    public string NeuRechnen { get; set; } = Resource.PAUS_NEU_RECHNEN;

    /// <summary>„Auf Vorbelegung zurücksetzen" (<c>PAUS_ZURUECKSETZEN</c>).</summary>
    public string Zuruecksetzen { get; set; } = Resource.PAUS_ZURUECKSETZEN;

    /// <summary>„Eingaben geändert — das Ergebnis rechnet beim Wechsel in Schritt 4 oder mit „Neu rechnen“ nach." (<c>PAUS_VERALTET_HINWEIS</c>).</summary>
    public string VeraltetHinweis { get; set; } = Resource.PAUS_VERALTET_HINWEIS;

    /// <summary>„Projekt" (<c>PAUS_QUELLE_PROJEKT</c>).</summary>
    public string QuelleProjekt { get; set; } = Resource.PAUS_QUELLE_PROJEKT;

    /// <summary>„Puffer" (<c>PAUS_QUELLE_PUFFER</c>).</summary>
    public string QuellePuffer { get; set; } = Resource.PAUS_QUELLE_PUFFER;

    /// <summary>„Kaskade" (<c>PAUS_QUELLE_KASKADE</c>).</summary>
    public string QuelleKaskade { get; set; } = Resource.PAUS_QUELLE_KASKADE;

    /// <summary>„Gebäude" (<c>PAUS_QUELLE_GEBAEUDE</c>).</summary>
    public string QuelleGebaeude { get; set; } = Resource.PAUS_QUELLE_GEBAEUDE;

    /// <summary>„Zapfprofil" (<c>PAUS_QUELLE_ZAPFPROFIL</c>).</summary>
    public string QuelleZapfprofil { get; set; } = Resource.PAUS_QUELLE_ZAPFPROFIL;

    /// <summary>„Bedarfsreihe" (<c>PAUS_QUELLE_REIHE</c>).</summary>
    public string QuelleReihe { get; set; } = Resource.PAUS_QUELLE_REIHE;

    /// <summary>„Katalog" (<c>PAUS_QUELLE_KATALOG</c>).</summary>
    public string QuelleKatalog { get; set; } = Resource.PAUS_QUELLE_KATALOG;

    /// <summary>„Vorgabetabelle" (<c>PAUS_QUELLE_PARAMETER</c>).</summary>
    public string QuelleParameter { get; set; } = Resource.PAUS_QUELLE_PARAMETER;

    /// <summary>„Vorgabe" (<c>PAUS_QUELLE_VORGABE</c>).</summary>
    public string QuelleVorgabe { get; set; } = Resource.PAUS_QUELLE_VORGABE;

    /// <summary>„gespeichert" (<c>PAUS_QUELLE_GESPEICHERT</c>).</summary>
    public string QuelleGespeichert { get; set; } = Resource.PAUS_QUELLE_GESPEICHERT;

    /// <summary>„überschrieben" (<c>PAUS_QUELLE_UEBERSCHRIEBEN</c>).</summary>
    public string QuelleUeberschrieben { get; set; } = Resource.PAUS_QUELLE_UEBERSCHRIEBEN;

    /// <summary>„Vorlage" (<c>PAUS_QUELLE_VORLAGE</c>).</summary>
    public string QuelleVorlage { get; set; } = Resource.PAUS_QUELLE_VORLAGE;

    /// <summary>„übergeben" (<c>PAUS_QUELLE_UEBERGEBEN</c>).</summary>
    public string QuelleUebergeben { get; set; } = Resource.PAUS_QUELLE_UEBERGEBEN;

    /// <summary>„Speicherklasse und Nutzung" (<c>PAUS_GRUPPE_KLASSE</c>).</summary>
    public string GruppeKlasse { get; set; } = Resource.PAUS_GRUPPE_KLASSE;

    /// <summary>„Heizung" (<c>PAUS_KLASSE_HEIZUNG</c>).</summary>
    public string KlasseHeizung { get; set; } = Resource.PAUS_KLASSE_HEIZUNG;

    /// <summary>„Brauchwasser" (<c>PAUS_KLASSE_BRAUCHWASSER</c>).</summary>
    public string KlasseBrauchwasser { get; set; } = Resource.PAUS_KLASSE_BRAUCHWASSER;

    /// <summary>„Prozess" (<c>PAUS_KLASSE_PROZESS</c>).</summary>
    public string KlasseProzess { get; set; } = Resource.PAUS_KLASSE_PROZESS;

    /// <summary>„Je Klasse eine Zone mit eigenem Verfahren: Heizzone nach den Kriterien, Brauchwasserzone aus dem Zapfprofil, Prozesszone auf der Prozessreihe; Kombi = Heizung + Brauchwasser." (<c>PAUS_KLASSE_HINWEIS</c>).</summary>
    public string KlasseHinweis { get; set; } = Resource.PAUS_KLASSE_HINWEIS;

    /// <summary>„Mindestens eine Speicherklasse wählen — ohne Klasse gibt es keine Zone." (<c>PAUS_KLASSE_LEER</c>).</summary>
    public string KlasseLeer { get; set; } = Resource.PAUS_KLASSE_LEER;

    /// <summary>„Nutzungsprofil" (<c>PAUS_NUTZUNGSPROFIL</c>).</summary>
    public string Nutzungsprofil { get; set; } = Resource.PAUS_NUTZUNGSPROFIL;

    /// <summary>„nur Anzeige: Das Profil prägt die Bedarfsreihen, aus denen bemessen wird; es ist kein Rechenwert." (<c>PAUS_NP_HINWEIS</c>).</summary>
    public string NutzungsprofilHinweis { get; set; } = Resource.PAUS_NP_HINWEIS;

    /// <summary>„Vorlage der Anlage" (<c>PAUS_GRUPPE_VORLAGE</c>).</summary>
    public string GruppeVorlage { get; set; } = Resource.PAUS_GRUPPE_VORLAGE;

    /// <summary>„Die Vorlage schaltet die Kriterien der Heizzone und belegt Beispielwerte; die Brauchwasserzone hat keine Vorlage — sie kommt aus dem Zapfprofil." (<c>PAUS_VORLAGE_HINWEIS</c>).</summary>
    public string VorlageHinweis { get; set; } = Resource.PAUS_VORLAGE_HINWEIS;

    /// <summary>„Gerätetyp" (<c>PAUS_GERAETETYP</c>).</summary>
    public string Geraetetyp { get; set; } = Resource.PAUS_GERAETETYP;

    /// <summary>„Fixed-Speed" (<c>PAUS_FIXED_SPEED</c>).</summary>
    public string FixedSpeed { get; set; } = Resource.PAUS_FIXED_SPEED;

    /// <summary>„leistungsgeregelt" (<c>PAUS_GEREGELT</c>).</summary>
    public string Geregelt { get; set; } = Resource.PAUS_GEREGELT;

    /// <summary>„Zweiterzeuger in der Sperre" (<c>PAUS_ZWEITERZEUGER</c>).</summary>
    public string Zweiterzeuger { get; set; } = Resource.PAUS_ZWEITERZEUGER;

    /// <summary>„Zweiterzeuger frei in der Sperre" (<c>PAUS_ZWEIT_FREI</c>).</summary>
    public string ZweitFrei { get; set; } = Resource.PAUS_ZWEIT_FREI;

    /// <summary>„Heizstab (gesperrt)" (<c>PAUS_ZWEIT_HEIZSTAB</c>).</summary>
    public string ZweitHeizstab { get; set; } = Resource.PAUS_ZWEIT_HEIZSTAB;

    /// <summary>„Erzeuger am Puffer" (<c>PAUS_GRUPPE_ERZEUGER</c>).</summary>
    public string GruppeErzeuger { get; set; } = Resource.PAUS_GRUPPE_ERZEUGER;

    /// <summary>„Kein Wärmeerzeuger im Projekt — die Vorlage rechnet mit ihren Vorgabewerten." (<c>PAUS_ERZEUGER_KEINER</c>).</summary>
    public string ErzeugerKeiner { get; set; } = Resource.PAUS_ERZEUGER_KEINER;

    /// <summary>„Temperaturen und Schwellen am Puffer" (<c>PAUS_GRUPPE_TEMPERATUR</c>).</summary>
    public string GruppeTemperatur { get; set; } = Resource.PAUS_GRUPPE_TEMPERATUR;

    /// <summary>„Vorlauf / Rücklauf" (<c>PAUS_TEMPERATURPAAR</c>).</summary>
    public string Temperaturpaar { get; set; } = Resource.PAUS_TEMPERATURPAAR;

    /// <summary>„Schwellen ein / aus" (<c>PAUS_SCHWELLEN</c>).</summary>
    public string Schwellen { get; set; } = Resource.PAUS_SCHWELLEN;

    /// <summary>„nutzbarer Anteil {0} %" (<c>PAUS_NUTZANTEIL_TEXT</c>).</summary>
    public string NutzanteilText { get; set; } = Resource.PAUS_NUTZANTEIL_TEXT;

    /// <summary>„Temperaturpaar und Schwellen kommen aus dem Puffer; die Auslegung ändert sie nicht." (<c>PAUS_TEMPERATUR_BLEIBT</c>).</summary>
    public string TemperaturBleibt { get; set; } = Resource.PAUS_TEMPERATUR_BLEIBT;

    /// <summary>„Bedarfsreihen" (<c>PAUS_GRUPPE_REIHEN</c>).</summary>
    public string GruppeReihen { get; set; } = Resource.PAUS_GRUPPE_REIHEN;

    /// <summary>„Bemessen wird gegen die stündlichen Bedarfsreihen des Projekts — ohne neuen Simulationslauf." (<c>PAUS_REIHEN_HINWEIS</c>).</summary>
    public string ReihenHinweis { get; set; } = Resource.PAUS_REIHEN_HINWEIS;

    /// <summary>„Die Bedarfsreihen sind nicht rechenbar: {0}" (<c>PAUS_REIHEN_FEHLER</c>).</summary>
    public string ReihenFehler { get; set; } = Resource.PAUS_REIHEN_FEHLER;

    /// <summary>„Kanal" (<c>PAUS_SPALTE_KANAL</c>).</summary>
    public string SpalteKanal { get; set; } = Resource.PAUS_SPALTE_KANAL;

    /// <summary>„Spitze" (<c>PAUS_SPALTE_SPITZE</c>).</summary>
    public string SpalteSpitze { get; set; } = Resource.PAUS_SPALTE_SPITZE;

    /// <summary>„Jahressumme" (<c>PAUS_SPALTE_JAHR</c>).</summary>
    public string SpalteJahr { get; set; } = Resource.PAUS_SPALTE_JAHR;

    /// <summary>„Übergabe und Anlage" (<c>PAUS_GRUPPE_UEBERGABE</c>).</summary>
    public string GruppeUebergabe { get; set; } = Resource.PAUS_GRUPPE_UEBERGABE;

    /// <summary>„Übergabeart" (<c>PAUS_UEBERGABEART</c>).</summary>
    public string Uebergabeart { get; set; } = Resource.PAUS_UEBERGABEART;

    /// <summary>„Heizgrenze" (<c>PAUS_HEIZGRENZE</c>).</summary>
    public string Heizgrenze { get; set; } = Resource.PAUS_HEIZGRENZE;

    /// <summary>„Auslegungsheizlast" (<c>PAUS_HEIZLAST</c>).</summary>
    public string Heizlast { get; set; } = Resource.PAUS_HEIZLAST;

    /// <summary>„Anlagenvolumen, nicht absperrbar" (<c>PAUS_ANLAGENVOLUMEN</c>).</summary>
    public string Anlagenvolumen { get; set; } = Resource.PAUS_ANLAGENVOLUMEN;

    /// <summary>„leer = Vorgabe" (<c>PAUS_LEER_VORGABE</c>).</summary>
    public string LeerVorgabe { get; set; } = Resource.PAUS_LEER_VORGABE;

    /// <summary>„leer = Maximum der Heizreihe" (<c>PAUS_HEIZLAST_LEER</c>).</summary>
    public string HeizlastLeer { get; set; } = Resource.PAUS_HEIZLAST_LEER;

    /// <summary>„leer = Übergabeart × Heizlast" (<c>PAUS_ANLAGENVOLUMEN_LEER</c>).</summary>
    public string AnlagenvolumenLeer { get; set; } = Resource.PAUS_ANLAGENVOLUMEN_LEER;

    /// <summary>„Sperrzeiten" (<c>PAUS_GRUPPE_SPERRE</c>).</summary>
    public string GruppeSperre { get; set; } = Resource.PAUS_GRUPPE_SPERRE;

    /// <summary>„Sperrprofil" (<c>PAUS_SPERRPROFIL</c>).</summary>
    public string Sperrprofil { get; set; } = Resource.PAUS_SPERRPROFIL;

    /// <summary>„Beginn der Sperre" (<c>PAUS_SPERRBEGINN</c>).</summary>
    public string Sperrbeginn { get; set; } = Resource.PAUS_SPERRBEGINN;

    /// <summary>„Dauer der Sperre" (<c>PAUS_SPERRDAUER</c>).</summary>
    public string Sperrdauer { get; set; } = Resource.PAUS_SPERRDAUER;

    /// <summary>„Neutrale Eingabe der Sperrdauer und ihrer Lage je Tag; die Übernahme schreibt die Sperrannahme nicht in die Wärmepumpe." (<c>PAUS_SPERRE_HINWEIS</c>).</summary>
    public string SperreHinweis { get; set; } = Resource.PAUS_SPERRE_HINWEIS;

    /// <summary>„Sperrzeit aus dem Lastgang bemessen (Expertenweg)" (<c>PAUS_EXPERTENWEG</c>).</summary>
    public string Expertenweg { get; set; } = Resource.PAUS_EXPERTENWEG;

    /// <summary>„Betriebsziel" (<c>PAUS_GRUPPE_ZIELE</c>).</summary>
    public string GruppeZiele { get; set; } = Resource.PAUS_GRUPPE_ZIELE;

    /// <summary>„Mindestlaufzeit je Start" (<c>PAUS_MINDESTLAUFZEIT</c>).</summary>
    public string Mindestlaufzeit { get; set; } = Resource.PAUS_MINDESTLAUFZEIT;

    /// <summary>„Kleinste Dauerleistung" (<c>PAUS_MINDESTLEISTUNG</c>).</summary>
    public string Mindestleistung { get; set; } = Resource.PAUS_MINDESTLEISTUNG;

    /// <summary>„Startziel" (<c>PAUS_STARTZIEL</c>).</summary>
    public string Startziel { get; set; } = Resource.PAUS_STARTZIEL;

    /// <summary>„Ziel-Deckungsgrad" (<c>PAUS_DECKUNGSZIEL</c>).</summary>
    public string Deckungsziel { get; set; } = Resource.PAUS_DECKUNGSZIEL;

    /// <summary>„je Tag" (<c>PAUS_EINHEIT_JE_TAG</c>).</summary>
    public string EinheitJeTag { get; set; } = Resource.PAUS_EINHEIT_JE_TAG;

    /// <summary>„Brauchwasserzone aus dem Zapfprofil" (<c>PAUS_GRUPPE_ZAPF</c>).</summary>
    public string GruppeZapf { get; set; } = Resource.PAUS_GRUPPE_ZAPF;

    /// <summary>„Topologie" (<c>PAUS_ZAPF_TOPOLOGIE</c>).</summary>
    public string ZapfTopologie { get; set; } = Resource.PAUS_ZAPF_TOPOLOGIE;

    /// <summary>„Größtes Defizit D_max" (<c>PAUS_ZAPF_DMAX</c>).</summary>
    public string ZapfDmax { get; set; } = Resource.PAUS_ZAPF_DMAX;

    /// <summary>„Personen" (<c>PAUS_ZAPF_PERSONEN</c>).</summary>
    public string ZapfPersonen { get; set; } = Resource.PAUS_ZAPF_PERSONEN;

    /// <summary>„Tagesbedarf bei 60 °C" (<c>PAUS_ZAPF_TAGESBEDARF</c>).</summary>
    public string ZapfTagesbedarf { get; set; } = Resource.PAUS_ZAPF_TAGESBEDARF;

    /// <summary>„Kein Zapfprofil-Ergebnis: Die Brauchwasserzone bleibt leer." (<c>PAUS_ZAPF_KEIN</c>).</summary>
    public string ZapfKein { get; set; } = Resource.PAUS_ZAPF_KEIN;

    /// <summary>„Zirkulationszuschlag" (<c>PAUS_ZIRKULATION</c>).</summary>
    public string Zirkulation { get; set; } = Resource.PAUS_ZIRKULATION;

    /// <summary>„Wohneinheiten" (<c>PAUS_WOHNEINHEITEN</c>).</summary>
    public string Wohneinheiten { get; set; } = Resource.PAUS_WOHNEINHEITEN;

    /// <summary>„Spreizung Brauchwasser ΔT_B" (<c>PAUS_DELTA_TB</c>).</summary>
    public string DeltaTB { get; set; } = Resource.PAUS_DELTA_TB;

    /// <summary>„Temperatur oben im Puffer" (<c>PAUS_T_OBEN</c>).</summary>
    public string TOben { get; set; } = Resource.PAUS_T_OBEN;

    /// <summary>„Je Zone bemisst das größte aktive Kriterium. Die Vorlage hat vorgewählt, was zur Anlage passt; Kriterien lassen sich zu- oder abschalten." (<c>PAUS_KRITERIEN_HINWEIS</c>).</summary>
    public string KriterienHinweis { get; set; } = Resource.PAUS_KRITERIEN_HINWEIS;

    /// <summary>„bemisst mit" (<c>PAUS_KRIT_AN</c>).</summary>
    public string KritAn { get; set; } = Resource.PAUS_KRIT_AN;

    /// <summary>„bemessend" (<c>PAUS_KRIT_BEMESSEND</c>).</summary>
    public string KritBemessend { get; set; } = Resource.PAUS_KRIT_BEMESSEND;

    /// <summary>„nicht erreichbar" (<c>PAUS_KRIT_NICHT_GUELTIG</c>).</summary>
    public string KritNichtGueltig { get; set; } = Resource.PAUS_KRIT_NICHT_GUELTIG;

    /// <summary>„abgeschaltet" (<c>PAUS_KRIT_AUS</c>).</summary>
    public string KritAus { get; set; } = Resource.PAUS_KRIT_AUS;

    /// <summary>„noch nicht gerechnet" (<c>PAUS_KRIT_NOCH_NICHT</c>).</summary>
    public string KritNochNicht { get; set; } = Resource.PAUS_KRIT_NOCH_NICHT;

    /// <summary>„Rechenweg" (<c>PAUS_KRIT_RECHENWEG</c>).</summary>
    public string KritRechenweg { get; set; } = Resource.PAUS_KRIT_RECHENWEG;

    /// <summary>„Brauchwasserzone" (<c>PAUS_KRIT_BRAUCHWASSER</c>).</summary>
    public string KritBrauchwasser { get; set; } = Resource.PAUS_KRIT_BRAUCHWASSER;

    /// <summary>„Vorschlag je Zone" (<c>PAUS_GRUPPE_ZONEN</c>).</summary>
    public string GruppeZonen { get; set; } = Resource.PAUS_GRUPPE_ZONEN;

    /// <summary>„Zone" (<c>PAUS_SPALTE_ZONE</c>).</summary>
    public string SpalteZone { get; set; } = Resource.PAUS_SPALTE_ZONE;

    /// <summary>„bemessend" (<c>PAUS_SPALTE_BEMESSEND</c>).</summary>
    public string SpalteBemessend { get; set; } = Resource.PAUS_SPALTE_BEMESSEND;

    /// <summary>„Volumen" (<c>PAUS_SPALTE_VOLUMEN</c>).</summary>
    public string SpalteVolumen { get; set; } = Resource.PAUS_SPALTE_VOLUMEN;

    /// <summary>„Summe der Zonen" (<c>PAUS_SUMME</c>).</summary>
    public string Summe { get; set; } = Resource.PAUS_SUMME;

    /// <summary>„Empfehlung" (<c>PAUS_EMPFEHLUNG</c>).</summary>
    public string Empfehlung { get; set; } = Resource.PAUS_EMPFEHLUNG;

    /// <summary>„{0} l · {1} kWh nutzbar" (<c>PAUS_EMPFEHLUNG_ZEILE</c>).</summary>
    public string EmpfehlungZeile { get; set; } = Resource.PAUS_EMPFEHLUNG_ZEILE;

    /// <summary>„kein Puffer erforderlich" (<c>PAUS_KEIN_PUFFER</c>).</summary>
    public string KeinPuffer { get; set; } = Resource.PAUS_KEIN_PUFFER;

    /// <summary>„über der Nenninhaltsliste auf das Raster gerundet" (<c>PAUS_UEBER_LISTENENDE</c>).</summary>
    public string UeberListenende { get; set; } = Resource.PAUS_UEBER_LISTENENDE;

    /// <summary>„an der Praxisgrenze gestoppt" (<c>PAUS_AN_PRAXISGRENZE</c>).</summary>
    public string AnPraxisgrenze { get; set; } = Resource.PAUS_AN_PRAXISGRENZE;

    /// <summary>„Katalogvorschlag" (<c>PAUS_KATALOGVORSCHLAG</c>).</summary>
    public string Katalogvorschlag { get; set; } = Resource.PAUS_KATALOGVORSCHLAG;

    /// <summary>„kein Katalogpuffer groß genug" (<c>PAUS_KATALOG_KEINER</c>).</summary>
    public string KatalogKeiner { get; set; } = Resource.PAUS_KATALOG_KEINER;

    /// <summary>„Kennzahlen" (<c>PAUS_GRUPPE_KENNZAHLEN</c>).</summary>
    public string GruppeKennzahlen { get; set; } = Resource.PAUS_GRUPPE_KENNZAHLEN;

    /// <summary>„Starts je Tag" (<c>PAUS_KZ_STARTS_TAG</c>).</summary>
    public string KzStartsTag { get; set; } = Resource.PAUS_KZ_STARTS_TAG;

    /// <summary>„Starts je Heizperiode" (<c>PAUS_KZ_STARTS_JAHR</c>).</summary>
    public string KzStartsJahr { get; set; } = Resource.PAUS_KZ_STARTS_JAHR;

    /// <summary>„Bereitschaftsverlust bei 45 K" (<c>PAUS_KZ_VERLUST_TAG</c>).</summary>
    public string KzVerlustTag { get; set; } = Resource.PAUS_KZ_VERLUST_TAG;

    /// <summary>„Wärmeverlustrate" (<c>PAUS_KZ_VERLUST_WK</c>).</summary>
    public string KzVerlustWk { get; set; } = Resource.PAUS_KZ_VERLUST_WK;

    /// <summary>„Betriebsverlust" (<c>PAUS_KZ_VERLUST_JAHR</c>).</summary>
    public string KzVerlustJahr { get; set; } = Resource.PAUS_KZ_VERLUST_JAHR;

    /// <summary>„Nutzbare Kapazität" (<c>PAUS_KZ_KAPAZITAET</c>).</summary>
    public string KzKapazitaet { get; set; } = Resource.PAUS_KZ_KAPAZITAET;

    /// <summary>„Nutzbarer Anteil" (<c>PAUS_KZ_NUTZANTEIL</c>).</summary>
    public string KzNutzanteil { get; set; } = Resource.PAUS_KZ_NUTZANTEIL;

    /// <summary>„Faustwert der Vorlage (Gegenprobe)" (<c>PAUS_KZ_FAUSTWERT</c>).</summary>
    public string KzFaustwert { get; set; } = Resource.PAUS_KZ_FAUSTWERT;

    /// <summary>„Tagesbedarf je Person bei 60 °C" (<c>PAUS_KZ_JE_PERSON</c>).</summary>
    public string KzJePerson { get; set; } = Resource.PAUS_KZ_JE_PERSON;

    /// <summary>„Zonenanteil Heizung" (<c>PAUS_KZ_ZONENANTEIL</c>).</summary>
    public string KzZonenanteil { get; set; } = Resource.PAUS_KZ_ZONENANTEIL;

    /// <summary>„Schichten mindestens" (<c>PAUS_KZ_SCHICHTEN</c>).</summary>
    public string KzSchichten { get; set; } = Resource.PAUS_KZ_SCHICHTEN;

    /// <summary>„aus dem Katalogsatz" (<c>PAUS_KZ_AUS_KATALOG</c>).</summary>
    public string KzAusKatalog { get; set; } = Resource.PAUS_KZ_AUS_KATALOG;

    /// <summary>„Grenze Klasse C" (<c>PAUS_KZ_KLASSE_C</c>).</summary>
    public string KzKlasseC { get; set; } = Resource.PAUS_KZ_KLASSE_C;

    /// <summary>„extrapoliert" (<c>PAUS_KZ_EXTRAPOLIERT</c>).</summary>
    public string KzExtrapoliert { get; set; } = Resource.PAUS_KZ_EXTRAPOLIERT;

    /// <summary>„Plausibilitätsband nach Übergabeart: {0}–{1} l" (<c>PAUS_BAND</c>).</summary>
    public string Band { get; set; } = Resource.PAUS_BAND;

    /// <summary>„Band nach Wärmepumpenleistung: {0}–{1} l" (<c>PAUS_BAND_WP</c>).</summary>
    public string BandWp { get; set; } = Resource.PAUS_BAND_WP;

    /// <summary>„Hinweise" (<c>PAUS_GRUPPE_WARNUNGEN</c>).</summary>
    public string GruppeWarnungen { get; set; } = Resource.PAUS_GRUPPE_WARNUNGEN;

    /// <summary>„Code" (<c>PAUS_SPALTE_CODE</c>).</summary>
    public string SpalteCode { get; set; } = Resource.PAUS_SPALTE_CODE;

    /// <summary>„Stufe" (<c>PAUS_SPALTE_STUFE</c>).</summary>
    public string SpalteStufe { get; set; } = Resource.PAUS_SPALTE_STUFE;

    /// <summary>„Hinweis" (<c>PAUS_SPALTE_TEXT</c>).</summary>
    public string SpalteText { get; set; } = Resource.PAUS_SPALTE_TEXT;

    /// <summary>„Herkunft" (<c>PAUS_SPALTE_HERKUNFT</c>).</summary>
    public string SpalteHerkunft { get; set; } = Resource.PAUS_SPALTE_HERKUNFT;

    /// <summary>„Hinweis" (<c>PAUS_STUFE_HINWEIS</c>).</summary>
    public string StufeHinweis { get; set; } = Resource.PAUS_STUFE_HINWEIS;

    /// <summary>„Warnung" (<c>PAUS_STUFE_WARNUNG</c>).</summary>
    public string StufeWarnung { get; set; } = Resource.PAUS_STUFE_WARNUNG;

    /// <summary>„keine Hinweise" (<c>PAUS_WARNUNGEN_LEER</c>).</summary>
    public string WarnungenLeer { get; set; } = Resource.PAUS_WARNUNGEN_LEER;

    /// <summary>„Noch nicht gerechnet — „Auslegen und Ergebnis zeigen“ rechnet die Zonen." (<c>PAUS_ERGEBNIS_LEER</c>).</summary>
    public string ErgebnisLeer { get; set; } = Resource.PAUS_ERGEBNIS_LEER;

    /// <summary>„Übernahme in den Projektpuffer" (<c>PAUS_GRUPPE_UEBERNAHME</c>).</summary>
    public string GruppeUebernahme { get; set; } = Resource.PAUS_GRUPPE_UEBERNAHME;

    /// <summary>„Ziel" (<c>PAUS_ZIEL</c>).</summary>
    public string Ziel { get; set; } = Resource.PAUS_ZIEL;

    /// <summary>„„{0}“ ändern" (<c>PAUS_ZIEL_AENDERN</c>).</summary>
    public string ZielAendern { get; set; } = Resource.PAUS_ZIEL_AENDERN;

    /// <summary>„neuen Pufferspeicher anlegen" (<c>PAUS_ZIEL_NEU</c>).</summary>
    public string ZielNeu { get; set; } = Resource.PAUS_ZIEL_NEU;

    /// <summary>„Bezeichnung" (<c>PAUS_BEZEICHNER</c>).</summary>
    public string Bezeichner { get; set; } = Resource.PAUS_BEZEICHNER;

    /// <summary>„Schwellen und Temperaturpaar bleiben. Übernommen werden Volumen, Bereitschaftsverlust und Nutzung, beim Kombipuffer Schichten und Zonenanteile; die Sperrannahme nicht." (<c>PAUS_UEBERNAHME_BLEIBT</c>).</summary>
    public string UebernahmeBleibt { get; set; } = Resource.PAUS_UEBERNAHME_BLEIBT;

    /// <summary>„Auslegung speichern" (<c>PAUS_BTN_SPEICHERN</c>).</summary>
    public string BtnSpeichern { get; set; } = Resource.PAUS_BTN_SPEICHERN;

    /// <summary>„Übernehmen" (<c>PAUS_BTN_UEBERNEHMEN</c>).</summary>
    public string BtnUebernehmen { get; set; } = Resource.PAUS_BTN_UEBERNEHMEN;

    /// <summary>„Auslegung gespeichert." (<c>PAUS_MELDUNG_GESPEICHERT</c>).</summary>
    public string MeldungGespeichert { get; set; } = Resource.PAUS_MELDUNG_GESPEICHERT;

    /// <summary>„Die Auslegung empfiehlt keinen Puffer — es gibt nichts zu übernehmen." (<c>PAUS_GRUND_KEINE_EMPFEHLUNG</c>).</summary>
    public string GrundKeineEmpfehlung { get; set; } = Resource.PAUS_GRUND_KEINE_EMPFEHLUNG;

    /// <summary>„Für einen neuen Speicher eine Bezeichnung eingeben." (<c>PAUS_GRUND_BEZEICHNER</c>).</summary>
    public string GrundBezeichner { get; set; } = Resource.PAUS_GRUND_BEZEICHNER;

    /// <summary>
    /// Der Text zum Schlüssel <paramref name="praefix"/> + <paramref name="wert"/> (Bindestrich wird
    /// Unterstrich, groß geschrieben); <paramref name="rueckfall"/>, wenn der Schlüssel fehlt.
    /// </summary>
    public static string Nach(string praefix, string wert, string? rueckfall = null)
    {
        string schluessel = praefix + (wert ?? "").Replace('-', '_').ToUpperInvariant();
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel, CultureInfo.CurrentUICulture); }
        catch (Exception) { t = null; }
        return string.IsNullOrEmpty(t) ? (rueckfall ?? wert ?? "") : t;
    }

    /// <summary>Der Name eines Kriteriums (<c>PAUS_KRIT_*</c>); Rückfall die Kennung.</summary>
    public string Kriterium(string kennung) => Nach("PAUS_KRIT_", kennung, kennung);

    /// <summary>Der Name einer Zone (<c>PAUS_ZONE_*</c>).</summary>
    public string Zone(string zone) => Nach("PAUS_ZONE_", zone, zone);

    /// <summary>Der Text eines Sperrprofils (<c>PAUS_SPERRE_*</c>).</summary>
    public string SperrprofilText(string profil) => Nach("PAUS_SPERRE_", profil, profil);

    /// <summary>Die Topologie der Brauchwasserbereitung (<c>PAUS_TOPO_*</c>).</summary>
    public string Topologie(string topologie) => Nach("PAUS_TOPO_", topologie, topologie);
}
