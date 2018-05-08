SCRIPTDIR=$(cd $(dirname $0); pwd -P)
$SCRIPTDIR/deploysvc.sh Api jamoki-alfa v1-0-release Release
$SCRIPTDIR/deployweb.sh Code-o-rama jamoki-alfa
$SCRIPTDIR/deploytool.sh MigrateDb jamoki-alfa v1-0-release Release
$SCRIPTDIR/deploytool.sh SiteEditor jamoki-alfa v1-0-release Release
$SCRIPTDIR/deploytool.sh SiteBuilder jamoki-alfa v1-0-release Release
$SCRIPTDIR/deploytool.sh UserEditor jamoki-alfa v1-0-release Release
