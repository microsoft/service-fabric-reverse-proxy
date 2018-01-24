#!/bin/bash

TAG=0.5.0
BRANCH=$1

if [ $BRANCH = "master" ]; then
    BRANCH=microsoft
fi

echo TAG=$TAG
echo BRANCH=$BRANCH

config=Release

echo dotnet build -c $config sfintegration
dotnet build -c $config sfintegration
echo dotnet publish -c $config sfintegration
dotnet publish -c $config sfintegration

# Build the Docker images
echo docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG ./sfintegration/bin/$config/netcoreapp2.0/publish/.
docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG ./sfintegration/bin/$config/netcoreapp2.0/publish/.
echo docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:xenial-$TAG
docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:xenial-$TAG
echo docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest
docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest