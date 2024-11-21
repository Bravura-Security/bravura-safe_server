#!/usr/bin/env bash
set -e
# Get the current directory
current_dir=$(pwd)

# Get the file system type of the current directory
fs_type=$(df -T "$current_dir" | tail -1 | awk '{print $1}')

if [[ "$fs_type" == "drvfs" ]]; then
    echo ""
    echo ""
    echo "**************************************************************************"
    echo "You are on a Windows file system."
	echo "If using postgres, please beware of the following"
	echo "Postgres container will not start correctly on a windows file system"
	echo "The postgres data folder must be on the Linux FS"
	echo "Please fix the docker compose yaml or add an override file"
	echo "**************************************************************************"
	echo ""
	echo ""
else
    echo ""
fi

cat << "EOF"
BRAVURA-SECURITY
Bravura Safe
EOF

cat << EOF
Open source password management solutions
===================================================
EOF

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SCRIPT_NAME=$(basename "$0")
SCRIPT_PATH="$DIR/$SCRIPT_NAME"
OUTPUT="$DIR/bvdata"
if [ $# -eq 2 ]
then
    OUTPUT=$2
fi

# Check if docker-compose is installed
if command -v docker-compose &> /dev/null
then
    dccmd='docker-compose'
fi

# Check if Docker Compose v2 (as a plugin) is installed
if docker compose version &> /dev/null
then
    echo "Docker Compose v2 is installed"
    echo "________________________________"
    echo ""
    dccmd='docker compose'
fi

SCRIPTS_DIR="$OUTPUT/scripts"
GITHUB_BASE_URL="https://gitlab.hitachi-id.com/bravura-vault/server"

# Please do not create pull requests modifying the version numbers.
COREVERSION="latest"
WEBVERSION="latest"
KEYCONNECTORVERSION="1.0.1"

echo "bravura.sh version $COREVERSION"
docker --version
if [[ "$dccmd" == "docker compose" ]]; then
    $dccmd version
else
    $dccmd --version
fi

echo ""

# Functions

function downloadSelf() {
    if curl -s -w "http_code %{http_code}" -o $SCRIPT_PATH.1 $GITHUB_BASE_URL/scripts/bravura.sh | grep -q "^http_code 20[0-9]"
    then
        mv $SCRIPT_PATH.1 $SCRIPT_PATH
        chmod u+x $SCRIPT_PATH
    else
        rm -f $SCRIPT_PATH.1
    fi
}

function downloadRunFile() {
    if [ ! -d "$SCRIPTS_DIR" ]
    then
        mkdir $SCRIPTS_DIR
    fi

    # Until we have a published, public place to stash this, manually copy run.sh with bravura.sh and use that file
    # curl -s -o $SCRIPTS_DIR/run.sh $GITHUB_BASE_URL/scripts/run.sh
    cp run.sh $SCRIPTS_DIR/run.sh

    chmod u+x $SCRIPTS_DIR/run.sh
    rm -f $SCRIPTS_DIR/install.sh
}

function checkOutputDirExists() {
    if [ ! -d "$OUTPUT" ]
    then
        echo "Cannot find a Bravura Safe installation at $OUTPUT."
        exit 1
    fi
}

function checkOutputDirNotExists() {
    if [ -d "$OUTPUT/docker" ]
    then
        echo "Looks like Bravura Safe is already installed at $OUTPUT."
        echo "Exiting script"
        exit 1
    fi

    volume_name="docker_postgres_data"

    # Check if the volume exists
    if docker volume inspect "$volume_name" &>/dev/null; then
      echo "Volume $volume_name exists. Please remove."
      echo "Remove volume with command: docker volume rm $volume_name"
      echo "Exiting script"
      exit 1
    else
      # Volume $volume_name does not exist.
      echo ""
    fi
}

function listCommands() {
cat << EOT
Available commands:

install
start
restart
stop
update
updatedb
updaterun
updateself
updateconf
uninstall
renewcert
rebuild
help

EOT
}

# Commands

case $1 in
    "install")
        checkOutputDirNotExists
        mkdir -p $OUTPUT
        downloadRunFile
        $SCRIPTS_DIR/run.sh install $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        echo "*****"
        echo "*****"
        echo "Please remember after a fresh install to correctly set email username/password"
        echo "in the docker/docker-compose.yml if you wish to correctly relay emails via AWS SES"
        echo "Otherwise all emails will be sent only to maildev container and will never reach intended recipient."
        echo "Then restart containers if any setup changes are made."
        echo "*****"
        echo "*****"
        ;;
    "start" | "restart")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh restart $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "update")
        checkOutputDirExists
        downloadRunFile
        $SCRIPTS_DIR/run.sh update $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "rebuild")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh rebuild $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "updateconf")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh updateconf $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "updatedb")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh updatedb $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "stop")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh stop $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "renewcert")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh renewcert $OUTPUT $COREVERSION $WEBVERSION $KEYCONNECTORVERSION
        ;;
    "updaterun")
        checkOutputDirExists
        downloadRunFile
        ;;
    "updateself")
        # Until we have a published, public place to stash this, manually retrieve bravura.sh
        # downloadSelf && echo "Updated self." && exit
        echo "Please manually retrieve latest file." && exit
        ;;
    "uninstall")
        checkOutputDirExists
        $SCRIPTS_DIR/run.sh uninstall $OUTPUT
        ;;
    "help")
        listCommands
        ;;
    *)
        echo "No command found."
        echo
        listCommands
esac
