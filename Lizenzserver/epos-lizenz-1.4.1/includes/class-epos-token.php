<?php
/**
 * Lizenz-Token: Ed25519-Schlüsselpaar und Signierung.
 *
 * Der private Signaturschlüssel liegt in einer Datei außerhalb des Web-Roots
 * (falls beschreibbar), sonst in einem per .htaccess gesperrten Verzeichnis
 * unterhalb von wp-content/uploads. Er liegt nie in der Datenbank.
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

class Epos_Token {

	const DATEI_PRIVAT     = 'epos-signatur-privat.key';
	const DATEI_OEFFENTLICH = 'epos-signatur-oeffentlich.key';

	/**
	 * Verzeichnis für das Schlüsselpaar bestimmen (einmalig gewählt, dann fest).
	 */
	public static function schluessel_verzeichnis() {
		$gespeichert = get_option( 'epos_lizenz_schluessel_verzeichnis' );
		if ( $gespeichert && is_dir( $gespeichert ) ) {
			return trailingslashit( $gespeichert );
		}

		// 1. Wahl: eine Ebene über dem Web-Root
		$kandidaten = array(
			dirname( ABSPATH ) . '/epos-lizenz-schluessel',
			WP_CONTENT_DIR . '/uploads/epos-lizenz-schluessel',
		);

		foreach ( $kandidaten as $verz ) {
			if ( ! is_dir( $verz ) ) {
				@mkdir( $verz, 0700, true );
			}
			if ( is_dir( $verz ) && is_writable( $verz ) ) {
				// Verzeichnis gegen Web-Zugriff absichern (relevant für den Uploads-Fall)
				if ( ! file_exists( $verz . '/.htaccess' ) ) {
					@file_put_contents( $verz . '/.htaccess', "Require all denied\nDeny from all\n" );
				}
				if ( ! file_exists( $verz . '/index.php' ) ) {
					@file_put_contents( $verz . '/index.php', "<?php // Zugriff verweigert\n" );
				}
				update_option( 'epos_lizenz_schluessel_verzeichnis', $verz, false );
				return trailingslashit( $verz );
			}
		}

		return null;
	}

	/**
	 * Schlüsselpaar erzeugen, falls noch nicht vorhanden.
	 */
	public static function schluesselpaar_sicherstellen() {
		if ( ! function_exists( 'sodium_crypto_sign_keypair' ) ) {
			return new WP_Error( 'sodium_fehlt', 'Die PHP-Erweiterung Sodium ist nicht verfügbar.' );
		}

		$verz = self::schluessel_verzeichnis();
		if ( ! $verz ) {
			return new WP_Error( 'verzeichnis', 'Kein beschreibbares Schlüsselverzeichnis gefunden.' );
		}

		if ( file_exists( $verz . self::DATEI_PRIVAT ) && file_exists( $verz . self::DATEI_OEFFENTLICH ) ) {
			return true;
		}

		$paar      = sodium_crypto_sign_keypair();
		$privat    = sodium_crypto_sign_secretkey( $paar );
		$oeffent   = sodium_crypto_sign_publickey( $paar );

		file_put_contents( $verz . self::DATEI_PRIVAT, base64_encode( $privat ) );
		@chmod( $verz . self::DATEI_PRIVAT, 0600 );
		file_put_contents( $verz . self::DATEI_OEFFENTLICH, base64_encode( $oeffent ) );
		@chmod( $verz . self::DATEI_OEFFENTLICH, 0644 );

		return true;
	}

	private static function privater_schluessel() {
		$verz = self::schluessel_verzeichnis();
		if ( ! $verz || ! file_exists( $verz . self::DATEI_PRIVAT ) ) {
			return null;
		}
		$inhalt = trim( (string) file_get_contents( $verz . self::DATEI_PRIVAT ) );
		$roh    = base64_decode( $inhalt, true );
		return ( false === $roh ) ? null : $roh;
	}

	/**
	 * Öffentlichen Schlüssel (Base64) liefern — wird in die Anwendung einkompiliert.
	 */
	public static function oeffentlicher_schluessel_base64() {
		$verz = self::schluessel_verzeichnis();
		if ( ! $verz || ! file_exists( $verz . self::DATEI_OEFFENTLICH ) ) {
			return null;
		}
		return trim( (string) file_get_contents( $verz . self::DATEI_OEFFENTLICH ) );
	}

	/**
	 * Nutzdaten kanonisch serialisieren (feste Schlüsselreihenfolge).
	 */
	public static function kanonisch( array $daten ) {
		ksort( $daten );
		return wp_json_encode( $daten, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE );
	}

	/**
	 * Beliebige Nutzdaten signieren.
	 *
	 * Signiert werden die exakten JSON-Bytes der Nutzdaten; genau diese Bytes
	 * werden Base64-codiert mitgeliefert. Der Client verifiziert die Signatur
	 * über die decodierten Bytes und parst erst danach das JSON — es ist
	 * keinerlei Kanonisierung auf Client-Seite nötig.
	 *
	 * @return array|WP_Error  { format, nutzdaten (Base64-JSON), signatur (Base64) }
	 */
	public static function signieren( array $daten ) {
		$privat = self::privater_schluessel();
		if ( ! $privat ) {
			return new WP_Error( 'signaturschluessel', 'Der Signaturschlüssel ist nicht verfügbar.' );
		}
		$nachricht = self::kanonisch( $daten );
		return array(
			'format'    => 'epos-signiert-1',
			'nutzdaten' => base64_encode( $nachricht ),
			'signatur'  => base64_encode( sodium_crypto_sign_detached( $nachricht, $privat ) ),
		);
	}

	/**
	 * Lizenz-Token für ein Gerät ausstellen.
	 *
	 * @param WP_Post $lizenz  Lizenzdatensatz
	 * @param WP_User $benutzer
	 * @param object  $geraet  Zeile aus der Geräte-Tabelle
	 * @return array|WP_Error
	 */
	public static function ausstellen( $lizenz, $benutzer, $geraet ) {
		$token_tage = (int) epos_lizenz_option( 'token_tage' );

		$daten = array(
			'format'      => 'epos-token-1',
			'lizenz_id'   => sprintf( 'EPOS-%d-%05d', (int) get_the_date( 'Y', $lizenz ), $lizenz->ID ),
			'nummer'      => (int) $lizenz->ID,
			'firma'       => (string) get_post_meta( $lizenz->ID, '_epos_firma', true ),
			'benutzer'    => $benutzer->user_email,
			'geraete_id'  => $geraet->geraete_hash,
			'token_id'    => $geraet->token_id,
			'typ'         => (string) get_post_meta( $lizenz->ID, '_epos_typ', true ),
			'edition'     => (string) get_post_meta( $lizenz->ID, '_epos_edition', true ),
			'gueltig_ab'  => (string) get_post_meta( $lizenz->ID, '_epos_gueltig_ab', true ),
			'gueltig_bis' => (string) get_post_meta( $lizenz->ID, '_epos_gueltig_bis', true ),
			'kulanz_tage' => (int) epos_lizenz_option( 'kulanz_tage' ),
			'token_bis'   => gmdate( 'Y-m-d', time() + $token_tage * DAY_IN_SECONDS ),
			'ausgestellt' => gmdate( 'c' ),
		);

		return self::signieren( $daten );
	}

	/**
	 * Inhalt einer .lic-Datei erzeugen (signiert).
	 */
	public static function lic_datei( $schluessel, $firma, $email ) {
		$daten = array(
			'format'     => 'epos-lic-1',
			'schluessel' => $schluessel,
			'firma'      => (string) $firma,
			'email'      => (string) $email,
			'portal'     => home_url( '/' ),
			'erstellt'   => gmdate( 'c' ),
		);
		$signiert = self::signieren( $daten );
		if ( is_wp_error( $signiert ) ) {
			return $signiert;
		}
		return wp_json_encode( $signiert, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE );
	}
}
