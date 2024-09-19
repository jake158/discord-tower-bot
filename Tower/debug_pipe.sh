#!/bin/bash

# Path to the named pipe (FIFO)
PIPE_PATH="/tmp/TowerScanPipe"

if [[ ! -p "$PIPE_PATH" ]]; then
    mkfifo "$PIPE_PATH"
    echo "Named pipe created at $PIPE_PATH"
fi

while true; do
    if read line <"$PIPE_PATH"; then
        echo "Received request: $line"
        echo "Debug pipe: No scan performed" > "$PIPE_PATH"
    fi
done

