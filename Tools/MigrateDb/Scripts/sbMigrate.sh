#!/bin/bash
#
# WARNING: Do not edit these files in Xamarin Studio as they will get BOM's 
# which makes bash shell scripts spit out an error message before running script.
# Very annoying! :)  Delete the file and use another text editor to get rid of them.
#
MONO=/usr/local/bin/mono

if [[ ! -e $MONO ]]; then
    MONO=/usr/bin/mono
fi
 
 $MONO /home/ubuntu/lib/Soapbox.MigrateDb.v1.0/MigrateDb.exe $*

