#!/bin/bash
TOOL=SiteBuilder
SCRIPTDIR=$(cd $(dirname $0); pwd -P)
SLNPATH=$(${SCRIPTDIR}/upfind.sh *.sln)
SLNDIR=$(dirname $SLNPATH)
CONFIG=Debug
MONO=/usr/local/bin/mono

if [[ ! -e $MONO ]]; then
    MONO=/usr/bin/mono
fi
 
$MONO $SLNDIR/Tools/$TOOL/bin/$CONFIG/$TOOL.exe $*
