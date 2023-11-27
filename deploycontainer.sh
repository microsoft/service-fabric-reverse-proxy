#!/bin/bash

TAG=0.40.0
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

# Build the Docker images
echo docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG --label GIT_COMMIT=$GIT_COMMIT ./sfintegration/bin/$config/netcoreapp2.1/ubuntu.16.04-x64/publish/.
docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG --label GIT_COMMIT=$GIT_COMMIT ./sfintegration/bin/$config/netcoreapp2.1/ubuntu.16.04-x64/publish/.
echo docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:xenial-$TAG
docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:xenial-$TAG
echo docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest
docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest

# Login to Docker Hub and upload images
echo $DOCKER_PASSWORD | docker login -u="$DOCKER_USERNAME" --password-stdin
echo docker push $BRANCH/service-fabric-reverse-proxy:$TAG
docker push $BRANCH/service-fabric-reverse-proxy:$TAG
echo docker push $BRANCH/service-fabric-reverse-proxy:xenial-$TAG
docker push $BRANCH/service-fabric-reverse-proxy:xenial-$TAG
echo docker push $BRANCH/service-fabric-reverse-proxy:latest
docker push $BRANCH/service-fabric-reverse-proxy:latest
