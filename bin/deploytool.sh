#!/bin/bash

TOOLNAME=$1
SSHCONFIG=$2
APPCONFIG=$3
BUILDCONFIG=$4

if [[ -z "$BUILDCONFIG" ]]; then
    BUILDCONFIG=Release
fi

SCRIPTDIR=$(cd $(dirname $0); pwd -P)
SCRIPTNAME=$(basename $0)
SLNPATH=$(${SCRIPTDIR}/upfind.sh *.sln)

if [[ -z "$SLNPATH" ]]; then
    echo "Unable to find an .sln file in root directories"
    exit 1
fi

SLNDIR=$(dirname $SLNPATH)
SLNFILE=$(basename $SLNPATH)
APPNAME="${SLNFILE%.*}"
SCRATCHDIR=$SLNDIR/Scratch

if [[ ! -d $SCRATCHDIR ]]; then mkdir $SCRATCHDIR; fi

TOOLPROJDIR=${SLNDIR}/Tools/${TOOLNAME}

if [[ -z "$TOOLNAME" || -z "$SSHCONFIG" ]]; then 
    echo "Usage: $SCRIPTNAME TOOLNAME SSHCONFIG APPCONFIG BUILDCONFIG"
    if [[ -z "$TOOLNAME" ]]; then
        echo "TOOLNAME is one of:"
        find "${SLNDIR}/Tools" -depth 1 -type d -exec basename {} \;
    fi
    echo "SSHCONFIG is a name listed in the ~/.ssh/config file"
    if [[ -n "$TOOLNAME" ]]; then
        echo "APPCONFIG's available for '${TOOLNAME}':"
        find "${TOOLPROJDIR}" -depth 1 -name app.\*.config -exec basename {} \; | sed 's/app.//;s/.config//'
    else
        echo "APPCONFIG's be discovered by giving TOOLNAME"
    fi
    echo "BUILDCONFIG is Release or Debug"
    exit 1
fi


# Read in version
VERSION=$(cat ${SLNDIR}/Scratch/$APPNAME.version.txt)
VERSION=v$(expr $VERSION : '\([0-9]*\.[0-9]*\)')
TOOLLIBDIR=lib/${APPNAME}.${TOOLNAME}.${VERSION}

# Delete the BUILDCONFIG build directories
rm -rf ${TOOLPROJDIR}/bin/${BUILDCONFIG}

# Do a build, and stop if it fails
bash -c "cd $SLNDIR; xbuild /property:Configuration=${BUILDCONFIG} ${TOOLPROJDIR}/${TOOLNAME}.csproj"
if [[ $? -ne 0 ]]; then exit 1; fi

# TODO: Run any unit tests
# TODO: Run the service feature tests (locally)

# Create remote bin & lib directories
ssh $SSHCONFIG "if [[ ! -d bin ]]; then mkdir -p bin; fi"
ssh $SSHCONFIG "if [[ ! -d ${TOOLLIBDIR} ]]; then mkdir -p ${TOOLLIBDIR}; fi"

# Synchronize the local and remote directories
rsync -rtvzp --delete --progress --rsh=ssh ${TOOLPROJDIR}/bin/${BUILDCONFIG}/* ubuntu@${SSHCONFIG}:${TOOLLIBDIR}

# Put correct app.config in place
ssh $SSHCONFIG "cd ${TOOLLIBDIR}; echo '<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><appSettings file=\"app.${APPCONFIG}.config\"></appSettings></configuration>' > ${TOOLNAME}.exe.config"
if [[ $? -ne 0 ]]; then exit 1; fi

# Create new symbolic links
ssh $SSHCONFIG "find ~/${TOOLLIBDIR}/Scripts -name \*.sh | while read -r FILENAME; do ln -sf \$FILENAME ~/bin/\$(basename \$FILENAME); done"

