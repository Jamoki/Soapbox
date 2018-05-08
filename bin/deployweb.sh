#!/bin/bash

WEBAPPNAME=$1
SSHCONFIG=$2

SCRIPTDIR=$(cd $(dirname $0); pwd -P)
SCRIPTNAME=$(basename $0)
SLNPATH=$(${SCRIPTDIR}/upfind.sh *.sln)
SLNDIR=$(dirname $SLNPATH)
SLNFILE=$(basename $SLNPATH)
APPNAME="${SLNFILE%.*}"

if [[ -z "$SSHCONFIG" || -z "$WEBAPPNAME" ]]; then 
    echo "usage: $SCRIPTNAME WEBAPPNAME SSHCONFIG"
    echo "WEBAPPNAME is Code-o-rama or GirlGetAClue"
    echo "SSHCONFIG is a name listed in the ~/.ssh/config file"
    exit 1
fi

# Do a release build of the web site
bash -c "cd $SLNDIR/$WEBAPPNAME; gulp clean; gulp --minify --config=release"
if [[ $? -ne 0 ]]; then exit 1; fi

# TODO: Run the website unit tests

# Re-create the remote website directory
WEBDIR=www/${APPNAME}/${WEBAPPNAME}

# Create remote directories
ssh $SSHCONFIG "if [[ ! -d ${WEBDIR} ]]; then mkdir -p ${WEBDIR}; fi"
if [[ $? -ne 0 ]]; then exit 1; fi

# Synchronize the local and remote directories
rsync -rtvzp --delete --progress --rsh=ssh --exclude='articles/*' --exclude='content/*' $SLNDIR/$WEBAPPNAME/build/* ubuntu@${SSHCONFIG}:${WEBDIR}
