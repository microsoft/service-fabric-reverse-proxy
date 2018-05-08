#!/bin/bash

# timestamp function
timestamp() {
  echo $(date +"%DT%T.%3N%Z")
}

mkdir -p /var/log/sfreverseproxy
reverse_proxy_log_path="/var/log/sfreverseproxy/stdout"

use_https=${UseHttps:-false}
if [ ${use_https,,} == "true" ]
then
    if [ -z "${ReverseProxyCertThumbprint}" ]
    then
        echo $(timestamp), startup.sh, Invalid Reverse Proxy Thumbprint > ${reverse_proxy_log_path}.log
        exit 1
    fi
    sed -e s/ReverseProxyCertThumbprint/$(echo ${ReverseProxyCertThumbprint} | sed 's/[&/\]/\\&/g')/g \
        config.secure.template.json > ./config.secure.json
    config_file=config.secure.json
else
    config_file=config.json
fi

if [ ${Fabric_NodeName} == ""]
then
    Fabric_NodeName="standalone"
fi

echo $(timestamp), startup.sh, Begin validate Envoy cofigfile, $config_file >> ${reverse_proxy_log_path}.log
echo /usr/local/bin/envoy -c ${config_file} --service-cluster ReverseProxy --service-node ${Fabric_NodeName} --mode validate
/usr/local/bin/envoy -c ${config_file} --service-cluster ReverseProxy --service-node ${Fabric_NodeName} --mode validate >> "${reverse_proxy_log_path}.log" 2>&1
retval=$?
if [ $retval -eq 1 ]
then
    echo $(timestamp), startup.sh, Failed validate Envoy cofigfile, $config_file >> "${reverse_proxy_log_path}.log" 2>&1
    exit 1
fi
echo $(timestamp), startup.sh, Succeeded validate Envoy cofigfile, $config_file >> "${reverse_proxy_log_path}.log" 2>&1


/usr/local/bin/envoy -l info -c ${config_file} --service-cluster ReverseProxy --service-node ${Fabric_NodeName} >> "${reverse_proxy_log_path}.envoy.log" 2>&1 &
LD_LIBRARY_PATH=/opt/microsoft/servicefabric/bin/Fabric/Fabric.Code:. FabricPackageFileName= dotnet sfintegration.dll >> "${reverse_proxy_log_path}.sfintegration.log" 2>&1
