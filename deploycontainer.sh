#!/bin/bash
set -e

TAG=$1
BRANCH=$2
DOCKER_USERNAME=$3
DOCKER_PASSWORD=$4

if [ $BRANCH = "master" ]; then
    BRANCH=microsoft
fi

echo TAG=$TAG
echo BRANCH=$BRANCH
echo DOCKER_USERNAME=$DOCKER_USERNAME
echo DOCKER_PASSWORD=$DOCKER_PASSWORD

echo dotnet build -c Release sfintegration
dotnet build -c Release sfintegration
echo dotnet publish -c Release sfintegration
dotnet publish -c Release sfintegration

echo docker build -t reverseproxyimage ./sfintegration/bin/Release/netcoreapp2.0/publish/.
docker build -t reverseproxyimage ./sfintegration/bin/Release/netcoreapp2.0/publish/.

# Build the Docker images
echo docker build -t $BRANCH/reverseproxyimage:$TAG ./sfintegration/bin/Release/netcoreapp2.0/publish/.
docker build -t $BRANCH/reverseproxyimage:$TAG ./sfintegration/bin/Release/netcoreapp2.0/publish/.
echo docker tag $BRANCH/reverseproxyimage:$TAG $BRANCH/reverseproxyimage:latest
docker tag $BRANCH/reverseproxyimage:$TAG $BRANCH/reverseproxyimage:latest

# Login to Docker Hub and upload images
echo docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
echo docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
echo docker push $BRANCH/reverseproxyimage:$TAG
echo docker push $BRANCH/reverseproxyimage:$TAG
echo docker push $BRANCH/reverseproxyimage:latest
echo docker push $BRANCH/reverseproxyimage:latest
