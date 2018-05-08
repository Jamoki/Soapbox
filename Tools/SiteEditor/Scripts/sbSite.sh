#!/bin/bash
MONO=/usr/local/bin/mono

if [[ ! -e $MONO ]]; then
    MONO=/usr/bin/mono
fi
 
$MONO /home/ubuntu/lib/Soapbox.SiteEditor.v1.0/SiteEditor.exe $*

