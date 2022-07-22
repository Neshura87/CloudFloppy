# Config Explanation

## Nextcloud

|name|type|description|
|----|----|-----------|
|credentials|string|stores the bytearray generated using password and username|
|username|string|duh|
|url|string|full url to the user's Nextcloud (including the dav route)|

## Games [Array]

|name|type|description|
|----|----|-----------|
|id|string|game ID ToDo|
|type|string|native/wine stored as string for readability|
|prefixPath|string|wine prefix path, only used if wine is type|
|saveDir|string|absolute save directory|
|regex|???| -- I'll implement this when hell freezes over /s--|