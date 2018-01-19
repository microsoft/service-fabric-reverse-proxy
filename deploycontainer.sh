#!/bin/bash

TAG=0.2.0
BRANCH=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3

if [ $BRANCH = "master" ]; then
    BRANCH=microsoft
fi

echo -e \e[92TAG=$TAG
echo -e \e[92BRANCH=$BRANCH

config=Release

echo -e \e[92dotnet build -c $config sfintegration
dotnet build -c $config sfintegration
echo -e \e[92dotnet publish -c $config sfintegration
dotnet publish -c $config sfintegration

# Build the Docker images
echo -e \e[92docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG ./sfintegration/bin/$config/netcoreapp2.0/publish/.
docker build -t $BRANCH/service-fabric-reverse-proxy:$TAG ./sfintegration/bin/$config/netcoreapp2.0/publish/.
echo -e \e[92docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest
docker tag $BRANCH/service-fabric-reverse-proxy:$TAG $BRANCH/service-fabric-reverse-proxy:latest

# Login to Docker Hub and upload images
echo $DOCKER_PASSWORD | docker login -u="$DOCKER_USERNAME" --password-stdin
echo -e \e[92docker push $BRANCH/service-fabric-reverse-proxy:$TAG
docker push $BRANCH/service-fabric-reverse-proxy:$TAG
echo -e \e[92docker push $BRANCH/service-fabric-reverse-proxy:latest
docker push $BRANCH/service-fabric-reverse-proxy:latest
