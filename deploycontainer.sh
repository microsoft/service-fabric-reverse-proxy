#!/bin/bash
set -e

TAG=0.2.0
BRANCH=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3

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
echo docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest
docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest

# Login to Docker Hub and upload images
docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
#echo docker push $BRANCH/service-fabric-reverse-proxy:$TAG
docker push $BRANCH/service-fabric-reverse-proxy:$TAG
#echo docker push $BRANCH/service-fabric-reverse-proxy:latest
docker push $BRANCH/service-fabric-reverse-proxy:latest
