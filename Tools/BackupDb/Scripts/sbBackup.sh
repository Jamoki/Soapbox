#!/bin/bash
MONO=/usr/local/bin/mono

if [[ ! -e $MONO ]]; then
    MONO=/usr/bin/mono
fi
 
$MONO ~/lib/Soapbox.BackupDb.v1.0/BackupDb.exe $*
