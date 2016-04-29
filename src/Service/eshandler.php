<?php
# Read get parameters
$id = filter_input(INPUT_GET, 'id');
$method = filter_input(INPUT_GET, 'method');

# Calculate folder for the id
$idFolder = realpath(dirname(__FILE__)) . DIRECTORY_SEPARATOR . $id . DIRECTORY_SEPARATOR;
# Create the folder if it does not yet exist
if(!is_dir($idFolder)) { mkdir($idFolder); }

# Handle the different methods
if ($method === 'check') {
	# Just return the status
	echo json_encode(getStatus());
	exit;
} elseif ($method === 'gettext') {
	header('Content-type: text/plain;');
	echo getText();
} elseif ($method === 'getfiles') {
	header('Content-type: text/plain;');
	echo getFiles();
} elseif ($method === 'updatetext') {
	$textData = filter_input(INPUT_POST, 'text');
	if (saveText($textData) === false) {
		die('There was an error writing the text');
	}
	echo saveStatus(Types::Text);
} elseif ($method === 'updatefiles') {
		# Create / clear the files folder
		$filesFolderPath = buildFilesPath();
		createOrEmptyDir($filesFolderPath);
		foreach ($_FILES['file']['name'] as $f => $name) {
			$targetPath = $filesFolderPath . html_entity_decode($name);
			move_uploaded_file($_FILES['file']['tmp_name'][$f], $targetPath);
		}
		echo saveStatus(Types::Files);
		exit;
	}
} elseif ($method === 'upload') {
	
} elseif ($method === 'download') {
	# Download the file requested
	$filename = filter_input(INPUT_POST, 'file');
	$fullPath = buildFilesPath() . $filename;
	header("Content-Type: application/octet-stream");
	header("Content-Transfer-Encoding: Binary");
	header("Content-Length: " . filesize($fullPath));
	header("Content-Disposition: attachment; filename=\"$filename\"");
	readfile($fullPath);
}

function saveStatus($type) {
	$filePath =  buildStatusFilePath();
	$content = buildStatusArray(time(), $type);
	$contentJson = json_encode($content);
	file_put_contents($filePath, $contentJson, LOCK_EX);
	return $contentJson;
}

function getStatus() {
	$filePath =  buildStatusFilePath();
	$contentJson = @file_get_contents($filePath);
	if ($contentJson === false) {
		return buildStatusArray(0, Types::Nothing);
	}
	$content = json_decode($contentJson, true);
	return $content;
}

function saveText($text) {
	$filePath =  buildTextFilePath();
	return file_put_contents($filePath, $text, LOCK_EX);
}

function getText() {
	$filePath =  buildTextFilePath();
	$content = file_get_contents($filePath);
	if ($content === false) {
		return '';
	}
	return $content;
}

function getFiles() {
	$rootPath = buildFilesPath();
	$files = array_diff(scandir($rootPath), array('..', '.'));
	return implode(PHP_EOL, $files);
}

function buildStatusFilePath() {
	GLOBAL $idFolder;
	return $idFolder . 'status.json';
}

function buildTextFilePath() {
	GLOBAL $idFolder;
	return $idFolder . 'text.txt';
}

function buildFilesPath() {
	GLOBAL $idFolder;
	return $idFolder . 'files' . DIRECTORY_SEPARATOR;
}

function buildStatusArray($time, $type) {
	return array('date' => $time, 'type' => Types::convertToString($type));
}

function createOrEmptyDir($dirPath) {
	if(!is_dir($dirPath)) {
		mkdir($dirPath);
	} else {
		$di = new RecursiveDirectoryIterator($dirPath, FilesystemIterator::SKIP_DOTS);
		$ri = new RecursiveIteratorIterator($di, RecursiveIteratorIterator::CHILD_FIRST);
		foreach ($ri as $file) {
			$file->isDir() ? rmdir($file) : unlink($file);
		}
	}
}

class Types
{
    const Nothing = 0;
    const Text = 1;
    const Files = 2;
	
	static public function convertToString($value) {
		if ($value == self::Nothing) {
			return 'nothing';
		}
		if ($value == self::Text) {
			return 'text';
		}
		if ($value == self::Files) {
			return 'files';
		}
	}
}

?>
