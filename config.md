# Config Explanation

## Nextcloud

|name|type|description|
|----|----|-----------|
|Password|bytearray|stores the bytearray generated using password encryption (no clue how secure this is but better than plaintext)|
|Username|string|duh|
|Url|string|full url to the user's Nextcloud (including the dav route)|
|SaveDir|string|root directory where the saves are synced|

## rsync/ssh
|name|type|description|
|Username|string|duh|
|Host|string|Hostname of sync target|
|SaveDir|string|root directroy where the saves are synced|

## Games [Array]

|name|type|description|
|----|----|-----------|
|id|string|game ID ToDo|
|Name|string||
|GameType|GameType|Wine or Native|
|SaveRoot|SaveRoot|game's save directory root|
|SaveRootSubdirectroy|string|subdirectory of the save root if needed|
|GameDirectory|string|Path to the Game Directory|
|ShellCommand|string|command to run the game|
|WinePrefix|string|if GameType is Wine this stores the used prefix|
|IncludeRegex|string|regex for explicitly included files in the save dir|
|ExcludeRegex|string|regex for explicitly excluded files in the save dir|
