<?php
/**
 * Aufbewahrung und Datensparsamkeit der Lizenzdaten.
 *
 * Bisher blieb jeder Lizenzdatensatz unbegrenzt auf dem Server liegen - es gab
 * im Plugin weder einen Cron noch eine Aufbewahrungsgrenze. Diese Klasse raeumt
 * taeglich auf:
 *
 *   1. abgelaufene Lizenzen samt Geraeteeintraegen loeschen (nach Karenzzeit)
 *   2. verwaiste Geraeteeintraege loeschen
 *   3. IP-Adresse aus dem Bestellprotokoll entfernen, sobald Widerrufs- und
 *      Verjaehrungsfristen abgelaufen sind - der uebrige Protokolldatensatz
 *      (Zeitstempel, Tarif, akzeptierte Fassung, SHA-256) bleibt unangetastet,
 *      weil die Spezifikation Abschnitt 5 ihn zur Beweissicherung verlangt
 *   4. erzeugte Vertragsausfertigungen (PDF) aus dem Uploads-Verzeichnis
 *      entfernen - sie werden bei Bedarf aus denselben Daten neu erzeugt
 *   5. Geraetenamen leeren, solange die Option 'geraetename' auf 0 steht
 *
 * BEWUSST NICHT geloescht werden Bestellungen und Rechnungsdaten: sie
 * unterliegen der Aufbewahrungspflicht nach § 147 AO und dienen der
 * Beweissicherung des Vertragsschlusses.
 *
 * Werte anpassen (einmalig, z. B. in der WordPress-Konsole):
 *   update_option( 'epos_aufbewahrung', array( 'lizenz_tage' => 365, 'ip_tage' => 1095 ) );
 * oder im Code ueber den Filter 'epos_aufbewahrung_optionen'.
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Aufbewahrung {

	const OPTION  = 'epos_aufbewahrung';
	const HOOK    = 'epos_aufbewahrung_taeglich';
	const BERICHT = 'epos_aufbewahrung_bericht';

	/** Wie viele Bestellungen je Lauf hoechstens angefasst werden. */
	const STAPEL = 200;

	public static function optionen() {
		$werte = wp_parse_args( (array) get_option( self::OPTION, array() ), array(
			'aktiv'       => 1,     // Aufraeumen ein-/ausschalten
			'lizenz_tage' => 365,   // Karenz nach Ablauf bei Laufzeiten ueber 6 Monaten
			'lizenz_tage_kurz' => 90, // Karenz bei kurzen Laufzeiten (Tarif "Einzel (3 Monate)")
			'ip_tage'     => 1095,  // 3 Jahre - Regelverjaehrung § 195 BGB
			'pdf_tage'    => 30,    // erzeugte Vertragsausfertigungen
			'geraetename' => 0,     // 1 = Geraetenamen im Klartext speichern
		) );
		return apply_filters( 'epos_aufbewahrung_optionen', $werte );
	}

	/* ------------------------------------------------- Zeitplan */

	public static function init() {
		add_action( self::HOOK, array( __CLASS__, 'lauf' ) );

		// Nachplanen, falls das Plugin per Dateiersetzung aktualisiert wurde:
		// dabei laeuft register_activation_hook nicht, der Termin fehlte sonst.
		if ( ! wp_next_scheduled( self::HOOK ) ) {
			self::planen();
		}
	}

	/** Bei der Plugin-Aktivierung aufrufen. */
	public static function planen() {
		if ( ! wp_next_scheduled( self::HOOK ) ) {
			// nachts, wenn wenig los ist
			wp_schedule_event( strtotime( 'tomorrow 03:20' ), 'daily', self::HOOK );
		}
	}

	/** Bei der Plugin-Deaktivierung aufrufen. */
	public static function abmelden() {
		wp_clear_scheduled_hook( self::HOOK );
	}

	/**
	 * Geraetename gemaess Option - Standard: nicht speichern.
	 * In Epos_Geraete::registrieren() verwenden:
	 *     'name' => Epos_Aufbewahrung::geraetename( $name ),
	 */
	public static function geraetename( $name ) {
		$opt = self::optionen();
		return empty( $opt['geraetename'] ) ? '' : sanitize_text_field( $name );
	}

	/* ------------------------------------------------- Lauf */

	public static function lauf() {
		$opt = self::optionen();
		if ( empty( $opt['aktiv'] ) ) {
			return;
		}

		$bericht = array(
			'zeitpunkt'    => current_time( 'mysql', true ),
			'lizenzen'     => self::abgelaufene_lizenzen_loeschen(
				(int) $opt['lizenz_tage'], (int) $opt['lizenz_tage_kurz'] ),
			'geraete'      => self::verwaiste_geraete_loeschen(),
			'namen'        => empty( $opt['geraetename'] ) ? self::geraetenamen_leeren() : 0,
			'ip_entfernt'  => self::ip_im_protokoll_entfernen( (int) $opt['ip_tage'] ),
			'pdf_geloescht' => self::vertrags_pdf_aufraeumen( (int) $opt['pdf_tage'] ),
		);

		update_option( self::BERICHT, $bericht, false );
	}

	/** Laufzeiten bis hierher gelten als kurz (rund ein halbes Jahr). */
	const KURZ_GRENZE_TAGE = 186;

	/**
	 * Lizenzen loeschen, deren Gueltigkeit laenger als die Karenzzeit vorbei ist.
	 * Lizenzen ohne Ablaufdatum (unbefristet) bleiben unangetastet.
	 *
	 * Die Karenz haengt an der tatsaechlichen Laufzeit, nicht am Tarifnamen:
	 * Der Tarif "Einzel (3 Monate)" endet automatisch und verlaengert sich
	 * nicht - eine lange Karenz haelt dort einen Datensatz vor, fuer den es
	 * nichts mehr zu verlaengern gibt. Die Laufzeit wird aus _epos_gueltig_ab
	 * und _epos_gueltig_bis bestimmt, damit kuenftige Tarife ohne Anpassung
	 * mitlaufen.
	 */
	private static function abgelaufene_lizenzen_loeschen( $karenz_lang, $karenz_kurz ) {
		$karenz_lang = max( 0, (int) $karenz_lang );
		$karenz_kurz = max( 0, (int) $karenz_kurz );
		if ( $karenz_lang <= 0 && $karenz_kurz <= 0 ) {
			return 0;
		}

		// Vorauswahl mit der kuerzeren Karenz; entschieden wird je Datensatz.
		$vorlauf = min( $karenz_lang, $karenz_kurz );
		$grenze  = gmdate( 'Y-m-d', time() - $vorlauf * DAY_IN_SECONDS );

		$ids = get_posts( array(
			'post_type'      => 'epos_lizenz',
			'post_status'    => 'any',
			'fields'         => 'ids',
			'posts_per_page' => self::STAPEL,
			'meta_query'     => array(
				array(
					'key'     => '_epos_gueltig_bis',
					'value'   => $grenze,
					'compare' => '<',
					'type'    => 'DATE',
				),
				array(
					'key'     => '_epos_gueltig_bis',
					'value'   => '',
					'compare' => '!=',
				),
			),
		) );

		$heute  = time();
		$anzahl = 0;

		foreach ( $ids as $id ) {
			$bis = (string) get_post_meta( (int) $id, '_epos_gueltig_bis', true );
			$ab  = (string) get_post_meta( (int) $id, '_epos_gueltig_ab', true );

			$bis_zeit = $bis ? strtotime( $bis . ' 00:00:00 UTC' ) : 0;
			if ( ! $bis_zeit ) {
				continue; // ohne belastbares Ablaufdatum wird nichts geloescht
			}

			$ab_zeit  = $ab ? strtotime( $ab . ' 00:00:00 UTC' ) : 0;
			$laufzeit = ( $ab_zeit && $bis_zeit > $ab_zeit ) ? ( $bis_zeit - $ab_zeit ) : 0;
			$kurz     = ( $laufzeit > 0 && $laufzeit <= self::KURZ_GRENZE_TAGE * DAY_IN_SECONDS );
			$karenz   = $kurz ? $karenz_kurz : $karenz_lang;

			if ( $karenz <= 0 || $bis_zeit + $karenz * DAY_IN_SECONDS >= $heute ) {
				continue;
			}

			self::geraete_der_lizenz_loeschen( (int) $id );
			if ( wp_delete_post( (int) $id, true ) ) {
				$anzahl++;
			}
		}

		return $anzahl;
	}

	private static function geraete_der_lizenz_loeschen( $lizenz_id ) {
		global $wpdb;
		$tabelle = Epos_Geraete::tabelle();
		$wpdb->query( $wpdb->prepare( "DELETE FROM {$tabelle} WHERE lizenz_id = %d", $lizenz_id ) );
	}

	/** Geraeteeintraege ohne zugehoerigen Lizenzdatensatz entfernen. */
	private static function verwaiste_geraete_loeschen() {
		global $wpdb;
		$tabelle = Epos_Geraete::tabelle();
		return (int) $wpdb->query(
			"DELETE g FROM {$tabelle} g
			 LEFT JOIN {$wpdb->posts} p ON p.ID = g.lizenz_id AND p.post_type = 'epos_lizenz'
			 WHERE p.ID IS NULL"
		);
	}

	/** Bereits gespeicherte Geraetenamen leeren. */
	private static function geraetenamen_leeren() {
		global $wpdb;
		$tabelle = Epos_Geraete::tabelle();
		return (int) $wpdb->query( "UPDATE {$tabelle} SET name = '' WHERE name <> ''" );
	}

	/**
	 * IP-Adresse aus dem Abschlussprotokoll entfernen, sobald sie nicht mehr
	 * gebraucht wird. Alles Uebrige bleibt - der Nachweis, wer wann welcher
	 * Fassung zugestimmt hat, haengt nicht an der IP.
	 */
	private static function ip_im_protokoll_entfernen( $tage ) {
		if ( $tage <= 0 || ! function_exists( 'wc_get_orders' ) ) {
			return 0;
		}
		$grenze = gmdate( 'Y-m-d H:i:s', time() - $tage * DAY_IN_SECONDS );

		$bestellungen = wc_get_orders( array(
			'limit'         => self::STAPEL,
			'date_created'  => '<' . $grenze,
			'meta_key'      => '_epos_vertrag_protokoll',
			'meta_compare'  => 'EXISTS',
			'return'        => 'objects',
		) );

		$anzahl = 0;
		foreach ( $bestellungen as $order ) {
			$protokoll = $order->get_meta( '_epos_vertrag_protokoll' );
			if ( ! is_array( $protokoll ) ) {
				continue;
			}
			$geaendert = false;
			foreach ( $protokoll as $i => $eintrag ) {
				if ( is_array( $eintrag ) && isset( $eintrag['ip'] ) && '' !== $eintrag['ip'] ) {
					$protokoll[ $i ]['ip'] = '';
					$geaendert             = true;
				}
			}
			if ( $geaendert ) {
				$order->update_meta_data( '_epos_vertrag_protokoll', $protokoll );
				$order->save();
				$anzahl++;
			}
		}
		return $anzahl;
	}

	/**
	 * Erzeugte Vertragsausfertigungen entfernen. Sie liegen dauerhaft im
	 * Uploads-Verzeichnis und werden bei Bedarf neu erzeugt
	 * (epos_vertrag_datenblatt_pfad legt sie wieder an).
	 */
	private static function vertrags_pdf_aufraeumen( $tage ) {
		if ( $tage <= 0 ) {
			return 0;
		}
		$uploads = wp_upload_dir();
		$dir     = trailingslashit( $uploads['basedir'] ) . 'epos-vertraege';
		if ( ! is_dir( $dir ) ) {
			return 0;
		}
		$grenze  = time() - $tage * DAY_IN_SECONDS;
		$dateien = glob( $dir . '/*.pdf' );
		if ( ! is_array( $dateien ) ) {
			return 0;
		}

		$anzahl = 0;
		foreach ( $dateien as $datei ) {
			if ( @filemtime( $datei ) < $grenze && @unlink( $datei ) ) {
				$anzahl++;
			}
		}
		return $anzahl;
	}
}
