#!/bin/bash
set -e

TAG=0.1.0
BRANCH=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3

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

echo docker build -t service-fabric-reverse-proxy ./sfintegration/bin/Release/netcoreapp2.0/publish/.
docker build -t service-fabric-reverse-proxy ./sfintegration/bin/Release/netcoreapp2.0/publish/.

# Build the Docker images
echo docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG ./sfintegration/bin/Release/netcoreapp2.0/publish/.
docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG ./sfintegration/bin/Release/netcoreapp2.0/publish/.
echo docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest
docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest

# Login to Docker Hub and upload images
docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
echo docker push $BRANCH/service-fabric-reverse-proxy:$TAG
docker push $BRANCH/service-fabric-reverse-proxy:$TAG
echo docker push $BRANCH/service-fabric-reverse-proxy:latest
docker push $BRANCH/service-fabric-reverse-proxy:latest
