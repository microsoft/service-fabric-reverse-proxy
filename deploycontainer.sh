#!/bin/bash

TAG=0.31.0
BRANCH=$1
GIT_COMMIT=$2
DOCKER_USERNAME=$3
DOCKER_PASSWORD=$4

if [ $BRANCH = "master" ]; then
    BRANCH=microsoft
fi

if [ -z $GIT_COMMIT ]; then
    GIT_COMMIT="Unknown"
fi

echo TAG=$TAG
echo BRANCH=$BRANCH
echo GIT_COMMIT=$GIT_COMMIT

config=Release

echo dotnet build -c $config sfintegration
dotnet build -c $config sfintegration
echo dotnet publish -c $config sfintegration -f netcoreapp2.1 -r ubuntu.16.04-x64 --self-contained
dotnet publish -c $config sfintegration -f netcoreapp2.1 -r ubuntu.16.04-x64 --self-contained

# Pull the previous image to speed up image generation
docker pull $BRANCH/service-fabric-reverse-proxy:latest

# Login to Docker Hub
docker login -u ${DOCKER_USERNAME} -p ${DOCKER_PASSWORD}
echo docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG --label GIT_COMMIT=$GIT_COMMIT ./sfintegration/bin/$config/netcoreapp2.1/ubuntu.16.04-x64/publish/.

# Build and upload images
docker buildx build --push --platform linux/amd64,linux/arm64 -t $BRANCH/service-fabric-reverse-proxy:$TAG -t $BRANCH/service-fabric-reverse-proxy:xenial-$TAG -t $BRANCH/service-fabric-reverse-proxy:latest --label GIT_COMMIT=$GIT_COMMIT ./sfintegration/bin/$config/netcoreapp2.1/ubuntu.16.04-x64/publish/.
