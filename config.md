# Config Explanation

## Nextcloud

|name|type|description|
|----|----|-----------|
|password|bytearray|stores the bytearray generated using password encryption (no clue how secure this is but better than plaintext)|
|username|string|duh|
|url|string|full url to the user's Nextcloud (including the dav route)|
|path|string|root directory where the saves are synced|

## Games [Array]

|name|type|description|
|----|----|-----------|
|id|string|game ID ToDo|
|type|string|native/wine stored as string for readability|
|prefixPath|string|wine prefix path, only used if wine is type|
|saveDir|string|absolute save directory|
|regex|???| -- I'll implement this when hell freezes over /s--|