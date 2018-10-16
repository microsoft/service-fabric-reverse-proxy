setlocal 

set TAG=0.20.0
set BRANCH=%1
set GIT_COMMIT=%2
set DOCKER_USERNAME=%3
set DOCKER_PASSWORD=%4

if "%BRANCH%" == "master" set BRANCH=seabreeze
if "%GIT_COMMIT%" == "" set GIT_COMMIT=Unknown

echo TAG=%TAG%
echo BRANCH=%BRANCH%
echo GIT_COMMIT=%GIT_COMMIT%

set config=Release

set PUBLISH_CMD=dotnet publish -c %config% . -r win10-x64 --self-contained
echo %PUBLISH_CMD%
%PUBLISH_CMD%

set publish_path=.\bin\%config%\netcoreapp2.0\win10-x64\publish

REM # Build the Docker images
set docker_cmd=docker build -t %BRANCH%/service-fabric-reverse-proxy:windows-%TAG% --label GIT_COMMIT=%GIT_COMMIT% -f %publish_path%\Dockerfile.windows %publish_path%\.
echo %docker_cmd%
%docker_cmd%
echo docker tag %BRANCH%/service-fabric-reverse-proxy:windows-%TAG% %BRANCH%/service-fabric-reverse-proxy:windows-latest
docker tag %BRANCH%/service-fabric-reverse-proxy:windows-%TAG% %BRANCH%/service-fabric-reverse-proxy:windows-latest

echo docker push %BRANCH%/service-fabric-reverse-proxy:windows-%TAG%
docker push %BRANCH%/service-fabric-reverse-proxy:windows-%TAG%
echo docker push %BRANCH%/service-fabric-reverse-proxy:windows-latest
docker push %BRANCH%/service-fabric-reverse-proxy:windows-latest
