@echo off
setlocal 

set TEST_CONTAINER=-test
if "RELEASE_BUILD" == "TRUE" set TEST_CONTAINER=

set TAG=0.30.0%TEST_CONTAINER%
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
set deploy_environment=Mesh

call :do_build

set deploy_environment=Mesh_Development
call :do_build

exit /b 0

:do_build

set publish_path=.\bin\%config%\netcoreapp2.0\win10-x64\publish
if "%deploy_environment%" == "Mesh_Development" set publish_path=.\bin\%config%\%deploy_environment%\netcoreapp2.0\win10-x64\publish

set deploy_environment=%deploy_environment%
set PUBLISH_CMD=dotnet publish -c %config% . -r win10-x64 --self-contained -o %publish_path%
echo %PUBLISH_CMD%
%PUBLISH_CMD%

xcopy %ENVOY_BINARIES_PATH%\* %publish_path% /y

REM # Build the Docker images
set image_tag_base=windows
if "%deploy_environment%" == "Mesh_Development" set image_tag_base=windows-devenv
set docker_cmd=docker build -t %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-%TAG% --label GIT_COMMIT=%GIT_COMMIT% -f %publish_path%\Dockerfile.windows %publish_path%\.
echo %docker_cmd%
%docker_cmd%
echo docker tag %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-%TAG% %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-latest%TEST_CONTAINER%
docker tag %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-%TAG% %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-latest%TEST_CONTAINER%

echo docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-%TAG%
docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-%TAG%
echo docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-latest%TEST_CONTAINER%
docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-latest%TEST_CONTAINER%

set docker_cmd=docker build -t %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-%TAG% --label GIT_COMMIT=%GIT_COMMIT% -f %publish_path%\Dockerfile.windows.1709 %publish_path%\.
echo %docker_cmd%
%docker_cmd%
echo docker tag %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-%TAG% %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-latest%TEST_CONTAINER%
docker tag %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-%TAG% %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-latest%TEST_CONTAINER%

echo docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-%TAG%
docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-%TAG%
echo docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-latest%TEST_CONTAINER%
docker push %BRANCH%/service-fabric-reverse-proxy:%image_tag_base%-1709-latest%TEST_CONTAINER%
exit /b 0
