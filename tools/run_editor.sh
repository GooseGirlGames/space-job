#!/usr/bin/bash

EDITOR_VERSION="2020.3.33f1"

if [ -z "$UNITYHUB" ]; then
    UNITYHUB=$(which unityhub)
    if [ -z "$UNITYHUB" ] && [ -f "$HOME/Downloads/UnityHub.AppImage" ]; then
        UNITYHUB="~/Downloads/UnityHub.AppImage"
    fi
fi

if [ -z "$UNITYHUB" ]; then
    echo "UnityHub not found :("
    echo "Please specify the location of your install like this"
    echo "    UNITYHUB=/path/to/hub $0 ..."
    exit 1
fi

EDITOR="$($UNITYHUB --headless editors -i | grep $EDITOR_VERSION | sed -e "s/^.*installed at //")"

if [ -z "$EDITOR" ]; then
    echo "Unity Editor not found :("
    echo "Do you have version $EDITOR_VERSION installed?"
    exit 1
fi

$EDITOR $@
