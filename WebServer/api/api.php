<?php
// Original code before modification copied from https://blog.ijasoneverett.com/2013/02/rest-api-a-simple-php-tutorial/
// DB connection and info retrieval
$servername = "localhost";
$username = "client1";
$password = "1isnotenough2istoomany";
$dbname = "wavfiles";

$conn = new mysqli($servername, $username, $password, $dbname);
if ($conn->connect_error) {
	die("Connection failed: " . $conn->connect_error ."<br>");
}


function clear_all_files($conn)
{
	// Add code here that will get rid of any existing wav files on the server.
	// Ideally, do a SQL call for this.
	if ($conn->query("TRUNCATE wavtable"))
	{
		echo("Wavefiles table successfully wiped.");
	}
	else
	{
		die("Failed to perform erasure of existing records: " . $conn->errno);
	}
}

function add_file($conn)
{
	$stmt = $conn->prepare("INSERT INTO `wavtable` (`name`, `wavfile`) VALUES (?, ?);");
	$stmt->bind_param("sb", $_POST["name"], $_POST["wavFile"]);
	$stmt->execute();
	if ($stmt->errno != 0)
	{
		die("Failed to add new record to database: Error " . $stmt->errno);
	}
	else
	{
		echo "New database entry added with filename " . $_POST["name"];
	}
}

$possible_url = array("clear_all_files", "get_files_list");

if (isset($_GET["action"]) && in_array($_GET["action"], $possible_url))
{
	switch ($_GET["action"])
	{
		case "get_files_list":
			// NOT IMPLEMENTED. TO BE DONE IF REQUESTED BY TESTDEVLAB.
			die("Not Implemented.");
			break;
		case "clear_all_files":
			clear_all_files($conn);
			break;
		default:
			die("Unknown command.");
			break;
	}
}
if (isset($_POST["action"]) && ($_POST["action"] == "add_file"))
{
	add_file($conn);
}
?>