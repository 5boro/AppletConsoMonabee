<?php 
	//Connection bdd
	$mysqli = new mysqli("localhost", "root", "root", "monabee");
	if ($mysqli->connect_errno) {
	    echo "Failed to connect to MySQL: (" . $mysqli->connect_errno . ") " . $mysqli->connect_error;
	}

	//Appel de la fonction en param
	if (function_exists($_GET['abonnement'])) {
		getAbonnement();
	}
	//Methodes
	function getAbonnement(){
		$etatAbonnement_sql=$mysqli->query("SELECT abonnement FROM clients WHERE idClient =".$_GET['idClient'].";");
		$etatAbonnement=$etatAbonnement_sql->fetch_assoc();
		$etatAbonnment=json_encode($etatAbonnement);
		echo $_GET['jsoncallback'].'('.$etatAbonnement.')';
	}
 ?>