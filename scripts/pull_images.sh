#!/bin/bash

# Check if version tag is provided
if [ -z "$1" ]; then
  echo "Usage: $0 <version-tag>"
  exit 1
fi

# Assign the version tag to a variable
VERSION_TAG=$1

# Define the repository URL
REPO_URL="299258155391.dkr.ecr.us-east-1.amazonaws.com/bravura_safe_dev"

# Run the commands with the provided version tag
./testbuild.sh pull $REPO_URL $VERSION_TAG bravura_vault latest
./testbuild.sh tag $REPO_URL $VERSION_TAG bravura_vault latest
docker tag $REPO_URL/web:$VERSION_TAG bravura_vault/web:latest
