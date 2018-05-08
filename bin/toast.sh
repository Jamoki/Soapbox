#!/bin/bash
#
# A script to find the newest version of the Toast tool in the NuGet packages directory under the solution root
#

PKGNAME=Toaster
TOOLNAME=Toast
PKGDIR=packages

# See http://stackoverflow.com/questions/4493205/unix-sort-of-version-numbers
mono $(find ${PKGDIR} -name ${PKGNAME}\.\* -type d -maxdepth 1 | sed -Ee 's/^(.*-)([0-9.]+)(\.ime)$/\2.-1 \1\2\3/' | sort -t. -n -r -k1,1 -k2,2 -k3,3 -k4,4 | head -1)/tools/${TOOLNAME}.exe $*