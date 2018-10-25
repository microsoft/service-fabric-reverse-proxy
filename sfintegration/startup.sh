#!/bin/bash

# timestamp function
timestamp() {
  echo [$(date +"%Y-%m-%d %H:%M:%S.%3N%Z")][info][startup.sh]
}

# timestamp function
timestamperror() {
  echo [$(date +"%Y-%m-%d %H:%M:%S.%3N%Z")][error][startup.sh]
}

if [ "${Fabric_Folder_App_Log}" == "" ]
then
    Fabric_Folder_App_Log=./log
    mkdir ./log
else
    ln -s ${Fabric_Folder_App_Log} ./log
fi
reverse_proxy_log_path=${Fabric_Folder_App_Log}/sfreverseproxy.stdout

set | tee -a ${reverse_proxy_log_path}.log
cat /proc/sys/kernel/random/entropy_avail

use_https=${UseHttps:-false}
gateway_mode=${GatewayMode:-false}
if [ ${gateway_mode,,} == "true" ]
then
    config_file=config.gateway.json
else
    if [ ${use_https,,} == "true" ]
    then
        if [ -z "${ReverseProxyCertThumbprint}" ]
        then
            echo $(timestamperror) Invalid Reverse Proxy Thumbprint | tee -a ${reverse_proxy_log_path}.log
            exit 1
        fi
        sed -e s/ReverseProxyCertThumbprint/$(echo ${ReverseProxyCertThumbprint} | sed 's/[&/\]/\\&/g')/g \
            config.secure.template.json > ./config.secure.json
        config_file=config.secure.json
    else
        config_file=config.json
    fi
fi

if [ "${Fabric_NodeName}" == "" ]
then
    Fabric_NodeName="standalone"
fi

echo $(timestamp) Begin validate Envoy cofigfile, $config_file | tee -a ${reverse_proxy_log_path}.log
echo $(timestamp) /usr/local/bin/envoy -c ${config_file} --service-cluster ReverseProxy --service-node ${Fabric_NodeName} --mode validate | tee -a ${reverse_proxy_log_path}.log
/usr/local/bin/envoy --disable-hot-restart -c ${config_file} --service-cluster ReverseProxy --service-node ${Fabric_NodeName} --mode validate 2>&1 | tee -a "${reverse_proxy_log_path}.log"
retval=$?
if [ $retval -eq 1 ]
then
    echo $(timestamperror) Failed validate Envoy cofigfile, $config_file | tee -a "${reverse_proxy_log_path}.log"
    exit 1
fi
echo $(timestamp) Succeeded validate Envoy cofigfile, $config_file | tee -a "${reverse_proxy_log_path}.log" 
echo $(timestamp) /usr/local/bin/envoy --max-obj-name-len 180 -l info --disable-hot-restart -c ${config_file} --service-cluster ReverseProxy --service-node ${Fabric_NodeName} | tee -a "${reverse_proxy_log_path}.log"
echo $(timestamp) LD_LIBRARY_PATH=/opt/microsoft/servicefabric/bin/Fabric/Fabric.Code:. FabricPackageFileName= ./sfintegration | tee -a "${reverse_proxy_log_path}.log"

/usr/local/bin/envoy --max-obj-name-len 180 -l info --disable-hot-restart -c ${config_file} --service-cluster ReverseProxy --service-node ${Fabric_NodeName}  2>&1 | tee -a "${reverse_proxy_log_path}.envoy.log" &
LD_LIBRARY_PATH=/opt/microsoft/servicefabric/bin/Fabric/Fabric.Code:. FabricPackageFileName= ./sfintegration  2>&1 | tee -a "${reverse_proxy_log_path}.sfintegration.log"
